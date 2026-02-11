import React, { useEffect, useState } from "react";
import DOMPurify from "dompurify";
import { CampaignService } from "../services/campaignService";

export default function Campaigns() {
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    let mounted = true;
    setLoading(true);
    setError("");

    CampaignService.getActiveCampaigns()
      .then((list) => {
        if (!mounted) return;
        setItems(Array.isArray(list) ? list : []);
      })
      .catch((e) => setError(e?.message || "Kampanyalar yüklenemedi"))
      .finally(() => mounted && setLoading(false));

    return () => {
      mounted = false;
    };
  }, []);

  const typeLabels = {
    0: "Yüzde İndirim",
    1: "Sabit Tutar İndirim",
    2: "X Al Y Öde",
    3: "Ücretsiz Kargo",
  };

  const typeColors = {
    0: {
      bg: "#fef3c7",
      border: "#f59e0b",
      text: "#b45309",
      icon: "fa-percent",
    },
    1: { bg: "#dbeafe", border: "#3b82f6", text: "#1d4ed8", icon: "fa-tag" },
    2: { bg: "#dcfce7", border: "#22c55e", text: "#15803d", icon: "fa-gift" },
    3: { bg: "#f3e8ff", border: "#a855f7", text: "#7e22ce", icon: "fa-truck" },
  };

  return (
    <div
      className="container-fluid px-4 py-4"
      style={{ maxWidth: "1200px", margin: "0 auto" }}
    >
      <div className="d-flex align-items-center justify-content-between mb-4">
        <div>
          <h1 className="h3 fw-bold mb-1" style={{ color: "#2d3748" }}>
            <i
              className="fas fa-bullhorn me-2"
              style={{ color: "#f57c00" }}
            ></i>
            Kampanyalar
          </h1>
          <div className="text-muted">Aktif fırsat ve indirimler</div>
        </div>
      </div>

      {loading && (
        <div className="text-center py-5 text-muted">
          <i className="fas fa-spinner fa-spin me-2"></i>
          Kampanyalar yükleniyor...
        </div>
      )}
      {error && <div className="alert alert-danger">{error}</div>}

      {!loading && !error && items.length === 0 && (
        <div className="text-center py-5" style={{ color: "#9ca3af" }}>
          <i className="fas fa-tags fa-3x mb-3 d-block opacity-50"></i>
          <p>Şu anda aktif kampanya bulunmuyor.</p>
        </div>
      )}

      {!loading && !error && items.length > 0 && (
        <div className="row">
          {items.map((c) => {
            const tc = typeColors[c.type] || typeColors[0];

            // XSS koruması için tüm kullanıcı girdilerini sanitize et
            const safeName = DOMPurify.sanitize(c.name, {
              ALLOWED_TAGS: [],
              KEEP_CONTENT: true,
            });
            const safeDisplayText = c.displayText
              ? DOMPurify.sanitize(c.displayText, {
                  ALLOWED_TAGS: [],
                  KEEP_CONTENT: true,
                })
              : null;
            const safeDescription = c.description
              ? DOMPurify.sanitize(c.description, {
                  ALLOWED_TAGS: [],
                  KEEP_CONTENT: true,
                })
              : null;

            return (
              <div key={c.id} className="col-lg-4 col-md-6 mb-4">
                <div
                  className="card h-100 shadow-sm border-0"
                  style={{ borderRadius: 16, overflow: "hidden" }}
                >
                  {c.imageUrl ? (
                    <div className="position-relative">
                      <img
                        src={c.imageUrl}
                        alt={c.name}
                        className="card-img-top"
                        style={{ height: 200, objectFit: "cover" }}
                      />
                      {c.badgeText && (
                        <span
                          className="position-absolute top-0 start-0 m-2 px-3 py-1 rounded-pill"
                          style={{
                            background:
                              "linear-gradient(135deg, #ff6b35, #ff8c00)",
                            color: "white",
                            fontWeight: 700,
                            fontSize: "0.85rem",
                          }}
                        >
                          {c.badgeText}
                        </span>
                      )}
                    </div>
                  ) : (
                    <div
                      style={{
                        height: 120,
                        background: `linear-gradient(135deg, ${tc.bg}, ${tc.border}30)`,
                        display: "flex",
                        alignItems: "center",
                        justifyContent: "center",
                      }}
                    >
                      <i
                        className={`fas ${tc.icon}`}
                        style={{
                          fontSize: "2.5rem",
                          color: tc.border,
                          opacity: 0.6,
                        }}
                      ></i>
                    </div>
                  )}
                  <div className="card-body">
                    <div className="d-flex align-items-center gap-2 mb-2">
                      <span
                        className="badge"
                        style={{
                          backgroundColor: tc.bg,
                          color: tc.text,
                          border: `1px solid ${tc.border}`,
                          fontSize: "0.75rem",
                        }}
                      >
                        {typeLabels[c.type] || "Kampanya"}
                      </span>
                      {c.badgeText && (
                        <span
                          className="badge"
                          style={{
                            backgroundColor: tc.border,
                            color: "white",
                            fontSize: "0.75rem",
                          }}
                        >
                          {c.badgeText}
                        </span>
                      )}
                    </div>
                    <h5
                      className="card-title mb-1"
                      style={{ color: "#2d3748" }}
                    >
                      {safeName}
                    </h5>
                    {safeDisplayText && (
                      <p
                        className="card-text"
                        style={{
                          color: tc.text,
                          fontWeight: 600,
                          fontSize: "0.9rem",
                        }}
                      >
                        {safeDisplayText}
                      </p>
                    )}
                    {safeDescription && (
                      <p
                        className="card-text text-muted"
                        style={{ fontSize: "0.85rem" }}
                      >
                        {safeDescription}
                      </p>
                    )}
                    {c.minCartTotal > 0 && (
                      <p
                        className="card-text"
                        style={{ fontSize: "0.8rem", color: "#6b7280" }}
                      >
                        <i className="fas fa-info-circle me-1"></i>
                        Minimum sepet: {c.minCartTotal.toFixed(0)} TL
                      </p>
                    )}
                  </div>
                  <div
                    className="card-footer bg-white border-0 d-flex justify-content-between align-items-center"
                    style={{ fontSize: "0.8rem", color: "#9ca3af" }}
                  >
                    <span>
                      <i className="fas fa-calendar-alt me-1"></i>
                      {new Date(c.endDate).toLocaleDateString("tr-TR")}
                      &apos;e kadar
                    </span>
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
