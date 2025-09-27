namespace ECommerce.Core.Interfaces
{
    public interface IMicroService
    {
        void UpdateProduct(ECommerce.Core.DTOs.Micro.MicroProductDto productDto);
        void UpdateStock(ECommerce.Core.DTOs.Micro.MicroStockDto stockDto);
        // İhtiyaca göre diğer metodlar eklenebilir
    }
}
/*Mikro ERP ile entegrasyon
	• Mikro’nün resmi API dokümanı var; ürün, stok, fiyat ve satış verilerini senkronize etmek için endpoint’leri kullan. Yapılacaklar:
		1. Ürün/fiyat senkronizasyonu: e-ticaret ➜ Mikro (ör. günlük toplu push veya gerçek zamanlı webhook).
		2. Satış verilerini Mikro’ya gönder (fatura/irsaliye).
		3. Mikro’daki stok değişikliklerini (alış, iade, sayım) çek ve e-ticaret DB’sini güncelle.
		4. Tüm bunlar için Hangfire ile günlük/daimi cron job kur: RecurringJob.AddOrUpdate("mikro-sync", ()=> SyncWithMikro(), Cron.Daily); (apidocs.mikro.com.tr)


 Mikro entegrasyonu için pratik öneriler
	• Alan eşlemesi (mapping): e-ticaret ürün alanları ↔ Mikro product alanları (SKU, barkod, fiyat, KDV, birim). Bu mapping’i JSON veya XML transform katmanında tut.
	• İki yönlü senkronizasyon: Conflict resolution politikası belirle (hangi sistem otorite — genelde Mikro ERP “kaynak” kabul edilir ya da e-ticaret “source of truth” seçilebilir).
	• Reliability: Her push için response logu + retry mekanizması (Hangfire recurring job + exponential backoff). (apidocs.mikro.com.tr)




*/
