# Design Document: Mobile Bottom Navigation & Newsletter

## Overview

Bu tasarım dokümanı, e-ticaret sitesinin mobil deneyimini iyileştirmek için gerekli bileşenleri ve mimari kararları detaylandırır. Ana hedefler:

1. Mobil cihazlarda sabit alt navigasyon çubuğu
2. Turuncu temalı newsletter abonelik formu
3. Responsive footer görünürlük kontrolü
4. Header mobil optimizasyonu

## Architecture

### Bileşen Hiyerarşisi

```
App.js
├── Header (mevcut - mobil optimizasyonu eklenecek)
│   ├── Logo
│   ├── SearchAutocomplete
│   └── HeaderActions (mobilde gizlenecek)
├── Routes (mevcut)
├── NewsletterForm (yeni - footer üzerinde)
├── Footer (mevcut - mobilde gizlenecek)
└── MobileBottomNav (yeni - sadece mobilde görünür)
```

### Responsive Breakpoint Stratejisi

```
┌─────────────────────────────────────────────────────────┐
│                    DESKTOP (>768px)                      │
│  ┌─────────────────────────────────────────────────┐    │
│  │ Header (tam görünüm)                            │    │
│  │ - Logo, Search, Hesabım, Siparişlerim, Sepet    │    │
│  ├─────────────────────────────────────────────────┤    │
│  │ Kategori Navigation Bar                         │    │
│  ├─────────────────────────────────────────────────┤    │
│  │ Page Content                                    │    │
│  ├─────────────────────────────────────────────────┤    │
│  │ Newsletter Form                                 │    │
│  ├─────────────────────────────────────────────────┤    │
│  │ Footer (lacivert - görünür)                     │    │
│  └─────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│                    MOBILE (≤768px)                       │
│  ┌─────────────────────────────────────────────────┐    │
│  │ Header (sadeleştirilmiş)                        │    │
│  │ - Logo, Search, Sepet                           │    │
│  ├─────────────────────────────────────────────────┤    │
│  │ Page Content                                    │    │
│  │ (padding-bottom: 80px for bottom nav)           │    │
│  ├─────────────────────────────────────────────────┤    │
│  │ Newsletter Form (kompakt)                       │    │
│  ├─────────────────────────────────────────────────┤    │
│  │ Footer: GİZLİ                                   │    │
│  ├─────────────────────────────────────────────────┤    │
│  │ ┌─────────────────────────────────────────────┐ │    │
│  │ │ Mobile Bottom Nav (fixed)                   │ │    │
│  │ │ 🏠 Anasayfa | 📂 Kategoriler | 🛒 Sepet    │ │    │
│  │ │ 🏷️ Kampanyalar | 👤 Hesabım                │ │    │
│  │ └─────────────────────────────────────────────┘ │    │
│  └─────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────┘
```

## Components and Interfaces

### 1. MobileBottomNav Component

```jsx
// frontend/src/components/MobileBottomNav.jsx

interface NavItem {
  id: string;
  label: string;
  icon: string; // FontAwesome icon class
  path: string;
  badge?: number; // Sepet için badge sayısı
}

interface MobileBottomNavProps {
  // Props gerekmez - context'ten alınacak
}

// Navigasyon öğeleri
const NAV_ITEMS: NavItem[] = [
  { id: "home", label: "Anasayfa", icon: "fa-home", path: "/" },
  {
    id: "categories",
    label: "Kategoriler",
    icon: "fa-th-large",
    path: "/categories",
  },
  { id: "cart", label: "Sepetim", icon: "fa-shopping-cart", path: "/cart" },
  {
    id: "campaigns",
    label: "Kampanyalar",
    icon: "fa-tags",
    path: "/campaigns",
  },
  { id: "account", label: "Hesabım", icon: "fa-user", path: "/profile" },
];
```

### 2. NewsletterForm Component

```jsx
// frontend/src/components/NewsletterForm.jsx

interface NewsletterFormProps {
  className?: string;
}

interface NewsletterFormState {
  email: string;
  status: "idle" | "loading" | "success" | "error";
  message: string;
}

// E-posta validasyon regex
const EMAIL_REGEX = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
```

### 3. CSS Modülleri

```css
/* frontend/src/styles/mobileNav.css */

/* Değişkenler - mevcut tema ile uyumlu */
:root {
  --mobile-nav-height: 65px;
  --mobile-nav-bg: #ffffff;
  --mobile-nav-shadow: 0 -2px 10px rgba(0, 0, 0, 0.1);
  --mobile-nav-active: #ff6b35;
  --mobile-nav-inactive: #6c757d;
}
```

## Data Models

### Newsletter Subscription

```typescript
interface NewsletterSubscription {
  email: string;
  subscribedAt: Date;
  source: "web" | "mobile";
}

// LocalStorage key
const NEWSLETTER_STORAGE_KEY = "newsletter_subscribed";
```

### Navigation State

```typescript
interface NavigationState {
  activeRoute: string;
  cartCount: number;
  isAuthenticated: boolean;
}
```

## Correctness Properties

_A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees._

### Property 1: Breakpoint-based Mobile Bottom Nav Visibility

_For any_ viewport width value, the Mobile_Bottom_Nav component should be visible if and only if the viewport width is less than or equal to 768px.

**Validates: Requirements 1.1, 1.2**

### Property 2: Navigation Route Mapping

_For any_ navigation item in the Mobile_Bottom_Nav, clicking on it should navigate to the correct corresponding route path.

**Validates: Requirements 1.4**

### Property 3: Active Route Highlighting

_For any_ current route path, the Mobile_Bottom_Nav should highlight exactly one navigation item that matches the current route.

**Validates: Requirements 1.6**

### Property 4: Cart Badge Count Consistency

_For any_ cart state with N items, the Mobile_Bottom_Nav cart badge should display the value N (or be hidden if N is 0).

**Validates: Requirements 1.8**

### Property 5: Header Elements Mobile Visibility

_For any_ viewport width value, the header "Hesabım" and "Siparişlerim" buttons should be hidden if and only if the viewport width is less than or equal to 768px.

**Validates: Requirements 2.1, 2.2, 2.3**

### Property 6: Email Validation and Form Submission

_For any_ email string input, the Newsletter_Form should show success message if the email matches valid format, and error message if it does not match valid format.

**Validates: Requirements 3.5, 3.6**

### Property 7: Newsletter Form Responsiveness

_For any_ viewport width value, the Newsletter_Form should render without overflow and maintain usability.

**Validates: Requirements 3.7**

### Property 8: Footer Breakpoint Visibility

_For any_ viewport width value, the Footer component should be visible if and only if the viewport width is greater than 768px.

**Validates: Requirements 4.1, 4.2**

## Error Handling

### Newsletter Form Errors

| Hata Durumu     | Kullanıcı Mesajı                   | Aksiyon             |
| --------------- | ---------------------------------- | ------------------- |
| Boş e-posta     | "Lütfen e-posta adresinizi girin"  | Input'a focus       |
| Geçersiz format | "Geçerli bir e-posta adresi girin" | Input'a focus       |
| Ağ hatası       | "Bağlantı hatası, tekrar deneyin"  | Retry butonu göster |
| Zaten abone     | "Bu e-posta zaten kayıtlı"         | Bilgi mesajı        |

### Navigation Errors

| Hata Durumu      | Aksiyon                 |
| ---------------- | ----------------------- |
| Route bulunamadı | 404 sayfasına yönlendir |
| Auth gerekli     | Login modal aç          |

## Testing Strategy

### Unit Tests

- MobileBottomNav render testi
- NewsletterForm render testi
- E-posta validasyon fonksiyonu testi
- Navigation item click handler testi

### Property-Based Tests

Property-based testing için **Jest** ve **fast-check** kütüphaneleri kullanılacak.

```javascript
// Minimum 100 iterasyon per property test
// Tag format: Feature: mobile-bottom-nav-newsletter, Property N: description
```

**Test Dosyaları:**

- `frontend/src/__tests__/MobileBottomNav.test.jsx`
- `frontend/src/__tests__/NewsletterForm.test.jsx`
- `frontend/src/__tests__/mobileNav.property.test.js`

### Integration Tests

- Viewport resize ile görünürlük geçişleri
- Cart context ile badge güncelleme
- Route değişimi ile active state güncelleme

## Implementation Notes

### CSS Media Query Stratejisi

```css
/* Mobile-first yaklaşım */
.mobile-bottom-nav {
  display: flex; /* Mobilde görünür */
}

@media (min-width: 769px) {
  .mobile-bottom-nav {
    display: none; /* Desktop'ta gizli */
  }
}

.desktop-footer {
  display: none; /* Mobilde gizli */
}

@media (min-width: 769px) {
  .desktop-footer {
    display: block; /* Desktop'ta görünür */
  }
}
```

### Z-Index Hiyerarşisi

```
z-index: 1000 - Header (sticky)
z-index: 1050 - Mobile Bottom Nav (fixed)
z-index: 1100 - Modals
z-index: 1200 - Toast notifications
```

### Performance Optimizasyonları

1. **CSS-only visibility**: JavaScript yerine CSS media queries kullanarak performans artışı
2. **Memoization**: Navigation items için useMemo kullanımı
3. **Lazy loading**: Newsletter form için intersection observer ile lazy load
