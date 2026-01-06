import React, { useState, useEffect } from "react";
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
    );
  }

  if (error) {
    return (
      <div className="alert alert-danger border-0 rounded-3" role="alert">
        <i className="fas fa-exclamation-triangle me-2"></i>
        {error}
      </div>
    );
  }

  return (
    <div>
      {/* Mobil için CSS */}
      <style>{`
        @media (max-width: 576px) {
          .admin-product-card img { height: 60px !important; }
          .admin-product-card .p-2 { padding: 0.4rem !important; }
          .admin-product-card h6 { font-size: 0.7rem !important; }
          .admin-product-card .badge { font-size: 0.5rem !important; padding: 1px 3px !important; }
          .admin-product-card .btn { font-size: 0.6rem !important; padding: 2px 4px !important; }
          .admin-product-card .fw-bold { font-size: 0.7rem !important; }
        }
      `}</style>
      <div className="container-fluid px-2">
        {/* Header - Mobil Uyumlu */}
        <div className="d-flex justify-content-between align-items-center mb-3">
          <div>
            <h5
              className="mb-0 fw-bold"
              style={{ color: "#1e293b", fontSize: "1rem" }}
            >
              <i className="fas fa-box me-1" style={{ color: "#f97316" }}></i>
              Ürünler
            </h5>
          </div>
          <button
            className="btn border-0 text-white fw-medium px-2 py-1"
            style={{
              background: "linear-gradient(135deg, #f97316, #fb923c)",
              borderRadius: "6px",
              fontSize: "0.75rem",
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
              const tempId = `temp-${Date.now()}`;
              setEditingProductId(tempId);
              setProductVariants(
                variantStore.getVariantsForProduct(tempId) || []
              );
              setShowModal(true);
            }}
          >
            <i className="fas fa-plus me-1"></i>Ekle
          </button>
        </div>

        {/* Products Grid - Mobil 2'li */}
        <div className="row g-2">
          {products.map((product) => (
            <div key={product.id} className="col-6 col-md-4 col-lg-3">
              <div
                className="admin-product-card h-100"
                style={{
                  borderRadius: "8px",
                  border: "1px solid #e2e8f0",
                  backgroundColor: "#fff",
                  overflow: "hidden",
                }}
              >
                <div
                  className="position-relative"
                  style={{ backgroundColor: "#f8f9fa" }}
                >
                  <img
                    src={product.imageUrl || "/images/placeholder.png"}
                    alt={product.name}
                    style={{
                      height: "80px",
                      objectFit: "contain",
                      width: "100%",
                      padding: "5px",
                    }}
                    onError={(e) => {
                      e.target.src = "/images/placeholder.png";
                    }}
                  />
                  <span
                    className={`badge position-absolute top-0 end-0 m-1 ${
                      product.isActive ? "bg-success" : "bg-secondary"
                    }`}
                    style={{ fontSize: "0.55rem", padding: "2px 5px" }}
                  >
                    {product.isActive ? "Aktif" : "Pasif"}
                  </span>
                </div>

                <div className="p-2">
                  <h6
                    className="fw-bold mb-1 text-truncate"
                    style={{ fontSize: "0.75rem", color: "#2d3748" }}
                  >
                    {product.name}
                  </h6>
                  <p
                    className="text-muted mb-1 text-truncate"
                    style={{ fontSize: "0.65rem" }}
                  >
                    {product.categoryName || "-"}
                  </p>
                  <div className="d-flex justify-content-between align-items-center mb-2">
                    <span
                      className="fw-bold"
                      style={{ color: "#f57c00", fontSize: "0.8rem" }}
                    >
                      ₺{product.price?.toFixed(2)}
                    </span>
                    <span
                      className={`badge ${
                        product.stock > 10
                          ? "bg-success"
                          : product.stock > 0
                          ? "bg-warning"
                          : "bg-danger"
                      }`}
                      style={{ fontSize: "0.55rem", padding: "2px 4px" }}
                    >
                      {product.stock}
                    </span>
                  </div>
                  <div className="d-flex gap-1">
                    <button
                      className="btn btn-outline-primary btn-sm flex-fill"
                      style={{ fontSize: "0.65rem", padding: "3px 6px" }}
                      onClick={() => handleEdit(product)}
                    >
                      <i className="fas fa-edit"></i>
                    </button>
                    <button
                      className="btn btn-outline-danger btn-sm"
                      style={{ fontSize: "0.65rem", padding: "3px 6px" }}
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
                          Ürün Resmi
                        </label>
                        <input
                          type="text"
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
                          placeholder="/images/urun.jpg veya https://..."
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
    </div>
  );
};

export default AdminProducts;
