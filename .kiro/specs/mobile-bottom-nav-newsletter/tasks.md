# Implementation Plan: Mobile Bottom Navigation & Newsletter

## Overview

Bu plan, mobil bottom navigation bar, newsletter formu ve responsive footer kontrolü için adım adım implementasyon görevlerini içerir. React/JavaScript kullanılacak, mevcut proje yapısına entegre edilecek.

## Tasks

- [x] 1. MobileBottomNav Bileşeni Oluşturma

  - [x] 1.1 MobileBottomNav.jsx dosyasını oluştur

    - `frontend/src/components/MobileBottomNav.jsx` dosyasını oluştur
    - 5 navigasyon öğesi: Anasayfa, Kategoriler, Sepetim, Kampanyalar, Hesabım
    - useLocation hook ile aktif route tespiti
    - useNavigate hook ile sayfa yönlendirme
    - useCartCount hook ile sepet badge entegrasyonu
    - _Requirements: 1.1, 1.3, 1.4, 1.6, 1.8_

  - [x] 1.2 mobileNav.css stil dosyasını oluştur

    - `frontend/src/styles/mobileNav.css` dosyasını oluştur
    - Fixed positioning (bottom: 0)
    - Turuncu tema renkleri (#ff6b35)
    - Aktif/pasif state stilleri
    - Touch-friendly buton boyutları (min 44x44px)
    - z-index: 1050
    - _Requirements: 1.5, 1.7, 5.5_

  - [x] 1.3 MobileBottomNav unit testleri yaz
    - Render testi
    - Navigation item click testi
    - Active state testi
    - _Requirements: 1.3, 1.4, 1.6_

- [x] 2. NewsletterForm Bileşeni Oluşturma

  - [x] 2.1 NewsletterForm.jsx dosyasını oluştur

    - `frontend/src/components/NewsletterForm.jsx` dosyasını oluştur
    - Turuncu gradient arka plan
    - E-posta input ve "Abone Ol" butonu
    - Dekoratif blur efektleri
    - Form state yönetimi (idle, loading, success, error)
    - E-posta validasyonu (regex)
    - LocalStorage ile abonelik durumu kaydetme
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6_

  - [x] 2.2 newsletterForm.css stil dosyasını oluştur

    - `frontend/src/styles/newsletterForm.css` dosyasını oluştur
    - Turuncu gradient background
    - Blur efektleri (::before, ::after pseudo elements)
    - Responsive tasarım (mobil ve web)
    - Input ve button stilleri
    - _Requirements: 3.1, 3.4, 3.7_

  - [x] 2.3 NewsletterForm unit testleri yaz

    - Render testi
    - E-posta validasyon testi
    - Form submission testi
    - _Requirements: 3.2, 3.3, 3.5, 3.6_

  - [x] 2.4 E-posta validasyon property testi yaz
    - **Property 6: Email Validation and Form Submission**
    - **Validates: Requirements 3.5, 3.6**

- [x] 3. Checkpoint - Bileşen testlerini çalıştır

  - Ensure all tests pass, ask the user if questions arise.

- [x] 4. Header Mobil Optimizasyonu

  - [x] 4.1 App.js Header bileşenini güncelle

    - Mobilde "Hesabım" butonunu gizle (d-none d-md-flex)
    - Mobilde "Siparişlerim" butonunu gizle
    - Mobilde kategori navigation bar'ı gizle
    - CSS class'ları ile responsive kontrol
    - _Requirements: 2.1, 2.2, 2.3_

  - [x] 4.2 Header responsive CSS güncellemeleri
    - App.css'e mobil gizleme stilleri ekle
    - 768px breakpoint media queries
    - _Requirements: 2.1, 2.2, 2.3_

- [x] 5. Footer Görünürlük Kontrolü

  - [x] 5.1 Footer.jsx bileşenini güncelle

    - Mobilde gizleme için CSS class ekle
    - `desktop-only-footer` class'ı
    - _Requirements: 4.1, 4.2_

  - [x] 5.2 Footer responsive CSS güncellemeleri
    - App.css'e footer gizleme stilleri ekle
    - 768px breakpoint media queries
    - _Requirements: 4.1, 4.2_

- [x] 6. App.js Entegrasyonu

  - [x] 6.1 Bileşenleri App.js'e entegre et

    - MobileBottomNav import ve render
    - NewsletterForm import ve render (Footer üzerinde)
    - CSS dosyalarını import et
    - Sayfa içeriğine bottom padding ekle (mobilde)
    - _Requirements: 3.8, 4.3, 5.3_

  - [x] 6.2 Responsive layout düzenlemeleri
    - Main content wrapper'a mobil padding ekle
    - Newsletter ve Footer sıralaması
    - _Requirements: 4.3, 5.1, 5.2_

- [x] 7. Checkpoint - Entegrasyon testleri

  - Ensure all tests pass, ask the user if questions arise.

- [x] 8. Property-Based Testler

  - [x] 8.1 Breakpoint visibility property testi yaz

    - **Property 1: Breakpoint-based Mobile Bottom Nav Visibility**
    - **Validates: Requirements 1.1, 1.2**

  - [x] 8.2 Navigation route mapping property testi yaz

    - **Property 2: Navigation Route Mapping**
    - **Validates: Requirements 1.4**

  - [x] 8.3 Active route highlighting property testi yaz

    - **Property 3: Active Route Highlighting**
    - **Validates: Requirements 1.6**

  - [x] 8.4 Cart badge count property testi yaz

    - **Property 4: Cart Badge Count Consistency**
    - **Validates: Requirements 1.8**

  - [x] 8.5 Footer visibility property testi yaz
    - **Property 8: Footer Breakpoint Visibility**
    - **Validates: Requirements 4.1, 4.2**

- [x] 9. Final Checkpoint - Tüm testleri çalıştır
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- All tasks are required for comprehensive implementation
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties
- Unit tests validate specific examples and edge cases
- Mevcut CartContext ve AuthContext kullanılacak
- CSS-only visibility tercih edilecek (performans için)
