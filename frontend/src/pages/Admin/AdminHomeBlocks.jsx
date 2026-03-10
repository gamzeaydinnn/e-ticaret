/**
 * AdminHomeBlocks.jsx - Ana Sayfa Ürün Blok Yönetimi
 *
 * Bu bileşen, ana sayfada gösterilecek ürün bloklarının
 * yönetimini sağlar. Her blok bir poster + ürün listesi içerir.
 *
 * Özellikler:
 * - Blok listeleme (tablo görünümü)
 * - Yeni blok ekleme
 * - Blok düzenleme
 * - Blok silme
 * - Aktif/Pasif toggle
 * - Manuel ürün seçimi (ID veya isim ile arama)
 * - Kategori bazlı otomatik ürün
 * - İndirimli ürünler otomatik
 * - Sıralama değiştirme
 * - Başlık özelleştirme
 *
 * @author Senior Developer
 * @version 2.1.0 - Gelişmiş ürün arama ve ID ile seçim eklendi
 */

import React, {
  useState,
  useEffect,
  useCallback,
  useRef,
  useMemo,
} from "react";
import homeBlockService, { BLOCK_TYPES } from "../../services/homeBlockService";
import bannerService, { validateFile } from "../../services/bannerService";
import api from "../../services/api";

// ============================================
// SABİTLER
// ============================================

// Hazır Başlık Şablonları - Font Awesome İkonlarla (Profesyonel)
const TITLE_TEMPLATES = [
  {
    icon: "fas fa-bullseye",
    color: "#ef4444",
    title: "Bu Fırsatları Kaçırmayın",
    category: "indirim",
  },
  {
    icon: "fas fa-bolt",
    color: "#f59e0b",
    title: "Şok İndirimler",
    category: "indirim",
  },
  {
    icon: "fas fa-tags",
    color: "#ef4444",
    title: "Süper Fırsatlar",
    category: "indirim",
  },
  {
    icon: "fas fa-percentage",
    color: "#10b981",
    title: "Haftalık Kampanyalar",
    category: "indirim",
  },
  {
    icon: "fas fa-star",
    color: "#8b5cf6",
    title: "Özel Seçimler",
    category: "indirim",
  },
  {
    icon: "fas fa-gift",
    color: "#ec4899",
    title: "Hediye Fırsatlar",
    category: "indirim",
  },
  {
    icon: "fas fa-gem",
    color: "#6366f1",
    title: "Premium Koleksiyon",
    category: "premium",
  },
  {
    icon: "fas fa-crown",
    color: "#f59e0b",
    title: "Elit Ürünler",
    category: "premium",
  },
  {
    icon: "fas fa-fire",
    color: "#ef4444",
    title: "En Çok Satanlar",
    category: "populer",
  },
  {
    icon: "fas fa-trophy",
    color: "#f59e0b",
    title: "Haftanın Yıldızları",
    category: "populer",
  },
  {
    icon: "fas fa-heart",
    color: "#ef4444",
    title: "Müşteri Favorileri",
    category: "populer",
  },
  {
    icon: "fas fa-thumbs-up",
    color: "#3b82f6",
    title: "Sizin İçin Seçtiklerimiz",
    category: "oneri",
  },
  {
    icon: "fas fa-magic",
    color: "#8b5cf6",
    title: "Özel Öneriler",
    category: "oneri",
  },
  {
    icon: "fas fa-lightbulb",
    color: "#f59e0b",
    title: "İlgini Çekebilecek Ürünler",
    category: "oneri",
  },
  {
    icon: "fas fa-sparkles",
    color: "#10b981",
    title: "Yeni Gelenler",
    category: "yeni",
  },
  {
    icon: "fas fa-rocket",
    color: "#3b82f6",
    title: "Az Önce Eklendi",
    category: "yeni",
  },
  {
    icon: "fas fa-leaf",
    color: "#10b981",
    title: "Taze Ürünler",
    category: "yeni",
  },
  {
    icon: "fas fa-cheese",
    color: "#f59e0b",
    title: "Süt & Süt Ürünleri",
    category: "kategori",
  },
  {
    icon: "fas fa-drumstick-bite",
    color: "#ef4444",
    title: "Et & Et Ürünleri",
    category: "kategori",
  },
  {
    icon: "fas fa-carrot",
    color: "#f97316",
    title: "Meyve & Sebze",
    category: "kategori",
  },
  {
    icon: "fas fa-cheese",
    color: "#fbbf24",
    title: "Peynir Dünyası",
    category: "kategori",
  },
  {
    icon: "fas fa-bread-slice",
    color: "#d97706",
    title: "Fırından Taze",
    category: "kategori",
  },
  {
    icon: "fas fa-pump-soap",
    color: "#06b6d4",
    title: "Temizlik & Bakım",
    category: "kategori",
  },
  {
    icon: "fas fa-cookie",
    color: "#a855f7",
    title: "Atıştırmalıklar",
    category: "kategori",
  },
  {
    icon: "fas fa-mug-hot",
    color: "#78350f",
    title: "Kahve & İçecekler",
    category: "kategori",
  },
  {
    icon: "fas fa-wheat-awn",
    color: "#ca8a04",
    title: "Bakliyat & Tahıllar",
    category: "kategori",
  },
  {
    icon: "fas fa-jar",
    color: "#65a30d",
    title: "Konserveler",
    category: "kategori",
  },
  {
    icon: "fas fa-egg",
    color: "#fbbf24",
    title: "Kahvaltılık Lezzetler",
    category: "kategori",
  },
  {
    icon: "fas fa-cart-plus",
    color: "#ff6b35",
    title: "Hemen Sepete",
    category: "genel",
  },
  {
    icon: "fas fa-percent",
    color: "#ef4444",
    title: "Kampanyalı Ürünler",
    category: "kampanya",
  },
  {
    icon: "fas fa-bell",
    color: "#f59e0b",
    title: "Son Fırsat",
    category: "acil",
  },
  {
    icon: "fas fa-clock",
    color: "#ef4444",
    title: "Sınırlı Süre",
    category: "acil",
  },
  { emoji: "🔥", title: "Bugünün Fırsatları", category: "acil" },
];

const INITIAL_FORM = {
  id: 0,
  name: "",
  slug: "",
  title: "", // Yeni: Ana sayfada görünecek başlık (örn: "Süt Ürünleri", "İndirimli Ürünler")
  blockType: "manual",
  categoryId: null,
  posterImageUrl: "",
  backgroundColor: "#00BCD4",
  displayOrder: 0,
  maxProductCount: 6,
  viewAllUrl: "",
  viewAllText: "Tümünü Gör",
  isActive: true,
};

// ============================================
// ANA BİLEŞEN
// ============================================

export default function AdminHomeBlocks() {
  // State'ler
  const [blocks, setBlocks] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [form, setForm] = useState(INITIAL_FORM);
  const [showModal, setShowModal] = useState(false);
  const [saving, setSaving] = useState(false);
  const [feedback, setFeedback] = useState({ msg: "", type: "" });
  const [categories, setCategories] = useState([]);
  const [products, setProducts] = useState([]);
  const [selectedProducts, setSelectedProducts] = useState([]);
  const [showProductModal, setShowProductModal] = useState(false);
  const [currentBlockId, setCurrentBlockId] = useState(null);
  const [productSearch, setProductSearch] = useState("");

  // Yeni: ID ile ürün arama
  const [productIdInput, setProductIdInput] = useState("");
  const [searchMode, setSearchMode] = useState("name"); // "name" veya "id"
  const searchDebounceRef = useRef(null);
  const [productSearchLoading, setProductSearchLoading] = useState(false);

  // Dosya yükleme state'leri
  const [imagePreview, setImagePreview] = useState("");
  const [uploading, setUploading] = useState(false);
  const [isDragging, setIsDragging] = useState(false);
  const fileInputRef = useRef(null);
  const dragCounterRef = useRef(0);

  // ============================================
  // VERİ ÇEKME
  // ============================================

  const fetchBlocks = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await homeBlockService.getAllBlocks();
      setBlocks(data || []);
    } catch (err) {
      console.error("[AdminHomeBlocks] Veri çekme hatası:", err);
      setError(err.message || "Bloklar yüklenirken hata oluştu");
      setBlocks([]);
    } finally {
      setLoading(false);
    }
  }, []);

  const fetchCategories = useCallback(async () => {
    try {
      const data = await api.get("/api/categories");
      const items = data?.$values || data || [];
      setCategories(Array.isArray(items) ? items : []);
    } catch (err) {
      console.error("[AdminHomeBlocks] Kategori çekme hatası:", err);
    }
  }, []);

  const fetchProducts = useCallback(async (searchQuery = "") => {
    try {
      let data;
      if (searchQuery.trim()) {
        data = await api.get(
          `/api/products/search?query=${encodeURIComponent(searchQuery.trim())}&page=1&size=50`,
        );
      } else {
        data = await api.get("/api/products/admin/all?page=1&size=500");
      }
      const items = data?.items || data?.$values || data || [];
      setProducts(Array.isArray(items) ? items : []);
    } catch (err) {
      console.error("[AdminHomeBlocks] Ürün çekme hatası:", err);
      setProducts([]);
    }
  }, []);

  useEffect(() => {
    fetchBlocks();
    fetchCategories();
    fetchProducts();
  }, [fetchBlocks, fetchCategories, fetchProducts]);

  // ============================================
  // FEEDBACK
  // ============================================

  const showFeedback = useCallback((msg, type = "success") => {
    setFeedback({ msg, type });
    setTimeout(() => setFeedback({ msg: "", type: "" }), 4000);
  }, []);

  // ============================================
  // MODAL İŞLEMLERİ
  // ============================================

  const openCreateModal = () => {
    setForm({ ...INITIAL_FORM, displayOrder: blocks.length });
    setImagePreview("");
    setShowModal(true);
  };

  const openEditModal = (block) => {
    setForm({
      id: block.id,
      name: block.name || "",
      slug: block.slug || "",
      title: block.title || "", // Yeni: Başlık alanı
      blockType: block.blockType || "manual",
      categoryId: block.categoryId || null,
      posterImageUrl: block.posterImageUrl || "",
      backgroundColor: block.backgroundColor || "#00BCD4",
      displayOrder: block.displayOrder || 0,
      maxProductCount: block.maxProductCount || 6,
      viewAllUrl: block.viewAllUrl || "",
      viewAllText: block.viewAllText || "Tümünü Gör",
      isActive: block.isActive ?? true,
    });
    setImagePreview(block.posterImageUrl || "");
    setShowModal(true);
  };

  const closeModal = () => {
    setShowModal(false);
    setForm(INITIAL_FORM);
    setImagePreview("");
    if (fileInputRef.current) {
      fileInputRef.current.value = "";
    }
  };

  // ============================================
  // DOSYA YÜKLEME İŞLEMLERİ
  // ============================================

  const handleFileUpload = useCallback(
    async (file) => {
      if (!file) return;

      // Dosya validasyonu
      const validation = validateFile(file);
      if (!validation.valid) {
        showFeedback(validation.error, "danger");
        return;
      }

      setUploading(true);
      try {
        // API'ye yükle (bannerService kullanıyoruz)
        const result = await bannerService.uploadImage(file);

        if (result && result.imageUrl) {
          setImagePreview(result.imageUrl);
          setForm((prev) => ({ ...prev, posterImageUrl: result.imageUrl }));
          showFeedback("Görsel başarıyla yüklendi", "success");
        } else {
          throw new Error("Görsel URL'si alınamadı");
        }
      } catch (err) {
        console.error("[AdminHomeBlocks] Dosya yükleme hatası:", err);
        showFeedback(err.message || "Görsel yüklenirken hata oluştu", "danger");

        // Hata durumunda local preview göster (fallback)
        const reader = new FileReader();
        reader.onload = (e) => {
          setImagePreview(e.target.result);
        };
        reader.readAsDataURL(file);
      } finally {
        setUploading(false);
      }
    },
    [showFeedback],
  );

  const handleInputFileChange = useCallback(
    (e) => {
      const file = e.target.files?.[0];
      if (file) {
        handleFileUpload(file);
      }
    },
    [handleFileUpload],
  );

  // Drag & Drop handlers
  const handleDragEnter = useCallback((e) => {
    e.preventDefault();
    e.stopPropagation();
    dragCounterRef.current++;
    if (e.dataTransfer?.items?.length > 0) {
      setIsDragging(true);
    }
  }, []);

  const handleDragLeave = useCallback((e) => {
    e.preventDefault();
    e.stopPropagation();
    dragCounterRef.current--;
    if (dragCounterRef.current === 0) {
      setIsDragging(false);
    }
  }, []);

  const handleDragOver = useCallback((e) => {
    e.preventDefault();
    e.stopPropagation();
  }, []);

  const handleDrop = useCallback(
    (e) => {
      e.preventDefault();
      e.stopPropagation();
      setIsDragging(false);
      dragCounterRef.current = 0;

      const files = e.dataTransfer?.files;
      if (files && files.length > 0) {
        handleFileUpload(files[0]);
      }
    },
    [handleFileUpload],
  );

  // ============================================
  // CRUD İŞLEMLERİ
  // ============================================

  const handleSubmit = async (e) => {
    e.preventDefault();

    if (!form.name?.trim()) {
      showFeedback("Blok adı zorunludur", "danger");
      return;
    }

    setSaving(true);
    try {
      if (form.id > 0) {
        await homeBlockService.updateBlock(form.id, form);
        showFeedback("Blok başarıyla güncellendi", "success");
      } else {
        await homeBlockService.createBlock(form);
        showFeedback("Blok başarıyla oluşturuldu", "success");
      }
      closeModal();
      await fetchBlocks();
    } catch (err) {
      console.error("[AdminHomeBlocks] Kaydetme hatası:", err);
      showFeedback(err.message || "Kaydetme sırasında hata oluştu", "danger");
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async (id) => {
    if (!window.confirm("Bu bloğu silmek istediğinize emin misiniz?")) return;

    try {
      await homeBlockService.deleteBlock(id);
      showFeedback("Blok başarıyla silindi", "success");
      await fetchBlocks();
    } catch (err) {
      showFeedback(err.message || "Silme sırasında hata oluştu", "danger");
    }
  };

  const toggleActive = async (block) => {
    try {
      await homeBlockService.toggleBlock(block.id);
      showFeedback(
        block.isActive ? "Blok pasif yapıldı" : "Blok aktif yapıldı",
        "success",
      );
      await fetchBlocks();
    } catch (err) {
      showFeedback(err.message || "Durum güncellenirken hata oluştu", "danger");
    }
  };

  // ============================================
  // ÜRÜN SEÇİMİ
  // ============================================

  const openProductModal = async (blockId) => {
    setCurrentBlockId(blockId);
    try {
      const blockProducts = await homeBlockService.getBlockProducts(blockId);
      setSelectedProducts(blockProducts.map((p) => p.id));
    } catch {
      setSelectedProducts([]);
    }
    setShowProductModal(true);
  };

  const toggleProductSelection = (productId) => {
    setSelectedProducts((prev) =>
      prev.includes(productId)
        ? prev.filter((id) => id !== productId)
        : [...prev, productId],
    );
  };

  const saveProducts = async () => {
    try {
      await homeBlockService.setBlockProducts(currentBlockId, selectedProducts);
      showFeedback("Ürünler başarıyla kaydedildi", "success");
      setShowProductModal(false);
      setProductSearch("");
      setProductIdInput("");
      setSearchMode("name");
      await fetchBlocks();
    } catch (err) {
      showFeedback(
        err.message || "Ürünler kaydedilirken hata oluştu",
        "danger",
      );
    }
  };

  // =====================================================
  // GELİŞMİŞ ÜRÜN ARAMA - BACKEND BAĞLANTILI
  // Debounce ile performanslı sunucu taraflı arama
  // =====================================================

  // Backend'den ürün ara (debounce ile)
  useEffect(() => {
    if (!showProductModal) return;

    if (searchMode === "name" && productSearch.trim().length >= 2) {
      setProductSearchLoading(true);
      if (searchDebounceRef.current) clearTimeout(searchDebounceRef.current);
      searchDebounceRef.current = setTimeout(async () => {
        await fetchProducts(productSearch.trim());
        setProductSearchLoading(false);
      }, 400);
    } else if (searchMode === "name" && productSearch.trim().length === 0) {
      fetchProducts();
    }

    return () => {
      if (searchDebounceRef.current) clearTimeout(searchDebounceRef.current);
    };
  }, [productSearch, searchMode, showProductModal, fetchProducts]);

  // ID ile arama - lokal filtreleme
  const filteredProducts = useMemo(() => {
    if (searchMode === "id" && productIdInput.trim()) {
      const searchId = productIdInput.trim();
      return products.filter(
        (p) => String(p.id) === searchId || String(p.id).includes(searchId),
      );
    }
    return products;
  }, [products, productIdInput, searchMode]);

  // ID ile doğrudan ürün ekleme
  const handleAddProductById = useCallback(async () => {
    const id = parseInt(productIdInput.trim());
    if (isNaN(id)) {
      showFeedback("Geçerli bir ürün ID'si girin", "warning");
      return;
    }

    if (selectedProducts.includes(id)) {
      showFeedback("Bu ürün zaten seçili", "warning");
      return;
    }

    // Önce yüklü listede ara
    let product = products.find((p) => p.id === id);

    // Bulunamazsa backend'den getir
    if (!product) {
      try {
        const data = await api.get(`/api/products/${id}`);
        if (data && data.id) {
          product = data;
          setProducts((prev) => [...prev, product]);
        }
      } catch {
        showFeedback(`ID: ${id} ile ürün bulunamadı`, "danger");
        return;
      }
    }

    if (!product) {
      showFeedback(`ID: ${id} ile ürün bulunamadı`, "danger");
      return;
    }

    setSelectedProducts((prev) => [...prev, id]);
    setProductIdInput("");
    showFeedback(`"${product.name}" eklendi`, "success");
  }, [productIdInput, products, selectedProducts, showFeedback]);

  // ============================================
  // HELPER FONKSİYONLAR
  // ============================================

  const getBlockTypeLabel = (type) => {
    const found = BLOCK_TYPES.find((t) => t.value === type);
    return found?.label || type;
  };

  const getBlockTypeIcon = (type) => {
    const icons = {
      manual: "fas fa-hand-pointer",
      category: "fas fa-folder",
      discounted: "fas fa-tags",
      newest: "fas fa-clock",
      bestseller: "fas fa-fire",
    };
    return icons[type] || "fas fa-cube";
  };

  // ============================================
  // LOADING STATE
  // ============================================

  if (loading && blocks.length === 0) {
    return (
      <div className="container-fluid py-4">
        <div className="d-flex justify-content-between align-items-center mb-4">
          <div className="placeholder-glow">
            <span
              className="placeholder col-6"
              style={{ width: "250px", height: "32px" }}
            ></span>
          </div>
          <div className="placeholder-glow">
            <span
              className="placeholder"
              style={{ width: "140px", height: "42px", borderRadius: "8px" }}
            ></span>
          </div>
        </div>
        <div className="card border-0 shadow-sm">
          <div className="card-body p-4">
            {[1, 2, 3].map((i) => (
              <div key={i} className="placeholder-glow mb-3">
                <span
                  className="placeholder w-100"
                  style={{ height: "60px", borderRadius: "8px" }}
                ></span>
              </div>
            ))}
          </div>
        </div>
      </div>
    );
  }

  // ============================================
  // RENDER
  // ============================================

  return (
    <div className="container-fluid py-4">
      {/* Header */}
      <div className="d-flex flex-wrap justify-content-between align-items-center mb-4 gap-3">
        <div>
          <h4 className="mb-1 d-flex align-items-center gap-2">
            <i className="fas fa-th-large text-primary"></i>
            Ana Sayfa Blokları
          </h4>
          <p className="text-muted mb-0 small">
            Ana sayfada görünen poster + ürün bloklarını yönetin
          </p>
        </div>
        <div className="d-flex gap-2">
          <button
            className="btn btn-outline-secondary"
            onClick={fetchBlocks}
            disabled={loading}
          >
            <i className={`fas fa-sync-alt ${loading ? "fa-spin" : ""}`}></i>
          </button>
          <button
            className="btn btn-primary d-flex align-items-center gap-2"
            onClick={openCreateModal}
          >
            <i className="fas fa-plus"></i>
            Yeni Blok Ekle
          </button>
        </div>
      </div>

      {/* Feedback Alert */}
      {feedback.msg && (
        <div
          className={`alert alert-${feedback.type} alert-dismissible fade show`}
          role="alert"
        >
          <i
            className={`fas ${feedback.type === "success" ? "fa-check-circle" : "fa-exclamation-circle"} me-2`}
          ></i>
          {feedback.msg}
          <button
            type="button"
            className="btn-close"
            onClick={() => setFeedback({ msg: "", type: "" })}
          ></button>
        </div>
      )}

      {/* Error Alert */}
      {error && (
        <div className="alert alert-danger d-flex align-items-center gap-2">
          <i className="fas fa-exclamation-triangle"></i>
          <span>{error}</span>
          <button
            className="btn btn-sm btn-danger ms-auto"
            onClick={fetchBlocks}
          >
            Tekrar Dene
          </button>
        </div>
      )}

      {/* Blok Listesi */}
      <div className="card border-0 shadow-sm">
        <div
          className="card-header bg-gradient text-white py-3"
          style={{
            background: "linear-gradient(135deg, #667eea 0%, #764ba2 100%)",
          }}
        >
          <div className="d-flex align-items-center gap-2">
            <i className="fas fa-layer-group"></i>
            <span className="fw-semibold">Bloklar ({blocks.length})</span>
          </div>
        </div>
        <div className="card-body p-0">
          {blocks.length === 0 ? (
            <div className="text-center py-5">
              <i className="fas fa-inbox fa-3x text-muted mb-3"></i>
              <h5 className="text-muted">Henüz blok oluşturulmamış</h5>
              <p className="text-muted small mb-3">
                Ana sayfada gösterilecek ilk bloğunuzu oluşturun
              </p>
              <button className="btn btn-primary" onClick={openCreateModal}>
                <i className="fas fa-plus me-2"></i>
                İlk Bloğu Oluştur
              </button>
            </div>
          ) : (
            <div className="table-responsive">
              <table className="table table-hover align-middle mb-0">
                <thead className="bg-light">
                  <tr>
                    <th style={{ width: "60px" }}>Sıra</th>
                    <th style={{ width: "80px" }}>Poster</th>
                    <th>Blok Adı</th>
                    <th style={{ width: "140px" }}>Tip</th>
                    <th style={{ width: "100px" }}>Ürün Sayısı</th>
                    <th style={{ width: "100px" }}>Durum</th>
                    <th style={{ width: "180px" }}>İşlemler</th>
                  </tr>
                </thead>
                <tbody>
                  {blocks
                    .sort((a, b) => a.displayOrder - b.displayOrder)
                    .map((block) => (
                      <tr
                        key={block.id}
                        className={!block.isActive ? "table-secondary" : ""}
                      >
                        <td>
                          <span className="badge bg-secondary rounded-pill">
                            #{block.displayOrder + 1}
                          </span>
                        </td>
                        <td>
                          {block.posterImageUrl ? (
                            <img
                              src={block.posterImageUrl}
                              alt={block.name}
                              style={{
                                width: "60px",
                                height: "60px",
                                objectFit: "cover",
                                borderRadius: "8px",
                                border: "2px solid #e5e7eb",
                              }}
                            />
                          ) : (
                            <div
                              style={{
                                width: "60px",
                                height: "60px",
                                borderRadius: "8px",
                                backgroundColor:
                                  block.backgroundColor || "#00BCD4",
                                display: "flex",
                                alignItems: "center",
                                justifyContent: "center",
                              }}
                            >
                              <i className="fas fa-image text-white"></i>
                            </div>
                          )}
                        </td>
                        <td>
                          <div className="fw-semibold">{block.name}</div>
                          <small className="text-muted">/{block.slug}</small>
                        </td>
                        <td>
                          <span
                            className={`badge ${
                              block.blockType === "manual"
                                ? "bg-primary"
                                : block.blockType === "category"
                                  ? "bg-info"
                                  : block.blockType === "discounted"
                                    ? "bg-danger"
                                    : block.blockType === "newest"
                                      ? "bg-success"
                                      : "bg-warning"
                            }`}
                          >
                            <i
                              className={`${getBlockTypeIcon(block.blockType)} me-1`}
                            ></i>
                            {getBlockTypeLabel(block.blockType)}
                          </span>
                        </td>
                        <td>
                          <span className="badge bg-light text-dark">
                            {block.products?.length || 0} /{" "}
                            {block.maxProductCount}
                          </span>
                        </td>
                        <td>
                          <span
                            className={`badge ${block.isActive ? "bg-success" : "bg-secondary"}`}
                          >
                            {block.isActive ? "Aktif" : "Pasif"}
                          </span>
                        </td>
                        <td>
                          <div className="btn-group btn-group-sm">
                            <button
                              className="btn btn-outline-primary"
                              onClick={() => openEditModal(block)}
                              title="Düzenle"
                            >
                              <i className="fas fa-edit"></i>
                            </button>
                            {block.blockType === "manual" && (
                              <button
                                className="btn btn-info text-white fw-bold"
                                onClick={() => openProductModal(block.id)}
                                title="Ürünleri Seç/Düzenle - Manuel seçim için gerekli"
                              >
                                <i className="fas fa-boxes me-1"></i>
                                Ürün Seç
                              </button>
                            )}
                            <button
                              className={`btn ${block.isActive ? "btn-outline-warning" : "btn-outline-success"}`}
                              onClick={() => toggleActive(block)}
                              title={block.isActive ? "Pasif Yap" : "Aktif Yap"}
                            >
                              <i
                                className={`fas ${block.isActive ? "fa-eye-slash" : "fa-eye"}`}
                              ></i>
                            </button>
                            <button
                              className="btn btn-outline-danger"
                              onClick={() => handleDelete(block.id)}
                              title="Sil"
                            >
                              <i className="fas fa-trash"></i>
                            </button>
                          </div>
                        </td>
                      </tr>
                    ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </div>

      {/* Blok Tipleri Bilgi Kartı */}
      <div className="card border-0 shadow-sm mt-4">
        <div className="card-header bg-light">
          <i className="fas fa-lightbulb text-warning me-2"></i>
          <span className="fw-semibold">Blok Tipleri Hakkında</span>
        </div>
        <div className="card-body">
          <div className="row g-3">
            {BLOCK_TYPES.map((type) => (
              <div key={type.value} className="col-md-4 col-lg-2">
                <div className="d-flex align-items-start gap-2">
                  <i
                    className={`${getBlockTypeIcon(type.value)} text-${
                      type.value === "manual"
                        ? "primary"
                        : type.value === "category"
                          ? "info"
                          : type.value === "discounted"
                            ? "danger"
                            : type.value === "newest"
                              ? "success"
                              : "warning"
                    } mt-1`}
                  ></i>
                  <div>
                    <div className="fw-semibold small">{type.label}</div>
                    <small className="text-muted">{type.description}</small>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* Blok Ekleme/Düzenleme Modal */}
      {showModal && (
        <div
          className="modal fade show d-block"
          style={{ backgroundColor: "rgba(0,0,0,0.5)" }}
        >
          <div className="modal-dialog modal-lg modal-dialog-centered modal-dialog-scrollable">
            <div className="modal-content">
              <div className="modal-header bg-primary text-white">
                <h5 className="modal-title">
                  <i
                    className={`fas ${form.id > 0 ? "fa-edit" : "fa-plus"} me-2`}
                  ></i>
                  {form.id > 0 ? "Blok Düzenle" : "Yeni Blok Oluştur"}
                </h5>
                <button
                  type="button"
                  className="btn-close btn-close-white"
                  onClick={closeModal}
                ></button>
              </div>
              <form
                id="home-block-form"
                onSubmit={handleSubmit}
                style={{ flex: "1 1 auto", overflow: "hidden", display: "flex", flexDirection: "column" }}
              >
                <div className="modal-body">
                  <div className="row g-3">
                    {/* Blok Adı */}
                    <div className="col-md-6">
                      <label className="form-label fw-semibold">
                        Blok Adı <span className="text-danger">*</span>
                      </label>
                      <input
                        type="text"
                        className="form-control"
                        value={form.name}
                        onChange={(e) =>
                          setForm({ ...form, name: e.target.value })
                        }
                        placeholder="Örn: İndirimli Ürünler (Admin için)"
                        required
                      />
                      <small className="text-muted">
                        Admin panelde görünen isim
                      </small>
                    </div>

                    {/* Başlık - Ana sayfada görünecek */}
                    <div className="col-md-6">
                      <label className="form-label fw-semibold">
                        Başlık (Ana Sayfa){" "}
                        <span className="text-danger">*</span>
                      </label>
                      <div className="input-group">
                        <input
                          type="text"
                          className="form-control"
                          value={form.title}
                          onChange={(e) =>
                            setForm({ ...form, title: e.target.value })
                          }
                          placeholder="Başlık yazın veya şablon seçin →"
                          required
                        />
                        <button
                          className="btn btn-outline-primary dropdown-toggle"
                          type="button"
                          data-bs-toggle="dropdown"
                          aria-expanded="false"
                          style={{ minWidth: "140px" }}
                        >
                          🎨 Şablonlar
                        </button>
                        <ul
                          className="dropdown-menu dropdown-menu-end"
                          style={{
                            maxHeight: "400px",
                            overflowY: "auto",
                            minWidth: "320px",
                          }}
                        >
                          <li>
                            <h6 className="dropdown-header">
                              <i className="fas fa-tags text-danger me-1"></i>{" "}
                              İndirim & Fırsat
                            </h6>
                          </li>
                          {TITLE_TEMPLATES.filter(
                            (t) => t.category === "indirim",
                          ).map((template, idx) => (
                            <li key={`indirim-${idx}`}>
                              <button
                                className="dropdown-item d-flex align-items-center gap-2"
                                type="button"
                                onClick={() =>
                                  setForm({
                                    ...form,
                                    title: template.title,
                                  })
                                }
                              >
                                <i
                                  className={template.icon}
                                  style={{
                                    color: template.color,
                                    width: "16px",
                                  }}
                                ></i>
                                {template.title}
                              </button>
                            </li>
                          ))}
                          <li>
                            <hr className="dropdown-divider" />
                          </li>
                          <li>
                            <h6 className="dropdown-header">
                              <i className="fas fa-fire text-warning me-1"></i>{" "}
                              Popüler & Favori
                            </h6>
                          </li>
                          {TITLE_TEMPLATES.filter(
                            (t) => t.category === "populer",
                          ).map((template, idx) => (
                            <li key={`populer-${idx}`}>
                              <button
                                className="dropdown-item d-flex align-items-center gap-2"
                                type="button"
                                onClick={() =>
                                  setForm({
                                    ...form,
                                    title: template.title,
                                  })
                                }
                              >
                                <i
                                  className={template.icon}
                                  style={{
                                    color: template.color,
                                    width: "16px",
                                  }}
                                ></i>
                                {template.title}
                              </button>
                            </li>
                          ))}
                          <li>
                            <hr className="dropdown-divider" />
                          </li>
                          <li>
                            <h6 className="dropdown-header">
                              <i
                                className="fas fa-gem text-purple me-1"
                                style={{ color: "#8b5cf6" }}
                              ></i>{" "}
                              Premium
                            </h6>
                          </li>
                          {TITLE_TEMPLATES.filter(
                            (t) => t.category === "premium",
                          ).map((template, idx) => (
                            <li key={`premium-${idx}`}>
                              <button
                                className="dropdown-item d-flex align-items-center gap-2"
                                type="button"
                                onClick={() =>
                                  setForm({
                                    ...form,
                                    title: template.title,
                                  })
                                }
                              >
                                <i
                                  className={template.icon}
                                  style={{
                                    color: template.color,
                                    width: "16px",
                                  }}
                                ></i>
                                {template.title}
                              </button>
                            </li>
                          ))}
                          <li>
                            <hr className="dropdown-divider" />
                          </li>
                          <li>
                            <h6 className="dropdown-header">
                              <i className="fas fa-thumbs-up text-primary me-1"></i>{" "}
                              Öneri & Seçim
                            </h6>
                          </li>
                          {TITLE_TEMPLATES.filter(
                            (t) => t.category === "oneri",
                          ).map((template, idx) => (
                            <li key={`oneri-${idx}`}>
                              <button
                                className="dropdown-item d-flex align-items-center gap-2"
                                type="button"
                                onClick={() =>
                                  setForm({
                                    ...form,
                                    title: template.title,
                                  })
                                }
                              >
                                <i
                                  className={template.icon}
                                  style={{
                                    color: template.color,
                                    width: "16px",
                                  }}
                                ></i>
                                {template.title}
                              </button>
                            </li>
                          ))}
                          <li>
                            <hr className="dropdown-divider" />
                          </li>
                          <li>
                            <h6 className="dropdown-header">
                              <i className="fas fa-sparkles text-success me-1"></i>{" "}
                              Yeni Ürünler
                            </h6>
                          </li>
                          {TITLE_TEMPLATES.filter(
                            (t) => t.category === "yeni",
                          ).map((template, idx) => (
                            <li key={`yeni-${idx}`}>
                              <button
                                className="dropdown-item d-flex align-items-center gap-2"
                                type="button"
                                onClick={() =>
                                  setForm({
                                    ...form,
                                    title: template.title,
                                  })
                                }
                              >
                                <i
                                  className={template.icon}
                                  style={{
                                    color: template.color,
                                    width: "16px",
                                  }}
                                ></i>
                                {template.title}
                              </button>
                            </li>
                          ))}
                          <li>
                            <hr className="dropdown-divider" />
                          </li>
                          <li>
                            <h6 className="dropdown-header">
                              <i className="fas fa-th-large text-info me-1"></i>{" "}
                              Kategoriler
                            </h6>
                          </li>
                          {TITLE_TEMPLATES.filter(
                            (t) => t.category === "kategori",
                          ).map((template, idx) => (
                            <li key={`kategori-${idx}`}>
                              <button
                                className="dropdown-item d-flex align-items-center gap-2"
                                type="button"
                                onClick={() =>
                                  setForm({
                                    ...form,
                                    title: template.title,
                                  })
                                }
                              >
                                <i
                                  className={template.icon}
                                  style={{
                                    color: template.color,
                                    width: "16px",
                                  }}
                                ></i>
                                {template.title}
                              </button>
                            </li>
                          ))}
                          <li>
                            <hr className="dropdown-divider" />
                          </li>
                          <li>
                            <h6 className="dropdown-header">
                              <i className="fas fa-clock text-danger me-1"></i>{" "}
                              Acil & Kampanya
                            </h6>
                          </li>
                          {TITLE_TEMPLATES.filter(
                            (t) =>
                              t.category === "acil" ||
                              t.category === "kampanya",
                          ).map((template, idx) => (
                            <li key={`acil-${idx}`}>
                              <button
                                className="dropdown-item d-flex align-items-center gap-2"
                                type="button"
                                onClick={() =>
                                  setForm({
                                    ...form,
                                    title: template.title,
                                  })
                                }
                              >
                                <i
                                  className={template.icon}
                                  style={{
                                    color: template.color,
                                    width: "16px",
                                  }}
                                ></i>
                                {template.title}
                              </button>
                            </li>
                          ))}
                        </ul>
                      </div>
                      <small className="text-muted">
                        Müşterilerin göreceği başlık • Hazır şablondan seçebilir
                        veya özel yazabilirsiniz
                      </small>
                    </div>

                    {/* Slug */}
                    <div className="col-md-6">
                      <label className="form-label fw-semibold">
                        Slug (URL)
                      </label>
                      <input
                        type="text"
                        className="form-control"
                        value={form.slug}
                        onChange={(e) =>
                          setForm({ ...form, slug: e.target.value })
                        }
                        placeholder="Otomatik oluşturulur"
                      />
                      <small className="text-muted">
                        Boş bırakılırsa otomatik oluşturulur
                      </small>
                    </div>

                    {/* Blok Tipi */}
                    <div className="col-md-6">
                      <label className="form-label fw-semibold">
                        Blok Tipi
                      </label>
                      <select
                        className="form-select"
                        value={form.blockType}
                        onChange={(e) =>
                          setForm({ ...form, blockType: e.target.value })
                        }
                      >
                        {BLOCK_TYPES.map((type) => (
                          <option key={type.value} value={type.value}>
                            {type.label} - {type.description}
                          </option>
                        ))}
                      </select>

                      {/* Manuel Seçim İçin Bilgilendirme */}
                      {form.blockType === "manual" && (
                        <div className="alert alert-info mt-2 py-2 px-3 mb-0">
                          <i className="fas fa-info-circle me-2"></i>
                          <strong>Manuel Seçim:</strong> Blok kaydedildikten
                          sonra listede{" "}
                          <i className="fas fa-boxes text-info"></i> "Ürün Seç"
                          butonuna tıklayarak ürünleri ekleyebilirsiniz.
                        </div>
                      )}

                      {/* Otomatik Blok Tipleri İçin Bilgilendirme */}
                      {form.blockType !== "manual" &&
                        form.blockType !== "category" && (
                          <div className="alert alert-success mt-2 py-2 px-3 mb-0">
                            <i className="fas fa-check-circle me-2"></i>
                            <strong>Otomatik:</strong> Ürünler blok tipine göre
                            otomatik seçilir.
                          </div>
                        )}
                    </div>

                    {/* Kategori (sadece category tipinde) */}
                    {form.blockType === "category" && (
                      <div className="col-md-6">
                        <label className="form-label fw-semibold">
                          Kategori
                        </label>
                        <select
                          className="form-select"
                          value={form.categoryId || ""}
                          onChange={(e) =>
                            setForm({
                              ...form,
                              categoryId: e.target.value
                                ? parseInt(e.target.value)
                                : null,
                            })
                          }
                        >
                          <option value="">Kategori seçin...</option>
                          {categories.map((cat) => (
                            <option key={cat.id} value={cat.id}>
                              {cat.name}
                            </option>
                          ))}
                        </select>
                      </div>
                    )}

                    {/* Poster Görseli - Dosya Yükleme */}
                    <div className="col-12">
                      <label className="form-label fw-semibold">
                        Poster Görseli
                      </label>
                      <div
                        className={`border rounded-3 p-4 text-center position-relative ${
                          isDragging
                            ? "border-primary bg-light"
                            : "border-dashed"
                        }`}
                        style={{
                          borderStyle: "dashed",
                          cursor: "pointer",
                          minHeight: "200px",
                          transition: "all 0.3s ease",
                        }}
                        onDragEnter={handleDragEnter}
                        onDragLeave={handleDragLeave}
                        onDragOver={handleDragOver}
                        onDrop={handleDrop}
                        onClick={() => fileInputRef.current?.click()}
                      >
                        <input
                          type="file"
                          ref={fileInputRef}
                          className="d-none"
                          accept="image/*"
                          onChange={handleInputFileChange}
                        />

                        {uploading ? (
                          <div className="py-4">
                            <div
                              className="spinner-border text-primary mb-3"
                              role="status"
                            >
                              <span className="visually-hidden">
                                Yükleniyor...
                              </span>
                            </div>
                            <p className="text-muted mb-0">
                              Görsel yükleniyor...
                            </p>
                          </div>
                        ) : imagePreview || form.posterImageUrl ? (
                          <div className="position-relative d-inline-block">
                            <img
                              src={imagePreview || form.posterImageUrl}
                              alt="Poster önizleme"
                              style={{
                                maxWidth: "100%",
                                maxHeight: "200px",
                                objectFit: "contain",
                                borderRadius: "8px",
                              }}
                            />
                            <button
                              type="button"
                              className="btn btn-danger btn-sm position-absolute top-0 end-0 m-1"
                              onClick={(e) => {
                                e.stopPropagation();
                                setImagePreview("");
                                setForm({ ...form, posterImageUrl: "" });
                              }}
                            >
                              <i className="fas fa-times"></i>
                            </button>
                          </div>
                        ) : (
                          <div className="py-4">
                            <i className="fas fa-cloud-upload-alt fa-3x text-muted mb-3"></i>
                            <p className="mb-2 text-muted">
                              <strong>Tıklayın</strong> veya{" "}
                              <strong>sürükleyip bırakın</strong>
                            </p>
                            <small className="text-muted d-block">
                              PNG, JPG, JPEG, WebP • Maks. 5MB
                            </small>
                            <div className="alert alert-info mt-3 mb-0 py-2 px-3 small">
                              <i className="fas fa-info-circle me-1"></i>
                              <strong>Önerilen Boyut:</strong> 220x320 piksel
                              (dikey poster)
                            </div>
                          </div>
                        )}

                        {isDragging && (
                          <div
                            className="position-absolute top-0 start-0 w-100 h-100 d-flex align-items-center justify-content-center"
                            style={{
                              backgroundColor: "rgba(13, 110, 253, 0.1)",
                              borderRadius: "12px",
                            }}
                          >
                            <div className="text-primary">
                              <i className="fas fa-download fa-2x mb-2"></i>
                              <p className="mb-0 fw-semibold">Bırakın</p>
                            </div>
                          </div>
                        )}
                      </div>
                    </div>

                    {/* Maksimum Ürün */}
                    <div className="col-md-4">
                      <label className="form-label fw-semibold">
                        Maks. Ürün Sayısı
                      </label>
                      <input
                        type="number"
                        className="form-control"
                        value={form.maxProductCount}
                        onChange={(e) =>
                          setForm({
                            ...form,
                            maxProductCount: parseInt(e.target.value) || 6,
                          })
                        }
                        min="1"
                        max="20"
                      />
                    </div>

                    {/* Sıralama */}
                    <div className="col-md-4">
                      <label className="form-label fw-semibold">Sıralama</label>
                      <input
                        type="number"
                        className="form-control"
                        value={form.displayOrder}
                        onChange={(e) =>
                          setForm({
                            ...form,
                            displayOrder: parseInt(e.target.value) || 0,
                          })
                        }
                        min="0"
                      />
                    </div>

                    {/* Durum */}
                    <div className="col-md-4">
                      <label className="form-label fw-semibold">Durum</label>
                      <div className="form-check form-switch mt-2">
                        <input
                          type="checkbox"
                          className="form-check-input"
                          id="isActiveSwitch"
                          checked={form.isActive}
                          onChange={(e) =>
                            setForm({ ...form, isActive: e.target.checked })
                          }
                        />
                        <label
                          className="form-check-label"
                          htmlFor="isActiveSwitch"
                        >
                          {form.isActive ? "Aktif" : "Pasif"}
                        </label>
                      </div>
                    </div>

                    {/* Tümünü Gör Linki - OTOMATİK OLUŞTURULUR */}
                    <div className="col-md-6">
                      <label className="form-label fw-semibold">
                        Tümünü Gör Linki
                        <span className="badge bg-success ms-2">Otomatik</span>
                      </label>
                      <div className="input-group">
                        <span className="input-group-text bg-light">
                          <i className="fas fa-link text-muted"></i>
                        </span>
                        <input
                          type="text"
                          className="form-control bg-light"
                          value={
                            form.slug
                              ? `/collection/${form.slug}`
                              : "(Slug oluşturulunca otomatik belirlenir)"
                          }
                          disabled
                          title="Bu link blok slug'ından otomatik oluşturulur"
                        />
                      </div>
                      <small className="text-muted">
                        <i className="fas fa-info-circle me-1"></i>
                        Tümünü Gör butonu otomatik olarak{" "}
                        <code>/collection/{form.slug || "slug"}</code> adresine
                        yönlendirir
                      </small>
                    </div>

                    {/* Tümünü Gör Metni */}
                    <div className="col-md-6">
                      <label className="form-label fw-semibold">
                        Tümünü Gör Metni
                      </label>
                      <input
                        type="text"
                        className="form-control"
                        value={form.viewAllText}
                        onChange={(e) =>
                          setForm({ ...form, viewAllText: e.target.value })
                        }
                        placeholder="Tümünü Gör"
                      />
                    </div>

                    {/* MANUEL SEÇİM İÇİN ÜRÜN ARAMA - MODAL İÇİNDE */}
                    {form.blockType === "manual" && (
                      <div className="col-12 mt-4">
                        <div className="card border-info">
                          <div className="card-header bg-info text-white">
                            <h5 className="mb-0">
                              <i className="fas fa-boxes me-2"></i>
                              Ürün Seçimi
                            </h5>
                          </div>
                          <div className="card-body">
                            <div className="alert alert-info">
                              <i className="fas fa-info-circle me-2"></i>
                              <strong>Önce bloğu kaydedin!</strong> Kaydetme
                              sonrası bu alan aktif olacak ve ürün
                              arayabileceksiniz.
                            </div>

                            {form.id && (
                              <button
                                type="button"
                                className="btn btn-info btn-lg w-100"
                                onClick={() => {
                                  closeModal();
                                  setTimeout(
                                    () => openProductModal(form.id),
                                    300,
                                  );
                                }}
                              >
                                <i className="fas fa-search me-2"></i>
                                Ürün Ara ve Seç
                              </button>
                            )}
                          </div>
                        </div>
                      </div>
                    )}
                  </div>
                </div>
              </form>
              <div className="modal-footer">
                <button
                  type="button"
                  className="btn btn-secondary"
                  onClick={closeModal}
                >
                  İptal
                </button>
                <button
                  type="submit"
                  form="home-block-form"
                  className="btn btn-primary"
                  disabled={saving}
                >
                  {saving ? (
                    <>
                      <span className="spinner-border spinner-border-sm me-2"></span>
                      Kaydediliyor...
                    </>
                  ) : (
                    <>
                      <i className="fas fa-save me-2"></i>
                      Kaydet
                    </>
                  )}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Ürün Seçimi Modal - GELİŞMİŞ ARAMA */}
      {showProductModal && (
        <div
          className="modal fade show d-block"
          style={{ backgroundColor: "rgba(0,0,0,0.5)" }}
        >
          <div className="modal-dialog modal-xl modal-dialog-centered modal-dialog-scrollable">
            <div className="modal-content">
              <div className="modal-header bg-info text-white">
                <h5 className="modal-title">
                  <i className="fas fa-boxes me-2"></i>
                  Ürün Seçimi ({selectedProducts.length} seçili)
                </h5>
                <button
                  type="button"
                  className="btn-close btn-close-white"
                  onClick={() => {
                    setShowProductModal(false);
                    setProductSearch("");
                    setProductIdInput("");
                    setSearchMode("name");
                  }}
                ></button>
              </div>
              <div className="modal-body">
                {/* =====================================================
                    GELİŞMİŞ ARAMA ALANI - ID veya İsim ile
                    ===================================================== */}
                <div className="card border-0 shadow-sm mb-3">
                  <div className="card-body p-3">
                    {/* Arama Modu Seçimi */}
                    <div className="btn-group mb-3 w-100" role="group">
                      <button
                        type="button"
                        className={`btn ${searchMode === "name" ? "btn-primary" : "btn-outline-primary"}`}
                        onClick={() => {
                          setSearchMode("name");
                          setProductIdInput("");
                        }}
                      >
                        <i className="fas fa-font me-2"></i>
                        İsim ile Ara
                      </button>
                      <button
                        type="button"
                        className={`btn ${searchMode === "id" ? "btn-primary" : "btn-outline-primary"}`}
                        onClick={() => {
                          setSearchMode("id");
                          setProductSearch("");
                        }}
                      >
                        <i className="fas fa-hashtag me-2"></i>
                        ID ile Ara
                      </button>
                    </div>

                    {/* İsim ile Arama - Backend bağlantılı */}
                    {searchMode === "name" && (
                      <div className="row g-2">
                        <div className="col">
                          <div className="input-group">
                            <span className="input-group-text bg-white">
                              {productSearchLoading ? (
                                <span className="spinner-border spinner-border-sm text-primary"></span>
                              ) : (
                                <i className="fas fa-search text-muted"></i>
                              )}
                            </span>
                            <input
                              type="text"
                              className="form-control form-control-lg"
                              placeholder="Ürün adı yazın (min 2 karakter, otomatik aranır)..."
                              value={productSearch}
                              onChange={(e) => setProductSearch(e.target.value)}
                              autoFocus
                            />
                            {productSearch && (
                              <button
                                type="button"
                                className="btn btn-outline-secondary"
                                onClick={() => setProductSearch("")}
                                title="Aramayı Temizle"
                              >
                                <i className="fas fa-times"></i>
                              </button>
                            )}
                          </div>
                          {productSearch.trim().length === 1 && (
                            <small className="text-warning mt-1 d-block">
                              <i className="fas fa-info-circle me-1"></i>
                              Arama için en az 2 karakter yazın
                            </small>
                          )}
                        </div>
                      </div>
                    )}

                    {/* ID ile Arama ve Ekleme */}
                    {searchMode === "id" && (
                      <div className="row g-2">
                        <div className="col">
                          <div className="input-group">
                            <span className="input-group-text bg-white">
                              <i className="fas fa-hashtag text-muted"></i>
                            </span>
                            <input
                              type="number"
                              className="form-control form-control-lg"
                              placeholder="Ürün ID'si girin (örn: 123)"
                              value={productIdInput}
                              onChange={(e) =>
                                setProductIdInput(e.target.value)
                              }
                              onKeyDown={(e) => {
                                if (e.key === "Enter") {
                                  e.preventDefault();
                                  handleAddProductById();
                                }
                              }}
                              autoFocus
                            />
                          </div>
                        </div>
                        <div className="col-auto">
                          <button
                            type="button"
                            className="btn btn-success btn-lg"
                            onClick={handleAddProductById}
                            title="ID ile Ekle"
                          >
                            <i className="fas fa-plus me-1"></i>
                            Ekle
                          </button>
                        </div>
                      </div>
                    )}

                    {/* Arama Sonuç Bilgisi */}
                    <div className="mt-2 d-flex justify-content-between align-items-center">
                      <small className="text-muted">
                        {productSearchLoading ? (
                          <>
                            <span className="spinner-border spinner-border-sm me-1"></span>
                            Aranıyor...
                          </>
                        ) : (
                          <>
                            {filteredProducts.length} ürün bulundu
                            {productSearch && (
                              <span className="ms-2 text-primary">
                                ("{productSearch}" araması)
                              </span>
                            )}
                          </>
                        )}
                      </small>
                      {selectedProducts.length > 0 && (
                        <button
                          type="button"
                          className="btn btn-sm btn-outline-danger"
                          onClick={() => setSelectedProducts([])}
                        >
                          <i className="fas fa-trash me-1"></i>
                          Tümünü Kaldır
                        </button>
                      )}
                    </div>
                  </div>
                </div>

                {/* Seçili Ürünler Önizlemesi */}
                {selectedProducts.length > 0 && (
                  <div className="alert alert-info py-2 mb-3">
                    <div className="d-flex align-items-center flex-wrap gap-2">
                      <strong className="me-2">
                        <i className="fas fa-check-circle me-1"></i>
                        Seçili ({selectedProducts.length}):
                      </strong>
                      {selectedProducts.slice(0, 5).map((id) => {
                        const product = products.find((p) => p.id === id);
                        return product ? (
                          <span
                            key={id}
                            className="badge bg-primary d-flex align-items-center gap-1"
                            style={{ fontSize: "0.8rem" }}
                          >
                            #{id} - {product.name?.substring(0, 20)}...
                            <button
                              type="button"
                              className="btn-close btn-close-white"
                              style={{ fontSize: "0.5rem" }}
                              onClick={() => toggleProductSelection(id)}
                            ></button>
                          </span>
                        ) : null;
                      })}
                      {selectedProducts.length > 5 && (
                        <span className="badge bg-secondary">
                          +{selectedProducts.length - 5} daha
                        </span>
                      )}
                    </div>
                  </div>
                )}

                {/* Ürün Grid */}
                <div
                  className="row g-2"
                  style={{ maxHeight: "400px", overflowY: "auto" }}
                >
                  {productSearchLoading ? (
                    <div className="col-12 text-center py-5">
                      <div
                        className="spinner-border text-primary mb-3"
                        role="status"
                      >
                        <span className="visually-hidden">Yükleniyor...</span>
                      </div>
                      <h5 className="text-muted">Ürünler aranıyor...</h5>
                    </div>
                  ) : filteredProducts.length === 0 ? (
                    <div className="col-12 text-center py-5">
                      <i className="fas fa-search fa-3x text-muted mb-3"></i>
                      <h5 className="text-muted">Ürün bulunamadı</h5>
                      <p className="text-muted small">
                        {searchMode === "id"
                          ? "Girdiğiniz ID ile eşleşen ürün yok"
                          : "Arama kriterlerinizi değiştirmeyi deneyin"}
                      </p>
                    </div>
                  ) : (
                    filteredProducts.map((product) => (
                      <div key={product.id} className="col-6 col-md-4 col-lg-3">
                        <div
                          className={`card h-100 ${selectedProducts.includes(product.id) ? "border-primary border-2 bg-light" : "border"}`}
                          style={{ cursor: "pointer", transition: "all 0.2s" }}
                          onClick={() => toggleProductSelection(product.id)}
                        >
                          <div className="position-relative">
                            <img
                              src={product.imageUrl || "/placeholder.png"}
                              alt={product.name}
                              className="card-img-top"
                              style={{
                                height: "100px",
                                objectFit: "contain",
                                padding: "8px",
                              }}
                            />
                            {selectedProducts.includes(product.id) && (
                              <div className="position-absolute top-0 end-0 m-1">
                                <span className="badge bg-primary rounded-circle p-2">
                                  <i className="fas fa-check"></i>
                                </span>
                              </div>
                            )}
                            {/* Ürün ID Badge */}
                            <div className="position-absolute top-0 start-0 m-1">
                              <span className="badge bg-dark opacity-75">
                                #{product.id}
                              </span>
                            </div>
                          </div>
                          <div className="card-body p-2">
                            <p className="card-text small mb-1 text-truncate fw-semibold">
                              {product.name}
                            </p>
                            <div className="d-flex justify-content-between align-items-center">
                              <small className="text-primary fw-bold">
                                {(
                                  product.specialPrice || product.price
                                )?.toFixed(2)}
                                ₺
                              </small>
                              {product.specialPrice &&
                                product.price > product.specialPrice && (
                                  <small className="text-muted text-decoration-line-through">
                                    {product.price?.toFixed(2)}₺
                                  </small>
                                )}
                            </div>
                          </div>
                        </div>
                      </div>
                    ))
                  )}
                </div>
              </div>
              <div className="modal-footer">
                <button
                  type="button"
                  className="btn btn-secondary"
                  onClick={() => {
                    setShowProductModal(false);
                    setProductSearch("");
                    setProductIdInput("");
                    setSearchMode("name");
                  }}
                >
                  İptal
                </button>
                <button
                  type="button"
                  className="btn btn-primary"
                  onClick={saveProducts}
                  disabled={selectedProducts.length === 0}
                >
                  <i className="fas fa-save me-2"></i>
                  Kaydet ({selectedProducts.length} ürün)
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
