namespace ECommerce.Core.Constants
{
    /// <summary>
    /// Sistemdeki tüm izinleri (permissions) merkezi olarak tanımlar.
    /// RBAC sisteminde granüler yetkilendirme için kullanılır.
    /// 
    /// İzin Adlandırma Kuralı:
    /// ────────────────────────
    /// Format: "modül.aksiyon"
    /// Örnek: "products.create", "orders.view", "users.delete"
    /// 
    /// Standart Aksiyonlar:
    /// - view: Görüntüleme (liste ve detay)
    /// - create: Oluşturma
    /// - update: Güncelleme
    /// - delete: Silme
    /// - manage: Tüm CRUD işlemleri (üst düzey yetki)
    /// - export: Dışa aktarma
    /// 
    /// Kullanım:
    /// [HasPermission(Permissions.Products.Create)]
    /// public async Task<IActionResult> CreateProduct(...)
    /// </summary>
    public static class Permissions
    {
        #region Dashboard İzinleri

        /// <summary>
        /// Dashboard modülü izinleri.
        /// Ana sayfa istatistikleri ve özet bilgiler.
        /// </summary>
        public static class Dashboard
        {
            public const string View = "dashboard.view";
            public const string ViewStatistics = "dashboard.statistics";
            public const string ViewRevenueChart = "dashboard.revenue";
            
            /// <summary>Dashboard modülündeki tüm izinler</summary>
            public static string[] All => new[] { View, ViewStatistics, ViewRevenueChart };
        }

        #endregion

        #region Ürün İzinleri

        /// <summary>
        /// Ürün yönetimi izinleri.
        /// Ürün ekleme, düzenleme, silme ve stok işlemleri.
        /// </summary>
        public static class Products
        {
            public const string View = "products.view";
            public const string Create = "products.create";
            public const string Update = "products.update";
            public const string Delete = "products.delete";
            public const string ManageStock = "products.stock";
            public const string ManagePricing = "products.pricing";
            public const string Import = "products.import";
            public const string Export = "products.export";
            
            /// <summary>Ürün modülündeki tüm izinler</summary>
            public static string[] All => new[] { View, Create, Update, Delete, ManageStock, ManagePricing, Import, Export };
        }

        #endregion

        #region Kategori İzinleri

        /// <summary>
        /// Kategori yönetimi izinleri.
        /// Kategori ağacı düzenleme ve sıralama.
        /// </summary>
        public static class Categories
        {
            public const string View = "categories.view";
            public const string Create = "categories.create";
            public const string Update = "categories.update";
            public const string Delete = "categories.delete";
            
            /// <summary>Kategori modülündeki tüm izinler</summary>
            public static string[] All => new[] { View, Create, Update, Delete };
        }

        #endregion

        #region Sipariş İzinleri

        /// <summary>
        /// Sipariş yönetimi izinleri.
        /// Sipariş durumu güncelleme, iptal, iade işlemleri.
        /// </summary>
        public static class Orders
        {
            public const string View = "orders.view";
            public const string ViewDetails = "orders.details";
            public const string UpdateStatus = "orders.status";
            public const string Cancel = "orders.cancel";
            public const string ProcessRefund = "orders.refund";
            public const string AssignCourier = "orders.assign_courier";
            public const string ViewCustomerInfo = "orders.customer_info";
            public const string Export = "orders.export";
            
            /// <summary>Sipariş modülündeki tüm izinler</summary>
            public static string[] All => new[] { View, ViewDetails, UpdateStatus, Cancel, ProcessRefund, AssignCourier, ViewCustomerInfo, Export };
        }

        #endregion

        #region Kullanıcı İzinleri

        /// <summary>
        /// Kullanıcı yönetimi izinleri.
        /// Kullanıcı oluşturma, düzenleme, rol atama.
        /// </summary>
        public static class Users
        {
            public const string View = "users.view";
            public const string Create = "users.create";
            public const string Update = "users.update";
            public const string Delete = "users.delete";
            public const string ManageRoles = "users.roles";
            public const string ViewSensitiveData = "users.sensitive";
            public const string Export = "users.export";
            
            /// <summary>Kullanıcı modülündeki tüm izinler</summary>
            public static string[] All => new[] { View, Create, Update, Delete, ManageRoles, ViewSensitiveData, Export };
        }

        #endregion

        #region Rol ve İzin Yönetimi

        /// <summary>
        /// Rol yönetimi izinleri.
        /// Sadece SuperAdmin için tasarlandı.
        /// </summary>
        public static class Roles
        {
            public const string View = "roles.view";
            public const string Create = "roles.create";
            public const string Update = "roles.update";
            public const string Delete = "roles.delete";
            public const string ManagePermissions = "roles.permissions";
            
            /// <summary>Rol modülündeki tüm izinler</summary>
            public static string[] All => new[] { View, Create, Update, Delete, ManagePermissions };
        }

        #endregion

        #region Kampanya ve Kupon İzinleri

        /// <summary>
        /// Kampanya ve kupon yönetimi izinleri.
        /// </summary>
        public static class Campaigns
        {
            public const string View = "campaigns.view";
            public const string Create = "campaigns.create";
            public const string Update = "campaigns.update";
            public const string Delete = "campaigns.delete";
            
            /// <summary>Kampanya modülündeki tüm izinler</summary>
            public static string[] All => new[] { View, Create, Update, Delete };
        }

        /// <summary>
        /// Kupon yönetimi izinleri.
        /// </summary>
        public static class Coupons
        {
            public const string View = "coupons.view";
            public const string Create = "coupons.create";
            public const string Update = "coupons.update";
            public const string Delete = "coupons.delete";
            
            /// <summary>Kupon modülündeki tüm izinler</summary>
            public static string[] All => new[] { View, Create, Update, Delete };
        }

        #endregion

        #region Kurye/Lojistik İzinleri

        /// <summary>
        /// Kurye ve lojistik yönetimi izinleri.
        /// </summary>
        public static class Couriers
        {
            public const string View = "couriers.view";
            public const string Create = "couriers.create";
            public const string Update = "couriers.update";
            public const string Delete = "couriers.delete";
            public const string AssignOrders = "couriers.assign";
            public const string ViewPerformance = "couriers.performance";
            
            /// <summary>Kurye modülündeki tüm izinler</summary>
            public static string[] All => new[] { View, Create, Update, Delete, AssignOrders, ViewPerformance };
        }

        /// <summary>
        /// Kargo ve teslimat izinleri.
        /// Lojistik personeli için özel izinler.
        /// </summary>
        public static class Shipping
        {
            public const string ViewPendingShipments = "shipping.pending";
            public const string UpdateTrackingNumber = "shipping.tracking";
            public const string MarkAsShipped = "shipping.ship";
            public const string MarkAsDelivered = "shipping.deliver";
            
            /// <summary>Kargo modülündeki tüm izinler</summary>
            public static string[] All => new[] { ViewPendingShipments, UpdateTrackingNumber, MarkAsShipped, MarkAsDelivered };
        }

        #endregion

        #region Rapor İzinleri

        /// <summary>
        /// Raporlama izinleri.
        /// Satış, stok, performans raporları.
        /// </summary>
        public static class Reports
        {
            /// <summary>Genel rapor görüntüleme izni</summary>
            public const string View = "reports.view";
            public const string ViewSales = "reports.sales";
            public const string ViewInventory = "reports.inventory";
            public const string ViewCustomers = "reports.customers";
            public const string ViewFinancial = "reports.financial";
            public const string ViewWeight = "reports.weight";
            public const string Export = "reports.export";
            
            /// <summary>Rapor modülündeki tüm izinler</summary>
            public static string[] All => new[] { View, ViewSales, ViewInventory, ViewCustomers, ViewFinancial, ViewWeight, Export };
        }

        #endregion

        #region Banner/Poster İzinleri

        /// <summary>
        /// Banner ve poster yönetimi izinleri.
        /// Ana sayfa görsel içerik yönetimi.
        /// </summary>
        public static class Banners
        {
            public const string View = "banners.view";
            public const string Create = "banners.create";
            public const string Update = "banners.update";
            public const string Delete = "banners.delete";
            
            /// <summary>Banner modülündeki tüm izinler</summary>
            public static string[] All => new[] { View, Create, Update, Delete };
        }

        #endregion

        #region Marka İzinleri

        /// <summary>
        /// Marka yönetimi izinleri.
        /// </summary>
        public static class Brands
        {
            public const string View = "brands.view";
            public const string Create = "brands.create";
            public const string Update = "brands.update";
            public const string Delete = "brands.delete";
            
            /// <summary>Marka modülündeki tüm izinler</summary>
            public static string[] All => new[] { View, Create, Update, Delete };
        }

        #endregion

        #region Sistem Ayarları İzinleri

        /// <summary>
        /// Sistem ayarları izinleri.
        /// Sadece SuperAdmin için kritik yapılandırmalar.
        /// </summary>
        public static class Settings
        {
            public const string View = "settings.view";
            public const string Update = "settings.update";
            /// <summary>Sistem seviyesi ayarlar izni</summary>
            public const string System = "settings.system";
            public const string ManagePayment = "settings.payment";
            public const string ManageSms = "settings.sms";
            public const string ManageEmail = "settings.email";
            
            /// <summary>Ayar modülündeki tüm izinler</summary>
            public static string[] All => new[] { View, Update, System, ManagePayment, ManageSms, ManageEmail };
        }

        #endregion

        #region Log İzinleri

        /// <summary>
        /// Sistem logları görüntüleme izinleri.
        /// Audit ve hata logları.
        /// </summary>
        public static class Logs
        {
            /// <summary>Genel log görüntüleme izni</summary>
            public const string View = "logs.view";
            public const string ViewAudit = "logs.audit";
            public const string ViewError = "logs.error";
            public const string ViewSync = "logs.sync";
            public const string Export = "logs.export";
            
            /// <summary>Log modülündeki tüm izinler</summary>
            public static string[] All => new[] { View, ViewAudit, ViewError, ViewSync, Export };
        }

        #endregion

        #region Yardımcı Metodlar

        /// <summary>
        /// Tüm sistemdeki izinleri döndürür.
        /// Seeder ve validation için kullanılır.
        /// </summary>
        public static IEnumerable<(string Name, string Module)> GetAllPermissions()
        {
            // Dashboard
            foreach (var p in Dashboard.All)
                yield return (p, "Dashboard");

            // Products
            foreach (var p in Products.All)
                yield return (p, "Products");

            // Categories
            foreach (var p in Categories.All)
                yield return (p, "Categories");

            // Orders
            foreach (var p in Orders.All)
                yield return (p, "Orders");

            // Users
            foreach (var p in Users.All)
                yield return (p, "Users");

            // Roles
            foreach (var p in Roles.All)
                yield return (p, "Roles");

            // Campaigns
            foreach (var p in Campaigns.All)
                yield return (p, "Campaigns");

            // Coupons
            foreach (var p in Coupons.All)
                yield return (p, "Coupons");

            // Couriers
            foreach (var p in Couriers.All)
                yield return (p, "Couriers");

            // Shipping
            foreach (var p in Shipping.All)
                yield return (p, "Shipping");

            // Reports
            foreach (var p in Reports.All)
                yield return (p, "Reports");

            // Banners
            foreach (var p in Banners.All)
                yield return (p, "Banners");

            // Brands
            foreach (var p in Brands.All)
                yield return (p, "Brands");

            // Settings
            foreach (var p in Settings.All)
                yield return (p, "Settings");

            // Logs
            foreach (var p in Logs.All)
                yield return (p, "Logs");
        }

        /// <summary>
        /// Permission adından okunabilir görüntü adı üretir.
        /// Örnek: "products.create" -> "Ürün Oluşturma"
        /// </summary>
        public static string GetDisplayName(string permissionName)
        {
            // Module ve action parçalarını ayır
            var parts = permissionName.Split('.');
            if (parts.Length != 2)
                return permissionName;

            var module = parts[0];
            var action = parts[1];

            // Modül çevirisi
            var moduleDisplay = module switch
            {
                "dashboard" => "Dashboard",
                "products" => "Ürün",
                "categories" => "Kategori",
                "orders" => "Sipariş",
                "users" => "Kullanıcı",
                "roles" => "Rol",
                "campaigns" => "Kampanya",
                "coupons" => "Kupon",
                "couriers" => "Kurye",
                "shipping" => "Kargo",
                "reports" => "Rapor",
                "banners" => "Banner",
                "brands" => "Marka",
                "settings" => "Ayar",
                "logs" => "Log",
                _ => module
            };

            // Aksiyon çevirisi
            var actionDisplay = action switch
            {
                "view" => "Görüntüleme",
                "create" => "Oluşturma",
                "update" => "Güncelleme",
                "delete" => "Silme",
                "manage" => "Yönetim",
                "export" => "Dışa Aktarma",
                "import" => "İçe Aktarma",
                "status" => "Durum Güncelleme",
                "cancel" => "İptal Etme",
                "refund" => "İade İşlemi",
                "stock" => "Stok Yönetimi",
                "pricing" => "Fiyat Yönetimi",
                "roles" => "Rol Atama",
                "permissions" => "İzin Yönetimi",
                "sensitive" => "Hassas Veri Erişimi",
                "assign" => "Atama",
                "assign_courier" => "Kurye Atama",
                "customer_info" => "Müşteri Bilgisi",
                "pending" => "Bekleyenler",
                "tracking" => "Takip Numarası",
                "ship" => "Kargoya Ver",
                "deliver" => "Teslim Et",
                "sales" => "Satış",
                "inventory" => "Envanter",
                "customers" => "Müşteriler",
                "financial" => "Finansal",
                "weight" => "Ağırlık",
                "audit" => "Denetim",
                "error" => "Hata",
                "sync" => "Senkronizasyon",
                "statistics" => "İstatistikler",
                "revenue" => "Gelir",
                "payment" => "Ödeme",
                "sms" => "SMS",
                "email" => "E-posta",
                "performance" => "Performans",
                "details" => "Detay",
                "system" => "Sistem",
                _ => action
            };

            return $"{moduleDisplay} {actionDisplay}";
        }

        /// <summary>
        /// Verilen permission adının sistemde tanımlı olup olmadığını kontrol eder.
        /// </summary>
        public static bool IsValidPermission(string permissionName)
        {
            return GetAllPermissions().Any(p => p.Name.Equals(permissionName, StringComparison.OrdinalIgnoreCase));
        }

        #endregion
    }
}
