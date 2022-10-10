using Refit;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.DataServices.Refit
{
    internal class RequestBuilder : IRequestBuilder
    {
        private static readonly ISet<HttpMethod> BodylessMethods = new HashSet<HttpMethod>
        {
            HttpMethod.Get,
            HttpMethod.Head,
        };

        private readonly Dictionary<string, List<RestMethodInfo>> interfaceHttpMethods;
        private readonly ConcurrentDictionary<CloseGenericMethodKey, RestMethodInfo> interfaceGenericHttpMethods;
        private readonly global::Refit.IHttpContentSerializer serializer;
        private readonly RefitSettings settings;

        public Type TargetType { get; }

        public RequestBuilder(Type refitInterfaceType, RefitSettings? refitSettings = null)
        {
            var targetInterfaceInheritedInterfaces = refitInterfaceType.GetInterfaces();

            this.settings = refitSettings ?? new RefitSettings();
            this.serializer = this.settings.ContentSerializer;
            this.interfaceGenericHttpMethods = new ConcurrentDictionary<CloseGenericMethodKey, RestMethodInfo>();

            if (!refitInterfaceType.GetTypeInfo().IsInterface)
            {
                throw new ArgumentException("targetInterface must be an Interface");
            }

            this.TargetType = refitInterfaceType;

            var dict = new Dictionary<string, List<RestMethodInfo>>();

            this.AddInterfaceHttpMethods(refitInterfaceType, dict);
            foreach (var inheritedInterface in targetInterfaceInheritedInterfaces)
            {
                this.AddInterfaceHttpMethods(inheritedInterface, dict);
            }

            this.interfaceHttpMethods = dict;
        }

        private void AddInterfaceHttpMethods(Type interfaceType, Dictionary<string, List<RestMethodInfo>> methods)
        {
#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
            var methodInfos = interfaceType
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .Where(i => i.IsAbstract);
#pragma warning restore S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields

            foreach (var methodInfo in methodInfos)
            {
                var attrs = methodInfo.GetCustomAttributes(true);
                var hasHttpMethod = attrs.OfType<HttpMethodAttribute>().Any();
                if (hasHttpMethod)
                {
                    if (!methods.ContainsKey(methodInfo.Name))
                    {
                        methods.Add(methodInfo.Name, new List<RestMethodInfo>());
                    }

                    var restinfo = new RestMethodInfo(interfaceType, methodInfo, this.settings);
                    methods[methodInfo.Name].Add(restinfo);
                }
            }
        }

        private RestMethodInfo FindMatchingRestMethodInfo(string key, Type[]? parameterTypes, Type[]? genericArgumentTypes)
        {
            if (this.interfaceHttpMethods.TryGetValue(key, out var httpMethods))
            {
                if (parameterTypes == null)
                {
                    if (httpMethods.Count > 1)
                    {
                        throw new ArgumentException($"MethodName exists more than once, '{nameof(parameterTypes)}' mut be defined");
                    }

                    return this.CloseGenericMethodIfNeeded(httpMethods[0], genericArgumentTypes);
                }

                var isGeneric = genericArgumentTypes?.Length > 0;
                var possibleMethodsList = httpMethods.Where(method => method.MethodInfo.GetParameters().Length == parameterTypes.Length);

                if (isGeneric)
                {
                    possibleMethodsList = possibleMethodsList.Where(method => method.MethodInfo.IsGenericMethod && method.MethodInfo.GetGenericArguments().Length == genericArgumentTypes!.Length);
                }
                else
                {
                    possibleMethodsList = possibleMethodsList.Where(method => !method.MethodInfo.IsGenericMethod);
                }

                var possibleMethods = possibleMethodsList.ToList();

                if (possibleMethods.Count == 1)
                {
                    return this.CloseGenericMethodIfNeeded(possibleMethods[0], genericArgumentTypes);
                }

                var parameterTypesArray = parameterTypes.ToArray();
                foreach (var method in possibleMethods)
                {
                    var match = method.MethodInfo.GetParameters()
                                      .Select(p => p.ParameterType)
                                      .SequenceEqual(parameterTypesArray);
                    if (match)
                    {
                        return this.CloseGenericMethodIfNeeded(method, genericArgumentTypes);
                    }
                }

                throw new InvalidOperationException("No suitable Method found...");
            }
            else
            {
                throw new ArgumentException("Method must be defined and have an HTTP Method attribute");
            }
        }

        private RestMethodInfo CloseGenericMethodIfNeeded(RestMethodInfo restMethodInfo, Type[]? genericArgumentTypes)
        {
            if (genericArgumentTypes != null)
            {
                return this.interfaceGenericHttpMethods.GetOrAdd(
                    new CloseGenericMethodKey(restMethodInfo.MethodInfo, genericArgumentTypes),
                    _ => new RestMethodInfo(restMethodInfo.Type, restMethodInfo.MethodInfo.MakeGenericMethod(genericArgumentTypes), restMethodInfo.RefitSettings));
            }

            return restMethodInfo;
        }

        public Func<HttpClient, object[], object?> BuildRestResultFuncForMethod(string methodName, Type[]? parameterTypes = null, Type[]? genericArgumentTypes = null)
        {
            if (!this.interfaceHttpMethods.ContainsKey(methodName))
            {
                throw new ArgumentException("Method must be defined and have an HTTP Method attribute");
            }

            var restMethod = this.FindMatchingRestMethodInfo(methodName, parameterTypes, genericArgumentTypes);
            if (restMethod.ReturnType == typeof(Task))
            {
                return this.BuildVoidTaskFuncForMethod(restMethod);
            }

            if (restMethod.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
                var taskFuncMi = typeof(RequestBuilder).GetMethod(nameof(this.BuildTaskFuncForMethod), BindingFlags.NonPublic | BindingFlags.Instance);
#pragma warning restore S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
                var taskFunc = (MulticastDelegate?)taskFuncMi!.MakeGenericMethod(restMethod.ReturnResultType, restMethod.DeserializedResultType).Invoke(this, new[] { restMethod });

                return (client, args) => taskFunc!.DynamicInvoke(client, args);
            }

#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
            var rx1FuncMi = typeof(RequestBuilder).GetMethod(nameof(this.BuildRxFuncForMethod), BindingFlags.NonPublic | BindingFlags.Instance);
#pragma warning restore S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
            var rx1Func = (MulticastDelegate?)rx1FuncMi!.MakeGenericMethod(restMethod.ReturnResultType, restMethod.DeserializedResultType).Invoke(this, new[] { restMethod });

            return (client, args) => rx1Func!.DynamicInvoke(client, args);
        }

        private void AddMultipartItem(MultipartFormDataContent multiPartContent, string fileName, string parameterName, object itemValue)
        {
            if (itemValue is HttpContent content)
            {
                multiPartContent.Add(content);
                return;
            }

            if (itemValue is MultipartItem multipartItem)
            {
                var httpContent = multipartItem.ToContent();
                multiPartContent.Add(httpContent, multipartItem.Name ?? parameterName, string.IsNullOrEmpty(multipartItem.FileName) ? fileName : multipartItem.FileName);
                return;
            }

            if (itemValue is Stream streamValue)
            {
                var streamContent = new StreamContent(streamValue);
                multiPartContent.Add(streamContent, parameterName, fileName);
                return;
            }

            if (itemValue is string stringValue)
            {
                multiPartContent.Add(new StringContent(stringValue), parameterName);
                return;
            }

            if (itemValue is FileInfo fileInfoValue)
            {
                var fileContent = new StreamContent(fileInfoValue.OpenRead());
                multiPartContent.Add(fileContent, parameterName, fileInfoValue.Name);
                return;
            }

            if (itemValue is byte[] byteArrayValue)
            {
                var fileContent = new ByteArrayContent(byteArrayValue);
                multiPartContent.Add(fileContent, parameterName, fileName);
                return;
            }

            Exception e;
            try
            {
                multiPartContent.Add(this.settings.ContentSerializer.ToHttpContent(itemValue), parameterName);
                return;
            }
            catch (Exception ex)
            {
                e = ex;
            }

            throw new ArgumentException($"Unexpected parameter type in a Multipart request. Parameter {fileName} is of type {itemValue.GetType().Name}, whereas allowed types are String, Stream, FileInfo, Byte array and anything that's JSON serializable", nameof(itemValue), e);
        }

        private Func<HttpClient, CancellationToken, object[], Task<T>> BuildCancellableTaskFuncForMethod<T, TBody>(RestMethodInfo restMethod)
        {
            return async (client, cancellationToken, paramList) =>
            {
                if (client.BaseAddress == null)
                {
                    throw new InvalidOperationException("BaseAddress must be set on the HttpClient instance");
                }

                var factory = this.BuildRequestFactoryForMethod(restMethod, client.BaseAddress.AbsolutePath, restMethod.CancellationToken != null);
                var rq = factory(paramList);
                HttpResponseMessage? resp = null;
                HttpContent? content = null;
                var disposeResponse = true;
                try
                {
                    if ((restMethod.BodyParameterInfo?.Item2).GetValueOrDefault() && rq.Content != null)
                    {
                        await rq.Content!.LoadIntoBufferAsync().ConfigureAwait(false);
                    }

                    resp = await client.SendAsync(rq, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
                    content = resp.Content ?? new StringContent(string.Empty);
                    Exception? exception = null;
                    disposeResponse = restMethod.ShouldDisposeResponse;

                    if (typeof(T) != typeof(HttpResponseMessage))
                    {
                        exception = await this.settings.ExceptionFactory(resp).ConfigureAwait(false);
                    }

                    if (restMethod.IsApiResponse)
                    {
                        var body = default(TBody);

                        try
                        {
                            body = exception == null
                                ? await this.DeserializeContentAsync<TBody>(restMethod, resp, content, cancellationToken).ConfigureAwait(false)
                                : default;
                        }
                        catch (Exception ex)
                        {
                            exception = await ApiException.Create(
                                "An error occured deserializing the response.",
                                resp.RequestMessage!,
                                resp.RequestMessage!.Method,
                                resp,
                                this.settings,
                                ex);
                        }

                        return ApiResponse.Create<T, TBody>(resp, body, this.settings, exception as ApiException);
                    }
                    else if (exception != null)
                    {
                        disposeResponse = false;
                        throw exception;
                    }
                    else
                    {
                        try
                        {
                            return await this.DeserializeContentAsync<T>(restMethod, resp, content, cancellationToken).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            throw await ApiException.Create(
                                "An error occured deserializing the response.",
                                resp.RequestMessage!,
                                resp.RequestMessage!.Method,
                                resp,
                                this.settings,
                                ex);
                        }
                    }
                }
                finally
                {
                    rq.Dispose();
                    if (disposeResponse)
                    {
                        resp?.Dispose();
                        content?.Dispose();
                    }
                }
            };
        }

        protected virtual async Task<T> DeserializeContentAsync<T>(
            RestMethodInfo restMethod,
            HttpResponseMessage resp,
            HttpContent content,
            CancellationToken cancellationToken)
        {
            T result;
            if (typeof(T) == typeof(HttpResponseMessage))
            {
                result = (T)(object)resp;
            }
            else if (typeof(T) == typeof(HttpContent))
            {
                result = (T)(object)content;
            }
            else if (typeof(T) == typeof(Stream))
            {
                var stream = (object)await content.ReadAsStreamAsync().ConfigureAwait(false);
                result = (T)stream;
            }
            else if (typeof(T) == typeof(string))
            {
                using var stream = await content.ReadAsStreamAsync().ConfigureAwait(false);
                using var reader = new StreamReader(stream);
                var str = (object)await reader.ReadToEndAsync().ConfigureAwait(false);
                result = (T)str;
            }
            else
            {
                result = (await this.serializer.FromHttpContentAsync<T>(content, cancellationToken).ConfigureAwait(false)) !;
            }

            return result!;
        }

        private List<KeyValuePair<string, object?>> BuildQueryMap(object? @object, string? delimiter = null, RestMethodParameterInfo? parameterInfo = null)
        {
            if (@object is System.Collections.IDictionary idictionary)
            {
                return this.BuildQueryMap(idictionary, delimiter);
            }

            var kvps = new List<KeyValuePair<string, object?>>();

            if (@object is null)
            {
                return kvps;
            }

            var props = @object.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.CanRead && p.GetMethod?.IsPublic == true);

            foreach (var propertyInfo in props)
            {
                var obj = propertyInfo.GetValue(@object);
                if (obj == null)
                {
                    continue;
                }

                if (parameterInfo != null &&
                    parameterInfo.IsObjectPropertyParameter &&
                    parameterInfo.ParameterProperties.Any(x => x.PropertyInfo == propertyInfo))
                {
                    continue;
                }

                var key = propertyInfo.Name;

                var aliasAttribute = propertyInfo.GetCustomAttribute<AliasAsAttribute>();
                if (aliasAttribute != null)
                {
                    key = aliasAttribute.Name;
                }

                var queryAttribute = propertyInfo.GetCustomAttribute<QueryAttribute>();
                if (queryAttribute != null && queryAttribute.Format != null)
                {
                    obj = this.settings.FormUrlEncodedParameterFormatter.Format(obj, queryAttribute.Format);
                }

                if (!(obj is string) && obj is System.Collections.IEnumerable ienu && !(obj is System.Collections.IDictionary))
                {
                    foreach (var value in this.ParseEnumerableQueryParameterValue(ienu, propertyInfo, propertyInfo.PropertyType, queryAttribute))
                    {
                        kvps.Add(new KeyValuePair<string, object?>(key, value));
                    }

                    continue;
                }

                if (DoNotConvertToQueryMap(obj))
                {
                    kvps.Add(new KeyValuePair<string, object?>(key, obj));
                    continue;
                }

                switch (obj)
                {
                    case System.Collections.IDictionary idict:
                        foreach (var keyValuePair in this.BuildQueryMap(idict, delimiter))
                        {
                            kvps.Add(new KeyValuePair<string, object?>($"{key}{delimiter}{keyValuePair.Key}", keyValuePair.Value));
                        }

                        break;

                    default:
                        foreach (var keyValuePair in this.BuildQueryMap(obj, delimiter))
                        {
                            kvps.Add(new KeyValuePair<string, object?>($"{key}{delimiter}{keyValuePair.Key}", keyValuePair.Value));
                        }

                        break;
                }
            }

            return kvps;
        }

        private List<KeyValuePair<string, object?>> BuildQueryMap(System.Collections.IDictionary dictionary, string? delimiter = null)
        {
            var kvps = new List<KeyValuePair<string, object?>>();

            foreach (var key in dictionary.Keys)
            {
                var obj = dictionary[key];
                if (obj == null)
                {
                    continue;
                }

                var keyType = key.GetType();
                var formattedKey = this.settings.UrlParameterFormatter.Format(key, keyType, keyType);

                if (string.IsNullOrWhiteSpace(formattedKey))
                {
                    continue;
                }

                if (DoNotConvertToQueryMap(obj))
                {
                    kvps.Add(new KeyValuePair<string, object?>(formattedKey!, obj));
                }
                else
                {
                    foreach (var keyValuePair in this.BuildQueryMap(obj, delimiter))
                    {
                        kvps.Add(new KeyValuePair<string, object?>($"{formattedKey}{delimiter}{keyValuePair.Key}", keyValuePair.Value));
                    }
                }
            }

            return kvps;
        }

        private Func<object[], HttpRequestMessage> BuildRequestFactoryForMethod(RestMethodInfo restMethod, string basePath, bool paramsContainsCancellationToken)
        {
            return paramList =>
            {
                if (paramsContainsCancellationToken)
                {
                    paramList = paramList.Where(o => !(o is CancellationToken)).ToArray();
                }

                var ret = new HttpRequestMessage
                {
                    Method = restMethod.HttpMethod,
                };

                MultipartFormDataContent? multiPartContent = null;
                if (restMethod.IsMultipart)
                {
                    multiPartContent = new MultipartFormDataContent(restMethod.MultipartBoundary);
                    ret.Content = multiPartContent;
                }

                var urlTarget = (basePath == "/" ? string.Empty : basePath) + restMethod.RelativePath;
                var queryParamsToAdd = new List<KeyValuePair<string, string?>>();
                var headersToAdd = new Dictionary<string, string?>(restMethod.Headers);
                var propertiesToAdd = new Dictionary<string, object?>();

                RestMethodParameterInfo? parameterInfo = null;

                for (var i = 0; i < paramList.Length; i++)
                {
                    var isParameterMappedToRequest = false;
                    var param = paramList[i];

                    if (restMethod.ParameterMap.ContainsKey(i))
                    {
                        parameterInfo = restMethod.ParameterMap[i];
                        if (parameterInfo.IsObjectPropertyParameter)
                        {
                            foreach (var propertyInfo in parameterInfo.ParameterProperties)
                            {
                                var propertyObject = propertyInfo.PropertyInfo.GetValue(param);
                                urlTarget = System.Text.RegularExpressions.Regex.Replace(
                                    urlTarget,
                                    "{" + propertyInfo.Name + "}",
                                    Uri.EscapeDataString(this.settings.UrlParameterFormatter
                                        .Format(
                                            propertyObject,
                                            propertyInfo.PropertyInfo,
                                            propertyInfo.PropertyInfo.PropertyType) ?? string.Empty),
                                    System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant);
                            }
                        }
                        else
                        {
                            string pattern;
                            string replacement;
                            if (restMethod.ParameterMap[i].Type == ParameterType.RoundTripping)
                            {
                                pattern = $@"{{\*\*{restMethod.ParameterMap[i].Name}}}";
                                var paramValue = (string)param;
                                replacement = string.Join(
                                    "/",
                                    paramValue.Split('/')
                                        .Select(s =>
                                            Uri.EscapeDataString(
                                                this.settings.UrlParameterFormatter.Format(
                                                    s,
                                                    restMethod.ParameterInfoMap[i],
                                                    restMethod.ParameterInfoMap[i].ParameterType) ?? string.Empty)));
                            }
                            else
                            {
                                pattern = "{" + restMethod.ParameterMap[i].Name + "}";
                                replacement = Uri.EscapeDataString(this.settings.UrlParameterFormatter
                                        .Format(
                                            param,
                                            restMethod.ParameterInfoMap[i],
                                            restMethod.ParameterInfoMap[i].ParameterType) ?? string.Empty);
                            }

                            urlTarget = System.Text.RegularExpressions.Regex.Replace(
                                urlTarget,
                                pattern,
                                replacement,
                                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant);

                            isParameterMappedToRequest = true;
                        }
                    }

                    if (restMethod.BodyParameterInfo != null && restMethod.BodyParameterInfo.Item3 == i)
                    {
                        if (param is HttpContent httpContentParam)
                        {
                            ret.Content = httpContentParam;
                        }
                        else if (param is Stream streamParam)
                        {
                            ret.Content = new StreamContent(streamParam);
                        }
                        else if (restMethod.BodyParameterInfo.Item1 == BodySerializationMethod.Default &&
                                 param is string stringParam)
                        {
                            ret.Content = new StringContent(stringParam);
                        }
                        else
                        {
                            switch (restMethod.BodyParameterInfo.Item1)
                            {
                                case BodySerializationMethod.UrlEncoded:
                                    ret.Content = param is string str ?
                                        (HttpContent)new StringContent(
                                            Uri.EscapeDataString(str),
                                            Encoding.UTF8,
                                            "application/x-www-form-urlencoded") :
                                        new FormUrlEncodedContent(new FormValueMultimap(param, this.settings));
                                    break;
                                case BodySerializationMethod.Default:
#pragma warning disable CS0618 // Type or member is obsolete
                                case BodySerializationMethod.Json:
#pragma warning restore CS0618 // Type or member is obsolete
                                case BodySerializationMethod.Serialized:
                                    var content = this.serializer.ToHttpContent(param);
                                    switch (restMethod.BodyParameterInfo.Item2)
                                    {
                                        case false:
                                            ret.Content = new PushStreamContent(
#pragma warning disable IDE1006 // Naming Styles
                                                async (stream, _, __) =>
#pragma warning restore IDE1006 // Naming Styles
                                                {
                                                    using (stream)
                                                    {
                                                        await content.CopyToAsync(stream).ConfigureAwait(false);
                                                    }
                                                }, content.Headers.ContentType);
                                            break;
                                        case true:
                                            ret.Content = content;
                                            break;
                                    }

                                    break;
                            }
                        }

                        isParameterMappedToRequest = true;
                    }

                    if (restMethod.HeaderParameterMap.ContainsKey(i))
                    {
                        headersToAdd[restMethod.HeaderParameterMap[i]] = param?.ToString();
                        isParameterMappedToRequest = true;
                    }

                    if (restMethod.HeaderCollectionParameterMap.Contains(i))
                    {
                        var headerCollection = param as IDictionary<string, string> ?? new Dictionary<string, string>();

                        foreach (var header in headerCollection)
                        {
                            headersToAdd[header.Key] = header.Value;
                        }

                        isParameterMappedToRequest = true;
                    }

                    if (restMethod.AuthorizeParameterInfo != null && restMethod.AuthorizeParameterInfo.Item2 == i)
                    {
                        headersToAdd["Authorization"] = $"{restMethod.AuthorizeParameterInfo.Item1} {param}";
                        isParameterMappedToRequest = true;
                    }

                    if (restMethod.PropertyParameterMap.ContainsKey(i))
                    {
                        propertiesToAdd[restMethod.PropertyParameterMap[i]] = param;
                        isParameterMappedToRequest = true;
                    }

                    if (isParameterMappedToRequest || param == null)
                    {
                        continue;
                    }

                    var queryAttribute = restMethod.ParameterInfoMap[i].GetCustomAttribute<QueryAttribute>();
                    if (!restMethod.IsMultipart ||
                        (restMethod.ParameterMap.ContainsKey(i) && restMethod.ParameterMap[i].IsObjectPropertyParameter) ||
                        queryAttribute != null)
                    {
                        var attr = queryAttribute ?? new QueryAttribute();
                        if (DoNotConvertToQueryMap(param))
                        {
                            queryParamsToAdd.AddRange(this.ParseQueryParameter(param, restMethod.ParameterInfoMap[i], restMethod.QueryParameterMap[i], attr));
                        }
                        else
                        {
                            foreach (var kvp in this.BuildQueryMap(param, attr.Delimiter, parameterInfo))
                            {
                                var path = !string.IsNullOrWhiteSpace(attr.Prefix) ? $"{attr.Prefix}{attr.Delimiter}{kvp.Key}" : kvp.Key;
                                queryParamsToAdd.AddRange(this.ParseQueryParameter(kvp.Value, restMethod.ParameterInfoMap[i], path, attr));
                            }
                        }

                        continue;
                    }

                    string itemName;
                    string parameterName;

                    if (!restMethod.AttachmentNameMap.TryGetValue(i, out var attachment))
                    {
                        itemName = restMethod.QueryParameterMap[i];
                        parameterName = itemName;
                    }
                    else
                    {
                        itemName = attachment.Item1;
                        parameterName = attachment.Item2;
                    }

                    var itemValue = param;
                    var enumerable = itemValue as IEnumerable<object>;
                    var typeIsCollection = enumerable != null;

                    if (typeIsCollection)
                    {
                        foreach (var item in enumerable!)
                        {
                            this.AddMultipartItem(multiPartContent!, itemName, parameterName, item);
                        }
                    }
                    else
                    {
                        this.AddMultipartItem(multiPartContent!, itemName, parameterName, itemValue);
                    }
                }

                if (headersToAdd.Count > 0)
                {
                    // We could have content headers, so we need to make
                    // sure we have an HttpContent object to add them to,
                    // provided the HttpClient will allow it for the method
                    if (ret.Content == null && !BodylessMethods.Contains(ret.Method))
                    {
                        ret.Content = new ByteArrayContent(Array.Empty<byte>());
                    }

                    foreach (var header in headersToAdd)
                    {
                        SetHeader(ret, header.Key, header.Value);
                    }
                }

                foreach (var property in propertiesToAdd)
                {
#if NET5_0_OR_GREATER
                    ret.Options.Set(new HttpRequestOptionsKey<object?>(property.Key), property.Value);
#else
                    ret.Properties[property.Key] = property.Value;
#endif
                }

                // Always add the top-level type of the interface to the properties
#if NET5_0_OR_GREATER
                ret.Options.Set(new HttpRequestOptionsKey<Type>(HttpRequestMessageOptions.InterfaceType), this.TargetType);
#else
                ret.Properties[HttpRequestMessageOptions.InterfaceType] = this.TargetType;
#endif

#pragma warning disable S1075 // URIs should not be hardcoded
                var uri = new UriBuilder(new Uri(new Uri("http://api"), urlTarget));
#pragma warning restore S1075 // URIs should not be hardcoded
                var query = System.Web.HttpUtility.ParseQueryString(uri.Query ?? string.Empty);
                foreach (var key in query.AllKeys)
                {
                    if (!string.IsNullOrWhiteSpace(key))
                    {
                        queryParamsToAdd.Insert(0, new KeyValuePair<string, string?>(key, query[key]));
                    }
                }

                if (queryParamsToAdd.Any())
                {
                    var pairs = queryParamsToAdd
                        .Where(x => x.Key != null && x.Value != null)
                        .Select(x => Uri.EscapeDataString(x.Key) + "=" + Uri.EscapeDataString(x.Value ?? string.Empty));
                    uri.Query = string.Join("&", pairs);
                }
                else
                {
                    uri.Query = null;
                }

                var uriFormat = restMethod.MethodInfo.GetCustomAttribute<QueryUriFormatAttribute>()?.UriFormat ?? UriFormat.UriEscaped;
                ret.RequestUri = new Uri(uri.Uri.GetComponents(UriComponents.PathAndQuery, uriFormat), UriKind.Relative);
                return ret;
            };
        }

        private IEnumerable<KeyValuePair<string, string?>> ParseQueryParameter(object? param, ParameterInfo parameterInfo, string queryPath, QueryAttribute queryAttribute)
        {
            if (!(param is string) && param is System.Collections.IEnumerable paramValues)
            {
                foreach (var value in this.ParseEnumerableQueryParameterValue(paramValues, parameterInfo, parameterInfo.ParameterType, queryAttribute))
                {
                    yield return new KeyValuePair<string, string?>(queryPath, value);
                }
            }
            else
            {
                yield return new KeyValuePair<string, string?>(queryPath, this.settings.UrlParameterFormatter.Format(param, parameterInfo, parameterInfo.ParameterType));
            }
        }

        private IEnumerable<string?> ParseEnumerableQueryParameterValue(System.Collections.IEnumerable paramValues, ICustomAttributeProvider customAttributeProvider, Type type, QueryAttribute? queryAttribute)
        {
            var collectionFormat = queryAttribute != null && queryAttribute.IsCollectionFormatSpecified
                ? queryAttribute.CollectionFormat
                : this.settings.CollectionFormat;

            switch (collectionFormat)
            {
                case CollectionFormat.Multi:
                    foreach (var paramValue in paramValues)
                    {
                        yield return this.settings.UrlParameterFormatter.Format(paramValue, customAttributeProvider, type);
                    }

                    break;

                default:
                    string delimiter;

                    if (collectionFormat == CollectionFormat.Ssv)
                    {
                        delimiter = " ";
                    }
                    else if (collectionFormat == CollectionFormat.Tsv)
                    {
                        delimiter = "\t";
                    }
                    else if (collectionFormat == CollectionFormat.Pipes)
                    {
                        delimiter = "|";
                    }
                    else
                    {
                        delimiter = ",";
                    }

                    var formattedValues = paramValues
                        .Cast<object>()
                        .Select(v => this.settings.UrlParameterFormatter.Format(v, customAttributeProvider, type));

                    yield return string.Join(delimiter, formattedValues);

                    break;
            }
        }

        private Func<HttpClient, object[], IObservable<T>> BuildRxFuncForMethod<T, TBody>(RestMethodInfo restMethod)
        {
            var taskFunc = this.BuildCancellableTaskFuncForMethod<T, TBody>(restMethod);

            return (client, paramList) =>
            {
                return new TaskToObservable<T>(ct =>
                {
                    var methodCt = CancellationToken.None;
                    if (restMethod.CancellationToken != null)
                    {
                        methodCt = paramList.OfType<CancellationToken>().FirstOrDefault();
                    }

                    var cts = CancellationTokenSource.CreateLinkedTokenSource(methodCt, ct);

                    return taskFunc(client, cts.Token, paramList);
                });
            };
        }

        private Func<HttpClient, object[], Task<T>> BuildTaskFuncForMethod<T, TBody>(RestMethodInfo restMethod)
        {
            var ret = this.BuildCancellableTaskFuncForMethod<T, TBody>(restMethod);

            return (client, paramList) =>
            {
                if (restMethod.CancellationToken != null)
                {
                    return ret(client, paramList.OfType<CancellationToken>().FirstOrDefault(), paramList);
                }

                return ret(client, CancellationToken.None, paramList);
            };
        }

        private Func<HttpClient, object[], Task> BuildVoidTaskFuncForMethod(RestMethodInfo restMethod)
        {
            return async (client, paramList) =>
            {
                if (client.BaseAddress == null)
                {
                    throw new InvalidOperationException("BaseAddress must be set on the HttpClient instance");
                }

                var factory = this.BuildRequestFactoryForMethod(restMethod, client.BaseAddress.AbsolutePath, restMethod.CancellationToken != null);
                var rq = factory(paramList);

                var ct = CancellationToken.None;

                if (restMethod.CancellationToken != null)
                {
                    ct = paramList.OfType<CancellationToken>().FirstOrDefault();
                }

                using var resp = await client.SendAsync(rq, ct).ConfigureAwait(false);

                var exception = await this.settings.ExceptionFactory(resp).ConfigureAwait(false);
                if (exception != null)
                {
                    throw exception;
                }
            };
        }

        private static bool DoNotConvertToQueryMap(object? value)
        {
            if (value == null)
            {
                return false;
            }

            var type = value.GetType();

            bool shouldReturn() =>
                type == typeof(string) ||
                type == typeof(bool) ||
                type == typeof(char) ||
                typeof(IFormattable).IsAssignableFrom(type) ||
                type == typeof(Uri);

            if (shouldReturn())
            {
                return true;
            }

            if (value is System.Collections.IEnumerable enu)
            {
                var ienu = typeof(IEnumerable<>);
                var intType = type
                    .GetInterfaces()
                    .FirstOrDefault(i =>
                        i.GetTypeInfo().IsGenericType &&
                        i.GetGenericTypeDefinition() == ienu);

                if (intType != null)
                {
                    type = intType.GetGenericArguments()[0];
                }
            }

            return shouldReturn();
        }

        private static void SetHeader(HttpRequestMessage request, string name, string? value)
        {
            if (request.Headers.Any(x => x.Key == name))
            {
                request.Headers.Remove(name);
            }

            if (request.Content != null && request.Content.Headers.Any(x => x.Key == name))
            {
                request.Content.Headers.Remove(name);
            }

            if (value == null)
            {
                return;
            }

            var added = request.Headers.TryAddWithoutValidation(name, value);

            if (!added && request.Content != null)
            {
                request.Content.Headers.TryAddWithoutValidation(name, value);
            }
        }
    }
}