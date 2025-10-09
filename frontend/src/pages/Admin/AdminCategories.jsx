import React, { useState, useEffect } from "react";
import AdminLayout from "../../components/AdminLayout";
import { AdminService } from "../../services/adminService";

const AdminCategories = () => {
  const [categories, setCategories] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [showModal, setShowModal] = useState(false);
  const [editingCategory, setEditingCategory] = useState(null);
  const [formData, setFormData] = useState({
    name: "",
    description: "",
    icon: "fa-folder",
    isActive: true,
  });

  useEffect(() => {
    fetchCategories();
  }, []);

  const fetchCategories = async () => {
    try {
      setLoading(true);
      const categoriesData = await AdminService.getCategories();
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
        await AdminService.updateCategory(editingCategory.id, formData);
      } else {
        await AdminService.createCategory(formData);
      }
      setShowModal(false);
      setFormData({
        name: "",
        description: "",
        icon: "fa-folder",
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
      description: category.description || "",
      icon: category.icon || "fa-folder",
      isActive: category.isActive,
    });
    setShowModal(true);
  };

  const handleDelete = async (id) => {
    if (window.confirm("Bu kategoriyi silmek istediğinizden emin misiniz?")) {
      try {
        await AdminService.deleteCategory(id);
        fetchCategories();
      } catch (err) {
        console.error("Kategori silme hatası:", err);
      }
    }
  };

  if (loading) {
    return (
      <AdminLayout>
        <div
          className="d-flex justify-content-center align-items-center"
          style={{ height: "400px" }}
        >
          <div className="spinner-border text-primary" role="status">
            <span className="visually-hidden">Yükleniyor...</span>
          </div>
        </div>
      </AdminLayout>
    );
  }

  if (error) {
    return (
      <AdminLayout>
        <div className="alert alert-danger" role="alert">
          {error}
        </div>
      </AdminLayout>
    );
  }

  return (
    <AdminLayout>
      <div className="container-fluid p-4">
        {/* Header */}
        <div className="d-flex justify-content-between align-items-center mb-5">
          <div>
            <h1 className="h2 mb-1 fw-bold" style={{ color: "#2d3748" }}>
              <i
                className="fas fa-layer-group me-3"
                style={{ color: "#f57c00" }}
              ></i>
              Kategori Yönetimi
            </h1>
            <p className="text-muted mb-0">
              Ürün kategorilerini düzenleyin ve yönetin
            </p>
          </div>
          <button
            className="btn btn-lg border-0 text-white fw-semibold px-4 py-2"
            style={{
              background: "linear-gradient(135deg, #f57c00, #ff9800)",
              borderRadius: "12px",
              boxShadow: "0 4px 15px rgba(245, 124, 0, 0.3)",
            }}
            onClick={() => {
              setEditingCategory(null);
              setFormData({
                name: "",
                description: "",
                icon: "fa-folder",
                isActive: true,
              });
              setShowModal(true);
            }}
          >
            <i className="fas fa-plus me-2"></i>
            Yeni Kategori
          </button>
        </div>

        {/* Categories Grid */}
        <div className="row g-4">
          {categories.map((category) => (
            <div key={category.id} className="col-md-6 col-xl-4">
              <div
                className="card h-100 border-0 shadow-sm"
                style={{ borderRadius: "16px", transition: "all 0.3s ease" }}
                onMouseEnter={(e) =>
                  (e.target.closest(".card").style.transform =
                    "translateY(-5px)")
                }
                onMouseLeave={(e) =>
                  (e.target.closest(".card").style.transform = "translateY(0)")
                }
              >
                <div className="card-body p-4">
                  <div className="d-flex align-items-start justify-content-between mb-3">
                    <div
                      className="d-flex align-items-center justify-content-center rounded-circle"
                      style={{
                        width: "56px",
                        height: "56px",
                        background: "linear-gradient(135deg, #f57c00, #ff9800)",
                        color: "white",
                      }}
                    >
                      <i className={`fas ${category.icon} fa-lg`}></i>
                    </div>
                    <span
                      className={`badge rounded-pill ${
                        category.isActive ? "bg-success" : "bg-secondary"
                      }`}
                    >
                      {category.isActive ? "Aktif" : "Pasif"}
                    </span>
                  </div>

                  <h5
                    className="card-title fw-bold mb-2"
                    style={{ color: "#2d3748" }}
                  >
                    {category.name}
                  </h5>

                  <p className="text-muted mb-3" style={{ fontSize: "0.9rem" }}>
                    {category.description || "Açıklama bulunmuyor"}
                  </p>

                  <div className="d-flex align-items-center justify-content-between">
                    <div className="d-flex align-items-center">
                      <i className="fas fa-box text-muted me-2"></i>
                      <span
                        className="text-muted"
                        style={{ fontSize: "0.9rem" }}
                      >
                        {category.productCount || 0} ürün
                      </span>
                    </div>

                    <div className="btn-group" role="group">
                      <button
                        className="btn btn-sm btn-outline-primary"
                        onClick={() => handleEdit(category)}
                        title="Düzenle"
                      >
                        <i className="fas fa-edit"></i>
                      </button>
                      <button
                        className="btn btn-sm btn-outline-danger"
                        onClick={() => handleDelete(category.id)}
                        title="Sil"
                      >
                        <i className="fas fa-trash"></i>
                      </button>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          ))}

          {categories.length === 0 && (
            <div className="col-12">
              <div className="text-center py-5">
                <i
                  className="fas fa-layer-group fa-4x text-muted mb-3"
                  style={{ opacity: 0.3 }}
                ></i>
                <h4 className="text-muted mb-2">Henüz kategori bulunmuyor</h4>
                <p className="text-muted">
                  İlk kategorinizi eklemek için "Yeni Kategori" butonuna
                  tıklayın.
                </p>
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
            <div className="modal-dialog modal-dialog-centered">
              <div
                className="modal-content border-0"
                style={{ borderRadius: "20px" }}
              >
                <div className="modal-header border-0 p-4">
                  <h5
                    className="modal-title fw-bold"
                    style={{ color: "#2d3748" }}
                  >
                    <i
                      className="fas fa-layer-group me-2"
                      style={{ color: "#f57c00" }}
                    ></i>
                    {editingCategory
                      ? "Kategoriyi Düzenle"
                      : "Yeni Kategori Ekle"}
                  </h5>
                  <button
                    className="btn-close"
                    onClick={() => setShowModal(false)}
                  ></button>
                </div>

                <form onSubmit={handleSubmit}>
                  <div className="modal-body p-4">
                    <div className="mb-4">
                      <label className="form-label fw-semibold mb-2">
                        Kategori Adı
                      </label>
                      <input
                        type="text"
                        className="form-control border-0 py-3"
                        style={{
                          background: "rgba(245, 124, 0, 0.05)",
                          borderRadius: "12px",
                        }}
                        value={formData.name}
                        onChange={(e) =>
                          setFormData({ ...formData, name: e.target.value })
                        }
                        required
                        placeholder="Kategori adını girin"
                      />
                    </div>

                    <div className="mb-4">
                      <label className="form-label fw-semibold mb-2">
                        Açıklama
                      </label>
                      <textarea
                        className="form-control border-0 py-3"
                        rows="3"
                        style={{
                          background: "rgba(245, 124, 0, 0.05)",
                          borderRadius: "12px",
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

                    <div className="mb-4">
                      <label className="form-label fw-semibold mb-2">
                        İkon
                      </label>
                      <select
                        className="form-control border-0 py-3"
                        style={{
                          background: "rgba(245, 124, 0, 0.05)",
                          borderRadius: "12px",
                        }}
                        value={formData.icon}
                        onChange={(e) =>
                          setFormData({ ...formData, icon: e.target.value })
                        }
                      >
                        <option value="fa-folder">📁 Klasör</option>
                        <option value="fa-drumstick-bite">🍗 Et</option>
                        <option value="fa-cheese">🧀 Süt Ürünleri</option>
                        <option value="fa-apple-alt">🍎 Meyve & Sebze</option>
                        <option value="fa-coffee">☕ İçecek</option>
                        <option value="fa-bread-slice">🍞 Ekmek</option>
                        <option value="fa-cookie-bite">🍪 Atıştırmalık</option>
                      </select>
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
                      <label className="form-check-label fw-semibold">
                        Aktif kategori
                      </label>
                    </div>
                  </div>

                  <div className="modal-footer border-0 p-4">
                    <button
                      type="button"
                      className="btn btn-light me-2"
                      onClick={() => setShowModal(false)}
                    >
                      İptal
                    </button>
                    <button
                      type="submit"
                      className="btn text-white fw-semibold px-4"
                      style={{
                        background: "linear-gradient(135deg, #f57c00, #ff9800)",
                        borderRadius: "8px",
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
    </AdminLayout>
  );
};

export default AdminCategories;
