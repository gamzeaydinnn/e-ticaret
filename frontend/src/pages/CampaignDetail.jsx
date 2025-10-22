import React, { useEffect, useState } from "react";
import { Helmet } from "react-helmet-async";
import { useParams } from "react-router-dom";
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
    return <div className="text-center py-5 text-muted">Yükleniyor...</div>;
  }

  if (error) {
    return (
      <div className="container px-4 py-4">
        <div className="alert alert-danger">{error}</div>
      </div>
    );
  }

  if (!campaign) return null;

  return (
    <>
      <Helmet>
        <title>{campaign.title} — Doğadan Sofranza</title>
        <meta
          name="description"
          content={
            campaign.summary || (campaign.description || "").slice(0, 150)
          }
        />
        <meta property="og:title" content={campaign.title} />
        <meta property="og:description" content={campaign.summary || ""} />
        <meta
          property="og:image"
          content={campaign.image || "/images/og-default.jpg"}
        />
        <link
          rel="canonical"
          href={typeof window !== "undefined" ? window.location.href : ""}
        />
      </Helmet>
      <div className="container-fluid px-4 py-4">
        <div className="mb-4">
          <div className="position-relative rounded-3 overflow-hidden shadow-sm">
            {campaign.image && (
              <img
                src={campaign.image}
                alt={campaign.title}
                className="w-100"
                style={{ maxHeight: 380, objectFit: "cover" }}
              />
            )}
            {campaign.badge && (
              <span
                className="position-absolute top-0 start-0 m-3 px-3 py-1 rounded-pill"
                style={{
                  background: "linear-gradient(135deg, #ff6b35, #ff8c00)",
                  color: "white",
                  fontWeight: 700,
                }}
              >
                {campaign.badge}
              </span>
            )}
          </div>
        </div>

        <div className="mb-4">
          <h1 className="h3 fw-bold" style={{ color: "#2d3748" }}>
            {campaign.title}
          </h1>
          {campaign.summary && (
            <div className="text-muted mb-2">{campaign.summary}</div>
          )}
          {campaign.description && (
            <div className="text-secondary" style={{ maxWidth: 840 }}>
              {campaign.description}
            </div>
          )}
        </div>

        {campaign.categoryId ? (
          <div className="mt-4">
            <h2 className="h5 fw-bold mb-3" style={{ color: "#2d3748" }}>
              Kampanyalı Ürünler
            </h2>
            <ProductGrid categoryId={campaign.categoryId} />
          </div>
        ) : null}
      </div>
    </>
  );
}
