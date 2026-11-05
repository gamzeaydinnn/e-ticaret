using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ECommerce.Core.Helpers;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using ECommerce.Infrastructure.Services.MicroServices;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Data
{
    /// <summary>
    /// Mikro ERP'deki gerçek kategori hiyerarşisini (STOK_ANA_GRUPLARI + STOK_ALT_GRUPLARI)
    /// yerel Categories ve MikroCategoryMappings tablolarına idempotent olarak senkronize eder.
    ///
    /// TEMEL KURALLAR:
    /// 1. 19 yeni düzen Mikro ana grubu (100..2200) ve tüm alt grupları (101..2204) eksiksiz oluşturulur
    ///    (0 ürünlü gruplar dahil).
    /// 2. Mevcut kategorilerin IsActive (aktif/pasif) durumuna ASLA dokunulmaz — Admin kontrolündedir.
    /// 3. Mikro'ya bu sınıftan ASLA yazma (INSERT/UPDATE) yapılmaz; yalnızca okuma yapılır.
    /// 4. Eski düzen (1-2 haneli: 01..32) grup kodları, karşılık gelen yeni düzen alt/ana kategorilerine
    ///    MikroCategoryMapping üzerinden yönlendirilir.
    /// 5. Yasaklı gruplar (02 Alkol, 03 Tütün) vitrinde pasife alınır.
    /// </summary>
    public class CategorySeeder
    {
        private static readonly CultureInfo TrCulture = new("tr-TR");

        // Sabit slug — kod genelinde bu slug ile "Diğer" kategorisi aranır
        public const string UncategorizedSlug = "diger";
        public const string UncategorizedName = "Diğer";

        // Yasaklı Mikro ana grup kodları (İnternetten satışı yasal olarak yasak: Alkol ve Tütün)
        public static readonly HashSet<string> ForbiddenAnaGrupKodlari = new(StringComparer.OrdinalIgnoreCase)
        {
            "02", "2", "ALKOLLU ICECEKLER", "ALKOLLÜ İÇECEKLER", "ALKOLLUICECEKLER",
            "03", "3", "SIGARA, TUTUN VE MAMULLERI", "SİGARA, TÜTÜN VE MAMÜLLERİ"
        };

        /// <summary>
        /// Üst menü (Navbar) 9 sabit kategori hapının anahtarları.
        /// Birleşik gruplar:
        /// - atistirmalik = 1400 (Atıştırmalık) + 1100 (Kuruyemiş)
        /// - dondurma-ve-dondurulmus-gida = 1300 (Dondurulmuş Gıda) + 800 (Dondurma)
        /// - temizlik = 1000 (Ev Bakım) + 1500 (Kağıt Islak Mendil)
        /// - ev-ve-mutfak = 2000 (Ev Yaşam)
        /// </summary>
        public static readonly (
            string AnaKod,
            string MikroIsim,
            string DefaultDisplayName,
            string Slug,
            string Description,
            int SortOrder,
            string? NavbarGroupKey
        )[] CanonicalMainGroups =
        {
            ("400",  "ET VE TAVUK",        "Et ve Tavuk",               "et-ve-et-urunleri",            "Taze kırmızı et, beyaz et ve şarküteri ürünleri",              1,  "et-ve-et-urunleri"),
            ("500",  "SÜT KAHVALTI",       "Süt & Kahvaltılık",         "sut-ve-sut-urunleri",          "Süt, peynir, yoğurt, tereyağı, yumurta, zeytin ve kahvaltılık", 2,  "sut-ve-sut-urunleri"),
            ("2200", "MEYVE SEBZE",        "Meyve ve Sebze",            "meyve-ve-sebze",               "Taze meyve, sebze ve yeşillikler",                              3,  "meyve-ve-sebze"),
            ("100",  "İÇECEKLER",          "İçecekler",                 "icecekler",                    "Su, maden suyu, çay, kahve, gazlı ve gazsız içecekler",         4,  "icecekler"),
            ("1400", "ATIŞTIRMALIK",       "Atıştırmalık",              "atistirmalik",                 "Çikolata, bisküvi, cips, gofret, kek, kraker ve şekerleme",     5,  "atistirmalik"),
            ("1100", "KURUYEMİŞ",          "Kuruyemiş",                 "kuruyemis",                    "Açık ve paket kuruyemiş çeşitleri",                             6,  "atistirmalik"),
            ("1300", "DONDURULMUŞ GIDA",   "Dondurulmuş Gıda",          "dondurma-ve-dondurulmus-gida", "Donuk balık, et, fırın, sebze ve hazır gıda ürünleri",          7,  "dondurma-ve-dondurulmus-gida"),
            ("800",  "DONDURMA",           "Dondurma",                  "dondurma",                     "Kap ve tek dondurma çeşitleri",                                 8,  "dondurma-ve-dondurulmus-gida"),
            ("1000", "EV BAKIM",           "Ev Bakım & Temizlik",       "temizlik",                     "Çamaşır, bulaşık, genel temizlik ve temizlik malzemeleri",      9,  "temizlik"),
            ("1500", "KAĞIT ISLAK MENDİL", "Kağıt & Islak Mendil",      "kagit-islak-mendil",           "Tuvalet kağıdı, kağıt havlu, peçete ve ıslak mendil",           10, "temizlik"),
            ("700",  "TEMEL GIDA",         "Temel Gıda",                "temel-gida",                   "Bakliyat, makarna, sıvı yağ, konserve, sos, un, şeker, baharat", 11, "temel-gida"),
            ("2000", "EV YAŞAM",           "Ev & Mutfak",               "ev-ve-mutfak",                 "Mutfak eşyaları, ev tekstili, piknik, kırtasiye, pil ve mum",   12, "ev-ve-mutfak"),
            ("600",  "FIRIN PASTANE",      "Fırın & Pastane",           "firin-pastane",                "Ekmek, sıcak fırın, kuru pasta, tatlı ve hamur malzemeleri",    13, null),
            ("1200", "HAZIR YEMEK MEZE",   "Hazır Yemek & Meze",        "hazir-yemek-meze",             "Meze ve pratik hazır yemek çeşitleri",                          14, null),
            ("1600", "KİŞİSEL BAKIM",      "Kişisel Bakım",             "kisisel-bakim",                "Saç, ağız, cilt, duş, güneş bakımı, parfüm ve hijyen",          15, null),
            ("900",  "BEBEK OYUNCAK",      "Bebek & Oyuncak",           "bebek-oyuncak",                "Bebek bezi, bebek beslenme, bebek bakım ve oyuncak",            16, null),
            ("1700", "PET SHOP",           "Pet Shop",                  "pet-shop",                     "Kedi, köpek, kuş mamaları ve pet aksesuarları",                 17, null),
            ("1900", "ELEKTRONİK",         "Elektronik",                "elektronik",                   "Küçük ev aletleri, aydınlatma ve telefon aksesuarları",         18, null),
            ("2100", "ORGANİK GURME",      "Organik & Gurme",           "organik-gurme",                "Organik ve gurme seçki ürünleri",                               19, null),
        };

        public static IReadOnlyCollection<string> PublicStorefrontCategorySlugs { get; } =
            Array.AsReadOnly(CanonicalMainGroups.Select(item => item.Slug).ToArray());

        /// <summary>
        /// Mikro STOK_ALT_GRUPLARI tablosundaki yeni düzen (101..2204) tüm alt gruplar.
        /// Mikro SQL bağlantısı çevrimdışı olsa bile tam ağaç bu tablodan kurulur.
        /// </summary>
        public static readonly (string AnaKod, string AltKod, string MikroIsim, string DisplayName, int SortOrder)[] CanonicalSubGroups =
        {
            // 100 - İÇECEKLER
            ("100", "101", "ÇAY",                        "Çay",                        1),
            ("100", "102", "KAHVE",                      "Kahve",                      2),
            ("100", "103", "GAZSIZ İÇECEK",              "Gazsız İçecek",              3),
            ("100", "104", "GAZLI İÇECEK",               "Gazlı İçecek",               4),
            ("100", "105", "MADEN SUYU",                 "Maden Suyu",                 5),
            ("100", "106", "SU",                         "Su",                         6),

            // 400 - ET VE TAVUK
            ("400", "401", "BEYAZ ET",                   "Beyaz Et",                   1),
            ("400", "402", "KIRMIZI ET",                 "Kırmızı Et",                 2),

            // 500 - SÜT KAHVALTI
            ("500", "501", "SÜT",                        "Süt",                        1),
            ("500", "502", "PEYNİR",                     "Peynir",                     2),
            ("500", "503", "ET ŞARKÜTERİ",               "Et Şarküteri",               3),
            ("500", "504", "YOĞURT",                     "Yoğurt",                     4),
            ("500", "505", "SÜTLÜ TATLI KREMA KAYMAK",   "Sütlü Tatlı, Krema & Kaymak", 5),
            ("500", "506", "KAHVALTILIK",                "Kahvaltılık",                6),
            ("500", "507", "MARGARİN",                   "Margarin",                   7),
            ("500", "508", "TEREYAĞ",                    "Tereyağı",                   8),
            ("500", "509", "YUMURTA",                    "Yumurta",                    9),
            ("500", "510", "ZEYTİN",                     "Zeytin",                     10),

            // 600 - FIRIN PASTANE
            ("600", "601", "EKMEK",                      "Ekmek",                      1),
            ("600", "602", "GALETA GRİSİNİ GEVREK",      "Galeta, Grissini & Gevrek",  2),
            ("600", "603", "HAMUR PASTA MALZ.",          "Hamur & Pasta Malzemeleri",  3),
            ("600", "604", "KURU PASTA",                 "Kuru Pasta",                 4),
            ("600", "605", "SICAK FIRIN",                "Sıcak Fırın",                5),
            ("600", "606", "TATLI",                      "Tatlı",                      6),

            // 700 - TEMEL GIDA
            ("700", "701", "BAKLİYAT",                   "Bakliyat",                   1),
            ("700", "702", "BULYON",                     "Bulyon",                     2),
            ("700", "703", "KONSERVE",                   "Konserve & Turşu",           3),
            ("700", "704", "MAKARNA",                    "Makarna",                    4),
            ("700", "705", "SIVI YAĞ",                   "Sıvı Yağ & Zeytinyağı",      5),
            ("700", "706", "SOS",                        "Sos, Ketçap & Mayonez",      6),
            ("700", "707", "ŞEKER",                      "Şeker",                      7),
            ("700", "708", "TUZ BAHARAT HARÇ",           "Tuz, Baharat & Harç",        8),
            ("700", "709", "UN",                         "Un",                         9),

            // 800 - DONDURMA
            ("800", "801", "KAP DONDURMA",               "Kap Dondurma",               1),
            ("800", "802", "TEK DONDURMA",               "Tek Dondurma",               2),

            // 900 - BEBEK OYUNCAK
            ("900", "901", "OYUNCAK",                    "Oyuncak",                    1),
            ("900", "902", "BEBEK BEZİ",                 "Bebek Bezi",                 2),
            ("900", "903", "BEBEK BAKIM",                "Bebek Bakım",                3),
            ("900", "904", "BEBEK BANYO",                "Bebek Banyo",                4),
            ("900", "905", "BEBEK BESLENME",             "Bebek Beslenme",             5),
            ("900", "906", "DETERJAN VE YUMUŞATICI",     "Bebek Deterjan & Yumuşatıcı", 6),

            // 1000 - EV BAKIM
            ("1000", "1001", "TEMİZLİK MALZ.",           "Temizlik Malzemeleri",       1),
            ("1000", "1002", "GENEL TEMİZLİK",           "Genel Temizlik",             2),
            ("1000", "1003", "ÇÖP POŞETİ",               "Çöp Poşeti",                 3),
            ("1000", "1004", "ÇAMAŞIR YIKAMA",           "Çamaşır Yıkama",             4),
            ("1000", "1005", "BULAŞIK YIKAMA",           "Bulaşık Yıkama",             5),

            // 1100 - KURUYEMİŞ
            ("1100", "1101", "AÇIK KURUYEMİŞ",           "Açık Kuruyemiş",             1),
            ("1100", "1102", "PAKET KURUYEMİŞ",          "Paket Kuruyemiş",            2),

            // 1200 - HAZIR YEMEK MEZE
            ("1200", "1201", "MEZE",                     "Meze",                       1),
            ("1200", "1202", "PRATİK YEMEK",             "Pratik Yemek",               2),
            ("1200", "1203", "HAMUR PASTA MALZ.",        "Hamur & Pasta Malzemeleri",  3),
            ("1200", "1204", "SOS",                      "Sos",                        4),
            ("1200", "1205", "TUZ BAHARAT HARÇ",         "Tuz, Baharat & Harç",        5),

            // 1300 - DONDURULMUŞ GIDA
            ("1300", "1301", "DONUK BALIK",              "Donuk Balık",                1),
            ("1300", "1302", "DONUK BEYAZ ET",           "Donuk Beyaz Et",             2),
            ("1300", "1303", "DONUK KIRMIZI ET",         "Donuk Kırmızı Et",           3),
            ("1300", "1304", "DONUK MEYVE SEBZE",        "Donuk Meyve & Sebze",        4),
            ("1300", "1305", "DONUK FIRIN",              "Donuk Fırın Ürünleri",       5),
            ("1300", "1306", "DONUK HAZIR GIDA",         "Donuk Hazır Gıda",           6),

            // 1400 - ATIŞTIRMALIK
            ("1400", "1401", "SAKIZ",                    "Sakız",                      1),
            ("1400", "1402", "ŞEKERLEME",                "Şekerleme",                  2),
            ("1400", "1403", "CİPS",                     "Cips",                       3),
            ("1400", "1404", "ÇİKOLATA",                 "Çikolata",                   4),
            ("1400", "1405", "KEK",                      "Kek",                        5),
            ("1400", "1406", "GOFRET",                   "Gofret",                     6),
            ("1400", "1407", "BİSKÜVİ",                  "Bisküvi",                    7),
            ("1400", "1408", "BAR KAPLAMALILAR",         "Bar & Kaplamalılar",         8),
            ("1400", "1409", "KRAKER",                   "Kraker",                     9),
            ("1400", "1410", "MISIR VE PİRİNÇ PATLAĞI",  "Mısır & Pirinç Patlağı",     10),

            // 1500 - KAĞIT ISLAK MENDİL
            ("1500", "1501", "ISLAK MENDİL",             "Islak Mendil",               1),
            ("1500", "1502", "KAĞIT HAVLU",              "Kağıt Havlu",                2),
            ("1500", "1503", "TUVALET KAĞIDI",           "Tuvalet Kağıdı",             3),
            ("1500", "1504", "KAĞIT MENDİL",             "Kağıt Mendil",               4),
            ("1500", "1505", "PEÇETE",                   "Peçete",                     5),

            // 1600 - KİŞİSEL BAKIM
            ("1600", "1601", "AĞDA EPİLASYON",           "Ağda & Epilasyon",           1),
            ("1600", "1602", "AĞIZ BAKIM",               "Ağız & Diş Bakımı",          2),
            ("1600", "1603", "CİLT BAKIM",               "Cilt Bakımı",                3),
            ("1600", "1604", "DUŞ BANYO SABUN",          "Duş, Banyo & Sabun",         4),
            ("1600", "1605", "GÜNEŞ BAKIM",              "Güneş Bakım",                5),
            ("1600", "1606", "HİJYENİK PET",             "Hijyenik Ped",               6),
            ("1600", "1607", "KOLONYA",                  "Kolonya",                    7),
            ("1600", "1608", "PARFÜM DEODORANT",         "Parfüm & Deodorant",         8),
            ("1600", "1609", "SAÇ BAKIM",                "Saç Bakımı",                 9),
            ("1600", "1610", "SAĞLIK ÜRÜNLERİ",          "Sağlık Ürünleri",            10),
            ("1600", "1611", "TIRAŞ MALZ.",              "Tıraş Malzemeleri",          11),

            // 1700 - PET SHOP
            ("1700", "1701", "KÖPEK",                    "Köpek Ürünleri",             1),
            ("1700", "1702", "KEDİ",                     "Kedi Ürünleri",              2),
            ("1700", "1703", "KUŞ",                      "Kuş Ürünleri",               3),
            ("1700", "1704", "PET VE AKSESUAR",          "Pet Aksesuar",               4),

            // 1900 - ELEKTRONİK
            ("1900", "1901", "AYDINATMA VE ELEKTRİK MALZ.", "Aydınlatma & Elektrik",   1),
            ("1900", "1902", "ELEKTRİKLİ EV ALETLERİ",      "Elektrikli Ev Aletleri",  2),
            ("1900", "1903", "TELEFON VE AKSESUARLARI",     "Telefon & Aksesuar",      3),

            // 2000 - EV YAŞAM
            ("2000", "2001", "ÇAKMAK",                   "Çakmak & Kibrit",            1),
            ("2000", "2002", "EV TEKSTİLİ VE AKSESUARLARI", "Ev Tekstili & Aksesuar",  2),
            ("2000", "2003", "GİYİM",                    "Giyim & Tekstil",            3),
            ("2000", "2004", "HIRDAVAT",                 "Hırdavat",                   4),
            ("2000", "2005", "KIRTASİYE",                "Kırtasiye",                  5),
            ("2000", "2006", "MUM",                      "Mum & Oda Kokusu",           6),
            ("2000", "2007", "MUTFAK EŞYALARI",          "Mutfak Eşyaları",            7),
            ("2000", "2008", "PİKNİK MALZ.",             "Piknik & Parti Malzemeleri", 8),
            ("2000", "2009", "PİL",                      "Pil",                        9),

            // 2100 - ORGANİK GURME
            ("2100", "2101", "ORGANİK GURME",            "Organik & Gurme",            1),

            // 2200 - MEYVE SEBZE
            ("2200", "2201", "MEYVE",                    "Meyve",                      1),
            ("2200", "2202", "SEBZE",                    "Sebze",                      2),
            ("2200", "2203", "KONSERVE",                 "Konserve Meyve & Sebze",     3),
            ("2200", "2204", "DONUK MEYVE SEBZE",        "Donuk Meyve & Sebze",        4),
        };

        /// <summary>
        /// Eski düzen (1-2 haneli) Mikro kodlarını yeni düzen (3-4 haneli) hedef kodlara bağlayan tablo.
        /// (EskiAnaKod, EskiAltKod, HedefYeniAnaKod, HedefYeniAltKod, Açıklama)
        /// </summary>
        private static readonly (string OldAna, string? OldAlt, string TargetAna, string? TargetAlt, string Description)[] LegacyToNewMappings =
        {
            // 01 -> 100 İÇECEKLER
            ("01", null, "100", null, "Eski İçecekler -> 100 İçecekler"),
            ("01", "01", "100", "101", "Eski Çay -> 101 Çay"),
            ("01", "02", "100", "102", "Eski Kahve -> 102 Kahve"),
            ("01", "03", "100", "106", "Eski Su -> 106 Su"),
            ("01", "04", "100", "104", "Eski Gazlı İçecek -> 104 Gazlı İçecek"),
            ("01", "05", "100", "105", "Eski Maden Suları -> 105 Maden Suyu"),
            ("01", "06", "100", "103", "Eski Gazsız İçecek -> 103 Gazsız İçecek"),
            ("01", "07", "100", "103", "Eski Şalgam Suları -> 103 Gazsız İçecek"),
            ("01", "08", "100", "103", "Eski Meyve Suları -> 103 Gazsız İçecek"),
            ("100", "05", "100", "105", "100/05 veri kirliliği -> 105 Maden Suyu"),

            // 04 -> 400 ET VE TAVUK
            ("04", null, "400", null, "Eski Et ve Tavuk -> 400 Et ve Tavuk"),
            ("04", "1",  "400", "401", "Eski Beyaz Et -> 401 Beyaz Et"),
            ("04", "2",  "400", "402", "Eski Kırmızı Et -> 402 Kırmızı Et"),

            // 05 & 06 & 07 -> 500 SÜT KAHVALTI
            ("05", null, "500", null, "Eski Süt ve Kahvaltı -> 500 Süt Kahvaltı"),
            ("05", "1",  "500", "501", "Eski Süt -> 501 Süt"),
            ("05", "2",  "500", "502", "Eski Peynir -> 502 Peynir"),
            ("05", "3",  "500", "503", "Eski Et Şarküteri -> 503 Et Şarküteri"),
            ("05", "4",  "500", "504", "Eski Yoğurt -> 504 Yoğurt"),
            ("05", "5",  "500", "505", "Eski Sütlü Tatlı -> 505 Sütlü Tatlı"),
            ("05", "6",  "500", "506", "Eski Kahvaltılık -> 506 Kahvaltılık"),
            ("05", "7",  "500", "507", "Eski Margarin -> 507 Margarin"),
            ("05", "8",  "500", "508", "Eski Tereyağ -> 508 Tereyağ"),
            ("05", "9",  "500", "509", "Eski Yumurta -> 509 Yumurta"),
            ("05", "10", "500", "510", "Eski Zeytin -> 510 Zeytin"),
            ("500", "5", "500", "505", "500/5 veri kirliliği -> 505 Sütlü Tatlı"),
            ("500", "402", "400", "402", "500/402 veri kirliliği -> 402 Kırmızı Et"),

            ("06", null, "500", "506", "Eski Şarküteri/Kahvaltılık -> 506 Kahvaltılık"),
            ("06", "01", "500", "503", "Eski Sosisler -> 503 Et Şarküteri"),
            ("06", "02", "500", "503", "Eski Sucuklar -> 503 Et Şarküteri"),
            ("06", "03", "500", "503", "Eski Salamlar -> 503 Et Şarküteri"),
            ("06", "04", "500", "503", "Eski Salamlar Tavuk -> 503 Et Şarküteri"),
            ("06", "05", "500", "503", "Eski Salamlar Et -> 503 Et Şarküteri"),
            ("06", "06", "500", "503", "Eski Sosisler Hindi -> 503 Et Şarküteri"),
            ("06", "07", "500", "506", "Eski Reçeller (Gölköy Gourmet) -> 506 Kahvaltılık"),
            ("06", "08", "500", "506", "Eski Marmelatlar -> 506 Kahvaltılık"),
            ("06", "09", "500", "506", "Eski Ballar -> 506 Kahvaltılık"),
            ("06", "10", "500", "510", "Eski Zeytin/Zeytin Ezmesi -> 510 Zeytin"),

            // 08 & 28 -> 600 FIRIN PASTANE
            ("08", null, "600", "601", "Eski Un ve Unlu Mamuller -> 601 Ekmek"),
            ("08", "05", "600", "601", "Eski Ekmek Tahıllı -> 601 Ekmek"),
            ("28", null, "600", null, "Eski Fırın Pastane -> 600 Fırın Pastane"),
            ("28", "1",  "600", "601", "Eski Ekmek -> 601 Ekmek"),
            ("28", "2",  "600", "602", "Eski Galeta -> 602 Galeta"),
            ("28", "3",  "600", "603", "Eski Hamur Pasta Malz -> 603 Hamur Pasta Malz"),
            ("28", "4",  "600", "604", "Eski Kuru Pasta -> 604 Kuru Pasta"),
            ("28", "5",  "600", "605", "Eski Sıcak Fırın -> 605 Sıcak Fırın"),
            ("28", "6",  "600", "606", "Eski Tatlı -> 606 Tatlı"),

            // 09 & 10 -> 700 TEMEL GIDA
            ("09", null, "700", "705", "Eski Yağlar -> 705 Sıvı Yağ"),
            ("09", "01", "700", "705", "Eski Sıvı Yağlar -> 705 Sıvı Yağ"),
            ("10", null, "700", null,  "Eski Temel Gıda -> 700 Temel Gıda"),
            ("10", "01", "700", "701", "Eski Bakliyatlar -> 701 Bakliyat"),
            ("10", "02", "700", "704", "Eski Makarnalar -> 704 Makarna"),
            ("10", "03", "700", "701", "Eski İrmikler -> 701 Bakliyat"),
            ("10", "04", "700", "708", "Eski Tuz Baharat Harç -> 708 Tuz Baharat Harç"),
            ("10", "05", "700", "703", "Eski Konserveler -> 703 Konserve"),
            ("10", "06", "700", "703", "Eski Turşular -> 703 Konserve & Turşu"),
            ("10", "07", "700", "706", "Eski Salçalar -> 706 Sos"),
            ("10", "08", "1200", "1202", "Eski Hazır Yemekler -> 1202 Pratik Yemek"),
            ("10", "10", "700", "706", "Eski Ketçaplar -> 706 Sos"),
            ("10", "11", "700", "706", "Eski Mayonezler -> 706 Sos"),
            ("10", "12", "700", "706", "Eski Soslar/Sirke/Nar Ekşisi -> 706 Sos"),
            ("10", "13", "700", "702", "Eski Bulyon -> 702 Bulyon"),
            ("10", "14", "700", "705", "Eski Sıvı Yağ -> 705 Sıvı Yağ"),
            ("10", "15", "700", "707", "Eski Şeker -> 707 Şeker"),
            ("10", "16", "700", "709", "Eski Un -> 709 Un"),

            // 11 -> 1100 KURUYEMİŞ
            ("11", null, "1100", null,   "Eski Kuruyemişler -> 1100 Kuruyemiş"),
            ("11", "01", "1100", "1101", "Eski Açık Kuruyemiş -> 1101 Açık Kuruyemiş"),
            ("11", "02", "1100", "1102", "Eski Paket Kuruyemiş -> 1102 Paket Kuruyemiş"),

            // 12 -> 1400 ATIŞTIRMALIK
            ("12", null,   "1400", null,   "Eski Atıştırmalık -> 1400 Atıştırmalık"),
            ("12", "1211", "1400", "1408", "Eski Bar Kaplamalılar -> 1408 Bar Kaplamalılar"),
            ("12", "1213", "1400", "1407", "Eski Bisküvi -> 1407 Bisküvi"),
            ("12", "1214", "1400", "1403", "Eski Cips -> 1403 Cips"),
            ("12", "1215", "1400", "1404", "Eski Çikolata -> 1404 Çikolata"),
            ("12", "1222", "1400", "1406", "Eski Gofret -> 1406 Gofret"),
            ("12", "1227", "1400", "1405", "Eski Kek -> 1405 Kek"),
            ("12", "1228", "1400", "1409", "Eski Kraker -> 1409 Kraker"),
            ("12", "1229", "600",  "604",  "Eski Pasta -> 604 Kuru Pasta"),
            ("12", "1232", "1400", "1410", "Eski Mısır Pirinç Patlağı -> 1410 Mısır ve Pirinç Patlağı"),
            ("12", "1233", "1400", "1401", "Eski Sakız -> 1401 Sakız"),
            ("12", "1234", "1400", "1402", "Eski Şekerleme -> 1402 Şekerleme"),

            // 13 -> 2200 MEYVE SEBZE
            ("13", null, "2200", null,   "Eski Sebze Meyve -> 2200 Meyve Sebze"),
            ("13", "01", "2200", "2201", "Eski Meyve -> 2201 Meyve"),
            ("13", "02", "2200", "2202", "Eski Sebze -> 2202 Sebze"),

            // 14 -> 1000 EV BAKIM
            ("14", null, "1000", null,   "Eski Deterjan Temizlik -> 1000 Ev Bakım"),
            ("14", "02", "1000", "1004", "Eski Yumuşatıcılar -> 1004 Çamaşır Yıkama"),
            ("14", "03", "1000", "1005", "Eski Bulaşık Deterjanı -> 1005 Bulaşık Yıkama"),
            ("14", "04", "1000", "1004", "Eski Çamaşır Deterjanı -> 1004 Çamaşır Yıkama"),
            ("14", "06", "1000", "1002", "Eski Yüzey Temizleyici -> 1002 Genel Temizlik"),
            ("14", "07", "1000", "1004", "Eski Çamaşır Deterjanı -> 1004 Çamaşır Yıkama"),
            ("14", "09", "900",  "906",  "Eski Bebek Çamaşır -> 906 Bebek Deterjan"),
            ("14", "10", "1000", "1002", "Eski Çamaşır Suları -> 1002 Genel Temizlik"),
            ("14", "11", "1000", "1003", "Eski Çöp Poşeti -> 1003 Çöp Poşeti"),
            ("14", "12", "1000", "1002", "Eski Genel Temizlik -> 1002 Genel Temizlik"),
            ("14", "13", "1000", "1001", "Eski Temizlik Malzemeleri -> 1001 Temizlik Malz."),

            // 15 -> 1500 KAĞIT ISLAK MENDİL
            ("15", null, "1500", "1501", "Eski Kağıt Islak Mendil -> 1501 Islak Mendil"),
            ("15", "01", "1500", "1503", "Eski Tuvalet Kağıtları -> 1503 Tuvalet Kağıdı"),
            ("15", "02", "1500", "1501", "Eski Islak Mendil -> 1501 Islak Mendil"),
            ("15", "03", "1500", "1504", "Eski Kağıt Mendil -> 1504 Kağıt Mendil"),
            ("15", "05", "1500", "1505", "Eski Peçeteler -> 1505 Peçete"),
            ("15", "06", "1500", "1502", "Eski Kağıt Havlu -> 1502 Kağıt Havlu"),

            // 16 -> 1600 KİŞİSEL BAKIM
            ("16", null, "1600", "1609", "Eski Kişisel Bakım -> 1609 Saç Bakım"),
            ("16", "01", "1600", "1611", "Eski Tıraş Malz -> 1611 Tıraş Malz"),
            ("16", "02", "1600", "1608", "Eski Parfüm Deodorant -> 1608 Parfüm Deodorant"),
            ("16", "03", "1600", "1606", "Eski Hijyenik Ped -> 1606 Hijyenik Ped"),
            ("16", "04", "1600", "1603", "Eski Kadın Makyaj -> 1603 Cilt Bakım"),
            ("16", "06", "1600", "1608", "Eski Kadın Parfüm -> 1608 Parfüm Deodorant"),
            ("16", "08", "1600", "1610", "Eski Sağlık Ürünleri -> 1610 Sağlık Ürünleri"),
            ("16", "09", "1900", "1902", "Eski Elektrikli Cihazlar (Trina) -> 1902 Elektrikli Ev Aletleri"),
            ("16", "10", "1600", "1609", "Eski Saç Bakım -> 1609 Saç Bakım"),
            ("16", "11", "1600", "1604", "Eski Duş Banyo Sabun -> 1604 Duş Banyo Sabun"),
            ("16", "12", "1600", "1604", "Eski Duş Jelleri -> 1604 Duş Banyo Sabun"),
            ("16", "13", "1600", "1603", "Eski Kremler -> 1603 Cilt Bakım"),
            ("16", "14", "1600", "1601", "Eski Ağda Epilasyon -> 1601 Ağda Epilasyon"),
            ("16", "15", "1600", "1602", "Eski Ağız Bakım -> 1602 Ağız Bakım"),
            ("16", "16", "1600", "1603", "Eski Cilt Bakım -> 1603 Cilt Bakım"),
            ("16", "17", "1600", "1605", "Eski Güneş Bakım -> 1605 Güneş Bakım"),
            ("16", "18", "1600", "1607", "Eski Kolonya -> 1607 Kolonya"),

            // 17 -> 1700 PET SHOP
            ("17", null, "1700", null,   "Eski Evcil Hayvan -> 1700 Pet Shop"),
            ("17", "1",  "1700", "1702", "Eski Kedi -> 1702 Kedi"),
            ("17", "2",  "1700", "1701", "Eski Köpek -> 1701 Köpek"),
            ("17", "3",  "1700", "1703", "Eski Kuş -> 1703 Kuş"),
            ("17", "4",  "1700", "1704", "Eski Pet Aksesuar -> 1704 Pet Aksesuar"),

            // 19 -> 900 BEBEK OYUNCAK
            ("19", null, "900", null,  "Eski Bebek ve Oyuncak -> 900 Bebek Oyuncak"),
            ("19", "1",  "900", "903", "Eski Bebek Bakım -> 903 Bebek Bakım"),
            ("19", "2",  "900", "904", "Eski Bebek Banyo -> 904 Bebek Banyo"),
            ("19", "3",  "900", "905", "Eski Bebek Beslenme -> 905 Bebek Beslenme"),
            ("19", "4",  "900", "902", "Eski Bebek Bezi -> 902 Bebek Bezi"),
            ("19", "5",  "900", "906", "Eski Bebek Deterjan -> 906 Deterjan ve Yumuşatıcı"),

            // 20, 21, 22, 31 -> 2000 EV YAŞAM
            ("20", null, "2000", "2007", "Eski Züccaciye -> 2007 Mutfak Eşyaları"),
            ("20", "1",  "2000", "2007", "Eski Züccaciye/1 -> 2007 Mutfak Eşyaları"),
            ("20", "10", "2000", "2007", "Eski Züccaciye/10 -> 2007 Mutfak Eşyaları"),
            ("21", null, "2000", "2007", "Eski Gıda Dışı Diğer -> 2007 Mutfak Eşyaları"),
            ("21", "10", "2000", "2005", "Eski Kırtasiye -> 2005 Kırtasiye"),
            ("21", "12", "2000", "2007", "Eski Ev Araç Gereçleri -> 2007 Mutfak Eşyaları"),
            ("21", "13", "1900", "1902", "Eski Elektrikli Ev Araçları -> 1902 Elektrikli Ev Aletleri"),
            ("21", "14", "2000", "2007", "Eski Mutfak Araç Gereçleri -> 2007 Mutfak Eşyaları"),
            ("22", null, "2000", "2003", "Eski Tekstil Mamulleri -> 2003 Giyim"),
            ("31", null, "1000", "1003", "Eski Ev Yaşam (Çöp Torbası) -> 1003 Çöp Poşeti"),
            ("31", "8",  "2000", "2008", "Eski Piknik Malzemeleri -> 2008 Piknik Malz."),

            // 26 -> 800 DONDURMA, 29 -> 1300 DONDURULMUŞ GIDA, 30 -> 1900 ELEKTRONİK
            ("26", null, "800",  null,   "Eski Dondurma -> 800 Dondurma"),
            ("26", "1",  "800",  "801",  "Eski Kap Dondurma -> 801 Kap Dondurma"),
            ("26", "2",  "800",  "802",  "Eski Tek Dondurma -> 802 Tek Dondurma"),
            ("800", "1", "800",  "801",  "800/1 veri kirliliği -> 801 Kap Dondurma"),
            ("29", null, "1300", null,   "Eski Dondurulmuş Gıda -> 1300 Dondurulmuş Gıda"),
            ("30", null, "1900", null,   "Eski Elektronik -> 1900 Elektronik"),
        };

        public static async Task SeedAsync(
            ECommerceDbContext context,
            IMikroGrupDbService? mikroGrupDbService = null,
            IMikroDbService? mikroDbService = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                Console.WriteLine("[CategorySeeder] 🔍 Mikro ERP kategori hiyerarşisi kontrol ediliyor...");

                // 1. "Diğer" kategorisini garanti et — eşlenemeyen ürünlerin güvenli limanı
                var digerCategory = await EnsureUncategorizedCategoryAsync(context, cancellationToken);

                // 2. 19 Yeni Düzen Ana Grubu ve 106 Alt Grubu (+ varsa Mikro'da yeni açılmış 3-4 haneli grupları) senkronize et
                var (mainByAnaKod, subByCompositeKey) = await SyncMikroCategoryTreeAsync(
                    context,
                    mikroGrupDbService,
                    cancellationToken);

                // 3. MikroCategoryMapping kayıtlarını (Yeni Düzen Alt Grup, Yeni Düzen Ana Grup, Eski Düzen -> Yeni Düzen) senkronize et
                await SyncCategoryMappingsAsync(
                    context,
                    mainByAnaKod,
                    subByCompositeKey,
                    digerCategory.Id,
                    cancellationToken);

                // 4. Mevcut ürünleri Canlı Mikro + MikroProductCache + Ürün Adı üzerinden doğru alt kategorilere ata
                //    ve yasaklı (02 Alkol / 03 Tütün) ürünleri pasife al
                await AssignProductsToMikroCategoriesAsync(
                    context,
                    mikroDbService,
                    mainByAnaKod,
                    subByCompositeKey,
                    digerCategory.Id,
                    cancellationToken);

                // 5. Yetim ürün ve alt kategori bütünlüğünü doğrula
                await FixOrphanProductsAsync(context, digerCategory.Id, cancellationToken);
                await FixOrphanSubcategoriesAsync(context, cancellationToken);

                Console.WriteLine("[CategorySeeder] ✅ Mikro ERP kategori ağacı + ürün eşlemesi tamamlandı.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CategorySeeder] ❌ Hata: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Doğrudan ana kategorilerde (ParentId == null) veya yanlış ana kategorilerde kalmış ürünleri,
        /// Mikro grup kodları ve ürün adlarına göre ilgili alt kategorilere dağıtır.
        /// </summary>
        public static async Task EnsureProductsClassifiedAsync(
            ECommerceDbContext context,
            IMikroDbService? mikroDbService = null,
            CancellationToken ct = default)
        {
            var allCategories = await context.Categories
                .AsNoTracking()
                .ToListAsync(ct);

            var mainByAnaKod = allCategories
                .Where(c => c.ParentId == null && !string.IsNullOrWhiteSpace(c.MikroAnaGrupKod))
                .GroupBy(c => c.MikroAnaGrupKod!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var subByCompositeKey = allCategories
                .Where(c => c.ParentId.HasValue && !string.IsNullOrWhiteSpace(c.MikroAnaGrupKod) && !string.IsNullOrWhiteSpace(c.MikroAltGrupKod))
                .GroupBy(c => $"{c.MikroAnaGrupKod}|{c.MikroAltGrupKod}", StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            if (subByCompositeKey.Count == 0)
                return;

            var digerId = allCategories.FirstOrDefault(c =>
                string.Equals(c.Slug, UncategorizedSlug, StringComparison.OrdinalIgnoreCase))?.Id ?? 0;

            await AssignProductsToMikroCategoriesAsync(
                context,
                mikroDbService,
                mainByAnaKod,
                subByCompositeKey,
                digerId,
                ct);
        }

        private static async Task<(Dictionary<string, Category> MainByAnaKod, Dictionary<string, Category> SubByCompositeKey)>
            SyncMikroCategoryTreeAsync(
                ECommerceDbContext context,
                IMikroGrupDbService? mikroGrupDbService,
                CancellationToken ct)
        {
            var now = DateTime.UtcNow;

            // Varsa canlı Mikro DB'den yeni düzen (3-4 haneli) grupları da oku
            var liveAnaMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var liveAltList = new List<(string AnaKod, string AltKod, string Isim)>();

            if (mikroGrupDbService is { IsConfigured: true })
            {
                try
                {
                    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    timeoutCts.CancelAfter(TimeSpan.FromSeconds(8));

                    var anaRes = await mikroGrupDbService.GetAnaGruplariAsync(timeoutCts.Token);
                    if (anaRes.Success)
                    {
                        foreach (var item in anaRes.Items)
                        {
                            var kod = MikroGrupKodHelper.NormalizeAnaKod(item.Kod);
                            if (MikroGrupKodHelper.IsYeniDuzen(kod) && !string.IsNullOrWhiteSpace(item.Isim))
                            {
                                liveAnaMap[kod] = item.Isim.Trim();
                            }
                        }
                    }

                    var altRes = await mikroGrupDbService.GetAltGruplariAsync(timeoutCts.Token);
                    if (altRes.Success)
                    {
                        foreach (var item in altRes.Items)
                        {
                            var anaKod = MikroGrupKodHelper.NormalizeAnaKod(item.AnaGrupKod);
                            var altKod = MikroGrupKodHelper.NormalizeAltKod(item.Kod);
                            if (MikroGrupKodHelper.IsYeniDuzen(anaKod) &&
                                !string.IsNullOrWhiteSpace(altKod) &&
                                !string.IsNullOrWhiteSpace(item.Isim))
                            {
                                liveAltList.Add((anaKod, altKod, item.Isim.Trim()));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CategorySeeder] ℹ️ Canlı Mikro grup okuma atlandı (yerel kanonik tablo kullanılacak): {ex.Message}");
                }
            }

            var existingCategories = await context.Categories.ToListAsync(ct);
            var usedSlugs = new HashSet<string>(
                existingCategories.Where(c => !string.IsNullOrWhiteSpace(c.Slug)).Select(c => c.Slug),
                StringComparer.OrdinalIgnoreCase);

            var mainByAnaKod = new Dictionary<string, Category>(StringComparer.OrdinalIgnoreCase);
            int mainCreated = 0, mainUpdated = 0;

            // A) Kanonik 19 Ana Grubu Upsert Et
            foreach (var def in CanonicalMainGroups)
            {
                var mikroIsim = liveAnaMap.TryGetValue(def.AnaKod, out var liveName) ? liveName : def.MikroIsim;

                var existing = existingCategories.FirstOrDefault(c =>
                    c.ParentId == null &&
                    !string.IsNullOrWhiteSpace(c.MikroAnaGrupKod) &&
                    string.Equals(c.MikroAnaGrupKod.Trim(), def.AnaKod, StringComparison.OrdinalIgnoreCase))
                    ?? existingCategories.FirstOrDefault(c =>
                        c.ParentId == null &&
                        string.Equals(c.Slug, def.Slug, StringComparison.OrdinalIgnoreCase));

                if (existing == null)
                {
                    var slug = EnsureUniqueSlug(def.Slug, usedSlugs);
                    existing = new Category
                    {
                        Name = def.DefaultDisplayName,
                        Description = def.Description,
                        Slug = slug,
                        ParentId = null,
                        SortOrder = def.SortOrder,
                        IsActive = true, // Yalnızca ilk oluşturmada true!
                        MikroAnaGrupKod = def.AnaKod,
                        MikroAltGrupKod = null,
                        MikroIsim = mikroIsim,
                        NavbarGroupKey = def.NavbarGroupKey,
                        IsMikroSynced = true,
                        LastMikroSyncAt = now,
                        CreatedAt = now,
                        UpdatedAt = now
                    };
                    context.Categories.Add(existing);
                    existingCategories.Add(existing);
                    mainCreated++;
                }
                else
                {
                    bool changed = false;
                    // DİKKAT: existing.IsActive ve existing.Name/ImageUrl admin tarafından değiştirilmiş olabilir, ASLA ezilmez!
                    if (string.IsNullOrWhiteSpace(existing.Name))
                    {
                        existing.Name = def.DefaultDisplayName;
                        changed = true;
                    }
                    if (string.IsNullOrWhiteSpace(existing.Description))
                    {
                        existing.Description = def.Description;
                        changed = true;
                    }
                    if (!string.Equals(existing.MikroAnaGrupKod, def.AnaKod, StringComparison.Ordinal))
                    {
                        existing.MikroAnaGrupKod = def.AnaKod;
                        changed = true;
                    }
                    if (existing.MikroAltGrupKod != null)
                    {
                        existing.MikroAltGrupKod = null;
                        changed = true;
                    }
                    if (!string.Equals(existing.MikroIsim, mikroIsim, StringComparison.Ordinal))
                    {
                        existing.MikroIsim = mikroIsim;
                        changed = true;
                    }
                    if (string.IsNullOrWhiteSpace(existing.NavbarGroupKey) && !string.IsNullOrWhiteSpace(def.NavbarGroupKey))
                    {
                        existing.NavbarGroupKey = def.NavbarGroupKey;
                        changed = true;
                    }
                    if (!existing.IsMikroSynced)
                    {
                        existing.IsMikroSynced = true;
                        existing.LastMikroSyncAt = now;
                        changed = true;
                    }
                    if (existing.SortOrder == 0)
                    {
                        existing.SortOrder = def.SortOrder;
                        changed = true;
                    }
                    if (changed)
                    {
                        existing.UpdatedAt = now;
                        mainUpdated++;
                    }
                }

                mainByAnaKod[def.AnaKod] = existing;
            }

            // A.2) Canlı Mikro'da 2300+ gibi yeni açılmış ana gruplar varsa onları da ekle
            int nextSortOrder = CanonicalMainGroups.Max(x => x.SortOrder) + 1;
            foreach (var (anaKod, mikroIsim) in liveAnaMap.OrderBy(x => x.Key))
            {
                if (mainByAnaKod.ContainsKey(anaKod))
                    continue;

                var existing = existingCategories.FirstOrDefault(c =>
                    c.ParentId == null &&
                    string.Equals(c.MikroAnaGrupKod, anaKod, StringComparison.OrdinalIgnoreCase));

                if (existing == null)
                {
                    var displayName = ToTurkishTitleCase(mikroIsim);
                    var baseSlug = Slugify(displayName);
                    var slug = EnsureUniqueSlug(string.IsNullOrWhiteSpace(baseSlug) ? $"grup-{anaKod}" : baseSlug, usedSlugs);

                    existing = new Category
                    {
                        Name = displayName,
                        Description = $"{displayName} ürünleri",
                        Slug = slug,
                        ParentId = null,
                        SortOrder = nextSortOrder++,
                        IsActive = true,
                        MikroAnaGrupKod = anaKod,
                        MikroAltGrupKod = null,
                        MikroIsim = mikroIsim,
                        IsMikroSynced = true,
                        LastMikroSyncAt = now,
                        CreatedAt = now,
                        UpdatedAt = now
                    };
                    context.Categories.Add(existing);
                    existingCategories.Add(existing);
                    mainCreated++;
                }

                mainByAnaKod[anaKod] = existing;
            }

            // Ana kategorilerin Id'lerinin oluşması için kaydet
            if (mainCreated > 0 || mainUpdated > 0)
            {
                await context.SaveChangesAsync(ct);
                Console.WriteLine($"[CategorySeeder] ✅ Ana kategoriler senkronize edildi. Yeni={mainCreated}, Güncel={mainUpdated}");
            }

            // B) Alt Grupları Upsert Et (Kanonik 106 alt grup + Canlı Mikro'daki yeni alt gruplar)
            var subByCompositeKey = new Dictionary<string, Category>(StringComparer.OrdinalIgnoreCase);
            int subCreated = 0, subUpdated = 0;

            var allSubDefs = new List<(string AnaKod, string AltKod, string MikroIsim, string DisplayName, int SortOrder)>(CanonicalSubGroups);
            var canonicalSubKeys = new HashSet<string>(
                CanonicalSubGroups.Select(s => $"{s.AnaKod}|{s.AltKod}"),
                StringComparer.OrdinalIgnoreCase);

            foreach (var liveAlt in liveAltList)
            {
                var key = $"{liveAlt.AnaKod}|{liveAlt.AltKod}";
                if (!canonicalSubKeys.Contains(key))
                {
                    allSubDefs.Add((liveAlt.AnaKod, liveAlt.AltKod, liveAlt.Isim, ToTurkishTitleCase(liveAlt.Isim), 99));
                    canonicalSubKeys.Add(key);
                }
            }

            foreach (var sub in allSubDefs)
            {
                if (!mainByAnaKod.TryGetValue(sub.AnaKod, out var parentCategory))
                    continue;

                var compositeKey = $"{sub.AnaKod}|{sub.AltKod}";
                var existingSub = existingCategories.FirstOrDefault(c =>
                    string.Equals(c.MikroAnaGrupKod, sub.AnaKod, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(c.MikroAltGrupKod, sub.AltKod, StringComparison.OrdinalIgnoreCase));

                var desiredBaseSlug = Slugify($"{parentCategory.Slug}-{sub.DisplayName}");
                if (string.IsNullOrWhiteSpace(desiredBaseSlug))
                    desiredBaseSlug = $"alt-{sub.AltKod}";

                // Eğer daha önce aynı parent altında aynı isimle oluşturulmuş alt kategori varsa onu bağla
                existingSub ??= existingCategories.FirstOrDefault(c =>
                    c.ParentId == parentCategory.Id &&
                    (string.Equals(c.Slug, desiredBaseSlug, StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(c.Name, sub.DisplayName, StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(c.Name, sub.MikroIsim, StringComparison.OrdinalIgnoreCase)));

                if (existingSub == null)
                {
                    var simpleSlug = Slugify(sub.DisplayName);
                    var candidateSlug = !usedSlugs.Contains(simpleSlug) && !string.IsNullOrWhiteSpace(simpleSlug)
                        ? simpleSlug
                        : desiredBaseSlug;
                    var finalSlug = EnsureUniqueSlug(candidateSlug, usedSlugs);

                    existingSub = new Category
                    {
                        Name = sub.DisplayName,
                        Description = $"{parentCategory.Name} - {sub.DisplayName}",
                        Slug = finalSlug,
                        ParentId = parentCategory.Id,
                        SortOrder = sub.SortOrder,
                        IsActive = true, // Yalnızca ilk oluşturmada true!
                        MikroAnaGrupKod = sub.AnaKod,
                        MikroAltGrupKod = sub.AltKod,
                        MikroIsim = sub.MikroIsim,
                        IsMikroSynced = true,
                        LastMikroSyncAt = now,
                        CreatedAt = now,
                        UpdatedAt = now
                    };
                    context.Categories.Add(existingSub);
                    existingCategories.Add(existingSub);
                    subCreated++;
                }
                else
                {
                    bool changed = false;
                    if (existingSub.ParentId != parentCategory.Id)
                    {
                        existingSub.ParentId = parentCategory.Id;
                        changed = true;
                    }
                    if (!string.Equals(existingSub.MikroAnaGrupKod, sub.AnaKod, StringComparison.Ordinal))
                    {
                        existingSub.MikroAnaGrupKod = sub.AnaKod;
                        changed = true;
                    }
                    if (!string.Equals(existingSub.MikroAltGrupKod, sub.AltKod, StringComparison.Ordinal))
                    {
                        existingSub.MikroAltGrupKod = sub.AltKod;
                        changed = true;
                    }
                    if (!string.Equals(existingSub.MikroIsim, sub.MikroIsim, StringComparison.Ordinal))
                    {
                        existingSub.MikroIsim = sub.MikroIsim;
                        changed = true;
                    }
                    if (!existingSub.IsMikroSynced)
                    {
                        existingSub.IsMikroSynced = true;
                        existingSub.LastMikroSyncAt = now;
                        changed = true;
                    }
                    if (existingSub.SortOrder == 0)
                    {
                        existingSub.SortOrder = sub.SortOrder;
                        changed = true;
                    }
                    if (changed)
                    {
                        existingSub.UpdatedAt = now;
                        subUpdated++;
                    }
                }

                subByCompositeKey[compositeKey] = existingSub;
            }

            if (subCreated > 0 || subUpdated > 0)
            {
                await context.SaveChangesAsync(ct);
                Console.WriteLine($"[CategorySeeder] ✅ Alt kategoriler senkronize edildi. Yeni={subCreated}, Güncel={subUpdated}");
            }

            return (mainByAnaKod, subByCompositeKey);
        }

        private static async Task SyncCategoryMappingsAsync(
            ECommerceDbContext context,
            IReadOnlyDictionary<string, Category> mainByAnaKod,
            IReadOnlyDictionary<string, Category> subByCompositeKey,
            int digerCategoryId,
            CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            var allMappings = await context.Set<MikroCategoryMapping>().ToListAsync(ct);

            var mappingLookup = allMappings
                .Where(m => m.MikroMarkaKod == null)
                .GroupBy(
                    m => $"{MikroGrupKodHelper.NormalizeAnaKod(m.MikroAnagrupKod)}|{MikroGrupKodHelper.NormalizeAltKod(m.MikroAltgrupKod)}",
                    StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.OrderBy(m => m.Id).First(), StringComparer.OrdinalIgnoreCase);

            int created = 0, updated = 0;

            void UpsertMapping(string anaKod, string? altKod, int targetCategoryId, int priority, string description)
            {
                var normAna = MikroGrupKodHelper.NormalizeAnaKod(anaKod);
                var normAlt = string.IsNullOrWhiteSpace(altKod) ? null : MikroGrupKodHelper.NormalizeAltKod(altKod);
                var key = $"{normAna}|{normAlt ?? string.Empty}";

                if (!mappingLookup.TryGetValue(key, out var existing))
                {
                    var newMapping = new MikroCategoryMapping
                    {
                        MikroAnagrupKod = normAna,
                        MikroAltgrupKod = normAlt,
                        MikroMarkaKod = null,
                        CategoryId = targetCategoryId,
                        Priority = priority,
                        IsActive = true,
                        MikroGrupAciklama = description,
                        Notes = "CategorySeeder Mikro hiyerarşi eşlemesi",
                        CreatedAt = now,
                        UpdatedAt = now
                    };
                    context.Set<MikroCategoryMapping>().Add(newMapping);
                    mappingLookup[key] = newMapping;
                    created++;
                }
                else
                {
                    bool changed = false;
                    if (existing.CategoryId != targetCategoryId)
                    {
                        existing.CategoryId = targetCategoryId;
                        changed = true;
                    }
                    if (existing.Priority != priority)
                    {
                        existing.Priority = priority;
                        changed = true;
                    }
                    if (!existing.IsActive)
                    {
                        existing.IsActive = true;
                        changed = true;
                    }
                    if (changed)
                    {
                        existing.UpdatedAt = now;
                        updated++;
                    }
                }
            }

            // 1) Yeni düzen Ana Gruplar: (AnaKod, null) -> MainCategory.Id (Priority = 10)
            foreach (var (anaKod, mainCat) in mainByAnaKod)
            {
                UpsertMapping(anaKod, null, mainCat.Id, 10, $"Mikro Ana Grup: {anaKod} - {mainCat.Name}");
            }

            // 2) Yeni düzen Alt Gruplar: (AnaKod, AltKod) -> SubCategory.Id (Priority = 100)
            foreach (var (compositeKey, subCat) in subByCompositeKey)
            {
                var parts = compositeKey.Split('|');
                if (parts.Length == 2)
                {
                    UpsertMapping(parts[0], parts[1], subCat.Id, 100, $"Mikro Alt Grup: {parts[0]}/{parts[1]} - {subCat.Name}");
                }
            }

            // 3) Eski düzen (1-2 haneli) -> Yeni düzen hedef kategori eşlemeleri
            foreach (var leg in LegacyToNewMappings)
            {
                Category? targetCat = null;
                if (!string.IsNullOrWhiteSpace(leg.TargetAlt))
                {
                    subByCompositeKey.TryGetValue($"{leg.TargetAna}|{leg.TargetAlt}", out targetCat);
                }
                if (targetCat == null)
                {
                    mainByAnaKod.TryGetValue(leg.TargetAna, out targetCat);
                }

                if (targetCat != null)
                {
                    int priority = string.IsNullOrWhiteSpace(leg.OldAlt) ? 5 : 50;
                    UpsertMapping(leg.OldAna, leg.OldAlt, targetCat.Id, priority, leg.Description);
                }
            }

            // 4) Wildcard (*) -> Diğer
            await EnsureWildcardMappingAsync(context, digerCategoryId, ct);

            if (created > 0 || updated > 0)
            {
                await context.SaveChangesAsync(ct);
                Console.WriteLine($"[CategorySeeder] ✅ MikroCategoryMapping güncellendi. Yeni={created}, Güncel={updated}");
            }
        }

        private static async Task AssignProductsToMikroCategoriesAsync(
            ECommerceDbContext context,
            IMikroDbService? mikroDbService,
            IReadOnlyDictionary<string, Category> mainByAnaKod,
            IReadOnlyDictionary<string, Category> subByCompositeKey,
            int digerCategoryId,
            CancellationToken ct)
        {
            var products = await context.Products.ToListAsync(ct);

            // 1) Canlı Mikro DB'den (varsa) SKU -> (AnagrupKod, GrupKod, StokAd, Fiyat, StokMiktar, WebAktif) haritası oluştur
            var mikroBySku = new Dictionary<string, (string? AnaKod, string? AltKod, string? StokAd, decimal Fiyat, int StokMiktar, bool WebAktif)>(StringComparer.OrdinalIgnoreCase);
            if (mikroDbService is { IsConfigured: true })
            {
                try
                {
                    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    timeoutCts.CancelAfter(TimeSpan.FromSeconds(12));
                    var unified = await mikroDbService.GetUnifiedProductsAsync(null, null, timeoutCts.Token);
                    foreach (var u in unified)
                    {
                        if (!string.IsNullOrWhiteSpace(u.StokKod))
                        {
                            mikroBySku[u.StokKod.Trim()] = (
                                u.AnagrupKod,
                                u.GrupKod,
                                u.StokAd,
                                u.Fiyat,
                                (int)Math.Max(0, u.StokMiktar),
                                u.WebeGonderilecekFl);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CategorySeeder] ℹ️ Canlı Mikro ürün grup okuma atlandı: {ex.Message}");
                }
            }

            if (products.Count == 0 && mikroBySku.Count == 0)
                return;

            // 2) MikroProductCaches tablosundan hem LocalProductId hem StokKod (SKU) üzerinden harita oluştur
            var cacheRows = await context.MikroProductCaches
                .AsNoTracking()
                .Select(c => new
                {
                    c.LocalProductId,
                    c.StokKod,
                    c.StokAd,
                    c.AnagrupKod,
                    c.GrupKod
                })
                .ToListAsync(ct);

            var cacheByProductId = new Dictionary<int, (string? AnaKod, string? AltKod, string? StokAd)>();
            foreach (var c in cacheRows)
            {
                if (c.LocalProductId.HasValue && !cacheByProductId.ContainsKey(c.LocalProductId.Value))
                {
                    cacheByProductId[c.LocalProductId.Value] = (c.AnagrupKod, c.GrupKod, c.StokAd);
                }
                if (!string.IsNullOrWhiteSpace(c.StokKod) && !mikroBySku.ContainsKey(c.StokKod.Trim()))
                {
                    mikroBySku[c.StokKod.Trim()] = (c.AnagrupKod, c.GrupKod, c.StokAd, 0m, 0, true);
                }
            }

            // 3) Eski -> Yeni lookup sözlüğü
            var legacyLookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var leg in LegacyToNewMappings)
            {
                Category? targetCat = null;
                if (!string.IsNullOrWhiteSpace(leg.TargetAlt))
                {
                    subByCompositeKey.TryGetValue($"{leg.TargetAna}|{leg.TargetAlt}", out targetCat);
                }
                if (targetCat == null)
                {
                    mainByAnaKod.TryGetValue(leg.TargetAna, out targetCat);
                }
                if (targetCat != null)
                {
                    var key = $"{MikroGrupKodHelper.NormalizeAnaKod(leg.OldAna)}|{MikroGrupKodHelper.NormalizeAltKod(leg.OldAlt)}";
                    legacyLookup[key] = targetCat.Id;
                }
            }

            var categoryById = await context.Categories
                .AsNoTracking()
                .ToDictionaryAsync(c => c.Id, ct);

            var mainCategoryIds = new HashSet<int>(mainByAnaKod.Values.Select(c => c.Id));
            var subCategoryIds = new HashSet<int>(subByCompositeKey.Values.Select(c => c.Id));

            var now = DateTime.UtcNow;
            int movedCount = 0;
            int createdFromMikroCount = 0;
            int forbiddenDeactivatedCount = 0;
            var existingSkus = new HashSet<string>(
                products
                    .Where(p => !string.IsNullOrWhiteSpace(p.SKU))
                    .Select(p => p.SKU!.Trim()),
                StringComparer.OrdinalIgnoreCase);

            foreach (var product in products)
            {
                string? rawAnaKod = null;
                string? rawAltKod = null;
                string? stokAd = product.Name;

                if (!string.IsNullOrWhiteSpace(product.SKU) &&
                    mikroBySku.TryGetValue(product.SKU.Trim(), out var skuInfo))
                {
                    rawAnaKod = skuInfo.AnaKod;
                    rawAltKod = skuInfo.AltKod;
                    if (!string.IsNullOrWhiteSpace(skuInfo.StokAd))
                        stokAd = skuInfo.StokAd;

                    if (product.Price <= 0 && skuInfo.Fiyat > 0)
                    {
                        product.Price = skuInfo.Fiyat;
                        product.UpdatedAt = now;
                    }
                    if (product.StockQuantity <= 0 && skuInfo.StokMiktar > 0)
                    {
                        product.StockQuantity = skuInfo.StokMiktar;
                        product.UpdatedAt = now;
                    }
                }
                else if (cacheByProductId.TryGetValue(product.Id, out var idInfo))
                {
                    rawAnaKod = idInfo.AnaKod;
                    rawAltKod = idInfo.AltKod;
                    if (!string.IsNullOrWhiteSpace(idInfo.StokAd))
                        stokAd = idInfo.StokAd;
                }

                var anaKod = MikroGrupKodHelper.NormalizeAnaKod(rawAnaKod);
                var altKod = MikroGrupKodHelper.NormalizeAltKod(rawAltKod);

                // Yasaklı grup kontrolü (02 Alkol, 03 Tütün)
                if (!string.IsNullOrWhiteSpace(anaKod) && ForbiddenAnaGrupKodlari.Contains(anaKod))
                {
                    if (product.IsActive || product.CategoryId != digerCategoryId)
                    {
                        product.IsActive = false;
                        product.CategoryId = digerCategoryId;
                        product.UpdatedAt = now;
                        forbiddenDeactivatedCount++;
                    }
                    continue;
                }

                int? explicitOverrideId = ResolveExplicitKeywordOverride(stokAd ?? product.Name, subByCompositeKey);

                int? resolvedCategoryId = explicitOverrideId ?? ResolveProductCategoryId(
                    anaKod,
                    altKod,
                    stokAd ?? product.Name,
                    mainByAnaKod,
                    subByCompositeKey,
                    legacyLookup);

                // Eğer ürünün Mikro alt grup kodu boşsa veya hâlâ bir Ana Kategorideyse,
                // ürün adından alt kategorisini çözümle (Yumurta -> 509, Zeytin -> 510, Şarküteri -> 503 vb.)
                if (explicitOverrideId.HasValue ||
                    (product.AdminOverrideCategory != true &&
                     (string.IsNullOrWhiteSpace(altKod) || !resolvedCategoryId.HasValue || mainCategoryIds.Contains(resolvedCategoryId.Value))))
                {
                    string? effectiveMainAnaKod = null;
                    if (resolvedCategoryId.HasValue &&
                        categoryById.TryGetValue(resolvedCategoryId.Value, out var resolvedCat) &&
                        !string.IsNullOrWhiteSpace(resolvedCat.MikroAnaGrupKod))
                    {
                        effectiveMainAnaKod = resolvedCat.MikroAnaGrupKod;
                    }
                    else if (categoryById.TryGetValue(product.CategoryId, out var currentCat))
                    {
                        effectiveMainAnaKod = currentCat.MikroAnaGrupKod;
                        if (string.IsNullOrWhiteSpace(effectiveMainAnaKod) &&
                            currentCat.ParentId.HasValue &&
                            categoryById.TryGetValue(currentCat.ParentId.Value, out var parentCat))
                        {
                            effectiveMainAnaKod = parentCat.MikroAnaGrupKod;
                        }
                    }

                    // Eğer ürün zaten geçerli bir alt kategorideyse ve çapraz grup düzeltmesi gerekmiyorsa mevcut alt kategoriyi koru
                    bool alreadyInValidSub = subCategoryIds.Contains(product.CategoryId);

                    var subFromName = ResolveSubCategoryByName(
                        effectiveMainAnaKod,
                        stokAd ?? product.Name,
                        subByCompositeKey,
                        allowDefaultFallback: !alreadyInValidSub);

                    if (subFromName.HasValue)
                    {
                        resolvedCategoryId = subFromName.Value;
                    }
                    else if (alreadyInValidSub)
                    {
                        resolvedCategoryId = product.CategoryId;
                    }
                }

                if (resolvedCategoryId.HasValue && product.CategoryId != resolvedCategoryId.Value)
                {
                    product.CategoryId = resolvedCategoryId.Value;
                    product.UpdatedAt = now;
                    movedCount++;
                }

                // Konserve, donuk/dondurulmuş veya paketli/adetli ürünlerde eski IsWeightBased=true bayrağını temizle
                categoryById.TryGetValue(product.CategoryId, out var finalCat);
                if (product.IsWeightBased &&
                    (ECommerce.Business.Helpers.WeightBasedProductRules.HasPackagedNameSignal(stokAd ?? product.Name) ||
                     ECommerce.Business.Helpers.WeightBasedProductRules.IsPackagedOrExcludedCategory(
                         finalCat?.Name,
                         finalCat?.Slug,
                         finalCat?.MikroAltGrupKod,
                         finalCat?.MikroAnaGrupKod)))
                {
                    product.IsWeightBased = false;
                    product.UpdatedAt = now;
                    movedCount++;
                }
            }

            // Canlı Mikro ERP'de olup henüz yerel Products tablosunda bulunmayan web-aktif ürünleri de alt kategorileriyle oluştur
            foreach (var kvp in mikroBySku)
            {
                var sku = kvp.Key.Trim();
                var info = kvp.Value;
                if (string.IsNullOrWhiteSpace(sku) || existingSkus.Contains(sku))
                    continue;

                if (!info.WebAktif || info.Fiyat <= 0 || info.StokMiktar <= 0 || string.IsNullOrWhiteSpace(info.StokAd))
                    continue;

                var anaKod = MikroGrupKodHelper.NormalizeAnaKod(info.AnaKod);
                var altKod = MikroGrupKodHelper.NormalizeAltKod(info.AltKod);

                if (!string.IsNullOrWhiteSpace(anaKod) && ForbiddenAnaGrupKodlari.Contains(anaKod))
                    continue;

                int? explicitOverrideId = ResolveExplicitKeywordOverride(info.StokAd, subByCompositeKey);

                int? resolvedCategoryId = explicitOverrideId ?? ResolveProductCategoryId(
                    anaKod,
                    altKod,
                    info.StokAd,
                    mainByAnaKod,
                    subByCompositeKey,
                    legacyLookup);

                if (string.IsNullOrWhiteSpace(altKod) || !resolvedCategoryId.HasValue || mainCategoryIds.Contains(resolvedCategoryId.Value))
                {
                    string? effectiveMainAnaKod = null;
                    if (resolvedCategoryId.HasValue &&
                        categoryById.TryGetValue(resolvedCategoryId.Value, out var resolvedCat) &&
                        !string.IsNullOrWhiteSpace(resolvedCat.MikroAnaGrupKod))
                    {
                        effectiveMainAnaKod = resolvedCat.MikroAnaGrupKod;
                    }

                    var subFromName = ResolveSubCategoryByName(
                        effectiveMainAnaKod,
                        info.StokAd,
                        subByCompositeKey,
                        allowDefaultFallback: true);

                    if (subFromName.HasValue)
                    {
                        resolvedCategoryId = subFromName.Value;
                    }
                }

                var finalCatId = resolvedCategoryId ?? digerCategoryId;
                if (finalCatId <= 0)
                    continue;

                context.Products.Add(new Product
                {
                    SKU = sku,
                    Name = info.StokAd.Trim(),
                    Description = info.StokAd.Trim(),
                    Price = info.Fiyat,
                    StockQuantity = info.StokMiktar,
                    CategoryId = finalCatId,
                    IsActive = true,
                    ImageUrl = "/images/default-product.png",
                    CreatedAt = now,
                    UpdatedAt = now
                });

                existingSkus.Add(sku);
                createdFromMikroCount++;
            }

            if (movedCount > 0 || createdFromMikroCount > 0 || forbiddenDeactivatedCount > 0)
            {
                await context.SaveChangesAsync(ct);
                Console.WriteLine(
                    $"[CategorySeeder] 🔄 {movedCount} ürün Mikro alt/ana kategorisine atandı, {createdFromMikroCount} Mikro ürünü eklendi, {forbiddenDeactivatedCount} yasaklı grup ürünü pasife alındı.");
            }
        }

        private static int? ResolveProductCategoryId(
            string anaKod,
            string altKod,
            string? productName,
            IReadOnlyDictionary<string, Category> mainByAnaKod,
            IReadOnlyDictionary<string, Category> subByCompositeKey,
            IReadOnlyDictionary<string, int> legacyLookup)
        {
            // 1) Yeni düzen (AnaKod, AltKod) tam eşleşme
            if (!string.IsNullOrWhiteSpace(anaKod) && !string.IsNullOrWhiteSpace(altKod))
            {
                if (subByCompositeKey.TryGetValue($"{anaKod}|{altKod}", out var subCat))
                    return subCat.Id;

                if (legacyLookup.TryGetValue($"{anaKod}|{altKod}", out var legacySubId))
                    return legacySubId;
            }

            // 2) Eski düzen 20/21/31 gibi torba gruplarda ürün adına göre spesifik alt kategori iyileştirmesi
            if (anaKod is "20" or "21" or "31" && string.IsNullOrWhiteSpace(altKod) && !string.IsNullOrWhiteSpace(productName))
            {
                var subByName = ResolveSubCategoryByName(null, productName, subByCompositeKey);
                if (subByName.HasValue)
                    return subByName.Value;
            }

            // 3) Sadece AnaKod üzerinden yeni düzen veya eski düzen eşleşmesi
            if (!string.IsNullOrWhiteSpace(anaKod))
            {
                if (legacyLookup.TryGetValue($"{anaKod}|", out var legacyMainId))
                    return legacyMainId;

                if (mainByAnaKod.TryGetValue(anaKod, out var mainCat))
                    return mainCat.Id;
            }

            return null;
        }

        /// <summary>
        /// Mikro ERP veya legacy eşleştirmelerden bağımsız olarak, ürün adından kesin olarak bilinen
        /// kategori atamalarını (örn. Calippo/Cornetto/Magnum -> Dondurma, Kuru Et/Pastırma -> Et Şarküteri,
        /// Kekik/Tarçın -> Baharat, Çiğ Köfte -> Meze, Mantı -> Dondurulmuş, Choco Snack -> Sütlü Tatlı) yapar.
        /// </summary>
        public static int? ResolveExplicitKeywordOverride(
            string? productName,
            IReadOnlyDictionary<string, Category> subByCompositeKey)
        {
            if (string.IsNullOrWhiteSpace(productName))
                return null;

            var upper = productName.Trim().ToUpper(TrCulture);

            int? TryGetSub(string ana, string alt)
                => subByCompositeKey.TryGetValue($"{ana}|{alt}", out var c) ? c.Id : null;

            // 1) Şarküteri: Kuru et ve Pastırma (asla kırmızı ette kalmamalı)
            if (ContainsAny(upper, "PASTIRMA", "KURU ET", "KURUET", "DANA KURU ET"))
                return TryGetSub("500", "503");

            // 2) Dondurma: Calippo, Cornetto, Magnum, Algida (asla meyve veya çikolatada kalmamalı)
            if (ContainsAny(upper, "CALIPPO", "CORNETTO", "CORNET", "MAGNUM") ||
                (ContainsAny(upper, "ALGİDA", "ALGIDA") && !ContainsAny(upper, "SOS", "BARDAK")))
            {
                return upper.Contains("ML") || upper.Contains("KAP") || upper.Contains("CARTE") || upper.Contains("MARAŞ")
                    ? (TryGetSub("800", "801") ?? TryGetSub("800", "802"))
                    : (TryGetSub("800", "802") ?? TryGetSub("800", "801"));
            }

            // 3) Sütlü Tatlı / Krema: Milka Choco Snack (asla çikolatada kalmamalı)
            if (ContainsAny(upper, "CHOCO SNACK"))
                return TryGetSub("500", "505");

            // 4) Dondurulmuş Ürün: Mantı ve Hıngel (asla makarna / temel gıdada kalmamalı)
            if (ContainsAny(upper, "MANTI", "HINGEL", "HİNGEL") && !ContainsAny(upper, "MANTAR"))
                return TryGetSub("1300", "1306") ?? TryGetSub("1300", "1305");

            // 5) Hazır Yemek / Meze: Çiğ Köfte (asla kahvaltılıkta kalmamalı)
            if (ContainsAny(upper, "ÇİĞ KÖFTE", "CIG KOFTE", "ÇİĞKÖFTE", "CIGKOFTE") &&
                !ContainsAny(upper, "BULGUR", "HARÇ", "HARCI", "SETSİZ", "BAHARAT"))
                return TryGetSub("1200", "1201");

            // 6) Fırın & Pastane: Alaçatı Kurabiye & Damla Sakızlı Kurabiye (asla sakızda kalmamalı)
            if ((ContainsAny(upper, "ALAÇATI", "ALACATI") && ContainsAny(upper, "KURABİYE", "KURABIYE", "GEVREK", "ÇITIR TOP", "CITIR TOP")) ||
                ContainsAny(upper, "DAMLA SAKIZLI KURABİYE", "DAMLA SAKIZLI KURABIYE"))
                return TryGetSub("600", "604");

            // 7) Fırın & Pastane: İzmir Bombası (asla çikolatada kalmamalı)
            if (ContainsAny(upper, "İZMİR BOMBASI", "IZMIR BOMBASI", "BOMBA KURABİYE") ||
                (ContainsAny(upper, "BOMBA") && ContainsAny(upper, "TATLI", "PASTA", "KURABİYE", "ÇİKOLATALI")))
                return TryGetSub("600", "606") ?? TryGetSub("600", "604");

            // 8) Baharat: Arifoğlu Kekik ve Arifoğlu Çubuk Tarçın (asla kek veya krakerde kalmamalı)
            if ((ContainsAny(upper, "KEKİK", "KEKIK") && !ContainsAny(upper, "ÇAY", "CAY")) ||
                ContainsAny(upper, "ÇUBUK TARÇIN", "CUBUK TARCIN", "TOZ TARÇIN", "TOZ TARCIN"))
            {
                if (!ContainsAny(upper, "DIFFUSER", "DİFÜZÖR", "ODA KOKUSU", "PARFÜM", "KOLONYA"))
                    return TryGetSub("700", "708");
            }

            // 9) Tereyağı: Silivri Aslan Tereyağ (asla margarinde kalmamalı)
            if (ContainsAny(upper, "TEREYAĞ", "TEREYAG", "SADE YAĞ", "SADEYAĞ", "SADEYAG") &&
                !ContainsAny(upper, "BİSKÜVİ", "BISKUVI", "KURABİYE", "KRUVASAN", "CİPS"))
                return TryGetSub("500", "508");

            // 10) Gazsız İçecek: Ben Organic portakal suyu / meyve suları
            if (ContainsAny(upper, "BEN ORGANIC", "BEN ORGANİK"))
                return TryGetSub("100", "103");

            return null;
        }

        /// <summary>
        /// Alt grup kodu boş olan veya doğrudan ana kategoriye atanmış ürünleri,
        /// ürün adına göre ilgili Mikro alt kategorisine (örn. Yumurta -> 509, Süt -> 501, Peynir -> 502) yerleştirir.
        /// </summary>
        public static int? ResolveSubCategoryByName(
            string? mainAnaKod,
            string? productName,
            IReadOnlyDictionary<string, Category> subByCompositeKey,
            bool allowDefaultFallback = true)
        {
            if (string.IsNullOrWhiteSpace(productName))
                return null;

            // Önce kesin anahtar kelime eşleşmesini kontrol et
            var explicitMatch = ResolveExplicitKeywordOverride(productName, subByCompositeKey);
            if (explicitMatch.HasValue)
                return explicitMatch.Value;

            var upper = productName.Trim().ToUpper(TrCulture);

            int? TryGetSub(string ana, string alt)
                => subByCompositeKey.TryGetValue($"{ana}|{alt}", out var c) ? c.Id : null;

            // 0) Çapraz kategori düzeltmeleri: Eskiden Temel Gıda (700) veya Et (400) altında kalmış
            //    Yumurta (509), Zeytin (510), Et Şarküteri (503), Kahvaltılık (506), Tereyağı (508), Margarin (507)
            //    ürünlerini hangi ana grupta olursa olsun doğrudan 500 (Süt & Kahvaltılık) alt gruplarına yönlendir.
            if (ContainsAny(upper, "YUMURTA", "BILDIRCIN YUMURTA", "GEZEN TAVUK YUMURTA") &&
                !ContainsAny(upper, "KINDER", "OZMO", "SÜRPRİZ", "SURPRIZ", "ÇİKOLATA", "CIKOLATA", "OYUNCAK"))
            {
                return TryGetSub("500", "509");
            }

            if (ContainsAny(upper, "ZEYTİN", "ZEYTIN") &&
                !ContainsAny(upper, "ZEYTİNYAĞ", "ZEYTINYAG", "SIZMA", "RİVİERA", "RIVIERA", "SABUN", "ŞAMPUAN", "SAMPUAN", "DUŞ", "DUS ", "KREM", "KOLONYA", "LOSYON", "MENDİL", "MENDIL"))
            {
                return TryGetSub("500", "510");
            }

            if (!string.Equals(mainAnaKod, "1300", StringComparison.OrdinalIgnoreCase) &&
                ContainsAny(upper, "SUCUK", "SALAM", "SOSİS", "SOSIS", "PASTIRMA", "JAMBON", "KAVURMA", "FÜME ET", "FUME ET", "DANA FÜME", "HİNDİ FÜME", "PİLİÇ FÜME", "ŞARKÜTERİ", "SARKUTERI"))
            {
                return TryGetSub("500", "503");
            }

            if (ContainsAny(upper, "TEREYAĞ", "TEREYAG", "SADE YAĞ", "SADEYAĞ", "SADEYAG") &&
                !ContainsAny(upper, "BİSKÜVİ", "BISKUVI", "KURABİYE", "KRUVASAN", "CİPS"))
            {
                return TryGetSub("500", "508");
            }

            if (ContainsAny(upper, "MARGARİN", "MARGARIN", "BECEL", "BİZİM YAĞ", "BIZIM YAG", "TEREMYAĞ", "TEREMYAG"))
            {
                return TryGetSub("500", "507");
            }

            if (ContainsAny(upper, "REÇEL", "RECEL", "MARMELAT", "ÇİÇEK BALI", "ÇAM BALI", "PETEK BAL", "SÜZME BAL", "KARAKOVAN", "KESTANE BALI", "PEKMEZ", "TAHİN", "TAHIN", "HELVA", "FINDIK EZME", "FISTIK EZME", "NUTELLA", "ÇOKOKREM", "COKOKREM", "SARELLE", "FISKOBIRLIK", "FİSKOBİRLİK", "TORKU BANADA", "MÜSLİ", "MUSLI", "GRANOLA", "CORN FLAKES", "KAHVALTILIK GEVREK", "NESFIT"))
            {
                return TryGetSub("500", "506");
            }

            // 1) Önce kendi ana kategorisi içinde eşleştirmeyi dene
            var matchedInMain = ResolveSubCategoryByNameCore(mainAnaKod, upper, subByCompositeKey);
            if (matchedInMain.HasValue)
                return matchedInMain.Value;

            // 2) Kendi ana kategorisinde eşleşmediyse tüm kategorilerde anahtar kelime ara
            if (!string.IsNullOrWhiteSpace(mainAnaKod))
            {
                var matchedGlobal = ResolveSubCategoryByNameCore(null, upper, subByCompositeKey);
                if (matchedGlobal.HasValue)
                    return matchedGlobal.Value;
            }

            // 3) Hâlâ eşleşmediyse ve ürün doğrudan bir ana kategorideyse, o ana kategorinin varsayılan alt kategorisine ata
            if (allowDefaultFallback && !string.IsNullOrWhiteSpace(mainAnaKod))
            {
                return mainAnaKod.Trim() switch
                {
                    "500"  => TryGetSub("500", "506") ?? TryGetSub("500", "501"),
                    "400"  => TryGetSub("400", "402") ?? TryGetSub("400", "401"),
                    "2200" => TryGetSub("2200", "2202") ?? TryGetSub("2200", "2201"),
                    "100"  => TryGetSub("100", "103") ?? TryGetSub("100", "106"),
                    "1400" => TryGetSub("1400", "1407") ?? TryGetSub("1400", "1404"),
                    "1100" => TryGetSub("1100", "1102") ?? TryGetSub("1100", "1101"),
                    "1300" => TryGetSub("1300", "1306") ?? TryGetSub("1300", "1305"),
                    "800"  => TryGetSub("800", "802") ?? TryGetSub("800", "801"),
                    "1000" => TryGetSub("1000", "1002") ?? TryGetSub("1000", "1001"),
                    "1500" => TryGetSub("1500", "1501") ?? TryGetSub("1500", "1505"),
                    "700"  => TryGetSub("700", "708") ?? TryGetSub("700", "701"),
                    "2000" => TryGetSub("2000", "2007") ?? TryGetSub("2000", "2002"),
                    "600"  => TryGetSub("600", "604") ?? TryGetSub("600", "601"),
                    "1200" => TryGetSub("1200", "1202") ?? TryGetSub("1200", "1201"),
                    "1600" => TryGetSub("1600", "1603") ?? TryGetSub("1600", "1604"),
                    "900"  => TryGetSub("900", "903") ?? TryGetSub("900", "901"),
                    "1700" => TryGetSub("1700", "1704") ?? TryGetSub("1700", "1702"),
                    "1900" => TryGetSub("1900", "1901") ?? TryGetSub("1900", "1902"),
                    "2100" => TryGetSub("2100", "2101"),
                    _      => null
                };
            }

            return null;
        }

        private static int? ResolveSubCategoryByNameCore(
            string? mainAnaKod,
            string upper,
            IReadOnlyDictionary<string, Category> subByCompositeKey)
        {
            int? TryGetSub(string ana, string alt)
                => subByCompositeKey.TryGetValue($"{ana}|{alt}", out var c) ? c.Id : null;

            bool MatchesMain(params string[] allowedMainCodes)
                => string.IsNullOrWhiteSpace(mainAnaKod) ||
                   allowedMainCodes.Any(code => string.Equals(code, mainAnaKod, StringComparison.OrdinalIgnoreCase));

            // --- 500: SÜT & KAHVALTILIK ---
            if (MatchesMain("500"))
            {
                if (ContainsAny(upper, "YUMURTA", "BILDIRCIN YUMURTA", "GEZEN TAVUK"))
                    return TryGetSub("500", "509");
                if (ContainsAny(upper, "ZEYTİN", "ZEYTIN"))
                    return TryGetSub("500", "510");
                if (ContainsAny(upper, "TEREYAĞ", "TEREYAG", "SADE YAĞ", "SADEYAĞ"))
                    return TryGetSub("500", "508");
                if (ContainsAny(upper, "MARGARİN", "MARGARIN", "SANA ", "BECEL", "BİZİM YAĞ", "BIZIM YAG", "TEREMYAĞ"))
                    return TryGetSub("500", "507");
                if (ContainsAny(upper, "SUCUK", "SALAM", "SOSİS", "SOSIS", "PASTIRMA", "JAMBON", "KAVURMA", "FÜME ET", "FUME ET", "DANA FÜME", "HİNDİ FÜME", "ŞARKÜTERİ"))
                    return TryGetSub("500", "503");
                if (ContainsAny(upper, "YOĞURT", "YOGURT", "AYRAN", "KEFİR", "KEFIR", "ACTİVİA", "ACTIVIA", "DANONİNO", "DANONINO"))
                    return TryGetSub("500", "504");
                if (ContainsAny(upper, "KREMA", "KAYMAK", "PUDİNG", "PUDING", "SÜTLAÇ", "SUTLAC", "KAZANDİBİ", "SUPANGLE", "PROFİTEROL", "MAGNOLIA", "TRİLEÇE", "KEŞKÜL", "MUHALLEBİ", "DANETTE"))
                    return TryGetSub("500", "505");
                if (ContainsAny(upper, "PEYNİR", "PEYNIR", "KAŞAR", "KASAR", "LABNE", "LOR ", "LORU", "TULUM", "MOZZARELLA", "HELLİM", "HELLIM", "BURGU", "ÇEÇİL", "CECIL", "EZİNE", "EZINE", "ÖRGÜ", "KREM PEY", "ÜÇGEN PEY", "SÜZME PEY", "BEYAZ PEY", "TOST PEY", "DİL PEY", "ÇERKES", "PARMESAN", "CHEDDAR", "GOUDA", "ROKFOR", "MASCARPO", "RICOTTA", "ÇÖKELEK", "COKELEK", "KARPER"))
                    return TryGetSub("500", "502");
                if (ContainsAny(upper, "REÇEL", "RECEL", "MARMELAT", "BAL ", "BALI", "ÇİÇEK BALI", "ÇAM BALI", "PETEK BAL", "PEKMEZ", "TAHİN", "TAHIN", "HELVA", "FINDIK EZME", "FISTIK EZME", "NUTELLA", "ÇOKOKREM", "SARELLE", "FISKOBIRLIK", "MÜSLİ", "MUSLI", "GRANOLA", "GEVREK", "CORN FLAKES", "YULAF EZME", "COCO POPS", "NESFIT"))
                    return TryGetSub("500", "506");
                if (ContainsAny(upper, "SÜT", "SUT ", "SÜTÜ", "SUTU", "LAKTOZSUZ", "DEVAM SÜTÜ"))
                    return TryGetSub("500", "501");
            }

            // --- 1400 / 1100: ATIŞTIRMALIK & KURUYEMİŞ ---
            if (MatchesMain("1400", "1100"))
            {
                if (ContainsAny(upper, "SAKIZ", "FALIM", "VIVIDENT", "FIRST ", "MENTOS"))
                    return TryGetSub("1400", "1401");
                if (ContainsAny(upper, "JELİBON", "JELIBON", "HARIBO", "ŞEKERLEME", "SEKERLEME", "LOLİPOP", "BONBON", "LOKUM", "DRAJE", "YUPO", "BEBETO"))
                    return TryGetSub("1400", "1402");
                if (ContainsAny(upper, "CİPS", "CIPS", "DORITOS", "RUFFLES", "LAYS", "LAY'S", "PRINGLES", "CHEETOS", "ÇEREZYA", "SAMBA", "PATOS", "CEREZ"))
                    return TryGetSub("1400", "1403");
                if (ContainsAny(upper, "GOFRET", "HOŞBEŞ", "HOSBES", "DİDO", "DIDO", "ÇOKOPRENS"))
                    return TryGetSub("1400", "1406");
                if (ContainsAny(upper, "BROWNİ", "BROWNI", "SUFLE", "KRUVASAN", "HALLEY", "LUPPO", "DANKek", "DANKEK", "POPKEK", "TOPKEK") ||
                    (ContainsAny(upper, "KEK") && !ContainsAny(upper, "KEKİK", "KEKIK")))
                    return TryGetSub("1400", "1405");
                if (ContainsAny(upper, "KRAKER", "ÇİZİ", "CIZI", "ETİ GONG") ||
                    (ContainsAny(upper, "ÇUBUK", "CUBUK") && !ContainsAny(upper, "TARÇIN", "TARCIN", "VANİLYA", "VANILYA", "DİFÜZÖR", "DIFFUSER")))
                    return TryGetSub("1400", "1409");
                if (ContainsAny(upper, "MISIR PATLAĞI", "POPCORN", "CİN MISIR", "PİRİNÇ PATLAĞI"))
                    return TryGetSub("1400", "1410");
                if (ContainsAny(upper, "BAR ", "PROTEİN BAR", "CORNY", "SNICKERS", "TWIX", "MARS ", "BOUNTY", "METRO ", "ALBENİ", "ALBENI", "ÇOKONAT", "COKONAT", "LAVİVA", "WANTED"))
                    return TryGetSub("1400", "1408");
                if (ContainsAny(upper, "BİSKÜVİ", "BISKUVI", "PETİBÖR", "PETIBOR", "BURÇAK", "BURCAK", "NEGRO", "OREO", "BİSKREM", "BISKREM", "TUTKU", "İKRAM", "IKRAM", "HANIMELLER", "PROBİS", "PROBIS", "CICIBEBE", "CİCİBEBE"))
                    return TryGetSub("1400", "1407");
                if (ContainsAny(upper, "ÇİKOLATA", "CIKOLATA", "KARAM", "MILKA", "TOBLERONE", "DAMAK", "TADELLE", " SÜTLÜ ", " BİTTER "))
                    return TryGetSub("1400", "1404");
                if (ContainsAny(upper, "KURUYEMİŞ", "FINDIK", "FISTIK", "BADEM", "CEVİZ", "CEVIZ", "KAJU", "LEBLEBİ", "LEBLEBI", "ÇEKİRDEK", "CEKIRDEK", "KURU ÜZÜM", "KURU KAYISI", "KURU İNCİR", "HURMA"))
                    return upper.Contains("AÇIK") || upper.Contains("KG")
                        ? (TryGetSub("1100", "1101") ?? TryGetSub("1100", "1102"))
                        : (TryGetSub("1100", "1102") ?? TryGetSub("1100", "1101"));
            }

            // --- 100: İÇECEKLER ---
            if (MatchesMain("100"))
            {
                if (ContainsAny(upper, "MADEN SUYU", "SODA", "BEYPAZARI", "KIZILAY", "SIRMA", "ULUDAG FRUTTI", "ULUDAĞ FRUTTİ", "SARIKIZ", "AKMİNA"))
                    return TryGetSub("100", "105");
                if (ContainsAny(upper, "ÇAY ", "ÇAYI", "CAY ", "CAYI", "LİPTON", "LIPTON", "ÇAYKUR", "CAYKUR", "DOĞUŞ ÇAY", "DOGUS CAY", "IHLAMUR", "ADAÇAYI", "PAPATYA", "YEŞİL ÇAY"))
                    return TryGetSub("100", "101");
                if (ContainsAny(upper, "KAHVE", "NESCAFE", "ESPRESSO", "CAPPUCCINO", "LATTE", "FİLTRE KAHVE", "TÜRK KAHVE", "KURUKAHVECİ", "JACOBS", "TCHIBO", "SALEP", "SICAK ÇİKOLATA"))
                    return TryGetSub("100", "102");
                if (ContainsAny(upper, "KOLA", "COLA", "FANTA", "SPRITE", "PEPSI", "GAZOZ", "SCHWEPPES", "TONIC", "YEDİGÜN", "FRUKO", "RED BULL", "MONSTER", "BURN "))
                    return TryGetSub("100", "104");
                if (ContainsAny(upper, "MEYVE SUYU", "NEKTAR", "LİMONATA", "LIMONATA", "SOĞUK ÇAY", "ICE TEA", "FUSE TEA", "ŞALGAM", "SALGAM", "ŞIRA", "BOZA", "CAPPY", "DIMES", "TAMEK", "JUCY"))
                    return TryGetSub("100", "103");
                if (ContainsAny(upper, " SU ", " SUYU", "ERiKLİ", "ERİKLİ", "PINAR SU", "HAYAT SU", "SAKA SU", "DAMACANA", "0.5 LT", "0,5 LT", "1.5 LT", "1,5 LT", "5 LT"))
                    return TryGetSub("100", "106");
            }

            // --- 400: ET VE TAVUK ---
            if (MatchesMain("400"))
            {
                if (ContainsAny(upper, "TAVUK", "PİLİÇ", "PILIC", "HİNDİ", "HINDI", "KANAT", "BAGET", "ŞİNİTZEL", "NUGGET"))
                    return TryGetSub("400", "401");
                if (ContainsAny(upper, "DANA", "KUZU", "KIYMA", "KUŞBAŞI", "KUSBASI", "ANTRİKOT", "ANTRIKOT", "BONFİLE", "BONFILE", "BİFTEK", "BIFTEK", "KÖFTE", "KOFTE", "pirzola", "PİRZOLA", "ET "))
                    return TryGetSub("400", "402");
            }

            // --- 2200: MEYVE & SEBZE ---
            if (MatchesMain("2200"))
            {
                if (ContainsAny(upper, "DONUK", "DONDURULMUŞ", "DONDURULMUS", "SUPERFRESH", "SUPER FRESH", "FEAST"))
                    return TryGetSub("2200", "2204");
                if (ContainsAny(upper, "KONSERVE", "TURŞU", "TURSU", "GARNİTÜR", "GARNITUR", "KÖZLENMİŞ", "KOZLENMIS", "KAVANOZ", "TENEKE", "HAŞLANMIŞ", "HASLANMIS"))
                    return TryGetSub("2200", "2203");
                if (ContainsAny(upper, "ELMA", "ARMUT", "MUZ", "PORTAKAL", "MANDALİNA", "LİMON", "ÇİLEK", "KİRAZ", "ŞEFTALİ", "KAYISI", "ÜZÜM", "KARPUZ", "KAVUN", "KİVİ", "ANANAS", "AVOKADO", "NAR ", "ERİK", "İNCİR", "GREYFURT", "MEYVE"))
                    return TryGetSub("2200", "2201");
                if (ContainsAny(upper, "DOMATES", "SALATALIK", "BİBER", "PATLICAN", "KABAK", "PATATES", "SOĞAN", "SARIMSAK", "HAVUÇ", "LAHANA", "KARNABAHAR", "BROKOLİ", "PIRASA", "KEREVİZ", "ENGİNAR", "FASULYE", "BEZELYE", "BAMYA", "MANTAR", "MAYDANOZ", "DEREOTU", "NANE", "ROKA", "MARUL", "KIVIRCIK", "ISPANAK", "SEBZE", "YEŞİLLİK"))
                    return TryGetSub("2200", "2202");
            }

            // --- 700: TEMEL GIDA ---
            if (MatchesMain("700"))
            {
                if (ContainsAny(upper, "ZEYTİNYAĞ", "ZEYTINYAG", "AYÇİÇEK YAĞ", "AYCICEK YAG", "MISIRÖZÜ", "MISIR YAĞ", "SIVI YAĞ", "FINDIK YAĞ", "KANOLA"))
                    return TryGetSub("700", "705");
                if (ContainsAny(upper, "MAKARNA", "SPAGETTİ", "SPAGETTI", "PENNE", "FİYONK", "BURGU MAKARNA", "LAZANYA", "ERİŞTE", "ŞEHRİYE", "KUSKUS", "NOODLE"))
                    return TryGetSub("700", "704");
                if (ContainsAny(upper, "PİRİNÇ", "PIRINC", "BULGUR", "MERCİMEK", "MERCIMEK", "NOHUT", "FASULYE", "BARBUNYA", "BAKLA", "İRMİK", "IRMIK", "KİNOA", "KARABUĞDAY", "BAKLİYAT"))
                    return TryGetSub("700", "701");
                if (ContainsAny(upper, "SALÇA", "SALCA", "KETÇAP", "KETCAP", "MAYONEZ", "HARDAL", "SOS", "SİRKE", "SIRKE", "NAR EKŞİ", "LİMON SUYU"))
                    return TryGetSub("700", "706");
                if (ContainsAny(upper, "KONSERVE", "TURŞU", "TURSU", "GARNİTÜR", "GARNITUR", "KÖZLENMİŞ", "YAPRAK SARMA", "TON BALIĞI", "MISIR KONSERVE"))
                    return TryGetSub("700", "703");
                if (ContainsAny(upper, "ŞEKER", "SEKER", "TOZ ŞEKER", "KÜP ŞEKER", "ESMER ŞEKER", "TATLANDIRICI"))
                    return TryGetSub("700", "707");
                if (ContainsAny(upper, "UN ", "UNU ", "BUĞDAY UNU", "MISIR UNU", "TAM BUĞDAY UN"))
                    return TryGetSub("700", "709");
                if (ContainsAny(upper, "BULYON", "ÇORBA", "CORBA"))
                    return TryGetSub("700", "702");
                if (ContainsAny(upper, "TUZ", "KARABİBER", "PUL BİBER", "KİMYON", "KEKİK", "NANE", "TARÇIN", "ZENCEFİL", "ZERDEÇAL", "BAHARAT", "HARÇ", "KABARTMA TOZU", "VANİLYA", "MAYA", "NİŞASTA", "KAKAO", "KREM ŞANTİ"))
                    return TryGetSub("700", "708");
            }

            // --- 1300 / 800: DONDURULMUŞ GIDA & DONDURMA ---
            if (MatchesMain("1300", "800"))
            {
                if (ContainsAny(upper, "DONDURMA", "MAGNUM", "CORNETTO", "CARTE D'OR", "CARTE DOR", "MARAŞ", "MEYBUZ", "MAX ", "BRAVO", "ALGİDA", "GOLF "))
                    return upper.Contains("ML") || upper.Contains("KAP") || upper.Contains("CARTE") || upper.Contains("MARAŞ")
                        ? (TryGetSub("800", "801") ?? TryGetSub("800", "802"))
                        : (TryGetSub("800", "802") ?? TryGetSub("800", "801"));
                if (ContainsAny(upper, "BALIK", "SOMON", "KARİDES", "KALAMAR"))
                    return TryGetSub("1300", "1301");
                if (ContainsAny(upper, "PİLİÇ", "TAVUK", "ŞİNİTZEL", "NUGGET", "CORDON BLEU"))
                    return TryGetSub("1300", "1302");
                if (ContainsAny(upper, "KÖFTE", "BURGER", "DÖNER", "sucuk"))
                    return TryGetSub("1300", "1303");
                if (ContainsAny(upper, "PİZZA", "PIZZA", "BÖREK", "BOREK", "MİLFÖY", "MILFOY", "LAHMACUN", "PİDE", "MANTI", "KÜNEFE", "SUFLE", "PASTA"))
                    return TryGetSub("1300", "1305");
                if (ContainsAny(upper, "PATATES", "BEZELYE", "MISIR", "GARNİTÜR", "BROKOLİ", "ISPANAK", "FASULYE", "SEBZE", "MEYVE"))
                    return TryGetSub("1300", "1304");
            }

            // --- 1000 / 1500: EV BAKIM & KAĞIT ISLAK MENDİL ---
            if (MatchesMain("1000", "1500"))
            {
                if (ContainsAny(upper, "ISLAK MENDİL", "ISLAK MENDIL", "ISLAK HAVLU"))
                    return TryGetSub("1500", "1501");
                if (ContainsAny(upper, "KAĞIT HAVLU", "KAGIT HAVLU", "RULO HAVLU"))
                    return TryGetSub("1500", "1502");
                if (ContainsAny(upper, "TUVALET KAĞIDI", "TUVALET KAGIDI"))
                    return TryGetSub("1500", "1503");
                if (ContainsAny(upper, "KAĞIT MENDİL", "CEP MENDİL", "KUTU MENDİL"))
                    return TryGetSub("1500", "1504");
                if (ContainsAny(upper, "PEÇETE", "PECETE"))
                    return TryGetSub("1500", "1505");
                if (ContainsAny(upper, "ÇÖP TORBA", "COP TORBA", "ÇÖP POŞET", "COP POSET"))
                    return TryGetSub("1000", "1003");
                if (ContainsAny(upper, "BULAŞIK", "BULASIK", "FAIRY", "PRİL", "PRIL", "FINISH", "CALGONİT"))
                    return TryGetSub("1000", "1005");
                if (ContainsAny(upper, "ÇAMAŞIR DETERJAN", "CAMASIR DETERJAN", "TOZ DETERJAN", "SIVI DETERJAN", "YUMUŞATICI", "YUMUSATICI", "OMO ", "ARIEL", "PERSİL", "PERSIL", "PERWOLL", "VERNEL", "YUMOŞ", "YUMOS", "VANISH", "KOSLA", "BİNGO", "BINGO"))
                    return TryGetSub("1000", "1004");
                if (ContainsAny(upper, "SÜNGER", "SUNGER", "TEMİZLİK BEZ", "ELDİVEN", "ELDIVEN", "VİLEDA", "VILEDA", "MOP ", "FIRÇA", "STREÇ", "FOLYO", "BUZDOLABI POŞET", "PİŞİRME KAĞIDI"))
                    return TryGetSub("1000", "1001");
                if (ContainsAny(upper, "ÇAMAŞIR SUYU", "DOMESTOS", "CİF", "CIF ", "ASPEROX", "YÜZEY TEMİZ", "CAM TEMİZ", "KİREÇ", "LAVABO", "WC ", "PORÇÖZ", "MR MUSCLE"))
                    return TryGetSub("1000", "1002");
            }

            // --- 2000: EV & MUTFAK ---
            if (MatchesMain("2000"))
            {
                if (ContainsAny(upper, "ÇAKMAK", "CAKMAK", "KİBRİT", "KIBRIT"))
                    return TryGetSub("2000", "2001");
                if (ContainsAny(upper, "PİL ", "PIL ", "VARTA", "DURACELL", "PANASONIC"))
                    return TryGetSub("2000", "2009");
                if (ContainsAny(upper, "MUM", "ODA KOKUSU", "TÜTSÜ"))
                    return TryGetSub("2000", "2006");
                if (ContainsAny(upper, "KALEM", "DEFTER", "SILGI", "BANT", "YAPIŞTIRICI", "KIRTASİYE"))
                    return TryGetSub("2000", "2005");
                if (ContainsAny(upper, "MANGAL", "ÇIRA", "KÖMÜR", "KÜRDAN", "ÇÖP ŞİŞ", "PLASTİK BARDAK", "KARTON BARDAK", "PLASTİK TABAK", "PİKNİK"))
                    return TryGetSub("2000", "2008");
                if (ContainsAny(upper, "BARDAK", "TABAK", "KASE", "TENCERE", "TAVA", "BIÇAK", "ÇATAL", "KAŞIK", "SAKLAMA KABI", "TERMOS", "SÜRAHİ", "RENDE", "SÜZGEÇ", "TEPSİ", "MUTFAK"))
                    return TryGetSub("2000", "2007");
            }

            return null;
        }

        private static bool ContainsAny(string upperText, params string[] keywords)
        {
            foreach (var kw in keywords)
            {
                if (upperText.Contains(kw.ToUpper(TrCulture), StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        private static async Task<Category> EnsureUncategorizedCategoryAsync(
            ECommerceDbContext context,
            CancellationToken ct)
        {
            var existing = await context.Categories
                .FirstOrDefaultAsync(c => c.Slug == UncategorizedSlug, ct);

            if (existing != null)
                return existing;

            var diger = new Category
            {
                Name = UncategorizedName,
                Description = "Henüz kategorize edilmemiş ürünler.",
                Slug = UncategorizedSlug,
                SortOrder = 999,
                IsActive = false, // Vitrinde görünmez
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Categories.Add(diger);
            await context.SaveChangesAsync(ct);
            return diger;
        }

        private static async Task EnsureWildcardMappingAsync(
            ECommerceDbContext context,
            int digerCategoryId,
            CancellationToken ct)
        {
            var existing = await context.Set<MikroCategoryMapping>()
                .FirstOrDefaultAsync(m => m.MikroAnagrupKod == "*", ct);

            if (existing != null)
            {
                if (existing.CategoryId != digerCategoryId)
                {
                    existing.CategoryId = digerCategoryId;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
                return;
            }

            context.Set<MikroCategoryMapping>().Add(new MikroCategoryMapping
            {
                MikroAnagrupKod = "*",
                MikroAltgrupKod = null,
                MikroMarkaKod = null,
                CategoryId = digerCategoryId,
                Priority = 999,
                IsActive = true,
                MikroGrupAciklama = "Varsayılan eşleme — hiçbir kurala uymayan ürünler 'Diğer' kategorisine atanır",
                Notes = "CategorySeeder tarafından otomatik oluşturuldu",
                CreatedAt = DateTime.UtcNow
            });
        }

        public static bool IsPublicStorefrontCategorySlug(string? slug)
        {
            return !string.IsNullOrWhiteSpace(slug) &&
                !string.Equals(slug.Trim(), UncategorizedSlug, StringComparison.OrdinalIgnoreCase);
        }

        private static async Task FixOrphanProductsAsync(
            ECommerceDbContext context,
            int digerCategoryId,
            CancellationToken ct)
        {
            var validCategoryIds = await context.Categories
                .Select(c => c.Id)
                .ToListAsync(ct);
            var validSet = new HashSet<int>(validCategoryIds);

            var orphanProducts = await context.Products
                .Where(p => !validSet.Contains(p.CategoryId))
                .ToListAsync(ct);

            if (orphanProducts.Count == 0)
                return;

            foreach (var product in orphanProducts)
            {
                product.CategoryId = digerCategoryId;
                product.UpdatedAt = DateTime.UtcNow;
            }

            await context.SaveChangesAsync(ct);
        }

        private static async Task FixOrphanSubcategoriesAsync(
            ECommerceDbContext context,
            CancellationToken ct)
        {
            var allCategoryIds = await context.Categories
                .Select(c => c.Id)
                .ToListAsync(ct);
            var validSet = new HashSet<int>(allCategoryIds);

            var orphanSubcategories = await context.Categories
                .Where(c => c.ParentId.HasValue && !validSet.Contains(c.ParentId.Value))
                .ToListAsync(ct);

            if (orphanSubcategories.Count == 0)
                return;

            foreach (var cat in orphanSubcategories)
            {
                cat.ParentId = null;
                cat.UpdatedAt = DateTime.UtcNow;
            }

            await context.SaveChangesAsync(ct);
        }

        private static string EnsureUniqueSlug(string baseSlug, HashSet<string> usedSlugs)
        {
            var candidate = baseSlug;
            int suffix = 2;
            while (usedSlugs.Contains(candidate))
            {
                candidate = $"{baseSlug}-{suffix++}";
            }
            usedSlugs.Add(candidate);
            return candidate;
        }

        public static string Slugify(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            string lower = input.Trim().ToLowerInvariant();
            lower = lower
                .Replace("ç", "c").Replace("ğ", "g").Replace("ı", "i").Replace("ö", "o").Replace("ş", "s").Replace("ü", "u")
                .Replace("Ç", "c").Replace("Ğ", "g").Replace("İ", "i").Replace("Ö", "o").Replace("Ş", "s").Replace("Ü", "u");
            lower = Regex.Replace(lower, @"[^a-z0-9\s-]", "");
            lower = Regex.Replace(lower, @"\s+", "-");
            lower = Regex.Replace(lower, "-+", "-");
            return lower.Trim('-');
        }

        private static string ToTurkishTitleCase(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            var lower = input.Trim().ToLower(TrCulture);
            return TrCulture.TextInfo.ToTitleCase(lower);
        }
    }
}
