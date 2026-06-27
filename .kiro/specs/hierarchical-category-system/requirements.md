# Alt Kategori Sistemi - Detaylı Analiz ve Planlama Dokümanı

## 📋 Proje Özeti

Mevcut e-ticaret sistemine hiyerarşik kategori yapısı (Alt Kategori) eklenmesi için detaylı analiz ve uygulama planı.

---

## 🔍 1. MEVCUT MİMARİ ANALİZİ

### 1.1 Veritabanı Yapısı (✅ Hazır)

**Category Entity** zaten alt kategori mantığını destekliyor:

```csharp
public class Category : BaseEntity
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string? ImageUrl { get; set; }
    public int? ParentId { get; set; }          // ✅ Üst kategori referansı
    public int SortOrder { get; set; }
    public string Slug { get; set; }
    public bool IsActive { get; set; }

    // Navigation Properties
    public virtual Category? Parent { get; set; }                    // ✅ Üst kategori
    public virtual ICollection<Category> SubCategories { get; set; }  // ✅ Alt kategoriler
    public ICollection<Product> Products { get; set; }
}
```

**SONUÇ:** ✅ Veritabanı yapısı alt kategori sistemine hazır. Migration'a gerek yok.

---

### 1.2 Backend Servisleri (⚠️ Geliştirme Gerekli)

#### Mevcut ICategoryService Interface:

```csharp
public interface ICategoryService
{
    Task<IEnumerable<Category>> GetAllAsync();
    Task<IEnumerable<Category>> GetAllAdminAsync();
    Task<Category?> GetByIdAsync(int id);
    Task AddAsync(Category category);
    Task UpdateAsync(Category category);
    Task DeleteAsync(Category category);
    Task<Category?> GetBySlugAsync(string slug);
    Task<int> GetCategoryCountAsync();
}
```

**EKSİKLER:**

- ❌ Hiyerarşik yapı döndüren metod yok
- ❌ Ana kategorileri getiren metod yok (ParentId == null)
- ❌ Belirli kategorinin alt kategorilerini getiren metod yok
- ❌ Kategori silme validasyonu yok (alt kategorisi varsa silinemez)
- ❌ Circular reference kontrolü yok (Kategori kendi alt kategorisi olamaz)

---

### 1.3 Backend API Endpoints (⚠️ Geliştirme Gerekli)

**Mevcut AdminCategoriesController:**

```
GET    /api/admin/categories          → Tüm kategoriler (flat)
GET    /api/admin/categories/{id}     → Tekil kategori
POST   /api/admin/categories          → Yeni kategori
PUT    /api/admin/categories/{id}     → Kategori güncelle
DELETE /api/admin/categories/{id}     → Kategori sil (soft delete)
```

**EKSİKLER:**

- ❌ ParentId alanı frontend'e gönderilmiyor
- ❌ SubCategories ilişkisi yüklenmiyor
- ❌ Hiyerarşik yapı endpoint'i yok
- ❌ Ana kategoriler endpoint'i yok

---

### 1.4 Frontend Admin Paneli (❌ Tamamen Eksik)

**Mevcut AdminCategories.jsx:**

- ✅ Kategorileri grid/card view ile gösteriyor
- ✅ Ekleme/düzenleme/silme işlemleri çalışıyor
- ❌ ParentId seçimi YOK
- ❌ Alt kategori ilişkisi gösterilmiyor
- ❌ Hiyerarşik görünüm YOK (tree view)
- ❌ ParentId form alanı YOK

---

### 1.5 Frontend Müşteri Tarafı (❌ Tamamen Eksik)

**Mevcut Home.js:**

- ✅ Kategoriler düz liste olarak gösteriliyor
- ❌ Alt kategoriler gösterilmiyor
- ❌ Kategori tıklanınca alt kategoriler açılmıyor
- ❌ Breadcrumb navigasyon yok

**Product.js / Kategori Filtreleme:**

- ❌ Alt kategoriye göre filtreleme yok
- ❌ Kategori hiyerarşisi gösterilmiyor

---

## 🎯 2. GEREKLI YAPILACAK İŞLER

### 2.1 Backend API Geliştirmeleri

#### 2.1.1 ICategoryService'e Yeni Metodlar Eklenmeli

**EKLENMESİ GEREKENLER:**

```csharp
public interface ICategoryService
{
    // Mevcut metodlar...

    // ✨ YENİ: Hiyerarşik kategori ağacı
    Task<IEnumerable<CategoryTreeDto>> GetCategoryTreeAsync();

    // ✨ YENİ: Sadece ana kategoriler (üst kategorisi olmayanlar)
    Task<IEnumerable<Category>> GetRootCategoriesAsync();

    // ✨ YENİ: Belirli kategorinin alt kategorileri
    Task<IEnumerable<Category>> GetSubCategoriesAsync(int parentId);

    // ✨ YENİ: Kategori yolu (breadcrumb için)
    Task<IEnumerable<Category>> GetCategoryPathAsync(int categoryId);

    // ✨ YENİ: Circular reference kontrolü
    Task<bool> WouldCreateCircularReferenceAsync(int categoryId, int? newParentId);

    // ✨ YENİ: Alt kategori sayısı kontrolü
    Task<bool> HasSubCategoriesAsync(int categoryId);
}
```

**YENİ DTO:**

```csharp
public class CategoryTreeDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Slug { get; set; }
    public string? ImageUrl { get; set; }
    public int? ParentId { get; set; }
    public bool IsActive { get; set; }
    public int ProductCount { get; set; }
    public List<CategoryTreeDto> Children { get; set; } = new();
}
```

---

#### 2.1.2 CategoryManager Geliştirmeleri

**EKLENMESİ GEREKEN VALİDASYONLAR:**

1. **Circular Reference Kontrolü:**
   - Kategori kendi alt kategorisi olamaz
   - A → B → C → A gibi döngüsel ilişki önlenmeli

2. **Silme Validasyonu:**
   - Alt kategorisi olan kategori silinemez
   - Önce alt kategoriler silinmeli veya taşınmalı

3. **Güncelleme Validasyonu:**
   - ParentId değiştirilirken circular reference kontrolü yapılmalı

**ÖRNEK UYGULAMA:**

```csharp
public async Task UpdateAsync(Category category)
{
    // Circular reference kontrolü
    if (category.ParentId.HasValue)
    {
        var wouldCreateCircular = await WouldCreateCircularReferenceAsync(
            category.Id,
            category.ParentId.Value
        );

        if (wouldCreateCircular)
        {
            throw new InvalidOperationException(
                "Bu işlem döngüsel kategori ilişkisi oluşturur."
            );
        }
    }

    NormalizeAndValidate(category, category.Id);
    await _categoryRepository.UpdateAsync(category);
}

public async Task DeleteAsync(Category category)
{
    // Alt kategori kontrolü
    var hasSubCategories = await HasSubCategoriesAsync(category.Id);
    if (hasSubCategories)
    {
        throw new InvalidOperationException(
            "Alt kategorisi olan kategori silinemez. Önce alt kategorileri silin."
        );
    }

    category.IsActive = false;
    await _categoryRepository.UpdateAsync(category);
}
```

---

#### 2.1.3 API Controller Endpoint Eklemeleri

**AdminCategoriesController'a EKLENMESİ GEREKENLER:**

```csharp
[ApiController]
[Route("api/admin/categories")]
public class AdminCategoriesController : ControllerBase
{
    // Mevcut metodlar...

    // ✨ YENİ: Hiyerarşik kategori ağacı
    [HttpGet("tree")]
    [HasPermission(Permissions.Categories.View)]
    public async Task<IActionResult> GetCategoryTree()
    {
        var tree = await _categoryService.GetCategoryTreeAsync();
        return Ok(tree);
    }

    // ✨ YENİ: Ana kategoriler
    [HttpGet("root")]
    [HasPermission(Permissions.Categories.View)]
    public async Task<IActionResult> GetRootCategories()
    {
        var categories = await _categoryService.GetRootCategoriesAsync();
        return Ok(categories);
    }

    // ✨ YENİ: Belirli kategorinin alt kategorileri
    [HttpGet("{id}/subcategories")]
    [HasPermission(Permissions.Categories.View)]
    public async Task<IActionResult> GetSubCategories(int id)
    {
        var subCategories = await _categoryService.GetSubCategoriesAsync(id);
        return Ok(subCategories);
    }

    // ✨ YENİ: Kategori yolu (breadcrumb)
    [HttpGet("{id}/path")]
    [HasPermission(Permissions.Categories.View)]
    public async Task<IActionResult> GetCategoryPath(int id)
    {
        var path = await _categoryService.GetCategoryPathAsync(id);
        return Ok(path);
    }
}
```

**Müşteri API (CategoriesController) için:**

```csharp
[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    // ✨ YENİ: Hiyerarşik kategori ağacı (sadece aktif)
    [HttpGet("tree")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategoryTree()
    {
        var tree = await _categoryService.GetCategoryTreeAsync();
        var activeTree = tree.Where(c => c.IsActive).ToList();
        return Ok(activeTree);
    }

    // ✨ YENİ: Ana kategoriler (sadece aktif)
    [HttpGet("root")]
    [AllowAnonymous]
    public async Task<IActionResult> GetRootCategories()
    {
        var categories = await _categoryService.GetRootCategoriesAsync();
        var active = categories.Where(c => c.IsActive).ToList();
        return Ok(active);
    }
}
```

---

### 2.2 Frontend Admin Paneli Geliştirmeleri

#### 2.2.1 AdminCategories.jsx Tam Yeniden Yapılandırma

**EKLENMESİ GEREKENLER:**

1. **ParentId Seçim Alanı (Dropdown)**

   ```jsx
   <FormGroup>
     <Label>Üst Kategori</Label>
     <Input
       type="select"
       value={formData.parentId || ""}
       onChange={(e) =>
         setFormData({
           ...formData,
           parentId: e.target.value ? parseInt(e.target.value) : null,
         })
       }
     >
       <option value="">Ana Kategori (Üst Yok)</option>
       {categories
         .filter((c) => c.id !== editingCategory?.id) // Kendini gösterme
         .map((c) => (
           <option key={c.id} value={c.id}>
             {c.name}
           </option>
         ))}
     </Input>
   </FormGroup>
   ```

2. **Hiyerarşik Görünüm (Tree View)**
   - Ağaç yapısında kategori gösterimi
   - Katlanabilir alt kategoriler
   - Drag & drop ile kategori taşıma (opsiyonel)

3. **Alt Kategori Sayısı Gösterimi**

   ```jsx
   <Badge color="info">
     {category.subCategories?.length || 0} Alt Kategori
   </Badge>
   ```

4. **Silme Validasyonu**
   ```jsx
   const handleDelete = async (id) => {
     const category = categories.find((c) => c.id === id);
     const hasSubCategories = categories.some((c) => c.parentId === id);

     if (hasSubCategories) {
       alert(
         "Bu kategorinin alt kategorileri var. Önce alt kategorileri silin.",
       );
       return;
     }

     if (window.confirm("Silmek istediğinizden emin misiniz?")) {
       await categoryService.delete(id);
       fetchCategories();
     }
   };
   ```

---

#### 2.2.2 Kategori Tree Component (Yeni)

**Oluşturulması Gereken Yeni Component:**

```jsx
// frontend/src/components/CategoryTree.jsx
import React, { useState } from "react";

const CategoryTreeNode = ({ category, onEdit, onDelete, level = 0 }) => {
  const [expanded, setExpanded] = useState(true);
  const hasChildren = category.children?.length > 0;

  return (
    <div style={{ marginLeft: level * 20 }}>
      <div className="category-node">
        {hasChildren && (
          <button onClick={() => setExpanded(!expanded)}>
            <i className={`fas fa-chevron-${expanded ? "down" : "right"}`}></i>
          </button>
        )}

        <span>{category.name}</span>
        <Badge>{category.productCount} Ürün</Badge>

        <button onClick={() => onEdit(category)}>
          <i className="fas fa-edit"></i>
        </button>
        <button onClick={() => onDelete(category.id)}>
          <i className="fas fa-trash"></i>
        </button>
      </div>

      {hasChildren && expanded && (
        <div className="category-children">
          {category.children.map((child) => (
            <CategoryTreeNode
              key={child.id}
              category={child}
              onEdit={onEdit}
              onDelete={onDelete}
              level={level + 1}
            />
          ))}
        </div>
      )}
    </div>
  );
};

export default function CategoryTree({ categories, onEdit, onDelete }) {
  return (
    <div className="category-tree">
      {categories.map((category) => (
        <CategoryTreeNode
          key={category.id}
          category={category}
          onEdit={onEdit}
          onDelete={onDelete}
        />
      ))}
    </div>
  );
}
```

---

### 2.3 Frontend Müşteri Tarafı Geliştirmeleri

#### 2.3.1 Home.js - Kategori Gösterimi

**DEĞİŞMESİ GEREKEN MANTIK:**

```jsx
// Mevcut: Düz liste
<div className="categories-grid">
  {categories.map(c => (
    <CategoryTile key={c.id} category={c} />
  ))}
</div>

// ✨ YENİ: Sadece ana kategoriler göster
<div className="categories-grid">
  {categories
    .filter(c => !c.parentId) // Sadece üst kategorisi olmayanlar
    .map(c => (
      <CategoryTile key={c.id} category={c} />
    ))
  }
</div>
```

---

#### 2.3.2 CategoryTile Component Geliştirmesi

**EKLENMESİ GEREKENLER:**

```jsx
// frontend/src/pages/components/CategoryTile.jsx
export default function CategoryTile({ category }) {
  const navigate = useNavigate();

  return (
    <div
      className="category-tile"
      onClick={() => navigate(`/kategoriler/${category.slug}`)}
    >
      {category.imageUrl && <img src={category.imageUrl} alt={category.name} />}
      <h3>{category.name}</h3>

      {/* ✨ YENİ: Alt kategori sayısı */}
      {category.subCategoryCount > 0 && (
        <Badge>{category.subCategoryCount} Alt Kategori</Badge>
      )}
    </div>
  );
}
```

---

#### 2.3.3 Kategori Detay Sayfası (YENİ)

**OLUŞTURULMASI GEREKEN YENİ SAYFA:**

```jsx
// frontend/src/pages/Category.js
import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import categoryService from "../services/categoryService";

export default function Category() {
  const { slug } = useParams();
  const [category, setCategory] = useState(null);
  const [subCategories, setSubCategories] = useState([]);
  const [products, setProducts] = useState([]);
  const [breadcrumb, setBreadcrumb] = useState([]);

  useEffect(() => {
    loadCategoryData();
  }, [slug]);

  const loadCategoryData = async () => {
    const cat = await categoryService.getBySlug(slug);
    setCategory(cat);

    // Alt kategoriler
    if (cat.id) {
      const subs = await categoryService.getSubCategories(cat.id);
      setSubCategories(subs);

      // Ürünler
      const prods = await productService.getByCategoryId(cat.id);
      setProducts(prods);

      // Breadcrumb
      const path = await categoryService.getCategoryPath(cat.id);
      setBreadcrumb(path);
    }
  };

  return (
    <div>
      {/* Breadcrumb */}
      <nav className="breadcrumb">
        <Link to="/">Ana Sayfa</Link>
        {breadcrumb.map((cat) => (
          <Link key={cat.id} to={`/kategoriler/${cat.slug}`}>
            {cat.name}
          </Link>
        ))}
      </nav>

      <h1>{category?.name}</h1>

      {/* Alt Kategoriler */}
      {subCategories.length > 0 && (
        <section className="subcategories">
          <h2>Alt Kategoriler</h2>
          <div className="subcategory-grid">
            {subCategories.map((sub) => (
              <CategoryTile key={sub.id} category={sub} />
            ))}
          </div>
        </section>
      )}

      {/* Ürünler */}
      <section className="products">
        <h2>Ürünler</h2>
        <div className="product-grid">
          {products.map((p) => (
            <ProductCard key={p.id} product={p} />
          ))}
        </div>
      </section>
    </div>
  );
}
```

**ROUTE EKLENMESİ:**

```jsx
// frontend/src/App.js
<Route path="/kategoriler/:slug" element={<Category />} />
```

---

#### 2.3.4 Ürün Filtreleme Sayfası Güncellenmesi

**Product.js'de Kategori Filtresi:**

```jsx
// ✨ YENİ: Alt kategorilere göre filtreleme
const [selectedCategory, setSelectedCategory] = useState(null);
const [categoryTree, setCategoryTree] = useState([]);

useEffect(() => {
  categoryService.getCategoryTree().then(setCategoryTree);
}, []);

const filterByCategory = (categoryId) => {
  setSelectedCategory(categoryId);
  // Seçilen kategori ve tüm alt kategorilerindeki ürünleri getir
  productService.getByCategoryAndSubCategories(categoryId).then(setProducts);
};
```

---

### 2.4 Frontend Servisleri Güncelleme

**categoryService.js'e eklenecekler:**

```javascript
// frontend/src/services/categoryService.js

const categoryService = {
  // Mevcut metodlar...

  // ✨ YENİ: Hiyerarşik ağaç
  getCategoryTree: async () => {
    const response = await api.get("/api/categories/tree");
    return response.data;
  },

  // ✨ YENİ: Ana kategoriler
  getRootCategories: async () => {
    const response = await api.get("/api/categories/root");
    return response.data;
  },

  // ✨ YENİ: Alt kategoriler
  getSubCategories: async (parentId) => {
    const response = await api.get(
      `/api/admin/categories/${parentId}/subcategories`,
    );
    return response.data;
  },

  // ✨ YENİ: Kategori yolu
  getCategoryPath: async (categoryId) => {
    const response = await api.get(`/api/admin/categories/${categoryId}/path`);
    return response.data;
  },

  // ✨ YENİ: Slug'a göre getir
  getBySlug: async (slug) => {
    const response = await api.get(`/api/categories/slug/${slug}`);
    return response.data;
  },
};
```

---

## 📊 3. ÖRNEK KULLANIM SENARYOLARI

### Senaryo 1: Temizlik → Kişisel Bakım

**Veritabanı Yapısı:**

```
Temizlik (id:1, parentId:null)
  ├─ Kişisel Bakım (id:2, parentId:1)
  │   ├─ Şampuan (id:3, parentId:2)
  │   ├─ Sabun (id:4, parentId:2)
  │   └─ Diş Macunu (id:5, parentId:2)
  │
  ├─ Ev Temizliği (id:6, parentId:1)
  │   ├─ Deterjan (id:7, parentId:6)
  │   ├─ Yumuşatıcı (id:8, parentId:6)
  │   └─ Bulaşık Deterjanı (id:9, parentId:6)
  │
  └─ Güneş Ürünleri (id:10, parentId:1)
      ├─ Güneş Kremi (id:11, parentId:10)
      └─ Bronzlaştırıcı (id:12, parentId:10)
```

**Müşteri Deneyimi:**

1. Ana sayfada "Temizlik" kategorisi görünür
2. Tıklanınca → Alt kategoriler: "Kişisel Bakım", "Ev Temizliği", "Güneş Ürünleri"
3. "Kişisel Bakım" tıklanınca → Alt kategoriler: "Şampuan", "Sabun", "Diş Macunu"
4. "Şampuan" tıklanınca → Şampuan ürünleri listelenir

**Breadcrumb:**

```
Ana Sayfa > Temizlik > Kişisel Bakım > Şampuan
```

---

### Senaryo 2: Gıda → Süt Ürünleri

```
Gıda (id:20, parentId:null)
  ├─ Süt Ürünleri (id:21, parentId:20)
  │   ├─ Süt (id:22, parentId:21)
  │   ├─ Yoğurt (id:23, parentId:21)
  │   ├─ Peynir (id:24, parentId:21)
  │   └─ Tereyağı (id:25, parentId:21)
  │
  ├─ Meyve & Sebze (id:26, parentId:20)
  │   ├─ Meyveler (id:27, parentId:26)
  │   └─ Sebzeler (id:28, parentId:26)
  │
  └─ Et & Tavuk (id:29, parentId:20)
      ├─ Kırmızı Et (id:30, parentId:29)
      └─ Beyaz Et (id:31, parentId:29)
```

---

## 🚀 4. UYGULAMA ADIMLARI (SIRA ÖNEMLİ)

### Adım 1: Backend Repository Geliştirmesi

- [ ] `ICategoryRepository`'ye yeni metodlar ekle
- [ ] `CategoryRepository`'de metodları implement et
- [ ] Alt kategori sorguları için EF Include ekle

### Adım 2: Backend Service Geliştirmesi

- [ ] `ICategoryService`'e yeni metodlar ekle
- [ ] `CategoryManager`'da validasyonları implement et
- [ ] Circular reference kontrolü
- [ ] Alt kategori silme kontrolü

### Adım 3: Backend API Endpoint'leri

- [ ] `AdminCategoriesController`'a yeni endpoint'ler ekle
- [ ] `CategoriesController`'a müşteri endpoint'leri ekle
- [ ] DTO'ları oluştur

### Adım 4: Frontend Servisler

- [ ] `categoryService.js`'e yeni metodlar ekle
- [ ] API çağrılarını test et

### Adım 5: Frontend Admin Paneli

- [ ] `AdminCategories.jsx`'e ParentId dropdown ekle
- [ ] CategoryTree component'i oluştur
- [ ] Silme validasyonunu ekle

### Adım 6: Frontend Müşteri Tarafı

- [ ] Ana sayfada sadece root kategorileri göster
- [ ] Kategori detay sayfası oluştur
- [ ] Breadcrumb navigasyon ekle
- [ ] Alt kategori listesi göster

### Adım 7: Test

- [ ] Backend unit testleri
- [ ] API endpoint testleri
- [ ] Frontend integration testleri

---

## ⚠️ 5. DİKKAT EDİLMESİ GEREKENLER

### 5.1 Performans

- Kategori ağacı için cache mekanizması
- Recursive sorgular için optimizasyon
- N+1 sorgu problemine dikkat (EF Include kullan)

### 5.2 Güvenlik

- ParentId validasyonu
- Circular reference kontrolü
- Yetki kontrolü (sadece admin kategorileri düzenleyebilir)

### 5.3 UX

- Kategori derinliği maksimum 3-4 seviye olmalı
- Mobil uyumluluk
- Kategori tıklamaları hızlı olmalı

### 5.4 Data Migration

- Mevcut kategorilere ParentId atanmalı mı?
- Eski verilerin kontrolü

---

## 📈 6. BAŞARI KRİTERLERİ

✅ Admin panelinde kategoriler hiyerarşik yapıda gösteriliyor
✅ Alt kategori ekleme/düzenleme/silme çalışıyor
✅ Müşteri tarafında kategori navigasyonu çalışıyor
✅ Breadcrumb navigasyon doğru çalışıyor
✅ Ürünler doğru kategorilerde listeleniyor
✅ Performans problemi yok
✅ Circular reference önleniyor

---

## 📝 7. TAHMINI SÜRE

| Görev              | Tahmini Süre |
| ------------------ | ------------ |
| Backend Repository | 2 saat       |
| Backend Service    | 3 saat       |
| Backend API        | 2 saat       |
| Frontend Servisler | 1 saat       |
| Admin Panel UI     | 4 saat       |
| Müşteri UI         | 4 saat       |
| Test               | 2 saat       |
| **TOPLAM**         | **18 saat**  |

---

## 🔗 8. İLGİLİ DOSYALAR

### Backend

- `src/ECommerce.Entities/Concrete/Category.cs` (✅ Hazır)
- `src/ECommerce.Core/Interfaces/ICategoryRepository.cs` (⚠️ Güncellenecek)
- `src/ECommerce.Data/Repositories/CategoryRepository.cs` (⚠️ Güncellenecek)
- `src/ECommerce.Business/Services/Interfaces/ICategoryService.cs` (⚠️ Güncellenecek)
- `src/ECommerce.Business/Services/Managers/CategoryManager.cs` (⚠️ Güncellenecek)
- `src/ECommerce.API/Controllers/Admin/AdminCategoriesController.cs` (⚠️ Güncellenecek)

### Frontend

- `frontend/src/pages/Admin/AdminCategories.jsx` (⚠️ Güncellenecek)
- `frontend/src/services/categoryService.js` (⚠️ Güncellenecek)
- `frontend/src/pages/Home.js` (⚠️ Güncellenecek)
- `frontend/src/pages/Category.js` (❌ Oluşturulacak)
- `frontend/src/components/CategoryTree.jsx` (❌ Oluşturulacak)

---

## ✅ SONUÇ

Mevcut mimari **alt kategori sistemine hazır** durumda. Veritabanı yapısı mevcut, sadece backend ve frontend tarafında geliştirme yapılması gerekiyor. Yukarıdaki adımlar takip edildiğinde sistem tamamen çalışır hale gelecektir.

**Kod yazma öncesi bu doküman onaylanmalıdır.**
