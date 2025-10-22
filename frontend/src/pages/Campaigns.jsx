import React, { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { CampaignService } from "../services/campaignService";

export default function Campaigns() {
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const navigate = useNavigate();

  useEffect(() => {
    let mounted = true;
    setLoading(true);
    setError("");

    CampaignService.list()
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

  return (
    <div className="container-fluid px-4 py-4">
      <div className="d-flex align-items-center justify-content-between mb-4">
        <div>
          <h1 className="h3 fw-bold mb-1" style={{ color: "#2d3748" }}>
            <i className="fas fa-bullhorn me-2" style={{ color: "#f57c00" }}></i>
            Kampanyalar
          </h1>
          <div className="text-muted">Özel fırsat ve indirimler</div>
        </div>
      </div>

      {loading && (
        <div className="text-center py-5 text-muted">Yükleniyor...</div>
      )}
      {error && <div className="alert alert-danger">{error}</div>}

      {!loading && !error && (
        <div className="row">
          {items.map((c) => (
            <div key={c.slug} className="col-lg-4 col-md-6 mb-4">
              <div
                className="card h-100 shadow-sm border-0"
                style={{ cursor: "pointer", borderRadius: 16 }}
                onClick={() => navigate(`/campaigns/${c.slug}`)}
              >
                {c.image && (
                  <div className="position-relative">
                    <img
                      src={c.image}
                      alt={c.title}
                      className="card-img-top"
                      style={{ height: 200, objectFit: "cover" }}
                    />
                    {c.badge && (
                      <span
                        className="position-absolute top-0 start-0 m-2 px-3 py-1 rounded-pill"
                        style={{
                          background: "linear-gradient(135deg, #ff6b35, #ff8c00)",
                          color: "white",
                          fontWeight: 700,
                        }}
                      >
                        {c.badge}
                      </span>
                    )}
                  </div>
                )}
                <div className="card-body">
                  <h5 className="card-title" style={{ color: "#2d3748" }}>
                    {c.title}
                  </h5>
                  {c.summary && (
                    <p className="card-text text-muted">{c.summary}</p>
                  )}
                </div>
                <div className="card-footer bg-white border-0">
                  <button className="btn btn-outline-warning w-100">
                    Detaylar
                  </button>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

