import React, { useEffect, useMemo, useState, useCallback } from "react";
import DOMPurify from "dompurify";
import { AdminService } from "../../services/adminService";
import api from "../../services/api";
import { sanitizeInput, sanitizeNumber } from "../../utils/inputSanitizer";

// =============================================================================
// Kampanya Yönetimi - Admin Panel
// =============================================================================
// Bu component, gelişmiş kampanya yönetim sistemini sağlar.
// Desteklenen kampanya türleri:
// - Percentage: Yüzdelik indirim (%10, %20 vb.)
// - FixedAmount: Sabit tutar indirim (50 TL, 100 TL vb.)
// - BuyXPayY: X al Y öde (3 al 2 öde vb.)
// - FreeShipping: Ücretsiz kargo
//
// Kampanya hedefleri:
// - All: Tüm ürünler
// - Category: Belirli kategoriler
// - Product: Belirli ürünler
// =============================================================================

// Kampanya türleri - Backend enum ile uyumlu
const CAMPAIGN_TYPES = {
  Percentage: {
    value: 0,
    label: "Yüzde İndirim",
    icon: "fa-percent",
    color: "danger",
  },
  FixedAmount: {
    value: 1,
    label: "Sabit Tutar İndirim",
    icon: "fa-tag",
    color: "warning",
  },
  BuyXPayY: {
    value: 2,
    label: "X Al Y Öde",
    icon: "fa-shopping-basket",
    color: "success",
  },
  FreeShipping: {
    value: 3,
    label: "Ücretsiz Kargo",
    icon: "fa-truck",
    color: "info",
  },
};

// Hedef türleri - Backend enum ile uyumlu
const TARGET_TYPES = {
  All: { value: 0, label: "Tüm Ürünler", icon: "fa-globe" },
  Category: { value: 1, label: "Kategori Bazlı", icon: "fa-folder" },
  Product: { value: 2, label: "Ürün Bazlı", icon: "fa-box" },
};

// Form başlangıç değerleri
const initialForm = {
  id: 0,
  name: "",
  description: "",
  startDate: "",
  endDate: "",
  isActive: true,
  // Yeni alanlar
  type: 0, // Percentage
  targetType: 0, // All
  discountValue: "",
  maxDiscountAmount: "",
  minCartTotal: "",
  minQuantity: "",
  buyQty: "3",
  payQty: "2",
  priority: "100",
  isStackable: true,
  targetIds: [],
  // Kampanya görseli (opsiyonel)
  imageUrl: null,
  // Geriye dönük uyumluluk
  conditionJson: "",
  rewardType: "Percent",
  rewardValue: "",
};

// Tarih formatları
const toInputDate = (value) => {
  if (!value) return "";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "";
  return date.toISOString().split("T")[0];
};

const formatDate = (value) => {
  if (!value) return "-";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "-";
  return date.toLocaleDateString("tr-TR");
};

// Kampanya türü adını getir
const getCampaignTypeName = (type) => {
  const typeNum = typeof type === "string" ? parseInt(type) : type;
  const entry = Object.entries(CAMPAIGN_TYPES).find(
    ([_, v]) => v.value === typeNum,
  );
  return entry ? entry[1].label : "Bilinmiyor";
};

// Hedef türü adını getir
const getTargetTypeName = (targetType) => {
  const typeNum =
    typeof targetType === "string" ? parseInt(targetType) : targetType;
  const entry = Object.entries(TARGET_TYPES).find(
    ([_, v]) => v.value === typeNum,
  );
  return entry ? entry[1].label : "Bilinmiyor";
};

// Kampanya türü badge rengi
const getCampaignTypeColor = (type) => {
  const typeNum = typeof type === "string" ? parseInt(type) : type;
  const entry = Object.entries(CAMPAIGN_TYPES).find(
    ([_, v]) => v.value === typeNum,
  );
  return entry ? entry[1].color : "secondary";
};

export default function AdminCampaigns() {
  // =========================================================================
  // STATE TANIMLAMALARI
  // =========================================================================

  // Kampanya listesi state'leri
  const [campaigns, setCampaigns] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [search, setSearch] = useState("");
  const [message, setMessage] = useState("");
  const [messageType, setMessageType] = useState("success");
  const [showModal, setShowModal] = useState(false);
  const [modalLoading, setModalLoading] = useState(false);
  const [editingId, setEditingId] = useState(null);
  const [form, setForm] = useState(initialForm);
  const [submitted, setSubmitted] = useState(false);

  // Ürün ve kategori listeleri (hedef seçimi için)
  const [products, setProducts] = useState([]);
  const [categories, setCategories] = useState([]);
  const [productsLoading, setProductsLoading] = useState(false);

  // Kampanya görseli state'leri (opsiyonel görsel yükleme)
  const [imageFile, setImageFile] = useState(null);
  const [imagePreview, setImagePreview] = useState(null);
  const [uploadingImage, setUploadingImage] = useState(false);

  // =========================================================================
  // KAMPANYA ÖNİZLEME STATE'LERİ
  // =========================================================================
  const [showPreviewModal, setShowPreviewModal] = useState(false);
  const [previewLoading, setPreviewLoading] = useState(false);
  const [previewData, setPreviewData] = useState(null);
  const [previewError, setPreviewError] = useState("");

  // =========================================================================
  // GELİŞMİŞ ÜRÜN ARAMA SİSTEMİ STATE'LERİ
  // =========================================================================

  // Ürün arama inputu (debounced search için)
  const [productSearchQuery, setProductSearchQuery] = useState("");
  // Debounce için timeout ref
  const [productSearchDebounce, setProductSearchDebounce] = useState(null);
  // Sayfalama state'leri
  const [productPage, setProductPage] = useState(1);
  const PRODUCTS_PER_PAGE = 20;

  // Kampanyaları yükle
  const loadCampaigns = useCallback(async () => {
    try {
      setLoading(true);
      setError("");
      const data = await AdminService.getCampaigns();
      setCampaigns(Array.isArray(data) ? data : []);
    } catch (err) {
      setError(err.message || "Kampanyalar yüklenemedi.");
      console.error("Kampanya yükleme hatası:", err);
    } finally {
      setLoading(false);
    }
  }, []);

  // Ürün ve kategorileri yükle
  const loadProductsAndCategories = useCallback(async () => {
    try {
      setProductsLoading(true);
      const [productsData, categoriesData] = await Promise.all([
        AdminService.getCampaignProducts?.() ||
          AdminService.getProducts?.() ||
          [],
        AdminService.getCampaignCategories?.() ||
          AdminService.getCategories?.() ||
          [],
      ]);
      setProducts(Array.isArray(productsData) ? productsData : []);
      setCategories(Array.isArray(categoriesData) ? categoriesData : []);
    } catch (err) {
      console.error("Ürün/kategori yükleme hatası:", err);
    } finally {
      setProductsLoading(false);
    }
  }, []);

  useEffect(() => {
    loadCampaigns();
    loadProductsAndCategories();
  }, [loadCampaigns, loadProductsAndCategories]);

  // Filtrelenmiş kampanyalar
  const filteredCampaigns = useMemo(() => {
    const query = search.trim().toLowerCase();
    if (!query) return campaigns;
    return campaigns.filter((campaign) => {
      if (campaign.name?.toLowerCase().includes(query)) return true;
      if (campaign.description?.toLowerCase().includes(query)) return true;
      if (query === "aktif" && campaign.isActive) return true;
      if (query === "pasif" && !campaign.isActive) return true;
      // Tür araması
      const typeName = getCampaignTypeName(campaign.type).toLowerCase();
      if (typeName.includes(query)) return true;
      return false;
    });
  }, [campaigns, search]);

  // =========================================================================
  // GELİŞMİŞ ÜRÜN ARAMA SİSTEMİ
  // =========================================================================

  /**
   * Ürün arama fonksiyonu - ID, İsim, SKU, Kategori ile arama
   * Debounced search (300ms) ile performans optimize edildi
   */
  const filteredProducts = useMemo(() => {
    const query = productSearchQuery.trim().toLowerCase();
    if (!query) return products;

    return products.filter((product) => {
      // ID ile arama (# ile başlıyorsa veya sayı ise)
      if (query.startsWith("#")) {
        const idQuery = query.slice(1);
        return product.id?.toString() === idQuery;
      }
      if (/^\d+$/.test(query)) {
        return product.id?.toString().includes(query);
      }

      // İsim ile arama (Türkçe karakter destekli)
      if (product.name?.toLowerCase().includes(query)) return true;

      // SKU ile arama
      if (product.sku?.toLowerCase().includes(query)) return true;

      // Kategori adı ile arama
      if (product.categoryName?.toLowerCase().includes(query)) return true;

      // Fiyat ile arama (₺ veya TL içeriyorsa)
      if ((query.includes("₺") || query.includes("tl")) && product.price) {
        const priceQuery = query.replace(/[₺tl\s]/gi, "");
        if (!isNaN(priceQuery)) {
          return product.price.toString().includes(priceQuery);
        }
      }

      return false;
    });
  }, [products, productSearchQuery]);

  // Sayfalanmış ürünler (performans için)
  const paginatedProducts = useMemo(() => {
    const startIndex = (productPage - 1) * PRODUCTS_PER_PAGE;
    return filteredProducts.slice(startIndex, startIndex + PRODUCTS_PER_PAGE);
  }, [filteredProducts, productPage]);

  // Toplam sayfa sayısı
  const totalProductPages = useMemo(() => {
    return Math.ceil(filteredProducts.length / PRODUCTS_PER_PAGE);
  }, [filteredProducts]);

  /**
   * Ürün arama input değişikliği - Debounced (300ms)
   */
  const handleProductSearchChange = useCallback(
    (e) => {
      const value = e.target.value;

      // Önceki debounce timeout'unu temizle
      if (productSearchDebounce) {
        clearTimeout(productSearchDebounce);
      }

      // 300ms sonra arama yap (debounce)
      const timeout = setTimeout(() => {
        setProductSearchQuery(value);
        setProductPage(1); // Arama yapıldığında ilk sayfaya dön
      }, 300);

      setProductSearchDebounce(timeout);
    },
    [productSearchDebounce],
  );

  /**
   * Ürün seçme/kaldırma fonksiyonu
   */
  const handleProductToggle = useCallback((productId) => {
    setForm((prev) => {
      const currentIds = prev.targetIds || [];
      const isSelected = currentIds.includes(productId);

      return {
        ...prev,
        targetIds: isSelected
          ? currentIds.filter((id) => id !== productId)
          : [...currentIds, productId],
      };
    });
  }, []);

  /**
   * Tümünü seç fonksiyonu
   */
  const handleSelectAllProducts = useCallback(() => {
    setForm((prev) => ({
      ...prev,
      targetIds: filteredProducts.map((p) => p.id),
    }));
  }, [filteredProducts]);

  /**
   * Tümünü kaldır fonksiyonu
   */
  const handleClearAllProducts = useCallback(() => {
    setForm((prev) => ({
      ...prev,
      targetIds: [],
    }));
  }, []);

  /**
   * Seçili ürün bilgilerini getir (badge gösterimi için)
   */
  const selectedProducts = useMemo(() => {
    return products.filter((p) => form.targetIds?.includes(p.id));
  }, [products, form.targetIds]);

  // =========================================================================
  // KAMPANYA GÖRSELİ FONKSİYONLARI (Opsiyonel)
  // =========================================================================

  /**
   * Görsel dosya seçimi handler - dosya tipi ve boyut validasyonu yapar
   */
  const handleImageChange = (e) => {
    const file = e.target.files[0];
    if (!file) return;

    // Dosya tipi validasyonu
    const validTypes = ["image/jpeg", "image/png", "image/gif", "image/webp"];
    if (!validTypes.includes(file.type)) {
      alert("Desteklenen dosya türleri: JPG, PNG, GIF, WebP");
      return;
    }

    // Dosya boyutu validasyonu (max 10MB)
    if (file.size > 10 * 1024 * 1024) {
      alert("Dosya boyutu maksimum 10MB olabilir.");
      return;
    }

    setImageFile(file);

    // Yerel önizleme oluştur
    const reader = new FileReader();
    reader.onloadend = () => setImagePreview(reader.result);
    reader.readAsDataURL(file);
  };

  /**
   * Seçilen görseli API'ye yükler ve imageUrl döndürür
   * @returns {string|null} Yüklenen görselin URL'si veya null
   */
  const uploadImage = async () => {
    if (!imageFile) return null;

    setUploadingImage(true);
    try {
      const formData = new FormData();
      formData.append("image", imageFile);

      const response = await api.post(
        "/api/admin/campaigns/upload-image",
        formData,
        {
          headers: { "Content-Type": "multipart/form-data" },
        },
      );

      return response?.imageUrl || response?.data?.imageUrl || null;
    } catch (err) {
      console.error("Görsel yükleme hatası:", err);
      alert("Görsel yüklenirken hata oluştu.");
      return null;
    } finally {
      setUploadingImage(false);
    }
  };

  /**
   * Seçilen veya mevcut görseli kaldırır
   */
  const removeImage = () => {
    setImageFile(null);
    setImagePreview(null);
    // Form'daki mevcut imageUrl'yi de temizle (düzenleme durumunda)
    setForm((prev) => ({ ...prev, imageUrl: null }));
  };

  // Modal açma - Yeni kampanya
  function openCreateModal() {
    setEditingId(null);
    setForm(initialForm);
    setImageFile(null);
    setImagePreview(null);
    setProductSearchQuery(""); // Arama sıfırla
    setProductPage(1); // Sayfalama sıfırla
    setShowModal(true);
    setModalLoading(false);
  }

  // Modal açma - Düzenleme
  async function openEditModal(campaign) {
    setEditingId(campaign.id);
    setShowModal(true);
    setModalLoading(true);
    setImageFile(null);
    setImagePreview(null);
    setProductSearchQuery(""); // Arama sıfırla
    setProductPage(1); // Sayfalama sıfırla

    try {
      const detail = await AdminService.getCampaignById(campaign.id);
      setForm({
        id: detail.id,
        name: detail.name || "",
        description: detail.description || "",
        startDate: toInputDate(detail.startDate),
        endDate: toInputDate(detail.endDate),
        isActive: !!detail.isActive,
        // Yeni alanlar
        type: detail.type ?? 0,
        targetType: detail.targetType ?? 0,
        discountValue: detail.discountValue?.toString() || "",
        maxDiscountAmount: detail.maxDiscountAmount?.toString() || "",
        minCartTotal: detail.minCartTotal?.toString() || "",
        minQuantity: detail.minQuantity?.toString() || "",
        buyQty: detail.buyQty?.toString() || "3",
        payQty: detail.payQty?.toString() || "2",
        priority: detail.priority?.toString() || "100",
        isStackable: detail.isStackable ?? true,
        targetIds: detail.targets?.map((t) => t.targetId) || [],
        // Kampanya görseli (opsiyonel)
        imageUrl: detail.imageUrl || null,
        // Geriye dönük uyumluluk
        conditionJson: detail.conditionJson || "",
        rewardType: detail.rewardType || "Percent",
        rewardValue: detail.rewardValue?.toString() || "",
      });
    } catch (err) {
      setMessage(err.message || "Kampanya bilgileri yüklenemedi.");
      setMessageType("danger");
      setShowModal(false);
    } finally {
      setModalLoading(false);
    }
  }

  // Modal kapatma
  function closeModal() {
    setShowModal(false);
    setModalLoading(false);
    setEditingId(null);
    setForm(initialForm);
    setImageFile(null);
    setImagePreview(null);
    setProductSearchQuery(""); // Arama sıfırla
    setProductPage(1); // Sayfalama sıfırla
    setSubmitted(false); // Validation state'ini sıfırla
  }

  // =========================================================================
  // KAMPANYA ÖNİZLEME FONKSİYONLARI
  // =========================================================================

  /**
   * Mevcut bir kampanyanın önizlemesini gösterir (ID ile)
   */
  async function handlePreviewCampaign(campaignId) {
    try {
      setPreviewLoading(true);
      setPreviewError("");
      setShowPreviewModal(true);

      const response = await AdminService.previewCampaign(campaignId);
      setPreviewData(response);
    } catch (err) {
      setPreviewError(err.message || "Önizleme yüklenemedi.");
      console.error("Önizleme hatası:", err);
    } finally {
      setPreviewLoading(false);
    }
  }

  /**
   * Form verilerine göre önizleme yapar (henüz kaydedilmemiş kampanya)
   */
  async function handlePreviewFormData() {
    try {
      setPreviewLoading(true);
      setPreviewError("");
      setShowPreviewModal(true);

      // Form verilerini API formatına çevir
      const previewDto = {
        name: form.name,
        description: form.description,
        startDate: form.startDate,
        endDate: form.endDate,
        isActive: form.isActive,
        type: parseInt(form.type),
        targetType: parseInt(form.targetType),
        discountValue: parseFloat(form.discountValue) || 0,
        maxDiscountAmount: form.maxDiscountAmount
          ? parseFloat(form.maxDiscountAmount)
          : null,
        targetIds: form.targetIds || [],
      };

      const response = await AdminService.previewCampaignData(previewDto);
      setPreviewData(response);
    } catch (err) {
      setPreviewError(err.message || "Önizleme yüklenemedi.");
      console.error("Önizleme hatası:", err);
    } finally {
      setPreviewLoading(false);
    }
  }

  /**
   * Önizleme modalını kapatır
   */
  function closePreviewModal() {
    setShowPreviewModal(false);
    setPreviewData(null);
    setPreviewError("");
  }

  // Form değişikliği
  const onFormChange = (event) => {
    const { name, value, type, checked } = event.target;
    setForm((prev) => ({
      ...prev,
      [name]: type === "checkbox" ? checked : value,
    }));
  };

  // Hedef ID'leri değişikliği (multi-select)
  const onTargetIdsChange = (selectedIds) => {
    setForm((prev) => ({
      ...prev,
      targetIds: selectedIds,
    }));
  };

  // Payload oluştur
  function buildPayload() {
    if (!form.startDate || !form.endDate) {
      throw new Error("Başlangıç ve bitiş tarihleri zorunludur.");
    }

    const typeNum = parseInt(form.type);
    const targetTypeNum = parseInt(form.targetType);

    // Kampanya türüne göre değer validasyonu
    let discountValue = 0;
    if (typeNum === 0) {
      // Percentage
      discountValue = parseFloat(form.discountValue) || 0;
      if (discountValue <= 0 || discountValue > 100) {
        throw new Error("Yüzde indirim 1-100 arasında olmalıdır.");
      }
    } else if (typeNum === 1) {
      // FixedAmount
      discountValue = parseFloat(form.discountValue) || 0;
      if (discountValue <= 0) {
        throw new Error("İndirim tutarı 0'dan büyük olmalıdır.");
      }
    } else if (typeNum === 2) {
      // BuyXPayY
      const buyQty = parseInt(form.buyQty) || 0;
      const payQty = parseInt(form.payQty) || 0;
      if (buyQty < 2 || payQty < 1 || payQty >= buyQty) {
        throw new Error(
          "X Al Y Öde için geçerli değerler giriniz (X > Y >= 1).",
        );
      }
    }

    // Hedef tipi kontrolü
    if (
      targetTypeNum !== 0 &&
      (!form.targetIds || form.targetIds.length === 0)
    ) {
      throw new Error(
        "Kategori veya ürün bazlı kampanyalar için en az bir hedef seçilmelidir.",
      );
    }

    return {
      // String alanlar - XSS korumalı sanitization
      name: sanitizeInput(form.name || "", {
        preventXSS: true,
        normalizeWhitespace: true,
        maxLength: 200,
      }),
      description: form.description
        ? sanitizeInput(form.description, {
            preventXSS: true,
            maxLength: 1000,
          })
        : null,

      // Tarihler
      startDate: new Date(`${form.startDate}T00:00:00`),
      endDate: new Date(`${form.endDate}T23:59:59`),
      isActive: !!form.isActive,

      // Yeni alanlar - Sayısal değerler sanitize edilmiş
      type: typeNum,
      targetType: targetTypeNum,
      discountValue: sanitizeNumber(discountValue, {
        min: 0,
        max: typeNum === 0 ? 100 : 1000000,
        decimals: 2,
      }),
      maxDiscountAmount: form.maxDiscountAmount
        ? sanitizeNumber(form.maxDiscountAmount, {
            min: 0,
            max: 1000000,
            decimals: 2,
          })
        : null,
      minCartTotal: form.minCartTotal
        ? sanitizeNumber(form.minCartTotal, {
            min: 0,
            max: 1000000,
            decimals: 2,
          })
        : null,
      minQuantity: form.minQuantity
        ? sanitizeNumber(form.minQuantity, {
            min: 1,
            max: 10000,
            decimals: 0,
          })
        : null,
      buyQty:
        typeNum === 2
          ? sanitizeNumber(form.buyQty, { min: 2, max: 100, decimals: 0 })
          : null,
      payQty:
        typeNum === 2
          ? sanitizeNumber(form.payQty, { min: 1, max: 99, decimals: 0 })
          : null,
      priority: sanitizeNumber(form.priority, {
        min: 1,
        max: 1000,
        decimals: 0,
        default: 100,
      }),
      isStackable: !!form.isStackable,
      targetIds: targetTypeNum !== 0 ? form.targetIds : null,

      // Geriye dönük uyumluluk
      conditionJson: form.conditionJson
        ? sanitizeInput(form.conditionJson, {
            preventXSS: true,
            maxLength: 5000,
          })
        : null,
      rewardType: form.rewardType || "Percent",
      rewardValue: discountValue,

      // Kampanya görseli (opsiyonel) - URL sanitization
      imageUrl: form.imageUrl || null,
    };
  }

  // Form gönderimi
  async function handleSubmit(event) {
    event.preventDefault();
    setSubmitted(true);
    try {
      if (!form.name.trim()) {
        throw new Error("Kampanya adı zorunludur.");
      }
      const payload = buildPayload();

      // Opsiyonel görsel yükleme - dosya seçildiyse yükle
      if (imageFile) {
        const uploadedUrl = await uploadImage();
        if (uploadedUrl) {
          payload.imageUrl = uploadedUrl;
        }
        // Yükleme başarısız olsa bile kampanya kaydedilsin (görsel opsiyonel)
      }

      if (editingId) {
        await AdminService.updateCampaign(editingId, payload);
        setMessage("Kampanya başarıyla güncellendi.");
      } else {
        await AdminService.createCampaign(payload);
        setMessage("Kampanya başarıyla oluşturuldu.");
      }
      setMessageType("success");
      await loadCampaigns();
      closeModal();
    } catch (err) {
      setMessage(err.message || "İşlem gerçekleştirilemedi.");
      setMessageType("danger");
    } finally {
      setTimeout(() => setMessage(""), 4000);
    }
  }

  // Kampanya silme
  async function handleDelete(id) {
    const confirmed = window.confirm(
      "Bu kampanyayı silmek istediğinize emin misiniz?\nBu işlem geri alınamaz.",
    );
    if (!confirmed) return;

    try {
      await AdminService.deleteCampaign(id);
      setMessage("Kampanya başarıyla silindi.");
      setMessageType("success");
      await loadCampaigns();
    } catch (err) {
      setMessage(err.message || "Kampanya silinemedi.");
      setMessageType("danger");
    } finally {
      setTimeout(() => setMessage(""), 3000);
    }
  }

  // Kampanya durumunu değiştir
  async function handleToggleStatus(id) {
    try {
      await AdminService.toggleCampaignStatus?.(id);
      setMessage("Kampanya durumu güncellendi.");
      setMessageType("success");
      await loadCampaigns();
    } catch (err) {
      setMessage(err.message || "Durum güncellenemedi.");
      setMessageType("danger");
    } finally {
      setTimeout(() => setMessage(""), 3000);
    }
  }

  // İstatistikler
  const totalCount = campaigns.length;
  const activeCount = campaigns.filter((c) => c.isActive).length;

  // Kampanya türüne göre indirim gösterimi
  const getDiscountDisplay = (campaign) => {
    const type =
      typeof campaign.type === "string"
        ? parseInt(campaign.type)
        : campaign.type;
    switch (type) {
      case 0: // Percentage
        return `%${campaign.discountValue || 0}`;
      case 1: // FixedAmount
        return `₺${campaign.discountValue || 0}`;
      case 2: // BuyXPayY
        return `${campaign.buyQty || 3} Al ${campaign.payQty || 2} Öde`;
      case 3: // FreeShipping
        return "Ücretsiz Kargo";
      default:
        return "-";
    }
  };

  return (
    <div
      className="container-fluid p-2 p-md-4"
      style={{ overflow: "hidden", maxWidth: "100%" }}
    >
      {/* Header - Responsive */}
      <div className="d-flex flex-column flex-md-row justify-content-between align-items-start align-items-md-center mb-3 gap-2">
        <div className="mb-2 mb-md-0">
          <h1 className="h4 h3-md fw-bold mb-1" style={{ color: "#2d3748" }}>
            <i className="fas fa-gift me-2" style={{ color: "#f57c00" }}></i>
            Kampanya Yönetimi
          </h1>
          <p
            className="text-muted mb-0 d-none d-sm-block"
            style={{ fontSize: "0.8rem" }}
          >
            İndirim kampanyalarını oluşturun ve yönetin
          </p>
        </div>
        <div className="d-flex flex-column flex-sm-row gap-2 w-100 w-md-auto">
          <input
            type="text"
            className="form-control form-control-sm"
            placeholder="Ara: ad, tür, aktif/pasif"
            style={{ minWidth: "150px", minHeight: "44px" }}
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
          <div className="d-flex gap-2">
            <button
              className="btn btn-sm btn-outline-secondary"
              style={{ minHeight: "44px", minWidth: "44px" }}
              onClick={loadCampaigns}
              disabled={loading}
              title="Yenile"
            >
              <i className={`fas fa-sync-alt ${loading ? "fa-spin" : ""}`}></i>
            </button>
            <button
              className="btn btn-sm text-white fw-semibold flex-grow-1"
              style={{
                background: "linear-gradient(135deg, #f57c00, #ff9800)",
                minHeight: "44px",
                whiteSpace: "nowrap",
              }}
              onClick={openCreateModal}
            >
              <i className="fas fa-plus me-1"></i>Yeni Kampanya
            </button>
          </div>
        </div>
      </div>

      {/* Mesajlar */}
      {error && (
        <div
          className="alert alert-danger py-2 d-flex align-items-center"
          style={{ fontSize: "0.85rem" }}
        >
          <i className="fas fa-exclamation-circle me-2"></i>
          {error}
        </div>
      )}
      {message && (
        <div
          className={`alert alert-${messageType} py-2 d-flex align-items-center`}
          style={{ fontSize: "0.85rem" }}
        >
          <i
            className={`fas ${messageType === "success" ? "fa-check-circle" : "fa-exclamation-triangle"} me-2`}
          ></i>
          {message}
        </div>
      )}

      {/* İstatistik Kartları - Mobilde gizli */}
      <div className="row g-2 mb-3 d-none d-md-flex">
        <div className="col-md-3">
          <div
            className="card border-0 shadow-sm h-100"
            style={{ borderRadius: "10px" }}
          >
            <div className="card-body py-2 px-3">
              <div className="d-flex align-items-center">
                <div
                  className="rounded-circle p-2 me-2"
                  style={{ backgroundColor: "#e3f2fd" }}
                >
                  <i className="fas fa-gift text-primary"></i>
                </div>
                <div>
                  <div className="text-muted" style={{ fontSize: "0.75rem" }}>
                    Toplam
                  </div>
                  <div className="fw-bold">{totalCount}</div>
                </div>
              </div>
            </div>
          </div>
        </div>
        <div className="col-md-3">
          <div
            className="card border-0 shadow-sm h-100"
            style={{ borderRadius: "10px" }}
          >
            <div className="card-body py-2 px-3">
              <div className="d-flex align-items-center">
                <div
                  className="rounded-circle p-2 me-2"
                  style={{ backgroundColor: "#e8f5e9" }}
                >
                  <i className="fas fa-check-circle text-success"></i>
                </div>
                <div>
                  <div className="text-muted" style={{ fontSize: "0.75rem" }}>
                    Aktif
                  </div>
                  <div className="fw-bold text-success">{activeCount}</div>
                </div>
              </div>
            </div>
          </div>
        </div>
        <div className="col-md-3">
          <div
            className="card border-0 shadow-sm h-100"
            style={{ borderRadius: "10px" }}
          >
            <div className="card-body py-2 px-3">
              <div className="d-flex align-items-center">
                <div
                  className="rounded-circle p-2 me-2"
                  style={{ backgroundColor: "#fff3e0" }}
                >
                  <i className="fas fa-pause-circle text-warning"></i>
                </div>
                <div>
                  <div className="text-muted" style={{ fontSize: "0.75rem" }}>
                    Pasif
                  </div>
                  <div className="fw-bold text-warning">
                    {totalCount - activeCount}
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
        <div className="col-md-3">
          <div
            className="card border-0 shadow-sm h-100"
            style={{ borderRadius: "10px" }}
          >
            <div className="card-body py-2 px-3">
              <div className="d-flex align-items-center">
                <div
                  className="rounded-circle p-2 me-2"
                  style={{ backgroundColor: "#fce4ec" }}
                >
                  <i className="fas fa-percent text-danger"></i>
                </div>
                <div>
                  <div className="text-muted" style={{ fontSize: "0.75rem" }}>
                    Sonuç
                  </div>
                  <div className="fw-bold">{filteredCampaigns.length}</div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Kampanya Listesi */}
      <div className="card border-0 shadow-sm" style={{ borderRadius: "10px" }}>
        <div className="card-header bg-white py-2 px-3 d-flex justify-content-between align-items-center">
          <span style={{ fontSize: "0.9rem" }}>
            <i className="fas fa-layer-group me-2 text-primary"></i>Kampanyalar
          </span>
          <small className="text-muted" style={{ fontSize: "0.75rem" }}>
            Toplam: {totalCount} · Aktif: {activeCount}
          </small>
        </div>
        <div className="card-body p-2 p-md-3">
          {loading ? (
            <div className="text-center py-4">
              <div className="spinner-border text-primary" role="status">
                <span className="visually-hidden">Yükleniyor...</span>
              </div>
              <div className="text-muted mt-2" style={{ fontSize: "0.85rem" }}>
                Kampanyalar yükleniyor...
              </div>
            </div>
          ) : (
            <div className="table-responsive">
              <table
                className="table table-sm table-hover mb-0 admin-mobile-table"
                style={{ fontSize: "0.8rem" }}
              >
                <thead className="bg-light">
                  <tr>
                    <th className="px-2">ID</th>
                    <th className="px-2">Ad</th>
                    <th className="px-2">Tür</th>
                    <th className="px-2 d-none d-lg-table-cell">Hedef</th>
                    <th className="px-2 d-none d-md-table-cell">İndirim</th>
                    <th className="px-2 d-none d-lg-table-cell">Tarih</th>
                    <th className="px-2">Durum</th>
                    <th className="px-2 text-end">İşlem</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredCampaigns.length ? (
                    filteredCampaigns.map((c) => (
                      <tr key={c.id}>
                        <td data-label="ID" className="px-2">
                          <span className="badge bg-light text-dark">
                            #{c.id}
                          </span>
                        </td>
                        <td
                          data-label="Ad"
                          className="px-2 fw-semibold"
                          style={{ maxWidth: "200px" }}
                        >
                          <div className="d-flex align-items-center gap-2">
                            {c.imageUrl && (
                              <img
                                src={c.imageUrl}
                                alt=""
                                style={{
                                  width: "36px",
                                  height: "36px",
                                  objectFit: "cover",
                                  borderRadius: "6px",
                                  border: "1px solid #e5e7eb",
                                  flexShrink: 0,
                                }}
                                onError={(e) => {
                                  e.target.style.display = "none";
                                }}
                              />
                            )}
                            <div>
                              <div className="text-truncate" title={c.name}>
                                {DOMPurify.sanitize(c.name, {
                                  ALLOWED_TAGS: [],
                                  KEEP_CONTENT: true,
                                })}
                              </div>
                              {c.description && (
                                <small
                                  className="text-muted d-block text-truncate"
                                  style={{ fontSize: "0.7rem" }}
                                >
                                  {DOMPurify.sanitize(c.description, {
                                    ALLOWED_TAGS: [],
                                    KEEP_CONTENT: true,
                                  })}
                                </small>
                              )}
                            </div>
                          </div>
                        </td>
                        <td data-label="Tür" className="px-2">
                          <span
                            className={`badge bg-${getCampaignTypeColor(c.type)}`}
                            style={{ fontSize: "0.7rem" }}
                          >
                            {getCampaignTypeName(c.type)}
                          </span>
                        </td>
                        <td
                          data-label="Hedef"
                          className="px-2 d-none d-lg-table-cell"
                        >
                          <span
                            className="text-muted"
                            style={{ fontSize: "0.75rem" }}
                          >
                            {getTargetTypeName(c.targetType)}
                          </span>
                        </td>
                        <td
                          data-label="İndirim"
                          className="px-2 d-none d-md-table-cell fw-semibold"
                        >
                          {getDiscountDisplay(c)}
                        </td>
                        <td
                          data-label="Tarih"
                          className="px-2 d-none d-lg-table-cell"
                          style={{ fontSize: "0.75rem" }}
                        >
                          <div>{formatDate(c.startDate)}</div>
                          <div className="text-muted">
                            {formatDate(c.endDate)}
                          </div>
                        </td>
                        <td data-label="Durum" className="px-2">
                          <button
                            type="button"
                            className={`btn btn-sm badge border-0 ${c.isActive ? "bg-success" : "bg-secondary"}`}
                            style={{ fontSize: "0.7rem", cursor: "pointer" }}
                            onClick={() => handleToggleStatus(c.id)}
                            onKeyDown={(e) => {
                              if (e.key === "Enter" || e.key === " ") {
                                e.preventDefault();
                                handleToggleStatus(c.id);
                              }
                            }}
                            aria-label={`Kampanya durumunu ${c.isActive ? "pasif" : "aktif"} yap`}
                            aria-pressed={c.isActive}
                            title="Durumu değiştirmek için tıklayın"
                          >
                            {c.isActive ? "Aktif" : "Pasif"}
                          </button>
                        </td>
                        <td data-label="İşlem" className="px-2">
                          <div className="d-flex gap-1 justify-content-end">
                            <button
                              className="btn btn-outline-info btn-sm"
                              style={{ minWidth: "36px", minHeight: "36px" }}
                              onClick={() => handlePreviewCampaign(c.id)}
                              title="Önizle"
                            >
                              <i className="fas fa-eye"></i>
                            </button>
                            <button
                              className="btn btn-outline-primary btn-sm"
                              style={{ minWidth: "36px", minHeight: "36px" }}
                              onClick={() => openEditModal(c)}
                              title="Düzenle"
                            >
                              <i className="fas fa-edit"></i>
                            </button>
                            <button
                              className="btn btn-outline-danger btn-sm"
                              style={{ minWidth: "36px", minHeight: "36px" }}
                              onClick={() => handleDelete(c.id)}
                              title="Sil"
                            >
                              <i className="fas fa-trash"></i>
                            </button>
                          </div>
                        </td>
                      </tr>
                    ))
                  ) : (
                    <tr>
                      <td colSpan="8" className="text-muted text-center py-4">
                        <i className="fas fa-inbox fa-2x mb-2 d-block text-secondary"></i>
                        Kayıt bulunamadı
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </div>

      {/* Modal - Kampanya Formu */}
      {showModal && (
        <div
          className="modal d-block"
          style={{ backgroundColor: "rgba(0,0,0,0.5)" }}
          onClick={(e) => e.target === e.currentTarget && closeModal()}
        >
          <div className="modal-dialog modal-lg modal-dialog-centered modal-dialog-scrollable modal-fullscreen-sm-down">
            <div
              className="modal-content border-0"
              style={{ borderRadius: "16px" }}
            >
              <div
                className="modal-header border-0 py-3 px-3"
                style={{
                  background: "linear-gradient(135deg, #f57c00, #ff9800)",
                }}
              >
                <h5
                  className="modal-title fw-bold text-white"
                  style={{ fontSize: "1rem" }}
                >
                  <i className="fas fa-gift me-2"></i>
                  {editingId ? "Kampanyayı Düzenle" : "Yeni Kampanya Oluştur"}
                </h5>
                <button
                  className="btn-close btn-close-white"
                  onClick={closeModal}
                  aria-label="Kapat"
                ></button>
              </div>
              <form onSubmit={handleSubmit} className="admin-mobile-form">
                <div
                  className="modal-body p-3"
                  style={{ maxHeight: "70vh", overflowY: "auto" }}
                >
                  {modalLoading ? (
                    <div className="text-center py-4">
                      <div
                        className="spinner-border text-primary"
                        role="status"
                      >
                        <span className="visually-hidden">Yükleniyor...</span>
                      </div>
                      <div className="text-muted mt-2">
                        Kampanya bilgileri yükleniyor...
                      </div>
                    </div>
                  ) : (
                    <div className="row g-3">
                      {/* Temel Bilgiler */}
                      <div className="col-12">
                        <div className="card border shadow-sm">
                          <div className="card-header bg-light py-2">
                            <i className="fas fa-info-circle me-2 text-primary"></i>
                            <span
                              className="fw-semibold"
                              style={{ fontSize: "0.9rem" }}
                            >
                              Temel Bilgiler
                            </span>
                          </div>
                          <div className="card-body">
                            <div className="row g-2">
                              <div className="col-12">
                                <label
                                  htmlFor="campaign-name"
                                  className="form-label fw-semibold mb-1"
                                  style={{ fontSize: "0.85rem" }}
                                >
                                  Kampanya Adı{" "}
                                  <span className="text-danger">*</span>
                                </label>
                                <input
                                  id="campaign-name"
                                  className="form-control"
                                  style={{ minHeight: "44px" }}
                                  name="name"
                                  value={form.name}
                                  onChange={onFormChange}
                                  required
                                  placeholder="Örn: Yaz İndirimi"
                                  aria-required="true"
                                  aria-invalid={!form.name && submitted}
                                  aria-describedby={
                                    !form.name && submitted
                                      ? "name-error"
                                      : undefined
                                  }
                                />
                                {!form.name && submitted && (
                                  <div
                                    id="name-error"
                                    className="text-danger small mt-1"
                                    role="alert"
                                  >
                                    Kampanya adı zorunludur
                                  </div>
                                )}
                              </div>
                              <div className="col-12">
                                <label
                                  htmlFor="campaign-description"
                                  className="form-label fw-semibold mb-1"
                                  style={{ fontSize: "0.85rem" }}
                                >
                                  Açıklama
                                </label>
                                <textarea
                                  id="campaign-description"
                                  className="form-control"
                                  rows="2"
                                  name="description"
                                  value={form.description}
                                  onChange={onFormChange}
                                  placeholder="Kampanya açıklaması (opsiyonel)"
                                  aria-label="Kampanya açıklaması"
                                />
                              </div>

                              {/* Kampanya Görseli (Opsiyonel) */}
                              <div className="col-12">
                                <label
                                  className="form-label fw-semibold mb-1"
                                  style={{ fontSize: "0.85rem" }}
                                >
                                  <i className="fas fa-image me-1"></i> Kampanya
                                  Görseli
                                  <span
                                    className="text-muted fw-normal ms-1"
                                    style={{ fontSize: "0.8rem" }}
                                  >
                                    (Opsiyonel)
                                  </span>
                                </label>

                                {imagePreview || form.imageUrl ? (
                                  <div
                                    style={{
                                      position: "relative",
                                      marginBottom: "10px",
                                    }}
                                  >
                                    <img
                                      src={imagePreview || form.imageUrl}
                                      alt="Kampanya görseli"
                                      style={{
                                        width: "100%",
                                        maxHeight: "200px",
                                        objectFit: "cover",
                                        borderRadius: "12px",
                                        border: "2px solid #e5e7eb",
                                      }}
                                    />
                                    <button
                                      type="button"
                                      className="btn btn-sm btn-danger"
                                      style={{
                                        position: "absolute",
                                        top: "8px",
                                        right: "8px",
                                        borderRadius: "50%",
                                        width: "32px",
                                        height: "32px",
                                        padding: 0,
                                        display: "flex",
                                        alignItems: "center",
                                        justifyContent: "center",
                                      }}
                                      onClick={removeImage}
                                    >
                                      <i className="fas fa-times"></i>
                                    </button>
                                  </div>
                                ) : (
                                  <div
                                    style={{
                                      border: "2px dashed #d1d5db",
                                      borderRadius: "12px",
                                      padding: "24px",
                                      textAlign: "center",
                                      cursor: "pointer",
                                      backgroundColor: "#f9fafb",
                                      transition: "all 0.2s ease",
                                    }}
                                    onClick={() =>
                                      document
                                        .getElementById("campaign-image-input")
                                        .click()
                                    }
                                    onDragOver={(e) => {
                                      e.preventDefault();
                                      e.currentTarget.style.borderColor =
                                        "#f97316";
                                    }}
                                    onDragLeave={(e) => {
                                      e.currentTarget.style.borderColor =
                                        "#d1d5db";
                                    }}
                                    onDrop={(e) => {
                                      e.preventDefault();
                                      e.currentTarget.style.borderColor =
                                        "#d1d5db";
                                      const file = e.dataTransfer.files[0];
                                      if (file) {
                                        const fakeEvent = {
                                          target: { files: [file] },
                                        };
                                        handleImageChange(fakeEvent);
                                      }
                                    }}
                                  >
                                    <i
                                      className="fas fa-cloud-upload-alt fa-2x mb-2"
                                      style={{ color: "#9ca3af" }}
                                    ></i>
                                    <p
                                      style={{
                                        margin: 0,
                                        color: "#6b7280",
                                        fontSize: "0.9rem",
                                      }}
                                    >
                                      Görsel yüklemek için tıklayın veya
                                      sürükleyin
                                    </p>
                                    <p
                                      style={{
                                        margin: 0,
                                        color: "#9ca3af",
                                        fontSize: "0.75rem",
                                      }}
                                    >
                                      JPG, PNG, WebP - Maks 10MB
                                    </p>
                                  </div>
                                )}

                                <input
                                  id="campaign-image-input"
                                  type="file"
                                  accept="image/jpeg,image/png,image/gif,image/webp"
                                  style={{ display: "none" }}
                                  onChange={handleImageChange}
                                />

                                {uploadingImage && (
                                  <div className="text-center mt-2">
                                    <div
                                      className="spinner-border spinner-border-sm text-primary me-2"
                                      role="status"
                                    ></div>
                                    <small className="text-muted">
                                      Görsel yükleniyor...
                                    </small>
                                  </div>
                                )}
                              </div>

                              <div className="col-6">
                                <label
                                  className="form-label fw-semibold mb-1"
                                  style={{ fontSize: "0.85rem" }}
                                >
                                  Başlangıç{" "}
                                  <span className="text-danger">*</span>
                                </label>
                                <input
                                  type="date"
                                  className="form-control"
                                  style={{ minHeight: "44px" }}
                                  name="startDate"
                                  value={form.startDate}
                                  onChange={onFormChange}
                                  required
                                />
                              </div>
                              <div className="col-6">
                                <label
                                  className="form-label fw-semibold mb-1"
                                  style={{ fontSize: "0.85rem" }}
                                >
                                  Bitiş <span className="text-danger">*</span>
                                </label>
                                <input
                                  type="date"
                                  className="form-control"
                                  style={{ minHeight: "44px" }}
                                  name="endDate"
                                  value={form.endDate}
                                  onChange={onFormChange}
                                  required
                                />
                              </div>
                            </div>
                          </div>
                        </div>
                      </div>

                      {/* Kampanya Türü */}
                      <div className="col-12">
                        <div className="card border shadow-sm">
                          <div className="card-header bg-light py-2">
                            <i className="fas fa-tags me-2 text-warning"></i>
                            <span
                              className="fw-semibold"
                              style={{ fontSize: "0.9rem" }}
                            >
                              Kampanya Türü
                            </span>
                          </div>
                          <div className="card-body">
                            <div className="row g-2">
                              <div className="col-md-6">
                                <label
                                  className="form-label fw-semibold mb-1"
                                  style={{ fontSize: "0.85rem" }}
                                >
                                  Tür <span className="text-danger">*</span>
                                </label>
                                <select
                                  className="form-select"
                                  style={{ minHeight: "44px" }}
                                  name="type"
                                  value={form.type}
                                  onChange={onFormChange}
                                >
                                  {Object.entries(CAMPAIGN_TYPES).map(
                                    ([key, val]) => (
                                      <option key={key} value={val.value}>
                                        {val.label}
                                      </option>
                                    ),
                                  )}
                                </select>
                              </div>

                              {/* Yüzde veya Sabit Tutar için İndirim Değeri */}
                              {(parseInt(form.type) === 0 ||
                                parseInt(form.type) === 1) && (
                                <div className="col-md-6">
                                  <label
                                    className="form-label fw-semibold mb-1"
                                    style={{ fontSize: "0.85rem" }}
                                  >
                                    {parseInt(form.type) === 0
                                      ? "İndirim Yüzdesi (%)"
                                      : "İndirim Tutarı (₺)"}{" "}
                                    <span className="text-danger">*</span>
                                  </label>
                                  <div className="input-group">
                                    <input
                                      type="number"
                                      className="form-control"
                                      style={{ minHeight: "44px" }}
                                      name="discountValue"
                                      value={form.discountValue}
                                      onChange={onFormChange}
                                      min={parseInt(form.type) === 0 ? 1 : 0.01}
                                      max={
                                        parseInt(form.type) === 0 ? 100 : 100000
                                      }
                                      step={
                                        parseInt(form.type) === 0 ? 1 : 0.01
                                      }
                                      required
                                    />
                                    <span className="input-group-text">
                                      {parseInt(form.type) === 0 ? "%" : "₺"}
                                    </span>
                                  </div>
                                </div>
                              )}

                              {/* Yüzde indirim için maksimum limit */}
                              {parseInt(form.type) === 0 && (
                                <div className="col-md-6">
                                  <label
                                    className="form-label fw-semibold mb-1"
                                    style={{ fontSize: "0.85rem" }}
                                  >
                                    Maksimum İndirim (₺)
                                  </label>
                                  <input
                                    type="number"
                                    className="form-control"
                                    style={{ minHeight: "44px" }}
                                    name="maxDiscountAmount"
                                    value={form.maxDiscountAmount}
                                    onChange={onFormChange}
                                    min={0}
                                    step={0.01}
                                    placeholder="Opsiyonel"
                                  />
                                  <small className="text-muted">
                                    Yüzde indirimin üst limiti
                                  </small>
                                </div>
                              )}

                              {/* X Al Y Öde için */}
                              {parseInt(form.type) === 2 && (
                                <>
                                  <div className="col-6">
                                    <label
                                      className="form-label fw-semibold mb-1"
                                      style={{ fontSize: "0.85rem" }}
                                    >
                                      Al (X){" "}
                                      <span className="text-danger">*</span>
                                    </label>
                                    <input
                                      type="number"
                                      className="form-control"
                                      style={{ minHeight: "44px" }}
                                      name="buyQty"
                                      value={form.buyQty}
                                      onChange={onFormChange}
                                      min={2}
                                      max={100}
                                      required
                                    />
                                    <small className="text-muted">
                                      Alınması gereken adet
                                    </small>
                                  </div>
                                  <div className="col-6">
                                    <label
                                      className="form-label fw-semibold mb-1"
                                      style={{ fontSize: "0.85rem" }}
                                    >
                                      Öde (Y){" "}
                                      <span className="text-danger">*</span>
                                    </label>
                                    <input
                                      type="number"
                                      className="form-control"
                                      style={{ minHeight: "44px" }}
                                      name="payQty"
                                      value={form.payQty}
                                      onChange={onFormChange}
                                      min={1}
                                      max={parseInt(form.buyQty) - 1 || 99}
                                      required
                                    />
                                    <small className="text-muted">
                                      Ödenecek adet
                                    </small>
                                  </div>
                                  <div className="col-12">
                                    <div
                                      className="alert alert-info py-2 mb-0"
                                      style={{ fontSize: "0.8rem" }}
                                    >
                                      <i className="fas fa-info-circle me-2"></i>
                                      <strong>
                                        {form.buyQty} Al {form.payQty} Öde:
                                      </strong>{" "}
                                      Müşteri {form.buyQty} adet alırsa{" "}
                                      {parseInt(form.buyQty) -
                                        parseInt(form.payQty)}{" "}
                                      tanesi bedava!
                                    </div>
                                  </div>
                                </>
                              )}

                              {/* Ücretsiz Kargo bilgisi */}
                              {parseInt(form.type) === 3 && (
                                <div className="col-12">
                                  <div
                                    className="alert alert-success py-2 mb-0"
                                    style={{ fontSize: "0.8rem" }}
                                  >
                                    <i className="fas fa-truck me-2"></i>
                                    Ücretsiz kargo kampanyası aktif edilecek.
                                  </div>
                                </div>
                              )}

                              {/* Ücretsiz Kargo + Kategori/Ürün bazlı UYARI */}
                              {parseInt(form.type) === 3 &&
                                parseInt(form.targetType) !== 0 && (
                                  <div className="col-12">
                                    <div
                                      className="alert alert-warning py-2 mb-0"
                                      style={{ fontSize: "0.8rem" }}
                                    >
                                      <i className="fas fa-exclamation-triangle me-2"></i>
                                      <strong>Önemli:</strong> Kategori/ürün
                                      bazlı ücretsiz kargo kampanyasında,
                                      müşterinin sepetindeki{" "}
                                      <strong>TÜM ürünler</strong> seçili
                                      kategori/üründen olmalıdır. Farklı
                                      kategoriden ürün eklendiğinde ücretsiz
                                      kargo geçersiz olur.
                                    </div>
                                  </div>
                                )}
                            </div>
                          </div>
                        </div>
                      </div>

                      {/* Hedef Seçimi */}
                      <div className="col-12">
                        <div className="card border shadow-sm">
                          <div className="card-header bg-light py-2">
                            <i className="fas fa-bullseye me-2 text-success"></i>
                            <span
                              className="fw-semibold"
                              style={{ fontSize: "0.9rem" }}
                            >
                              Kampanya Hedefi
                            </span>
                          </div>
                          <div className="card-body">
                            <div className="row g-2">
                              <div className="col-12">
                                <label
                                  className="form-label fw-semibold mb-1"
                                  style={{ fontSize: "0.85rem" }}
                                >
                                  Hedef Türü
                                </label>
                                <select
                                  className="form-select"
                                  style={{ minHeight: "44px" }}
                                  name="targetType"
                                  value={form.targetType}
                                  onChange={onFormChange}
                                >
                                  {Object.entries(TARGET_TYPES).map(
                                    ([key, val]) => (
                                      <option key={key} value={val.value}>
                                        {val.label}
                                      </option>
                                    ),
                                  )}
                                </select>
                              </div>

                              {/* Kategori seçimi */}
                              {parseInt(form.targetType) === 1 && (
                                <div className="col-12">
                                  <label
                                    className="form-label fw-semibold mb-1"
                                    style={{ fontSize: "0.85rem" }}
                                  >
                                    Kategoriler{" "}
                                    <span className="text-danger">*</span>
                                  </label>
                                  {productsLoading ? (
                                    <div className="text-muted">
                                      Kategoriler yükleniyor...
                                    </div>
                                  ) : (
                                    <select
                                      className="form-select"
                                      style={{ minHeight: "120px" }}
                                      multiple
                                      value={form.targetIds.map(String)}
                                      onChange={(e) => {
                                        const selected = Array.from(
                                          e.target.selectedOptions,
                                          (opt) => parseInt(opt.value),
                                        );
                                        onTargetIdsChange(selected);
                                      }}
                                    >
                                      {categories.map((cat) => (
                                        <option key={cat.id} value={cat.id}>
                                          {cat.name}
                                        </option>
                                      ))}
                                    </select>
                                  )}
                                  <small className="text-muted">
                                    Ctrl/Cmd tuşuyla birden fazla seçebilirsiniz
                                  </small>
                                </div>
                              )}

                              {/* Ürün seçimi - GELİŞMİŞ ARAMA SİSTEMİ */}
                              {parseInt(form.targetType) === 2 && (
                                <div className="col-12">
                                  <label
                                    className="form-label fw-semibold mb-1"
                                    style={{ fontSize: "0.85rem" }}
                                  >
                                    Ürünler{" "}
                                    <span className="text-danger">*</span>
                                    <span
                                      className="badge bg-primary ms-2"
                                      style={{ fontSize: "0.7rem" }}
                                    >
                                      {form.targetIds?.length || 0} seçili
                                    </span>
                                  </label>

                                  {productsLoading ? (
                                    <div className="text-center py-4">
                                      <div
                                        className="spinner-border spinner-border-sm text-primary me-2"
                                        role="status"
                                      ></div>
                                      <span className="text-muted">
                                        Ürünler yükleniyor...
                                      </span>
                                    </div>
                                  ) : (
                                    <>
                                      {/* Arama Input'u */}
                                      <div className="input-group mb-2">
                                        <span className="input-group-text bg-light">
                                          <i className="fas fa-search text-muted"></i>
                                        </span>
                                        <input
                                          type="text"
                                          className="form-control"
                                          placeholder="Ürün ara: ID (#123), isim, SKU veya kategori..."
                                          onChange={handleProductSearchChange}
                                          style={{ minHeight: "44px" }}
                                        />
                                        {productSearchQuery && (
                                          <button
                                            type="button"
                                            className="btn btn-outline-secondary"
                                            onClick={() =>
                                              setProductSearchQuery("")
                                            }
                                            title="Aramayı temizle"
                                          >
                                            <i className="fas fa-times"></i>
                                          </button>
                                        )}
                                      </div>

                                      {/* Hızlı İşlem Butonları */}
                                      <div className="d-flex gap-2 mb-2 flex-wrap">
                                        <button
                                          type="button"
                                          className="btn btn-sm btn-outline-success"
                                          onClick={handleSelectAllProducts}
                                          title="Tüm filtrelenmiş ürünleri seç"
                                        >
                                          <i className="fas fa-check-double me-1"></i>
                                          Tümünü Seç ({filteredProducts.length})
                                        </button>
                                        <button
                                          type="button"
                                          className="btn btn-sm btn-outline-danger"
                                          onClick={handleClearAllProducts}
                                          title="Tüm seçimleri kaldır"
                                          disabled={!form.targetIds?.length}
                                        >
                                          <i className="fas fa-times me-1"></i>
                                          Tümünü Kaldır
                                        </button>
                                        <span
                                          className="text-muted ms-auto"
                                          style={{
                                            fontSize: "0.8rem",
                                            alignSelf: "center",
                                          }}
                                        >
                                          {filteredProducts.length} ürün bulundu
                                        </span>
                                      </div>

                                      {/* Seçili Ürünler Badge'leri */}
                                      {selectedProducts.length > 0 && (
                                        <div
                                          className="mb-2 p-2 bg-light rounded"
                                          style={{
                                            maxHeight: "100px",
                                            overflowY: "auto",
                                          }}
                                        >
                                          <small className="text-muted d-block mb-1">
                                            Seçili ürünler:
                                          </small>
                                          <div className="d-flex flex-wrap gap-1">
                                            {selectedProducts.map((prod) => (
                                              <span
                                                key={prod.id}
                                                className="badge bg-primary d-inline-flex align-items-center gap-1"
                                                style={{
                                                  fontSize: "0.75rem",
                                                  cursor: "pointer",
                                                }}
                                                onClick={() =>
                                                  handleProductToggle(prod.id)
                                                }
                                                title="Kaldırmak için tıklayın"
                                              >
                                                #{prod.id}{" "}
                                                {prod.name?.substring(0, 20)}
                                                {prod.name?.length > 20 &&
                                                  "..."}
                                                <i className="fas fa-times-circle ms-1"></i>
                                              </span>
                                            ))}
                                          </div>
                                        </div>
                                      )}

                                      {/* Ürün Listesi */}
                                      <div
                                        className="border rounded"
                                        style={{
                                          maxHeight: "250px",
                                          overflowY: "auto",
                                          backgroundColor: "#fafafa",
                                        }}
                                      >
                                        {paginatedProducts.length === 0 ? (
                                          <div className="text-center py-4 text-muted">
                                            <i className="fas fa-search fa-2x mb-2 d-block"></i>
                                            {productSearchQuery
                                              ? "Aramanızla eşleşen ürün bulunamadı"
                                              : "Ürün bulunamadı"}
                                          </div>
                                        ) : (
                                          <div className="list-group list-group-flush">
                                            {paginatedProducts.map((prod) => {
                                              const isSelected =
                                                form.targetIds?.includes(
                                                  prod.id,
                                                );
                                              return (
                                                <label
                                                  key={prod.id}
                                                  className={`list-group-item list-group-item-action d-flex align-items-center gap-2 py-2 ${isSelected ? "bg-primary bg-opacity-10" : ""}`}
                                                  style={{
                                                    cursor: "pointer",
                                                    fontSize: "0.85rem",
                                                  }}
                                                >
                                                  <input
                                                    type="checkbox"
                                                    className="form-check-input m-0"
                                                    checked={isSelected}
                                                    onChange={() =>
                                                      handleProductToggle(
                                                        prod.id,
                                                      )
                                                    }
                                                    style={{
                                                      minWidth: "18px",
                                                      minHeight: "18px",
                                                    }}
                                                  />
                                                  <span
                                                    className="badge bg-secondary"
                                                    style={{ minWidth: "50px" }}
                                                  >
                                                    #{prod.id}
                                                  </span>
                                                  <div className="flex-grow-1">
                                                    <div
                                                      className="fw-medium text-truncate"
                                                      style={{
                                                        maxWidth: "200px",
                                                      }}
                                                    >
                                                      {prod.name}
                                                    </div>
                                                    {prod.categoryName && (
                                                      <small className="text-muted">
                                                        {prod.categoryName}
                                                      </small>
                                                    )}
                                                  </div>
                                                  <div className="text-end">
                                                    <div className="fw-bold text-success">
                                                      ₺
                                                      {(
                                                        prod.price || 0
                                                      ).toFixed(2)}
                                                    </div>
                                                    {prod.sku && (
                                                      <small className="text-muted">
                                                        SKU: {prod.sku}
                                                      </small>
                                                    )}
                                                  </div>
                                                </label>
                                              );
                                            })}
                                          </div>
                                        )}
                                      </div>

                                      {/* Sayfalama */}
                                      {totalProductPages > 1 && (
                                        <div className="d-flex justify-content-between align-items-center mt-2">
                                          <small className="text-muted">
                                            Sayfa {productPage} /{" "}
                                            {totalProductPages}
                                          </small>
                                          <div className="btn-group btn-group-sm">
                                            <button
                                              type="button"
                                              className="btn btn-outline-secondary"
                                              onClick={() =>
                                                setProductPage((p) =>
                                                  Math.max(1, p - 1),
                                                )
                                              }
                                              disabled={productPage === 1}
                                            >
                                              <i className="fas fa-chevron-left"></i>
                                            </button>
                                            <button
                                              type="button"
                                              className="btn btn-outline-secondary"
                                              onClick={() =>
                                                setProductPage((p) =>
                                                  Math.min(
                                                    totalProductPages,
                                                    p + 1,
                                                  ),
                                                )
                                              }
                                              disabled={
                                                productPage ===
                                                totalProductPages
                                              }
                                            >
                                              <i className="fas fa-chevron-right"></i>
                                            </button>
                                          </div>
                                        </div>
                                      )}
                                    </>
                                  )}
                                </div>
                              )}
                            </div>
                          </div>
                        </div>
                      </div>

                      {/* Koşullar */}
                      <div className="col-12">
                        <div className="card border shadow-sm">
                          <div className="card-header bg-light py-2">
                            <i className="fas fa-sliders-h me-2 text-info"></i>
                            <span
                              className="fw-semibold"
                              style={{ fontSize: "0.9rem" }}
                            >
                              Koşullar (Opsiyonel)
                            </span>
                          </div>
                          <div className="card-body">
                            <div className="row g-2">
                              <div className="col-md-4">
                                <label
                                  className="form-label fw-semibold mb-1"
                                  style={{ fontSize: "0.85rem" }}
                                >
                                  Min. Sepet Tutarı (₺)
                                </label>
                                <input
                                  type="number"
                                  className="form-control"
                                  style={{ minHeight: "44px" }}
                                  name="minCartTotal"
                                  value={form.minCartTotal}
                                  onChange={onFormChange}
                                  min={0}
                                  step={0.01}
                                  placeholder="0"
                                />
                              </div>
                              <div className="col-md-4">
                                <label
                                  className="form-label fw-semibold mb-1"
                                  style={{ fontSize: "0.85rem" }}
                                >
                                  Min. Ürün Adedi
                                </label>
                                <input
                                  type="number"
                                  className="form-control"
                                  style={{ minHeight: "44px" }}
                                  name="minQuantity"
                                  value={form.minQuantity}
                                  onChange={onFormChange}
                                  min={1}
                                  placeholder="1"
                                />
                              </div>
                              <div className="col-md-4">
                                <label
                                  className="form-label fw-semibold mb-1"
                                  style={{ fontSize: "0.85rem" }}
                                >
                                  Öncelik
                                </label>
                                <input
                                  type="number"
                                  className="form-control"
                                  style={{ minHeight: "44px" }}
                                  name="priority"
                                  value={form.priority}
                                  onChange={onFormChange}
                                  min={1}
                                  max={1000}
                                />
                                <small className="text-muted">
                                  Düşük = Yüksek öncelik
                                </small>
                              </div>
                            </div>
                          </div>
                        </div>
                      </div>

                      {/* Durum */}
                      <div className="col-12">
                        <div className="d-flex gap-3 align-items-center p-3 bg-light rounded">
                          <div className="form-check form-switch">
                            <input
                              type="checkbox"
                              className="form-check-input"
                              id="isActive"
                              name="isActive"
                              checked={form.isActive}
                              onChange={onFormChange}
                              style={{ width: "3rem", height: "1.5rem" }}
                            />
                            <label
                              className="form-check-label fw-semibold ms-2"
                              htmlFor="isActive"
                            >
                              {form.isActive ? "Aktif" : "Pasif"}
                            </label>
                          </div>
                          <div className="form-check form-switch ms-3">
                            <input
                              type="checkbox"
                              className="form-check-input"
                              id="isStackable"
                              name="isStackable"
                              checked={form.isStackable}
                              onChange={onFormChange}
                              style={{ width: "3rem", height: "1.5rem" }}
                            />
                            <label
                              className="form-check-label fw-semibold ms-2"
                              htmlFor="isStackable"
                            >
                              Birleştirilebilir
                            </label>
                          </div>
                        </div>
                      </div>
                    </div>
                  )}
                </div>
                <div className="modal-footer border-0 py-3 px-3 bg-light">
                  <button
                    type="button"
                    className="btn btn-outline-secondary"
                    style={{ minHeight: "44px", minWidth: "100px" }}
                    onClick={closeModal}
                  >
                    <i className="fas fa-times me-1"></i>İptal
                  </button>
                  <button
                    type="button"
                    className="btn btn-outline-info"
                    style={{ minHeight: "44px", minWidth: "100px" }}
                    onClick={handlePreviewFormData}
                    disabled={modalLoading || !form.name || !form.discountValue}
                    title="Kampanya önizlemesini göster"
                  >
                    <i className="fas fa-eye me-1"></i>Önizle
                  </button>
                  <button
                    type="submit"
                    className="btn text-white fw-semibold px-4"
                    style={{
                      background: "linear-gradient(135deg, #f57c00, #ff9800)",
                      minHeight: "44px",
                      minWidth: "120px",
                    }}
                    disabled={modalLoading || uploadingImage}
                  >
                    {uploadingImage ? (
                      <>
                        <span className="spinner-border spinner-border-sm me-1"></span>
                        Yükleniyor...
                      </>
                    ) : (
                      <>
                        <i
                          className={`fas ${editingId ? "fa-save" : "fa-plus"} me-1`}
                        ></i>
                        {editingId ? "Güncelle" : "Oluştur"}
                      </>
                    )}
                  </button>
                </div>
              </form>
            </div>
          </div>
        </div>
      )}

      {/* =========================================================================
          KAMPANYA ÖNİZLEME MODALI
          Kampanyanın etkileyeceği ürünleri ve fiyat değişikliklerini gösterir
          ========================================================================= */}
      {showPreviewModal && (
        <div
          className="modal d-block"
          style={{ backgroundColor: "rgba(0,0,0,0.5)" }}
          onClick={(e) => e.target === e.currentTarget && closePreviewModal()}
        >
          <div className="modal-dialog modal-lg modal-dialog-centered modal-dialog-scrollable">
            <div className="modal-content" style={{ borderRadius: "15px" }}>
              {/* Modal Header */}
              <div
                className="modal-header border-0 py-3 px-4"
                style={{
                  background: "linear-gradient(135deg, #17a2b8, #20c997)",
                }}
              >
                <h5 className="modal-title text-white fw-bold">
                  <i className="fas fa-eye me-2"></i>
                  Kampanya Önizleme
                </h5>
                <button
                  type="button"
                  className="btn-close btn-close-white"
                  onClick={closePreviewModal}
                ></button>
              </div>

              {/* Modal Body */}
              <div className="modal-body p-4">
                {previewLoading ? (
                  <div className="text-center py-5">
                    <div className="spinner-border text-info" role="status">
                      <span className="visually-hidden">Yükleniyor...</span>
                    </div>
                    <p className="mt-3 text-muted">Önizleme hesaplanıyor...</p>
                  </div>
                ) : previewError ? (
                  <div className="alert alert-danger">
                    <i className="fas fa-exclamation-circle me-2"></i>
                    {previewError}
                  </div>
                ) : previewData ? (
                  <>
                    {/* Özet Bilgiler */}
                    <div className="alert alert-info mb-4">
                      <i className="fas fa-info-circle me-2"></i>
                      {previewData.message}
                    </div>

                    {/* İstatistik Kartları */}
                    <div className="row g-3 mb-4">
                      <div className="col-6 col-md-3">
                        <div
                          className="card border-0 shadow-sm h-100"
                          style={{ borderRadius: "10px" }}
                        >
                          <div className="card-body text-center py-3">
                            <div className="h4 mb-1 text-primary">
                              {previewData.totalProductCount || 0}
                            </div>
                            <small className="text-muted">Etkilenen Ürün</small>
                          </div>
                        </div>
                      </div>
                      <div className="col-6 col-md-3">
                        <div
                          className="card border-0 shadow-sm h-100"
                          style={{ borderRadius: "10px" }}
                        >
                          <div className="card-body text-center py-3">
                            <div className="h4 mb-1 text-success">
                              ₺
                              {(previewData.totalDiscount || 0).toLocaleString(
                                "tr-TR",
                                { minimumFractionDigits: 2 },
                              )}
                            </div>
                            <small className="text-muted">Toplam İndirim</small>
                          </div>
                        </div>
                      </div>
                      <div className="col-6 col-md-3">
                        <div
                          className="card border-0 shadow-sm h-100"
                          style={{ borderRadius: "10px" }}
                        >
                          <div className="card-body text-center py-3">
                            <div className="h4 mb-1 text-warning">
                              %
                              {(
                                previewData.averageDiscountPercentage || 0
                              ).toFixed(1)}
                            </div>
                            <small className="text-muted">Ort. İndirim</small>
                          </div>
                        </div>
                      </div>
                      <div className="col-6 col-md-3">
                        <div
                          className="card border-0 shadow-sm h-100"
                          style={{ borderRadius: "10px" }}
                        >
                          <div className="card-body text-center py-3">
                            <div className="h4 mb-1 text-danger">
                              ₺
                              {previewData.affectedProducts?.length > 0
                                ? (
                                    previewData.affectedProducts.reduce(
                                      (sum, p) => sum + p.discountAmount,
                                      0,
                                    ) / previewData.affectedProducts.length
                                  ).toLocaleString("tr-TR", {
                                    minimumFractionDigits: 2,
                                  })
                                : "0,00"}
                            </div>
                            <small className="text-muted">
                              Ort. İndirim (₺)
                            </small>
                          </div>
                        </div>
                      </div>
                    </div>

                    {/* Ürün Listesi */}
                    {previewData.affectedProducts?.length > 0 ? (
                      <div
                        className="table-responsive"
                        style={{ maxHeight: "400px", overflowY: "auto" }}
                      >
                        <table className="table table-hover table-sm">
                          <thead className="table-light sticky-top">
                            <tr>
                              <th style={{ fontSize: "0.8rem" }}>Ürün</th>
                              <th style={{ fontSize: "0.8rem" }}>Kategori</th>
                              <th
                                className="text-end"
                                style={{ fontSize: "0.8rem" }}
                              >
                                Eski Fiyat
                              </th>
                              <th
                                className="text-end"
                                style={{ fontSize: "0.8rem" }}
                              >
                                Yeni Fiyat
                              </th>
                              <th
                                className="text-end"
                                style={{ fontSize: "0.8rem" }}
                              >
                                İndirim
                              </th>
                            </tr>
                          </thead>
                          <tbody>
                            {previewData.affectedProducts.map((product) => (
                              <tr key={product.productId}>
                                <td style={{ fontSize: "0.85rem" }}>
                                  <span className="fw-medium">
                                    {product.productName}
                                  </span>
                                  <br />
                                  <small className="text-muted">
                                    #{product.productId}
                                  </small>
                                </td>
                                <td style={{ fontSize: "0.85rem" }}>
                                  <span className="badge bg-secondary">
                                    {product.categoryName}
                                  </span>
                                </td>
                                <td
                                  className="text-end"
                                  style={{ fontSize: "0.85rem" }}
                                >
                                  <span className="text-decoration-line-through text-muted">
                                    ₺
                                    {product.originalPrice.toLocaleString(
                                      "tr-TR",
                                      { minimumFractionDigits: 2 },
                                    )}
                                  </span>
                                </td>
                                <td
                                  className="text-end"
                                  style={{ fontSize: "0.85rem" }}
                                >
                                  <span className="fw-bold text-success">
                                    ₺
                                    {product.newPrice.toLocaleString("tr-TR", {
                                      minimumFractionDigits: 2,
                                    })}
                                  </span>
                                </td>
                                <td
                                  className="text-end"
                                  style={{ fontSize: "0.85rem" }}
                                >
                                  <span className="badge bg-danger">
                                    -₺
                                    {product.discountAmount.toLocaleString(
                                      "tr-TR",
                                      { minimumFractionDigits: 2 },
                                    )}
                                    <br />
                                    <small>
                                      (%{product.discountPercentage})
                                    </small>
                                  </span>
                                </td>
                              </tr>
                            ))}
                          </tbody>
                        </table>
                      </div>
                    ) : (
                      <div className="text-center text-muted py-4">
                        <i className="fas fa-inbox fa-3x mb-3 opacity-50"></i>
                        <p>Bu kampanya için etkilenecek ürün bulunamadı.</p>
                      </div>
                    )}
                  </>
                ) : (
                  <div className="text-center text-muted py-4">
                    <i className="fas fa-search fa-3x mb-3 opacity-50"></i>
                    <p>Önizleme verisi yükleniyor...</p>
                  </div>
                )}
              </div>

              {/* Modal Footer */}
              <div className="modal-footer border-0 py-3 px-4 bg-light">
                <button
                  type="button"
                  className="btn btn-secondary"
                  style={{ minHeight: "44px", minWidth: "100px" }}
                  onClick={closePreviewModal}
                >
                  <i className="fas fa-times me-1"></i>Kapat
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
