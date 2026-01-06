# Requirements Document

## Introduction

Bu özellik, admin panelinden yapılan değişikliklerin (kategori güncelleme, ürün ekleme/silme vb.) ana sayfaya ve diğer sayfalara gerçek zamanlı olarak yansımasını sağlar. Backend API henüz hazır olmadığı için mock data kullanılmaktadır. Mevcut sistemde `mockDataStore.js` merkezi bir store olarak tasarlanmış ancak `adminService.js` içinde ayrı mock data tanımları bulunmakta ve bu tutarsızlığa yol açmaktadır.

## Glossary

- **MockDataStore**: Tüm mock verilerin tutulduğu ve yönetildiği merkezi JavaScript modülü
- **AdminService**: Admin paneli işlemlerini yöneten servis modülü
- **Subscribe/Notify Pattern**: Veri değişikliklerinde ilgili bileşenleri bilgilendiren olay tabanlı mimari
- **Real-time Sync**: Admin panelinde yapılan değişikliklerin anında diğer sayfalara yansıması

## Requirements

### Requirement 1

**User Story:** Bir admin olarak, kategori adını güncellediğimde bu değişikliğin ana sayfadaki kategori butonlarına ve kategori sayfası başlığına anında yansımasını istiyorum, böylece tutarlı bir kullanıcı deneyimi sağlanır.

#### Acceptance Criteria

1. WHEN admin panelinde bir kategori adı güncellendiğinde THEN MockDataStore SHALL kategori verisini güncellemeli ve tüm abone bileşenleri bilgilendirmelidir
2. WHEN kategori verisi güncellendiğinde THEN ana sayfa kategori butonları SHALL yeni kategori adını göstermelidir
3. WHEN kategori verisi güncellendiğinde THEN kategori sayfası başlığı SHALL yeni kategori adını göstermelidir
4. WHEN kategori güncellendiğinde THEN ilgili ürünlerin categoryName alanı SHALL otomatik olarak güncellenmelidir

### Requirement 2

**User Story:** Bir admin olarak, yeni bir ürün eklediğimde bu ürünün ana sayfada ve ilgili kategori sayfasında anında görünmesini istiyorum, böylece müşteriler yeni ürünleri hemen görebilir.

#### Acceptance Criteria

1. WHEN admin panelinde yeni bir ürün eklendiğinde THEN MockDataStore SHALL ürünü listeye eklemeli ve tüm abone bileşenleri bilgilendirmelidir
2. WHEN yeni ürün eklendiğinde THEN ana sayfa öne çıkan ürünler bölümü SHALL yeni ürünü içermelidir
3. WHEN yeni ürün eklendiğinde THEN ilgili kategori sayfası SHALL yeni ürünü listelemeli ve ürün sayısını güncellemelidir

### Requirement 3

**User Story:** Bir admin olarak, bir ürünü sildiğimde veya pasif yaptığımda bu değişikliğin tüm sayfalara anında yansımasını istiyorum, böylece müşteriler mevcut olmayan ürünleri görmez.

#### Acceptance Criteria

1. WHEN admin panelinde bir ürün silindiğinde THEN MockDataStore SHALL ürünü listeden kaldırmalı ve tüm abone bileşenleri bilgilendirmelidir
2. WHEN bir ürün silindiğinde THEN ana sayfa ve kategori sayfaları SHALL silinen ürünü göstermemelidir
3. WHEN bir ürün pasif yapıldığında THEN public sayfalar SHALL pasif ürünü göstermemelidir

### Requirement 4

**User Story:** Bir geliştirici olarak, tüm mock data işlemlerinin tek bir merkezi store üzerinden yapılmasını istiyorum, böylece veri tutarsızlıkları önlenir.

#### Acceptance Criteria

1. THE AdminService SHALL tüm mock data işlemleri için MockDataStore modülünü kullanmalıdır
2. THE AdminService SHALL kendi içinde ayrı mock data dizileri tanımlamamalıdır
3. WHEN mock data modu aktifken THEN tüm servisler SHALL aynı MockDataStore instance'ını kullanmalıdır
