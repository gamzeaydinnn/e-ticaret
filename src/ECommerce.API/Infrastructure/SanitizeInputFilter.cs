using System;
using System.Reflection;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ECommerce.API.Infrastructure
{
    /// <summary>
    /// Action filter that HTML-encodes all public string properties on incoming action arguments.
    /// Lightweight default protection against reflected/stored XSS from user inputs.
    /// Keep this conservative: it encodes input so controllers receive safe text.
    /// </summary>
    public class SanitizeInputFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            foreach (var kv in context.ActionArguments)
            {
                SanitizeObject(kv.Value);
            }
        }

        public void OnActionExecuted(ActionExecutedContext context) { }

        private void SanitizeObject(object? obj)
        {
            if (obj == null) return;

            // Primitive or string handled below
            var type = obj.GetType();

            // If the argument itself is a string (rare), encode it by reflection is not needed here
            if (type == typeof(string)) return;

            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!prop.CanRead || !prop.CanWrite) continue;

                try
                {
                    if (prop.PropertyType == typeof(string))
                    {
                        var val = (string?)prop.GetValue(obj);
                        if (!string.IsNullOrEmpty(val))
                        {
                            prop.SetValue(obj, HtmlEncoder.Default.Encode(val));
                        }
                    }
                    else if (!prop.PropertyType.IsValueType && !prop.PropertyType.IsPrimitive)
                    {
                        var nested = prop.GetValue(obj);
                        if (nested != null)
                        {
                            SanitizeObject(nested);
                        }
                    }
                }
                catch
                {
                    // Swallow: sanitization must not break model binding; leave original value if error
                }
            }
        }
    }
}
