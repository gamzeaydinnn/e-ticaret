# Requirements Document

## Introduction

Bu doküman, e-ticaret platformu ana sayfasında kategori navigasyonunu iyileştirmek için kategori megamenu/popup özelliğinin gereksinimlerini tanımlar. Kullanıcılar bir kategorinin (örneğin "Temizlik") üzerine geldiğinde veya tıkladığında, o kategoriye ait alt kategoriler (saç bakım, ev temizliği vb.) modern ve kullanıcı dostu bir popup içinde gösterilecektir. Bu özellik hem mobil hem de desktop platformlarda sorunsuz çalışmalıdır.

## Glossary

- **System**: Category Megamenu Popup özelliği - Ana sayfa kategori navigasyonu için popup/megamenu bileşeni
- **Kategori_Kartı**: Ana sayfada gösterilen kategori görseli ve adını içeren tıklanabilir UI bileşeni
- **Megamenu**: Bir kategori üzerine gelindiğinde veya tıklandığında açılan, alt kategorileri gösteren popup/overlay bileşeni
- **Alt_Kategori**: Bir ana kategorinin altında bulunan kategori (örneğin "Temizlik" kategorisi altında "Saç Bakım")
- **Desktop_Modu**: Ekran genişliği 768px ve üzeri olan cihazlar
- **Mobil_Modu**: Ekran genişliği 768px altında olan cihazlar
- **Hover_Etkileşimi**: Fare imlecinin bir UI bileşeni üzerinde durması
- **Kategori_API**: Backend'den kategori verilerini getiren REST API endpoint'i

## Requirements

### Requirement 1: Alt Kategori Verilerinin Yüklenmesi

**User Story:** As a kullanıcı, I want kategorilerin alt kategorileriyle birlikte yüklenmesini, so that megamenu'de alt kategorileri görebilirim.

#### Acceptance Criteria

1. WHEN Ana_Sayfa yüklendiğinde, THE System SHALL kategorileri alt kategorileri (subCategories) ile birlikte Kategori_API'den almalıdır
2. THE System SHALL sadece aktif (isActive=true) kategorileri ve alt kategorileri göstermelidir
3. IF bir kategorinin alt kategorisi yoksa (subCategories.length === 0), THEN THE System SHALL o kategori için megamenu göstermemelidir
4. THE System SHALL yükleme sırasında kullanıcıya loading göstergesi sunmalıdır
5. IF Kategori_API çağrısı başarısız olursa, THEN THE System SHALL kullanıcıya anlaşılır bir hata mesajı göstermelidir

### Requirement 2: Desktop Hover Etkileşimi

**User Story:** As a desktop kullanıcı, I want bir kategori üzerine geldiğimde alt kategorileri görmek, so that hızlı bir şekilde ilgilendiğim alt kategoriye ulaşabilirim.

#### Acceptance Criteria

1. WHILE Desktop_Modu aktifken, WHEN kullanıcı fare imlecini Kategori_Kartı üzerine getirdiğinde, THE System SHALL 200ms içinde Megamenu'yu göstermelidir
2. WHILE Megamenu açıkken, WHEN kullanıcı fare imlecini Kategori_Kartı veya Megamenu dışına çıkardığında, THE System SHALL 300ms gecikme ile Megamenu'yu kapatmalıdır
3. WHILE kullanıcı fare imleci Megamenu içindeyken, THE System SHALL Megamenu'yu açık tutmalıdır
4. THE System SHALL Megamenu'nun Kategori_Kartı'nın altında veya üzerinde (ekran konumuna göre) düzgün konumlanmasını sağlamalıdır
5. THE System SHALL Megamenu'nun ekran sınırlarını aşmamasını sağlamalıdır (viewport bounds kontrolü)

### Requirement 3: Mobil Tıklama Etkileşimi

**User Story:** As a mobil kullanıcı, I want bir kategoriye tıkladığımda alt kategorileri görmek, so that mobil cihazımda rahatlıkla kategoriler arası gezinebilirim.

#### Acceptance Criteria

1. WHILE Mobil_Modu aktifken, WHEN kullanıcı Kategori_Kartı'na tıkladığında, THE System SHALL Megamenu'yu göstermelidir
2. WHILE Megamenu açıkken, WHEN kullanıcı Megamenu dışındaki bir alana tıkladığında, THE System SHALL Megamenu'yu kapatmalıdır
3. WHILE Megamenu açıkken, WHEN kullanıcı kapatma (X) butonuna tıkladığında, THE System SHALL Megamenu'yu kapatmalıdır
4. THE System SHALL aynı anda sadece bir Megamenu'nun açık olmasını sağlamalıdır
5. THE System SHALL Megamenu'yu modal/overlay olarak göstermelidir (arka plan karartma ile)

### Requirement 4: Megamenu İçerik Gösterimi

**User Story:** As a kullanıcı, I want megamenu içinde alt kategorileri düzenli ve okunabilir görmek, so that istediğim alt kategoriye kolayca tıklayabilirim.

#### Acceptance Criteria

1. THE System SHALL her alt kategoriyi tıklanabilir bir liste öğesi olarak göstermelidir
2. THE System SHALL alt kategori adlarını kategori nesnesinin "name" özelliğinden alarak göstermelidir
3. WHEN bir alt kategoriye tıklandığında, THE System SHALL kullanıcıyı o kategorinin sayfasına yönlendirmelidir (navigate: `/kategoriler/${slug}`)
4. THE System SHALL Megamenu'da en fazla 2 sütun halinde alt kategorileri düzenlemelidir (desktop'ta)
5. WHILE Mobil_Modu aktifken, THE System SHALL alt kategorileri tek sütun halinde göstermelidir

### Requirement 5: Megamenu Görsel Tasarım ve Animasyon

**User Story:** As a kullanıcı, I want megamenu'nun modern ve akıcı animasyonlarla açılıp kapanmasını, so that profesyonel bir alışveriş deneyimi yaşayabilirim.

#### Acceptance Criteria

1. THE System SHALL Megamenu'nun açılırken opacity (0 → 1) ve transform (translateY(-10px → 0)) animasyonlarını kullanmalıdır
2. THE System SHALL animasyon süresini 200ms olarak ayarlamalıdır
3. THE System SHALL Megamenu arka plan rengini beyaz (#ffffff) ve gölge efekti (box-shadow) ile göstermelidir
4. THE System SHALL Megamenu köşelerini 8px border-radius ile yuvarlaklamalıdır
5. THE System SHALL her alt kategori öğesinin üzerine gelindiğinde (hover) arka plan rengini açık gri (#f3f4f6) yapmalıdır
6. THE System SHALL Megamenu'nun z-index değerini 1000 olarak ayarlayarak diğer içeriklerin üstünde görünmesini sağlamalıdır

### Requirement 6: Responsive Tasarım ve Ekran Uyumluluğu

**User Story:** As a kullanıcı, I want megamenu'nun tüm ekran boyutlarında düzgün görünmesini, so that hangi cihazdan alışveriş yaparsam yapayım sorunsuz kullanabilirim.

#### Acceptance Criteria

1. THE System SHALL 768px ve üzeri ekranlarda Desktop_Modu'nu aktif etmelidir
2. THE System SHALL 768px altı ekranlarda Mobil_Modu'nu aktif etmelidir
3. WHILE Desktop_Modu aktifken, THE System SHALL Megamenu genişliğini minimum 280px, maksimum 400px olarak ayarlamalıdır
4. WHILE Mobil_Modu aktifken, THE System SHALL Megamenu genişliğini ekran genişliğinin %90'ı olarak ayarlamalıdır
5. THE System SHALL Megamenu yüksekliğinin ekran yüksekliğinin %80'ini aşmamasını sağlamalıdır
6. IF Megamenu içeriği ekran yüksekliğini aşarsa, THEN THE System SHALL içeriği scroll edilebilir yapmalıdır (overflow-y: auto)

### Requirement 7: Klavye Erişilebilirliği ve Accessibility

**User Story:** As a klavye kullanan kullanıcı, I want megamenu'yu klavye ile kontrol edebilmeyi, so that fareye ihtiyaç duymadan kategoriler arası gezinebilirim.

#### Acceptance Criteria

1. WHEN kullanıcı Tab tuşu ile Kategori_Kartı'na odaklandığında, THE System SHALL Kategori_Kartı etrafında görünür bir focus ring göstermelidir
2. WHEN Kategori_Kartı'na odaklanılmışken Enter veya Space tuşuna basıldığında, THE System SHALL Megamenu'yu açmalıdır
3. WHEN Megamenu açıkken Escape tuşuna basıldığında, THE System SHALL Megamenu'yu kapatmalıdır
4. THE System SHALL Megamenu içindeki alt kategoriler arasında Tab tuşu ile geçiş yapılabilmesini sağlamalıdır
5. THE System SHALL tüm interaktif öğelerde (kategori kartı, alt kategoriler, kapatma butonu) uygun ARIA etiketleri (aria-label, aria-expanded) kullanmalıdır

### Requirement 8: Performans ve Optimizasyon

**User Story:** As a kullanıcı, I want megamenu'nun hızlı açılıp kapanmasını, so that kategoriler arası gezinme deneyimim akıcı olsun.

#### Acceptance Criteria

1. THE System SHALL Megamenu açılırken re-render sayısını minimize etmek için React.memo veya useMemo kullanmalıdır
2. THE System SHALL kategori verilerini gereksiz yere tekrar yüklememelidir (caching veya state management)
3. THE System SHALL Megamenu DOM elementini conditional rendering ile sadece gerektiğinde render etmelidir
4. THE System SHALL hover debounce mekanizması kullanarak gereksiz açma/kapama işlemlerini önlemelidir
5. THE System SHALL Megamenu animasyonlarını CSS transitions ile gerçekleştirerek JavaScript animation overhead'ini azaltmalıdır

### Requirement 9: Alt Kategori Olmayan Kategoriler İçin Davranış

**User Story:** As a kullanıcı, I want alt kategorisi olmayan bir kategoriye tıkladığımda direkt o kategorinin sayfasına gitmek, so that gereksiz bir popup görmeden hızlıca ürünlere ulaşabilirim.

#### Acceptance Criteria

1. IF Kategori_Kartı'na tıklanıldığında ve kategorinin alt kategorisi yoksa (subCategories.length === 0), THEN THE System SHALL Megamenu'yu göstermeden kullanıcıyı kategorinin sayfasına yönlendirmelidir
2. THE System SHALL alt kategorisi olmayan kategorilerde hover efektini göstermemelidir (visual indicator)
3. IF bir kategorinin alt kategorileri varsa, THEN THE System SHALL Kategori_Kartı üzerinde küçük bir dropdown icon (chevron-down) göstermelidir
4. THE System SHALL alt kategori varlığı kontrolünü kategori verisi yüklendikten sonra yapmalıdır

### Requirement 10: Hata Yönetimi ve Fallback Davranışı

**User Story:** As a kullanıcı, I want bir hata oluştuğunda bile temel kategori navigasyonunun çalışmaya devam etmesini, so that alışveriş deneyimim kesintiye uğramasın.

#### Acceptance Criteria

1. IF alt kategori verisi yüklenirken bir hata oluşursa, THEN THE System SHALL kullanıcıyı ana kategorinin sayfasına yönlendirmelidir
2. IF Megamenu render edilirken bir hata oluşursa, THEN THE System SHALL React Error Boundary kullanarak uygulamanın çökmesini önlemelidir
3. THE System SHALL hata durumunda console'a detaylı hata logu yazmalıdır
4. IF kritik bir hata oluşursa, THEN THE System SHALL fallback olarak basit kategori listesi göstermelidir
5. THE System SHALL kullanıcıya gösterilen hata mesajlarını Türkçe olarak sunmalıdır
