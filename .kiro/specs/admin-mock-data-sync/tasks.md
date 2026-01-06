# Implementation Plan

- [x] 1. AdminService'i MockDataStore ile entegre et

  - [x] 1.1 AdminService içindeki duplicate mockCategories ve mockProducts dizilerini kaldır

    - `adminService.js` dosyasındaki `let mockCategories = [...]` ve `let mockProducts = [...]` tanımlarını sil
    - Tüm kategori ve ürün işlemlerini `mockDataStore` üzerinden yap
    - _Requirements: 4.1, 4.2_

  - [x] 1.2 Kategori CRUD işlemlerini mockDataStore'a yönlendir

    - `getCategories()` → `mockDataStore.getAllCategories()`
    - `createCategory()` → `mockDataStore.createCategory()`
    - `updateCategory()` → `mockDataStore.updateCategory()`
    - `deleteCategory()` → `mockDataStore.deleteCategory()`
    - _Requirements: 1.1, 4.1_

  - [x] 1.3 Ürün CRUD işlemlerini mockDataStore'a yönlendir

    - `getProducts()` → `mockDataStore.getAllProducts()`
    - `createProduct()` → `mockDataStore.createProduct()`
    - `updateProduct()` → `mockDataStore.updateProduct()`
    - `deleteProduct()` → `mockDataStore.deleteProduct()`
    - _Requirements: 2.1, 3.1, 4.1_

  - [ ]\* 1.4 Write property test for category update propagation

    - **Property 1: Category Update Propagation**
    - **Validates: Requirements 1.1**

  - [ ]\* 1.5 Write property test for category-product name consistency
    - **Property 2: Category-Product Name Consistency**
    - **Validates: Requirements 1.4**

- [x] 2. ProductGrid bileşenine subscribe mekanizması ekle

  - [x] 2.1 ProductGrid'e mockDataStore subscription ekle

    - `useEffect` içinde `mockDataStore.subscribe("products", loadProducts)` çağır
    - Cleanup fonksiyonunda unsubscribe yap
    - _Requirements: 2.2, 2.3, 3.2_

  - [ ]\* 2.2 Write property test for product addition

    - **Property 3: Product Addition Increases List Size**
    - **Validates: Requirements 2.1**

  - [ ]\* 2.3 Write property test for product deletion
    - **Property 5: Product Deletion Decreases List Size**
    - **Validates: Requirements 3.1**

- [x] 3. Checkpoint - Tüm testlerin geçtiğinden emin ol

  - Ensure all tests pass, ask the user if questions arise.

- [x] 4. ProductService'i MockDataStore ile entegre et

  - [x] 4.1 ProductService'in mock modda mockDataStore kullanmasını sağla

    - `list()` → `mockDataStore.getProducts()`
    - `get(id)` → `mockDataStore.getProductById(id)`
    - _Requirements: 2.2, 3.2, 4.3_

  - [ ]\* 4.2 Write property test for inactive products filtering
    - **Property 7: Inactive Products Not In Public List**
    - **Validates: Requirements 3.3**

- [x] 5. Final Checkpoint - Tüm testlerin geçtiğinden emin ol

  - Ensure all tests pass, ask the user if questions arise.
