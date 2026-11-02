/**
 * Şef Tavsiyesi & Gurme Lezzetler Tarif Kataloğu
 * Her tarif için yüksek çözünürlüklü profesyonel yemek fotoğrafı ve
 * sepete eklenecek mağaza malzemeleri tanımlanmıştır.
 */

export const RECIPE_COLLECTIONS = [
  { id: "tumu", name: "Tüm Tarifler" },
  { id: "gurme-lezzetler", name: "✨ Gurme Lezzetler (Elit)" },
  { id: "sef-tavsiyesi", name: "👨‍🍳 Şef Tavsiyesi (Klasik)" },
];

export const RECIPE_CATEGORIES = [
  "Tümü",
  "Et & Balık",
  "Makarna",
  "Ana Yemek",
  "Çorba",
  "Kahvaltı",
  "Tatlı",
];

export const RECIPES = [
  /* =========================================================
   * GURME LEZZETLER (ELİT ŞEF SEÇKİSİ)
   * ========================================================= */
  {
    id: "biberiyeli-dana-bonfile",
    collection: "gurme-lezzetler",
    title: "Biberiyeli & Sarımsaklı Dana Bonfile",
    subtitle: "Döküm tavada mühürlenmiş, sarımsaklı tereyağı ve taze biberiye dokunuşlu lokum bonfile",
    category: "Et & Balık",
    image: "https://images.unsplash.com/photo-1558030006-450675393462?auto=format&fit=crop&w=1200&q=85",
    time: 25,
    servings: 2,
    difficulty: "Orta",
    calories: 480,
    chefNote:
      "Bonfileyi pişirmeden en az 20 dakika önce dolaptan çıkarıp oda sıcaklığına getirin. Döküm tava duman çıkaracak kadar kızgın olmalı; her iki yüzünü 3'er dakika mühürledikten sonra tereyağı ve biberiyeyi tavaya atıp kaşıkla etin üzerine gezdirin.",
    ingredients: [
      { name: "Dana Bonfile", amount: "400 g (2 kalın dilim)", query: "dana bonfile", defaultImg: "https://images.unsplash.com/photo-1603048588665-791ca8aea617?auto=format&fit=crop&w=200&q=80" },
      { name: "Tereyağı", amount: "2 yemek kaşığı (50 g)", query: "tereyağı", defaultImg: "https://images.unsplash.com/photo-1589985270826-4b7bb135bc9d?auto=format&fit=crop&w=200&q=80" },
      { name: "Taze Biberiye", amount: "3-4 dal", query: "biberiye", defaultImg: "https://images.unsplash.com/photo-1599818818579-a1b7346b9623?auto=format&fit=crop&w=200&q=80" },
      { name: "Taze Sarımsak", amount: "4 diş ezilmiş", query: "sarımsak", defaultImg: "https://images.unsplash.com/photo-1615477032219-bc1885ab4c3d?auto=format&fit=crop&w=200&q=80" },
      { name: "Sızma Zeytinyağı", amount: "2 yemek kaşığı", query: "sızma zeytinyağı", defaultImg: "https://images.unsplash.com/photo-1474979266404-7eaacbcd87c5?auto=format&fit=crop&w=200&q=80" },
      { name: "Deniz Tuzu & Karabiber", amount: "1 çay kaşığı taze çekilmiş", query: "karabiber", defaultImg: "https://images.unsplash.com/photo-1509358271058-acd22cc93898?auto=format&fit=crop&w=200&q=80" },
    ],
    steps: [
      "Bonfile dilimlerini kağıt havlu ile kurulayın. Her iki yüzünü zeytinyağı, iri deniz tuzu ve taze çekilmiş karabiber ile ovun.",
      "Ağır tabanlı döküm tavayı yüksek ateşte 4-5 dakika iyice kızdırın.",
      "Etleri tavaya bırakın ve oynatmadan her yüzünü 3-3.5 dakika mühürleyin (orta pişmiş için).",
      "Ocağın altını orta dereceye alın; tavaya tereyağını, ezilmiş kabuklu sarımsakları ve taze biberiyeyi ekleyin.",
      "Eriyen köpüklü tereyağını kaşıkla etlerin üzerine 1-2 dakika boyunca sürekli gezdirin (Arrosé tekniği).",
      "Eti kesme tahtasına alıp dilimlemeden önce 5 dakika dinlendirin; suyunu içine hapsetmesi için dinlendirme çok önemlidir.",
    ],
  },
  {
    id: "kuskonmazli-somon-fileto",
    collection: "gurme-lezzetler",
    title: "Kuşkonmazlı Fırın Somon Fileto",
    subtitle: "Limon dilimleri, taze dereotu ve sızma zeytinyağı sosuyla çıtır derili fırın somon",
    category: "Et & Balık",
    image: "https://images.unsplash.com/photo-1519708227418-c8fd9a32b7a2?auto=format&fit=crop&w=1200&q=85",
    time: 25,
    servings: 2,
    difficulty: "Kolay",
    calories: 420,
    chefNote:
      "Somonun kurumasını önlemek için fırında 14-16 dakikadan fazla tutmayın. İçi hafif pembe ve sulu kaldığında lezzeti zirvededir.",
    ingredients: [
      { name: "Somon Fileto", amount: "400 g (2 porsiyon)", query: "somon", defaultImg: "https://images.unsplash.com/photo-1519708227418-c8fd9a32b7a2?auto=format&fit=crop&w=200&q=80" },
      { name: "Taze Kuşkonmaz", amount: "1 demet (250 g)", query: "kuşkonmaz", defaultImg: "https://images.unsplash.com/photo-1515471204630-d33633630f78?auto=format&fit=crop&w=200&q=80" },
      { name: "Limon", amount: "1 adet ince dilimlenmiş", query: "limon", defaultImg: "https://images.unsplash.com/photo-1533038590840-1cde6e668a91?auto=format&fit=crop&w=200&q=80" },
      { name: "Sızma Zeytinyağı", amount: "3 yemek kaşığı", query: "sızma zeytinyağı", defaultImg: "https://images.unsplash.com/photo-1474979266404-7eaacbcd87c5?auto=format&fit=crop&w=200&q=80" },
      { name: "Taze Dereotu", amount: "Yarım demet ince kıyılmış", query: "dereotu", defaultImg: "https://images.unsplash.com/photo-1599818818579-a1b7346b9623?auto=format&fit=crop&w=200&q=80" },
      { name: "Sarımsak", amount: "2 diş ezilmiş", query: "sarımsak", defaultImg: "https://images.unsplash.com/photo-1615477032219-bc1885ab4c3d?auto=format&fit=crop&w=200&q=80" },
    ],
    steps: [
      "Fırını 200°C'ye ısıtın. Fırın tepsisine pişirme kağıdı serin.",
      "Kuşkonmazların sert alt kısımlarını kırıp tepsiye dizin, 1 kaşık zeytinyağı ve tuz ile harmanlayın.",
      "Somon filetoları kurulayıp kuşkonmazların yanına yerleştirin.",
      "Kalan zeytinyağı, ezilmiş sarımsak, tuz ve karabiberi karıştırıp somonların üzerine sürün. Limon dilimlerini etin üzerine yerleştirin.",
      "Fırında 15 dakika pişirin. Fırından çıkınca üzerine taze dereotu serpiştirip sıcak servis yapın.",
    ],
  },
  {
    id: "ozel-kuzu-pirzola",
    collection: "gurme-lezzetler",
    title: "Taze Baharatlı Fırın Kuzu Pirzola",
    subtitle: "Taze kekik marinasyonu, çeri domates ve fırınlanmış bebek patates ile gurme sunum",
    category: "Et & Balık",
    image: "https://images.unsplash.com/photo-1603360946369-dc9bb6258143?auto=format&fit=crop&w=1200&q=85",
    time: 35,
    servings: 3,
    difficulty: "Orta",
    calories: 540,
    chefNote:
      "Pirzolaları fırınlamadan önce tavada 1'er dakika mühürlemek etin suyunu hapseder ve fırında lokum gibi yumuşacık kalmasını sağlar.",
    ingredients: [
      { name: "Kuzu Pirzola", amount: "6 adet (500 g)", query: "kuzu pirzola", defaultImg: "https://images.unsplash.com/photo-1603048588665-791ca8aea617?auto=format&fit=crop&w=200&q=80" },
      { name: "Bebek Patates", amount: "8 adet ortadan ikiye kesilmiş", query: "patates", defaultImg: "https://images.unsplash.com/photo-1518977676601-b53f82aba655?auto=format&fit=crop&w=200&q=80" },
      { name: "Çeri Domates", amount: "10-12 adet salkım", query: "çeri domates", defaultImg: "https://images.unsplash.com/photo-1592924357228-91a4daadcfea?auto=format&fit=crop&w=200&q=80" },
      { name: "Taze Kekik & Biberiye", amount: "1'er yemek kaşığı kıyılmış", query: "kekik", defaultImg: "https://images.unsplash.com/photo-1599818818579-a1b7346b9623?auto=format&fit=crop&w=200&q=80" },
      { name: "Sızma Zeytinyağı", amount: "3 yemek kaşığı", query: "sızma zeytinyağı", defaultImg: "https://images.unsplash.com/photo-1474979266404-7eaacbcd87c5?auto=format&fit=crop&w=200&q=80" },
      { name: "Sarımsak", amount: "3 diş ezilmiş", query: "sarımsak", defaultImg: "https://images.unsplash.com/photo-1615477032219-bc1885ab4c3d?auto=format&fit=crop&w=200&q=80" },
    ],
    steps: [
      "Pirzolaları zeytinyağı, sarımsak, taze kekik, biberiye ve karabiberle harmanlayıp 15 dakika dinlendirin.",
      "Bebek patatesleri zeytinyağı ve tuzla karıştırıp 200°C fırında 15 dakika önden pişirin.",
      "Kızgın tavada pirzolaların iki yüzünü 1'er dakika hızlıca mühürleyin.",
      "Mühürlenen pirzolaları fırın tepsisine patateslerin yanına alın, çeri domatesleri ekleyin.",
      "200°C fırında 12-14 dakika daha pişirip sıcak servis yapın.",
    ],
  },
  {
    id: "truflu-mantarli-tagliatelle",
    collection: "gurme-lezzetler",
    title: "Trüf Yağlı & Porçini Mantarlı Tagliatelle",
    subtitle: "Rendelenmiş parmesan, taze krema ve siyah trüf esansıyla İtalyan restoran deneyimi",
    category: "Makarna",
    image: "https://images.unsplash.com/photo-1546549032-9571cd6b27df?auto=format&fit=crop&w=1200&q=85",
    time: 20,
    servings: 2,
    difficulty: "Kolay",
    calories: 510,
    chefNote:
      "Makarnayı süzdüğünüz sudan yarım çay bardağı ayırıp sosa ekleyin; nişastalı makarna suyu sosun kremamsı ve parlak olmasını sağlar.",
    ingredients: [
      { name: "Tagliatelle veya Yassı Makarna", amount: "250 g", query: "makarna", defaultImg: "https://images.unsplash.com/photo-1551183053-bf91a1d81141?auto=format&fit=crop&w=200&q=80" },
      { name: "Kültür / Kestane Mantarı", amount: "300 g ince dilimlenmiş", query: "mantar", defaultImg: "https://images.unsplash.com/photo-1504674900247-0877df9cc836?auto=format&fit=crop&w=200&q=80" },
      { name: "Sıvı Krema", amount: "200 ml (%35 yağlı)", query: "krema", defaultImg: "https://images.unsplash.com/photo-1550583724-b2692b85b150?auto=format&fit=crop&w=200&q=80" },
      { name: "Eski Kaşar veya Parmesan", amount: "1 çay bardağı rendelenmiş", query: "eski kaşar", defaultImg: "https://images.unsplash.com/photo-1452195100486-9cc805987862?auto=format&fit=crop&w=200&q=80" },
      { name: "Tereyağı", amount: "2 yemek kaşığı", query: "tereyağı", defaultImg: "https://images.unsplash.com/photo-1589985270826-4b7bb135bc9d?auto=format&fit=crop&w=200&q=80" },
      { name: "Taze Sarımsak", amount: "2 diş ince kıyılmış", query: "sarımsak", defaultImg: "https://images.unsplash.com/photo-1615477032219-bc1885ab4c3d?auto=format&fit=crop&w=200&q=80" },
    ],
    steps: [
      "Geniş tencerede bol tuzlu su kaynatın, tagliatelle'leri 'al dente' kıvamında haşlayıp süzün (haşlama suyundan yarım bardak ayırın).",
      "Geniş tavada tereyağını eritin. Mantarları yüksek ateşte 5 dakika suyunu salıp çekene dek soteleyin.",
      "Sarımsakları ilave edip 30 saniye çevirin. Sıvı kremayı ve ayırdığınız makarna suyunu ekleyin.",
      "Sos hafif koyulaşınca makarnayı tavaya ekleyin ve rendelenmiş peynirle 1 dakika harmanlayın.",
      "Tabaklara paylaştırıp üzerine taze çekilmiş karabiber ve ekstra peynir serperek servis edin.",
    ],
  },

  /* =========================================================
   * ŞEF TAVSİYESİ (KLASİK / EV LEZZETLERİ)
   * ========================================================= */
  {
    id: "kremali-mantarli-makarna",
    collection: "sef-tavsiyesi",
    title: "Kremalı Mantarlı Penne",
    subtitle: "20 dakikada sofrada restoran lezzeti, bol kaşarlı ve taze mantarlı",
    category: "Makarna",
    image: "/images/recipes/kremali-makarna.jpg",
    time: 20,
    servings: 3,
    difficulty: "Kolay",
    calories: 490,
    chefNote:
      "Mantarları kısık ateşte değil, yüksek ateşte tavayı kalabalık etmeden kavurun; suyunu bırakmadan kızarmaları lezzetin sırrı.",
    ingredients: [
      { name: "Penne Makarna", amount: "350 g (yarım paket)", query: "penne makarna", defaultImg: "https://images.unsplash.com/photo-1551183053-bf91a1d81141?auto=format&fit=crop&w=200&q=80" },
      { name: "Taze Mantar", amount: "250 g dilimlenmiş", query: "mantar", defaultImg: "https://images.unsplash.com/photo-1504674900247-0877df9cc836?auto=format&fit=crop&w=200&q=80" },
      { name: "Yemeklik Krema", amount: "200 ml", query: "krema", defaultImg: "https://images.unsplash.com/photo-1550583724-b2692b85b150?auto=format&fit=crop&w=200&q=80" },
      { name: "Tereyağı", amount: "1 yemek kaşığı", query: "tereyağı", defaultImg: "https://images.unsplash.com/photo-1589985270826-4b7bb135bc9d?auto=format&fit=crop&w=200&q=80" },
      { name: "Taze Sarımsak", amount: "2 diş ince kıyılmış", query: "sarımsak", defaultImg: "https://images.unsplash.com/photo-1615477032219-bc1885ab4c3d?auto=format&fit=crop&w=200&q=80" },
      { name: "Kaşar Peyniri", amount: "1 su bardağı rendelenmiş", query: "kaşar peyniri", defaultImg: "https://images.unsplash.com/photo-1452195100486-9cc805987862?auto=format&fit=crop&w=200&q=80" },
    ],
    steps: [
      "Makarnayı bol tuzlu kaynar suda paket süresinden 1 dakika az haşlayın, süzün.",
      "Geniş tavada tereyağını eritin, dilimlenmiş mantarları yüksek ateşte 5-6 dakika kavurun.",
      "İnce doğranmış sarımsağı ekleyip 30 saniye çevirin.",
      "Kremayı ilave edin, tuz ve karabiber ekleyip kaynama noktasına gelince ateşi kısın.",
      "Makarnayı tavaya dökün, rendelenmiş kaşarı serpip 2 dakika karıştırarak sıcak servis yapın.",
    ],
  },
  {
    id: "mercimek-corbasi",
    collection: "sef-tavsiyesi",
    title: "Süzme Kırmızı Mercimek Çorbası",
    subtitle: "Tereyağlı pul biber sosu ve limonla geleneksel lokanta usulü mercimek çorbası",
    category: "Çorba",
    image: "/images/recipes/mercimek-corbasi.jpg",
    time: 30,
    servings: 5,
    difficulty: "Kolay",
    calories: 210,
    chefNote:
      "Soğanı tereyağında hafif pembeleşene kadar kavurmak çorbaya tatlı bir derinlik katar. Limonu mutlaka servis anında sıkın.",
    ingredients: [
      { name: "Kırmızı Mercimek", amount: "1,5 su bardağı", query: "kırmızı mercimek", defaultImg: "https://images.unsplash.com/photo-1585994192701-f254e4e6d36e?auto=format&fit=crop&w=200&q=80" },
      { name: "Kuru Soğan", amount: "1 adet yemeklik doğranmış", query: "kuru soğan", defaultImg: "https://images.unsplash.com/photo-1508747703725-719777637510?auto=format&fit=crop&w=200&q=80" },
      { name: "Taze Havuç", amount: "1 adet rendelenmiş", query: "havuç", defaultImg: "https://images.unsplash.com/photo-1598170845058-32b9d6a5da37?auto=format&fit=crop&w=200&q=80" },
      { name: "Patates", amount: "1 adet küçük boy", query: "patates", defaultImg: "https://images.unsplash.com/photo-1518977676601-b53f82aba655?auto=format&fit=crop&w=200&q=80" },
      { name: "Tereyağı", amount: "2 yemek kaşığı", query: "tereyağı", defaultImg: "https://images.unsplash.com/photo-1589985270826-4b7bb135bc9d?auto=format&fit=crop&w=200&q=80" },
      { name: "Taze Limon", amount: "1 adet", query: "limon", defaultImg: "https://images.unsplash.com/photo-1533038590840-1cde6e668a91?auto=format&fit=crop&w=200&q=80" },
    ],
    steps: [
      "Mercimeği bol suyla yıkayıp süzün; soğan, havuç ve patatesi doğrayın.",
      "Tencerede tereyağını eritip soğanı pembeleşene kadar kavurun. Havuç ve patatesi ekleyin.",
      "Mercimeği ve 6 su bardağı sıcak suyu ekleyip orta ateşte 20-25 dakika sebzeler yumuşayana dek kaynatın.",
      "Çorbayı pürüzsüz olana kadar el blenderından geçirin; tuz ve kimyonla tatlandırın.",
      "Tereyağında hafifçe kızdırılmış pul biberi üzerine gezdirip limon dilimleriyle sıcak servis yapın.",
    ],
  },
  {
    id: "menemen",
    collection: "sef-tavsiyesi",
    title: "Geleneksel Tavada Menemen",
    subtitle: "Taze köy yumurtası, bol domates ve tatlı sivri biberle kahvaltı klasiği",
    category: "Kahvaltı",
    image: "/images/recipes/menemen.jpg",
    time: 15,
    servings: 2,
    difficulty: "Kolay",
    calories: 320,
    chefNote:
      "Yumurtaları kırdıktan sonra fazla çırpmayın; beyazı ve sarısı hafif ayrı kalacak şekilde kısık ateşte pişirip tavada sulu bırakın.",
    ingredients: [
      { name: "Köy Yumurtası", amount: "4 adet", query: "yumurta", defaultImg: "https://images.unsplash.com/photo-1506976785307-8732e854ad03?auto=format&fit=crop&w=200&q=80" },
      { name: "Salkım Domates", amount: "3 adet rendelenmiş veya küp", query: "domates", defaultImg: "https://images.unsplash.com/photo-1592924357228-91a4daadcfea?auto=format&fit=crop&w=200&q=80" },
      { name: "Sivri Biber", amount: "3 adet ince doğranmış", query: "sivri biber", defaultImg: "https://images.unsplash.com/photo-1563565375-f3fdfdbefa83?auto=format&fit=crop&w=200&q=80" },
      { name: "Tereyağı", amount: "1.5 yemek kaşığı", query: "tereyağı", defaultImg: "https://images.unsplash.com/photo-1589985270826-4b7bb135bc9d?auto=format&fit=crop&w=200&q=80" },
      { name: "Beyaz Peynir", amount: "60 g uvalanmış", query: "beyaz peynir", defaultImg: "https://images.unsplash.com/photo-1452195100486-9cc805987862?auto=format&fit=crop&w=200&q=80" },
    ],
    steps: [
      "Biberleri ince ince doğrayıp tavada eritilmiş tereyağında 2-3 dakika soteleyin.",
      "Kabukları soyulup doğranmış domatesleri ekleyin; suyunu çekene kadar orta ateşte 6-8 dakika pişirin.",
      "Tuz, pul biber ve karabiber serpiştirin.",
      "Yumurtaları kırın; sarılarını hafifçe dağıtarak beyazları pişene kadar kısık ateşte 2 dakika tutun.",
      "İsteğe göre üzerine beyaz peynir serpip sıcak sıcak tavada servis edin.",
    ],
  },
  {
    id: "firinda-tavuk-baget",
    collection: "sef-tavsiyesi",
    title: "Fırında Çıtır Baharatlı Tavuk Baget",
    subtitle: "Yoğurtlu özel marinasyon sosu ve baharatlı fırın patatesle altın sarısı lezzet",
    category: "Ana Yemek",
    image: "/images/recipes/firinda-tavuk-baget.jpg",
    time: 50,
    servings: 4,
    difficulty: "Kolay",
    calories: 440,
    chefNote:
      "Tavukları sosta en az 30 dakika dinlendirin. Patatesleri aynı tepside tavukların altına yayarak tavuğun lezzetli suyunu çekmelerini sağlayın.",
    ingredients: [
      { name: "Tavuk Baget", amount: "8 adet (yaklaşık 800 g)", query: "tavuk baget", defaultImg: "https://images.unsplash.com/photo-1598515214211-89d3c73ae83b?auto=format&fit=crop&w=200&q=80" },
      { name: "Taze Patates", amount: "4 adet elma dilim", query: "patates", defaultImg: "https://images.unsplash.com/photo-1518977676601-b53f82aba655?auto=format&fit=crop&w=200&q=80" },
      { name: "Domates Salçası", amount: "1 yemek kaşığı", query: "domates salçası", defaultImg: "https://images.unsplash.com/photo-1592924357228-91a4daadcfea?auto=format&fit=crop&w=200&q=80" },
      { name: "Süzme Yoğurt", amount: "2 yemek kaşığı", query: "yoğurt", defaultImg: "https://images.unsplash.com/photo-1550583724-b2692b85b150?auto=format&fit=crop&w=200&q=80" },
      { name: "Sızma Zeytinyağı", amount: "3 yemek kaşığı", query: "sızma zeytinyağı", defaultImg: "https://images.unsplash.com/photo-1474979266404-7eaacbcd87c5?auto=format&fit=crop&w=200&q=80" },
      { name: "Sarımsak", amount: "3 diş ezilmiş", query: "sarımsak", defaultImg: "https://images.unsplash.com/photo-1615477032219-bc1885ab4c3d?auto=format&fit=crop&w=200&q=80" },
    ],
    steps: [
      "Yoğurt, salça, zeytinyağı, ezilmiş sarımsak, kekik, toz kırmızı biber, tuz ve karabiberi çırparak sos hazırlayın.",
      "Tavuk bagetlerin üzerine bıçakla hafif çizikler atın ve sosa bulayıp 30 dakika buzdolabında bekletin.",
      "Elma dilim patatesleri tepsiye serin; marine tavukları üzerine dizin.",
      "200°C önceden ısıtılmış fırında üzeri altın sarısı ve çıtır olana kadar yaklaşık 40-45 dakika pişirin.",
      "Sıcak servis yapın.",
    ],
  },
  {
    id: "sutlac",
    collection: "sef-tavsiyesi",
    title: "Geleneksel Fırın Sütlaç",
    subtitle: "Közlenmiş üzeri ve kadifemsi kıvamıyla toprak güveçte pişen geleneksel lezzet",
    category: "Tatlı",
    image: "/images/recipes/sutlac.jpg",
    time: 50,
    servings: 6,
    difficulty: "Orta",
    calories: 260,
    chefNote:
      "Güveç kaplarını derin bir fırın tepsisine dizin ve tepsiye kapların yarısına gelecek kadar soğuk su doldurun; böylece fırında kuruyup yanmadan üstü nar gibi kızarır.",
    ingredients: [
      { name: "Tam Yağlı Süt", amount: "1 Litre", query: "süt", defaultImg: "https://images.unsplash.com/photo-1550583724-b2692b85b150?auto=format&fit=crop&w=200&q=80" },
      { name: "Pirinç", amount: "Yarım su bardağı yıkanmış", query: "pirinç", defaultImg: "https://images.unsplash.com/photo-1585994192701-f254e4e6d36e?auto=format&fit=crop&w=200&q=80" },
      { name: "Toz Şeker", amount: "1 su bardağı", query: "toz şeker", defaultImg: "https://images.unsplash.com/photo-1589985270826-4b7bb135bc9d?auto=format&fit=crop&w=200&q=80" },
      { name: "Mısır Nişastası", amount: "2 yemek kaşığı", query: "nişasta", defaultImg: "https://images.unsplash.com/photo-1509358271058-acd22cc93898?auto=format&fit=crop&w=200&q=80" },
      { name: "Şekerli Vanilin", amount: "1 paket", query: "vanilin", defaultImg: "https://images.unsplash.com/photo-1509358271058-acd22cc93898?auto=format&fit=crop&w=200&q=80" },
    ],
    steps: [
      "Pirinçleri 1.5 su bardağı suyla yumuşayıp suyunu çekene kadar haşlayın.",
      "Sütü tencereye ekleyin, kaynamaya başlayınca şekeri ilave edip 10 dakika kısık ateşte pişirin.",
      "Nişastayı yarım çay bardağı soğuk suyla açıp karıştırarak tencereye ilave edin. 3 dakika koyulaşana kadar karıştırın, vanilini ekleyip ocaktan alın.",
      "Sütlacı fırına dayanıklı güveç kaplarına paylaştırın. Fırın tepsisine soğuk su döküp kapları içine yerleştirin.",
      "220°C fırının üst ızgarasında üzeri kabarcıklanıp kızarana kadar 10-12 dakika pişirin. Soğuk servis yapın.",
    ],
  },
  {
    id: "ispanakli-borek",
    collection: "sef-tavsiyesi",
    title: "El Açması Lezzetinde Ispanaklı Börek",
    subtitle: "Taze ıspanak, beyaz peynir ve çıtır yufkayla altın sarısı tepsi böreği",
    category: "Kahvaltı",
    image: "/images/recipes/ispanakli-borek.jpg",
    time: 45,
    servings: 6,
    difficulty: "Orta",
    calories: 310,
    chefNote:
      "Ispanağı pişirmeden, tuzla iyice ovup suyunu sıkarak iç harca ekleyin. Böylece börek fırında hamurlaşmaz, çıtır kalır.",
    ingredients: [
      { name: "Taze Yufka", amount: "4 adet", query: "yufka", defaultImg: "https://images.unsplash.com/photo-1509358271058-acd22cc93898?auto=format&fit=crop&w=200&q=80" },
      { name: "Taze Ispanak", amount: "500 g temizlenmiş", query: "ıspanak", defaultImg: "https://images.unsplash.com/photo-1576045057995-568f588f82fb?auto=format&fit=crop&w=200&q=80" },
      { name: "Beyaz Peynir", amount: "200 g ezilmiş", query: "beyaz peynir", defaultImg: "https://images.unsplash.com/photo-1452195100486-9cc805987862?auto=format&fit=crop&w=200&q=80" },
      { name: "Köy Yumurtası", amount: "2 adet", query: "yumurta", defaultImg: "https://images.unsplash.com/photo-1506976785307-8732e854ad03?auto=format&fit=crop&w=200&q=80" },
      { name: "Tam Yağlı Süt", amount: "1 su bardağı", query: "süt", defaultImg: "https://images.unsplash.com/photo-1550583724-b2692b85b150?auto=format&fit=crop&w=200&q=80" },
      { name: "Sıvı Yağ", amount: "Yarım su bardağı", query: "sıvı yağ", defaultImg: "https://images.unsplash.com/photo-1474979266404-7eaacbcd87c5?auto=format&fit=crop&w=200&q=80" },
    ],
    steps: [
      "Ispanakları ince kıyıp tuzla ovun, çıkan suyunu iki elinizle iyice sıkın ve ufalanmış peynirle karıştırın.",
      "Sos için yumurta, süt ve sıvı yağı çırpın.",
      "Tepsiyi yağlayıp ilk yufkayı kenarlardan taşacak şekilde serin. Üzerine sos gezdirin.",
      "Yufka katlarının arasına sos ve ıspanaklı harcı paylaştırarak katlayın.",
      "Kalan sosu en üste sürüp 180°C fırında üzeri altın sarısı kızarana dek 35-40 dakika pişirin.",
    ],
  },
];

export const getRecipeById = (id) =>
  RECIPES.find((r) => String(r.id) === String(id)) || null;
