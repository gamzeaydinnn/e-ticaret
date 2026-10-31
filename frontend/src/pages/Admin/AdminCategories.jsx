import React, { useState, useEffect, useRef, useMemo } from "react";
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
  const [filterType, setFilterType] = useState("all"); // 'all' | 'main' | 'sub' | 'active' | 'inactive'
  const [searchTerm, setSearchTerm] = useState("");
  const [loading, setLoading] = useState(true);
  const [syncingMikro, setSyncingMikro] = useState(false);
  const [togglingId, setTogglingId] = useState(null);
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
      setCategories(categoriesData || []);
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

  const handleToggleActive = async (category) => {
    if (!category?.id) return;
    try {
      setTogglingId(category.id);
      await categoryService.toggleActive(category);
      await fetchCategories();
    } catch (err) {
      console.error("Aktif/Pasif değiştirme hatası:", err);
      alert(
        err.response?.data?.message ||
          err.message ||
          "Kategori durumu güncellenemedi.",
      );
    } finally {
      setTogglingId(null);
    }
  };

  const handleSyncMikro = async () => {
    try {
      setSyncingMikro(true);
      await categoryService.syncFromMikro();
      await fetchCategories();
      alert(
        "Mikro ERP ana ve alt grup tanımları başarıyla senkronize edildi. Mevcut Aktif/Pasif tercihleriniz korundu.",
      );
    } catch (err) {
      console.error("Mikro senkronizasyon hatası:", err);
      alert(
        err.response?.data?.message ||
          err.message ||
          "Mikro senkronizasyonu başarısız oldu.",
      );
    } finally {
      setSyncingMikro(false);
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
        const uploadedUrl =
          await categoryService.uploadCategoryImage(imageFile);
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
    setImagePreview(
      category.imageUrl ? resolvePreviewUrl(category.imageUrl) : null,
    );
    setShowModal(true);
  };

  const handleDelete = async (id) => {
    const cat = categories.find((c) => c.id === id);

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

    const productCount = cat?.productCount ?? 0;
    if (productCount > 0) {
      alert(
        `❌ "${cat?.name}" kategorisi silinemez!\n\n` +
          `Bu kategoriye bağlı ${productCount} adet ürün var.\n\n` +
          `Silmek yerine "Pasife Al" butonuyla sitede gizleyebilirsiniz.`,
      );
      return;
    }

    if (
      window.confirm(
        `"${cat?.name}" kategorisini silmek istediğinizden emin misiniz?`,
      )
    ) {
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

  const handleAddSubCategory = (parentCategory) => {
    setEditingCategory(null);
    setFormData({
      name: "",
      slug: "",
      description: "",
      parentId: parentCategory.id,
      imageUrl: "",
      sortOrder: 0,
      isActive: true,
    });
    resetImageState();
    setShowModal(true);
  };

  const filteredCategories = useMemo(() => {
    const q = searchTerm.trim().toLowerCase();
    return categories.filter((cat) => {
      const isSub = Boolean(cat.parentId);
      if (filterType === "main" && isSub) return false;
      if (filterType === "sub" && !isSub) return false;
      if (filterType === "active" && !cat.isActive) return false;
      if (filterType === "inactive" && cat.isActive) return false;

      if (!q) return true;
      const nameMatch = String(cat.name || "").toLowerCase().includes(q);
      const anaKodMatch = String(cat.mikroAnaGrupKod || "")
        .toLowerCase()
        .includes(q);
      const altKodMatch = String(cat.mikroAltGrupKod || "")
        .toLowerCase()
        .includes(q);
      return nameMatch || anaKodMatch || altKodMatch;
    });
  }, [categories, filterType, searchTerm]);

  const stats = useMemo(() => {
    const mainCount = categories.filter((c) => !c.parentId).length;
    const subCount = categories.filter((c) => Boolean(c.parentId)).length;
    const activeCount = categories.filter((c) => c.isActive).length;
    const inactiveCount = categories.filter((c) => !c.isActive).length;
    return { mainCount, subCount, activeCount, inactiveCount };
  }, [categories]);

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

  const selectedParentForModal = formData.parentId
    ? categories.find((c) => c.id === formData.parentId)
    : null;

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
            Kategori Yönetimi (Mikro ERP Entegreli)
          </h5>
          <p
            className="text-muted mb-0 d-none d-sm-block"
            style={{ fontSize: "0.75rem" }}
          >
            Ana kategori ({stats.mainCount}) ve alt kategorileri (
            {stats.subCount}) sitede Aktif/Pasif yapabilir veya Mikro ERP&apos;ye
            yeni kategori ekleyebilirsiniz
          </p>
        </div>

        <div className="d-flex flex-wrap gap-2 align-items-center">
          {/* View Mode Toggle */}
          <div className="btn-group" role="group" aria-label="Görünüm">
            <button
              type="button"
              className={`btn btn-sm ${
                viewMode === "grid" ? "btn-primary" : "btn-outline-secondary"
              }`}
              onClick={() => setViewMode("grid")}
              title="Kart görünümü"
              style={{ fontSize: "0.75rem" }}
            >
              <i className="fas fa-th me-1"></i>
              Kart
            </button>
            <button
              type="button"
              className={`btn btn-sm ${
                viewMode === "tree" ? "btn-primary" : "btn-outline-secondary"
              }`}
              onClick={() => setViewMode("tree")}
              title="Ağaç görünümü (ana → alt)"
              style={{ fontSize: "0.75rem" }}
            >
              <i className="fas fa-sitemap me-1"></i>
              Ağaç
            </button>
          </div>

          <button
            type="button"
            className="btn btn-sm btn-outline-dark px-2 py-1"
            style={{ fontSize: "0.75rem", borderRadius: "6px" }}
            onClick={handleSyncMikro}
            disabled={syncingMikro}
            title="Mikro ERP'deki grup tanımlarını yerel veritabanı ile eşitle"
          >
            <i
              className={`fas fa-sync-alt me-1 ${
                syncingMikro ? "fa-spin" : ""
              }`}
            ></i>
            {syncingMikro ? "Senkronize..." : "Mikro'dan Eşitle"}
          </button>

          <button
            type="button"
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

      {/* Filter & Search Bar */}
      <div className="d-flex flex-wrap align-items-center justify-content-between gap-2 mb-3 p-2 rounded-2 bg-white shadow-sm border">
        <div className="d-flex flex-wrap gap-1">
          {[
            { id: "all", label: `Tümü (${categories.length})` },
            { id: "main", label: `Ana (${stats.mainCount})` },
            { id: "sub", label: `Alt (${stats.subCount})` },
            { id: "active", label: `Aktif (${stats.activeCount})` },
            { id: "inactive", label: `Pasif (${stats.inactiveCount})` },
          ].map((tab) => (
            <button
              key={tab.id}
              type="button"
              className={`btn btn-sm ${
                filterType === tab.id ? "btn-warning text-white" : "btn-light"
              }`}
              style={{ fontSize: "0.72rem", fontWeight: 600 }}
              onClick={() => setFilterType(tab.id)}
            >
              {tab.label}
            </button>
          ))}
        </div>

        <div style={{ minWidth: "220px", flex: "0 1 280px" }}>
          <input
            type="text"
            className="form-control form-control-sm"
            placeholder="Kategori adı veya Mikro kodu ara (örn: 1400)..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            style={{ fontSize: "0.75rem" }}
          />
        </div>
      </div>

      <div
        className="mb-3 px-2 py-2 rounded-2"
        style={{
          background: "#f8fafc",
          fontSize: "0.72rem",
          color: "#64748b",
        }}
      >
        <strong style={{ color: "#334155" }}>Bilgi:</strong>{" "}
        <strong>Aktif Yap / Pasife Al</strong> butonu ile kategorinin sitede
        görünüp görünmeyeceğini tek tıkla belirleyebilirsiniz. Yeni bir{" "}
        <strong>Ana Kategori</strong> veya <strong>+ Alt ekle</strong> ile alt
        kategori oluşturduğunuzda otomatik olarak Mikro ERP (
        <code>STOK_ANA_GRUPLARI</code> / <code>STOK_ALT_GRUPLARI</code>)
        tablolarına da kaydedilir.
      </div>

      {/* Categories Grid or Tree */}
      {viewMode === "grid" ? (
        <div className="row g-2 g-md-3">
          {filteredCategories.map((category) => {
            const isSub = Boolean(category.parentId);
            const parentCat = isSub
              ? categories.find((c) => c.id === category.parentId)
              : null;
            const parentName = parentCat?.name || null;
            const childCount = categories.filter(
              (c) => c.parentId === category.id,
            ).length;
            const productCount = category.productCount ?? 0;
            const mikroAna =
              category.mikroAnaGrupKod || parentCat?.mikroAnaGrupKod || "";
            const mikroAlt = category.mikroAltGrupKod || "";

            return (
              <div key={category.id} className="col-6 col-md-6 col-xl-4">
                <div
                  className="card h-100 border-0 shadow-sm"
                  style={{
                    borderRadius: "10px",
                    borderLeft: isSub
                      ? "3px solid #94a3b8"
                      : "3px solid #f97316",
                    opacity: category.isActive ? 1 : 0.72,
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
                              const fallback =
                                e.currentTarget.nextElementSibling;
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
                          style={{
                            fontSize: "0.6rem",
                            padding: "0.25em 0.55em",
                          }}
                        >
                          {isSub ? "Alt kategori" : "Ana kategori"}
                        </span>
                        <span
                          className={`badge rounded-pill ${
                            category.isActive ? "bg-success" : "bg-secondary"
                          }`}
                          style={{
                            fontSize: "0.55rem",
                            padding: "0.2em 0.5em",
                          }}
                        >
                          {category.isActive
                            ? "Sitede Aktif"
                            : "Sitede Gizli (Pasif)"}
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

                    <div className="d-flex flex-wrap gap-1 mb-1">
                      {(mikroAna || mikroAlt) && (
                        <span
                          className="badge bg-dark text-white"
                          style={{ fontSize: "0.6rem" }}
                          title="Mikro ERP Grup Kodu"
                        >
                          Mikro:{" "}
                          {mikroAlt ? `${mikroAna} / ${mikroAlt}` : mikroAna}
                        </span>
                      )}

                      {isSub && (
                        <span
                          className="badge bg-light text-dark border"
                          style={{ fontSize: "0.6rem" }}
                        >
                          Üstü: {parentName || "—"}
                        </span>
                      )}

                      {!isSub && childCount > 0 && (
                        <span
                          className="badge bg-info text-white"
                          style={{ fontSize: "0.6rem" }}
                        >
                          {childCount} alt kategori
                        </span>
                      )}

                      <span
                        className={`badge ${
                          productCount > 0
                            ? "bg-warning text-dark"
                            : "bg-light text-muted border"
                        }`}
                        style={{ fontSize: "0.6rem" }}
                        title="Bu kategoriye bağlı ürün sayısı (yalnızca admin panelinde görünür)"
                      >
                        {productCount} ürün
                      </span>
                    </div>

                    <p
                      className="text-muted mb-2 text-truncate"
                      style={{ fontSize: "0.7rem" }}
                    >
                      {category.description || "Açıklama yok"}
                    </p>

                    <div className="d-flex flex-wrap gap-1">
                      <button
                        type="button"
                        className={`btn btn-sm ${
                          category.isActive
                            ? "btn-outline-warning"
                            : "btn-outline-success"
                        } px-2 py-1`}
                        onClick={() => handleToggleActive(category)}
                        disabled={togglingId === category.id}
                        title={
                          category.isActive
                            ? "Bu kategoriyi sitede gizle (Pasife al)"
                            : "Bu kategoriyi sitede göster (Aktife al)"
                        }
                        style={{ fontSize: "0.65rem" }}
                      >
                        <i
                          className={`fas ${
                            category.isActive ? "fa-eye-slash" : "fa-eye"
                          } me-1`}
                        ></i>
                        {category.isActive ? "Pasife Al" : "Aktif Yap"}
                      </button>

                      {!isSub && (
                        <button
                          type="button"
                          className="btn btn-sm btn-success px-2 py-1"
                          onClick={() => handleAddSubCategory(category)}
                          title="Bu ana kategorinin altına yeni alt kategori ekler (Mikro'ya da kaydedilir)"
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

          {filteredCategories.length === 0 && (
            <div className="col-12">
              <div className="text-center py-4">
                <i
                  className="fas fa-layer-group fa-3x text-muted mb-2"
                  style={{ opacity: 0.3 }}
                ></i>
                <h6 className="text-muted mb-1">Eşleşen kategori bulunamadı</h6>
              </div>
            </div>
          )}
        </div>
      ) : (
        <CategoryTree
          categories={categoryTree}
          onEdit={handleEdit}
          onDelete={handleDelete}
          onToggleActive={handleToggleActive}
          onAddSubCategory={handleAddSubCategory}
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
                    ? "Kategori Düzenle"
                    : formData.parentId
                      ? `Alt Kategori Ekle → ${selectedParentForModal?.name || ""}`
                      : "Yeni Ana Kategori Ekle"}
                </h6>
                <button
                  type="button"
                  className="btn-close btn-close-sm"
                  onClick={() => setShowModal(false)}
                ></button>
              </div>

              <form onSubmit={handleSubmit}>
                <div className="modal-body p-3">
                  {!editingCategory && (
                    <div
                      className="alert alert-warning py-2 px-3 mb-3 border-0"
                      style={{
                        fontSize: "0.73rem",
                        background: "rgba(249, 115, 22, 0.1)",
                        color: "#9a3412",
                        borderRadius: "8px",
                      }}
                    >
                      <i className="fas fa-database me-1"></i>
                      <strong>Mikro ERP Entegrasyonu:</strong> Kaydettiğinizde
                      bu{" "}
                      {formData.parentId
                        ? `alt kategori (${
                            selectedParentForModal?.mikroAnaGrupKod
                              ? `Ana Grup: ${selectedParentForModal.mikroAnaGrupKod}`
                              : "Üst Kategori"
                          } altında)`
                        : "ana kategori"}{" "}
                      otomatik sıradaki grup koduyla{" "}
                      <strong>
                        Mikro ERP (
                        {formData.parentId
                          ? "STOK_ALT_GRUPLARI"
                          : "STOK_ANA_GRUPLARI"}
                        )
                      </strong>{" "}
                      tablosuna da eklenecektir.
                    </div>
                  )}

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
                      placeholder="Örn: Glutensiz Ürünler, Yöresel Peynirler"
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
                      placeholder="ornegin: glutensiz-urunler"
                      required
                    />
                    <small
                      className="text-muted"
                      style={{ fontSize: "0.65rem" }}
                    >
                      URL&apos;de görünecek isim (otomatik oluşturulur)
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
                            ? parseInt(e.target.value, 10)
                            : null,
                        })
                      }
                    >
                      <option value="">Ana Kategori (Üst Yok)</option>
                      {categories
                        .filter(
                          (c) => !c.parentId && c.id !== editingCategory?.id,
                        )
                        .map((c) => (
                          <option key={c.id} value={c.id}>
                            {c.name}
                            {c.mikroAnaGrupKod
                              ? ` (Mikro: ${c.mikroAnaGrupKod})`
                              : ""}
                          </option>
                        ))}
                    </select>
                    <small
                      className="text-muted"
                      style={{ fontSize: "0.65rem" }}
                    >
                      Boş bırakırsanız yeni ana kategori, seçerseniz o ana
                      kategorinin alt kategorisi olur
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
                      Ana sayfa &quot;Gölköy Gurme&apos;yi Keşfet&quot; alanında
                      görünür. Kare görsel önerilir (örn. 400×400).
                    </small>

                    {(imagePreview || formData.imageUrl) && (
                      <div
                        className="mb-2 position-relative d-inline-block"
                        style={{ width: "96px", height: "96px" }}
                      >
                        <img
                          src={
                            imagePreview || resolvePreviewUrl(formData.imageUrl)
                          }
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
                          sortOrder: parseInt(e.target.value, 10) || 0,
                        })
                      }
                      placeholder="0"
                    />
                  </div>

                  <div className="form-check">
                    <input
                      className="form-check-input"
                      type="checkbox"
                      checked={formData.isActive}
                      onChange={(e) =>
                        setFormData({
                          ...formData,
                          isActive: e.target.checked,
                        })
                      }
                    />
                    <label
                      className="form-check-label fw-semibold"
                      style={{ fontSize: "0.8rem" }}
                    >
                      Sitede Aktif Göster
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
                      "Kaydet ve Mikro'ya Gönder"
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
