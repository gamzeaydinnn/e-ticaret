namespace ECommerce.Core.Constants
{
    /// <summary>
    /// Sistemdeki tüm kullanıcı rollerini tanımlar.
    /// RBAC (Role-Based Access Control) sisteminin temel yapı taşlarıdır.
    /// 
    /// Rol Hiyerarşisi ve Sorumlulukları:
    /// ──────────────────────────────────
    /// 1. SuperAdmin: Sistemin tam yetkili sahibi. Tüm ayarları değiştirebilir,
    ///    kullanıcı rollerini yönetebilir, kritik operasyonları gerçekleştirebilir.
    ///    
    /// 2. StoreManager (Mağaza Yöneticisi): Günlük iş operasyonlarını yönetir.
    ///    Ürün/kategori yönetimi, kampanya oluşturma, satış raporları görüntüleme.
    ///    
    /// 3. CustomerSupport (Müşteri Hizmetleri): Müşteri memnuniyeti odaklı.
    ///    Sipariş durumu güncelleme, iade işlemleri, müşteri yorumları yönetimi.
    ///    
    /// 4. Logistics (Lojistik): Depo ve kargo operasyonları.
    ///    Gönderilecek siparişleri görme, kargo takip numarası girme.
    ///    Hassas müşteri bilgilerine erişim YOK.
    ///    
    /// 5. Admin: Eski uyumluluk için korunmuş rol (deprecated, SuperAdmin kullanın).
    /// 
    /// 6. User/Customer: Sistemin son kullanıcısı, alışveriş yapan müşteri.
    /// 
    /// 7. StoreAttendant (Market Görevlisi): Siparişleri hazırlayan personel.
    ///    Sipariş hazırlama, tartı girişi, "Hazır" durumuna geçirme yetkisi.
    ///    Kurye atama veya sipariş onaylama yetkisi YOK.
    ///    
    /// 8. Dispatcher (Sevkiyat Görevlisi): Kurye atama ve sevkiyat yönetimi.
    ///    Hazır siparişleri görme, kurye atama, kurye performans takibi.
    ///    Sipariş hazırlama veya ürün yönetimi yetkisi YOK.
    /// 
    /// Güvenlik Prensibi: "En Az Yetki" (Least Privilege)
    /// Her role sadece işini yapması için gereken minimum yetki verilir.
    /// </summary>
    public static class Roles
    {
        #region Ana Roller

        /// <summary>
        /// Sistem yöneticisi - Tam yetki.
        /// Sadece şirket sahipleri veya teknik liderlerde bulunmalı.
        /// </summary>
        public const string SuperAdmin = "SuperAdmin";

        /// <summary>
        /// [DEPRECATED] Eski admin rolü - Geriye dönük uyumluluk için korunuyor.
        /// Yeni kullanıcılar için SuperAdmin veya StoreManager tercih edilmeli.
        /// </summary>
        public const string Admin = "Admin";

        /// <summary>
        /// Mağaza/Operasyon Yöneticisi.
        /// Ürün, kategori, kampanya ve stok yönetimi yetkilerine sahip.
        /// Sistem ayarlarına ve kullanıcı yönetimine erişim YOK.
        /// </summary>
        public const string StoreManager = "StoreManager";

        /// <summary>
        /// Müşteri Hizmetleri/Destek personeli.
        /// Sipariş durumu güncelleme, iade/iptal işlemleri, müşteri iletişimi.
        /// Ürün fiyatlarını değiştirme veya finansal raporlara erişim YOK.
        /// </summary>
        public const string CustomerSupport = "CustomerSupport";

        /// <summary>
        /// Lojistik/Depo personeli.
        /// Sadece kargoya verilecek siparişleri görür ve takip numarası girer.
        /// Müşteri e-posta, ödeme bilgileri gibi hassas verilere erişim YOK.
        /// </summary>
        public const string Logistics = "Logistics";

        /// <summary>
        /// Normal kullanıcı/müşteri.
        /// Alışveriş yapma, profil düzenleme, sipariş geçmişi görüntüleme.
        /// </summary>
        public const string User = "User";

        /// <summary>
        /// Customer rolü - User ile aynı, semantik netlik için.
        /// </summary>
        public const string Customer = "Customer";

        /// <summary>
        /// Kurye rolü.
        /// Teslimat operasyonları için kullanılır.
        /// </summary>
        public const string Courier = "Courier";

        /// <summary>
        /// Market Görevlisi (Store Attendant) rolü.
        /// Siparişleri fiziksel olarak hazırlayan, tartan ve "Hazır" durumuna getiren personel.
        /// Sorumlulukları:
        /// - Onaylanmış siparişleri görme
        /// - "Hazırlanıyor" durumuna geçirme
        /// - Ağırlık bazlı ürünleri tartma
        /// - "Hazır" durumuna geçirme
        /// - Sipariş durumunu güncelleme (Admin ile aynı yetkiler)
        /// - Kurye atama yetkisi (Admin ile aynı yetkiler)
        /// - Sipariş iptal ve iade işlemleri
        /// Kısıtlamalar:
        /// - Ürün/fiyat değiştirme yetkisi YOK
        /// - Kullanıcı yönetimi yetkisi YOK
        /// </summary>
        public const string StoreAttendant = "StoreAttendant";

        /// <summary>
        /// Sevkiyat/Kargo Görevlisi (Dispatcher) rolü.
        /// Hazır siparişlere kurye atayan ve sevkiyat sürecini yöneten personel.
        /// Sorumlulukları:
        /// - Hazır siparişleri görme
        /// - Müsait kuryeleri listeleme
        /// - Kurye atama/değiştirme
        /// - Sevkiyat performansını izleme
        /// Kısıtlamalar:
        /// - Sipariş hazırlama yetkisi YOK
        /// - Sipariş onaylama yetkisi YOK
        /// - Ürün/fiyat değiştirme yetkisi YOK
        /// </summary>
        public const string Dispatcher = "Dispatcher";

        #endregion

        #region Rol Grupları - Yetkilendirme Kolaylığı İçin

        /// <summary>
        /// Yönetici seviyesi roller - Admin paneline erişim hakkı.
        /// SuperAdmin, Admin ve StoreManager bu gruba dahil.
        /// </summary>
        public const string AdminLike = SuperAdmin + "," + Admin + "," + StoreManager;

        /// <summary>
        /// Tüm personel rolleri - Şirket çalışanları.
        /// Müşteri dışındaki tüm roller.
        /// </summary>
        public const string AllStaff = SuperAdmin + "," + Admin + "," + StoreManager + "," + CustomerSupport + "," + Logistics + "," + StoreAttendant + "," + Dispatcher;

        /// <summary>
        /// Sipariş yönetimi yapabilecek roller.
        /// Sipariş durumunu güncelleyebilir veya görüntüleyebilir.
        /// StoreAttendant ve Dispatcher da sipariş görebilir.
        /// </summary>
        public const string OrderManagement = SuperAdmin + "," + Admin + "," + StoreManager + "," + CustomerSupport + "," + Logistics + "," + StoreAttendant + "," + Dispatcher;

        /// <summary>
        /// Ürün yönetimi yapabilecek roller.
        /// Ürün ekleme, düzenleme, silme yetkisi.
        /// </summary>
        public const string ProductManagement = SuperAdmin + "," + Admin + "," + StoreManager;

        /// <summary>
        /// Kullanıcı yönetimi yapabilecek roller.
        /// Sadece en üst düzey yetkililer.
        /// </summary>
        public const string UserManagement = SuperAdmin + "," + Admin;

        /// <summary>
        /// Sipariş hazırlama yetkisine sahip roller (OrderPrepare).
        /// Market görevlisi ve üst düzey yetkililer.
        /// Bu roller siparişi "Hazırlanıyor" ve "Hazır" durumuna geçirebilir.
        /// </summary>
        public const string OrderPrepare = SuperAdmin + "," + Admin + "," + StoreManager + "," + StoreAttendant;

        /// <summary>
        /// Kurye atama yetkisine sahip roller (OrderDispatch).
        /// Sevkiyat görevlisi ve üst düzey yetkililer.
        /// Bu roller hazır siparişlere kurye atayabilir.
        /// </summary>
        public const string OrderDispatch = SuperAdmin + "," + Admin + "," + StoreManager + "," + Dispatcher;

        /// <summary>
        /// Market operasyonları için yetkili roller.
        /// Store Attendant paneline erişim hakkı.
        /// </summary>
        public const string StoreOperations = SuperAdmin + "," + Admin + "," + StoreManager + "," + StoreAttendant;

        /// <summary>
        /// Sevkiyat operasyonları için yetkili roller.
        /// Dispatcher paneline erişim hakkı.
        /// </summary>
        public const string DispatchOperations = SuperAdmin + "," + Admin + "," + StoreManager + "," + Dispatcher;

        #endregion

        #region Yardımcı Metodlar

        /// <summary>
        /// Sistemdeki tüm rollerin listesini döndürür.
        /// Seeder ve validation işlemlerinde kullanılır.
        /// </summary>
        public static string[] GetAllRoles() => new[]
        {
            SuperAdmin,
            Admin,
            StoreManager,
            CustomerSupport,
            Logistics,
            User,
            Customer,
            Courier,
            StoreAttendant,
            Dispatcher
        };

        /// <summary>
        /// Yönetici paneline erişebilecek rolleri döndürür.
        /// Bu roller admin panel ana sayfasına erişebilir.
        /// </summary>
        public static string[] GetAdminPanelRoles() => new[]
        {
            SuperAdmin,
            Admin,
            StoreManager,
            CustomerSupport,
            Logistics,
            StoreAttendant,
            Dispatcher
        };

        /// <summary>
        /// Sipariş hazırlama yetkisine sahip rolleri döndürür.
        /// Store Attendant paneline erişim için kullanılır.
        /// </summary>
        public static string[] GetOrderPrepareRoles() => new[]
        {
            SuperAdmin,
            Admin,
            StoreManager,
            StoreAttendant
        };

        /// <summary>
        /// Kurye atama yetkisine sahip rolleri döndürür.
        /// Dispatcher paneline erişim için kullanılır.
        /// </summary>
        public static string[] GetOrderDispatchRoles() => new[]
        {
            SuperAdmin,
            Admin,
            StoreManager,
            Dispatcher
        };

        /// <summary>
        /// Verilen rolün admin paneline erişim hakkı olup olmadığını kontrol eder.
        /// </summary>
        /// <param name="roleName">Kontrol edilecek rol adı</param>
        /// <returns>Admin paneline erişim hakkı varsa true</returns>
        public static bool CanAccessAdminPanel(string? roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                return false;

            var adminRoles = GetAdminPanelRoles();
            return adminRoles.Any(r => r.Equals(roleName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Verilen rolün süper yönetici olup olmadığını kontrol eder.
        /// </summary>
        public static bool IsSuperAdmin(string? roleName)
        {
            return !string.IsNullOrWhiteSpace(roleName) &&
                   roleName.Equals(SuperAdmin, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Verilen rolün sipariş hazırlama yetkisi olup olmadığını kontrol eder.
        /// </summary>
        /// <param name="roleName">Kontrol edilecek rol adı</param>
        /// <returns>Sipariş hazırlama yetkisi varsa true</returns>
        public static bool CanPrepareOrders(string? roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                return false;

            var prepareRoles = GetOrderPrepareRoles();
            return prepareRoles.Any(r => r.Equals(roleName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Verilen rolün kurye atama yetkisi olup olmadığını kontrol eder.
        /// </summary>
        /// <param name="roleName">Kontrol edilecek rol adı</param>
        /// <returns>Kurye atama yetkisi varsa true</returns>
        public static bool CanDispatchOrders(string? roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                return false;

            var dispatchRoles = GetOrderDispatchRoles();
            return dispatchRoles.Any(r => r.Equals(roleName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Verilen rolün Market Görevlisi olup olmadığını kontrol eder.
        /// </summary>
        public static bool IsStoreAttendant(string? roleName)
        {
            return !string.IsNullOrWhiteSpace(roleName) &&
                   roleName.Equals(StoreAttendant, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Verilen rolün Sevkiyat Görevlisi olup olmadığını kontrol eder.
        /// </summary>
        public static bool IsDispatcher(string? roleName)
        {
            return !string.IsNullOrWhiteSpace(roleName) &&
                   roleName.Equals(Dispatcher, StringComparison.OrdinalIgnoreCase);
        }

        #endregion
    }
}
