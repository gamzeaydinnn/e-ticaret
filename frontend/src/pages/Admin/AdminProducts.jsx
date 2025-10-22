import React, { useState, useEffect } from "react";
import AdminLayout from "../../components/AdminLayout";
import { AdminService } from "../../services/adminService";
import variantStore from "../../utils/variantStore";

const AdminProducts = () => {
  const [products, setProducts] = useState([]);
  const [categories, setCategories] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [showModal, setShowModal] = useState(false);
  const [editingProduct, setEditingProduct] = useState(null);
  const [formData, setFormData] = useState({
    name: "",
    categoryId: "",
    price: "",
    stock: "",
    description: "",
    imageUrl: "",
    isActive: true,
  });
  const [editingProductId, setEditingProductId] = useState(null);
  const [productVariants, setProductVariants] = useState([]);

  useEffect(() => {
    fetchProducts();
    fetchCategories();
  }, []);

  const fetchProducts = async () => {
    try {
      setLoading(true);
      const productsData = await AdminService.getProducts();
      setProducts(productsData);
    } catch (err) {
      setError("Ürünler yüklenirken hata oluştu");
      console.error("Ürünler yükleme hatası:", err);
    } finally {
      setLoading(false);
    }
  };

  const fetchCategories = async () => {
    try {
      const categoriesData = await AdminService.getCategories();
      setCategories(categoriesData);
    } catch (err) {
      console.error("Kategoriler yükleme hatası:", err);
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    try {
      const productData = {
        ...formData,
        price: parseFloat(formData.price),
        stock: parseInt(formData.stock),
      };

      if (editingProduct) {
        await AdminService.updateProduct(editingProduct.id, productData);
      } else {
        // create and then migrate any temporary variants
        const created = await AdminService.createProduct(productData);
        // created may be the created product object or an id depending on API
        const newId = created && created.id ? created.id : created;
        // if we had a temp editing id, move local variants into new product id
        if (editingProductId && String(editingProductId).startsWith("temp-")) {
          try {
            variantStore.moveVariants(editingProductId, newId);
          } catch (moveErr) {
            console.warn("Variant migration failed:", moveErr);
          }
        }
      }
      setShowModal(false);
      setFormData({
        name: "",
        categoryId: "",
        price: "",
        stock: "",
        description: "",
        imageUrl: "",
        isActive: true,
      });
      setEditingProduct(null);
      fetchProducts();
    } catch (err) {
      console.error("Ürün kaydetme hatası:", err);
    }
  };

  const handleEdit = (product) => {
    setEditingProduct(product);
    setEditingProductId(product.id);
    setProductVariants(variantStore.getVariantsForProduct(product.id) || []);
    setFormData({
      name: product.name,
      categoryId: product.categoryId || "",
      price: product.price.toString(),
      stock: product.stock.toString(),
      description: product.description || "",
      imageUrl: product.imageUrl || "",
      isActive: product.isActive,
    });
    setShowModal(true);
  };

  const handleAddVariant = (variant) => {
    if (!editingProductId) return;
    const added = variantStore.addVariant(editingProductId, variant);
    setProductVariants(variantStore.getVariantsForProduct(editingProductId));
    return added;
  };

  const handleRemoveVariant = (variantId) => {
    if (!editingProductId) return;
    variantStore.removeVariant(editingProductId, variantId);
    setProductVariants(variantStore.getVariantsForProduct(editingProductId));
  };

  const handleDelete = async (id) => {
    if (window.confirm("Bu ürünü silmek istediğinizden emin misiniz?")) {
      try {
        await AdminService.deleteProduct(id);
        fetchProducts();
      } catch (err) {
        console.error("Ürün silme hatası:", err);
      }
    }
  };

  if (loading) {
    return (
      <AdminLayout>
        <div
          className="d-flex justify-content-center align-items-center"
          style={{ height: "60vh" }}
        >
          <div className="text-center">
            <div
              className="spinner-border mb-3"
              style={{ color: "#f57c00" }}
              role="status"
            ></div>
            <p className="text-muted">Ürünler yükleniyor...</p>
          </div>
        </div>
      </AdminLayout>
    );
  }

  if (error) {
    return (
      <AdminLayout>
        <div className="alert alert-danger border-0 rounded-4" role="alert">
          <i className="fas fa-exclamation-triangle me-2"></i>
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
              <i className="fas fa-box me-3" style={{ color: "#f57c00" }}></i>
              Ürün Yönetimi
            </h1>
            <p className="text-muted mb-0">
              Mağaza ürünlerini düzenleyin ve yönetin
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
              setEditingProduct(null);
              setFormData({
                name: "",
                categoryId: "",
                price: "",
                stock: "",
                description: "",
                imageUrl: "",
                isActive: true,
              });
              // create temporary editing id so admin can add local variants before saving
              const tempId = `temp-${Date.now()}`;
              setEditingProductId(tempId);
              setProductVariants(
                variantStore.getVariantsForProduct(tempId) || []
              );
              setShowModal(true);
            }}
          >
            <i className="fas fa-plus me-2"></i>
            Yeni Ürün
          </button>
        </div>

        {/* Products Grid */}
        <div className="row g-3">
          {products.map((product) => (
            <div key={product.id} className="col-xl-3 col-lg-4 col-md-6">
              <div
                className="admin-product-card h-100"
                style={{
                  borderRadius: "12px",
                  transition: "all 0.3s ease",
                  border: "1px solid #e2e8f0",
                  backgroundColor: "#ffffff",
                  boxShadow: "0 2px 10px rgba(0,0,0,0.08)",
                  overflow: "hidden",
                }}
                onMouseEnter={(e) => {
                  e.target.closest(".admin-product-card").style.transform =
                    "translateY(-5px)";
                  e.target.closest(".admin-product-card").style.boxShadow =
                    "0 8px 25px rgba(245, 124, 0, 0.15)";
                }}
                onMouseLeave={(e) => {
                  e.target.closest(".admin-product-card").style.transform =
                    "translateY(0)";
                  e.target.closest(".admin-product-card").style.boxShadow =
                    "0 2px 10px rgba(0,0,0,0.08)";
                }}
              >
                <div
                  className="position-relative"
                  style={{
                    overflow: "hidden",
                    borderTopLeftRadius: "12px",
                    borderTopRightRadius: "12px",
                    backgroundColor: "#f8f9fa",
                  }}
                >
                  <img
                    src={product.imageUrl || "/images/placeholder.png"}
                    alt={product.name}
                    className="card-img-top"
                    style={{
                      height: "140px",
                      objectFit: "contain",
                      objectPosition: "center",
                      width: "100%",
                      padding: "10px",
                    }}
                    onError={(e) => {
                      e.target.src = "/images/placeholder.png";
                    }}
                  />
                  <div className="position-absolute top-0 end-0 m-2">
                    <span
                      className={`badge rounded-pill ${
                        product.isActive ? "bg-success" : "bg-secondary"
                      }`}
                      style={{ fontSize: "0.7rem", padding: "5px 10px" }}
                    >
                      {product.isActive ? "Aktif" : "Pasif"}
                    </span>
                  </div>
                </div>

                <div className="card-body p-3">
                  <div className="d-flex justify-content-between align-items-start mb-2">
                    <h5
                      className="card-title fw-bold mb-0"
                      style={{ color: "#2d3748", fontSize: "1rem" }}
                    >
                      {product.name}
                    </h5>
                    <small
                      className="badge bg-light text-muted"
                      style={{ fontSize: "0.7rem" }}
                    >
                      #{product.id}
                    </small>
                  </div>

                  <p className="text-muted mb-2" style={{ fontSize: "0.8rem" }}>
                    {product.categoryName || "Kategori Yok"}
                  </p>

                  <div className="d-flex justify-content-between align-items-center mb-3">
                    <div
                      className="fw-bold"
                      style={{ color: "#f57c00", fontSize: "1.1rem" }}
                    >
                      ₺
                      {product.price?.toLocaleString("tr-TR", {
                        minimumFractionDigits: 2,
                      })}
                    </div>
                    <span
                      className={`badge ${
                        product.stock > 10
                          ? "bg-success"
                          : product.stock > 0
                          ? "bg-warning"
                          : "bg-danger"
                      }`}
                      style={{ fontSize: "0.75rem", padding: "4px 8px" }}
                    >
                      Stok: {product.stock || 0}
                    </span>
                  </div>

                  <div className="d-flex gap-2">
                    <button
                      className="btn btn-outline-primary btn-sm flex-fill"
                      style={{ fontSize: "0.8rem", padding: "6px 12px" }}
                      onClick={() => handleEdit(product)}
                    >
                      <i className="fas fa-edit me-1"></i>
                      Düzenle
                    </button>
                    <button
                      className="btn btn-outline-danger btn-sm"
                      style={{ fontSize: "0.8rem", padding: "6px 10px" }}
                      onClick={() => handleDelete(product.id)}
                    >
                      <i className="fas fa-trash"></i>
                    </button>
                  </div>
                </div>
              </div>
            </div>
          ))}

          {products.length === 0 && (
            <div className="col-12">
              <div className="text-center py-5">
                <i
                  className="fas fa-box fa-4x text-muted mb-3"
                  style={{ opacity: 0.3 }}
                ></i>
                <h4 className="text-muted mb-2">Henüz ürün bulunmuyor</h4>
                <p className="text-muted">
                  İlk ürününüzü eklemek için "Yeni Ürün" butonuna tıklayın.
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
            <div className="modal-dialog modal-dialog-centered modal-lg">
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
                      className="fas fa-box me-2"
                      style={{ color: "#f57c00" }}
                    ></i>
                    {editingProduct ? "Ürünü Düzenle" : "Yeni Ürün Ekle"}
                  </h5>
                  <button
                    className="btn-close"
                    onClick={() => setShowModal(false)}
                  ></button>
                </div>

                <form onSubmit={handleSubmit}>
                  <div className="modal-body p-4">
                    <div className="row g-3">
                      <div className="col-md-6">
                        <label className="form-label fw-semibold mb-2">
                          Ürün Adı
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
                          placeholder="Ürün adını girin"
                        />
                      </div>

                      <div className="col-md-6">
                        <label className="form-label fw-semibold mb-2">
                          Kategori
                        </label>
                        <select
                          className="form-control border-0 py-3"
                          style={{
                            background: "rgba(245, 124, 0, 0.05)",
                            borderRadius: "12px",
                          }}
                          value={formData.categoryId}
                          onChange={(e) =>
                            setFormData({
                              ...formData,
                              categoryId: e.target.value,
                            })
                          }
                          required
                        >
                          <option value="">Kategori seçin</option>
                          {categories.map((category) => (
                            <option key={category.id} value={category.id}>
                              {category.name}
                            </option>
                          ))}
                        </select>
                      </div>

                      <div className="col-md-6">
                        <label className="form-label fw-semibold mb-2">
                          Fiyat (₺)
                        </label>
                        <input
                          type="number"
                          step="0.01"
                          className="form-control border-0 py-3"
                          style={{
                            background: "rgba(245, 124, 0, 0.05)",
                            borderRadius: "12px",
                          }}
                          value={formData.price}
                          onChange={(e) =>
                            setFormData({ ...formData, price: e.target.value })
                          }
                          required
                          placeholder="0.00"
                        />
                      </div>

                      <div className="col-md-6">
                        <label className="form-label fw-semibold mb-2">
                          Stok Adedi
                        </label>
                        <input
                          type="number"
                          className="form-control border-0 py-3"
                          style={{
                            background: "rgba(245, 124, 0, 0.05)",
                            borderRadius: "12px",
                          }}
                          value={formData.stock}
                          onChange={(e) =>
                            setFormData({ ...formData, stock: e.target.value })
                          }
                          required
                          placeholder="0"
                        />
                      </div>

                      <div className="col-12">
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
                          placeholder="Ürün açıklaması (opsiyonel)"
                        />
                      </div>

                      <div className="col-12">
                        <label className="form-label fw-semibold mb-2">
                          Ürün Resmi URL
                        </label>
                        <input
                          type="url"
                          className="form-control border-0 py-3"
                          style={{
                            background: "rgba(245, 124, 0, 0.05)",
                            borderRadius: "12px",
                          }}
                          value={formData.imageUrl}
                          onChange={(e) =>
                            setFormData({
                              ...formData,
                              imageUrl: e.target.value,
                            })
                          }
                          placeholder="https://example.com/image.jpg"
                        />
                      </div>

                      {(editingProduct ||
                        (editingProductId &&
                          String(editingProductId).startsWith("temp-"))) && (
                        <div className="col-12 mt-3">
                          <h6 className="mb-2">Varyantlar (geçici - local)</h6>
                          <div className="d-flex gap-2 mb-2">
                            <input
                              id="v-package"
                              className="form-control"
                              placeholder="Paket tipi (ör. 500g)"
                            ></input>
                            <input
                              id="v-qty"
                              className="form-control"
                              placeholder="Miktar"
                              type="number"
                            ></input>
                            <input
                              id="v-unit"
                              className="form-control"
                              placeholder="Unit (kg/adet/lt)"
                            ></input>
                            <button
                              className="btn btn-primary"
                              type="button"
                              onClick={() => {
                                const pkg =
                                  document.getElementById("v-package").value;
                                const qty =
                                  parseFloat(
                                    document.getElementById("v-qty").value
                                  ) || 0;
                                const unit =
                                  document.getElementById("v-unit").value ||
                                  "adet";
                                handleAddVariant({
                                  packageType: pkg,
                                  quantity: qty,
                                  unit,
                                  stock: 0,
                                });
                                document.getElementById("v-package").value = "";
                                document.getElementById("v-qty").value = "";
                                document.getElementById("v-unit").value = "";
                              }}
                            >
                              Varyant Ekle
                            </button>
                          </div>

                          <div>
                            {productVariants.map((v) => (
                              <div
                                key={v.id}
                                className="d-flex align-items-center justify-content-between mb-1"
                              >
                                <div>
                                  {v.packageType || `${v.quantity} ${v.unit}`}
                                  {v.expiresAt ? ` — SKT: ${v.expiresAt}` : ""}
                                </div>
                                <div>
                                  <button
                                    className="btn btn-sm btn-danger"
                                    type="button"
                                    onClick={() => handleRemoveVariant(v.id)}
                                  >
                                    Sil
                                  </button>
                                </div>
                              </div>
                            ))}
                          </div>
                        </div>
                      )}

                      <div className="col-12">
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
                            Aktif ürün
                          </label>
                        </div>
                      </div>
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
                      {editingProduct ? "Güncelle" : "Kaydet"}
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

export default AdminProducts;
