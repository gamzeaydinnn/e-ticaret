using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure.Config
{
    public class PosnetPaymentSettingsValidator : IValidateOptions<PaymentSettings>
    {
        private readonly IConfiguration _configuration;

        public PosnetPaymentSettingsValidator(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public ValidateOptionsResult Validate(string? name, PaymentSettings options)
        {
            var posnetEnabled = _configuration.GetValue("Payment:Posnet:Enabled", false);
            if (!posnetEnabled)
            {
                return ValidateOptionsResult.Success;
            }

            var failures = new List<string>();

            if (IsUnset(options.PosnetMerchantId) || options.PosnetMerchantId.Length != 10 || !options.PosnetMerchantId.All(char.IsDigit))
            {
                failures.Add("PaymentSettings:PosnetMerchantId 10 haneli numerik bir değer olmalıdır.");
            }

            if (IsUnset(options.PosnetTerminalId) || options.PosnetTerminalId.Length != 8 || !options.PosnetTerminalId.All(char.IsLetterOrDigit))
            {
                failures.Add("PaymentSettings:PosnetTerminalId 8 karakterli alfanumerik bir değer olmalıdır.");
            }

            if (IsUnset(options.PosnetId))
            {
                failures.Add("PaymentSettings:PosnetId zorunludur ve placeholder olamaz.");
            }

            if (IsUnset(options.PosnetEncKey))
            {
                failures.Add("PaymentSettings:PosnetEncKey zorunludur ve placeholder olamaz.");
            }

            if (string.IsNullOrWhiteSpace(options.PosnetCallbackUrl))
            {
                failures.Add("PaymentSettings:PosnetCallbackUrl zorunludur.");
            }

            return failures.Count == 0
                ? ValidateOptionsResult.Success
                : ValidateOptionsResult.Fail(failures);
        }

        private static bool IsUnset(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            return value.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase);
        }
    }
}