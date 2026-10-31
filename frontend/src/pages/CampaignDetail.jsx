import React, { useEffect, useState } from "react";
import { Helmet } from "react-helmet-async";
import { useParams, Link } from "react-router-dom";
import { CampaignService } from "../services/campaignService";
import ProductGrid from "../components/ProductGrid";

export default function CampaignDetail() {
  const { slug } = useParams();
  const [campaign, setCampaign] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    let mounted = true;
    setLoading(true);
    setError("");

    CampaignService.getBySlug(slug)
      .then((c) => {
        if (!mounted) return;
        setCampaign(c);
      })
      .catch((e) => setError(e?.message || "Kampanya bulunamadı"))
      .finally(() => mounted && setLoading(false));

    return () => {
      mounted = false;
    };
  }, [slug]);

  if (loading) {
    return (
      <div className="container py-5 text-center text-muted">
        <i className="fas fa-spinner fa-spin fa-2x mb-3 text-warning"></i>
        <p>Ürünler yükleniyor...</p>
      </div>
    );
  }

  if (error || !campaign) {
    return (
      <div className="container px-4 py-5 text-center">
        <div className="alert alert-warning d-inline-block px-4 py-3">
          <i className="fas fa-exclamation-triangle me-2"></i>
          {error || "Kampanya bulunamadı."}
        </div>
        <div className="mt-3">
          <Link to="/" className="btn btn-outline-secondary btn-sm">
            Ana Sayfaya Dön
          </Link>
        </div>
      </div>
    );
  }

  return (
    <>
      <Helmet>
        <title>{campaign.title} — Gölköy Gurme</title>
      </Helmet>

      <div
        className="container-fluid px-3 px-md-4 py-3"
        style={{ maxWidth: "1400px", margin: "0 auto" }}
      >
        {/* Sade ve Temiz Başlık Alanı (Bozuk/büyük afiş yok, indirim tekrarı yok) */}
        <div className="d-flex align-items-center justify-content-between flex-wrap gap-2 pb-2 mb-3 border-bottom">
          <div className="d-flex align-items-center gap-2">
            <h1 className="h4 fw-bold mb-0 text-dark">{campaign.title}</h1>
            {campaign.badge && (
              <span
                className="badge rounded-pill px-3 py-1 ms-2"
                style={{
                  background: "linear-gradient(135deg, #ff6b35, #ff8c00)",
                  color: "#fff",
                  fontSize: "0.85rem",
                  fontWeight: 600,
                }}
              >
                {campaign.badge}
              </span>
            )}
          </div>

          <Link to="/" className="btn btn-sm btn-outline-secondary">
            <i className="fas fa-arrow-left me-1"></i>
            Ana Sayfa
          </Link>
        </div>

        {/* Doğrudan Ürünler Grid Görünümü */}
        <div className="mt-2">
          {campaign.categoryId ? (
            <ProductGrid
              categoryId={campaign.categoryId}
              showTitle={false}
              showViewAll={false}
              displayMode="grid"
              initialPageSize={50}
            />
          ) : (
            <ProductGrid
              showTitle={false}
              showViewAll={false}
              displayMode="grid"
              initialPageSize={50}
            />
          )}
        </div>
      </div>
    </>
  );
}
