# Requirements Document

## Introduction

Bu doküman, e-ticaret platformunun ana sayfasında bulunan tüm görsellerin (slider banner'lar, kategori blokları, promo banner'lar, ürün blok posterler) tıklanabilir hale getirilmesi, admin panelinden kapsamlı bir şekilde yönetilebilmesi, alt kategori desteğinin eklenmesi ve mobil uyumluluğun sağlanması için gerekli gereksinimleri tanımlar.

**Mevcut Durum:**

- Banner sistemi: Banner entity ile slider ve promo banner'lar mevcut, tıklanabilir (linkUrl), admin panelinde yönetilebilir
- Ana Sayfa Blok Sistemi: HomeProductBlock entity ile admin panelinden yönetilebilen ürün blokları var (manuel, category, discounted, newest, bestseller tipleri)
- Kategori Sistemi: Hiyerarşik kategori yapısı (parent-child) mevcut, ana sayfada CategoryTile bileşeni ile gösterilir
- HomeProductBlock'larda BannerId ve PosterImageUrl var ancak Banner'ın linkUrl'i blokta kullanılmıyor
- CategoryTile şu an statik veya tıklanabilir değil
- Alt kategori sistemi backend'de var ama admin paneli ve ana sayfa blokları alt kategori seçimini desteklemiyor

**Hedef:**
Ana sayfadaki tüm görsel elemanların (slider, kategori, promo, poster) tam tıklanabilir olması, admin panelinden kolay yönetimi, alt kategori desteği ve tam mobil uyumluluk.

## Glossary

- **System**: Ana sayfa görsel yönetim sistemi (Frontend + Backend)
- **Admin_Panel**: Admin kullanıcı arayüzü (AdminHomeBlocks.jsx)
- **Home_Page**: Müşterilerin gördüğü ana sayfa (Home.js)
- **Banner**: Slider ve promo görsellerini temsil eden entity
- **HomeProductBlock**: Ana sayfa ürün bloklarını temsil eden entity
- **Category**: Ürün kategorilerini temsil eden entity (hiyerarşik yapı)
- **CategoryTile**: Ana sayfada kategori kartlarını gösteren React bileşeni
- **Poster**: Ürün bloklarının sol tarafındaki banner/görsel
- **LinkUrl**: Tıklama hedef URL'i
- **TargetCategoryId**: Hedef kategori ID'si (ana veya alt kategori)
- **Subcategory**: Alt kategori (ParentId dolu olan Category)
- **Mobile_Interface**: Dokunmatik ekran uyumlu arayüz
- **Touch_Event**: Mobil cihaz dokunma olayları
- **Backend_API**: ASP.NET Core REST API endpoint'leri
- **Frontend_Component**: React bileşenleri

## Requirements

### Requirement 1: Banner Tıklanabilirlik ve Yönetim

**User Story:** Admin olarak, slider ve promo banner'ların tıklama hedeflerini yönetmek istiyorum, böylece müşteriler doğru sayfalara yönlendirilebilsin.

#### Acceptance Criteria

1. THE Banner SHALL preserve existing linkUrl field for click targets
2. WHEN a slider banner is clicked on Home_Page, THE System SHALL navigate to the banner's linkUrl
3. WHEN a promo banner is clicked on Home_Page, THE System SHALL navigate to the banner's linkUrl
4. WHEN linkUrl is empty or null, THE Banner SHALL not be clickable
5. THE Admin_Panel SHALL allow admins to edit banner linkUrl through existing banner management interface
6. FOR ALL banner types (slider, promo), THE System SHALL support absolute URLs (https://...) and relative URLs (/category/...)
7. WHEN a banner with linkUrl contains special characters, THE System SHALL properly encode the URL before navigation

### Requirement 2: Kategori Blok Tıklanabilirliği

**User Story:** Müşteri olarak, ana sayfada kategori görsellerine tıklayarak o kategoriye gitmek istiyorum, böylece ilgilendiğim ürünleri hızlıca bulabilirim.

#### Acceptance Criteria

1. WHEN a category tile is clicked on Home_Page, THE System SHALL navigate to /category/{category.slug}
2. WHEN a category has subcategories AND user hovers (desktop), THE CategoryTile SHALL display subcategory megamenu
3. WHEN a subcategory is clicked from megamenu, THE System SHALL navigate to /category/{subcategory.slug}
4. THE CategoryTile SHALL maintain existing keyboard navigation support (Enter, Space, Escape keys)
5. WHEN category imageUrl is null or empty, THE CategoryTile SHALL still be clickable using the category name area
6. THE System SHALL preserve existing mobile megamenu behavior (click opens modal overlay)

### Requirement 3: Ana Sayfa Ürün Blok Poster Tıklanabilirliği

**User Story:** Admin olarak, ana sayfa ürün bloklarındaki posterlerin tıklama hedeflerini belirlemek istiyorum, böylece müşteriler ilgili kampanya veya kategori sayfalarına yönlendirilebilsin.

#### Acceptance Criteria

1. THE HomeProductBlock entity SHALL add a new nullable field: PosterLinkUrl (string, max 500 chars)
2. WHEN HomeProductBlock.BannerId is not null, THE System SHALL use Banner.linkUrl as poster click target
3. WHEN HomeProductBlock.BannerId is null AND PosterLinkUrl is not null, THE System SHALL use PosterLinkUrl as poster click target
4. WHEN both BannerId and PosterLinkUrl are null or empty, THE Poster SHALL not be clickable
5. WHEN a poster is clicked on Home_Page, THE System SHALL navigate to the determined poster click target
6. THE Admin_Panel SHALL display a "Poster Tıklama Hedefi" input field in HomeProductBlock form
7. THE Admin_Panel SHALL show current poster link source (Banner linkUrl OR PosterLinkUrl OR "Hedef Yok")
8. WHEN admin selects a Banner with linkUrl, THE Admin_Panel SHALL display a preview of the Banner's linkUrl
9. THE Backend_API SHALL include PosterLinkUrl in CreateHomeBlockDto and UpdateHomeBlockDto
10. THE Backend_API SHALL validate PosterLinkUrl format (must start with http:// or https:// or /)

### Requirement 4: Alt Kategori Desteği - Ürün Blokları

**User Story:** Admin olarak, ana sayfa bloklarında sadece ana kategori değil alt kategori de seçebilmek istiyorum, böylece daha spesifik ürün grupları oluşturabilirim.

#### Acceptance Criteria

1. THE HomeProductBlock entity SHALL add a new nullable field: TargetCategoryId (int, foreign key to Category.Id)
2. THE System SHALL deprecate existing CategoryId field in favor of TargetCategoryId (migration strategy required)
3. WHEN BlockType is "category", THE System SHALL fetch products where Product.CategoryId equals TargetCategoryId
4. WHEN TargetCategoryId points to a parent category, THE System SHALL include products from that category AND all its subcategories
5. WHEN TargetCategoryId points to a subcategory, THE System SHALL include products only from that subcategory
6. THE Admin_Panel SHALL display a hierarchical category selector showing parent categories and their subcategories
7. THE Admin_Panel SHALL visually distinguish parent categories from subcategories (indentation or icon)
8. WHEN admin selects a subcategory, THE Admin_Panel SHALL display breadcrumb: "ParentName > SubcategoryName"
9. THE Backend_API SHALL include TargetCategoryId in CreateHomeBlockDto and UpdateHomeBlockDto
10. THE Backend_API SHALL validate TargetCategoryId exists and Category.IsActive is true
11. WHEN TargetCategoryId category is deleted or deactivated, THE System SHALL mark the HomeProductBlock as inactive

### Requirement 5: Alt Kategori Desteği - Manuel Ürün Seçimi

**User Story:** Admin olarak, manuel blok oluştururken hem ana kategori hem alt kategoriden ürün seçebilmek istiyorum, böylece esnek ürün kombinasyonları oluşturabilirim.

#### Acceptance Criteria

1. WHEN BlockType is "manual", THE Admin_Panel SHALL allow admins to search and select products from any category
2. THE Admin_Panel SHALL display product's full category path in product selection modal: "ParentCategory > Subcategory"
3. THE Admin_Panel SHALL support filtering products by parent category OR subcategory in product search
4. WHEN admin searches by category, THE System SHALL return products from that category AND all its subcategories
5. WHEN admin searches by subcategory, THE System SHALL return products only from that subcategory
6. THE Admin_Panel SHALL display selected products with their category breadcrumb
7. THE System SHALL allow mixing products from different categories and subcategories in a single manual block

### Requirement 6: Alt Kategori Desteği - Banner/Poster Hedefleme

**User Story:** Admin olarak, banner ve posterlerin alt kategorilere de link verebilmesini istiyorum, böylece daha hedefli kampanyalar oluşturabilirim.

#### Acceptance Criteria

1. THE Admin_Panel SHALL support autocomplete for category URLs when editing Banner.linkUrl or HomeProductBlock.PosterLinkUrl
2. WHEN admin types "/category/" in linkUrl field, THE System SHALL suggest available category slugs (parent and subcategory)
3. THE Admin_Panel SHALL display category type indicator (Ana Kategori / Alt Kategori) in autocomplete dropdown
4. WHEN a banner/poster links to a subcategory slug, THE System SHALL navigate correctly to that subcategory page
5. THE System SHALL validate category slugs in linkUrl against existing active categories (warning if invalid)

### Requirement 7: Mobil Uyumluluk - Dokunmatik Etkileşim

**User Story:** Mobil kullanıcı olarak, tüm tıklanabilir alanlara rahatlıkla dokunmak istiyorum, böylece mobil cihazda sorunsuz gezinebilirim.

#### Acceptance Criteria

1. THE System SHALL ensure all clickable elements (banners, posters, category tiles) have minimum touch target size of 44x44 pixels
2. WHEN user touches a banner on Mobile_Interface, THE System SHALL provide visual touch feedback (opacity change or scale)
3. WHEN user touches a poster on Mobile_Interface, THE System SHALL provide visual touch feedback
4. WHEN user touches a category tile on Mobile_Interface, THE System SHALL provide visual touch feedback
5. THE System SHALL prevent accidental double-tap zoom on clickable elements using CSS touch-action property
6. THE System SHALL handle touch events (touchstart, touchend) in addition to click events for better responsiveness
7. WHEN user performs a long press on clickable element, THE System SHALL not trigger navigation (prevent context menu on links)

### Requirement 8: Mobil Uyumluluk - Responsive Görsel Düzeni

**User Story:** Mobil kullanıcı olarak, banner ve posterların ekran boyutuma uygun görünmesini istiyorum, böylece görsel deneyim bozulmasın.

#### Acceptance Criteria

1. THE System SHALL serve responsive images for banners using appropriate aspect ratios per device type
2. WHEN viewport width is less than 768px, THE System SHALL display slider banners with 16:9 aspect ratio (mobile optimized)
3. WHEN viewport width is 768px or greater, THE System SHALL display slider banners with original aspect ratio
4. THE System SHALL apply CSS object-fit: cover to all banner and poster images to prevent distortion
5. THE System SHALL lazy-load banner and poster images to improve mobile page load performance
6. WHEN image fails to load, THE System SHALL display fallback background with category/block name text
7. THE System SHALL ensure promo banner grid uses responsive columns: 2 columns on mobile, 3+ on tablet/desktop

### Requirement 9: Mobil Uyumluluk - Kategori Grid Düzeni

**User Story:** Mobil kullanıcı olarak, kategori bloklarının düzenli grid halinde görünmesini istiyorum, böylece kolayca kategorilere erişebilirim.

#### Acceptance Criteria

1. WHEN viewport width is less than 576px, THE System SHALL display category tiles in 3 columns grid
2. WHEN viewport width is between 576px and 768px, THE System SHALL display category tiles in 4 columns grid
3. WHEN viewport width is 768px or greater, THE System SHALL display category tiles in auto-fill grid (minmax(110px, 1fr))
4. THE System SHALL maintain equal spacing (gap: 10px) between category tiles across all viewport sizes
5. THE System SHALL ensure category tile text remains readable on small screens (minimum font-size: 0.7rem)
6. WHEN category has no image, THE System SHALL display colored placeholder square maintaining grid alignment

### Requirement 10: Admin Panel - Kapsamlı URL Yönetimi

**User Story:** Admin olarak, tüm banner ve blokların hedef URL'lerini tek bir yerden görüp düzenlemek istiyorum, böylece kampanya yönetimi kolaylaşsın.

#### Acceptance Criteria

1. THE Admin_Panel SHALL display current click targets for all banners in banner list table (new column: "Hedef URL")
2. THE Admin_Panel SHALL display current click targets for all home blocks in block list table (new column: "Poster Hedefi")
3. WHEN banner or block has no link target, THE Admin_Panel SHALL display "Hedef Yok" badge with warning color
4. WHEN banner or block has link target, THE Admin_Panel SHALL display truncated URL with copy-to-clipboard button
5. THE Admin_Panel SHALL provide inline link testing feature: "Test Et" button opens URL in new tab
6. WHEN admin hovers over a link target, THE Admin_Panel SHALL show full URL in tooltip
7. THE Admin_Panel SHALL highlight broken links (404 categories) with red indicator
8. THE Admin_Panel SHALL provide bulk operations: "Toplu Hedef Güncelle" for multiple blocks/banners

### Requirement 11: Backend API - Entity ve DTO Güncellemeleri

**User Story:** Developer olarak, backend entity ve DTO'ların güncellenmiş gereksinimleri desteklemesini istiyorum, böylece API tutarlı çalışsın.

#### Acceptance Criteria

1. THE HomeProductBlock entity SHALL add PosterLinkUrl property (string, nullable, max length 500)
2. THE HomeProductBlock entity SHALL add TargetCategoryId property (int, nullable, foreign key to Category)
3. THE CreateHomeBlockDto SHALL include PosterLinkUrl and TargetCategoryId fields
4. THE UpdateHomeBlockDto SHALL include PosterLinkUrl and TargetCategoryId fields
5. THE HomeProductBlockDto (response) SHALL include PosterLinkUrl, TargetCategoryId, and resolved poster click target
6. THE Backend_API SHALL create database migration adding PosterLinkUrl and TargetCategoryId columns
7. THE Backend_API SHALL add index on HomeProductBlock.TargetCategoryId for query performance
8. WHEN TargetCategoryId is provided, THE Backend_API SHALL validate category exists and is active
9. THE Backend_API SHALL add foreign key constraint: HomeProductBlock.TargetCategoryId -> Category.Id (SET NULL on delete)

### Requirement 12: Backend API - Service Layer Logic

**User Story:** Developer olarak, service layer'ın yeni gereksinimleri desteklemesini istiyorum, böylece iş mantığı doğru çalışsın.

#### Acceptance Criteria

1. THE IHomeBlockService SHALL add method: Task&lt;string&gt; GetPosterClickTarget(int blockId)
2. WHEN GetPosterClickTarget is called, THE Service SHALL return Banner.linkUrl if BannerId is not null
3. WHEN BannerId is null AND PosterLinkUrl is not null, THE Service SHALL return PosterLinkUrl
4. WHEN both are null/empty, THE Service SHALL return null
5. THE HomeBlockService SHALL update CreateBlockAsync to accept and save PosterLinkUrl and TargetCategoryId
6. THE HomeBlockService SHALL update UpdateBlockAsync to accept and update PosterLinkUrl and TargetCategoryId
7. WHEN TargetCategoryId changes, THE Service SHALL invalidate block products cache (if caching is implemented)
8. THE HomeBlockService SHALL update GetBlockByIdAsync to include resolved poster click target in response DTO
9. WHEN fetching blocks for public display, THE Service SHALL verify TargetCategoryId category is active

### Requirement 13: Frontend Component - Home.js Güncellemeleri

**User Story:** Developer olarak, Home.js bileşeninin tüm tıklanabilir elemanları render etmesini istiyorum, böylece müşteri deneyimi iyileşsin.

#### Acceptance Criteria

1. THE Home_Page SHALL wrap slider banner images in clickable anchor tags using Banner.linkUrl
2. THE Home_Page SHALL wrap promo banner images in clickable anchor tags using Banner.linkUrl
3. WHEN Banner.linkUrl starts with "http" or "https", THE System SHALL use &lt;a href&gt; with target="\_blank" and rel="noopener noreferrer"
4. WHEN Banner.linkUrl starts with "/", THE System SHALL use React Router Link component
5. THE Home_Page SHALL add CSS cursor: pointer to all clickable banners and posters
6. THE Home_Page SHALL add hover effect to clickable elements: transform: scale(1.02) with transition
7. THE Home_Page SHALL add loading skeleton placeholders for banners during data fetch
8. WHEN banner click target is null/empty, THE Home_Page SHALL render img without anchor wrapper

### Requirement 14: Frontend Component - Ana Sayfa Ürün Blokları

**User Story:** Developer olarak, ana sayfa ürün bloklarının posterlerinin tıklanabilir olmasını istiyorum, böylece blok işlevselliği tam olsun.

#### Acceptance Criteria

1. THE Home_Page SHALL create new component: HomeProductBlock.jsx to render product blocks
2. THE HomeProductBlock component SHALL receive posterClickTarget prop from API response
3. WHEN posterClickTarget is not null, THE HomeProductBlock SHALL wrap poster image in clickable anchor/Link
4. WHEN posterClickTarget is null, THE HomeProductBlock SHALL render poster image without wrapper
5. THE HomeProductBlock SHALL display block title, poster, and product grid
6. THE HomeProductBlock SHALL apply consistent styling: poster on left (30% width), products on right (70% width) on desktop
7. WHEN viewport is mobile, THE HomeProductBlock SHALL stack poster on top, products below (full width)
8. THE HomeProductBlock SHALL fetch block data from /api/homeblocks endpoint
9. THE Home_Page SHALL render all active HomeProductBlocks sorted by displayOrder

### Requirement 15: Frontend Component - Admin Panel Kategori Seçici

**User Story:** Developer olarak, admin panelinde hiyerarşik kategori seçicinin çalışmasını istiyorum, böylece admin alt kategori seçebilsin.

#### Acceptance Criteria

1. THE Admin_Panel SHALL create new component: CategorySelector.jsx with hierarchical display
2. THE CategorySelector SHALL fetch categories from /api/categories/root and /api/categories/{id}/subcategories
3. THE CategorySelector SHALL render parent categories with expandable chevron icon
4. WHEN parent category is expanded, THE CategorySelector SHALL show subcategories with indentation (20px padding-left)
5. THE CategorySelector SHALL display subcategories with visual indicator (icon or "└─" prefix)
6. WHEN admin selects a subcategory, THE CategorySelector SHALL emit full category object with parentId
7. THE CategorySelector SHALL show selected category breadcrumb below dropdown: "Parent > Subcategory"
8. THE Admin_Panel SHALL integrate CategorySelector in HomeProductBlock form replacing current categoryId dropdown
9. THE CategorySelector SHALL support search/filter: admin can type to filter categories and subcategories

### Requirement 16: Frontend Component - Admin Panel Ürün Arama Geliştirmeleri

**User Story:** Admin olarak, ürün ararken ürünün hangi kategoride olduğunu görmek istiyorum, böylece doğru ürünü seçtiğimden emin olabilirim.

#### Acceptance Criteria

1. THE Admin_Panel SHALL update product search modal to display product category breadcrumb
2. WHEN product belongs to subcategory, THE Modal SHALL display: "ParentCategory > Subcategory"
3. WHEN product belongs to parent category only, THE Modal SHALL display: "ParentCategory"
4. THE Admin_Panel SHALL add category filter dropdown to product search modal
5. WHEN admin selects category filter, THE Modal SHALL show only products from that category and its subcategories
6. THE Admin_Panel SHALL preserve existing product search by name and ID functionality
7. THE Product search results SHALL show category badge with color coding (parent: blue, subcategory: green)

### Requirement 17: Frontend Component - Admin Panel Link Hedefi Önizleme

**User Story:** Admin olarak, banner veya poster için girdiğim linkin önizlemesini görmek istiyorum, böylece doğru hedef belirlediğimden emin olabilirim.

#### Acceptance Criteria

1. THE Admin_Panel SHALL add link preview component below linkUrl input fields
2. WHEN admin types/pastes a URL, THE Preview SHALL debounce (500ms) and display parsed link information
3. WHEN URL is internal category link (/category/slug), THE Preview SHALL fetch and display category name and image thumbnail
4. WHEN URL is external (http/https), THE Preview SHALL display domain name and "Harici Link" badge
5. THE Preview SHALL show "Test Et" button opening URL in new tab for verification
6. WHEN URL is invalid or category not found, THE Preview SHALL display error message with red border
7. THE Admin_Panel SHALL add link preview for both Banner.linkUrl and HomeProductBlock.PosterLinkUrl fields

### Requirement 18: Database Migration ve Backward Compatibility

**User Story:** Developer olarak, database migration'ın mevcut verileri koruyarak güncellenmesini istiyorum, böylece veri kaybı olmasın.

#### Acceptance Criteria

1. THE Backend_API SHALL create migration adding HomeProductBlock.PosterLinkUrl column (nullable, nvarchar(500))
2. THE Backend_API SHALL create migration adding HomeProductBlock.TargetCategoryId column (nullable, int)
3. THE Migration SHALL copy existing CategoryId values to TargetCategoryId for backward compatibility
4. THE Migration SHALL add foreign key constraint: TargetCategoryId references Category(Id) ON DELETE SET NULL
5. THE Migration SHALL add index: IX_HomeProductBlock_TargetCategoryId
6. THE Migration SHALL NOT drop CategoryId column immediately (deprecation period: mark as obsolete in code)
7. THE Backend_API SHALL update seed data to include sample blocks with TargetCategoryId and PosterLinkUrl
8. THE Migration SHALL execute successfully on development, staging, and production environments without data loss

### Requirement 19: Hata Yönetimi ve Kullanıcı Bildirimleri

**User Story:** Kullanıcı ve admin olarak, hatalı linklerde veya eksik kategorilerde bilgilendirilmek istiyorum, böylece sorunları hızlıca çözebilirim.

#### Acceptance Criteria

1. WHEN Home_Page encounters broken banner link (404), THE System SHALL log error to console and gracefully skip navigation
2. WHEN Home_Page encounters null posterClickTarget, THE System SHALL render poster without click handler (no error)
3. WHEN Admin_Panel saves block with invalid TargetCategoryId, THE Backend_API SHALL return 400 Bad Request with descriptive error
4. THE Admin_Panel SHALL display validation error below category selector: "Seçilen kategori bulunamadı veya pasif"
5. WHEN banner image fails to load, THE System SHALL display fallback placeholder with banner title text
6. THE Admin_Panel SHALL validate linkUrl format client-side before submission (must start with http/https or /)
7. WHEN admin saves block with both BannerId and PosterLinkUrl, THE Admin_Panel SHALL show warning: "Banner linki öncelikli olacak"

### Requirement 20: Performance ve Caching

**User Story:** Kullanıcı olarak, ana sayfanın hızlı yüklenmesini istiyorum, böylece bekleme süresi minimum olsun.

#### Acceptance Criteria

1. THE Backend_API SHALL implement caching for GetAllActiveBlocks endpoint (cache duration: 5 minutes)
2. WHEN HomeProductBlock or Category is updated, THE Backend_API SHALL invalidate related caches
3. THE Home_Page SHALL lazy-load images using native loading="lazy" attribute
4. THE System SHALL preload first slider banner image for faster initial render (rel="preload")
5. THE Backend_API SHALL optimize query for blocks with TargetCategoryId: include Category in single JOIN query
6. THE Home_Page SHALL use React.memo for CategoryTile and HomeProductBlock components to prevent unnecessary re-renders
7. THE System SHALL compress banner images during upload (max width: 1200px, quality: 85%, WebP format preferred)
8. THE Backend_API SHALL return paginated product lists for blocks (default: 6 products per block, configurable via MaxProductCount)

### Requirement 21: Accessibility (A11y) İyileştirmeleri

**User Story:** Engelli kullanıcı olarak, ekran okuyucu ile ana sayfada gezinebilmek istiyorum, böylece alışveriş deneyimi erişilebilir olsun.

#### Acceptance Criteria

1. THE System SHALL add descriptive alt text to all banner images (use Banner.title or Banner.description)
2. THE System SHALL add descriptive alt text to all poster images (use HomeProductBlock.name or title)
3. THE System SHALL add aria-label to all clickable elements describing destination (örn: "Süt Ürünleri kategorisine git")
4. WHEN banner/poster is not clickable, THE System SHALL use role="img" and descriptive alt text (no anchor tag)
5. THE CategoryTile SHALL maintain existing keyboard navigation (Tab, Enter, Space, Escape) and add aria-expanded state
6. THE System SHALL ensure focus indicators are visible on all clickable elements (outline or box-shadow)
7. THE Home_Page SHALL use semantic HTML: &lt;nav&gt; for category tiles, &lt;section&gt; for product blocks
8. THE System SHALL provide skip link: "Ana içeriğe atla" for keyboard users to bypass slider

### Requirement 22: Analytics ve Tracking

**User Story:** Admin olarak, hangi banner ve posterlerin ne kadar tıklandığını görmek istiyorum, böylece kampanya performansını ölçebilirim.

#### Acceptance Criteria

1. THE System SHALL increment Banner.ClickCount when banner is clicked on Home_Page
2. THE System SHALL add ClickCount field to HomeProductBlock entity (int, default: 0)
3. THE System SHALL increment HomeProductBlock.ClickCount when poster is clicked on Home_Page
4. THE System SHALL add ViewCount field to HomeProductBlock entity (int, default: 0)
5. THE System SHALL increment HomeProductBlock.ViewCount when block is visible in viewport (Intersection Observer)
6. THE Backend_API SHALL create endpoint: POST /api/banners/{id}/track-click for async click tracking
7. THE Backend_API SHALL create endpoint: POST /api/homeblocks/{id}/track-click for poster click tracking
8. THE Backend_API SHALL create endpoint: POST /api/homeblocks/{id}/track-view for block view tracking
9. THE Admin_Panel SHALL display click and view counts in banner and block list tables
10. THE Admin_Panel SHALL calculate and display CTR (Click-Through Rate): (ClickCount / ViewCount) \* 100

### Requirement 23: Testing Gereksinimleri

**User Story:** Developer olarak, tüm yeni özelliklerin test edilmiş olmasını istiyorum, böylece production'da sorun çıkmasın.

#### Acceptance Criteria

1. THE System SHALL include unit tests for HomeBlockService.GetPosterClickTarget method (3 scenarios: BannerId, PosterLinkUrl, null)
2. THE System SHALL include integration tests for HomeProductBlock API endpoints with TargetCategoryId validation
3. THE System SHALL include component tests for CategorySelector showing parent/child hierarchy
4. THE System SHALL include E2E tests for banner click navigation (slider + promo)
5. THE System SHALL include E2E tests for category tile click navigation (parent + subcategory)
6. THE System SHALL include E2E tests for poster click navigation
7. THE System SHALL include mobile responsive tests for touch events and grid layouts (viewport: 375px, 768px, 1024px)
8. THE System SHALL include accessibility tests using axe-core (no critical violations allowed)
9. THE System SHALL include database migration tests ensuring backward compatibility
10. THE System SHALL achieve minimum 80% code coverage for new/modified backend code
