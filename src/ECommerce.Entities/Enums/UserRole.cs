using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ECommerce.Entities.Enums
{
    /// <summary>
    /// Sistemdeki kullanıcı rollerini tanımlayan enum.
    /// RBAC (Role-Based Access Control) sisteminin temel yapı taşıdır.
    /// 
    /// NOT: Bu enum Roles.cs sabitlerini enum olarak temsil eder.
    /// Yeni rol eklerken her iki dosyayı da güncellemeyi unutmayın.
    /// </summary>
    public enum UserRole
    {
        /// <summary>
        /// Sistem yöneticisi - Tam yetki
        /// </summary>
        Admin,
        
        /// <summary>
        /// Normal kullanıcı/müşteri
        /// </summary>
        User,
        
        /// <summary>
        /// Mağaza/Market rolü (eski uyumluluk için)
        /// </summary>
        Market,
        
        /// <summary>
        /// Süper yönetici - En üst düzey yetki
        /// </summary>
        SuperAdmin,
        
        /// <summary>
        /// Mağaza Yöneticisi
        /// </summary>
        StoreManager,
        
        /// <summary>
        /// Müşteri Hizmetleri
        /// </summary>
        CustomerSupport,
        
        /// <summary>
        /// Lojistik personeli
        /// </summary>
        Logistics,
        
        /// <summary>
        /// Kurye - Teslimat operasyonları
        /// </summary>
        Courier,
        
        /// <summary>
        /// Market Görevlisi - Sipariş hazırlama
        /// Siparişleri fiziksel olarak hazırlayan ve tartan personel.
        /// </summary>
        StoreAttendant,
        
        /// <summary>
        /// Sevkiyat Görevlisi - Kurye atama
        /// Hazır siparişlere kurye atayan personel.
        /// </summary>
        Dispatcher
    }
}
