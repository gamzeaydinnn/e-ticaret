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
                  Görsel URL
                </label>
                <input
                  name="imageUrl"
                  value={form.imageUrl}
                  onChange={handleChange}
                  placeholder="https://example.com/image.jpg"
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
