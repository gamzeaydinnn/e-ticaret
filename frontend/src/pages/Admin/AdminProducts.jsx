import React, { useState, useEffect, useRef } from "react";
import { ProductService } from "../../services/productService";
import categoryService from "../../services/categoryService";
import variantStore from "../../utils/variantStore";
import XmlImportModal from "../../components/admin/XmlImportModal";
import VariantManager from "../../components/admin/VariantManager";

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

  // Excel Import State'leri
  const [showExcelModal, setShowExcelModal] = useState(false);
  const [excelFile, setExcelFile] = useState(null);
  const [excelLoading, setExcelLoading] = useState(false);
  const [excelResult, setExcelResult] = useState(null);
  const [excelError, setExcelError] = useState(null);

  // XML Import State'leri
  const [showXmlImportModal, setShowXmlImportModal] = useState(false);

  // Variant Manager State'leri
  const [showVariantManager, setShowVariantManager] = useState(false);
  const [selectedProductForVariants, setSelectedProductForVariants] =
    useState(null);

  // Excel Export State'leri
  const [exportLoading, setExportLoading] = useState(false);

  // Resim Upload State'leri
  const [imageFile, setImageFile] = useState(null);
  const [imageUploading, setImageUploading] = useState(false);
  const [imagePreview, setImagePreview] = useState(null);
  const [isDragging, setIsDragging] = useState(false);
  const imageInputRef = useRef(null);
  const dropZoneRef = useRef(null);

  useEffect(() => {
    fetchProducts();
    fetchCategories();

    // ProductService subscription - CRUD değişikliklerinde otomatik refetch
    // Bu sayede başka bir yerde yapılan değişiklikler de yansır
    const unsubscribe = ProductService.subscribe((event) => {
      console.log("📦 Ürün değişikliği algılandı:", event.action);
      // Kendi CRUD işlemlerimizden sonra zaten fetchProducts çağrılıyor
      // Bu subscription daha çok multi-tab senkronizasyonu için
      if (event.action === "import") {
        // Excel import sonrası tam yenileme
        fetchProducts();
      }
    });

    // Cleanup on unmount
    return () => {
      unsubscribe();
    };
  }, []);

  const fetchProducts = async () => {
    try {
      setLoading(true);
      const productsData = await ProductService.getAll();
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
      const categoriesData = await categoryService.getAll();
      setCategories(categoriesData);
    } catch (err) {
      console.error("Kategoriler yükleme hatası:", err);
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();

    // ============================================================
    // FORM VALİDASYONU - Kapsamlı kontroller
    // ============================================================

    // Ürün adı kontrolü
    if (!formData.name || formData.name.trim().length < 2) {
      alert("❌ Ürün adı en az 2 karakter olmalıdır.");
      return;
    }

    // Fiyat kontrolü
    const price = parseFloat(formData.price);
    if (isNaN(price) || price <= 0) {
      alert("❌ Geçerli bir fiyat girin (0'dan büyük olmalı).");
      return;
    }

    // Stok kontrolü
    const stock = parseInt(formData.stock);
    if (isNaN(stock) || stock < 0) {
      alert("❌ Geçerli bir stok adedi girin (0 veya daha büyük olmalı).");
      return;
    }

    // Kategori kontrolü
    if (!formData.categoryId) {
      alert("❌ Lütfen bir kategori seçin.");
      return;
    }

    try {
      // Ürün verilerini API formatına dönüştür
      const productData = {
        name: formData.name.trim(),
        description: formData.description?.trim() || "",
        price: price,
        stockQuantity: stock, // API stockQuantity bekliyor
        categoryId: parseInt(formData.categoryId),
        imageUrl: formData.imageUrl?.trim() || null,
        isActive: formData.isActive !== false,
      };

      if (editingProduct) {
        // Mevcut ürünü güncelle - ProductService.updateAdmin kullan
        await ProductService.updateAdmin(editingProduct.id, productData);
        alert("✅ Ürün başarıyla güncellendi!");
      } else {
        // Yeni ürün oluştur - ProductService.createAdmin kullan
        const created = await ProductService.createAdmin(productData);
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
        alert("✅ Yeni ürün başarıyla eklendi!");
      }

      // Modal'ı kapat ve formu sıfırla
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
      setEditingProductId(null);
      setImageFile(null);
      setImagePreview(null);

      // Listeyi yenile
      fetchProducts();
    } catch (err) {
      console.error("Ürün kaydetme hatası:", err);
      alert(
        "❌ Ürün kaydedilirken hata oluştu: " +
          (err.response?.data?.message || err.message),
      );
    }
  };

  const handleEdit = (product) => {
    setEditingProduct(product);
    setEditingProductId(product.id);
    setProductVariants(variantStore.getVariantsForProduct(product.id) || []);
    // Mevcut ürünün resmini önizleme olarak göster
    setImagePreview(product.imageUrl || null);
    setImageFile(null);
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

  /**
   * Resim dosyasını validate eder ve önizleme oluşturur.
   * File input veya drag & drop'tan gelen dosyalar için çalışır.
   * @param {File} file - Seçilen dosya
   * @returns {boolean} - Dosya geçerli mi
   */
  const validateAndSetImage = (file) => {
    if (!file) return false;

    // Dosya türü kontrolü (frontend'de de güvenlik)
    const allowedTypes = ["image/jpeg", "image/png", "image/gif", "image/webp"];
    if (!allowedTypes.includes(file.type)) {
      alert("Sadece resim dosyaları (jpg, png, gif, webp) yüklenebilir.");
      return false;
    }

    // Dosya boyutu kontrolü (10MB)
    const maxSize = 10 * 1024 * 1024;
    if (file.size > maxSize) {
      alert("Dosya boyutu maksimum 10MB olabilir.");
      return false;
    }

    setImageFile(file);
    // Önizleme için ObjectURL oluştur
    const previewUrl = URL.createObjectURL(file);
    setImagePreview(previewUrl);
    return true;
  };

  /**
   * Resim dosyası seçildiğinde çağrılır.
   * Dosyayı validate eder ve önizleme oluşturur.
   * @param {Event} e - File input change event
   */
  const handleImageSelect = (e) => {
    const file = e.target.files?.[0];
    if (!validateAndSetImage(file)) {
      e.target.value = "";
    }
  };

  /**
   * Drag & Drop Event Handlers
   */
  const handleDragEnter = (e) => {
    e.preventDefault();
    e.stopPropagation();
    setIsDragging(true);
  };

  const handleDragLeave = (e) => {
    e.preventDefault();
    e.stopPropagation();
    // Sadece drop zone'dan çıkıldığında
    if (e.currentTarget.contains(e.relatedTarget)) return;
    setIsDragging(false);
  };

  const handleDragOver = (e) => {
    e.preventDefault();
    e.stopPropagation();
  };

  const handleDrop = (e) => {
    e.preventDefault();
    e.stopPropagation();
    setIsDragging(false);

    const files = e.dataTransfer?.files;
    if (files && files.length > 0) {
      validateAndSetImage(files[0]);
    }
  };

  /**
   * Seçilen resim dosyasını sunucuya yükler.
   * Başarılı olursa imageUrl'i formData'ya set eder.
   */
  const handleImageUpload = async () => {
    if (!imageFile) {
      alert("Lütfen önce bir resim dosyası seçin.");
      return;
    }

    setImageUploading(true);
    try {
      const result = await ProductService.uploadImage(imageFile);
      if (result?.success && result?.imageUrl) {
        // Yükleme başarılı - form'a URL'i ekle
        setFormData((prev) => ({ ...prev, imageUrl: result.imageUrl }));
        setImagePreview(result.imageUrl);
        setImageFile(null);
        // Input'u temizle
        if (imageInputRef.current) {
          imageInputRef.current.value = "";
        }
        alert("✅ Resim başarıyla yüklendi!");
      } else {
        throw new Error(result?.message || "Resim yüklenemedi");
      }
    } catch (err) {
      console.error("Resim yükleme hatası:", err);
      alert(
        "Resim yüklenirken hata oluştu: " +
          (err.response?.data?.message || err.message),
      );
    } finally {
      setImageUploading(false);
    }
  };

  /**
   * Resim seçimini iptal eder ve önizlemeyi temizler.
   */
  const handleClearImage = () => {
    setImageFile(null);
    // Mevcut ürün düzenleniyorsa eski resmi göster
    setImagePreview(editingProduct?.imageUrl || null);
    setFormData((prev) => ({
      ...prev,
      imageUrl: editingProduct?.imageUrl || "",
    }));
    if (imageInputRef.current) {
      imageInputRef.current.value = "";
    }
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
    if (window.confirm("Ürünü silmek istediğinizden emin misiniz?")) {
      try {
        // ProductService.deleteAdmin kullan - veritabanından kalıcı siler
        await ProductService.deleteAdmin(id);
        fetchProducts(); // Listeyi yenile
      } catch (err) {
        console.error("Ürün silme hatası:", err);
        alert(
          "Ürün silinirken hata oluştu: " +
            (err.response?.data?.message || err.message),
        );
      }
    }
  };

  // Excel dosyasından toplu ürün yükleme
  const handleExcelUpload = async (e) => {
    e.preventDefault();
    if (!excelFile) {
      setExcelError("Lütfen bir Excel veya CSV dosyası seçin.");
      return;
    }

    setExcelLoading(true);
    setExcelResult(null);
    setExcelError(null);

    try {
      // ProductService.importExcel kullanarak dosyayı yükle
      const result = await ProductService.importExcel(excelFile);
      setExcelResult(result);
      // Başarılı olursa ürün listesini yenile
      fetchProducts();
    } catch (err) {
      console.error("Excel yükleme hatası:", err);
      setExcelError(
        err.response?.data?.message || "Dosya yüklenirken hata oluştu.",
      );
    } finally {
      setExcelLoading(false);
    }
  };

  // Excel şablonu indir
  const handleDownloadTemplate = async () => {
    try {
      const blob = await ProductService.downloadTemplate();
      const url = window.URL.createObjectURL(new Blob([blob]));
      const link = document.createElement("a");
      link.href = url;
      link.setAttribute("download", "urun_sablonu.xlsx");
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.URL.revokeObjectURL(url);
    } catch (err) {
      console.error("Şablon indirme hatası:", err);
      alert(
        "Şablon indirilemedi: " + (err.response?.data?.message || err.message),
      );
    }
  };

  /**
   * Mevcut ürünleri Excel dosyası olarak dışa aktarır.
   * Dosya adı: urunler_YYYYMMDD_HHMMSS.xlsx formatında oluşturulur.
   * Türkçe karakterler UTF-8 encoding ile korunur.
   */
  const handleExportExcel = async () => {
    setExportLoading(true);
    try {
      const blob = await ProductService.exportExcel();

      // Dosya adı oluştur: urunler_20260116_143025.xlsx
      const now = new Date();
      const dateStr = now.toISOString().slice(0, 10).replace(/-/g, "");
      const timeStr = now.toTimeString().slice(0, 8).replace(/:/g, "");
      const fileName = `urunler_${dateStr}_${timeStr}.xlsx`;

      // Blob'u dosya olarak indir
      const url = window.URL.createObjectURL(new Blob([blob]));
      const link = document.createElement("a");
      link.href = url;
      link.setAttribute("download", fileName);
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.URL.revokeObjectURL(url);

      // Başarı mesajı göster (opsiyonel)
      console.log(`✅ Excel export tamamlandı: ${fileName}`);
    } catch (err) {
      console.error("Excel export hatası:", err);
      alert(
        "Ürünler dışa aktarılamadı: " +
          (err.response?.data?.message || err.message),
      );
    } finally {
      setExportLoading(false);
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
        <div className="d-flex justify-content-between align-items-center mb-3 flex-wrap gap-2">
          <div>
            <h5
              className="mb-0 fw-bold"
              style={{ color: "#1e293b", fontSize: "1rem" }}
            >
              <i className="fas fa-box me-1" style={{ color: "#f97316" }}></i>
              Ürünler
            </h5>
          </div>
          <div className="d-flex gap-2 flex-wrap">
            {/* Excel'den Yükle Butonu */}
            <button
              className="btn border-0 text-white fw-medium px-2 py-1"
              style={{
                background: "linear-gradient(135deg, #10b981, #34d399)",
                borderRadius: "6px",
                fontSize: "0.75rem",
              }}
              onClick={() => {
                setExcelFile(null);
                setExcelResult(null);
                setExcelError(null);
                setShowExcelModal(true);
              }}
            >
              <i className="fas fa-file-excel me-1"></i>Excel Yükle
            </button>
            {/* XML Import Butonu */}
            <button
              className="btn border-0 text-white fw-medium px-2 py-1"
              style={{
                background: "linear-gradient(135deg, #8b5cf6, #a78bfa)",
                borderRadius: "6px",
                fontSize: "0.75rem",
              }}
              onClick={() => setShowXmlImportModal(true)}
            >
              <i className="fas fa-code me-1"></i>XML Import
            </button>
            {/* Excel Export Butonu */}
            <button
              className="btn border-0 text-white fw-medium px-2 py-1"
              style={{
                background: "linear-gradient(135deg, #3b82f6, #60a5fa)",
                borderRadius: "6px",
                fontSize: "0.75rem",
              }}
              onClick={handleExportExcel}
              disabled={exportLoading}
            >
              {exportLoading ? (
                <>
                  <span className="spinner-border spinner-border-sm me-1"></span>
                  İndiriliyor...
                </>
              ) : (
                <>
                  <i className="fas fa-file-export me-1"></i>Excel İndir
                </>
              )}
            </button>
            {/* Şablon İndir Butonu */}
            <button
              className="btn border-0 text-white fw-medium px-2 py-1"
              style={{
                background: "linear-gradient(135deg, #6366f1, #818cf8)",
                borderRadius: "6px",
                fontSize: "0.75rem",
              }}
              onClick={handleDownloadTemplate}
            >
              <i className="fas fa-download me-1"></i>Şablon
            </button>
            {/* Yeni Ürün Ekle Butonu */}
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
                // Resim state'lerini sıfırla
                setImageFile(null);
                setImagePreview(null);
                if (imageInputRef.current) {
                  imageInputRef.current.value = "";
                }
                const tempId = `temp-${Date.now()}`;
                setEditingProductId(tempId);
                setProductVariants(
                  variantStore.getVariantsForProduct(tempId) || [],
                );
                setShowModal(true);
              }}
            >
              <i className="fas fa-plus me-1"></i>Ekle
            </button>
          </div>
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
                      title="Düzenle"
                    >
                      <i className="fas fa-edit"></i>
                    </button>
                    <button
                      className="btn btn-outline-success btn-sm"
                      style={{ fontSize: "0.65rem", padding: "3px 6px" }}
                      onClick={() => {
                        setSelectedProductForVariants(product);
                        setShowVariantManager(true);
                      }}
                      title="Varyantlar"
                    >
                      <i className="fas fa-layer-group"></i>
                    </button>
                    <button
                      className="btn btn-outline-danger btn-sm"
                      style={{ fontSize: "0.65rem", padding: "3px 6px" }}
                      onClick={() => handleDelete(product.id)}
                      title="Sil"
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

                        {/* Drag & Drop Alanı */}
                        <div
                          ref={dropZoneRef}
                          onDragEnter={handleDragEnter}
                          onDragLeave={handleDragLeave}
                          onDragOver={handleDragOver}
                          onDrop={handleDrop}
                          onClick={() => imageInputRef.current?.click()}
                          style={{
                            border: isDragging
                              ? "3px dashed #f57c00"
                              : "2px dashed #e2e8f0",
                            borderRadius: "16px",
                            padding: "24px",
                            textAlign: "center",
                            cursor: "pointer",
                            transition: "all 0.3s ease",
                            background: isDragging
                              ? "rgba(245, 124, 0, 0.1)"
                              : "rgba(245, 124, 0, 0.02)",
                            transform: isDragging ? "scale(1.02)" : "scale(1)",
                          }}
                        >
                          {/* Resim Önizleme Alanı */}
                          {imagePreview ? (
                            <div className="mb-3">
                              <img
                                src={imagePreview}
                                alt="Ürün önizleme"
                                style={{
                                  maxWidth: "200px",
                                  maxHeight: "150px",
                                  objectFit: "contain",
                                  borderRadius: "12px",
                                  border: "2px solid #e2e8f0",
                                }}
                                onError={(e) => {
                                  e.target.src = "/images/placeholder.png";
                                }}
                              />
                            </div>
                          ) : (
                            <div className="py-3">
                              <i
                                className="fas fa-cloud-upload-alt"
                                style={{
                                  fontSize: "48px",
                                  color: isDragging ? "#f57c00" : "#94a3b8",
                                  transition: "color 0.3s ease",
                                }}
                              ></i>
                              <p
                                className="mt-3 mb-1 fw-medium"
                                style={{ color: "#64748b" }}
                              >
                                {isDragging
                                  ? "Bırakarak yükle"
                                  : "Resim sürükleyip bırakın veya tıklayın"}
                              </p>
                              <small className="text-muted">
                                JPG, PNG, GIF, WEBP (Maks. 10MB)
                              </small>
                            </div>
                          )}

                          {/* Gizli File Input */}
                          <input
                            ref={imageInputRef}
                            type="file"
                            accept="image/jpeg,image/png,image/gif,image/webp"
                            style={{ display: "none" }}
                            onChange={handleImageSelect}
                          />
                        </div>

                        {/* Yükle & Temizle Butonları */}
                        {(imageFile || imagePreview) && (
                          <div className="d-flex gap-2 mt-3 justify-content-center">
                            {/* Yükle Butonu */}
                            {imageFile && (
                              <button
                                type="button"
                                className="btn text-white"
                                style={{
                                  background:
                                    "linear-gradient(135deg, #10b981, #34d399)",
                                  borderRadius: "8px",
                                  minWidth: "120px",
                                }}
                                onClick={handleImageUpload}
                                disabled={imageUploading}
                              >
                                {imageUploading ? (
                                  <>
                                    <span className="spinner-border spinner-border-sm me-1"></span>
                                    Yükleniyor...
                                  </>
                                ) : (
                                  <>
                                    <i className="fas fa-upload me-1"></i>
                                    Sunucuya Yükle
                                  </>
                                )}
                              </button>
                            )}

                            {/* Temizle Butonu */}
                            <button
                              type="button"
                              className="btn btn-outline-secondary"
                              style={{ borderRadius: "8px" }}
                              onClick={handleClearImage}
                            >
                              <i className="fas fa-times me-1"></i>
                              Temizle
                            </button>
                          </div>
                        )}

                        {/* Mevcut URL gösterimi */}
                        {formData.imageUrl && (
                          <div className="mt-2 text-center">
                            <small className="text-muted">
                              <i className="fas fa-link me-1"></i>
                              Kayıtlı: {formData.imageUrl}
                            </small>
                          </div>
                        )}

                        {/* Manuel URL girişi (opsiyonel) */}
                        <div className="mt-2 text-center">
                          <small
                            className="text-primary"
                            style={{ cursor: "pointer" }}
                            onClick={(e) => {
                              e.stopPropagation();
                              const url = prompt(
                                "Resim URL'si girin (opsiyonel):",
                                formData.imageUrl,
                              );
                              if (url !== null) {
                                setFormData((prev) => ({
                                  ...prev,
                                  imageUrl: url,
                                }));
                                setImagePreview(url || null);
                              }
                            }}
                          >
                            <i className="fas fa-edit me-1"></i>
                            Manuel URL gir
                          </small>
                        </div>
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
                                    document.getElementById("v-qty").value,
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

        {/* Excel Import Modal */}
        {showExcelModal && (
          <div
            className="modal fade show d-block"
            style={{ backgroundColor: "rgba(0,0,0,0.5)" }}
          >
            <div className="modal-dialog modal-dialog-centered">
              <div
                className="modal-content border-0"
                style={{ borderRadius: "16px" }}
              >
                <div
                  className="modal-header border-0 p-4"
                  style={{
                    background: "linear-gradient(135deg, #10b981, #34d399)",
                    borderRadius: "16px 16px 0 0",
                  }}
                >
                  <h5 className="modal-title text-white fw-bold">
                    <i className="fas fa-file-excel me-2"></i>
                    Excel'den Ürün Yükle
                  </h5>
                  <button
                    type="button"
                    className="btn-close btn-close-white"
                    onClick={() => setShowExcelModal(false)}
                  ></button>
                </div>
                <div className="modal-body p-4">
                  {/* Bilgi kutusu */}
                  <div className="alert alert-info border-0 mb-4">
                    <h6 className="fw-bold mb-2">
                      <i className="fas fa-info-circle me-2"></i>
                      Dosya Formatı
                    </h6>
                    <p className="mb-2 small">
                      Excel (.xlsx, .xls) veya CSV dosyası yükleyebilirsiniz.
                    </p>
                    <p className="mb-0 small">
                      <strong>Sütunlar:</strong> Ürün Adı, Açıklama, Fiyat,
                      Stok, Kategori ID, Görsel URL
                    </p>
                  </div>

                  {/* Dosya seçimi */}
                  <form onSubmit={handleExcelUpload}>
                    <div className="mb-4">
                      <label className="form-label fw-semibold">
                        Dosya Seçin
                      </label>
                      <input
                        type="file"
                        className="form-control"
                        accept=".xlsx,.xls,.csv"
                        onChange={(e) => setExcelFile(e.target.files[0])}
                      />
                      {excelFile && (
                        <small className="text-success mt-1 d-block">
                          <i className="fas fa-check-circle me-1"></i>
                          {excelFile.name} seçildi
                        </small>
                      )}
                    </div>

                    {/* Hata mesajı */}
                    {excelError && (
                      <div className="alert alert-danger border-0 py-2">
                        <i className="fas fa-exclamation-circle me-2"></i>
                        {excelError}
                      </div>
                    )}

                    {/* Başarı mesajı */}
                    {excelResult && (
                      <div
                        className={`alert border-0 py-2 ${
                          excelResult.errorCount > 0
                            ? "alert-warning"
                            : "alert-success"
                        }`}
                      >
                        <i
                          className={`fas ${
                            excelResult.errorCount > 0
                              ? "fa-exclamation-triangle"
                              : "fa-check-circle"
                          } me-2`}
                        ></i>
                        <strong>{excelResult.message}</strong>
                        <div className="mt-2 small">
                          <div>İşlenen: {excelResult.totalProcessed}</div>
                          <div className="text-success">
                            Başarılı: {excelResult.successCount}
                          </div>
                          {excelResult.errorCount > 0 && (
                            <div className="text-danger">
                              Hatalı: {excelResult.errorCount}
                            </div>
                          )}
                          {excelResult.errors &&
                            excelResult.errors.length > 0 && (
                              <ul className="mt-2 mb-0 ps-3">
                                {excelResult.errors
                                  .slice(0, 5)
                                  .map((err, i) => (
                                    <li key={i} className="text-danger">
                                      {err}
                                    </li>
                                  ))}
                              </ul>
                            )}
                        </div>
                      </div>
                    )}

                    <div className="d-flex gap-2">
                      <button
                        type="button"
                        className="btn btn-outline-primary flex-fill"
                        onClick={handleDownloadTemplate}
                      >
                        <i className="fas fa-download me-2"></i>
                        Şablon İndir
                      </button>
                      <button
                        type="submit"
                        className="btn text-white fw-semibold flex-fill"
                        style={{
                          background:
                            "linear-gradient(135deg, #10b981, #34d399)",
                        }}
                        disabled={excelLoading || !excelFile}
                      >
                        {excelLoading ? (
                          <>
                            <span className="spinner-border spinner-border-sm me-2"></span>
                            Yükleniyor...
                          </>
                        ) : (
                          <>
                            <i className="fas fa-upload me-2"></i>
                            Yükle
                          </>
                        )}
                      </button>
                    </div>
                  </form>
                </div>
              </div>
            </div>
          </div>
        )}

        {/* XML Import Modal */}
        {showXmlImportModal && (
          <XmlImportModal
            isOpen={showXmlImportModal}
            onClose={() => setShowXmlImportModal(false)}
            onImportComplete={() => {
              setShowXmlImportModal(false);
              fetchProducts(); // Ürün listesini yenile
            }}
          />
        )}

        {/* Variant Manager Modal */}
        {showVariantManager && selectedProductForVariants && (
          <VariantManager
            productId={selectedProductForVariants.id}
            productName={selectedProductForVariants.name}
            onClose={() => {
              setShowVariantManager(false);
              setSelectedProductForVariants(null);
            }}
          />
        )}
      </div>
    </div>
  );
};

export default AdminProducts;
