import React, { useState, useEffect, useRef } from "react";
import categoryService from "../../services/categoryService";
import CategoryTree from "../../components/CategoryTree";
import { getApiBaseUrl } from "../../config/apiConfig";

const resolvePreviewUrl = (url) => {
  if (!url) return "";
  if (url.startsWith("http://") || url.startsWith("https://")) return url;
  const base = getApiBaseUrl().replace(/\/api\/?$/, "");
  return url.startsWith("/") ? `${base}${url}` : `${base}/${url}`;
};

const AdminCategories = () => {
  const [categories, setCategories] = useState([]);
  const [categoryTree, setCategoryTree] = useState([]);
  const [viewMode, setViewMode] = useState("grid"); // 'grid' or 'tree'
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [showModal, setShowModal] = useState(false);
  const [editingCategory, setEditingCategory] = useState(null);
  const [imageFile, setImageFile] = useState(null);
  const [imagePreview, setImagePreview] = useState(null);
  const [uploadingImage, setUploadingImage] = useState(false);
  const imageInputRef = useRef(null);
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

  useEffect(() => {
    if (viewMode === "tree" && categoryTree.length === 0 && !loading) {
      fetchCategoryTree();
    }
  }, [viewMode]);

  const fetchCategories = async () => {
    try {
      setLoading(true);
      setError(null);
      const categoriesData = await categoryService.getAllAdmin();
      setCategories(categoriesData);
      // Ağaç daha önce yüklendiyse veya ağaç görünümündeyse yenile
      if (viewMode === "tree" || categoryTree.length > 0) {
        await fetchCategoryTree();
      }
    } catch (err) {
      setError("Kategoriler yüklenirken hata oluştu");
      console.error("Kategoriler yükleme hatası:", err);
    } finally {
      setLoading(false);
    }
  };

  const fetchCategoryTree = async () => {
    try {
      const treeData = await categoryService.getAdminCategoryTree();
      setCategoryTree(treeData || []);
    } catch (err) {
      console.error("Kategori ağacı yükleme hatası:", err);
    }
  };

  const resetImageState = () => {
    setImageFile(null);
    setImagePreview(null);
    if (imageInputRef.current) imageInputRef.current.value = "";
  };

  const handleImageSelect = (event) => {
    const file = event.target.files?.[0];
    if (!file) return;

    if (!file.type.startsWith("image/")) {
      alert("Lütfen bir görsel dosyası seçin (jpg, png, webp).");
      return;
    }

    if (file.size > 10 * 1024 * 1024) {
      alert("Görsel boyutu en fazla 10MB olabilir.");
      return;
    }

    setImageFile(file);
    const reader = new FileReader();
    reader.onloadend = () => setImagePreview(reader.result);
    reader.readAsDataURL(file);
  };

  const removeImage = () => {
    resetImageState();
    setFormData((prev) => ({ ...prev, imageUrl: "" }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();

    try {
      let finalImageUrl = formData.imageUrl?.trim() || "";

      if (imageFile) {
        setUploadingImage(true);
        const uploadedUrl = await categoryService.uploadCategoryImage(imageFile);
        setUploadingImage(false);
        if (!uploadedUrl) {
          alert("Görsel yüklenemedi. Lütfen tekrar deneyin.");
          return;
        }
        finalImageUrl = uploadedUrl;
      }

      const payload = { ...formData, imageUrl: finalImageUrl };

      if (editingCategory) {
        await categoryService.update(editingCategory.id, payload);
      } else {
        await categoryService.create(payload);
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
      resetImageState();
      setEditingCategory(null);
      fetchCategories();
    } catch (err) {
      setUploadingImage(false);
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
    resetImageState();
    setImagePreview(category.imageUrl ? resolvePreviewUrl(category.imageUrl) : null);
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
    resetImageState();
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
        <div className="mt-2">
          <button
            type="button"
            className="btn btn-sm btn-outline-danger"
            onClick={fetchCategories}
          >
            Tekrar dene
          </button>
        </div>
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
          <div className="btn-group" role="group" aria-label="Görünüm">
            <button
              type="button"
              className={`btn btn-sm ${viewMode === "grid" ? "btn-primary" : "btn-outline-secondary"}`}
              onClick={() => setViewMode("grid")}
              title="Kart görünümü"
              style={{ fontSize: "0.75rem" }}
            >
              <i className="fas fa-th me-1"></i>
              Kart
            </button>
            <button
              type="button"
              className={`btn btn-sm ${viewMode === "tree" ? "btn-primary" : "btn-outline-secondary"}`}
              onClick={() => setViewMode("tree")}
              title="Ağaç görünümü (ana → alt)"
              style={{ fontSize: "0.75rem" }}
            >
              <i className="fas fa-sitemap me-1"></i>
              Ağaç
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
              resetImageState();
              setShowModal(true);
            }}
          >
            <i className="fas fa-plus me-1"></i>
            Ana kategori
          </button>
        </div>
      </div>

      <div
        className="mb-3 px-2 py-2 rounded-2"
        style={{ background: "#f8fafc", fontSize: "0.72rem", color: "#64748b" }}
      >
        <strong style={{ color: "#334155" }}>Nasıl kullanılır:</strong>{" "}
        <span className="badge text-bg-primary me-1" style={{ fontSize: "0.65rem" }}>Ana kategori</span>
        ve
        <span className="badge text-bg-secondary mx-1" style={{ fontSize: "0.65rem" }}>Alt kategori</span>
        ana sayfa &quot;Keşfet&quot; alanında birlikte listelenir.
        Kartta <strong>Düzenle</strong> → görsel ekle.
        <strong> + Alt ekle</strong> alt kategori,
        <strong> Sil</strong> silme.
      </div>

      {/* Categories Grid - 2'li mobil */}
      {viewMode === "grid" ? (
        <div className="row g-2 g-md-3">
          {categories.map((category) => {
            const isSub = Boolean(category.parentId);
            const parentName = isSub
              ? categories.find((c) => c.id === category.parentId)?.name
              : null;
            const childCount = categories.filter(
              (c) => c.parentId === category.id,
            ).length;
            const productCount = category.productCount ?? 0;

            return (
            <div key={category.id} className="col-6 col-md-6 col-xl-4">
              <div
                className="card h-100 border-0 shadow-sm"
                style={{
                  borderRadius: "10px",
                  borderLeft: isSub
                    ? "3px solid #94a3b8"
                    : "3px solid #f97316",
                }}
              >
                <div className="card-body p-2 p-md-3">
                  <div className="d-flex align-items-start justify-content-between mb-2 gap-1">
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
                          src={resolvePreviewUrl(category.imageUrl)}
                          alt={category.name}
                          style={{
                            width: "100%",
                            height: "100%",
                            objectFit: "cover",
                          }}
                          onError={(e) => {
                            e.currentTarget.style.display = "none";
                            const fallback = e.currentTarget.nextElementSibling;
                            if (fallback) fallback.style.display = "inline";
                          }}
                        />
                      ) : null}
                      <i
                        className="fas fa-folder"
                        style={{
                          color: "#f57c00",
                          fontSize: "0.9rem",
                          display: category.imageUrl ? "none" : "inline",
                        }}
                      ></i>
                    </div>
                    <div className="d-flex flex-column align-items-end gap-1">
                      <span
                        className={`badge rounded-pill ${
                          isSub ? "text-bg-secondary" : "text-bg-primary"
                        }`}
                        style={{ fontSize: "0.6rem", padding: "0.25em 0.55em" }}
                      >
                        {isSub ? "Alt kategori" : "Ana kategori"}
                      </span>
                      <span
                        className={`badge rounded-pill ${
                          category.isActive ? "bg-success" : "bg-secondary"
                        }`}
                        style={{ fontSize: "0.55rem", padding: "0.2em 0.5em" }}
                      >
                        {category.isActive ? "Aktif" : "Pasif"}
                      </span>
                    </div>
                  </div>

                  <h6
                    className="card-title fw-bold mb-1 text-truncate"
                    style={{ color: "#2d3748", fontSize: "0.85rem" }}
                    title={category.name}
                  >
                    {category.name}
                  </h6>

                  {isSub && (
                    <div className="mb-1">
                      <span
                        className="badge bg-light text-dark border"
                        style={{ fontSize: "0.6rem" }}
                      >
                        Üstü: {parentName || "—"}
                      </span>
                    </div>
                  )}

                  {!isSub && childCount > 0 && (
                    <div className="mb-1">
                      <span
                        className="badge bg-info text-white"
                        style={{ fontSize: "0.6rem" }}
                      >
                        {childCount} alt kategorisi var
                      </span>
                    </div>
                  )}

                  {productCount > 0 && (
                    <div className="mb-1">
                      <span
                        className="badge bg-warning text-dark"
                        style={{ fontSize: "0.6rem" }}
                        title="Bu kategoriye bağlı ürün sayısı"
                      >
                        {productCount} ürün
                      </span>
                    </div>
                  )}

                  <p
                    className="text-muted mb-2 text-truncate"
                    style={{ fontSize: "0.7rem" }}
                  >
                    {category.description || "Açıklama yok"}
                  </p>

                  <div className="d-flex flex-wrap gap-1">
                    {!isSub && (
                      <button
                        type="button"
                        className="btn btn-sm btn-success px-2 py-1"
                        onClick={() => handleAddSubCategory(category)}
                        title="Bu ana kategorinin altına yeni alt kategori ekler"
                        style={{ fontSize: "0.65rem" }}
                      >
                        + Alt ekle
                      </button>
                    )}
                    <button
                      type="button"
                      className="btn btn-sm btn-outline-primary px-2 py-1"
                      onClick={() => handleEdit(category)}
                      title="İsim, görsel, açıklama düzenle"
                      style={{ fontSize: "0.65rem" }}
                    >
                      Düzenle
                    </button>
                    <button
                      type="button"
                      className="btn btn-sm btn-outline-danger px-2 py-1"
                      onClick={() => handleDelete(category.id)}
                      title="Kategoriyi sil"
                      style={{ fontSize: "0.65rem" }}
                    >
                      Sil
                    </button>
                  </div>
                </div>
              </div>
            </div>
            );
          })}

          {categories.length === 0 && (
            <div className="col-12">
              <div className="text-center py-4">
                <i
                  className="fas fa-layer-group fa-3x text-muted mb-2"
                  style={{ opacity: 0.3 }}
                ></i>
                <h6 className="text-muted mb-1">Henüz kategori yok</h6>
                <p className="text-muted small">
                  &quot;Ana kategori&quot; butonuna tıklayın.
                </p>
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
                      <i
                        className="fas fa-image me-1"
                        style={{ color: "#f57c00" }}
                      ></i>
                      Kategori Görseli
                    </label>
                    <small
                      className="d-block text-muted mb-2"
                      style={{ fontSize: "0.65rem" }}
                    >
                      Ana sayfa &quot;Keşfet&quot; alanında görünür (ana ve alt
                      kategoriler). Kare görsel önerilir (örn. 400×400).
                    </small>

                    {(imagePreview || formData.imageUrl) && (
                      <div
                        className="mb-2 position-relative d-inline-block"
                        style={{ width: "96px", height: "96px" }}
                      >
                        <img
                          src={imagePreview || resolvePreviewUrl(formData.imageUrl)}
                          alt="Kategori önizleme"
                          style={{
                            width: "96px",
                            height: "96px",
                            objectFit: "cover",
                            borderRadius: "12px",
                            border: "1px solid #eee",
                          }}
                        />
                        <button
                          type="button"
                          className="btn btn-sm btn-danger position-absolute top-0 end-0"
                          style={{
                            transform: "translate(25%, -25%)",
                            borderRadius: "50%",
                            width: "22px",
                            height: "22px",
                            padding: 0,
                            fontSize: "0.65rem",
                          }}
                          onClick={removeImage}
                          title="Görseli kaldır"
                        >
                          <i className="fas fa-times"></i>
                        </button>
                      </div>
                    )}

                    <input
                      ref={imageInputRef}
                      type="file"
                      accept="image/jpeg,image/png,image/gif,image/webp"
                      className="form-control form-control-sm border-0 py-2 mb-2"
                      style={{
                        background: "rgba(245, 124, 0, 0.05)",
                        borderRadius: "8px",
                      }}
                      onChange={handleImageSelect}
                    />

                    <input
                      type="url"
                      className="form-control form-control-sm border-0 py-2"
                      style={{
                        background: "rgba(245, 124, 0, 0.05)",
                        borderRadius: "8px",
                      }}
                      value={formData.imageUrl}
                      onChange={(e) => {
                        setFormData({ ...formData, imageUrl: e.target.value });
                        if (!imageFile) {
                          setImagePreview(
                            e.target.value
                              ? resolvePreviewUrl(e.target.value)
                              : null,
                          );
                        }
                      }}
                      placeholder="veya görsel URL'si yapıştırın"
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
                    disabled={uploadingImage}
                  >
                    {uploadingImage ? (
                      <>
                        <span
                          className="spinner-border spinner-border-sm me-1"
                          role="status"
                          aria-hidden="true"
                        ></span>
                        Yükleniyor...
                      </>
                    ) : editingCategory ? (
                      "Güncelle"
                    ) : (
                      "Kaydet"
                    )}
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
