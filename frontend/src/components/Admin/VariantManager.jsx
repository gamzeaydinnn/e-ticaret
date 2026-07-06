// ============================================================
// VARIANT MANAGER - Ürün Varyant Yönetim Bileşeni
// ============================================================
// Bu bileşen, bir ürünün varyantlarını (SKU bazlı stok birimleri)
// listeleme, ekleme, düzenleme ve silme işlemlerini yönetir.
// Mobil uyumlu, profesyonel tasarım.
// ============================================================

import React, { useState, useEffect, useCallback } from "react";
import variantService, {
  formatPrice,
  getStockStatus,
} from "../../services/variantService";
import "./VariantManager.css";

const VariantManager = ({
  productId,
  productName,
  isOpen,
  onClose,
  onUpdate,
}) => {
  // ============================================================
  // STATE YÖNETİMİ
  // ============================================================
  const [variants, setVariants] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [success, setSuccess] = useState(null);

  // Form state
  const [showForm, setShowForm] = useState(false);
  const [editingVariant, setEditingVariant] = useState(null);
  const [formData, setFormData] = useState({
    sku: "",
    title: "",
    price: "",
    stock: "",
    barcode: "",
    currency: "TRY",
    weightGrams: "",
    volumeML: "",
    supplierCode: "",
    maxOrderQuantity: "",
  });

  // Stok hızlı düzenleme — onBlur ile kaydet
  const [stockDrafts, setStockDrafts] = useState({});

  // Bulk operations
  const [selectedVariants, setSelectedVariants] = useState([]);
  const [showBulkActions, setShowBulkActions] = useState(false);

  // ============================================================
  // DATA LOADING
  // ============================================================

  const loadVariants = useCallback(async () => {
    if (!productId) return;

    setLoading(true);
    setError(null);

    try {
      const data = await variantService.getVariantsByProductAdmin(productId);
      setVariants(data || []);
      const drafts = {};
      (data || []).forEach((v) => {
        drafts[v.id] = String(v.stock ?? 0);
      });
      setStockDrafts(drafts);
    } catch (err) {
      setError(
        err.response?.data?.message || "Varyantlar yüklenirken hata oluştu.",
      );
    } finally {
      setLoading(false);
    }
  }, [productId]);

  useEffect(() => {
    if (isOpen && productId) {
      loadVariants();
    }
  }, [isOpen, productId, loadVariants]);

  // ============================================================
  // FORM HANDLERS
  // ============================================================

  const resetForm = useCallback(() => {
    setFormData({
      sku: "",
      title: "",
      price: "",
      stock: "",
      barcode: "",
      currency: "TRY",
      weightGrams: "",
      volumeML: "",
      supplierCode: "",
      maxOrderQuantity: "",
    });
    setEditingVariant(null);
    setShowForm(false);
  }, []);

  const handleInputChange = useCallback((field, value) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
  }, []);

  const handleEdit = useCallback((variant) => {
    setEditingVariant(variant);
    setFormData({
      sku: variant.sku || "",
      title: variant.title || "",
      price: variant.price?.toString() || "",
      stock: variant.stock?.toString() || "",
      barcode: variant.barcode || "",
      currency: variant.currency || "TRY",
      weightGrams: variant.weightGrams?.toString() || "",
      volumeML: variant.volumeML?.toString() || "",
      supplierCode: variant.supplierCode || "",
      maxOrderQuantity:
        variant.maxOrderQuantity > 0
          ? String(variant.maxOrderQuantity)
          : "",
    });
    setShowForm(true);
  }, []);

  const handleSubmit = useCallback(
    async (e) => {
      e.preventDefault();

      // Validation
      if (!formData.sku.trim()) {
        setError("SKU zorunludur.");
        return;
      }
      if (!formData.price || parseFloat(formData.price) < 0) {
        setError("Geçerli bir fiyat giriniz.");
        return;
      }

      setLoading(true);
      setError(null);

      try {
        if (editingVariant) {
          // Güncelleme
          await variantService.updateVariant(editingVariant.id, formData);
          setSuccess("Varyant güncellendi.");
        } else {
          // Yeni ekleme
          await variantService.createVariant(productId, formData);
          setSuccess("Varyant eklendi.");
        }

        resetForm();
        loadVariants();

        if (onUpdate) {
          onUpdate();
        }
      } catch (err) {
        setError(err.response?.data?.message || "İşlem sırasında hata oluştu.");
      } finally {
        setLoading(false);
      }
    },
    [formData, editingVariant, productId, resetForm, loadVariants, onUpdate],
  );

  const handleDelete = useCallback(
    async (variantId) => {
      if (!window.confirm("Bu varyantı silmek istediğinize emin misiniz?")) {
        return;
      }

      setLoading(true);
      setError(null);

      try {
        await variantService.deleteVariant(variantId);
        setSuccess("Varyant silindi.");
        loadVariants();

        if (onUpdate) {
          onUpdate();
        }
      } catch (err) {
        setError(err.response?.data?.message || "Silme sırasında hata oluştu.");
      } finally {
        setLoading(false);
      }
    },
    [loadVariants, onUpdate],
  );

  // ============================================================
  // STOCK QUICK UPDATE
  // ============================================================

  const handleQuickStockUpdate = useCallback(
    async (variantId, newStock) => {
      const parsed = parseInt(newStock, 10);
      if (!Number.isFinite(parsed) || parsed < 0) {
        setError("Geçerli bir stok miktarı giriniz.");
        return;
      }

      try {
        await variantService.updateVariantStock(variantId, parsed);
        loadVariants();
      } catch (err) {
        setError("Stok güncellenirken hata oluştu.");
      }
    },
    [loadVariants],
  );

  const handleStockDraftChange = useCallback((variantId, value) => {
    setStockDrafts((prev) => ({ ...prev, [variantId]: value }));
  }, []);

  const handleStockBlur = useCallback(
    (variantId, originalStock) => {
      const draft = stockDrafts[variantId];
      const parsed = parseInt(draft, 10);
      if (!Number.isFinite(parsed) || parsed < 0) {
        setStockDrafts((prev) => ({
          ...prev,
          [variantId]: String(originalStock ?? 0),
        }));
        return;
      }
      if (parsed === originalStock) return;
      handleQuickStockUpdate(variantId, parsed);
    },
    [stockDrafts, handleQuickStockUpdate],
  );

  // ============================================================
  // SELECTION & BULK
  // ============================================================

  const handleSelectVariant = useCallback((variantId) => {
    setSelectedVariants((prev) => {
      if (prev.includes(variantId)) {
        return prev.filter((id) => id !== variantId);
      }
      return [...prev, variantId];
    });
  }, []);

  const handleSelectAll = useCallback(() => {
    if (selectedVariants.length === variants.length) {
      setSelectedVariants([]);
    } else {
      setSelectedVariants(variants.map((v) => v.id));
    }
  }, [selectedVariants.length, variants]);

  const handleBulkDelete = useCallback(async () => {
    if (selectedVariants.length === 0) return;

    if (
      !window.confirm(
        `${selectedVariants.length} varyantı silmek istediğinize emin misiniz?`,
      )
    ) {
      return;
    }

    setLoading(true);

    try {
      for (const id of selectedVariants) {
        await variantService.deleteVariant(id);
      }
      setSuccess(`${selectedVariants.length} varyant silindi.`);
      setSelectedVariants([]);
      loadVariants();
    } catch (err) {
      setError("Toplu silme sırasında hata oluştu.");
    } finally {
      setLoading(false);
    }
  }, [selectedVariants, loadVariants]);

  // ============================================================
  // RENDER
  // ============================================================

  if (!isOpen) return null;

  return (
    <div className="variant-manager-overlay" onClick={onClose}>
      <div
        className="variant-manager-modal"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Header */}
        <div className="variant-manager-header">
          <div className="variant-manager-title">
            <h2>📦 Varyant Yönetimi</h2>
            {productName && (
              <span className="variant-manager-product">{productName}</span>
            )}
          </div>
          <button className="variant-manager-close" onClick={onClose}>
            ×
          </button>
        </div>

        {/* Toolbar */}
        <div className="variant-manager-toolbar">
          <button
            className="variant-manager-btn primary"
            onClick={() => {
              resetForm();
              setShowForm(true);
            }}
          >
            ➕ Yeni Varyant
          </button>

          {selectedVariants.length > 0 && (
            <div className="variant-manager-bulk-actions">
              <span className="variant-manager-selected-count">
                {selectedVariants.length} seçili
              </span>
              <button
                className="variant-manager-btn danger"
                onClick={handleBulkDelete}
              >
                🗑️ Sil
              </button>
            </div>
          )}

          <button
            className="variant-manager-btn secondary"
            onClick={loadVariants}
            disabled={loading}
          >
            🔄 Yenile
          </button>
        </div>

        {/* Messages */}
        {error && (
          <div className="variant-manager-message error">
            ❌ {error}
            <button onClick={() => setError(null)}>×</button>
          </div>
        )}

        {success && (
          <div className="variant-manager-message success">
            ✅ {success}
            <button onClick={() => setSuccess(null)}>×</button>
          </div>
        )}

        {/* Content */}
        <div className="variant-manager-content">
          {/* Form */}
          {showForm && (
            <div className="variant-manager-form-section">
              <h3>
                {editingVariant ? "✏️ Varyant Düzenle" : "➕ Yeni Varyant Ekle"}
              </h3>

              <form onSubmit={handleSubmit} className="variant-manager-form">
                <div className="variant-form-row">
                  <div className="variant-form-group">
                    <label>SKU *</label>
                    <input
                      type="text"
                      value={formData.sku}
                      onChange={(e) => handleInputChange("sku", e.target.value)}
                      placeholder="Örn: PRD-001-RED"
                      disabled={!!editingVariant}
                      required
                    />
                  </div>
                  <div className="variant-form-group">
                    <label>Başlık</label>
                    <input
                      type="text"
                      value={formData.title}
                      onChange={(e) =>
                        handleInputChange("title", e.target.value)
                      }
                      placeholder="Kırmızı - XL"
                    />
                  </div>
                </div>

                <div className="variant-form-row">
                  <div className="variant-form-group">
                    <label>Fiyat *</label>
                    <div className="variant-input-group">
                      <input
                        type="number"
                        step="0.01"
                        min="0"
                        value={formData.price}
                        onChange={(e) =>
                          handleInputChange("price", e.target.value)
                        }
                        placeholder="0.00"
                        required
                      />
                      <select
                        value={formData.currency}
                        onChange={(e) =>
                          handleInputChange("currency", e.target.value)
                        }
                      >
                        <option value="TRY">₺</option>
                        <option value="USD">$</option>
                        <option value="EUR">€</option>
                      </select>
                    </div>
                  </div>
                  <div className="variant-form-group">
                    <label>Stok</label>
                    <input
                      type="number"
                      min="0"
                      value={formData.stock}
                      onChange={(e) =>
                        handleInputChange("stock", e.target.value)
                      }
                      placeholder="0"
                    />
                  </div>
                </div>

                <div className="variant-form-row">
                  <div className="variant-form-group">
                    <label>Barkod</label>
                    <input
                      type="text"
                      value={formData.barcode}
                      onChange={(e) =>
                        handleInputChange("barcode", e.target.value)
                      }
                      placeholder="8691234567890"
                    />
                  </div>
                  <div className="variant-form-group">
                    <label>Tedarikçi Kodu</label>
                    <input
                      type="text"
                      value={formData.supplierCode}
                      onChange={(e) =>
                        handleInputChange("supplierCode", e.target.value)
                      }
                      placeholder="SUP-001"
                    />
                  </div>
                </div>

                <div className="variant-form-row">
                  <div className="variant-form-group">
                    <label>Max Sipariş Adedi</label>
                    <input
                      type="number"
                      min="0"
                      value={formData.maxOrderQuantity}
                      onChange={(e) =>
                        handleInputChange("maxOrderQuantity", e.target.value)
                      }
                      placeholder="Boş = ürün/global limit"
                    />
                  </div>
                  <div className="variant-form-group">
                    <label>Ağırlık (gram)</label>
                    <input
                      type="number"
                      min="0"
                      value={formData.weightGrams}
                      onChange={(e) =>
                        handleInputChange("weightGrams", e.target.value)
                      }
                      placeholder="500"
                    />
                  </div>
                  <div className="variant-form-group">
                    <label>Hacim (ml)</label>
                    <input
                      type="number"
                      min="0"
                      value={formData.volumeML}
                      onChange={(e) =>
                        handleInputChange("volumeML", e.target.value)
                      }
                      placeholder="330"
                    />
                  </div>
                </div>

                <div className="variant-form-actions">
                  <button
                    type="button"
                    className="variant-manager-btn secondary"
                    onClick={resetForm}
                  >
                    İptal
                  </button>
                  <button
                    type="submit"
                    className="variant-manager-btn primary"
                    disabled={loading}
                  >
                    {loading
                      ? "Kaydediliyor..."
                      : editingVariant
                        ? "Güncelle"
                        : "Ekle"}
                  </button>
                </div>
              </form>
            </div>
          )}

          {/* Variants List */}
          <div className="variant-manager-list">
            {loading && variants.length === 0 ? (
              <div className="variant-manager-loading">
                <div className="variant-spinner"></div>
                <span>Yükleniyor...</span>
              </div>
            ) : variants.length === 0 ? (
              <div className="variant-manager-empty">
                <span className="variant-empty-icon">📦</span>
                <p>Henüz varyant eklenmemiş</p>
                <button
                  className="variant-manager-btn primary"
                  onClick={() => setShowForm(true)}
                >
                  İlk Varyantı Ekle
                </button>
              </div>
            ) : (
              <>
                {/* List Header */}
                <div className="variant-list-header">
                  <label className="variant-checkbox">
                    <input
                      type="checkbox"
                      checked={
                        selectedVariants.length === variants.length &&
                        variants.length > 0
                      }
                      onChange={handleSelectAll}
                    />
                  </label>
                  <span className="variant-col-sku">SKU</span>
                  <span className="variant-col-title">Başlık</span>
                  <span className="variant-col-price">Fiyat</span>
                  <span className="variant-col-stock">Stok</span>
                  <span className="variant-col-limit">Max Adet</span>
                  <span className="variant-col-actions">İşlemler</span>
                </div>

                {/* List Items */}
                {variants.map((variant) => {
                  const stockStatus = getStockStatus(variant.stock);

                  return (
                    <div
                      key={variant.id}
                      className={`variant-list-item ${selectedVariants.includes(variant.id) ? "selected" : ""}`}
                    >
                      <label className="variant-checkbox">
                        <input
                          type="checkbox"
                          checked={selectedVariants.includes(variant.id)}
                          onChange={() => handleSelectVariant(variant.id)}
                        />
                      </label>

                      <div className="variant-col-sku">
                        <span className="variant-sku">{variant.sku}</span>
                        {variant.barcode && (
                          <span className="variant-barcode">
                            {variant.barcode}
                          </span>
                        )}
                      </div>

                      <div className="variant-col-title">
                        {variant.title || "-"}
                      </div>

                      <div className="variant-col-price">
                        <span className="variant-price">
                          {formatPrice(variant.price, variant.currency)}
                        </span>
                      </div>

                      <div className="variant-col-stock">
                        <div
                          className={`variant-stock-badge ${stockStatus.status}`}
                        >
                          <span className="variant-stock-icon">
                            {stockStatus.icon}
                          </span>
                          <input
                            type="number"
                            min="0"
                            value={stockDrafts[variant.id] ?? variant.stock}
                            onChange={(e) =>
                              handleStockDraftChange(variant.id, e.target.value)
                            }
                            onBlur={() =>
                              handleStockBlur(variant.id, variant.stock)
                            }
                            className="variant-stock-input"
                          />
                        </div>
                      </div>

                      <div className="variant-col-limit">
                        {variant.maxOrderQuantity > 0
                          ? variant.maxOrderQuantity
                          : "—"}
                      </div>

                      <div className="variant-col-actions">
                        <button
                          className="variant-action-btn edit"
                          onClick={() => handleEdit(variant)}
                          title="Düzenle"
                        >
                          ✏️
                        </button>
                        <button
                          className="variant-action-btn delete"
                          onClick={() => handleDelete(variant.id)}
                          title="Sil"
                        >
                          🗑️
                        </button>
                      </div>
                    </div>
                  );
                })}
              </>
            )}
          </div>
        </div>

        {/* Footer */}
        <div className="variant-manager-footer">
          <span className="variant-manager-count">
            Toplam: {variants.length} varyant
          </span>
          <button className="variant-manager-btn secondary" onClick={onClose}>
            Kapat
          </button>
        </div>
      </div>
    </div>
  );
};

export default VariantManager;
