using ECommerce.Core.DTOs.Micro;
using ECommerce.Core.Interfaces.Mapping;
using ECommerce.Entities.Concrete;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ECommerce.Business.Services.Mapping
{
    /// <summary>
    /// E-ticaret Müşteri → Mikro ERP Cari Hesap dönüşümü.
    /// 
    /// GÖREV: E-ticaret müşterilerini Mikro ERP cari hesap formatına çevirir.
    /// Yeni sipariş oluşturulmadan önce müşteri Mikro'ya aktarılmalıdır.
    /// 
    /// MAPPING:
    /// User.Id → cari_ozel_kod (çift yönlü eşleştirme için)
    /// User.FullName → cari_unvan1
    /// User.Email → cari_EMail
    /// User.PhoneNumber → cari_CepTel
    /// "ONL-{UserId}" → cari_kod (online müşteri prefix'i)
    /// Address → adresler listesi
    /// </summary>
    public class MikroCariMapper : IMikroCariMapper
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<MikroCariMapper> _logger;

        /// <summary>
        /// Mikro cari ayarları cache.
        /// </summary>
        private readonly MikroCariSettings _settings;

        public MikroCariMapper(
            IConfiguration configuration,
            ILogger<MikroCariMapper> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _settings = LoadSettings();
        }

        /// <summary>
        /// E-ticaret kullanıcısını Mikro ERP cari formatına dönüştürür.
        /// </summary>
        /// <param name="user">E-ticaret kullanıcı entity'si</param>
        /// <param name="addresses">Kullanıcının adresleri (opsiyonel)</param>
        /// <returns>Mikro ERP cari kayıt DTO</returns>
        public MikroCariKaydetRequestDto ToMikroCari(User user, IEnumerable<Address>? addresses = null)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user), "Kullanıcı null olamaz");

            _logger.LogDebug(
                "Kullanıcı {UserId} ({Email}) Mikro cari formatına dönüştürülüyor",
                user.Id, user.Email);

            try
            {
                var dto = new MikroCariKaydetRequestDto
                {
                    // Cari Kodu - Benzersiz tanımlayıcı
                    CariKod = GenerateCariKod(user),

                    // Ünvan Bilgileri
                    CariUnvan1 = GetUnvan(user),
                    CariUnvan2 = null, // İkinci ünvan satırı - bireysel müşteride kullanılmıyor

                    // Cari Tipi
                    CariHareketTipi = 0, // 0 = Müşteri (Alıcı)
                    CariKisiKurumFlg = 0, // 0 = Şahıs (bireysel)

                    // Grup / Bölge
                    CariBolgeKodu = _settings.DefaultBolgeKodu,

                    // İletişim Bilgileri
                    CariEmail = user.Email,
                    CariCepTel = FormatPhoneNumber(user.PhoneNumber),
                    CariTel1 = null, // Sabit telefon

                    // Vergi Bilgileri (bireysel müşteri için boş)
                    CariVdaireAdi = null,
                    CariVdaireNo = null,
                    CariTckNo = null, // TC Kimlik - KVKK gereği saklanmıyor olabilir

                    // Ödeme Ayarları
                    CariOdemeVade = _settings.DefaultOdemeVade,
                    CariRiskLimit = _settings.DefaultRiskLimit,
                    CariFiyatListeNo = _settings.DefaultFiyatListeNo,
                    CariIskontoOran = null, // Müşteriye özel iskonto yok

                    // Temsilci
                    CariTemsilciKodu = _settings.DefaultTemsilciKodu,

                    // Durum
                    CariPasifFl = !user.IsActive,

                    // E-ticaret Referans
                    CariOzelKod = user.Id.ToString(),

                    // Açıklama
                    CariAciklama = BuildCariAciklama(user),

                    // Web
                    CariWeb = null,

                    // Adresler
                    Adresler = MapAddresses(addresses),

                    // Yetkililer (bireysel müşteri için kullanıcının kendisi)
                    Yetkililer = MapYetkililer(user)
                };

                _logger.LogInformation(
                    "Kullanıcı {UserId} başarıyla Mikro cari formatına dönüştürüldü. " +
                    "Cari Kod: {CariKod}",
                    user.Id, dto.CariKod);

                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Kullanıcı {UserId} Mikro cari formatına dönüştürülürken hata",
                    user.Id);
                throw;
            }
        }

        /// <summary>
        /// Misafir sipariş bilgilerinden Mikro cari oluşturur.
        /// Kayıtlı olmayan müşteriler için geçici cari hesap.
        /// </summary>
        /// <param name="order">Sipariş bilgileri (misafir)</param>
        /// <returns>Mikro ERP cari kayıt DTO</returns>
        public MikroCariKaydetRequestDto ToMikroCariFromGuestOrder(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order), "Sipariş null olamaz");

            if (string.IsNullOrEmpty(order.CustomerEmail))
            {
                _logger.LogWarning(
                    "Misafir sipariş {OrderNumber} için e-posta bulunamadı, " +
                    "geçici cari oluşturuluyor",
                    order.OrderNumber);
            }

            _logger.LogDebug(
                "Misafir sipariş {OrderNumber} için cari oluşturuluyor",
                order.OrderNumber);

            try
            {
                var dto = new MikroCariKaydetRequestDto
                {
                    // Cari Kodu - Misafir siparişler için özel format
                    CariKod = GenerateGuestCariKod(order),

                    // Ünvan
                    CariUnvan1 = order.CustomerName ?? "Misafir Müşteri",
                    CariUnvan2 = null,

                    // Cari Tipi
                    CariHareketTipi = 0, // Müşteri
                    CariKisiKurumFlg = 0, // Şahıs

                    // Grup
                    CariBolgeKodu = _settings.GuestBolgeKodu,

                    // İletişim
                    CariEmail = order.CustomerEmail,
                    CariCepTel = FormatPhoneNumber(order.CustomerPhone),
                    CariTel1 = null,

                    // Vergi (yok)
                    CariVdaireAdi = null,
                    CariVdaireNo = null,
                    CariTckNo = null,

                    // Ödeme (peşin)
                    CariOdemeVade = 0,
                    CariRiskLimit = 0,
                    CariFiyatListeNo = _settings.DefaultFiyatListeNo,
                    CariIskontoOran = null,

                    // Temsilci
                    CariTemsilciKodu = _settings.DefaultTemsilciKodu,

                    // Durum
                    CariPasifFl = false,

                    // E-ticaret Referans
                    CariOzelKod = $"GUEST-{order.OrderNumber}",

                    // Açıklama
                    CariAciklama = $"E-ticaret misafir sipariş: {order.OrderNumber}",

                    // Adres (sipariş teslimat adresi)
                    Adresler = MapGuestAddress(order),

                    // Yetkili yok
                    Yetkililer = null
                };

                _logger.LogInformation(
                    "Misafir sipariş {OrderNumber} için cari oluşturuldu. " +
                    "Cari Kod: {CariKod}",
                    order.OrderNumber, dto.CariKod);

                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Misafir sipariş {OrderNumber} için cari oluşturulurken hata",
                    order.OrderNumber);
                throw;
            }
        }

        #region Private Mapping Methods

        /// <summary>
        /// Cari kodu oluşturur.
        /// Format: {Prefix}-{UserId:D6}
        /// Örnek: ONL-000123
        /// </summary>
        private string GenerateCariKod(User user)
        {
            return $"{_settings.CariKodPrefix}-{user.Id:D6}";
        }

        /// <summary>
        /// Misafir müşteri için cari kodu oluşturur.
        /// Format: {GuestPrefix}-{Date}{Random}
        /// Örnek: MSF-20240115A1B2
        /// </summary>
        private string GenerateGuestCariKod(Order order)
        {
            // Sipariş numarasından kısa bir kod oluştur
            var dateCode = order.OrderDate.ToString("yyMMdd");
            var shortId = order.OrderNumber.Length > 4 
                ? order.OrderNumber[^4..] 
                : order.OrderNumber;
            
            return $"{_settings.GuestCariKodPrefix}-{dateCode}{shortId}";
        }

        /// <summary>
        /// Kullanıcı ünvanını döndürür.
        /// </summary>
        private string GetUnvan(User user)
        {
            // FullName varsa kullan
            if (!string.IsNullOrWhiteSpace(user.FullName))
                return user.FullName.Trim();

            // FirstName + LastName
            var fullName = $"{user.FirstName} {user.LastName}".Trim();
            if (!string.IsNullOrWhiteSpace(fullName))
                return fullName;

            // E-posta'dan türet
            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                var emailPrefix = user.Email.Split('@')[0];
                return $"Müşteri {emailPrefix}";
            }

            return $"Müşteri #{user.Id}";
        }

        /// <summary>
        /// Telefon numarasını formatlar.
        /// Mikro formatı: 05XXXXXXXXX
        /// </summary>
        private string? FormatPhoneNumber(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return null;

            // Sadece rakamları al
            var digits = new string(phone.Where(char.IsDigit).ToArray());

            // Türkiye kodu varsa kaldır
            if (digits.StartsWith("90") && digits.Length == 12)
                digits = "0" + digits[2..];
            else if (digits.StartsWith("9") && digits.Length == 11)
                digits = "0" + digits[1..];
            else if (!digits.StartsWith("0") && digits.Length == 10)
                digits = "0" + digits;

            // 11 haneli olmalı
            if (digits.Length == 11 && digits.StartsWith("05"))
                return digits;

            // Orijinal değeri döndür (format hatalıysa)
            _logger.LogWarning(
                "Telefon numarası formatlanamadı: {Phone} → {Digits}",
                phone, digits);
            return phone;
        }

        /// <summary>
        /// Kullanıcı adreslerini Mikro formatına dönüştürür.
        /// </summary>
        private List<MikroCariAdresDto>? MapAddresses(IEnumerable<Address>? addresses)
        {
            if (addresses == null || !addresses.Any())
                return null;

            var result = new List<MikroCariAdresDto>();
            int adrNo = 1;

            // Önce varsayılan adres
            var orderedAddresses = addresses
                .OrderByDescending(a => a.IsDefault)
                .ThenBy(a => a.Id);

            foreach (var address in orderedAddresses)
            {
                result.Add(new MikroCariAdresDto
                {
                    AdrNo = adrNo,
                    AdrBaslik = address.Title ?? (adrNo == 1 ? "Ana Adres" : $"Adres {adrNo}"),
                    AdrTipi = adrNo == 1 ? 2 : 1, // İlk adres hem fatura hem teslimat
                    AdrCadde = address.Street,
                    AdrMahalle = null, // Street içinde olabilir
                    AdrBinaNo = null, // Street içinde
                    AdrIlce = address.District,
                    AdrIl = address.City,
                    AdrUlke = "TÜRKİYE",
                    AdrPostaKodu = address.PostalCode,
                    AdrTelefon = FormatPhoneNumber(address.Phone),
                    AdrVarsayilan = address.IsDefault
                });

                adrNo++;
            }

            return result;
        }

        /// <summary>
        /// Misafir sipariş adresini Mikro formatına dönüştürür.
        /// </summary>
        private List<MikroCariAdresDto>? MapGuestAddress(Order order)
        {
            if (string.IsNullOrEmpty(order.ShippingAddress))
                return null;

            return new List<MikroCariAdresDto>
            {
                new MikroCariAdresDto
                {
                    AdrNo = 1,
                    AdrBaslik = "Teslimat Adresi",
                    AdrTipi = 2, // Hem fatura hem teslimat
                    AdrCadde = order.ShippingAddress,
                    AdrMahalle = null,
                    AdrBinaNo = null,
                    AdrIlce = null,
                    AdrIl = order.ShippingCity,
                    AdrUlke = "TÜRKİYE",
                    AdrPostaKodu = null,
                    AdrTelefon = FormatPhoneNumber(order.CustomerPhone),
                    AdrVarsayilan = true
                }
            };
        }

        /// <summary>
        /// Yetkili kişiler listesi oluşturur (bireysel müşteri için).
        /// </summary>
        private List<MikroCariYetkiliDto>? MapYetkililer(User user)
        {
            // Temel bilgiler yoksa yetkili ekleme
            if (string.IsNullOrWhiteSpace(user.FullName) && 
                string.IsNullOrWhiteSpace(user.FirstName))
            {
                return null;
            }

            return new List<MikroCariYetkiliDto>
            {
                new MikroCariYetkiliDto
                {
                    YtkAdSoyad = GetUnvan(user),
                    YtkUnvan = null,
                    YtkEmail = user.Email,
                    YtkCep = FormatPhoneNumber(user.PhoneNumber),
                    YtkDahili = null,
                    YtkAnaYetkili = true
                }
            };
        }

        /// <summary>
        /// Cari açıklama metni oluşturur.
        /// </summary>
        private string BuildCariAciklama(User user)
        {
            var parts = new List<string>
            {
                "E-ticaret müşterisi"
            };

            if (user.CreatedAt != default)
            {
                parts.Add($"Kayıt: {user.CreatedAt:dd.MM.yyyy}");
            }

            if (user.LastLoginAt.HasValue)
            {
                parts.Add($"Son giriş: {user.LastLoginAt.Value:dd.MM.yyyy}");
            }

            return string.Join(" | ", parts);
        }

        /// <summary>
        /// Mikro cari ayarlarını yükler.
        /// </summary>
        private MikroCariSettings LoadSettings()
        {
            var section = _configuration.GetSection("MikroApi:Cari");

            return new MikroCariSettings
            {
                CariKodPrefix = section["CariKodPrefix"] ?? "ONL",
                GuestCariKodPrefix = section["GuestCariKodPrefix"] ?? "MSF",
                DefaultBolgeKodu = section["DefaultBolgeKodu"] ?? "ONLINE",
                GuestBolgeKodu = section["GuestBolgeKodu"] ?? "GUEST",
                DefaultOdemeVade = section.GetValue<int>("DefaultOdemeVade", 0),
                DefaultRiskLimit = section.GetValue<decimal>("DefaultRiskLimit", 0),
                DefaultFiyatListeNo = section.GetValue<int>("DefaultFiyatListeNo", 1),
                DefaultTemsilciKodu = section["DefaultTemsilciKodu"]
            };
        }

        #endregion

        #region Settings Class

        /// <summary>
        /// Mikro cari ayarları.
        /// </summary>
        private class MikroCariSettings
        {
            /// <summary>
            /// Kayıtlı müşteri cari kod prefix'i.
            /// Örn: ONL → ONL-000123
            /// </summary>
            public string CariKodPrefix { get; set; } = "ONL";

            /// <summary>
            /// Misafir müşteri cari kod prefix'i.
            /// Örn: MSF → MSF-240115A1B2
            /// </summary>
            public string GuestCariKodPrefix { get; set; } = "MSF";

            /// <summary>
            /// Kayıtlı müşterilerin bölge kodu.
            /// </summary>
            public string DefaultBolgeKodu { get; set; } = "ONLINE";

            /// <summary>
            /// Misafir müşterilerin bölge kodu.
            /// </summary>
            public string GuestBolgeKodu { get; set; } = "GUEST";

            /// <summary>
            /// Varsayılan ödeme vadesi (gün).
            /// 0 = Peşin
            /// </summary>
            public int DefaultOdemeVade { get; set; } = 0;

            /// <summary>
            /// Varsayılan risk limiti.
            /// </summary>
            public decimal DefaultRiskLimit { get; set; } = 0;

            /// <summary>
            /// Varsayılan fiyat listesi numarası.
            /// </summary>
            public int DefaultFiyatListeNo { get; set; } = 1;

            /// <summary>
            /// Varsayılan satış temsilcisi kodu.
            /// </summary>
            public string? DefaultTemsilciKodu { get; set; }
        }

        #endregion
    }
}
