using System.Collections.Generic;

namespace ECommerce.Core.DTOs
{
    public class PagedResult<T>
    {
        public IReadOnlyList<T> Items { get; }
        public int Total { get; }
        public int Skip { get; }
        public int Take { get; }

        public PagedResult(IReadOnlyList<T> items, int total, int skip, int take)
        {
            Items = items;
            Total = total;
            Skip = skip;
            Take = take;
        }
    }
}
