using System;
using System.Collections.Generic;
using System.Linq;
namespace ECommerce.Core.Helpers
{
    public static class PaginationHelper
    {
        public static IEnumerable<T> Paginate<T>(IEnumerable<T> source, int page, int pageSize)
        {
            return source.Skip((page - 1) * pageSize).Take(pageSize);
        }
    }
}
