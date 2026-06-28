import React, { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { ProductService } from "../../services/productService";
import categoryService from "../../services/categoryService";
import variantStore from "../../utils/variantStore";
import XmlImportModal from "../../components/Admin/XmlImportModal";
import VariantManager from "../../components/Admin/VariantManager";

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
    specialPrice: "",
    stock: "",
    description: "",
    imageUrl: "",
    imageUrls: [],
    isActive: true,
    adminOverrideName: null,
    adminOverridePrice: null,
    adminOverrideCategory: null,
  });
  const [globalOverrideSettings, setGlobalOverrideSettings] = useState({
    defaultAdminOverrideName: false,
    defaultAdminOverridePrice: false,
    defaultAdminOverrideCategory: false,
  });
  const [globalOverrideLoading, setGlobalOverrideLoading] = useState(false);
  const [globalOverrideSaving, setGlobalOverrideSaving] = useState(false);
  const [editingProductId, setEditingProductId] = useState(null);
  const [productVariants, setProductVariants] = useState([]);

  // Excel Import State'leri
  const [showExcelModal, setShowExcelModal] = useState(false);
  const [excelFile, setExcelFile] = useState(null);
  const [excelImageFiles, setExcelImageFiles] = useState([]); // Görsel dosyaları
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

  // Otomatik Kategorize State'leri
  const [categorizingLoading, setCategorizingLoading] = useState(false);
  const [duplicateGroups, setDuplicateGroups] = useState([]);
  const [duplicateLoading, setDuplicateLoading] = useState(false);
  const [showDuplicatePanel, setShowDuplicatePanel] = useState(false);
  const [searchFilters, setSearchFilters] = useState({
    sku: "",
    name: "",
    status: "all",
    stockStatus: "all",
  });
  const [draftSearchFilters, setDraftSearchFilters] = useState({
    sku: "",
    name: "",
  });
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize, setPageSize] = useState(24);
  const [totalProductsCount, setTotalProductsCount] = useState(0);
  const [serverTotalPages, setServerTotalPages] = useState(1);

  // Resim Upload State'leri
  const [imageUploading, setImageUploading] = useState(false);
  const [isDragging, setIsDragging] = useState(false);
  const imageInputRef = useRef(null);
  const replaceImageInputRef = useRef(null);
  const replacingImageIndexRef = useRef(null);
  const dropZoneRef = useRef(null);
  const fetchProductsRef = useRef(null);

  const resolveEffectiveOverride = (fieldName, explicitValue) => {
    if (explicitValue === true || explicitValue === false) {
      return explicitValue;
    }

    const globalFieldMap = {
      adminOverrideName: "defaultAdminOverrideName",
      adminOverridePrice: "defaultAdminOverridePrice",
      adminOverrideCategory: "defaultAdminOverrideCategory",
    };

    return globalOverrideSettings[globalFieldMap[fieldName]] === true;
  };

  const fetchGlobalOverrideSettings = async () => {
    try {
      setGlobalOverrideLoading(true);
      const result = await ProductService.getAdminOverrideSettings();
      setGlobalOverrideSettings({
        defaultAdminOverrideName: result.defaultAdminOverrideName === true,
        defaultAdminOverridePrice: result.defaultAdminOverridePrice === true,
        defaultAdminOverrideCategory: result.defaultAdminOverrideCategory === true,
      });
    } catch (err) {
      console.error("Genel Mikro bağımsızlık ayarları yüklenemedi:", err);
    } finally {
      setGlobalOverrideLoading(false);
    }
  };

  const handleSaveGlobalOverrideSettings = async () => {
    try {
      setGlobalOverrideSaving(true);
      const updated = await ProductService.updateAdminOverrideSettings(
        globalOverrideSettings,
      );
      setGlobalOverrideSettings({
        defaultAdminOverrideName: updated.defaultAdminOverrideName === true,
        defaultAdminOverridePrice: updated.defaultAdminOverridePrice === true,
        defaultAdminOverrideCategory: updated.defaultAdminOverrideCategory === true,
      });
      alert("✅ Tüm ürünler için varsayılan Mikro bağımsızlık ayarları güncellendi.");
      fetchProducts();
    } catch (err) {
      console.error("Genel Mikro bağımsızlık ayarları kaydedilemedi:", err);
      alert(
        "❌ Genel Mikro bağımsızlık ayarları kaydedilemedi: " +
          (err.response?.data?.message || err.message),
      );
    } finally {
      setGlobalOverrideSaving(false);
    }
  };

  const handleProductOverrideModeChange = (fieldName, mode) => {
    const nextValue = mode === "inherit" ? null : mode === "admin";
    setFormData((prev) => ({
      ...prev,
      [fieldName]: nextValue,
    }));
  };

  const fetchProducts = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const response = await ProductService.getAdminPage({
        page: currentPage,
        size: pageSize,
        sku: searchFilters.sku,
        name: searchFilters.name,
        status: searchFilters.status,
        stockStatus: searchFilters.stockStatus,
      });
      setProducts(response.items || []);
      setTotalProductsCount(response.total || 0);
      setServerTotalPages(Math.max(1, response.totalPages || 1));
    } catch (err) {
      setError("Ürünler yüklenirken hata oluştu");
      console.error("Ürünler yükleme hatası:", err);
    } finally {
      setLoading(false);
    }
  }, [currentPage, pageSize, searchFilters]);

  useEffect(() => {
    fetchProductsRef.current = fetchProducts;
  }, [fetchProducts]);

  useEffect(() => {
    const debounceTimer = window.setTimeout(() => {
      setSearchFilters((prev) => {
        const nextSku = draftSearchFilters.sku.trimStart();
        const nextName = draftSearchFilters.name.trimStart();

        if (prev.sku === nextSku && prev.name === nextName) {
          return prev;
        }

        return {
          ...prev,
          sku: nextSku,
          name: nextName,
        };
      });
      setCurrentPage(1);
    }, 350);

    return () => {
      window.clearTimeout(debounceTimer);
    };
  }, [draftSearchFilters]);

  useEffect(() => {
    fetchCategories();
    fetchDuplicateGroups();
    fetchGlobalOverrideSettings();

    // ProductService subscription - CRUD değişikliklerinde otomatik refetch
    // Bu sayede başka bir yerde yapılan değişiklikler de yansır
    const unsubscribe = ProductService.subscribe((event) => {
      console.log("📦 Ürün değişikliği algılandı:", event.action);
      // Kendi CRUD işlemlerimizden sonra zaten fetchProducts çağrılıyor
      // Bu subscription daha çok multi-tab senkronizasyonu için
      if (event.action === "import") {
        // Excel import sonrası tam yenileme
        fetchProductsRef.current?.();
      }
    });

    // Cleanup on unmount
    return () => {
      unsubscribe();
    };
  }, []);

  useEffect(() => {
    fetchProducts();
  }, [fetchProducts]);

  const fetchCategories = async () => {
    try {
      const categoriesData = await categoryService.getAll();
      setCategories(categoriesData);
    } catch (err) {
      console.error("Kategoriler yükleme hatası:", err);
    }
  };

  const fetchDuplicateGroups = async () => {
    try {
      setDuplicateLoading(true);
      const result = await ProductService.getDuplicateGroups();
      setDuplicateGroups(Array.isArray(result?.groups) ? result.groups : []);
    } catch (err) {
      console.error("Mükerrer ürünler yükleme hatası:", err);
      setDuplicateGroups([]);
    } finally {
      setDuplicateLoading(false);
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

    const specialPriceValue =
      formData.specialPrice === "" ? null : parseFloat(formData.specialPrice);
    if (
      specialPriceValue !== null &&
      (isNaN(specialPriceValue) || specialPriceValue < 0)
    ) {
      alert("❌ İndirimli fiyat 0 veya daha büyük bir değer olmalıdır.");
      return;
    }
    if (specialPriceValue !== null && specialPriceValue >= price) {
      alert("❌ İndirimli fiyat normal fiyattan düşük olmalıdır.");
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

    const categoryId = Number.parseInt(formData.categoryId, 10);
    if (!Number.isInteger(categoryId) || categoryId <= 0) {
      alert("❌ Geçerli bir kategori seçin.");
      return;
    }

    try {
      // Ürün verilerini API formatına dönüştür
      const isEditing = Boolean(editingProduct);
      const currentImages = (formData.imageUrls || []).filter(Boolean);
      const primaryImageUrl =
        formData.imageUrl?.trim() || currentImages[0] || null;

      const productData = {
        name: formData.name.trim(),
        description: formData.description?.trim() || "",
        price: price,
        specialPrice: specialPriceValue,
        stockQuantity: stock, // API stockQuantity bekliyor
        categoryId: categoryId,
        imageUrl: primaryImageUrl,
        imageUrls: currentImages,
        isActive: formData.isActive !== false,
        sku: formData.sku || "", // Mikro ERP ürünlerinde SKU bazlı güncelleme için
        adminOverrideName: formData.adminOverrideName,
        adminOverridePrice: formData.adminOverridePrice,
        adminOverrideCategory: formData.adminOverrideCategory,
      };

      if (editingProduct) {
        // Mevcut ürünü güncelle — id > 0 ise id bazlı, değilse SKU bazlı (Mikro ürünleri)
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
        specialPrice: "",
        stock: "",
        description: "",
        imageUrl: "",
        imageUrls: [],
        isActive: true,
        adminOverrideName: null,
        adminOverridePrice: null,
        adminOverrideCategory: null,
      });
      setEditingProduct(null);
      setEditingProductId(null);

      // Listeyi yenile
      fetchProducts();
      fetchDuplicateGroups();
    } catch (err) {
      console.error("Ürün kaydetme hatası:", err);
      alert(
        "❌ Ürün kaydedilirken hata oluştu: " +
          (err.response?.data?.message || err.message),
      );
    }
  };

  const handleEdit = (product) => {
    const resolvedCategoryId = Number.parseInt(
      String(product.categoryId ?? product.category?.id ?? ""),
      10,
    );
    const resolvedStock = product.stock ?? product.stockQuantity ?? 0;

    setEditingProduct(product);
    setEditingProductId(product.id);
    setProductVariants(variantStore.getVariantsForProduct(product.id) || []);
    const existingImages =
      Array.isArray(product.imageUrls) && product.imageUrls.length > 0
        ? product.imageUrls
        : product.imageUrl
          ? [product.imageUrl]
          : [];
    setFormData({
      name: product.name,
      categoryId:
        Number.isInteger(resolvedCategoryId) && resolvedCategoryId > 0
          ? String(resolvedCategoryId)
          : "",
      price: product.price.toString(),
      specialPrice:
        product.specialPrice !== null && product.specialPrice !== undefined
          ? String(product.specialPrice)
          : "",
      stock: String(resolvedStock),
      description: product.description || "",
      imageUrl: product.imageUrl || existingImages[0] || "",
      imageUrls: existingImages,
      isActive: product.isActive,
      sku: product.sku || "", // Mikro ERP ürünlerinde SKU üzerinden güncelleme
      adminOverrideName:
        product.adminOverrideName === true || product.adminOverrideName === false
          ? product.adminOverrideName
          : null,
      adminOverridePrice:
        product.adminOverridePrice === true || product.adminOverridePrice === false
          ? product.adminOverridePrice
          : null,
      adminOverrideCategory:
        product.adminOverrideCategory === true || product.adminOverrideCategory === false
          ? product.adminOverrideCategory
          : null,
    });
    setShowModal(true);
  };

  /**
   * Resim dosyasını validate eder ve önizleme oluşturur.
   * File input veya drag & drop'tan gelen dosyalar için çalışır.
   * @param {File} file - Seçilen dosya
   * @returns {boolean} - Dosya geçerli mi
   */
  const validateImageFile = (file) => {
    if (!file) return false;

    const allowedTypes = ["image/jpeg", "image/png", "image/gif", "image/webp"];
    if (!allowedTypes.includes(file.type)) {
      alert("Sadece resim dosyaları (jpg, png, gif, webp) yüklenebilir.");
      return false;
    }

    const maxSize = 10 * 1024 * 1024;
    if (file.size > maxSize) {
      alert("Dosya boyutu maksimum 10MB olabilir.");
      return false;
    }

    return true;
  };

  const appendUploadedImages = (urls) => {
    if (!urls.length) return;
    setFormData((prev) => {
      const merged = [...(prev.imageUrls || []), ...urls];
      return {
        ...prev,
        imageUrls: merged,
        imageUrl: prev.imageUrl || merged[0] || "",
      };
    });
  };

  const uploadImageFiles = async (files) => {
    const validFiles = Array.from(files || []).filter(validateImageFile);
    if (!validFiles.length) return;

    setImageUploading(true);
    try {
      const uploadedUrls = [];
      for (const file of validFiles) {
        const result = await ProductService.uploadImage(file);
        if (result?.success && result?.imageUrl) {
          uploadedUrls.push(result.imageUrl);
        }
      }

      if (uploadedUrls.length > 0) {
        appendUploadedImages(uploadedUrls);
      } else {
        throw new Error("Resim yüklenemedi");
      }
    } catch (err) {
      console.error("Resim yükleme hatası:", err);
      alert(
        "Resim yüklenirken hata oluştu: " +
          (err.response?.data?.message || err.message),
      );
    } finally {
      setImageUploading(false);
      if (imageInputRef.current) {
        imageInputRef.current.value = "";
      }
    }
  };

  const handleImageSelect = (e) => {
    const files = e.target.files;
    if (files?.length) {
      uploadImageFiles(files);
    }
  };

  const handleRemoveImage = (index) => {
    const url = formData.imageUrls?.[index];
    if (!url) return;

    if (!window.confirm("Bu görseli kaldırmak istediğinize emin misiniz?")) {
      return;
    }

    setFormData((prev) => {
      const nextUrls = (prev.imageUrls || []).filter((_, i) => i !== index);
      return {
        ...prev,
        imageUrls: nextUrls,
        imageUrl:
          prev.imageUrl?.trim() === url?.trim()
            ? nextUrls[0] || ""
            : prev.imageUrl,
      };
    });
  };

  const handleReplaceImageUrl = (index) => {
    const current = formData.imageUrls?.[index];
    if (!current) return;

    const newUrl = window.prompt("Yeni görsel URL'si:", current);
    if (!newUrl?.trim() || newUrl.trim() === current) return;

    setFormData((prev) => {
      const nextUrls = [...(prev.imageUrls || [])];
      nextUrls[index] = newUrl.trim();
      return {
        ...prev,
        imageUrls: nextUrls,
        imageUrl:
          prev.imageUrl?.trim() === current?.trim()
            ? newUrl.trim()
            : prev.imageUrl,
      };
    });
  };

  const handleReplaceImageWithFile = (index) => {
    replacingImageIndexRef.current = index;
    replaceImageInputRef.current?.click();
  };

  const handleReplaceImageFileSelect = async (e) => {
    const file = e.target.files?.[0];
    const index = replacingImageIndexRef.current;
    e.target.value = "";

    if (index === null || index === undefined || !validateImageFile(file)) {
      replacingImageIndexRef.current = null;
      return;
    }

    setImageUploading(true);
    try {
      const result = await ProductService.uploadImage(file);
      if (!result?.success || !result?.imageUrl) {
        throw new Error(result?.message || "Resim yüklenemedi");
      }

      const oldUrl = formData.imageUrls?.[index];
      setFormData((prev) => {
        const nextUrls = [...(prev.imageUrls || [])];
        nextUrls[index] = result.imageUrl;
        return {
          ...prev,
          imageUrls: nextUrls,
          imageUrl:
            prev.imageUrl?.trim() === oldUrl?.trim()
              ? result.imageUrl
              : prev.imageUrl,
        };
      });
    } catch (err) {
      alert(
        "Görsel güncellenemedi: " +
          (err.response?.data?.message || err.message),
      );
    } finally {
      setImageUploading(false);
      replacingImageIndexRef.current = null;
    }
  };

  const handleSetPrimaryImage = (index) => {
    setFormData((prev) => {
      const selected = (prev.imageUrls || [])[index];
      if (!selected) return prev;
      return {
        ...prev,
        imageUrl: selected,
      };
    });
  };

  const handleMoveImage = (index, direction) => {
    setFormData((prev) => {
      const urls = [...(prev.imageUrls || [])];
      const targetIndex = index + direction;
      if (targetIndex < 0 || targetIndex >= urls.length) return prev;
      [urls[index], urls[targetIndex]] = [urls[targetIndex], urls[index]];
      return {
        ...prev,
        imageUrls: urls,
      };
    });
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
      uploadImageFiles(files);
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
      // excelImageFiles: kullanıcının seçtiği görsel dosyaları array'i
      const result = await ProductService.importExcel(
        excelFile,
        excelImageFiles,
      );
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

  // Tüm Mikro ürünlerini otomatik kategorize et
  const handleAutoCategorize = async () => {
    if (
      !window.confirm(
        "Tüm Mikro ERP ürünleri otomatik kategorize edilecek. Devam etmek istiyor musunuz?",
      )
    )
      return;

    setCategorizingLoading(true);
    try {
      const api = (await import("../../services/api")).default;
      const response = await api.post("/api/products/admin/auto-categorize");
      const result = response?.data || response;

      let msg = `✅ Otomatik kategorileme tamamlandı!\n`;
      msg += `Toplam: ${result.totalProducts} ürün\n`;
      msg += `Yeni oluşturulan: ${result.created}\n`;
      msg += `Güncellenen: ${result.updated}\n`;
      if (result.newCategories > 0)
        msg += `Yeni kategori: ${result.newCategories}\n`;
      if (result.distribution) {
        msg += `\nKategori dağılımı:\n`;
        result.distribution.forEach((d) => {
          msg += `  ${d.category}: ${d.productCount} ürün\n`;
        });
      }
      alert(msg);

      // Listeyi ve kategorileri yenile
      fetchProducts();
      fetchCategories();
      fetchDuplicateGroups();
    } catch (err) {
      console.error("Otomatik kategorize hatası:", err);
      alert(
        "❌ Kategorize hatası: " + (err.response?.data?.message || err.message),
      );
    } finally {
      setCategorizingLoading(false);
    }
  };

  const getResolvedCategoryId = (product) => {
    const parsedCategoryId = Number.parseInt(
      String(product?.categoryId ?? product?.category?.id ?? ""),
      10,
    );

    return Number.isInteger(parsedCategoryId) ? parsedCategoryId : 0;
  };

  const getResolvedCategoryName = (product) => {
    if (product?.categoryName && String(product.categoryName).trim()) {
      return product.categoryName;
    }

    if (typeof product?.category === "string" && product.category.trim()) {
      return product.category;
    }

    const categoryId = getResolvedCategoryId(product);
    if (categoryId > 0) {
      const matchedCategory = categories.find((category) => category.id === categoryId);
      if (matchedCategory?.name) {
        return matchedCategory.name;
      }
    }

    return "Kategori atanmadı";
  };

  const uncategorizedProducts = useMemo(
    () => products.filter((product) => getResolvedCategoryId(product) <= 0),
    [products],
  );

  const categorizedProducts = useMemo(
    () => products.filter((product) => getResolvedCategoryId(product) > 0),
    [products],
  );

  const visiblePageNumbers = useMemo(() => {
    const maxButtons = 5;
    const startPage = Math.max(1, currentPage - Math.floor(maxButtons / 2));
    const endPage = Math.min(serverTotalPages, startPage + maxButtons - 1);
    const normalizedStart = Math.max(1, endPage - maxButtons + 1);

    return Array.from(
      { length: endPage - normalizedStart + 1 },
      (_, index) => normalizedStart + index,
    );
  }, [currentPage, serverTotalPages]);

  const handleDeactivateDuplicate = async (product) => {
    if (!product?.id) {
      alert("❌ Pasife alınacak ürün kimliği bulunamadı.");
      return;
    }

    const confirmed = window.confirm(
      `${product.name} ürününü pasife almak istediğinize emin misiniz?\n\nBu işlem ürünü silmez, sadece satıştan kaldırır.`,
    );
    if (!confirmed) {
      return;
    }

    try {
      await ProductService.deactivateAdmin(product.id);
      await Promise.all([fetchProducts(), fetchDuplicateGroups()]);
      alert("✅ Ürün pasife alındı.");
    } catch (err) {
      console.error("Ürün pasife alma hatası:", err);
      alert(
        "❌ Ürün pasife alınamadı: " +
          (err.response?.data?.message || err.message),
      );
    }
  };

  const renderProductCards = (items, { emphasizeUncategorized = false } = {}) =>
    items.map((product) => {
      const displayedStock = product.stock ?? product.stockQuantity ?? 0;
      const canDeleteProduct = Number(product.id) > 0;
      const stockBadgeClass =
        displayedStock > 10
          ? "bg-success"
          : displayedStock > 0
            ? "bg-warning text-dark"
            : "bg-danger";
      const productKey = product.id || product.sku || product.name;

      return (
        <div key={productKey} className="col-6 col-md-4 col-lg-3">
          <div
            className="admin-product-card h-100"
            style={
              emphasizeUncategorized
                ? {
                    borderColor: "#fdba74",
                    boxShadow: "0 10px 24px rgba(249, 115, 22, 0.14)",
                  }
                : undefined
            }
          >
            <div className="admin-product-image-wrap">
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
                className={`badge position-absolute top-0 end-0 m-2 admin-product-status-badge ${
                  product.isActive ? "bg-success" : "bg-secondary"
                }`}
              >
                {product.isActive ? "Aktif" : "Pasif"}
              </span>
            </div>

            <div className="p-2">
              <h6
                className="fw-bold mb-1 text-truncate"
                style={{ fontSize: "0.9rem", color: "#1e293b" }}
              >
                {product.name}
              </h6>
              <p
                className="text-muted mb-2 text-truncate admin-product-meta"
                style={{ fontSize: "0.72rem" }}
              >
                {getResolvedCategoryName(product)}
              </p>
              <div className="d-flex justify-content-between align-items-center mb-2">
                <span
                  className="fw-bold"
                  style={{ color: "#f57c00", fontSize: "1rem" }}
                >
                  ₺{product.price?.toFixed(2)}
                </span>
                <span
                  className={`badge admin-product-stock-badge ${stockBadgeClass}`}
                >
                  Stok {displayedStock}
                </span>
              </div>
              <div className="admin-product-actions">
                <button
                  className="btn btn-outline-primary btn-sm flex-fill admin-product-action-btn"
                  onClick={() => handleEdit(product)}
                  title="Düzenle"
                >
                  <i className="fas fa-edit"></i>
                  <span className="admin-product-action-text">Düzenle</span>
                </button>
                <button
                  className="btn btn-outline-success btn-sm admin-product-action-btn"
                  onClick={() => {
                    setSelectedProductForVariants(product);
                    setShowVariantManager(true);
                  }}
                  title="Varyantlar"
                >
                  <i className="fas fa-layer-group"></i>
                  <span className="admin-product-action-text">
                    Varyant Yönet
                  </span>
                </button>
                <button
                  className="btn btn-outline-danger btn-sm admin-product-action-btn"
                  onClick={() => handleDelete(product.id)}
                  title={canDeleteProduct ? "Sil" : "Mikro ürünü doğrudan silinemez"}
                  disabled={!canDeleteProduct}
                >
                  <i className="fas fa-trash"></i>
                  <span className="admin-product-action-text">Sil</span>
                </button>
              </div>
            </div>
          </div>
        </div>
      );
    });

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

  const totalProducts = totalProductsCount;
  const activeProducts = products.filter((product) => product.isActive).length;
  const lowStockProducts = products.filter(
    (product) => (product.stock ?? product.stockQuantity ?? 0) <= 10,
  ).length;
  const totalCategories = categories.length;
  const uncategorizedCount = uncategorizedProducts.length;
  const hasActiveFilters =
    searchFilters.sku.trim().length > 0 ||
    searchFilters.name.trim().length > 0 ||
    searchFilters.status !== "all" ||
    searchFilters.stockStatus !== "all";
  const hasPendingTextFilters =
    draftSearchFilters.sku.trim().length > 0 ||
    draftSearchFilters.name.trim().length > 0;
  const hasAnyActiveFilters = hasActiveFilters || hasPendingTextFilters;

  return (
    <div>
      {/* Mobil için CSS */}
      <style>{`
        .admin-products-hero {
          background: linear-gradient(135deg, #ffffff 0%, #fff7ed 100%);
          border: 1px solid rgba(249, 115, 22, 0.12);
          border-radius: 18px;
          padding: 1rem;
          box-shadow: 0 10px 30px rgba(15, 23, 42, 0.06);
        }

        .admin-products-kicker {
          font-size: 0.72rem;
          font-weight: 700;
          letter-spacing: 0.08em;
          text-transform: uppercase;
          color: #f97316;
          margin-bottom: 0.35rem;
        }

        .admin-products-hero-top {
          display: flex;
          align-items: flex-start;
          justify-content: space-between;
          gap: 1rem;
          margin-bottom: 0.9rem;
        }

        .admin-products-subtitle {
          margin: 0.35rem 0 0;
          color: #64748b;
          font-size: 0.88rem;
          line-height: 1.45;
        }

        .admin-products-summary {
          display: grid;
          grid-template-columns: repeat(2, minmax(84px, 1fr));
          gap: 0.55rem;
          min-width: 180px;
        }

        .admin-products-stat {
          background: rgba(255, 255, 255, 0.88);
          border: 1px solid rgba(226, 232, 240, 0.95);
          border-radius: 14px;
          padding: 0.7rem 0.8rem;
        }

        .admin-products-stat-value {
          display: block;
          font-size: 1rem;
          font-weight: 800;
          color: #0f172a;
          line-height: 1.1;
        }

        .admin-products-stat-label {
          display: block;
          margin-top: 0.2rem;
          color: #64748b;
          font-size: 0.68rem;
          font-weight: 600;
        }

        .admin-products-actions {
          display: grid;
          grid-template-columns: repeat(3, minmax(0, 1fr));
          gap: 0.55rem;
        }

        .admin-products-action-btn {
          min-height: 42px;
          display: inline-flex;
          align-items: center;
          justify-content: center;
          gap: 0.35rem;
          white-space: nowrap;
          box-shadow: 0 8px 18px rgba(15, 23, 42, 0.08);
        }

        .admin-product-card {
          border-radius: 16px !important;
          border: 1px solid #dbe4f0 !important;
          background: linear-gradient(180deg, #ffffff 0%, #fbfdff 100%);
          overflow: hidden;
          box-shadow: 0 8px 24px rgba(15, 23, 42, 0.05);
          transition: transform 0.2s ease, box-shadow 0.2s ease;
        }

        .admin-product-card:hover {
          transform: translateY(-2px);
          box-shadow: 0 14px 32px rgba(15, 23, 42, 0.08);
        }

        .admin-product-image-wrap {
          position: relative;
          background: linear-gradient(180deg, #f8fafc 0%, #f1f5f9 100%);
          border-bottom: 1px solid #eef2f7;
        }

        .admin-product-status-badge {
          font-size: 0.76rem !important;
          font-weight: 700 !important;
          padding: 0.38rem 0.68rem !important;
          border-radius: 999px !important;
          letter-spacing: 0.01em;
          box-shadow: 0 6px 14px rgba(15, 23, 42, 0.1);
        }

        .admin-product-stock-badge {
          display: inline-flex;
          align-items: center;
          justify-content: center;
          font-size: 0.78rem !important;
          font-weight: 700 !important;
          padding: 0.38rem 0.72rem !important;
          border-radius: 999px !important;
          min-width: 74px;
          box-shadow: 0 6px 14px rgba(15, 23, 42, 0.08);
        }

        .admin-product-meta {
          min-height: 34px;
        }

        .admin-product-actions {
          display: grid;
          grid-template-columns: repeat(3, minmax(0, 1fr));
          gap: 0.55rem;
          align-items: stretch;
        }

        .admin-product-action-btn {
          display: inline-flex !important;
          align-items: center;
          justify-content: center;
          flex-direction: column;
          gap: 0.35rem;
          min-height: 70px;
          width: 100%;
          border-radius: 14px !important;
          font-size: 0.72rem !important;
          font-weight: 700 !important;
          padding: 0.7rem 0.45rem !important;
          text-transform: none !important;
          letter-spacing: 0.01em;
          background: #ffffff;
          border-width: 1px !important;
          box-shadow: 0 6px 14px rgba(15, 23, 42, 0.04);
        }

        .admin-product-action-btn.btn-outline-primary {
          background: linear-gradient(180deg, #f8fbff 0%, #eef5ff 100%);
          border-color: #bfdbfe !important;
        }

        .admin-product-action-btn.btn-outline-success {
          background: linear-gradient(180deg, #f3fff8 0%, #ebfbf2 100%);
          border-color: #bbf7d0 !important;
        }

        .admin-product-action-btn.btn-outline-danger {
          background: linear-gradient(180deg, #fff7f7 0%, #fff0f0 100%);
          border-color: #fecaca !important;
        }

        .admin-product-action-btn i {
          font-size: 1rem;
        }

        .admin-product-action-text {
          display: block;
          width: 100%;
          text-align: center;
          line-height: 1.1;
          white-space: normal;
          word-break: break-word;
        }

        .admin-products-pagination {
          display: flex;
          align-items: center;
          justify-content: center;
          gap: 0.5rem;
          flex-wrap: wrap;
        }

        .admin-products-page-nav,
        .admin-products-page-btn {
          min-width: 42px;
          height: 38px;
          display: inline-flex !important;
          align-items: center;
          justify-content: center;
          border-radius: 10px !important;
          line-height: 1;
          font-weight: 700;
        }

        .admin-products-page-btn {
          padding: 0 0.8rem !important;
        }

        .admin-products-page-nav {
          padding: 0 0.9rem !important;
          white-space: nowrap;
        }

        @media (max-width: 576px) {
          .admin-products-hero {
            padding: 0.8rem;
            border-radius: 16px;
          }

          .admin-products-hero-top {
            flex-direction: column;
            gap: 0.75rem;
            margin-bottom: 0.75rem;
          }

          .admin-products-summary {
            width: 100%;
            min-width: 0;
          }

          .admin-products-actions {
            grid-template-columns: repeat(2, minmax(0, 1fr));
          }

          .admin-products-action-btn {
            font-size: 0.68rem !important;
            min-height: 40px;
            padding: 0.45rem 0.35rem !important;
            white-space: normal;
            line-height: 1.15;
          }

          .admin-product-card img { height: 60px !important; }
          .admin-product-card .p-2 { padding: 0.65rem !important; }
          .admin-product-card h6 { font-size: 0.85rem !important; }
          .admin-product-card .fw-bold { font-size: 0.86rem !important; }
          .admin-product-status-badge {
            font-size: 0.68rem !important;
            padding: 0.28rem 0.56rem !important;
          }
          .admin-product-stock-badge {
            min-width: 64px;
            font-size: 0.7rem !important;
            padding: 0.3rem 0.55rem !important;
          }
          .admin-product-actions {
            gap: 0.4rem;
          }
          .admin-product-action-btn {
            gap: 0.18rem;
            min-height: 58px;
            padding: 0.45rem 0.2rem !important;
            font-size: 0.58rem !important;
            border-radius: 12px !important;
          }
          .admin-product-action-btn i {
            font-size: 0.82rem;
          }
          .admin-product-action-text {
            line-height: 1.05;
            letter-spacing: 0;
          }

          .admin-products-pagination {
            width: 100%;
            gap: 0.35rem;
          }
        }

        @media (min-width: 577px) and (max-width: 991.98px) {
          .admin-product-action-btn {
            min-height: 64px;
            padding: 0.6rem 0.3rem !important;
            font-size: 0.64rem !important;
          }

          .admin-product-action-btn i {
            font-size: 0.9rem;
          }
        }

        @media (min-width: 992px) {
          .admin-product-actions {
            gap: 0.65rem;
          }

          .admin-product-action-btn {
            min-height: 74px;
            padding: 0.8rem 0.5rem !important;
          }

          .admin-product-action-text {
            font-size: 0.74rem;
          }
        }
      `}</style>
      <div className="container-fluid px-2">
        <div className="admin-products-hero mb-3">
          <div className="admin-products-hero-top">
            <div>
              <div className="admin-products-kicker">Katalog Yönetimi</div>
              <h5
                className="mb-0 fw-bold"
                style={{ color: "#1e293b", fontSize: "1.05rem" }}
              >
                <i className="fas fa-box me-2" style={{ color: "#f97316" }}></i>
                Ürünler
              </h5>
              <p className="admin-products-subtitle">
                Ürünleri düzenleyin, içe ve dışa aktarım işlemlerini yönetin,
                stok ve kategori durumunu tek alandan izleyin.
              </p>
            </div>

            <div className="admin-products-summary">
              <div className="admin-products-stat">
                <span className="admin-products-stat-value">
                  {totalProducts}
                </span>
                <span className="admin-products-stat-label">Toplam Ürün</span>
              </div>
              <div className="admin-products-stat">
                <span className="admin-products-stat-value">
                  {activeProducts}
                </span>
                <span className="admin-products-stat-label">Aktif</span>
              </div>
              <div className="admin-products-stat">
                <span className="admin-products-stat-value">
                  {lowStockProducts}
                </span>
                <span className="admin-products-stat-label">Düşük Stok</span>
              </div>
              <div className="admin-products-stat">
                <span className="admin-products-stat-value">
                  {totalCategories}
                </span>
                <span className="admin-products-stat-label">Kategori</span>
              </div>
            </div>
          </div>

          <div
            className="border-0 p-3 mb-3"
            style={{
              background: "rgba(15, 23, 42, 0.04)",
              borderRadius: "16px",
            }}
          >
            <div className="d-flex flex-column flex-lg-row justify-content-between gap-3 align-items-lg-start">
              <div>
                <div className="fw-semibold mb-2" style={{ color: "#1e293b" }}>
                  Mikro'dan Bağımsız Alanlar - Tüm Ürünler İçin Varsayılan
                </div>
                <div className="text-muted" style={{ fontSize: "0.88rem" }}>
                  Ürün bazında özel seçim yapılmadıysa bu varsayılanlar kullanılır.
                  Üründe açıkça bir seçim yaptıysanız önce ürün ayarı geçerlidir.
                </div>
              </div>

              <div className="d-flex align-items-center gap-2">
                {globalOverrideLoading ? (
                  <span className="text-muted small">Yükleniyor...</span>
                ) : null}
                <button
                  type="button"
                  className="btn btn-sm btn-primary"
                  onClick={handleSaveGlobalOverrideSettings}
                  disabled={globalOverrideSaving || globalOverrideLoading}
                >
                  {globalOverrideSaving ? "Kaydediliyor..." : "Genel Ayarı Kaydet"}
                </button>
              </div>
            </div>

            <div className="row g-2 mt-1">
              <div className="col-md-4">
                <div className="form-check form-switch">
                  <input
                    id="globalAdminOverrideName"
                    className="form-check-input"
                    type="checkbox"
                    checked={globalOverrideSettings.defaultAdminOverrideName === true}
                    onChange={(e) =>
                      setGlobalOverrideSettings((prev) => ({
                        ...prev,
                        defaultAdminOverrideName: e.target.checked,
                      }))
                    }
                  />
                  <label className="form-check-label" htmlFor="globalAdminOverrideName">
                    İsim Mikro'dan bağımsız
                  </label>
                </div>
              </div>
              <div className="col-md-4">
                <div className="form-check form-switch">
                  <input
                    id="globalAdminOverridePrice"
                    className="form-check-input"
                    type="checkbox"
                    checked={globalOverrideSettings.defaultAdminOverridePrice === true}
                    onChange={(e) =>
                      setGlobalOverrideSettings((prev) => ({
                        ...prev,
                        defaultAdminOverridePrice: e.target.checked,
                      }))
                    }
                  />
                  <label className="form-check-label" htmlFor="globalAdminOverridePrice">
                    Fiyat Mikro'dan bağımsız
                  </label>
                </div>
              </div>
              <div className="col-md-4">
                <div className="form-check form-switch">
                  <input
                    id="globalAdminOverrideCategory"
                    className="form-check-input"
                    type="checkbox"
                    checked={globalOverrideSettings.defaultAdminOverrideCategory === true}
                    onChange={(e) =>
                      setGlobalOverrideSettings((prev) => ({
                        ...prev,
                        defaultAdminOverrideCategory: e.target.checked,
                      }))
                    }
                  />
                  <label className="form-check-label" htmlFor="globalAdminOverrideCategory">
                    Kategori Mikro'dan bağımsız
                  </label>
                </div>
              </div>
            </div>
          </div>

          <div className="admin-products-actions">
            <button
              className="btn border-0 text-white fw-medium px-2 py-1 admin-products-action-btn"
              style={{
                background:
                  duplicateGroups.length > 0
                    ? "linear-gradient(135deg, #ef4444, #f97316)"
                    : "linear-gradient(135deg, #64748b, #94a3b8)",
                borderRadius: "6px",
                fontSize: "0.75rem",
              }}
              onClick={() => setShowDuplicatePanel((prev) => !prev)}
              disabled={duplicateLoading && duplicateGroups.length === 0}
            >
              {duplicateLoading && duplicateGroups.length === 0 ? (
                <>
                  <span className="spinner-border spinner-border-sm me-1"></span>
                  Mükerrerler Taranıyor...
                </>
              ) : (
                <>
                  <i className="fas fa-clone me-1"></i>
                  Mükerrer Ürünler
                  {duplicateGroups.length > 0 ? ` (${duplicateGroups.length})` : ""}
                </>
              )}
            </button>
            {/* Excel'den Yükle Butonu */}
            <button
              className="btn border-0 text-white fw-medium px-2 py-1 admin-products-action-btn"
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
              className="btn border-0 text-white fw-medium px-2 py-1 admin-products-action-btn"
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
              className="btn border-0 text-white fw-medium px-2 py-1 admin-products-action-btn"
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
              className="btn border-0 text-white fw-medium px-2 py-1 admin-products-action-btn"
              style={{
                background: "linear-gradient(135deg, #6366f1, #818cf8)",
                borderRadius: "6px",
                fontSize: "0.75rem",
              }}
              onClick={handleDownloadTemplate}
            >
              <i className="fas fa-download me-1"></i>Şablon
            </button>
            {/* Otomatik Kategorize Butonu */}
            <button
              className="btn border-0 text-white fw-medium px-2 py-1 admin-products-action-btn"
              style={{
                background: "linear-gradient(135deg, #ec4899, #f472b6)",
                borderRadius: "6px",
                fontSize: "0.75rem",
              }}
              onClick={handleAutoCategorize}
              disabled={categorizingLoading}
            >
              {categorizingLoading ? (
                <>
                  <span className="spinner-border spinner-border-sm me-1"></span>
                  Kategorize ediliyor...
                </>
              ) : (
                <>
                  <i className="fas fa-tags me-1"></i>Otomatik Kategorize
                </>
              )}
            </button>
            {/* Yeni Ürün Ekle Butonu */}
            <button
              className="btn border-0 text-white fw-medium px-2 py-1 admin-products-action-btn"
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
                  specialPrice: "",
                  stock: "",
                  description: "",
                  imageUrl: "",
                  imageUrls: [],
                  isActive: true,
                  adminOverrideName: null,
                  adminOverridePrice: null,
                  adminOverrideCategory: null,
                });
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

          <div
            className="border-0 p-3 mt-3"
            style={{
              background: "rgba(255, 255, 255, 0.78)",
              borderRadius: "16px",
              border: "1px solid rgba(226, 232, 240, 0.9)",
            }}
          >
            <div className="row g-3 align-items-end">
              <div className="col-12 col-lg-4">
                <label className="form-label fw-semibold mb-2">Ürün Kodu</label>
                <input
                  type="text"
                  className="form-control"
                  placeholder="SKU veya stok kodu ile ara"
                  value={draftSearchFilters.sku}
                  onChange={(e) => {
                    setDraftSearchFilters((prev) => ({
                      ...prev,
                      sku: e.target.value,
                    }));
                  }}
                />
              </div>
              <div className="col-12 col-lg-5">
                <label className="form-label fw-semibold mb-2">Ürün İsmi</label>
                <input
                  type="text"
                  className="form-control"
                  placeholder="Ürün adına göre filtrele"
                  value={draftSearchFilters.name}
                  onChange={(e) => {
                    setDraftSearchFilters((prev) => ({
                      ...prev,
                      name: e.target.value,
                    }));
                  }}
                />
              </div>
              <div className="col-6 col-lg-2">
                <label className="form-label fw-semibold mb-2">Sayfa Boyutu</label>
                <select
                  className="form-select"
                  value={pageSize}
                  onChange={(e) => {
                    setCurrentPage(1);
                    setPageSize(Number(e.target.value));
                  }}
                >
                  <option value={12}>12</option>
                  <option value={24}>24</option>
                  <option value={48}>48</option>
                  <option value={96}>96</option>
                </select>
              </div>
              <div className="col-12 col-lg-4">
                <div className="row g-3">
                  <div className="col-sm-6">
                    <label className="form-label fw-semibold mb-2">Durum</label>
                    <select
                      className="form-select"
                      value={searchFilters.status}
                      onChange={(e) => {
                        setCurrentPage(1);
                        setSearchFilters((prev) => ({
                          ...prev,
                          status: e.target.value,
                        }));
                      }}
                    >
                      <option value="all">Tümü</option>
                      <option value="active">Aktif</option>
                      <option value="inactive">Pasif</option>
                    </select>
                  </div>
                  <div className="col-sm-6">
                    <label className="form-label fw-semibold mb-2">Stok Durumu</label>
                    <select
                      className="form-select"
                      value={searchFilters.stockStatus}
                      onChange={(e) => {
                        setCurrentPage(1);
                        setSearchFilters((prev) => ({
                          ...prev,
                          stockStatus: e.target.value,
                        }));
                      }}
                    >
                      <option value="all">Tümü</option>
                      <option value="in-stock">Stokta Var</option>
                      <option value="out-of-stock">Stokta Yok</option>
                    </select>
                  </div>
                </div>
              </div>
              <div className="col-12 col-lg-1 d-grid">
                <button
                  type="button"
                  className="btn btn-outline-secondary"
                  onClick={() => {
                    setDraftSearchFilters({
                      sku: "",
                      name: "",
                    });
                    setSearchFilters({
                      sku: "",
                      name: "",
                      status: "all",
                      stockStatus: "all",
                    });
                    setCurrentPage(1);
                  }}
                  disabled={!hasAnyActiveFilters}
                >
                  Temizle
                </button>
              </div>
            </div>

            <div className="d-flex flex-column flex-lg-row justify-content-between gap-2 mt-3 text-muted">
              <small>
                {totalProductsCount} sonuç bulundu.
                {hasAnyActiveFilters ? " Filtrelenmiş görünüm aktif." : ""}
              </small>
              <small>
                Sayfa {currentPage} / {serverTotalPages}
              </small>
            </div>
          </div>
        </div>

        {showDuplicatePanel && (
          <div
            className="alert border-0 mb-3"
            style={{
              background: "linear-gradient(135deg, #fff1f2 0%, #ffe4e6 100%)",
              borderRadius: "18px",
              boxShadow: "0 10px 28px rgba(225, 29, 72, 0.12)",
            }}
          >
            <div className="d-flex flex-column flex-lg-row align-items-lg-center justify-content-between gap-3 mb-3">
              <div>
                <div className="fw-bold mb-1" style={{ color: "#9f1239" }}>
                  <i className="fas fa-clone me-2"></i>
                  {duplicateGroups.length > 0
                    ? "Mükerrer ürün grupları tespit edildi"
                    : "Mükerrer ürün taraması"}
                </div>
                <div style={{ color: "#881337", fontSize: "0.92rem" }}>
                  {duplicateGroups.length > 0
                    ? `${duplicateGroups.length} grup tıpatıp aynı ürün bulundu. Pasife alma işleminden önce aktif kalacak ürünü doğrulayın.`
                    : "Şu anda tespit edilmiş mükerrer ürün grubu bulunmuyor."}
                </div>
              </div>
              <span className="badge rounded-pill text-bg-danger px-3 py-2">
                {duplicateLoading ? "Taranıyor..." : `${duplicateGroups.length} grup`}
              </span>
            </div>

            {duplicateGroups.length > 0 ? (
              <div className="row g-3">
                {duplicateGroups.map((group) => (
                  <div key={group.groupKey} className="col-12 col-xl-6">
                    <div
                      className="bg-white h-100"
                      style={{
                        borderRadius: "14px",
                        border: "1px solid rgba(244, 63, 94, 0.16)",
                        padding: "0.9rem",
                      }}
                    >
                      <div className="d-flex justify-content-between align-items-center mb-2">
                        <div>
                          <div className="fw-bold" style={{ color: "#9f1239" }}>
                            {group.reason}
                          </div>
                          <small className="text-muted">{group.groupKey}</small>
                        </div>
                        <span className="badge text-bg-light">
                          {group.products?.length || 0} ürün
                        </span>
                      </div>

                      <div className="d-flex flex-column gap-2">
                        {(group.products || []).map((product) => (
                          <div
                            key={`${group.groupKey}-${product.id}`}
                            className="d-flex justify-content-between align-items-start gap-3"
                            style={{
                              border: "1px solid #ffe4e6",
                              borderRadius: "12px",
                              padding: "0.7rem 0.8rem",
                              background: product.isActive ? "#fffafb" : "#f8fafc",
                            }}
                          >
                            <div>
                              <div className="fw-semibold" style={{ color: "#1e293b" }}>
                                {product.name}
                              </div>
                              <small className="text-muted d-block">
                                SKU: {product.sku || "-"}
                                {product.barcode ? ` | Barkod: ${product.barcode}` : ""}
                              </small>
                              <small className="text-muted d-block">
                                Kategori: {getResolvedCategoryName(product)} | Fiyat: ₺
                                {Number(product.price || 0).toFixed(2)}
                              </small>
                            </div>

                            <div className="text-end">
                              <span
                                className={`badge ${product.isActive ? "text-bg-success" : "text-bg-secondary"}`}
                              >
                                {product.isActive ? "Aktif" : "Pasif"}
                              </span>
                              {product.isActive && (
                                <button
                                  className="btn btn-sm btn-outline-danger d-block mt-2"
                                  onClick={() => handleDeactivateDuplicate(product)}
                                >
                                  <i className="fas fa-eye-slash me-1"></i>
                                  Pasife Al
                                </button>
                              )}
                            </div>
                          </div>
                        ))}
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            ) : (
              <div
                className="bg-white"
                style={{ borderRadius: "14px", padding: "1rem", color: "#475569" }}
              >
                Tespit edilen mükerrer ürün yok.
              </div>
            )}
          </div>
        )}

        {uncategorizedCount > 0 && (
          <div
            className="alert border-0 mb-3"
            style={{
              background: "linear-gradient(135deg, #fff7ed 0%, #ffedd5 100%)",
              borderRadius: "18px",
              boxShadow: "0 10px 28px rgba(249, 115, 22, 0.12)",
            }}
          >
            <div className="d-flex flex-column flex-lg-row align-items-lg-center justify-content-between gap-3">
              <div>
                <div className="fw-bold mb-1" style={{ color: "#9a3412" }}>
                  <i className="fas fa-exclamation-triangle me-2"></i>
                  Kategorilendirilmemiş ürünler tespit edildi
                </div>
                <div style={{ color: "#7c2d12", fontSize: "0.92rem" }}>
                  {uncategorizedCount} ürün henüz kategoriye bağlı değil. Bu
                  ürünler aşağıda ayrı bölümde gösteriliyor.
                </div>
              </div>
              <button
                className="btn text-white fw-semibold"
                style={{
                  background: "linear-gradient(135deg, #f97316, #ea580c)",
                  borderRadius: "12px",
                  minWidth: "220px",
                }}
                onClick={handleAutoCategorize}
                disabled={categorizingLoading}
              >
                {categorizingLoading ? (
                  <>
                    <span className="spinner-border spinner-border-sm me-2"></span>
                    Kategorize ediliyor...
                  </>
                ) : (
                  <>
                    <i className="fas fa-magic me-2"></i>
                    Kategorileri Otomatik Ata
                  </>
                )}
              </button>
            </div>
          </div>
        )}

        {products.length === 0 ? (
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
        ) : totalProductsCount === 0 ? (
          <div className="text-center py-5">
            <i
              className="fas fa-search fa-4x text-muted mb-3"
              style={{ opacity: 0.25 }}
            ></i>
            <h4 className="text-muted mb-2">Filtreye uygun ürün bulunamadı</h4>
            <p className="text-muted">
              Ürün kodu veya ürün ismi filtresini değiştirip tekrar deneyin.
            </p>
          </div>
        ) : (
          <>
            {uncategorizedProducts.length > 0 && (
              <section className="mb-4">
                <div className="d-flex align-items-center justify-content-between mb-2">
                  <div>
                    <h6 className="mb-1 fw-bold" style={{ color: "#9a3412" }}>
                      Kategorilendirilmemiş Ürünler
                    </h6>
                    <p className="mb-0 text-muted" style={{ fontSize: "0.85rem" }}>
                      Öncelikli kontrol gerektiren ürünler burada listelenir.
                    </p>
                  </div>
                  <span className="badge rounded-pill text-bg-warning px-3 py-2">
                    {uncategorizedProducts.length} ürün
                  </span>
                </div>
                <div className="row g-2">
                  {renderProductCards(uncategorizedProducts, {
                    emphasizeUncategorized: true,
                  })}
                </div>
              </section>
            )}

            {categorizedProducts.length > 0 && (
              <section>
                {uncategorizedCount > 0 && (
                  <div className="d-flex align-items-center justify-content-between mb-2">
                    <div>
                      <h6 className="mb-1 fw-bold" style={{ color: "#1e293b" }}>
                        Kategorilendirilmiş Ürünler
                      </h6>
                      <p className="mb-0 text-muted" style={{ fontSize: "0.85rem" }}>
                        Kategori ataması tamamlanan ürünler.
                      </p>
                    </div>
                    <span className="badge rounded-pill text-bg-light px-3 py-2">
                      {categorizedProducts.length} ürün
                    </span>
                  </div>
                )}
                <div className="row g-2">
                  {renderProductCards(categorizedProducts)}
                </div>
              </section>
            )}

            <div
              className="d-flex flex-column flex-md-row align-items-center justify-content-between gap-3 mt-4"
            >
              <div className="text-muted small">
                {totalProductsCount === 0 ? 0 : (currentPage - 1) * pageSize + 1}
                -
                {Math.min(currentPage * pageSize, totalProductsCount)} arası gösteriliyor
              </div>
              <div className="admin-products-pagination">
                <button
                  type="button"
                  className="btn btn-outline-secondary btn-sm admin-products-page-nav"
                  onClick={() => setCurrentPage((prev) => Math.max(prev - 1, 1))}
                  disabled={currentPage === 1}
                >
                  <i className="fas fa-chevron-left me-1"></i>Önceki
                </button>
                {visiblePageNumbers.map((pageNumber) => (
                  <button
                    key={pageNumber}
                    type="button"
                    className={`btn btn-sm admin-products-page-btn ${pageNumber === currentPage ? "btn-primary" : "btn-outline-secondary"}`}
                    onClick={() => setCurrentPage(pageNumber)}
                  >
                    {pageNumber}
                  </button>
                ))}
                <button
                  type="button"
                  className="btn btn-outline-secondary btn-sm admin-products-page-nav"
                  onClick={() =>
                    setCurrentPage((prev) => Math.min(prev + 1, serverTotalPages))
                  }
                  disabled={currentPage === serverTotalPages}
                >
                  Sonraki<i className="fas fa-chevron-right ms-1"></i>
                </button>
              </div>
            </div>
          </>
        )}

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

                      <div className="col-12">
                        <div
                          className="border-0 p-3"
                          style={{
                            background: "rgba(15, 23, 42, 0.04)",
                            borderRadius: "14px",
                          }}
                        >
                          <div className="fw-semibold mb-2" style={{ color: "#1e293b" }}>
                            Mikro'dan Bağımsız Alanlar
                          </div>
                          <div className="text-muted mb-3" style={{ fontSize: "0.88rem" }}>
                            Ürün ayarı önce uygulanır. "Geneli kullan" seçerseniz yukarıdaki tüm ürünler varsayılanı devreye girer.
                          </div>
                          <div className="row g-2">
                            <div className="col-md-4">
                              <label className="form-label fw-semibold">İsim</label>
                              <select
                                id="adminOverrideName"
                                className="form-select"
                                value={
                                  formData.adminOverrideName === true
                                    ? "admin"
                                    : formData.adminOverrideName === false
                                      ? "mikro"
                                      : "inherit"
                                }
                                onChange={(e) =>
                                  handleProductOverrideModeChange(
                                    "adminOverrideName",
                                    e.target.value,
                                  )
                                }
                              >
                                <option value="inherit">Geneli kullan</option>
                                <option value="admin">Mikro'dan bağımsız</option>
                                <option value="mikro">Mikro güncellesin</option>
                              </select>
                              <small className="text-muted d-block mt-1">
                                Etkin sonuç: {resolveEffectiveOverride("adminOverrideName", formData.adminOverrideName) ? "Admin değeri korunur" : "Mikro günceller"}
                              </small>
                            </div>
                            <div className="col-md-4">
                              <label className="form-label fw-semibold">Fiyat</label>
                              <select
                                id="adminOverridePrice"
                                className="form-select"
                                value={
                                  formData.adminOverridePrice === true
                                    ? "admin"
                                    : formData.adminOverridePrice === false
                                      ? "mikro"
                                      : "inherit"
                                }
                                onChange={(e) =>
                                  handleProductOverrideModeChange(
                                    "adminOverridePrice",
                                    e.target.value,
                                  )
                                }
                              >
                                <option value="inherit">Geneli kullan</option>
                                <option value="admin">Mikro'dan bağımsız</option>
                                <option value="mikro">Mikro güncellesin</option>
                              </select>
                              <small className="text-muted d-block mt-1">
                                Etkin sonuç: {resolveEffectiveOverride("adminOverridePrice", formData.adminOverridePrice) ? "Admin fiyatı korunur" : "Mikro fiyatı günceller"}
                              </small>
                            </div>
                            <div className="col-md-4">
                              <label className="form-label fw-semibold">Kategori</label>
                              <select
                                id="adminOverrideCategory"
                                className="form-select"
                                value={
                                  formData.adminOverrideCategory === true
                                    ? "admin"
                                    : formData.adminOverrideCategory === false
                                      ? "mikro"
                                      : "inherit"
                                }
                                onChange={(e) =>
                                  handleProductOverrideModeChange(
                                    "adminOverrideCategory",
                                    e.target.value,
                                  )
                                }
                              >
                                <option value="inherit">Geneli kullan</option>
                                <option value="admin">Mikro'dan bağımsız</option>
                                <option value="mikro">Mikro güncellesin</option>
                              </select>
                              <small className="text-muted d-block mt-1">
                                Etkin sonuç: {resolveEffectiveOverride("adminOverrideCategory", formData.adminOverrideCategory) ? "Admin kategorisi korunur" : "Mikro kategorisi günceller"}
                              </small>
                            </div>
                          </div>
                        </div>
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
                          İndirimli Fiyat (₺)
                        </label>
                        <input
                          type="number"
                          step="0.01"
                          min="0"
                          className="form-control border-0 py-3"
                          style={{
                            background: "rgba(245, 124, 0, 0.05)",
                            borderRadius: "12px",
                          }}
                          value={formData.specialPrice}
                          onChange={(e) =>
                            setFormData({
                              ...formData,
                              specialPrice: e.target.value,
                            })
                          }
                          placeholder="Boş bırakılırsa indirim uygulanmaz"
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
                          Ürün Görselleri
                          {(formData.imageUrls?.length || 0) > 0 && (
                            <span className="text-muted ms-2">
                              ({formData.imageUrls.length} fotoğraf)
                            </span>
                          )}
                        </label>

                        {(formData.imageUrls?.length || 0) > 0 && (
                          <div className="row g-3 mb-3">
                            {formData.imageUrls.map((url, index) => {
                              const isPrimary =
                                formData.imageUrl?.trim() === url?.trim();
                              return (
                                <div
                                  key={`${url}-${index}`}
                                  className="col-6 col-md-4 col-lg-3"
                                >
                                  <div
                                    style={{
                                      border: isPrimary
                                        ? "2px solid #f57c00"
                                        : "1px solid #e2e8f0",
                                      borderRadius: "12px",
                                      background: "#fff",
                                      boxShadow: "0 2px 8px rgba(15,23,42,0.06)",
                                    }}
                                  >
                                    <div style={{ position: "relative" }}>
                                      <img
                                        src={url}
                                        alt={`Ürün görseli ${index + 1}`}
                                        style={{
                                          width: "100%",
                                          height: "140px",
                                          objectFit: "cover",
                                          borderRadius: "11px 11px 0 0",
                                          display: "block",
                                        }}
                                        onError={(e) => {
                                          e.target.src = "/images/placeholder.png";
                                        }}
                                      />

                                      {isPrimary && (
                                        <span
                                          className="badge position-absolute"
                                          style={{
                                            top: 8,
                                            left: 8,
                                            background: "#f57c00",
                                          }}
                                        >
                                          Kapak
                                        </span>
                                      )}

                                      <div
                                        className="position-absolute d-flex flex-column gap-1"
                                        style={{ top: 8, right: 8, zIndex: 2 }}
                                      >
                                        <button
                                          type="button"
                                          className="btn btn-sm btn-light border-0 shadow-sm"
                                          style={{
                                            width: 32,
                                            height: 32,
                                            padding: 0,
                                            borderRadius: 8,
                                          }}
                                          onClick={() =>
                                            handleReplaceImageWithFile(index)
                                          }
                                          title="Dosyayla değiştir"
                                          disabled={imageUploading}
                                        >
                                          <i className="fas fa-pen text-primary"></i>
                                        </button>
                                        <button
                                          type="button"
                                          className="btn btn-sm btn-danger shadow-sm"
                                          style={{
                                            width: 32,
                                            height: 32,
                                            padding: 0,
                                            borderRadius: 8,
                                          }}
                                          onClick={() =>
                                            handleRemoveImage(index)
                                          }
                                          title="Sil"
                                        >
                                          <i className="fas fa-trash"></i>
                                        </button>
                                      </div>
                                    </div>

                                    <div className="d-flex flex-wrap gap-1 p-2 border-top bg-light">
                                      {!isPrimary && (
                                        <button
                                          type="button"
                                          className="btn btn-sm btn-outline-primary"
                                          onClick={() =>
                                            handleSetPrimaryImage(index)
                                          }
                                          title="Kapak yap"
                                        >
                                          <i className="fas fa-star me-1"></i>
                                          Kapak
                                        </button>
                                      )}
                                      <button
                                        type="button"
                                        className="btn btn-sm btn-outline-secondary"
                                        onClick={() =>
                                          handleReplaceImageUrl(index)
                                        }
                                        title="URL düzenle"
                                      >
                                        <i className="fas fa-link me-1"></i>
                                        URL
                                      </button>
                                      <button
                                        type="button"
                                        className="btn btn-sm btn-outline-secondary"
                                        onClick={() =>
                                          handleMoveImage(index, -1)
                                        }
                                        disabled={index === 0}
                                        title="Sola taşı"
                                      >
                                        <i className="fas fa-arrow-left"></i>
                                      </button>
                                      <button
                                        type="button"
                                        className="btn btn-sm btn-outline-secondary"
                                        onClick={() =>
                                          handleMoveImage(index, 1)
                                        }
                                        disabled={
                                          index === formData.imageUrls.length - 1
                                        }
                                        title="Sağa taşı"
                                      >
                                        <i className="fas fa-arrow-right"></i>
                                      </button>
                                    </div>
                                  </div>
                                </div>
                              );
                            })}
                          </div>
                        )}

                        <input
                          ref={replaceImageInputRef}
                          type="file"
                          accept="image/jpeg,image/png,image/gif,image/webp"
                          style={{ display: "none" }}
                          onChange={handleReplaceImageFileSelect}
                        />

                        {/* Drag & Drop Alanı */}
                        <div
                          ref={dropZoneRef}
                          onDragEnter={handleDragEnter}
                          onDragLeave={handleDragLeave}
                          onDragOver={handleDragOver}
                          onDrop={handleDrop}
                          onClick={() => !imageUploading && imageInputRef.current?.click()}
                          style={{
                            border: isDragging
                              ? "3px dashed #f57c00"
                              : "2px dashed #e2e8f0",
                            borderRadius: "16px",
                            padding: "24px",
                            textAlign: "center",
                            cursor: imageUploading ? "wait" : "pointer",
                            transition: "all 0.3s ease",
                            background: isDragging
                              ? "rgba(245, 124, 0, 0.1)"
                              : "rgba(245, 124, 0, 0.02)",
                            transform: isDragging ? "scale(1.02)" : "scale(1)",
                            opacity: imageUploading ? 0.7 : 1,
                          }}
                        >
                          <div className="py-2">
                            {imageUploading ? (
                              <>
                                <span className="spinner-border text-warning mb-2"></span>
                                <p className="mb-0 fw-medium" style={{ color: "#64748b" }}>
                                  Görseller yükleniyor...
                                </p>
                              </>
                            ) : (
                              <>
                                <i
                                  className="fas fa-cloud-upload-alt"
                                  style={{
                                    fontSize: "42px",
                                    color: isDragging ? "#f57c00" : "#94a3b8",
                                  }}
                                ></i>
                                <p className="mt-3 mb-1 fw-medium" style={{ color: "#64748b" }}>
                                  {isDragging
                                    ? "Bırakarak yükle"
                                    : "Fotoğraf sürükleyin veya tıklayın"}
                                </p>
                                <small className="text-muted">
                                  JPG, PNG, GIF, WEBP (Maks. 10MB) — Görselleri
                                  düzenlemek/silmek için kaydetmeden önce kart
                                  üzerindeki butonları kullanın
                                </small>
                              </>
                            )}
                          </div>

                          <input
                            ref={imageInputRef}
                            type="file"
                            accept="image/jpeg,image/png,image/gif,image/webp"
                            multiple
                            style={{ display: "none" }}
                            onChange={handleImageSelect}
                          />
                        </div>

                        <div className="mt-2 text-center">
                          <small
                            className="text-primary"
                            style={{ cursor: "pointer" }}
                            onClick={(e) => {
                              e.stopPropagation();
                              const url = prompt("Resim URL'si girin:");
                              if (url?.trim()) {
                                appendUploadedImages([url.trim()]);
                              }
                            }}
                          >
                            <i className="fas fa-edit me-1"></i>
                            Manuel URL ekle
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
                    onClick={() => {
                      setShowExcelModal(false);
                      setExcelFile(null);
                      setExcelImageFiles([]);
                      setExcelResult(null);
                      setExcelError(null);
                    }}
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
                    <p className="mb-1 small">
                      <strong>Sütunlar:</strong> Ürün Adı, Açıklama, Fiyat,
                      Stok, Kategori ID, Görsel URL, İndirimli Fiyat
                    </p>
                    <div className="mt-2 small">
                      <strong>Görsel URL seçenekleri:</strong>
                      <ul className="mb-0 mt-1 ps-3">
                        <li>
                          <code>https://...</code> — URL'den otomatik indirilir
                        </li>
                        <li>
                          <code>urun.jpg</code> — aşağıdan görsel seçin, dosya
                          adıyla eşleştirilir
                        </li>
                        <li>
                          <code>/uploads/products/urun.jpg</code> — mevcut
                          sisteme bağlanır
                        </li>
                      </ul>
                    </div>
                  </div>

                  {/* Dosya seçimi */}
                  <form onSubmit={handleExcelUpload}>
                    <div className="mb-3">
                      <label className="form-label fw-semibold">
                        Excel / CSV Dosyası
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

                    {/* Görsel dosyaları seçimi */}
                    <div className="mb-4">
                      <label className="form-label fw-semibold">
                        Görsel Dosyaları{" "}
                        <span className="text-muted fw-normal">
                          (isteğe bağlı)
                        </span>
                      </label>
                      <input
                        type="file"
                        className="form-control"
                        accept="image/*,.jpg,.jpeg,.png,.gif,.webp,.bmp,.tiff,.tif,.avif,.svg"
                        multiple
                        onChange={(e) =>
                          setExcelImageFiles(Array.from(e.target.files))
                        }
                      />
                      <small className="text-muted d-block mt-1">
                        Birden fazla görsel seçebilirsiniz. CSV'deki görsel
                        adıyla eşleştirilir. Desteklenen: JPG, PNG, GIF, WEBP,
                        BMP, TIFF, AVIF, SVG
                      </small>
                      {excelImageFiles.length > 0 && (
                        <div className="mt-2">
                          <small className="text-success d-block mb-1">
                            <i className="fas fa-images me-1"></i>
                            {excelImageFiles.length} görsel seçildi:
                          </small>
                          <div
                            className="d-flex flex-wrap gap-1"
                            style={{ maxHeight: "80px", overflowY: "auto" }}
                          >
                            {excelImageFiles.map((f, i) => (
                              <span
                                key={i}
                                className="badge bg-light text-dark border"
                                style={{ fontSize: "0.75rem" }}
                              >
                                {f.name}
                              </span>
                            ))}
                          </div>
                        </div>
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
                          {excelResult.imagesProcessed > 0 && (
                            <div className="text-primary">
                              Görsel: {excelResult.imagesProcessed} adet
                              yüklendi
                            </div>
                          )}
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
