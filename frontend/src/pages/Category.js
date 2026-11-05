// Kategoriye göre ürün listeleme — Sticky Alt Kategori Barı + Scroll-Spy Bölümleri (Mikro ERP Hiyerarşisi)
import React, {
  useEffect,
  useState,
  useCallback,
  useMemo,
  useRef,
} from "react";
import {
  useParams,
  useSearchParams,
  useLocation,
  Link,
} from "react-router-dom";
import { Helmet } from "react-helmet-async";
import ProductGrid from "../components/ProductGrid";
import { ProductService, mapProduct } from "../services/productService";
import categoryServiceReal, {
  matchesCategorySlug,
  normalizeCategorySlug,
} from "../services/categoryServiceReal";
import { sanitizeUrlParam } from "../utils/securityHelpers";

const createSlug = (name) => {
  return String(name || "")
    .toLowerCase()
    .replace(/ç/g, "c")
    .replace(/ğ/g, "g")
    .replace(/ı/g, "i")
    .replace(/ö/g, "o")
    .replace(/ş/g, "s")
    .replace(/ü/g, "u")
    .replace(/[^a-z0-9\s-]/g, "")
    .replace(/\s+/g, "-")
    .replace(/-+/g, "-")
    .trim();
};

const toTrUpper = (text) => String(text || "").trim().toLocaleUpperCase("tr-TR");

const containsAnyKeyword = (upperText, keywords) =>
  keywords.some((kw) => upperText.includes(toTrUpper(kw)));

/**
 * Ürün adından Mikro alt grup kodunu (örn. 509 Yumurta, 501 Süt, 502 Peynir) çözümler.
 * Backend henüz yeniden başlatılmamış olsa bile `(Genel)` altında kalan veya çapraz kategorideki
 * ürünleri doğru alt kategori bölümüne dağıtır.
 */
const resolveSubAltKodByProductName = (mainAnaKod, productName) => {
  const upper = toTrUpper(productName);
  if (!upper) return null;

  // Dondurma ürünleri (Cornetto, Magnum, Algida, Calippo vb.) kesinlikle dondurmadır (800 / 801-802),
  // içinde OREO, ÇİKOLATA, MEYVE geçse dahi asla bisküvi, çikolata veya meyveye giremez!
  if (
    containsAnyKeyword(upper, ["DONDURMA", "MAGNUM", "CORNETTO", "CORNET", "CARTE D'OR", "CARTE DOR", "CALIPPO", "MARAŞ", "MEYBUZ", "MAX ", "BRAVO", "ALGİDA", "ALGIDA", "GOLF "])
  ) {
    return upper.includes("ML") || upper.includes("KAP") || upper.includes("CARTE") || upper.includes("MARAŞ") ? "801" : "802";
  }

  // Milka Choco Snack, çikolata değil tatlı/krema (505) bölümünde olmalı
  if (containsAnyKeyword(upper, ["CHOCO SNACK"])) {
    return "505";
  }

  // Mantı & Hıngel donuk gıdadır (1306), makarna değildir
  if (containsAnyKeyword(upper, ["MANTI", "HINGEL", "HİNGEL"])) {
    return "1306";
  }

  // Çiğ köfte hazır yemek / mezedir (1201), kahvaltılık değildir
  if (containsAnyKeyword(upper, ["ÇİĞ KÖFTE", "CIG KOFTE"])) {
    return "1201";
  }

  // Alaçatı ve kurabiyeler fırın / pastanedir (604)
  if (containsAnyKeyword(upper, ["ALAÇATI", "ALACATI"]) || (containsAnyKeyword(upper, ["DAMLA SAKIZ"]) && containsAnyKeyword(upper, ["KURABİYE", "KURABIYE"]))) {
    return "604";
  }

  // İzmir bombası kuru pasta / tatlıdır (604), çikolata değildir
  if (containsAnyKeyword(upper, ["IZMIR BOMBASI", "İZMİR BOMBASI"])) {
    return "604";
  }

  // Kekik ve Çubuk Tarçın baharattır (708), kek veya kraker değildir
  if (containsAnyKeyword(upper, ["KEKİK", "KEKIK"]) || containsAnyKeyword(upper, ["ÇUBUK TARÇIN", "CUBUK TARCIN"])) {
    return "708";
  }

  // Ben Organic Portakal Suyu gazsız içecektir (103)
  if (containsAnyKeyword(upper, ["BEN ORGANIC", "BEN ORGANİK"]) && containsAnyKeyword(upper, ["PORTAKAL"])) {
    return "103";
  }

  // Çapraz kategori kesin eşleşmeler (500 Süt & Kahvaltılık alt grupları)
  if (
    containsAnyKeyword(upper, ["YUMURTA", "BILDIRCIN YUMURTA", "GEZEN TAVUK YUMURTA"]) &&
    !containsAnyKeyword(upper, ["KINDER", "OZMO", "SÜRPRİZ", "SURPRIZ", "ÇİKOLATA", "CIKOLATA", "OYUNCAK"])
  ) {
    return "509";
  }

  if (
    containsAnyKeyword(upper, ["ZEYTİN", "ZEYTIN"]) &&
    !containsAnyKeyword(upper, ["ZEYTİNYAĞ", "ZEYTINYAG", "SIZMA", "RİVİERA", "RIVIERA", "SABUN", "ŞAMPUAN", "SAMPUAN", "DUŞ", "KREM", "KOLONYA", "LOSYON", "MENDİL"])
  ) {
    return "510";
  }

  if (
    mainAnaKod !== "1300" &&
    containsAnyKeyword(upper, ["SUCUK", "SALAM", "SOSİS", "SOSIS", "PASTIRMA", "JAMBON", "KAVURMA", "FÜME ET", "FUME ET", "DANA FÜME", "HİNDİ FÜME", "PİLİÇ FÜME", "ŞARKÜTERİ", "SARKUTERI", "KURU ET"])
  ) {
    return "503";
  }

  if (
    containsAnyKeyword(upper, ["TEREYAĞ", "TEREYAG", "SADE YAĞ", "SADEYAĞ", "SADEYAG", "SILIVRI ASLAN", "SİLİVRİ ASLAN"]) &&
    !containsAnyKeyword(upper, ["BİSKÜVİ", "BISKUVI", "KURABİYE", "KRUVASAN", "CİPS"])
  ) {
    return "508";
  }

  if (containsAnyKeyword(upper, ["MARGARİN", "MARGARIN", "BECEL", "BİZİM YAĞ", "BIZIM YAG", "TEREMYAĞ", "TEREMYAG", "SANA "]) && !containsAnyKeyword(upper, ["TEREYAĞ", "TEREYAG", "SILIVRI ASLAN", "SİLİVRİ ASLAN"])) {
    return "507";
  }

  if (
    containsAnyKeyword(upper, [
      "REÇEL", "RECEL", "MARMELAT", "ÇİÇEK BALI", "ÇAM BALI", "PETEK BAL", "SÜZME BAL", "KARAKOVAN", "KESTANE BALI",
      "PEKMEZ", "TAHİN", "TAHIN", "HELVA", "FINDIK EZME", "FISTIK EZME", "NUTELLA", "ÇOKOKREM", "COKOKREM",
      "SARELLE", "FISKOBIRLIK", "FİSKOBİRLİK", "TORKU BANADA", "MÜSLİ", "MUSLI", "GRANOLA", "CORN FLAKES", "KAHVALTILIK GEVREK", "NESFIT"
    ])
  ) {
    return "506";
  }

  const matchesMain = (...codes) =>
    !mainAnaKod || codes.some((c) => String(c) === String(mainAnaKod));

  if (matchesMain("500")) {
    if (containsAnyKeyword(upper, ["YOĞURT", "YOGURT", "AYRAN", "KEFİR", "KEFIR", "ACTİVİA", "ACTIVIA", "DANONİNO"])) return "504";
    if (containsAnyKeyword(upper, ["KREMA", "KAYMAK", "PUDİNG", "PUDING", "SÜTLAÇ", "SUTLAC", "KAZANDİBİ", "SUPANGLE", "PROFİTEROL", "MAGNOLIA", "TRİLEÇE", "KEŞKÜL", "MUHALLEBİ", "DANETTE"])) return "505";
    if (containsAnyKeyword(upper, ["PEYNİR", "PEYNIR", "KAŞAR", "KASAR", "LABNE", "LOR ", "LORU", "TULUM", "MOZZARELLA", "HELLİM", "HELLIM", "BURGU", "ÇEÇİL", "CECIL", "EZİNE", "EZINE", "ÖRGÜ", "KREM PEY", "ÜÇGEN PEY", "SÜZME PEY", "BEYAZ PEY", "TOST PEY", "DİL PEY", "ÇERKES", "PARMESAN", "CHEDDAR", "GOUDA", "ROKFOR", "MASCARPO", "RICOTTA", "ÇÖKELEK", "KARPER"])) return "502";
    if (containsAnyKeyword(upper, ["BAL ", "BALI ", "GEVREK", "YULAF EZME", "COCO POPS"])) return "506";
    if (containsAnyKeyword(upper, ["SÜT", "SUT ", "SÜTÜ", "SUTU", "LAKTOZSUZ", "DEVAM SÜTÜ"])) return "501";
  }

  if (matchesMain("1400", "1100")) {
    if (containsAnyKeyword(upper, ["SAKIZ", "FALIM", "VIVIDENT", "FIRST ", "MENTOS"])) return "1401";
    if (containsAnyKeyword(upper, ["JELİBON", "JELIBON", "HARIBO", "ŞEKERLEME", "SEKERLEME", "LOLİPOP", "BONBON", "LOKUM", "DRAJE", "YUPO", "BEBETO"])) return "1402";
    if (containsAnyKeyword(upper, ["CİPS", "CIPS", "DORITOS", "RUFFLES", "LAYS", "LAY'S", "PRINGLES", "CHEETOS", "ÇEREZYA", "SAMBA", "PATOS"])) return "1403";
    if (containsAnyKeyword(upper, ["GOFRET", "HOŞBEŞ", "HOSBES", "DİDO", "DIDO", "ÇOKOPRENS"])) return "1406";
    if (containsAnyKeyword(upper, ["KEK", "BROWNİ", "BROWNI", "SUFLE", "KRUVASAN", "HALLEY", "LUPPO", "DANKEK", "POPKEK", "TOPKEK"]) && !containsAnyKeyword(upper, ["KEKİK", "KEKIK"])) return "1405";
    if (containsAnyKeyword(upper, ["KRAKER", "ÇUBUK", "CUBUK", "ÇİZİ", "CIZI", "ETİ GONG", "TUZLU"]) && !containsAnyKeyword(upper, ["TARÇIN", "TARCIN"])) return "1409";
    if (containsAnyKeyword(upper, ["MISIR PATLAĞI", "POPCORN", "CİN MISIR", "PİRİNÇ PATLAĞI"])) return "1410";
    if (containsAnyKeyword(upper, ["BAR ", "PROTEİN BAR", "CORNY", "SNICKERS", "TWIX", "MARS ", "BOUNTY", "METRO ", "ALBENİ", "ALBENI", "ÇOKONAT", "COKONAT", "LAVİVA", "WANTED"])) return "1408";
    if (containsAnyKeyword(upper, ["BİSKÜVİ", "BISKUVI", "PETİBÖR", "PETIBOR", "BURÇAK", "BURCAK", "NEGRO", "OREO", "BİSKREM", "BISKREM", "TUTKU", "İKRAM", "IKRAM", "HANIMELLER", "PROBİS", "PROBIS", "CİCİBEBE"]) && !containsAnyKeyword(upper, ["CORNETTO", "CORNET", "MAGNUM", "DONDURMA", "CALIPPO", "ALGİDA", "ALGIDA"])) return "1407";
    if (containsAnyKeyword(upper, ["ÇİKOLATA", "CIKOLATA", "KARAM", "MILKA", "TOBLERONE", "DAMAK", "TADELLE", " SÜTLÜ ", " BİTTER "]) && !containsAnyKeyword(upper, ["CORNETTO", "CORNET", "MAGNUM", "DONDURMA", "CALIPPO", "ALGİDA", "ALGIDA", "BOMBASI", "CHOCO SNACK"])) return "1404";
    if (containsAnyKeyword(upper, ["KURUYEMİŞ", "FINDIK", "FISTIK", "BADEM", "CEVİZ", "CEVIZ", "KAJU", "LEBLEBİ", "LEBLEBI", "ÇEKİRDEK", "CEKIRDEK", "KURU ÜZÜM", "KURU KAYISI", "KURU İNCİR", "HURMA"])) {
      return upper.includes("AÇIK") || upper.includes("KG") ? "1101" : "1102";
    }
  }

  if (matchesMain("100")) {
    if (containsAnyKeyword(upper, ["MADEN SUYU", "SODA", "BEYPAZARI", "KIZILAY", "SIRMA", "SARIKIZ", "AKMİNA"])) return "105";
    if (containsAnyKeyword(upper, ["ÇAY ", "ÇAYI", "CAY ", "CAYI", "LİPTON", "LIPTON", "ÇAYKUR", "CAYKUR", "DOĞUŞ ÇAY", "IHLAMUR", "ADAÇAYI", "PAPATYA", "YEŞİL ÇAY"])) return "101";
    if (containsAnyKeyword(upper, ["KAHVE", "NESCAFE", "ESPRESSO", "CAPPUCCINO", "LATTE", "FİLTRE KAHVE", "TÜRK KAHVE", "KURUKAHVECİ", "JACOBS", "TCHIBO", "SALEP", "SICAK ÇİKOLATA"])) return "102";
    if (containsAnyKeyword(upper, ["KOLA", "COLA", "FANTA", "SPRITE", "PEPSI", "GAZOZ", "SCHWEPPES", "TONIC", "YEDİGÜN", "FRUKO", "RED BULL", "MONSTER", "BURN "])) return "104";
    if (containsAnyKeyword(upper, ["MEYVE SUYU", "NEKTAR", "LİMONATA", "LIMONATA", "SOĞUK ÇAY", "ICE TEA", "FUSE TEA", "ŞALGAM", "SALGAM", "ŞIRA", "BOZA", "CAPPY", "DIMES", "TAMEK", "JUCY"])) return "103";
    if (containsAnyKeyword(upper, [" SU ", " SUYU", "ERİKLİ", "PINAR SU", "HAYAT SU", "SAKA SU", "DAMACANA", "0.5 LT", "0,5 LT", "1.5 LT", "1,5 LT", "5 LT"])) return "106";
  }

  if (matchesMain("400")) {
    if (containsAnyKeyword(upper, ["TAVUK", "PİLİÇ", "PILIC", "HİNDİ", "HINDI", "KANAT", "BAGET", "ŞİNİTZEL", "NUGGET"])) return "401";
    if (containsAnyKeyword(upper, ["DANA", "KUZU", "KIYMA", "KUŞBAŞI", "KUSBASI", "ANTRİKOT", "ANTRIKOT", "BONFİLE", "BONFILE", "BİFTEK", "BIFTEK", "KÖFTE", "KOFTE", "PİRZOLA", "ET "])) return "402";
  }

  if (matchesMain("2200")) {
    if (containsAnyKeyword(upper, ["DONUK", "DONDURULMUŞ", "DONDURULMUS", "SUPERFRESH", "SUPER FRESH", "FEAST"])) return "2204";
    if (containsAnyKeyword(upper, ["KONSERVE", "TURŞU", "TURSU", "GARNİTÜR", "GARNITUR", "KÖZLENMİŞ", "KOZLENMIS", "KAVANOZ", "TENEKE", "HAŞLANMIŞ", "HASLANMIS"])) return "2203";
    if (containsAnyKeyword(upper, ["ELMA", "ARMUT", "MUZ", "PORTAKAL", "MANDALİNA", "LİMON", "ÇİLEK", "KİRAZ", "ŞEFTALİ", "KAYISI", "ÜZÜM", "KARPUZ", "KAVUN", "KİVİ", "ANANAS", "AVOKADO", "NAR ", "ERİK", "İNCİR", "GREYFURT", "MEYVE"])) return "2201";
    if (containsAnyKeyword(upper, ["DOMATES", "SALATALIK", "BİBER", "PATLICAN", "KABAK", "PATATES", "SOĞAN", "SARIMSAK", "HAVUÇ", "LAHANA", "KARNABAHAR", "BROKOLİ", "PIRASA", "KEREVİZ", "ENGİNAR", "FASULYE", "BEZELYE", "BAMYA", "MANTAR", "MAYDANOZ", "DEREOTU", "NANE", "ROKA", "MARUL", "KIVIRCIK", "ISPANAK", "SEBZE", "YEŞİLLİK"])) return "2202";
  }

  if (matchesMain("700")) {
    if (containsAnyKeyword(upper, ["ZEYTİNYAĞ", "ZEYTINYAG", "AYÇİÇEK YAĞ", "AYCICEK YAG", "MISIRÖZÜ", "MISIR YAĞ", "SIVI YAĞ", "FINDIK YAĞ", "KANOLA"])) return "705";
    if (containsAnyKeyword(upper, ["MAKARNA", "SPAGETTİ", "SPAGETTI", "PENNE", "FİYONK", "BURGU MAKARNA", "LAZANYA", "ERİŞTE", "MANTI", "ŞEHRİYE", "KUSKUS", "NOODLE"])) return "704";
    if (containsAnyKeyword(upper, ["PİRİNÇ", "PIRINC", "BULGUR", "MERCİMEK", "MERCIMEK", "NOHUT", "FASULYE", "BARBUNYA", "BAKLA", "İRMİK", "IRMIK", "KİNOA", "KARABUĞDAY", "BAKLİYAT"])) return "701";
    if (containsAnyKeyword(upper, ["SALÇA", "SALCA", "KETÇAP", "KETCAP", "MAYONEZ", "HARDAL", "SOS", "SİRKE", "SIRKE", "NAR EKŞİ", "LİMON SUYU"])) return "706";
    if (containsAnyKeyword(upper, ["KONSERVE", "TURŞU", "TURSU", "GARNİTÜR", "GARNITUR", "KÖZLENMİŞ", "YAPRAK SARMA", "TON BALIĞI", "MISIR KONSERVE"])) return "703";
    if (containsAnyKeyword(upper, ["ŞEKER", "SEKER", "TOZ ŞEKER", "KÜP ŞEKER", "ESMER ŞEKER", "TATLANDIRICI"])) return "707";
    if (containsAnyKeyword(upper, ["UN ", "UNU ", "BUĞDAY UNU", "MISIR UNU", "TAM BUĞDAY UN"])) return "709";
    if (containsAnyKeyword(upper, ["BULYON", "ÇORBA", "CORBA"])) return "702";
    if (containsAnyKeyword(upper, ["TUZ", "KARABİBER", "PUL BİBER", "KİMYON", "KEKİK", "NANE", "TARÇIN", "ZENCEFİL", "ZERDEÇAL", "BAHARAT", "HARÇ", "KABARTMA TOZU", "VANİLYA", "MAYA", "NİŞASTA", "KAKAO", "KREM ŞANTİ"])) return "708";
  }

  if (matchesMain("1300", "800")) {
    if (containsAnyKeyword(upper, ["DONDURMA", "MAGNUM", "CORNETTO", "CARTE D'OR", "CARTE DOR", "MARAŞ", "MEYBUZ", "MAX ", "BRAVO", "ALGİDA", "GOLF "])) {
      return upper.includes("ML") || upper.includes("KAP") || upper.includes("CARTE") || upper.includes("MARAŞ") ? "801" : "802";
    }
    if (containsAnyKeyword(upper, ["BALIK", "SOMON", "KARİDES", "KALAMAR"])) return "1301";
    if (containsAnyKeyword(upper, ["PİLİÇ", "TAVUK", "ŞİNİTZEL", "NUGGET", "CORDON BLEU"])) return "1302";
    if (containsAnyKeyword(upper, ["KÖFTE", "BURGER", "DÖNER"])) return "1303";
    if (containsAnyKeyword(upper, ["PİZZA", "PIZZA", "BÖREK", "BOREK", "MİLFÖY", "MILFOY", "LAHMACUN", "PİDE", "MANTI", "KÜNEFE", "SUFLE", "PASTA"])) return "1305";
    if (containsAnyKeyword(upper, ["PATATES", "BEZELYE", "MISIR", "GARNİTÜR", "BROKOLİ", "ISPANAK", "FASULYE", "SEBZE", "MEYVE"])) return "1304";
  }

  if (matchesMain("1000", "1500")) {
    if (containsAnyKeyword(upper, ["ISLAK MENDİL", "ISLAK MENDIL", "ISLAK HAVLU"])) return "1501";
    if (containsAnyKeyword(upper, ["KAĞIT HAVLU", "KAGIT HAVLU", "RULO HAVLU"])) return "1502";
    if (containsAnyKeyword(upper, ["TUVALET KAĞIDI", "TUVALET KAGIDI"])) return "1503";
    if (containsAnyKeyword(upper, ["KAĞIT MENDİL", "CEP MENDİL", "KUTU MENDİL"])) return "1504";
    if (containsAnyKeyword(upper, ["PEÇETE", "PECETE"])) return "1505";
    if (containsAnyKeyword(upper, ["ÇÖP TORBA", "COP TORBA", "ÇÖP POŞET", "COP POSET"])) return "1003";
    if (containsAnyKeyword(upper, ["BULAŞIK", "BULASIK", "FAIRY", "PRİL", "PRIL", "FINISH", "CALGONİT"])) return "1005";
    if (containsAnyKeyword(upper, ["ÇAMAŞIR DETERJAN", "CAMASIR DETERJAN", "TOZ DETERJAN", "SIVI DETERJAN", "YUMUŞATICI", "YUMUSATICI", "OMO ", "ARIEL", "PERSİL", "PERSIL", "PERWOLL", "VERNEL", "YUMOŞ", "YUMOS", "VANISH", "KOSLA", "BİNGO", "BINGO"])) return "1004";
    if (containsAnyKeyword(upper, ["SÜNGER", "SUNGER", "TEMİZLİK BEZ", "ELDİVEN", "ELDIVEN", "VİLEDA", "VILEDA", "MOP ", "FIRÇA", "STREÇ", "FOLYO", "BUZDOLABI POŞET", "PİŞİRME KAĞIDI"])) return "1001";
    if (containsAnyKeyword(upper, ["ÇAMAŞIR SUYU", "DOMESTOS", "CİF", "CIF ", "ASPEROX", "YÜZEY TEMİZ", "CAM TEMİZ", "KİREÇ", "LAVABO", "WC ", "PORÇÖZ", "MR MUSCLE"])) return "1002";
  }

  if (matchesMain("2000")) {
    if (containsAnyKeyword(upper, ["ÇAKMAK", "CAKMAK", "KİBRİT", "KIBRIT"])) return "2001";
    if (containsAnyKeyword(upper, ["PİL ", "PIL ", "VARTA", "DURACELL", "PANASONIC"])) return "2009";
    if (containsAnyKeyword(upper, ["MUM", "ODA KOKUSU", "TÜTSÜ"])) return "2006";
    if (containsAnyKeyword(upper, ["KALEM", "DEFTER", "SILGI", "BANT", "YAPIŞTIRICI", "KIRTASİYE"])) return "2005";
    if (containsAnyKeyword(upper, ["MANGAL", "ÇIRA", "KÖMÜR", "KÜRDAN", "ÇÖP ŞİŞ", "PLASTİK BARDAK", "KARTON BARDAK", "PLASTİK TABAK", "PİKNİK"])) return "2008";
    if (containsAnyKeyword(upper, ["BARDAK", "TABAK", "KASE", "TENCERE", "TAVA", "BIÇAK", "ÇATAL", "KAŞIK", "SAKLAMA KABI", "TERMOS", "SÜRAHİ", "RENDE", "SÜZGEÇ", "TEPSİ", "MUTFAK"])) return "2007";
  }

  return null;
};

export default function Category() {
  const { slug: rawSlug } = useParams();
  const location = useLocation();
  const [searchParams, setSearchParams] = useSearchParams();

  const slug = useMemo(
    () => normalizeCategorySlug(sanitizeUrlParam(rawSlug)),
    [rawSlug],
  );
  const currentPage = Math.max(
    1,
    parseInt(searchParams.get("page") || "1", 10) || 1,
  );
  const pageSize = Math.max(
    1,
    parseInt(searchParams.get("size") || "25", 10) || 25,
  );

  const [category, setCategory] = useState(null);
  const [subCategories, setSubCategories] = useState([]);
  const [sections, setSections] = useState([]);
  const [breadcrumb, setBreadcrumb] = useState([]);
  const [activeSubSlug, setActiveSubSlug] = useState("");
  const [forcedEmptySlug, setForcedEmptySlug] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const stickyBarRef = useRef(null);
  const pillRefs = useRef({});
  const sectionRefs = useRef({});
  const isProgrammaticScrollRef = useRef(false);
  const scrollLockTimeoutRef = useRef(null);

  const scrollToSection = useCallback((targetSlug, smooth = true) => {
    if (!targetSlug) {
      isProgrammaticScrollRef.current = true;
      setForcedEmptySlug(null);
      window.scrollTo({ top: 0, behavior: smooth ? "smooth" : "auto" });
      if (scrollLockTimeoutRef.current)
        clearTimeout(scrollLockTimeoutRef.current);
      scrollLockTimeoutRef.current = setTimeout(() => {
        isProgrammaticScrollRef.current = false;
      }, 650);
      return;
    }

    const normalizedTarget = normalizeCategorySlug(targetSlug);
    setActiveSubSlug(normalizedTarget);
    setForcedEmptySlug(normalizedTarget);

    // Wait a tick in case the section was just revealed
    requestAnimationFrame(() => {
      const el =
        sectionRefs.current[normalizedTarget] ||
        document.getElementById(`subcat-section-${normalizedTarget}`);
      if (!el) return;

      isProgrammaticScrollRef.current = true;
      const subbarEl = stickyBarRef.current?.closest(".category-sticky-subbar");
      const subbarH = subbarEl ? subbarEl.getBoundingClientRect().height : 56;
      const stickyOffset = subbarH + 16;
      const rect = el.getBoundingClientRect();
      const targetTop = window.pageYOffset + rect.top - stickyOffset;

      window.scrollTo({
        top: Math.max(0, targetTop),
        behavior: smooth ? "smooth" : "auto",
      });

      if (scrollLockTimeoutRef.current)
        clearTimeout(scrollLockTimeoutRef.current);
      scrollLockTimeoutRef.current = setTimeout(() => {
        isProgrammaticScrollRef.current = false;
      }, 750);
    });
  }, []);

  const loadCategory = useCallback(async () => {
    if (!slug) {
      setError("Geçersiz kategori.");
      setLoading(false);
      return;
    }

    setLoading(true);
    setError("");
    setForcedEmptySlug(null);

    const normalizeCat = (c) => {
      if (!c) return null;
      return {
        ...c,
        id: c.id ?? c.Id,
        name: c.name ?? c.Name,
        slug: normalizeCategorySlug(
          c.slug ?? c.Slug ?? createSlug(c.name ?? c.Name),
        ),
        description: c.description ?? c.Description,
        imageUrl: c.imageUrl ?? c.ImageUrl,
        parentId: c.parentId ?? c.ParentId,
        isActive: c.isActive ?? c.IsActive,
        mikroAnaGrupKod: c.mikroAnaGrupKod ?? c.MikroAnaGrupKod ?? null,
        mikroAltGrupKod: c.mikroAltGrupKod ?? c.MikroAltGrupKod ?? null,
        hasProducts: Boolean(c.hasProducts ?? c.HasProducts),
        initialSubCategoryId:
          c.initialSubCategoryId ?? c.InitialSubCategoryId ?? null,
        initialSubCategorySlug:
          c.initialSubCategorySlug ?? c.InitialSubCategorySlug ?? null,
      };
    };

    try {
      // 1. Önce yeni /api/categories/{slug}/sections endpoint'ini dene
      const sectionsData = await categoryServiceReal.getSectionsBySlug(slug);
      if (sectionsData?.category) {
        const cat = normalizeCat(sectionsData.category);
        const subs = (sectionsData.subCategories || [])
          .map(normalizeCat)
          .filter((c) => c && c.isActive !== false);
        const rawSections = (sectionsData.sections || []).map((s) => {
          const rawProducts = Array.isArray(s.products ?? s.Products)
            ? s.products ?? s.Products
            : [];
          const secName = s.name ?? s.Name ?? "";
          const secSlug = normalizeCategorySlug(
            s.slug ?? s.Slug ?? createSlug(secName),
          );
          const secAnaKod = s.mikroAnaGrupKod ?? s.MikroAnaGrupKod ?? null;
          const secAltKod = s.mikroAltGrupKod ?? s.MikroAltGrupKod ?? null;
          const isFallback =
            Boolean(s.isRootFallback ?? s.IsRootFallback) ||
            String(s.slug ?? s.Slug ?? "").endsWith("-genel") ||
            !secAltKod;
          return {
            id: s.id ?? s.Id,
            name: secName,
            slug: secSlug,
            mikroAnaGrupKod: secAnaKod,
            mikroAltGrupKod: secAltKod,
            isRootFallback: isFallback,
            products: rawProducts
              .map((rp) =>
                mapProduct(
                  !isFallback
                    ? {
                        ...rp,
                        categoryName: secName || rp.categoryName || rp.CategoryName,
                        categorySlug: secSlug || rp.categorySlug || rp.CategorySlug,
                        mikroAltGrupKod: secAltKod || rp.mikroAltGrupKod,
                        mikroAnaGrupKod: secAnaKod || rp.mikroAnaGrupKod,
                      }
                    : rp,
                ),
              )
              .filter(Boolean),
          };
        });

        // Eğer ürünler '(Genel)' ana kategori bölümünde toplanmışsa veya bazı alt kategoriler boşsa,
        // ürünleri ürün adına ve Mikro alt grup koduna göre gerçek alt kategorilere dağıt.
        const realSections = rawSections.filter((s) => !s.isRootFallback);
        const fallbackSections = rawSections.filter((s) => s.isRootFallback);

        if (realSections.length > 0) {
          const sectionByAltKod = new Map();
          const sectionById = new Map();
          const existingProductKeys = new Set();

          realSections.forEach((sec) => {
            if (sec.mikroAltGrupKod) {
              sectionByAltKod.set(String(sec.mikroAltGrupKod).trim(), sec);
            }
            if (sec.id) {
              sectionById.set(Number(sec.id), sec);
            }
            (sec.products || []).forEach((p) => {
              const key = p.sku ? `sku:${p.sku}` : `id:${p.id}`;
              existingProductKeys.add(key);
            });
          });

          // Meyve & Sebze (2200) içinde eskiden 2201/2202'ye düşmüş Konserve (2203) veya Donuk (2204) ürünleri doğru alt bölüme taşı
          if (String(cat.mikroAnaGrupKod || "").trim() === "2200") {
            realSections.forEach((sec) => {
              const currentAlt = String(sec.mikroAltGrupKod || "").trim();
              if (currentAlt !== "2201" && currentAlt !== "2202") return;
              const remaining = [];
              (sec.products || []).forEach((p) => {
                const resolvedAlt = resolveSubAltKodByProductName("2200", p.name);
                if (
                  (resolvedAlt === "2203" || resolvedAlt === "2204") &&
                  sectionByAltKod.has(resolvedAlt)
                ) {
                  const destSec = sectionByAltKod.get(resolvedAlt);
                  destSec.products.push(
                    mapProduct({
                      ...p,
                      categoryName: destSec.name,
                      categorySlug: destSec.slug,
                      mikroAltGrupKod: destSec.mikroAltGrupKod,
                      mikroAnaGrupKod: destSec.mikroAnaGrupKod,
                    }) || p,
                  );
                } else {
                  remaining.push(p);
                }
              });
              sec.products = remaining;
            });
          }

          // Sadece bu ana kategorinin genel havuzundaki (fallback) ürünleri,
          // alt kategorilerine dağıtılmak üzere aday havuzuna ekle.
          // Başka reyonların ürünleri ASLA çekilmez; veritabanında ne tanımlıysa o gösterilir.
          const candidatePool = [];
          fallbackSections.forEach((fb) => {
            (fb.products || []).forEach((p) => candidatePool.push(p));
          });

          const rootProductKeys = new Set(
            fallbackSections.flatMap((fb) =>
              (fb.products || []).map((p) =>
                p.sku ? `sku:${p.sku}` : `id:${p.id}`,
              ),
            ),
          );

          const defaultAltKodByMain = {
            "500": "506",
            "400": "402",
            "2200": "2202",
            "100": "103",
            "1400": "1407",
            "1100": "1102",
            "1300": "1306",
            "800": "802",
            "1000": "1002",
            "1500": "1501",
            "700": "708",
            "2000": "2007",
          };

          candidatePool.forEach((p) => {
            const key = p.sku ? `sku:${p.sku}` : `id:${p.id}`;
            if (existingProductKeys.has(key)) return;

            // 1) Ürünün kendi categoryId'si doğrudan bu alt kategorilerden biriyse
            let targetSec = p.categoryId
              ? sectionById.get(Number(p.categoryId))
              : null;

            // 2) Ürün adından Mikro alt grup kodunu çözümle
            if (!targetSec) {
              const isFromThisCategoryRoot = rootProductKeys.has(key);
              const altKod = resolveSubAltKodByProductName(
                isFromThisCategoryRoot ? cat.mikroAnaGrupKod : null,
                p.name,
              );
              if (altKod && sectionByAltKod.has(altKod)) {
                targetSec = sectionByAltKod.get(altKod);
              } else if (isFromThisCategoryRoot) {
                const fallbackAlt =
                  defaultAltKodByMain[String(cat.mikroAnaGrupKod || "").trim()];
                targetSec =
                  (fallbackAlt && sectionByAltKod.get(fallbackAlt)) ||
                  realSections[0];
              }
            }

            if (targetSec) {
              const enrichedProduct = mapProduct({
                ...p,
                categoryName: targetSec.name || p.categoryName,
                categorySlug: targetSec.slug || p.categorySlug,
                mikroAltGrupKod: targetSec.mikroAltGrupKod || p.categoryCode,
                mikroAnaGrupKod: targetSec.mikroAnaGrupKod || p.anagrupCode,
              });
              targetSec.products.push(enrichedProduct || p);
              existingProductKeys.add(key);
            }
          });
        }

        const finalSections =
          realSections.length > 0 ? realSections : rawSections;
        const slugsWithProducts = new Set(
          finalSections
            .filter((s) => Array.isArray(s.products) && s.products.length > 0)
            .map((s) => s.slug),
        );

        const realSubs = subs.filter(
          (c) => !String(c.slug || "").endsWith("-genel"),
        );

        setCategory(cat);
        setSubCategories(realSubs.length > 0 ? realSubs : subs);
        setSections(finalSections);

        try {
          const path = await categoryServiceReal.getCategoryPath(cat.id);
          setBreadcrumb(path || []);
        } catch {
          setBreadcrumb([{ id: cat.id, name: cat.name, slug: cat.slug }]);
        }

        const hashSlug = location.hash
          ? normalizeCategorySlug(location.hash.replace(/^#/, ""))
          : null;
        const targetSubSlug = hashSlug || cat.initialSubCategorySlug || null;
        const activeSubList = realSubs.length > 0 ? realSubs : subs;
        const firstSubSlug = activeSubList[0]
          ? normalizeCategorySlug(
            activeSubList[0].slug || createSlug(activeSubList[0].name),
          )
          : "";

        if (targetSubSlug) {
          setActiveSubSlug(targetSubSlug);
          setForcedEmptySlug(targetSubSlug);
          setTimeout(() => scrollToSection(targetSubSlug, true), 180);
        } else {
          setActiveSubSlug(firstSubSlug);
        }

        setLoading(false);
        return;
      }

      // 2. Fallback: klasik getBySlug
      const cat = normalizeCat(await categoryServiceReal.getBySlug(slug));
      if (cat?.id) {
        setCategory(cat);
        try {
          const subs = await categoryServiceReal.getSubCategories(cat.id);
          setSubCategories(
            (subs || []).map(normalizeCat).filter((c) => c.isActive !== false),
          );
        } catch {
          setSubCategories([]);
        }
        try {
          const path = await categoryServiceReal.getCategoryPath(cat.id);
          setBreadcrumb(path || []);
        } catch {
          setBreadcrumb([]);
        }
        setSections([]);
        setLoading(false);
        return;
      }

      // 3. Tüm aktif kategorilerden ara
      const allCategories = await categoryServiceReal.getActive();
      const foundCat = normalizeCat(
        allCategories.find((c) => {
          const catSlug = c.slug || c.Slug || createSlug(c.name || c.Name);
          return matchesCategorySlug(catSlug, slug);
        }),
      );

      if (foundCat?.id) {
        setCategory(foundCat);
        setSubCategories([]);
        setSections([]);
      } else {
        setError("Kategori bulunamadı.");
      }
    } catch (err) {
      console.error("Kategoriler yüklenemedi:", err);
      setError(err?.message || "Kategori bilgisi yüklenemedi.");
    } finally {
      setLoading(false);
    }
  }, [slug, location.hash, scrollToSection]);

  useEffect(() => {
    loadCategory();
  }, [loadCategory]);

  // URL hash değiştiğinde (ör. üst menüden aynı kategorinin farklı bir alt kategorisine tıklandığında)
  useEffect(() => {
    if (!location.hash || loading) return;
    const hashSlug = normalizeCategorySlug(location.hash.replace(/^#/, ""));
    if (hashSlug) {
      scrollToSection(hashSlug, true);
    }
  }, [location.hash, loading, scrollToSection]);

  // Görünür alt kategori bölümleri: ürünü olanlar + kullanıcının tıkladığı alt kategori
  const visibleSections = useMemo(() => {
    if (!Array.isArray(sections) || sections.length === 0) return [];
    const withProducts = sections.filter(
      (s) =>
        (Array.isArray(s.products) && s.products.length > 0) ||
        (forcedEmptySlug && s.slug === forcedEmptySlug),
    );
    return withProducts;
  }, [sections, forcedEmptySlug]);

  // Scroll-Spy: Sayfa aşağı/yukarı kaydırıldıkça aktif alt kategoriyi tespit et (kutucukların sırası ve konumu sabit kalır)
  useEffect(() => {
    if (visibleSections.length === 0) return undefined;

    const handleScroll = () => {
      if (isProgrammaticScrollRef.current) return;

      const subbarEl = stickyBarRef.current?.closest(".category-sticky-subbar");
      const subbarH = subbarEl ? subbarEl.getBoundingClientRect().height : 56;
      const triggerLine = subbarH + 40;

      let currentActive =
        visibleSections[0]?.slug ||
        (subCategories[0]
          ? normalizeCategorySlug(
            subCategories[0].slug || createSlug(subCategories[0].name),
          )
          : "");

      for (const sec of visibleSections) {
        const el =
          sectionRefs.current[sec.slug] ||
          document.getElementById(`subcat-section-${sec.slug}`);
        if (!el) continue;
        const rect = el.getBoundingClientRect();
        if (rect.top <= triggerLine) {
          currentActive = sec.slug;
        } else {
          break;
        }
      }

      setActiveSubSlug((prev) =>
        prev !== currentActive ? currentActive : prev,
      );
    };

    window.addEventListener("scroll", handleScroll, { passive: true });
    return () => window.removeEventListener("scroll", handleScroll);
  }, [visibleSections, subCategories]);

  // Mobilde yatay kaydırmalı barda aktif alt kategori kutucuğunu görünür alana getir
  useEffect(() => {
    if (!activeSubSlug || !stickyBarRef.current) return;
    if (typeof window !== "undefined" && window.innerWidth > 768) return;
    const pillEl = pillRefs.current[activeSubSlug];
    const barEl = stickyBarRef.current;
    if (pillEl && barEl && barEl.scrollWidth > barEl.clientWidth) {
      const targetLeft =
        pillEl.offsetLeft - barEl.clientWidth / 2 + pillEl.clientWidth / 2;
      barEl.scrollTo({ left: Math.max(0, targetLeft), behavior: "smooth" });
    }
  }, [activeSubSlug]);

  const handlePaginationChange = useCallback(
    ({ page, pageSize: nextPageSize }) => {
      const params = new URLSearchParams(searchParams);
      if (page > 1) {
        params.set("page", String(page));
      } else {
        params.delete("page");
      }
      if (nextPageSize && nextPageSize !== 25) {
        params.set("size", String(nextPageSize));
      } else {
        params.delete("size");
      }
      setSearchParams(params, { replace: false });
    },
    [searchParams, setSearchParams],
  );

  useEffect(() => {
    const unsub = categoryServiceReal.subscribe(() => {
      loadCategory();
    });
    return () => unsub && unsub();
  }, [loadCategory]);

  const hasSubCategorySections =
    subCategories.length > 0 && visibleSections.length > 0;

  return (
    <div className="category-page py-3 py-md-4">
      <Helmet>
        {(() => {
          const siteUrl =
            process.env.REACT_APP_SITE_URL ||
            (typeof window !== "undefined" ? window.location.origin : "");
          const ogImage =
            category && category.imageUrl
              ? `${siteUrl}${category.imageUrl}`
              : `${siteUrl}/images/og-default.jpg`;
          const canonical = `${siteUrl}${typeof window !== "undefined" ? window.location.pathname : ""
            }`;
          return (
            <>
              <title>
                {category?.name
                  ? `${category.name} — Gölköy Gurme`
                  : "Kategori — Gölköy Gurme"}
              </title>
              <meta
                name="description"
                content={
                  category?.description || `Kategori: ${category?.name || ""}`
                }
              />
              <meta
                property="og:title"
                content={category?.name || "Kategori"}
              />
              <meta
                property="og:description"
                content={category?.description || ""}
              />
              <meta property="og:image" content={ogImage} />
              <link rel="canonical" href={canonical} />
            </>
          );
        })()}
      </Helmet>

      <div className="container-fluid px-3 px-md-4 category-page-top">
        {breadcrumb.length > 0 && (
          <nav className="mb-1 mb-md-2 category-breadcrumb-nav" aria-label="breadcrumb">
            <ol className="breadcrumb mb-1 mb-md-2" style={{ fontSize: "0.82rem" }}>
              <li className="breadcrumb-item">
                <Link
                  to="/"
                  style={{ color: "#f57c00", textDecoration: "none" }}
                >
                  <i className="fas fa-home me-1"></i>
                  Ana Sayfa
                </Link>
              </li>
              {breadcrumb.map((cat, index) => (
                <li
                  key={cat.id}
                  className={`breadcrumb-item ${index === breadcrumb.length - 1 ? "active" : ""
                    }`}
                  aria-current={
                    index === breadcrumb.length - 1 ? "page" : undefined
                  }
                >
                  {index === breadcrumb.length - 1 ? (
                    cat.name
                  ) : (
                    <Link
                      to={`/category/${cat.slug || createSlug(cat.name)}`}
                      style={{ color: "#f57c00", textDecoration: "none" }}
                    >
                      {cat.name}
                    </Link>
                  )}
                </li>
              ))}
            </ol>
          </nav>
        )}

        <div className="d-flex align-items-center justify-content-between flex-wrap gap-3 mb-2 mb-md-3 category-header-block">
          <div>
            <h1 className="h4 fw-bold mb-0 category-main-title" style={{ color: "#1e293b" }}>
              <i
                className="fas fa-layer-group me-2"
                style={{ color: "#f57c00" }}
              ></i>
              {category?.name || "Kategori"}
            </h1>
            {category?.description ? (
              <div className="text-muted small mt-1">{category.description}</div>
            ) : null}
          </div>

          <Link
            to="/"
            className="category-back-btn"
            title="Ana Sayfaya Dön"
            aria-label="Ana Sayfaya Dön"
          >
            <i className="fas fa-arrow-left category-back-icon"></i>
            <span className="d-none d-sm-inline">Ana Sayfaya Dön</span>
            <span className="d-inline d-sm-none">Ana Sayfa</span>
          </Link>
        </div>

        {error && (
          <div className="alert alert-danger" role="alert">
            {error}
          </div>
        )}
      </div>

      {/* Sticky Alt Kategori Barı (Sıralı ve sabit kutucuklar, aşağı kaydırdıkça ekranda kalır) */}
      {subCategories.length > 0 && (
        <div
          className="category-sticky-subbar"
          style={{
            position: "sticky",
            top: 0,
            zIndex: 1020,
            background: "rgba(255, 255, 255, 0.98)",
            backdropFilter: "blur(10px)",
            borderTop: "1px solid #f1f5f9",
            borderBottom: "1px solid #e2e8f0",
            boxShadow: "0 4px 12px rgba(15, 23, 42, 0.05)",
            marginBottom: "1rem",
          }}
        >
          <div className="container-fluid px-2 px-md-4">
            <div
              ref={stickyBarRef}
              className={`category-sticky-subbar-list subcount-${subCategories.length} py-2`}
            >
              {subCategories.map((subCat) => {
                const subSlug = normalizeCategorySlug(
                  subCat.slug || createSlug(subCat.name),
                );
                const isActive = activeSubSlug === subSlug;
                return (
                  <button
                    key={subCat.id}
                    ref={(el) => {
                      pillRefs.current[subSlug] = el;
                    }}
                    type="button"
                    className={`category-subcat-pill${isActive ? " is-active" : ""}`}
                    onClick={() => scrollToSection(subSlug, true)}
                    style={{
                      border: isActive
                        ? "1.5px solid #f57c00"
                        : "1px solid #e2e8f0",
                      background: isActive
                        ? "linear-gradient(135deg, #f57c00, #ff9800)"
                        : "#f8fafc",
                      color: isActive ? "#ffffff" : "#334155",
                      fontWeight: isActive ? 700 : 600,
                      fontSize: "0.82rem",
                      padding: "7px 14px",
                      borderRadius: "10px",
                      cursor: "pointer",
                      transition: "all 0.2s ease",
                      boxShadow: isActive
                        ? "0 3px 10px rgba(245, 124, 0, 0.24)"
                        : "none",
                    }}
                  >
                    <span className="category-subcat-pill-text">{subCat.name}</span>
                  </button>
                );
              })}
            </div>
          </div>
          <style>{`
            .category-sticky-subbar-list {
              display: flex;
              flex-wrap: wrap;
              align-items: center;
              gap: 8px;
              scrollbar-width: none;
              -ms-overflow-style: none;
            }
            .category-sticky-subbar-list::-webkit-scrollbar {
              display: none;
            }
            .category-subcat-pill {
              display: inline-flex;
              align-items: center;
              justify-content: center;
              text-align: center;
              flex-shrink: 0;
              white-space: nowrap;
            }
            @media (max-width: 768px) {
              .category-main-title {
                font-size: 1.05rem !important;
              }
              .category-breadcrumb-nav .breadcrumb {
                font-size: 0.74rem !important;
              }
              .category-sticky-subbar {
                margin-bottom: 0.75rem !important;
              }
              .category-sticky-subbar-list {
                display: flex !important;
                flex-wrap: nowrap !important;
                overflow-x: auto !important;
                overflow-y: hidden !important;
                -webkit-overflow-scrolling: touch;
                scroll-snap-type: x proximity;
                gap: 8px !important;
                padding: 8px 2px !important;
              }
              .category-subcat-pill {
                width: auto !important;
                height: auto !important;
                min-height: 34px !important;
                font-size: 0.8rem !important;
                line-height: 1.2 !important;
                padding: 6px 14px !important;
                border-radius: 999px !important;
                white-space: nowrap !important;
                flex-shrink: 0 !important;
                scroll-snap-align: start;
              }
              .category-subcat-pill-text {
                display: inline !important;
                overflow: visible !important;
                white-space: nowrap !important;
              }
            }
          `}</style>
        </div>
      )}

      {/* Alt Kategori Bölümleri (Aşağı kaydırdıkça alt kategori geçişi) */}
      {loading ? (
        <div className="container-fluid px-3 px-md-4 py-5 text-center">
          <div className="spinner-border text-warning" role="status">
            <span className="visually-hidden">Yükleniyor...</span>
          </div>
        </div>
      ) : hasSubCategorySections ? (
        <div className="container-fluid px-3 px-md-4">
          {visibleSections.map((sec) => {
            const isActiveSection = activeSubSlug === sec.slug;
            return (
              <section
                key={`${sec.id}-${sec.slug}`}
                id={`subcat-section-${sec.slug}`}
                ref={(el) => {
                  sectionRefs.current[sec.slug] = el;
                }}
                data-subcat-slug={sec.slug}
                className="mb-4 pb-2"
                style={{ scrollMarginTop: "110px" }}
              >
                <div
                  className="d-flex align-items-center mb-3 py-1 px-2 rounded-2"
                  style={{
                    background: isActiveSection
                      ? "rgba(245, 124, 0, 0.07)"
                      : "transparent",
                    transition: "background 0.25s ease",
                  }}
                >
                  <span
                    style={{
                      width: "5px",
                      height: "20px",
                      borderRadius: "4px",
                      background: "linear-gradient(180deg, #f57c00, #ff9800)",
                      marginRight: "10px",
                      display: "inline-block",
                      flexShrink: 0,
                    }}
                  />
                  <h2
                    className="h5 fw-bold mb-0"
                    style={{
                      color: isActiveSection ? "#ea580c" : "#1e293b",
                      fontSize: "1.02rem",
                      letterSpacing: "-0.01em",
                    }}
                  >
                    {sec.name}
                  </h2>
                </div>

                {sec.products && sec.products.length > 0 ? (
                  <ProductGrid
                    products={sec.products}
                    showTitle={false}
                    showViewAll={false}
                    displayMode="grid"
                  />
                ) : (
                  <div
                    className="py-4 px-3 rounded-3 text-muted small"
                    style={{
                      background: "#f8fafc",
                      border: "1px dashed #cbd5e1",
                    }}
                  >
                    Bu alt kategoride şu an ürün bulunmamaktadır.
                  </div>
                )}
              </section>
            );
          })}
        </div>
      ) : (
        /* Alt kategorisi olmayan veya doğrudan listelenen kategori */
        category &&
        (category.id || category.Id) && (
          <ProductGrid
            categoryId={category.id || category.Id}
            showTitle={false}
            displayMode="grid"
            initialPage={currentPage}
            initialPageSize={pageSize}
            onPaginationChange={handlePaginationChange}
          />
        )
      )}

      {!category && !loading && !error && (
        <div className="container-fluid px-3 px-md-4 py-5 text-center">
          <div className="text-muted mb-3">
            <i className="fas fa-layer-group fa-3x mb-3 text-warning"></i>
            <h5 className="fw-bold">Kategori bulunamadı</h5>
            <p className="small text-muted">Aradığınız kategori mevcut değil veya henüz ürün eklenmemiş olabilir.</p>
          </div>
          <Link
            to="/"
            className="category-back-btn d-inline-flex"
            title="Ana Sayfaya Dön"
          >
            <i className="fas fa-arrow-left category-back-icon"></i>
            <span>Ana Sayfaya Dön</span>
          </Link>
        </div>
      )}
    </div>
  );
}
