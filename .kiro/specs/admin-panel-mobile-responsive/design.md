# Design Document: Admin Panel Mobile Responsive

## Overview

Bu tasarım, admin panelindeki tüm sekmelerin mobil cihazlarda düzgün çalışmasını sağlamak için gerekli mimari ve bileşen değişikliklerini tanımlar. Mevcut mobil-uyumlu sayfalar (Dashboard, AdminUsers, AdminProducts, AdminOrders, AdminCategories, AdminCouriers, AdminReports) referans alınarak, diğer sayfalar aynı standartlara getirilecektir.

### Mevcut Durum Analizi

**Mobil Uyumlu Sayfalar (Referans):**

- `Dashboard.jsx` - col-6 col-xl-3 grid, p-2 p-md-3 padding, 0.65rem font
- `AdminUsers.jsx` - adminUsers.css ile kart görünümü, data-label attribute
- `AdminProducts.jsx` - Responsive tablo, d-none d-sm-table-cell
- `AdminOrders.jsx` - Responsive grid ve tablo
- `AdminCategories.jsx` - Responsive kart layout
- `AdminCouriers.jsx` - Responsive tablo
- `AdminReports.jsx` - Responsive grid

**Mobil Uyumsuz Sayfalar (Düzeltilecek):**

- `AdminMicro.js` - Büyük butonlar, desktop layout
- `CouponManagement.jsx` - Desktop tablo layout
- `BannerManagement.jsx` - Stilsiz form/tablo
- `AdminRoles.jsx` - Bootstrap icons, desktop layout
- `AdminWeightReports.jsx` - WeightReportsPanel kontrolü gerekli
- `AdminCampaigns.jsx` - Kısmen mobil uyumlu
- Log sayfaları (TypeScript)

## Architecture

### Responsive Tasarım Stratejisi

```
┌─────────────────────────────────────────────────────────────┐
│                    Admin Panel Layout                        │
├─────────────────────────────────────────────────────────────┤
│  Breakpoints:                                                │
│  - xs: < 576px  (Mobil - Tek sütun)                         │
│  - sm: 576-768px (Tablet - 2 sütun)                         │
│  - md: 768-992px (Tablet landscape - 3 sütun)               │
│  - lg: > 992px  (Desktop - 4+ sütun)                        │
├─────────────────────────────────────────────────────────────┤
│  Ortak Stiller: adminMobile.css                             │
│  - Tablo → Kart dönüşümü                                    │
│  - Touch-friendly butonlar (min 44px)                       │
│  - Responsive grid utilities                                 │
│  - Font size: min 0.65rem                                   │
└─────────────────────────────────────────────────────────────┘
```

### CSS Dosya Yapısı

```
frontend/src/styles/
├── adminUsers.css          (mevcut - referans)
├── adminMobile.css         (yeni - ortak mobil stiller)
└── [sayfa-specific].css    (gerekirse)
```

## Components and Interfaces

### 1. AdminMicro.js Mobil Tasarımı

**Mevcut Sorunlar:**

- Aksiyon butonları yan yana, mobilde taşıyor
- Tablolar responsive değil

**Çözüm:**

```jsx
// Header bölümü - responsive flex
<div className="d-flex flex-column flex-md-row justify-content-between align-items-start align-items-md-center mb-3 mb-md-4">
  <div className="mb-2 mb-md-0">
    <h1 className="h4 h3-md fw-bold mb-1">...</h1>
  </div>
  {/* Butonlar - mobilde grid, desktop'ta flex */}
  <div className="d-grid d-md-flex gap-2 w-100 w-md-auto">
    <button className="btn btn-sm">...</button>
  </div>
</div>

// Tablolar - responsive wrapper
<div className="table-responsive">
  <table className="table table-sm admin-mobile-table">
    ...
  </table>
</div>
```

### 2. CouponManagement.jsx Mobil Tasarımı

**Mevcut Sorunlar:**

- Tablo 9 sütunlu, mobilde okunmuyor
- Arama ve buton yan yana

**Çözüm:**

```jsx
// Kart görünümü için data-label attribute
<td data-label="Kod">{c.code}</td>
<td data-label="İndirim">...</td>

// Mobilde gizlenecek sütunlar
<th className="d-none d-md-table-cell">Min Tutar</th>
<th className="d-none d-md-table-cell">Kullanım</th>
```

### 3. BannerManagement.jsx Mobil Tasarımı

**Mevcut Sorunlar:**

- Hiç stil yok, ham HTML
- Form elemanları inline

**Çözüm:**

```jsx
// Modern kart tabanlı tasarım
<div className="container-fluid p-2 p-md-4">
  <div className="card border-0 shadow-sm">
    <div className="card-body">
      <form className="row g-2 g-md-3">
        <div className="col-12 col-md-6">
          <input className="form-control" />
        </div>
      </form>
    </div>
  </div>

  {/* Banner listesi - responsive grid */}
  <div className="row g-2 g-md-3">
    {banners.map((b) => (
      <div className="col-12 col-sm-6 col-lg-4">
        <div className="card">...</div>
      </div>
    ))}
  </div>
</div>
```

### 4. AdminRoles.jsx Mobil Tasarımı

**Mevcut Sorunlar:**

- Bootstrap Icons (bi-\*) kullanıyor, tutarsız
- Modal çok geniş

**Çözüm:**

```jsx
// Font Awesome'a geçiş
<i className="fas fa-user-shield me-2"></i>

// Rol kartları - mobilde tek sütun
<div className="col-12 col-md-6 col-lg-4">

// Modal - mobilde full-width
<div className="modal-dialog modal-dialog-centered modal-fullscreen-sm-down">
```

### 5. AdminWeightReports.jsx / WeightReportsPanel.jsx

**Çözüm:**

- WeightReportsPanel'e responsive stiller eklenmeli
- Filtreler collapsible olmalı
- Tablo kart görünümüne geçmeli

### 6. AdminCampaigns.jsx İyileştirmeleri

**Mevcut:** Kısmen mobil uyumlu
**İyileştirmeler:**

- Form layout optimizasyonu
- Date picker touch-friendly
- İstatistik kartları 2x2 grid

### 7. Log Sayfaları (TypeScript)

**Çözüm:**

- Ortak responsive tablo stili
- Collapsible filtreler
- Truncated log mesajları

## Data Models

Bu özellik için yeni data model gerekmiyor. Mevcut bileşenler aynı veri yapılarını kullanmaya devam edecek.

## Correctness Properties

_A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees._

### Property 1: Responsive Layout at 768px Breakpoint

_For any_ admin panel page, when the viewport width is 768px or less, the page layout SHALL transform to a mobile-friendly format (single column for cards, card view for tables).
**Validates: Requirements 1.1, 2.1, 3.1, 4.1, 5.1, 6.1, 7.1, 8.1**

### Property 2: Vertical Stacking at 576px Breakpoint

_For any_ admin panel page with action buttons, when the viewport width is 576px or less, the buttons SHALL stack vertically with full width.
**Validates: Requirements 1.2, 2.2**

### Property 3: Minimum Font Size Compliance

_For any_ text element in admin panel pages, the computed font-size SHALL be at least 0.65rem (approximately 10.4px) on mobile viewports.
**Validates: Requirements 1.4, 9.2**

### Property 4: Touch Target Size Compliance

_For any_ interactive element (button, link, checkbox, input) in admin panel pages, the touch target area SHALL be at least 44x44 pixels on mobile viewports.
**Validates: Requirements 2.4, 3.4, 4.3, 5.2, 6.3, 7.3, 9.3**

### Property 5: Table Horizontal Scroll or Card Transform

_For any_ data table in admin panel pages, when viewport width is 768px or less, the table SHALL either have horizontal scroll capability OR transform to card layout.
**Validates: Requirements 1.3, 3.3, 7.4, 8.1**

### Property 6: Modal Full-Width on Mobile

_For any_ modal dialog in admin panel pages, when viewport width is 576px or less, the modal SHALL display in full-width format.
**Validates: Requirements 2.3, 3.2, 6.2**

### Property 7: Consistent Breakpoint Usage

_For any_ CSS media query in admin panel styles, the breakpoints SHALL be one of: 576px, 768px, or 992px.
**Validates: Requirements 9.1**

### Property 8: Consistent Card Styling

_For any_ mobile card view (transformed from table), the card SHALL use consistent styling: border-radius 12px, shadow, and data-label attributes for field names.
**Validates: Requirements 9.4**

## Error Handling

### Responsive Fallbacks

1. **CSS Grid/Flexbox Fallback:**

   - Modern CSS ile uyumsuz tarayıcılar için Bootstrap grid kullanılacak
   - `@supports` ile feature detection

2. **Touch Event Fallback:**

   - Drag-and-drop için touch events desteklenmiyorsa alternatif butonlar
   - Sıralama için yukarı/aşağı ok butonları

3. **Viewport Meta Tag:**
   ```html
   <meta
     name="viewport"
     content="width=device-width, initial-scale=1, maximum-scale=5"
   />
   ```

## Testing Strategy

### Unit Tests

- Her sayfa için responsive class'ların doğru uygulandığını test et
- data-label attribute'larının mevcut olduğunu doğrula
- Modal responsive class'larını kontrol et

### Property-Based Tests

- Viewport genişliği değiştiğinde layout değişikliklerini test et
- Touch target boyutlarını rastgele elementler için doğrula
- Font size minimum değerini tüm text elementleri için kontrol et

### Visual Regression Tests

- Her sayfa için 320px, 576px, 768px, 992px viewport'larında screenshot karşılaştırması
- Kart görünümü dönüşümlerini doğrula

### Manual Testing Checklist

- [ ] iPhone SE (320px) - En küçük mobil
- [ ] iPhone 12 (390px) - Orta mobil
- [ ] iPad Mini (768px) - Tablet
- [ ] iPad Pro (1024px) - Büyük tablet
- [ ] Touch interaction testi (gerçek cihaz)

### Test Framework

- Jest + React Testing Library (unit tests)
- fast-check (property-based tests)
- Playwright (visual regression - opsiyonel)
