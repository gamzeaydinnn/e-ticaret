// ==========================================================================
// PosnetXmlBuilderRefundTests - Faz 3 tranDateRequired banka uyumluluk testleri
// ==========================================================================

using ECommerce.Infrastructure.Services.Payment.Posnet;
using ECommerce.Infrastructure.Services.Payment.Posnet.Models;

namespace ECommerce.Tests.Payment.Posnet
{
    public class PosnetXmlBuilderRefundTests
    {
        private readonly PosnetXmlBuilder _builder = new();

        private const string MerchantId = "1234567890";
        private const string TerminalId = "12345678";

        [Fact]
        public void BuildReverseXml_IncludesTranDateRequired()
        {
            var xml = _builder.BuildReverseXml(new PosnetReverseRequest
            {
                MerchantId = MerchantId,
                TerminalId = TerminalId,
                OrderId = "ORD1001",
                HostLogKey = "HLK123456",
                Transaction = "sale"
            });

            Assert.Contains("<tranDateRequired>1</tranDateRequired>", xml);
            Assert.Contains("<reverse>", xml);
        }

        [Fact]
        public void BuildReturnXml_IncludesTranDateRequired()
        {
            var xml = _builder.BuildReturnXml(new PosnetReturnRequest
            {
                MerchantId = MerchantId,
                TerminalId = TerminalId,
                OrderId = "ORD1001",
                HostLogKey = "HLK123456",
                Amount = 1250,
                CurrencyCode = "TL"
            });

            Assert.Contains("<tranDateRequired>1</tranDateRequired>", xml);
            Assert.Contains("<return>", xml);
            Assert.Contains("<amount>1250</amount>", xml);
        }
    }
}
