// ============================================================
// XML IMPORT MODAL - Tedarikçi XML Feed Import UI
// ============================================================
// Bu bileşen, XML dosyası veya URL üzerinden ürün/varyant
// import işlemlerini yönetir. Mobil uyumlu tasarım.
// ============================================================

import React, { useState, useCallback } from "react";
import xmlImportService, {
  SUPPLIER_TEMPLATES,
} from "../../services/xmlImportService";
import "./XmlImportModal.css";

const XmlImportModal = ({ isOpen, onClose, onImportComplete }) => {
  // ============================================================
  // STATE YÖNETİMİ
  // ============================================================
  const [activeTab, setActiveTab] = useState("file"); // 'file' | 'url' | 'feed'
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [success, setSuccess] = useState(null);

  // File import state
  const [selectedFile, setSelectedFile] = useState(null);
  const [dragActive, setDragActive] = useState(false);

  // URL import state
  const [importUrl, setImportUrl] = useState("");
  const [urlValidation, setUrlValidation] = useState(null);

  // Mapping state
  const [selectedTemplate, setSelectedTemplate] = useState("generic");
  const [customMapping, setCustomMapping] = useState(
    SUPPLIER_TEMPLATES.generic.mapping,
  );
  const [showAdvancedMapping, setShowAdvancedMapping] = useState(false);

  // Preview state
  const [previewData, setPreviewData] = useState(null);
  const [showPreview, setShowPreview] = useState(false);

  // Import result state
  const [importResult, setImportResult] = useState(null);

  // ============================================================
  // EVENT HANDLERS
  // ============================================================

  // Dosya seçimi
  const handleFileSelect = useCallback((e) => {
    const file = e.target.files?.[0];
    if (file) {
      if (!file.name.toLowerCase().endsWith(".xml")) {
        setError("Sadece XML dosyaları kabul edilir.");
        return;
      }
      if (file.size > 50 * 1024 * 1024) {
        setError("Dosya boyutu 50MB'dan büyük olamaz.");
        return;
      }
      setSelectedFile(file);
      setError(null);
    }
  }, []);

  // Sürükle-bırak
  const handleDrag = useCallback((e) => {
    e.preventDefault();
    e.stopPropagation();
    if (e.type === "dragenter" || e.type === "dragover") {
      setDragActive(true);
    } else if (e.type === "dragleave") {
      setDragActive(false);
    }
  }, []);

  const handleDrop = useCallback((e) => {
    e.preventDefault();
    e.stopPropagation();
    setDragActive(false);

    const file = e.dataTransfer.files?.[0];
    if (file && file.name.toLowerCase().endsWith(".xml")) {
      setSelectedFile(file);
      setError(null);
    } else {
      setError("Sadece XML dosyaları kabul edilir.");
    }
  }, []);

  // Template değişimi
  const handleTemplateChange = useCallback((templateKey) => {
    setSelectedTemplate(templateKey);
    const template = SUPPLIER_TEMPLATES[templateKey];
    if (template) {
      setCustomMapping(template.mapping);
    }
  }, []);

  // Mapping alanı güncelleme
  const handleMappingChange = useCallback((field, value) => {
    setCustomMapping((prev) => ({
      ...prev,
      [field]: value,
    }));
  }, []);

  // URL doğrulama
  const handleValidateUrl = useCallback(async () => {
    if (!importUrl.trim()) {
      setError("URL giriniz.");
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const result = await xmlImportService.validateFeedUrl(importUrl);
      setUrlValidation(result);
      if (!result.isValid) {
        setError(result.error || "URL doğrulanamadı.");
      }
    } catch (err) {
      setError(err.message || "URL doğrulama hatası.");
    } finally {
      setLoading(false);
    }
  }, [importUrl]);

  // Import işlemi
  const handleImport = useCallback(async () => {
    setLoading(true);
    setError(null);
    setSuccess(null);

    try {
      let result;

      if (activeTab === "file" && selectedFile) {
        result = await xmlImportService.importFromFile(
          selectedFile,
          customMapping,
        );
      } else if (activeTab === "url" && importUrl) {
        result = await xmlImportService.importFromUrl(importUrl, customMapping);
      } else {
        throw new Error("Dosya veya URL seçiniz.");
      }

      setImportResult(result);
      setSuccess(
        `Import tamamlandı! ${result.createdCount || 0} yeni, ${result.updatedCount || 0} güncellendi.`,
      );

      if (onImportComplete) {
        onImportComplete(result);
      }
    } catch (err) {
      setError(err.response?.data?.message || err.message || "Import hatası.");
    } finally {
      setLoading(false);
    }
  }, [activeTab, selectedFile, importUrl, customMapping, onImportComplete]);

  // Modal kapatma
  const handleClose = useCallback(() => {
    setSelectedFile(null);
    setImportUrl("");
    setError(null);
    setSuccess(null);
    setImportResult(null);
    setPreviewData(null);
    setShowPreview(false);
    onClose();
  }, [onClose]);

  // ============================================================
  // RENDER
  // ============================================================

  if (!isOpen) return null;

  return (
    <div className="xml-import-overlay" onClick={handleClose}>
      <div className="xml-import-modal" onClick={(e) => e.stopPropagation()}>
        {/* Header */}
        <div className="xml-import-header">
          <h2>📥 XML Import</h2>
          <button className="xml-import-close" onClick={handleClose}>
            ×
          </button>
        </div>

        {/* Tab Navigation */}
        <div className="xml-import-tabs">
          <button
            className={`xml-import-tab ${activeTab === "file" ? "active" : ""}`}
            onClick={() => setActiveTab("file")}
          >
            📁 Dosya Yükle
          </button>
          <button
            className={`xml-import-tab ${activeTab === "url" ? "active" : ""}`}
            onClick={() => setActiveTab("url")}
          >
            🔗 URL'den Al
          </button>
          <button
            className={`xml-import-tab ${activeTab === "feed" ? "active" : ""}`}
            onClick={() => setActiveTab("feed")}
          >
            📡 Feed Kaynakları
          </button>
        </div>

        {/* Content */}
        <div className="xml-import-content">
          {/* File Upload Tab */}
          {activeTab === "file" && (
            <div className="xml-import-file-section">
              <div
                className={`xml-import-dropzone ${dragActive ? "active" : ""} ${selectedFile ? "has-file" : ""}`}
                onDragEnter={handleDrag}
                onDragLeave={handleDrag}
                onDragOver={handleDrag}
                onDrop={handleDrop}
              >
                {selectedFile ? (
                  <div className="xml-import-file-info">
                    <span className="xml-import-file-icon">📄</span>
                    <div className="xml-import-file-details">
                      <span className="xml-import-file-name">
                        {selectedFile.name}
                      </span>
                      <span className="xml-import-file-size">
                        {(selectedFile.size / 1024).toFixed(1)} KB
                      </span>
                    </div>
                    <button
                      className="xml-import-file-remove"
                      onClick={() => setSelectedFile(null)}
                    >
                      ✕
                    </button>
                  </div>
                ) : (
                  <>
                    <span className="xml-import-dropzone-icon">📤</span>
                    <p>XML dosyasını sürükleyip bırakın</p>
                    <p className="xml-import-dropzone-hint">veya</p>
                    <label className="xml-import-file-btn">
                      Dosya Seç
                      <input
                        type="file"
                        accept=".xml"
                        onChange={handleFileSelect}
                        hidden
                      />
                    </label>
                  </>
                )}
              </div>
            </div>
          )}

          {/* URL Import Tab */}
          {activeTab === "url" && (
            <div className="xml-import-url-section">
              <div className="xml-import-url-input-group">
                <input
                  type="url"
                  className="xml-import-url-input"
                  placeholder="https://example.com/feed.xml"
                  value={importUrl}
                  onChange={(e) => setImportUrl(e.target.value)}
                />
                <button
                  className="xml-import-url-validate"
                  onClick={handleValidateUrl}
                  disabled={loading || !importUrl.trim()}
                >
                  {loading ? "..." : "✓ Doğrula"}
                </button>
              </div>

              {urlValidation && (
                <div
                  className={`xml-import-url-status ${urlValidation.isValid ? "valid" : "invalid"}`}
                >
                  {urlValidation.isValid ? (
                    <>
                      ✅ URL geçerli
                      {urlValidation.estimatedProductCount && (
                        <span>
                          {" "}
                          • ~{urlValidation.estimatedProductCount} ürün
                        </span>
                      )}
                    </>
                  ) : (
                    <>❌ {urlValidation.error}</>
                  )}
                </div>
              )}
            </div>
          )}

          {/* Feed Sources Tab */}
          {activeTab === "feed" && (
            <div className="xml-import-feed-section">
              <p className="xml-import-feed-hint">
                Kayıtlı feed kaynakları için Admin Panel &gt; XML Feed Yönetimi
                sayfasını kullanın.
              </p>
            </div>
          )}

          {/* Mapping Configuration */}
          <div className="xml-import-mapping-section">
            <div className="xml-import-mapping-header">
              <h3>⚙️ Mapping Şablonu</h3>
              <button
                className="xml-import-mapping-toggle"
                onClick={() => setShowAdvancedMapping(!showAdvancedMapping)}
              >
                {showAdvancedMapping ? "▲ Gizle" : "▼ Gelişmiş"}
              </button>
            </div>

            <div className="xml-import-template-select">
              <select
                value={selectedTemplate}
                onChange={(e) => handleTemplateChange(e.target.value)}
              >
                {Object.entries(SUPPLIER_TEMPLATES).map(([key, template]) => (
                  <option key={key} value={key}>
                    {template.name}
                  </option>
                ))}
              </select>
            </div>

            {showAdvancedMapping && (
              <div className="xml-import-mapping-grid">
                <div className="xml-import-mapping-field">
                  <label>Root Element</label>
                  <input
                    type="text"
                    value={customMapping.rootElement || ""}
                    onChange={(e) =>
                      handleMappingChange("rootElement", e.target.value)
                    }
                    placeholder="Products"
                  />
                </div>
                <div className="xml-import-mapping-field">
                  <label>Item Element</label>
                  <input
                    type="text"
                    value={customMapping.itemElement || ""}
                    onChange={(e) =>
                      handleMappingChange("itemElement", e.target.value)
                    }
                    placeholder="Product"
                  />
                </div>
                <div className="xml-import-mapping-field">
                  <label>SKU Alanı</label>
                  <input
                    type="text"
                    value={customMapping.skuMapping || ""}
                    onChange={(e) =>
                      handleMappingChange("skuMapping", e.target.value)
                    }
                    placeholder="SKU"
                  />
                </div>
                <div className="xml-import-mapping-field">
                  <label>Başlık Alanı</label>
                  <input
                    type="text"
                    value={customMapping.titleMapping || ""}
                    onChange={(e) =>
                      handleMappingChange("titleMapping", e.target.value)
                    }
                    placeholder="ProductName"
                  />
                </div>
                <div className="xml-import-mapping-field">
                  <label>Fiyat Alanı</label>
                  <input
                    type="text"
                    value={customMapping.priceMapping || ""}
                    onChange={(e) =>
                      handleMappingChange("priceMapping", e.target.value)
                    }
                    placeholder="Price"
                  />
                </div>
                <div className="xml-import-mapping-field">
                  <label>Stok Alanı</label>
                  <input
                    type="text"
                    value={customMapping.stockMapping || ""}
                    onChange={(e) =>
                      handleMappingChange("stockMapping", e.target.value)
                    }
                    placeholder="StockQuantity"
                  />
                </div>
              </div>
            )}
          </div>

          {/* Error/Success Messages */}
          {error && <div className="xml-import-message error">❌ {error}</div>}

          {success && (
            <div className="xml-import-message success">✅ {success}</div>
          )}

          {/* Import Result */}
          {importResult && (
            <div className="xml-import-result">
              <h4>📊 Import Sonucu</h4>
              <div className="xml-import-result-stats">
                <div className="xml-import-stat">
                  <span className="xml-import-stat-value">
                    {importResult.createdCount || 0}
                  </span>
                  <span className="xml-import-stat-label">Yeni Eklendi</span>
                </div>
                <div className="xml-import-stat">
                  <span className="xml-import-stat-value">
                    {importResult.updatedCount || 0}
                  </span>
                  <span className="xml-import-stat-label">Güncellendi</span>
                </div>
                <div className="xml-import-stat">
                  <span className="xml-import-stat-value">
                    {importResult.skippedCount || 0}
                  </span>
                  <span className="xml-import-stat-label">Atlandı</span>
                </div>
                <div className="xml-import-stat error">
                  <span className="xml-import-stat-value">
                    {importResult.failedCount || 0}
                  </span>
                  <span className="xml-import-stat-label">Hatalı</span>
                </div>
              </div>

              {importResult.errors && importResult.errors.length > 0 && (
                <div className="xml-import-errors">
                  <h5>⚠️ Hatalar ({importResult.errors.length})</h5>
                  <ul>
                    {importResult.errors.slice(0, 5).map((err, idx) => (
                      <li key={idx}>
                        <strong>{err.sku || `Satır ${err.rowNumber}`}:</strong>{" "}
                        {err.errorMessage}
                      </li>
                    ))}
                    {importResult.errors.length > 5 && (
                      <li className="xml-import-more-errors">
                        ...ve {importResult.errors.length - 5} hata daha
                      </li>
                    )}
                  </ul>
                </div>
              )}
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="xml-import-footer">
          <button className="xml-import-btn secondary" onClick={handleClose}>
            Kapat
          </button>
          <button
            className="xml-import-btn primary"
            onClick={handleImport}
            disabled={loading || (!selectedFile && !importUrl.trim())}
          >
            {loading ? (
              <>
                <span className="xml-import-spinner"></span>
                İşleniyor...
              </>
            ) : (
              "📥 Import Başlat"
            )}
          </button>
        </div>
      </div>
    </div>
  );
};

export default XmlImportModal;
