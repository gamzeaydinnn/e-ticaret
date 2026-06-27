import React, { useState, useEffect } from "react";
import categoryService from "../../services/categoryService";
import CategoryTree from "../../components/CategoryTree";

const AdminCategories = () => {
  const [categories, setCategories] = useState([]);
  const [categoryTree, setCategoryTree] = useState([]);
  const [viewMode, setViewMode] = useState("grid"); // 'grid' or 'tree'
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [showModal, setShowModal] = useState(false);
  const [editingCategory, setEditingCategory] = useState(null);
  const [formData, setFormData] = useState({
    name: "",
    slug: "",
    description: "",
    parentId: null,
    imageUrl: "",
    sortOrder: 0,
    isActive: true,
  });

  useEffect(() => {
    fetchCategories();
  }, []);

  const fetchCategories = async () => {
    try {
      setLoading(true);
      const categoriesData = await categoryService.getAllAdmin();
      setCategories(categoriesData);

      // Tree view için hiyerarşik veri çek
      const treeData = await categoryService.getAdminCategoryTree();
      setCategoryTree(treeData);
    } catch (err) {
      setError("Kategoriler yüklenirken hata oluştu");
      console.error("Kategoriler yükleme hatası:", err);
    } finally {
      setLoading(false);
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();

    // Alt kategorisi olan kategori silinmeye çalışılıyorsa kontrol et
    const hasSubCategories = categories.some(
      (c) => c.parentId === parseInt(formData.parentId),
    );

    try {
      if (editingCategory) {
        await categoryService.update(editingCategory.id, formData);
      } else {
        await categoryService.create(formData);
      }
      setShowModal(false);
      setFormData({
        name: "",
        slug: "",
        description: "",
        parentId: null,
        imageUrl: "",
        sortOrder: 0,
        isActive: true,
      });
      setEditingCategory(null);
      fetchCategories();
    } catch (err) {
      console.error("Kategori kaydetme hatası:", err);
      const errorMessage =
        err.response?.data?.message || err.message || "Bir hata oluştu";
      alert(errorMessage);
    }
  };

  const handleEdit = (category) => {
    setEditingCategory(category);
    setFormData({
      name: category.name,
      slug: category.slug || "",
      description: category.description || "",
      parentId: category.parentId || null,
      imageUrl: category.imageUrl || "",
      sortOrder: category.sortOrder || 0,
      isActive: category.isActive,
    });
    setShowModal(true);
  };

  const handleDelete = async (id) => {
    const cat = categories.find((c) => c.id === id);

    // 1. Alt kategorisi var mı kontrol et
    const subCats = categories.filter((c) => c.parentId === id);
    if (subCats.length > 0) {
      const subNames = subCats.map((c) => `• ${c.name}`).join("\n");
      alert(
        `❌ "${cat?.name}" kategorisi silinemez!\n\n` +
        `Bu kategorinin ${subCats.length} alt kategorisi var:\n${subNames}\n\n` +
        `Önce bu alt kategorileri silin veya üst kategorilerini değiştirin.`,
      );
      return;
    }

    // 2. Bu kategoriye bağlı ürün var mı — backend'e gitmeden önceden kontrol
    const productCount = cat?.productCount ?? 0;
    if (productCount > 0) {
      alert(
        `❌ "${cat?.name}" kategorisi silinemez!\n\n` +
        `Bu kategoriye bağlı ${productCount} adet ürün var.\n\n` +
        `Silmeden önce bu ürünleri başka bir kategoriye taşıyın veya silin.\n` +
        `(Admin → Ürünler bölümünden filtreleyebilirsiniz)`,
      );
      return;
    }

    if (window.confirm(`"${cat?.name}" kategorisini silmek istediğinizden emin misiniz?`)) {
      try {
        await categoryService.delete(id);
        fetchCategories();
      } catch (err) {
        console.error("Kategori silme hatası:", err);
        const errorMessage =
          err.response?.data?.message ||
          err.message ||
          "Silme işlemi başarısız";
        alert(`❌ Hata: ${errorMessage}`);
      }
    }
  };

  // Alt kategori ekleme: üst kategoriyi seçili olarak formu aç
  const handleAddSubCategory = (parentCategory) => {
    setEditingCategory(null);
    setFormData({
      name: "",
      slug: "",
      description: "",
      parentId: parentCategory.id,  // Üst kategori otomatik seçili
      imageUrl: "",
      sortOrder: 0,
      isActive: true,
    });
    setShowModal(true);
  };

  if (loading) {
    return (
      <div
        className="d-flex justify-content-center align-items-center"
        style={{ height: "400px" }}
      >
        <div className="spinner-border text-primary" role="status">
          <span className="visually-hidden">Yükleniyor...</span>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="alert alert-danger" role="alert">
        {error}
      </div>
    );
  }

  return (
    <div
      className="container-fluid px-2 px-md-3"
      style={{ overflow: "hidden", maxWidth: "100%" }}
    >
      {/* Header */}
      <div className="d-flex flex-wrap justify-content-between align-items-center mb-3 gap-2">
        <div>
          <h5
            className="mb-0 fw-bold"
            style={{ color: "#1e293b", fontSize: "1rem" }}
          >
            <i
              className="fas fa-layer-group me-2"
              style={{ color: "#f97316" }}
            ></i>
            Kategori Yönetimi
          </h5>
          <p
            className="text-muted mb-0 d-none d-sm-block"
            style={{ fontSize: "0.75rem" }}
          >
            Ürün kategorilerini düzenleyin
          </p>
        </div>

        <div className="d-flex gap-2 align-items-center">
          {/* View Mode Toggle */}
          <div className="btn-group" role="group">
            <button
              type="button"
              className={`btn btn-sm ${viewMode === "grid" ? "btn-primary" : "btn-outline-secondary"}`}
              onClick={() => setViewMode("grid")}
              title="Kart Görünümü"
              style={{ fontSize: "0.75rem" }}
            >
              <i className="fas fa-th"></i>
            </button>
            <button
              type="button"
              className={`btn btn-sm ${viewMode === "tree" ? "btn-primary" : "btn-outline-secondary"}`}
              onClick={() => setViewMode("tree")}
              title="Ağaç Görünümü"
              style={{ fontSize: "0.75rem" }}
            >
              <i className="fas fa-sitemap"></i>
            </button>
          </div>

          <button
            className="btn border-0 text-white fw-medium px-2 py-1"
            style={{
              background: "linear-gradient(135deg, #f97316, #fb923c)",
              borderRadius: "6px",
              fontSize: "0.75rem",
              boxShadow: "0 2px 8px rgba(249, 115, 22, 0.25)",
            }}
            onClick={() => {
              setEditingCategory(null);
              setFormData({
                name: "",
                slug: "",
                description: "",
                parentId: null,
                imageUrl: "",
                sortOrder: 0,
                isActive: true,
              });
              setShowModal(true);
            }}
          >
            <i className="fas fa-plus me-1"></i>
            Yeni
          </button>
        </div>
      </div>

      {/* Categories Grid - 2'li mobil */}
      {viewMode === "grid" ? (
        <div className="row g-2 g-md-3">
          {categories.map((category) => (
            <div key={category.id} className="col-6 col-md-6 col-xl-4">
              <div
                className="card h-100 border-0 shadow-sm"
                style={{ borderRadius: "10px" }}
              >
                <div className="card-body p-2 p-md-3">
                  <div className="d-flex align-items-start justify-content-between mb-2">
                    <div
                      className="d-flex align-items-center justify-content-center rounded-circle overflow-hidden"
                      style={{
                        width: "36px",
                        height: "36px",
                        minWidth: "36px",
                        background: "#f5f5f5",
                      }}
                    >
                      {category.imageUrl ? (
                        <img
                          src={category.imageUrl}
                          alt={category.name}
                          style={{
                            width: "100%",
                            height: "100%",
                            objectFit: "cover",
                          }}
                          onError={(e) => {
                            e.currentTarget.style.display = "none";
                          }}
                        />
                      ) : (
                        <i
                          className="fas fa-folder"
                          style={{ color: "#f57c00", fontSize: "0.9rem" }}
                        ></i>
                      )}
                    </div>
                    <span
                      className={`badge rounded-pill ${
                        category.isActive ? "bg-success" : "bg-secondary"
                      }`}
                      style={{ fontSize: "0.6rem", padding: "0.2em 0.5em" }}
                    >
                      {category.isActive ? "Aktif" : "Pasif"}
                    </span>
                  </div>

                  <h6
                    className="card-title fw-bold mb-1 text-truncate"
                    style={{ color: "#2d3748", fontSize: "0.85rem" }}
                  >
                    {category.name}
                  </h6>

                  {category.parentId && (
                    <div className="mb-1">
                      <span
                        className="badge bg-light text-dark"
                        style={{ fontSize: "0.6rem" }}
                      >
                        <i className="fas fa-level-up-alt me-1"></i>
                        {categories.find((c) => c.id === category.parentId)
                          ?.name || "Üst Kategori"}
                      </span>
                    </div>
                  )}

                  {categories.filter((c) => c.parentId === category.id).length >
                    0 && (
                    <div className="mb-1">
                      <span
                        className="badge bg-info text-white"
                        style={{ fontSize: "0.6rem" }}
                      >
                        <i className="fas fa-sitemap me-1"></i>
                        {
                          categories.filter((c) => c.parentId === category.id)
                            .length
                        }{" "}
                        Alt Kategori
                      </span>
                    </div>
                  )}

                  {/* Ürün sayısı — sıfırdan fazlaysa göster */}
                  {(category.productCount ?? 0) > 0 && (
                    <div className="mb-1">
                      <span
                        className="badge bg-warning text-dark"
                        style={{ fontSize: "0.6rem" }}
                        title="Bu kategoriye bağlı ürün sayısı — silerken engel oluşturur"
                      >
                        <i className="fas fa-box me-1"></i>
                        {category.productCount} ürün
                      </span>
                    </div>
                  )}

                  <p
                    className="text-muted mb-2 text-truncate"
                    style={{ fontSize: "0.7rem" }}
                  >
                    {category.description || "Açıklama yok"}
                  </p>

                  <div className="d-flex justify-content-end gap-1 flex-wrap">
                    {/* Alt kategori ekle - sadece ana kategorilerde göster */}
                    {!category.parentId && (
                      <button
                        className="btn btn-sm btn-outline-success p-1"
                        onClick={() => handleAddSubCategory(category)}
                        title="Alt Kategori Ekle"
                        style={{ fontSize: "0.7rem", lineHeight: 1 }}
                      >
                        <i className="fas fa-plus"></i>
                        <i className="fas fa-level-down-alt ms-1" style={{ fontSize: "0.6rem" }}></i>
                      </button>
                    )}
                    <button
                      className="btn btn-sm btn-outline-primary p-1"
                      onClick={() => handleEdit(category)}
                      title="Düzenle"
                      style={{ fontSize: "0.7rem", lineHeight: 1 }}
                    >
                      <i className="fas fa-edit"></i>
                    </button>
                    <button
                      className="btn btn-sm btn-outline-danger p-1"
                      onClick={() => handleDelete(category.id)}
                      title="Sil"
                      style={{ fontSize: "0.7rem", lineHeight: 1 }}
                    >
                      <i className="fas fa-trash"></i>
                    </button>
                  </div>
                </div>
              </div>
            </div>
          ))}

          {categories.length === 0 && (
            <div className="col-12">
              <div className="text-center py-4">
                <i
                  className="fas fa-layer-group fa-3x text-muted mb-2"
                  style={{ opacity: 0.3 }}
                ></i>
                <h6 className="text-muted mb-1">Henüz kategori yok</h6>
                <p className="text-muted small">"Yeni" butonuna tıklayın.</p>
              </div>
            </div>
          )}
        </div>
      ) : (
        <CategoryTree
          categories={categoryTree}
          onEdit={handleEdit}
          onDelete={handleDelete}
        />
      )}

      {/* Modal */}
      {showModal && (
        <div
          className="modal d-block"
          style={{ backgroundColor: "rgba(0,0,0,0.5)" }}
        >
          <div className="modal-dialog modal-dialog-centered mx-2">
            <div
              className="modal-content border-0"
              style={{ borderRadius: "12px" }}
            >
              <div className="modal-header border-0 p-3">
                <h6
                  className="modal-title fw-bold"
                  style={{ color: "#2d3748" }}
                >
                  <i
                    className="fas fa-layer-group me-2"
                    style={{ color: "#f57c00" }}
                  ></i>
                  {editingCategory
                    ? "Düzenle"
                    : formData.parentId
                      ? `Alt Kategori Ekle → ${categories.find((c) => c.id === formData.parentId)?.name || ""}`
                      : "Yeni Ana Kategori"}
                </h6>
                <button
                  className="btn-close btn-close-sm"
                  onClick={() => setShowModal(false)}
                ></button>
              </div>

              <form onSubmit={handleSubmit}>
                <div className="modal-body p-3">
                  <div className="mb-3">
                    <label
                      className="form-label fw-semibold mb-1"
                      style={{ fontSize: "0.8rem" }}
                    >
                      Kategori Adı *
                    </label>
                    <input
                      type="text"
                      className="form-control form-control-sm border-0 py-2"
                      style={{
                        background: "rgba(245, 124, 0, 0.05)",
                        borderRadius: "8px",
                      }}
                      value={formData.name}
                      onChange={(e) =>
                        setFormData({ ...formData, name: e.target.value })
                      }
                      onBlur={() => {
                        if (!formData.slug?.trim() && formData.name?.trim()) {
                          const slug = formData.name
                            .toLowerCase()
                            .replaceAll("ç", "c")
                            .replaceAll("ğ", "g")
                            .replaceAll("ı", "i")
                            .replaceAll("ö", "o")
                            .replaceAll("ş", "s")
                            .replaceAll("ü", "u")
                            .replace(/[^a-z0-9\s-]/g, "")
                            .replace(/\s+/g, "-")
                            .replace(/-+/g, "-")
                            .trim();
                          setFormData((f) => ({ ...f, slug }));
                        }
                      }}
                      required
                      placeholder="Örn: Temizlik, Kişisel Bakım"
                    />
                  </div>

                  <div className="mb-3">
                    <label
                      className="form-label fw-semibold mb-1"
                      style={{ fontSize: "0.8rem" }}
                    >
                      URL Slug *
                    </label>
                    <input
                      type="text"
                      className="form-control form-control-sm border-0 py-2"
                      style={{
                        background: "rgba(245, 124, 0, 0.05)",
                        borderRadius: "8px",
                      }}
                      value={formData.slug}
                      onChange={(e) =>
                        setFormData({ ...formData, slug: e.target.value })
                      }
                      placeholder="ornegin: temizlik-urunleri"
                      required
                    />
                    <small
                      className="text-muted"
                      style={{ fontSize: "0.65rem" }}
                    >
                      URL'de görünecek isim (otomatik oluşturulur)
                    </small>
                  </div>

                  <div className="mb-3">
                    <label
                      className="form-label fw-semibold mb-1"
                      style={{ fontSize: "0.8rem" }}
                    >
                      <i
                        className="fas fa-sitemap me-1"
                        style={{ color: "#f57c00" }}
                      ></i>
                      Üst Kategori
                    </label>
                    <select
                      className="form-select form-select-sm border-0 py-2"
                      style={{
                        background: "rgba(245, 124, 0, 0.05)",
                        borderRadius: "8px",
                      }}
                      value={formData.parentId || ""}
                      onChange={(e) =>
                        setFormData({
                          ...formData,
                          parentId: e.target.value
                            ? parseInt(e.target.value)
                            : null,
                        })
                      }
                    >
                      <option value="">Ana Kategori (Üst Yok)</option>
                      {categories
                        .filter((c) => c.id !== editingCategory?.id) // Kendini gösterme
                        .map((c) => (
                          <option key={c.id} value={c.id}>
                            {c.parentId && "  ↳ "}
                            {c.name}
                          </option>
                        ))}
                    </select>
                    <small
                      className="text-muted"
                      style={{ fontSize: "0.65rem" }}
                    >
                      Boş bırakırsanız ana kategori olur
                    </small>
                  </div>

                  <div className="mb-3">
                    <label
                      className="form-label fw-semibold mb-1"
                      style={{ fontSize: "0.8rem" }}
                    >
                      Açıklama
                    </label>
                    <textarea
                      className="form-control form-control-sm border-0 py-2"
                      rows="2"
                      style={{
                        background: "rgba(245, 124, 0, 0.05)",
                        borderRadius: "8px",
                      }}
                      value={formData.description}
                      onChange={(e) =>
                        setFormData({
                          ...formData,
                          description: e.target.value,
                        })
                      }
                      placeholder="Kategori açıklaması (opsiyonel)"
                    />
                  </div>

                  <div className="mb-3">
                    <label
                      className="form-label fw-semibold mb-1"
                      style={{ fontSize: "0.8rem" }}
                    >
                      Sıra
                    </label>
                    <input
                      type="number"
                      className="form-control form-control-sm border-0 py-2"
                      style={{
                        background: "rgba(245, 124, 0, 0.05)",
                        borderRadius: "8px",
                      }}
                      value={formData.sortOrder}
                      onChange={(e) =>
                        setFormData({
                          ...formData,
                          sortOrder: parseInt(e.target.value) || 0,
                        })
                      }
                      placeholder="0"
                    />
                    <small
                      className="text-muted"
                      style={{ fontSize: "0.65rem" }}
                    >
                      Kategorilerin sıralama önceliği (küçük önce)
                    </small>
                  </div>

                  <div className="form-check">
                    <input
                      className="form-check-input"
                      type="checkbox"
                      checked={formData.isActive}
                      onChange={(e) =>
                        setFormData({ ...formData, isActive: e.target.checked })
                      }
                    />
                    <label
                      className="form-check-label fw-semibold"
                      style={{ fontSize: "0.8rem" }}
                    >
                      Aktif
                    </label>
                  </div>
                </div>

                <div className="modal-footer border-0 p-3 pt-0">
                  <button
                    type="button"
                    className="btn btn-light btn-sm me-2"
                    onClick={() => setShowModal(false)}
                  >
                    İptal
                  </button>
                  <button
                    type="submit"
                    className="btn btn-sm text-white fw-semibold px-3"
                    style={{
                      background: "linear-gradient(135deg, #f57c00, #ff9800)",
                      borderRadius: "6px",
                    }}
                  >
                    {editingCategory ? "Güncelle" : "Kaydet"}
                  </button>
                </div>
              </form>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default AdminCategories;
