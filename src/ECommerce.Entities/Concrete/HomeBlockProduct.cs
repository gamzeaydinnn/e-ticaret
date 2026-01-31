using System;

namespace ECommerce.Entities.Concrete
{
    /// <summary>
    /// Ana Sayfa Blok - Ürün İlişki Tablosu (Many-to-Many)
    /// ------------------------------------------------
    /// HomeProductBlock ve Product arasındaki ilişkiyi yönetir.
    /// Sadece BlockType = "manual" olan bloklar için kullanılır.
    /// 
    /// Kullanım Senaryosu:
    /// Admin panelinde bir blok oluşturduktan sonra, o bloğa
    /// hangi ürünlerin dahil edileceğini tek tek seçer.
    /// Bu tablo seçilen ürünleri ve sıralamasını saklar.
    /// 
    /// Performans Notu:
    /// - Composite Primary Key (BlockId, ProductId) kullanılır
    /// - Index'ler BlockId ve ProductId üzerinde ayrı ayrı tanımlanmalı
    /// - DisplayOrder ile sıralama yapılır
    /// 
    /// Cascade Davranışı:
    /// - Blok silinirse: İlişkili kayıtlar da silinir (Cascade)
    /// - Ürün silinirse: İlişkili kayıtlar da silinir (Cascade)
    /// </summary>
    public class HomeBlockProduct
    {
        #region Foreign Keys (Composite Primary Key)

        /// <summary>
        /// İlişkili blok ID'si (HomeProductBlock tablosu)
        /// Composite Primary Key'in birinci parçası
        /// </summary>
        public int BlockId { get; set; }

        /// <summary>
        /// İlişkili ürün ID'si (Product tablosu)
        /// Composite Primary Key'in ikinci parçası
        /// Aynı ürün aynı bloğa sadece bir kez eklenebilir
        /// </summary>
        public int ProductId { get; set; }

        #endregion

        #region Sıralama ve Görünürlük

        /// <summary>
        /// Ürünün blok içindeki gösterim sırası
        /// Küçükten büyüğe sıralanır (0, 1, 2, ...)
        /// Admin panelinde drag & drop ile değiştirilebilir
        /// </summary>
        public int DisplayOrder { get; set; } = 0;

        /// <summary>
        /// Bu ürün bu blokta aktif mi?
        /// false ise blokta gösterilmez ama ilişki korunur
        /// Admin hızlıca ürünü gizleyip tekrar gösterebilir
        /// </summary>
        public bool IsActive { get; set; } = true;

        #endregion

        #region Audit Alanları

        /// <summary>
        /// Ürünün bloğa eklenme tarihi
        /// Raporlama ve analiz için kullanışlı
        /// </summary>
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        #endregion

        #region Navigation Properties

        /// <summary>
        /// İlişkili blok entity'si
        /// Lazy loading ile yüklenir
        /// </summary>
        public virtual HomeProductBlock Block { get; set; } = null!;

        /// <summary>
        /// İlişkili ürün entity'si
        /// Lazy loading ile yüklenir
        /// </summary>
        public virtual Product Product { get; set; } = null!;

        #endregion
    }
}
