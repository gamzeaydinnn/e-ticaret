import { useEffect, useState, useRef } from "react";
import axios from "../../services/api";

const BannerManagement = () => {
  const [banners, setBanners] = useState([]);
  const [form, setForm] = useState({
    id: 0,
    title: "",
    imageUrl: "",
    linkUrl: "",
    isActive: true,
    displayOrder: 0,
  });
  const [editing, setEditing] = useState(false);
  const [feedback, setFeedback] = useState("");
  const [feedbackType, setFeedbackType] = useState("success");
  const [loading, setLoading] = useState(true);
  const tableRef = useRef(null);

  // Resim Upload State'leri
  const [imageFile, setImageFile] = useState(null);
  const [imageUploading, setImageUploading] = useState(false);
  const [imagePreview, setImagePreview] = useState(null);
  const imageInputRef = useRef(null);

  const fetchBanners = async () => {
    try {
      setLoading(true);
      const res = await axios.get("/banners");
      setBanners(res.data);
    } catch (err) {
      setFeedback("Banner'lar yüklenirken hata oluştu");
      setFeedbackType("danger");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchBanners();
  }, []);

  const handleChange = (e) => {
    const { name, value, type, checked } = e.target;
    setForm((prev) => ({
      ...prev,
      [name]: type === "checkbox" ? checked : value,
    }));
  };

  /**
   * Resim dosyası seçildiğinde çağrılır.
   * Dosyayı validate eder ve önizleme oluşturur.
   */
  const handleImageSelect = (e) => {
    const file = e.target.files?.[0];
    if (!file) return;

    // Dosya türü kontrolü
    const allowedTypes = ["image/jpeg", "image/png", "image/gif", "image/webp"];
    if (!allowedTypes.includes(file.type)) {
      setFeedback("Sadece resim dosyaları (jpg, png, gif, webp) yüklenebilir.");
      setFeedbackType("danger");
      setTimeout(() => setFeedback(""), 3000);
      e.target.value = "";
      return;
    }

    // Dosya boyutu kontrolü (10MB)
    const maxSize = 10 * 1024 * 1024;
    if (file.size > maxSize) {
      setFeedback("Dosya boyutu maksimum 10MB olabilir.");
      setFeedbackType("danger");
      setTimeout(() => setFeedback(""), 3000);
      e.target.value = "";
      return;
    }

    setImageFile(file);
    // Önizleme için ObjectURL oluştur
    const previewUrl = URL.createObjectURL(file);
    setImagePreview(previewUrl);
  };

  /**
   * Seçilen resim dosyasını sunucuya yükler.
   * Başarılı olursa imageUrl'i form'a set eder.
   */
  const handleImageUpload = async () => {
    if (!imageFile) {
      setFeedback("Lütfen önce bir resim dosyası seçin.");
      setFeedbackType("warning");
      setTimeout(() => setFeedback(""), 3000);
      return;
    }

    setImageUploading(true);
    try {
      const formData = new FormData();
      formData.append("image", imageFile);

      // Banner için admin/banners/upload-image endpoint'i kullan
      const response = await axios.post(
        "/admin/banners/upload-image",
        formData,
        {
          headers: { "Content-Type": "multipart/form-data" },
        }
      );

      const result = response?.data || response;
      if (result?.success && result?.imageUrl) {
        // Yükleme başarılı - form'a URL'i ekle
        setForm((prev) => ({ ...prev, imageUrl: result.imageUrl }));
        setImagePreview(result.imageUrl);
        setImageFile(null);
        // Input'u temizle
        if (imageInputRef.current) {
          imageInputRef.current.value = "";
        }
        setFeedback("✅ Resim başarıyla yüklendi!");
        setFeedbackType("success");
      } else {
        throw new Error(result?.message || "Resim yüklenemedi");
      }
    } catch (err) {
      console.error("Resim yükleme hatası:", err);
      setFeedback(
        "Resim yüklenirken hata oluştu: " +
          (err.response?.data?.message || err.message)
      );
      setFeedbackType("danger");
    } finally {
      setImageUploading(false);
      setTimeout(() => setFeedback(""), 3000);
    }
  };

  /**
   * Resim seçimini iptal eder ve önizlemeyi temizler.
   */
  const handleClearImage = () => {
    setImageFile(null);
    // Mevcut banner düzenleniyorsa eski resmi göster
    setImagePreview(editing && form.imageUrl ? form.imageUrl : null);
    if (imageInputRef.current) {
      imageInputRef.current.value = "";
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    try {
      if (editing) {
        await axios.put("/banners", form);
        setFeedback("Banner başarıyla güncellendi.");
        setFeedbackType("success");
      } else {
        await axios.post("/banners", form);
        setFeedback("Banner başarıyla eklendi.");
        setFeedbackType("success");
      }
      setForm({
        id: 0,
        title: "",
        imageUrl: "",
        linkUrl: "",
        isActive: true,
        displayOrder: 0,
      });
      // Resim state'lerini temizle
      setImageFile(null);
      setImagePreview(null);
      if (imageInputRef.current) {
        imageInputRef.current.value = "";
      }
      setEditing(false);
      await fetchBanners();
      if (tableRef.current) {
        tableRef.current.scrollIntoView({ behavior: "smooth" });
      }
    } catch (err) {
      setFeedback("Bir hata oluştu. Lütfen tekrar deneyin.");
      setFeedbackType("danger");
    }
    setTimeout(() => setFeedback(""), 2500);
  };

  const handleEdit = (banner) => {
    setForm(banner);
    // Mevcut resmi önizleme olarak göster
    setImagePreview(banner.imageUrl || null);
    setImageFile(null);
    if (imageInputRef.current) {
      imageInputRef.current.value = "";
    }
    setEditing(true);
    window.scrollTo({ top: 0, behavior: "smooth" });
  };

  const handleDelete = async (id) => {
    if (!window.confirm("Bu banner'ı silmek istediğinize emin misiniz?"))
      return;
    try {
      await axios.delete(`/banners/${id}`);
      setFeedback("Banner silindi.");
      setFeedbackType("success");
      fetchBanners();
    } catch (err) {
      setFeedback("Silme işlemi başarısız.");
      setFeedbackType("danger");
    }
    setTimeout(() => setFeedback(""), 2500);
  };

  const handleCancel = () => {
    setEditing(false);
    // Resim state'lerini temizle
    setImageFile(null);
    setImagePreview(null);
    if (imageInputRef.current) {
      imageInputRef.current.value = "";
    }
    setForm({
      id: 0,
      title: "",
      imageUrl: "",
      linkUrl: "",
      isActive: true,
      displayOrder: 0,
    });
  };

  return (
    <div className="container-fluid p-2 p-md-4">
      {/* Header */}
      <div className="d-flex flex-column flex-md-row justify-content-between align-items-start align-items-md-center mb-3 mb-md-4">
        <div className="mb-2 mb-md-0">
          <h1 className="h4 h3-md fw-bold mb-1" style={{ color: "#2d3748" }}>
            <i className="fas fa-images me-2" style={{ color: "#f57c00" }} />
            Banner Yönetimi
          </h1>
          <div className="text-muted" style={{ fontSize: "0.8rem" }}>
            Ana sayfa banner'larını yönetin
          </div>
        </div>
      </div>

      {/* Feedback */}
      {feedback && (
        <div className={`alert alert-${feedbackType} py-2 mb-3`} role="alert">
          <i
            className={`fas fa-${
              feedbackType === "success"
                ? "check-circle"
                : "exclamation-triangle"
            } me-2`}
          ></i>
          {feedback}
        </div>
      )}

      {/* Form Card */}
      <div className="card border-0 shadow-sm mb-3 mb-md-4">
        <div className="card-header bg-white py-2 px-3">
          <h6 className="mb-0 fw-semibold" style={{ fontSize: "0.9rem" }}>
            <i
              className={`fas fa-${
                editing ? "edit" : "plus"
              } me-2 text-primary`}
            ></i>
            {editing ? "Banner Düzenle" : "Yeni Banner Ekle"}
          </h6>
        </div>
        <div className="card-body p-2 p-md-3">
          <form onSubmit={handleSubmit} className="admin-mobile-form">
            <div className="row g-2 g-md-3">
              <div className="col-12 col-md-6">
                <label
                  className="form-label fw-semibold mb-1"
                  style={{ fontSize: "0.85rem" }}
                >
                  Başlık
                </label>
                <input
                  name="title"
                  value={form.title}
                  onChange={handleChange}
                  placeholder="Banner başlığı"
                  className="form-control"
                  style={{ minHeight: "44px" }}
                  required
                />
              </div>
              <div className="col-12 col-md-6">
                <label
                  className="form-label fw-semibold mb-1"
                  style={{ fontSize: "0.85rem" }}
                >
                  Görsel
                </label>

                {/* Resim Önizleme Alanı */}
                {imagePreview && (
                  <div className="mb-2 text-center">
                    <img
                      src={imagePreview}
                      alt="Banner önizleme"
                      style={{
                        maxWidth: "100%",
                        maxHeight: "120px",
                        objectFit: "contain",
                        borderRadius: "8px",
                        border: "2px solid #e2e8f0",
                      }}
                      onError={(e) => {
                        e.target.src = "/images/placeholder.png";
                      }}
                    />
                  </div>
                )}

                {/* Dosya Seçme Alanı */}
                <div className="d-flex gap-2 align-items-center flex-wrap">
                  <input
                    ref={imageInputRef}
                    type="file"
                    accept="image/jpeg,image/png,image/gif,image/webp"
                    className="form-control"
                    style={{ minHeight: "44px", flex: "1" }}
                    onChange={handleImageSelect}
                  />

                  {/* Yükle Butonu */}
                  {imageFile && (
                    <button
                      type="button"
                      className="btn text-white"
                      style={{
                        background: "linear-gradient(135deg, #10b981, #34d399)",
                        minHeight: "44px",
                        minWidth: "80px",
                      }}
                      onClick={handleImageUpload}
                      disabled={imageUploading}
                    >
                      {imageUploading ? (
                        <span className="spinner-border spinner-border-sm"></span>
                      ) : (
                        <>
                          <i className="fas fa-upload me-1"></i>
                          Yükle
                        </>
                      )}
                    </button>
                  )}

                  {/* Temizle Butonu */}
                  {(imageFile || imagePreview) && (
                    <button
                      type="button"
                      className="btn btn-outline-secondary"
                      style={{ minHeight: "44px" }}
                      onClick={handleClearImage}
                    >
                      <i className="fas fa-times"></i>
                    </button>
                  )}
                </div>

                {/* Mevcut URL gösterimi ve manuel giriş */}
                {form.imageUrl && (
                  <div className="mt-1">
                    <small
                      className="text-muted text-truncate d-block"
                      style={{ maxWidth: "100%" }}
                    >
                      <i className="fas fa-link me-1"></i>
                      {form.imageUrl}
                    </small>
                  </div>
                )}
                <div className="mt-1">
                  <small
                    className="text-primary"
                    style={{ cursor: "pointer", fontSize: "0.75rem" }}
                    onClick={() => {
                      const url = prompt("Resim URL'si girin:", form.imageUrl);
                      if (url !== null) {
                        setForm((prev) => ({ ...prev, imageUrl: url }));
                        setImagePreview(url || null);
                      }
                    }}
                  >
                    <i className="fas fa-edit me-1"></i>
                    Manuel URL gir
                  </small>
                </div>
              </div>
              <div className="col-12 col-md-6">
                <label
                  className="form-label fw-semibold mb-1"
                  style={{ fontSize: "0.85rem" }}
                >
                  Link URL
                </label>
                <input
                  name="linkUrl"
                  value={form.linkUrl}
                  onChange={handleChange}
                  placeholder="Tıklanınca gidilecek URL"
                  className="form-control"
                  style={{ minHeight: "44px" }}
                />
              </div>
              <div className="col-6 col-md-3">
                <label
                  className="form-label fw-semibold mb-1"
                  style={{ fontSize: "0.85rem" }}
                >
                  Sıra
                </label>
                <input
                  name="displayOrder"
                  type="number"
                  value={form.displayOrder}
                  onChange={handleChange}
                  placeholder="0"
                  className="form-control"
                  style={{ minHeight: "44px" }}
                />
              </div>
              <div className="col-6 col-md-3 d-flex align-items-end">
                <div
                  className="form-check"
                  style={{
                    minHeight: "44px",
                    display: "flex",
                    alignItems: "center",
                  }}
                >
                  <input
                    name="isActive"
                    type="checkbox"
                    checked={form.isActive}
                    onChange={handleChange}
                    className="form-check-input"
                    style={{ width: "1.25rem", height: "1.25rem" }}
                  />
                  <label className="form-check-label fw-semibold ms-2">
                    Aktif
                  </label>
                </div>
              </div>
              <div className="col-12">
                <div className="d-flex gap-2 flex-wrap">
                  <button
                    type="submit"
                    className="btn text-white fw-semibold"
                    style={{
                      background: "linear-gradient(135deg, #f57c00, #ff9800)",
                      minHeight: "44px",
                      minWidth: "120px",
                    }}
                  >
                    <i
                      className={`fas fa-${editing ? "save" : "plus"} me-2`}
                    ></i>
                    {editing ? "Güncelle" : "Ekle"}
                  </button>
                  {editing && (
                    <button
                      type="button"
                      onClick={handleCancel}
                      className="btn btn-outline-secondary"
                      style={{ minHeight: "44px" }}
                    >
                      <i className="fas fa-times me-2"></i>
                      İptal
                    </button>
                  )}
                </div>
              </div>
            </div>
          </form>
        </div>
      </div>

      {/* Banner List */}
      <div className="card border-0 shadow-sm" ref={tableRef}>
        <div className="card-header bg-white d-flex justify-content-between align-items-center py-2 px-3">
          <h6 className="mb-0 fw-semibold" style={{ fontSize: "0.9rem" }}>
            <i className="fas fa-list me-2 text-primary"></i>
            Banner Listesi
            <span
              className="badge bg-secondary ms-2"
              style={{ fontSize: "0.7rem" }}
            >
              {banners.length}
            </span>
          </h6>
          <button
            className="btn btn-sm btn-outline-secondary"
            onClick={fetchBanners}
            disabled={loading}
            style={{ minHeight: "36px" }}
          >
            <i className="fas fa-sync-alt"></i>
          </button>
        </div>
        <div className="card-body p-2 p-md-3">
          {loading ? (
            <div className="text-center py-4">
              <div className="spinner-border text-primary" role="status">
                <span className="visually-hidden">Yükleniyor...</span>
              </div>
            </div>
          ) : banners.length === 0 ? (
            <div className="text-center py-4 text-muted">
              <i className="fas fa-images fa-2x mb-2 opacity-50"></i>
              <p className="mb-0">Henüz banner eklenmemiş.</p>
            </div>
          ) : (
            <div className="row g-2 g-md-3">
              {(Array.isArray(banners) ? banners : []).map((b) => (
                <div key={b.id} className="col-12 col-sm-6 col-lg-4">
                  <div className="card h-100 border">
                    {/* Banner Preview */}
                    <div
                      className="position-relative"
                      style={{
                        height: "120px",
                        overflow: "hidden",
                        backgroundColor: "#f8fafc",
                      }}
                    >
                      <img
                        src={b.imageUrl}
                        alt={b.title}
                        className="w-100 h-100"
                        style={{ objectFit: "cover" }}
                        onError={(e) => {
                          e.target.src = "/images/placeholder.png";
                        }}
                      />
                      {/* Status Badge */}
                      <span
                        className={`badge position-absolute top-0 end-0 m-2 ${
                          b.isActive ? "bg-success" : "bg-secondary"
                        }`}
                        style={{ fontSize: "0.65rem" }}
                      >
                        {b.isActive ? "Aktif" : "Pasif"}
                      </span>
                      {/* Order Badge */}
                      <span
                        className="badge bg-dark position-absolute top-0 start-0 m-2"
                        style={{ fontSize: "0.65rem" }}
                      >
                        #{b.displayOrder}
                      </span>
                    </div>
                    {/* Banner Info */}
                    <div className="card-body p-2">
                      <h6
                        className="card-title mb-1 text-truncate"
                        style={{ fontSize: "0.85rem" }}
                        title={b.title}
                      >
                        {b.title}
                      </h6>
                      {b.linkUrl && (
                        <a
                          href={b.linkUrl}
                          target="_blank"
                          rel="noopener noreferrer"
                          className="text-muted text-truncate d-block"
                          style={{ fontSize: "0.75rem" }}
                        >
                          <i className="fas fa-link me-1"></i>
                          {b.linkUrl}
                        </a>
                      )}
                    </div>
                    {/* Actions */}
                    <div className="card-footer bg-transparent border-top-0 p-2">
                      <div className="d-flex gap-2">
                        <button
                          onClick={() => handleEdit(b)}
                          className="btn btn-outline-primary btn-sm flex-grow-1"
                          style={{ minHeight: "40px" }}
                        >
                          <i className="fas fa-edit me-1"></i>
                          Düzenle
                        </button>
                        <button
                          onClick={() => handleDelete(b.id)}
                          className="btn btn-outline-danger btn-sm flex-grow-1"
                          style={{ minHeight: "40px" }}
                        >
                          <i className="fas fa-trash me-1"></i>
                          Sil
                        </button>
                      </div>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

export default BannerManagement;
