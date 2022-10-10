using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Colosoft.DataServices
{
    internal class PagedResult<T> : IPagedResult<T>, IPagedResult, IResettableResult
    {
        private readonly IEnumerable<T> items;
        private readonly IPagedResultFactory pagedResultFactory;

        public PagedResult(
            IEnumerable<T> items,
            LinkHeader linkHeader,
            int totalCount,
            IPagedResultFactory pagedResultFactory)
        {
            this.items = items;
            this.TotalCount = totalCount;
            this.pagedResultFactory = pagedResultFactory;
            this.LinkHeader = linkHeader;

            var link = linkHeader.PrevLink;
            var pageIncrement = 1;

            if (string.IsNullOrEmpty(link))
            {
                link = linkHeader.NextLink;
                pageIncrement = -1;
            }

            if (!string.IsNullOrEmpty(link))
            {
                var pageMatch = System.Text.RegularExpressions.Regex.Match(
                    link,
                    $"({PagingConstants.PageName}=)([^&]+)",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                if (pageMatch.Success &&
                    int.TryParse(pageMatch.Groups[2].Value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var page))
                {
                    this.Page = page + pageIncrement;
                }
                else
                {
                    this.Page = 1;
                }
            }
            else
            {
                this.Page = 1;
            }

            link = linkHeader.FirstLink ?? linkHeader.LastLink;

            if (!string.IsNullOrEmpty(link))
            {
                var pageSizeMatch = System.Text.RegularExpressions.Regex.Match(
                    link,
                    $"({PagingConstants.PageSizeName}=)([^&]+)",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                var pageSizeText = pageSizeMatch.Groups[2].Value;

                if (pageSizeMatch.Success &&
                    int.TryParse(pageSizeText, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var pageSize))
                {
                    this.PageSize = pageSize;
                }
                else
                {
                    this.PageSize = this.TotalCount;
                }
            }
            else
            {
                this.Page = 1;
                this.PageSize = this.TotalCount;
            }
        }

        protected LinkHeader LinkHeader { get; }

        public int TotalCount { get; }

        public int Page { get; }

        public int PageSize { get; }

        public bool HasNextPage => !string.IsNullOrEmpty(this.LinkHeader.NextLink);

        public bool HasPreviousPage => !string.IsNullOrEmpty(this.LinkHeader.PrevLink);

        public bool HasLastPage => !string.IsNullOrEmpty(this.LinkHeader.LastLink);

        public bool HasFirstPage => !string.IsNullOrEmpty(this.LinkHeader.FirstLink);

        public IEnumerator<T> GetEnumerator() => this.items.GetEnumerator();

        private async Task<IPagedResult<T>?> GetResult(string link, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(link))
            {
                return null;
            }

            return await this.pagedResultFactory.Create<T>(
                new Uri(link, UriKind.RelativeOrAbsolute),
                cancellationToken);
        }

        public Task<IPagedResult<T>?> GetFirst(CancellationToken cancellationToken) =>
            this.GetResult(this.LinkHeader.FirstLink!, cancellationToken);

        public Task<IPagedResult<T>?> GetLast(CancellationToken cancellationToken) =>
            this.GetResult(this.LinkHeader.LastLink!, cancellationToken);

        public Task<IPagedResult<T>?> GetNext(CancellationToken cancellationToken) =>
            this.GetResult(this.LinkHeader.NextLink!, cancellationToken);

        public Task<IPagedResult<T>?> GetPrevious(CancellationToken cancellationToken) =>
            this.GetResult(this.LinkHeader.PrevLink!, cancellationToken);

        public async Task<IPagedResult<T>?> GetPage(int page, CancellationToken cancellationToken)
        {
            var link = this.CreatePageLink(page);

            if (string.IsNullOrEmpty(link))
            {
                return this;
            }

            return await this.GetResult(link, cancellationToken);
        }

        protected string? CreatePageLink(int page)
        {
            var link = this.LinkHeader.FirstLink ?? this.LinkHeader.LastLink;

            if (!string.IsNullOrEmpty(link))
            {
                link = System.Text.RegularExpressions.Regex.Replace(
                    link,
                    $"({PagingConstants.PageName}=)([^&]+)",
                    $"{PagingConstants.PageName}={page}",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }

            return link;
        }

        IEnumerator IEnumerable.GetEnumerator() => this.items.GetEnumerator();

        async Task<IPagedResult?> IPagedResult.GetNext(CancellationToken cancellationToken) =>
            (IPagedResult?)(await this.GetNext(cancellationToken));

        async Task<IPagedResult?> IPagedResult.GetPrevious(CancellationToken cancellationToken) =>
            (IPagedResult?)(await this.GetPrevious(cancellationToken));

        async Task<IPagedResult?> IPagedResult.GetFirst(CancellationToken cancellationToken) =>
            (IPagedResult?)(await this.GetFirst(cancellationToken));

        async Task<IPagedResult?> IPagedResult.GetLast(CancellationToken cancellationToken) =>
            (IPagedResult?)(await this.GetLast(cancellationToken));

        async Task<IPagedResult?> IPagedResult.GetPage(int page, CancellationToken cancellationToken) =>
            (IPagedResult?)(await this.GetPage(page, cancellationToken));

        public virtual async Task<IResettableResult?> Reset(CancellationToken cancellationToken)
        {
            return (IResettableResult?)(await this.GetPage(1, cancellationToken));
        }
    }
}
