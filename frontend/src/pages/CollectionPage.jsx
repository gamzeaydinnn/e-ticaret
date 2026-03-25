/**
 * CollectionPage.jsx - Blok/Koleksiyon Ürün Sayfası
 *
 * Manuel seçilmiş ürünlerin "Tümünü Gör" sayfası.
 * URL: /collection/:slug
 *
 * Özellikler:
 * - Blok başlığı gösterilir
 * - Tüm seçili ürünler grid olarak listelenir
 * - Sıralama ve filtreleme desteği
 * - Pagination / "Daha Fazla Yükle" butonu
 *
 * @version 2.0.0 - Pagination eklendi
 */

import React, { useState, useEffect } from "react";
import { useParams, Link } from "react-router-dom";
import homeBlockService from "../services/homeBlockService";
import ProductBlockSection from "./components/ProductBlockSection";
import "./CollectionPage.css";

const CollectionPage = () => {
  const { slug } = useParams();

  // ============================================
  // STATE - HOOK'LAR EN ÜSTTE OLMALI
  // ============================================
  const [block, setBlock] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  // ============================================
  // VERİ ÇEKME
  // ============================================
  useEffect(() => {
    const fetchBlock = async () => {
      try {
        setLoading(true);
        const data = await homeBlockService.getBlockBySlug(slug);

        console.log("📦 CollectionPage - API Response:", {
          slug,
          data,
          products: data?.products || data?.Products,
          blockType: data?.blockType || data?.BlockType,
        });

        if (data) {
          const normalizedBlock = {
            ...data,
            id: data.id || data.Id,
            products: data.products || data.Products || [],
            title: data.title || data.Title || data.name || data.Name,
            blockType: data.blockType || data.BlockType || "manual",
            posterImageUrl: data.posterImageUrl || data.PosterImageUrl || "",
            isActive: data.isActive ?? data.IsActive ?? true,
            viewAllText: "",
          };
          setBlock(normalizedBlock);
        } else {
          setError("Koleksiyon bulunamadı");
        }
      } catch (err) {
        console.error("❌ Koleksiyon yüklenirken hata:", err);
        setError("Koleksiyon yüklenirken bir hata oluştu");
      } finally {
        setLoading(false);
      }
    };

    if (slug) {
      fetchBlock();
    }
  }, [slug]);

  const products = block?.products || [];
  const title = block?.title || block?.name || "Koleksiyon";

  // ============================================
  // RENDER - CONDITIONAL RENDER EN SONDA
  // ============================================
  if (loading) {
    return (
      <div className="collection-page">
        <div className="container py-5">
          <div className="text-center">
            <div className="spinner-border text-primary" role="status">
              <span className="visually-hidden">Yükleniyor...</span>
            </div>
            <p className="mt-3 text-muted">Koleksiyon yükleniyor...</p>
          </div>
        </div>
      </div>
    );
  }

  if (error || !block) {
    return (
      <div className="collection-page">
        <div className="container py-5">
          <div className="text-center">
            <i className="fas fa-exclamation-circle fa-4x text-muted mb-3"></i>
            <h3 className="text-muted">{error || "Koleksiyon bulunamadı"}</h3>
            <Link to="/" className="btn btn-primary mt-3">
              <i className="fas fa-home me-2"></i>
              Ana Sayfaya Dön
            </Link>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="collection-page">
      <div className="container py-4">
        {products.length === 0 ? (
          <div className="text-center py-5">
            <i className="fas fa-box-open fa-4x text-muted mb-3"></i>
            <h4 className="text-muted">Bu koleksiyonda henüz ürün yok</h4>
            {/* Blok tipi bilgisi - debug amaçlı */}
            <p className="text-muted small mt-2">
              <span className="badge bg-secondary me-2">
                {block.blockType || "manual"}
              </span>
              {block.blockType === "discounted" && (
                <span>
                  İndirimli ürün bulunamadı. Ürünlere indirimli fiyat
                  tanımlayın.
                </span>
              )}
              {block.blockType === "category" && (
                <span>Bu kategoride ürün bulunmuyor.</span>
              )}
              {block.blockType === "manual" && (
                <span>Admin panelden bu bloğa ürün ekleyin.</span>
              )}
            </p>
            <Link to="/" className="btn btn-outline-primary mt-3">
              <i className="fas fa-home me-2"></i>
              Ana Sayfaya Dön
            </Link>
          </div>
        ) : (
          <ProductBlockSection block={block} showViewAllButton={false} />
        )}
      </div>
    </div>
  );
};

export default CollectionPage;
