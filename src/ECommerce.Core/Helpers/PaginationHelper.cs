using System;
using System.Collections.Generic;
using System.Linq;
namespace ECommerce.Core.Helpers
{
    public static class PaginationHelper
    {
        public static IEnumerable<T> Paginate<T>(IEnumerable<T> source, int page, int pageSize)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (page <= 0) throw new ArgumentOutOfRangeException(nameof(page), "Page must be >= 1");
            if (pageSize <= 0) throw new ArgumentOutOfRangeException(nameof(pageSize), "PageSize must be >= 1");

            // Safe calculation
            var skip = checked((page - 1) * pageSize);
            return source.Skip(skip).Take(pageSize);
        }
    }
}
