# Design Document: Admin Mock Data Sync

## Overview

Bu tasarım, admin panelinden yapılan değişikliklerin (kategori güncelleme, ürün ekleme/silme) ana sayfaya ve diğer sayfalara gerçek zamanlı olarak yansımasını sağlar. Mevcut `mockDataStore.js` modülü merkezi store olarak kullanılacak ve `adminService.js` içindeki duplicate mock data tanımları kaldırılacaktır.

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        MockDataStore                             │
│  (Tek Kaynak - Single Source of Truth)                          │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐              │
│  │ categories  │  │  products   │  │  listeners  │              │
│  └─────────────┘  └─────────────┘  └─────────────┘              │
│         │                │                │                      │
│         └────────────────┼────────────────┘                      │
│                          │                                       │
│                    notify(type)                                  │
└─────────────────────────────────────────────────────────────────┘
                           │
           ┌───────────────┼───────────────┐
           │               │               │
           ▼               ▼               ▼
    ┌─────────────┐ ┌─────────────┐ ┌─────────────┐
    │AdminService │ │ProductService│ │   Home.js   │
    │(CRUD ops)   │ │(read ops)   │ │Category.js  │
    └─────────────┘ └─────────────┘ └─────────────┘
```

### Veri Akışı

1. Admin panelinde değişiklik yapılır (örn: kategori adı güncelleme)
2. `AdminService` → `mockDataStore.updateCategory()` çağrılır
3. `mockDataStore` veriyi günceller ve `notify("categories")` çağrılır
4. Tüm abone bileşenler (Home.js, Category.js) callback'leri tetiklenir
5. Bileşenler yeni veriyi `mockDataStore.getCategories()` ile alır ve re-render olur

## Components and Interfaces

### MockDataStore (Mevcut - Güncelleme Gerekmiyor)

```javascript
// Zaten mevcut ve doğru çalışıyor
export const subscribe = (type, callback) => { ... }
export const getCategories = () => { ... }
export const updateCategory = (id, data) => { ... }
// ... diğer metodlar
```

### AdminService (Güncelleme Gerekli)

```javascript
// ÖNCE (Sorunlu - Duplicate Data)
let mockCategories = [...]; // ❌ Ayrı mock data

// SONRA (Düzeltilmiş)
import mockDataStore from "./mockDataStore";

getCategories: async () => {
  if (shouldUseMockData()) {
    return mockDataStore.getAllCategories(); // ✅ Merkezi store
  }
  // ...
}
```

### ProductGrid (Güncelleme Gerekli)

```javascript
// Subscribe mekanizması eklenmeli
useEffect(() => {
  const unsubscribe = mockDataStore.subscribe("products", loadProducts);
  return () => unsubscribe && unsubscribe();
}, []);
```

## Data Models

### Category

```typescript
interface Category {
  id: number;
  name: string;
  slug: string;
  description?: string;
  icon?: string;
  productCount: number;
  isActive: boolean;
}
```

### Product

```typescript
interface Product {
  id: number;
  name: string;
  categoryId: number;
  categoryName: string; // Kategori güncellendiğinde otomatik güncellenir
  price: number;
  stock: number;
  description?: string;
  imageUrl?: string;
  isActive: boolean;
}
```

## Correctness Properties

_A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees._

### Property 1: Category Update Propagation

_For any_ kategori güncellemesi, güncelleme sonrası `getCategories()` çağrıldığında dönen listede ilgili kategorinin yeni adı bulunmalıdır.
**Validates: Requirements 1.1**

### Property 2: Category-Product Name Consistency

_For any_ kategori adı güncellemesi, güncelleme sonrası o kategoriye ait tüm ürünlerin `categoryName` alanı yeni kategori adıyla eşleşmelidir.
**Validates: Requirements 1.4**

### Property 3: Product Addition Increases List Size

_For any_ geçerli ürün verisi, `createProduct()` çağrıldıktan sonra `getAllProducts()` listesinin boyutu bir artmalıdır.
**Validates: Requirements 2.1**

### Property 4: Product Addition Updates Category Count

_For any_ yeni ürün eklemesi, ekleme sonrası ilgili kategorinin `productCount` değeri bir artmalıdır.
**Validates: Requirements 2.3**

### Property 5: Product Deletion Decreases List Size

_For any_ mevcut ürün silme işlemi, `deleteProduct()` çağrıldıktan sonra `getAllProducts()` listesinin boyutu bir azalmalıdır.
**Validates: Requirements 3.1**

### Property 6: Deleted Products Not In Public List

_For any_ silinen ürün, silme sonrası `getProducts()` (public) listesinde bulunmamalıdır.
**Validates: Requirements 3.2**

### Property 7: Inactive Products Not In Public List

_For any_ `isActive=false` olan ürün, `getProducts()` (public) listesinde bulunmamalı ama `getAllProducts()` (admin) listesinde bulunmalıdır.
**Validates: Requirements 3.3**

### Property 8: Singleton Store Consistency

_For any_ iki farklı servis modülünden yapılan değişiklik, aynı `mockDataStore` instance'ını etkilemelidir.
**Validates: Requirements 4.3**

## Error Handling

- Kategori/ürün bulunamadığında `throw new Error("... bulunamadı")`
- Geçersiz ID için graceful handling
- Subscribe callback'lerinde hata yakalama

## Testing Strategy

### Unit Tests

- `mockDataStore` CRUD operasyonlarının doğru çalıştığını test et
- Subscribe/notify mekanizmasının çalıştığını test et
- Kategori güncellemesinin ürün categoryName'lerini güncellediğini test et

### Property-Based Tests

Property-based testing için **fast-check** kütüphanesi kullanılacaktır.

Her property testi minimum 100 iterasyon çalıştırılacaktır.

Test dosyası: `frontend/src/__tests__/mockDataStore.property.test.js`

Her test, ilgili correctness property'yi referans alacaktır:

```javascript
// **Feature: admin-mock-data-sync, Property 1: Category Update Propagation**
// **Validates: Requirements 1.1**
```
