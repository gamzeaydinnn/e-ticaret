using System;
using System.Collections.Generic;

namespace ECommerce.Core.Interfaces
{
    public interface ILogService
    {
        void Info(string message, IDictionary<string, object>? context = null);
        void Warn(string message, IDictionary<string, object>? context = null);
        void Error(Exception exception, string message, IDictionary<string, object>? context = null);

        // Veri değişiklikleri için audit kaydı
        void Audit(string action, string entityName, int? entityId, object? oldValues, object? newValues, string? performedBy = null);
    }
}

