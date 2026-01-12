import React, { useState, useEffect } from "react";
import categoryService from "../../services/categoryService";

const AdminCategories = () => {
  const [categories, setCategories] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [showModal, setShowModal] = useState(false);
  const [editingCategory, setEditingCategory] = useState(null);
  const [formData, setFormData] = useState({
    name: "",
    slug: "",
    description: "",
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
    } catch (err) {
      setError("Kategoriler yüklenirken hata oluştu");
      console.error("Kategoriler yükleme hatası:", err);
    } finally {
      setLoading(false);
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
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
        isActive: true,
      });
      setEditingCategory(null);
      fetchCategories();
    } catch (err) {
      console.error("Kategori kaydetme hatası:", err);
    }
  };

  const handleEdit = (category) => {
    setEditingCategory(category);
    setFormData({
      name: category.name,
      slug: category.slug || "",
      description: category.description || "",
      isActive: category.isActive,
    });
    setShowModal(true);
  };

  const handleDelete = async (id) => {
    if (window.confirm("Bu kategoriyi silmek istediğinizden emin misiniz?")) {
      try {
        await categoryService.delete(id);
        fetchCategories();
      } catch (err) {
        console.error("Kategori silme hatası:", err);
      }
    }
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
              isActive: true,
            });
            setShowModal(true);
          }}
        >
          <i className="fas fa-plus me-1"></i>
          Yeni
        </button>
      </div>

      {/* Categories Grid - 2'li mobil */}
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

                <p
                  className="text-muted mb-2 text-truncate"
                  style={{ fontSize: "0.7rem" }}
                >
                  {category.description || "Açıklama yok"}
                </p>

                <div className="d-flex justify-content-end gap-1">
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
                  {editingCategory ? "Düzenle" : "Yeni Kategori"}
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
                      URL Slug
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
                      placeholder="ornegin: meyve-sebze"
                      required
                    />
                  </div>
                  <div className="mb-3">
                    <label
                      className="form-label fw-semibold mb-1"
                      style={{ fontSize: "0.8rem" }}
                    >
                      Kategori Adı
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
                      placeholder="Kategori adı"
                    />
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
                      placeholder="Açıklama (opsiyonel)"
                    />
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
