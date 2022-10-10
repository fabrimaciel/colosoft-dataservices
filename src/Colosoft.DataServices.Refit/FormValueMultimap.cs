using Refit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Colosoft.DataServices.Refit
{
    internal class FormValueMultimap : IEnumerable<KeyValuePair<string?, string?>>
    {
        private static readonly Dictionary<Type, PropertyInfo[]> PropertyCache = new Dictionary<Type, PropertyInfo[]>();
        private readonly IList<KeyValuePair<string?, string?>> formEntries = new List<KeyValuePair<string?, string?>>();
        private readonly global::Refit.IHttpContentSerializer contentSerializer;

        public FormValueMultimap(object source, RefitSettings settings)
        {
            if (settings is null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            this.contentSerializer = settings.ContentSerializer;

            if (source == null)
            {
                return;
            }

            if (source is IDictionary dictionary)
            {
                foreach (var key in dictionary.Keys)
                {
                    var value = dictionary[key];
                    if (value != null)
                    {
                        this.Add(key.ToString(), settings.FormUrlEncodedParameterFormatter.Format(value, null));
                    }
                }

                return;
            }

            var type = source.GetType();

            lock (PropertyCache)
            {
                if (!PropertyCache.ContainsKey(type))
                {
                    PropertyCache[type] = GetProperties(type);
                }

                foreach (var property in PropertyCache[type])
                {
                    var value = property.GetValue(source, null);
                    if (value != null)
                    {
                        var fieldName = this.GetFieldNameForProperty(property);

                        var attrib = property.GetCustomAttribute<QueryAttribute>(true);

                        if (value is IEnumerable enumerable)
                        {
                            var collectionFormat = attrib != null && attrib.IsCollectionFormatSpecified
                                ? attrib.CollectionFormat
                                : settings.CollectionFormat;

                            switch (collectionFormat)
                            {
                                case CollectionFormat.Multi:
                                    foreach (var item in enumerable)
                                    {
                                        this.Add(fieldName, settings.FormUrlEncodedParameterFormatter.Format(item, attrib?.Format));
                                    }

                                    break;

                                case CollectionFormat.Csv:
                                case CollectionFormat.Ssv:
                                case CollectionFormat.Tsv:
                                case CollectionFormat.Pipes:
                                    var delimiter = collectionFormat switch
                                    {
                                        CollectionFormat.Csv => ",",
                                        CollectionFormat.Ssv => " ",
                                        CollectionFormat.Tsv => "\t",
                                        _ => "|"
                                    };

                                    var formattedValues = enumerable
                                        .Cast<object>()
                                        .Select(v => settings.FormUrlEncodedParameterFormatter.Format(v, attrib?.Format));
                                    this.Add(fieldName, string.Join(delimiter, formattedValues));
                                    break;
                                default:
                                    this.Add(fieldName, settings.FormUrlEncodedParameterFormatter.Format(value, attrib?.Format));
                                    break;
                            }
                        }
                        else
                        {
                            this.Add(fieldName, settings.FormUrlEncodedParameterFormatter.Format(value, attrib?.Format));
                        }
                    }
                }
            }
        }

        private static PropertyInfo[] GetProperties(Type type)
        {
            return type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                       .Where(p => p.CanRead && p.GetMethod?.IsPublic == true)
                       .ToArray();
        }

        public IEnumerable<string?> Keys => this.Select(it => it.Key);

        private void Add(string? key, string? value)
        {
            this.formEntries.Add(new KeyValuePair<string?, string?>(key, value));
        }

        private string GetFieldNameForProperty(PropertyInfo propertyInfo)
        {
            var name = propertyInfo.GetCustomAttributes<AliasAsAttribute>(true)
                               .Select(a => a.Name)
                               .FirstOrDefault()
                   ?? this.contentSerializer.GetFieldNameForProperty(propertyInfo)
                   ?? propertyInfo.Name;

            var qattrib = propertyInfo.GetCustomAttributes<QueryAttribute>(true)
                           .Select(attr => !string.IsNullOrWhiteSpace(attr.Prefix) ? $"{attr.Prefix}{attr.Delimiter}{name}" : name)
                           .FirstOrDefault();

            return qattrib ?? name;
        }

        public IEnumerator<KeyValuePair<string?, string?>> GetEnumerator()
        {
            return this.formEntries.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}