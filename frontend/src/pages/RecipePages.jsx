// src/pages/RecipePages.jsx — Şef Tavsiyesi & Gurme Lezzetler
import { useEffect, useMemo, useState } from "react";
import { Link, useNavigate, useParams, useSearchParams } from "react-router-dom";
import {
  RECIPES,
  RECIPE_CATEGORIES,
  RECIPE_COLLECTIONS,
  getRecipeById,
} from "../data/recipes";
import { ProductService, getProductDetailPath } from "../services/productService";
import { useCart } from "../contexts/CartContext";
import { useAuth } from "../contexts/AuthContext";
import CartActionToast, {
  useCartActionToast,
} from "../components/CartActionToast";

const PLACEHOLDER_IMG =
  "https://images.unsplash.com/photo-1495521821757-a1efb6729352?auto=format&fit=crop&w=400&q=80";

const formatPrice = (n) =>
  `${Number(n || 0).toLocaleString("tr-TR", {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  })} TL`;

const unitPrice = (p) =>
  Number(
    p?.specialPrice && p.specialPrice < p.price ? p.specialPrice : p?.price,
  ) || 0;

/* ---------- Akıllı Ürün Eşleştirme (Negatif Kelime Korumalı) ---------- */
const norm = (t) =>
  String(t || "")
    .toLocaleLowerCase("tr-TR")
    .replace(/ı/g, "i")
    .replace(/ğ/g, "g")
    .replace(/ü/g, "u")
    .replace(/ş/g, "s")
    .replace(/ö/g, "o")
    .replace(/ç/g, "c")
    .replace(/[^a-z0-9\s]/g, " ")
    .replace(/\s+/g, " ")
    .trim();

// Malzemelerin asla eşleşmemesi gereken hazır/alakasız kelimeler
const FORBIDDEN_WORDS = [
  "corbasi", "corba", "cips", "aroma", "cikolata", "cikolatali", "findik",
  "findikli", "gofret", "biskuvi", "kek", "puding", "dondurma", "bulyon",
  "harc", "sabun", "deterjan", "sampuan", "kolonya", "bebek", "mama",
];

const scoreProduct = (product, query) => {
  if (!product || !product.name) return -1;
  const name = ` ${norm(product.name)} `;
  const q = norm(query);
  const qTokens = q.split(" ").filter(Boolean);
  if (!qTokens.length) return -1;

  // Arama kelimelerinden en az biri ürün adında kesin geçmeli
  const hasToken = qTokens.some((t) => name.includes(` ${t}`));
  if (!hasToken) return -1;

  let score = 50;

  // Tüm kelimeler geçiyorsa büyük puan
  if (qTokens.every((t) => name.includes(` ${t}`))) score += 40;

  // Tam eşleşme
  if (name.includes(` ${q} `)) score += 30;

  // Alakasız hazır gıda koruması
  for (const w of FORBIDDEN_WORDS) {
    if (!q.includes(w) && name.includes(` ${w}`)) score -= 70;
  }

  // Stok kontrolü
  if (product.stockQuantity != null && product.stockQuantity <= 0) score -= 40;

  // Fotoğrafı varsa önceliklendir
  if (product.imageUrl) score += 15;

  return score;
};

const pickBestProduct = (list, query) => {
  let best = null;
  let bestScore = 0;
  for (const p of list || []) {
    const s = scoreProduct(p, query);
    if (s > bestScore) {
      best = p;
      bestScore = s;
    }
  }
  return best;
};

/* ---------- Güvenli Görsel Bileşeni ---------- */
function RecipeImage({ src, fallback, alt, className, style }) {
  const [imgSrc, setImgSrc] = useState(src || fallback || PLACEHOLDER_IMG);

  useEffect(() => {
    setImgSrc(src || fallback || PLACEHOLDER_IMG);
  }, [src, fallback]);

  return (
    <img
      src={imgSrc}
      alt={alt || "Yemek Tarifi"}
      className={className}
      style={style}
      loading="lazy"
      onError={() => {
        if (imgSrc !== fallback && fallback) {
          setImgSrc(fallback);
        } else if (imgSrc !== PLACEHOLDER_IMG) {
          setImgSrc(PLACEHOLDER_IMG);
        }
      }}
    />
  );
}

const RECIPE_STYLES = `
/* Global Sayfa */
.rcp-page {
  background-color: #f8fafc;
  min-height: 85vh;
  padding: 16px 0 100px;
  color: #0f172a;
  font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
  overflow-x: hidden;
  box-sizing: border-box;
}
.rcp-container {
  max-width: 1200px;
  margin: 0 auto;
  padding: 0 16px;
  box-sizing: border-box;
  width: 100%;
}

/* Ekmek Kırıntısı */
.rcp-breadcrumb {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 0.8rem;
  color: #64748b;
  margin-bottom: 14px;
}
.rcp-breadcrumb a {
  color: #64748b;
  text-decoration: none;
  transition: color 0.15s;
}
.rcp-breadcrumb a:hover {
  color: #ff6b35;
}
.rcp-breadcrumb span {
  color: #0f172a;
  font-weight: 600;
}

/* Başlık ve Koleksiyon Seçimi */
.rcp-header {
  margin-bottom: 20px;
}
.rcp-title-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  flex-wrap: wrap;
  margin-bottom: 8px;
}
.rcp-title-row h1 {
  font-size: 1.75rem;
  font-weight: 800;
  color: #0f172a;
  margin: 0;
  letter-spacing: -0.02em;
}
.rcp-header-subtitle {
  color: #64748b;
  font-size: 0.95rem;
  margin: 0 0 16px;
}

/* Koleksiyon Tab Butonları (Elit Gurme & Şef Tavsiyesi) */
.rcp-collection-tabs {
  display: flex;
  gap: 10px;
  margin-bottom: 16px;
  overflow-x: auto;
  scrollbar-width: none;
  padding-bottom: 2px;
}
.rcp-collection-tabs::-webkit-scrollbar {
  display: none;
}
.rcp-col-tab {
  flex: 0 0 auto;
  padding: 9px 18px;
  border-radius: 999px;
  font-size: 0.86rem;
  font-weight: 700;
  border: 1px solid #e2e8f0;
  background: #ffffff;
  color: #475569;
  cursor: pointer;
  transition: all 0.2s ease;
  display: inline-flex;
  align-items: center;
  gap: 6px;
}
.rcp-col-tab:hover {
  border-color: #cbd5e1;
  color: #0f172a;
}
.rcp-col-tab.active {
  background: #0f172a;
  color: #ffffff;
  border-color: #0f172a;
  box-shadow: 0 4px 12px rgba(15, 23, 42, 0.15);
}
.rcp-col-tab.active-gourmet {
  background: linear-gradient(135deg, #1e293b 0%, #0f172a 100%);
  color: #fbbf24;
  border-color: #334155;
  box-shadow: 0 4px 14px rgba(15, 23, 42, 0.2);
}

/* Kategori Hapları */
.rcp-category-chips {
  display: flex;
  gap: 8px;
  overflow-x: auto;
  padding-bottom: 18px;
  scrollbar-width: none;
}
.rcp-category-chips::-webkit-scrollbar {
  display: none;
}
.rcp-chip {
  flex: 0 0 auto;
  padding: 6px 14px;
  border-radius: 999px;
  font-size: 0.8rem;
  font-weight: 600;
  border: 1px solid #e2e8f0;
  background: #ffffff;
  color: #64748b;
  cursor: pointer;
  transition: all 0.15s ease;
}
.rcp-chip:hover {
  color: #ff6b35;
  border-color: #fed7aa;
}
.rcp-chip.active {
  background: #ff6b35;
  color: #ffffff;
  border-color: #ff6b35;
}

/* Tarif Grid / Kartları */
.rcp-cards-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(270px, 1fr));
  gap: 20px;
}
.rcp-card-item {
  display: flex;
  flex-direction: column;
  background: #ffffff;
  border-radius: 16px;
  overflow: hidden;
  text-decoration: none;
  color: inherit;
  border: 1px solid #e2e8f0;
  box-shadow: 0 2px 8px rgba(15, 23, 42, 0.04);
  transition: transform 0.25s ease, box-shadow 0.25s ease, border-color 0.25s ease;
  min-width: 0;
}
.rcp-card-item:hover {
  transform: translateY(-4px);
  box-shadow: 0 12px 24px rgba(15, 23, 42, 0.08);
  border-color: #cbd5e1;
  color: inherit;
}
.rcp-card-thumb-wrap {
  position: relative;
  width: 100%;
  aspect-ratio: 16 / 10;
  background: #f1f5f9;
  overflow: hidden;
}
.rcp-card-thumb-wrap img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  display: block;
  transition: transform 0.4s ease;
}
.rcp-card-item:hover .rcp-card-thumb-wrap img {
  transform: scale(1.05);
}
.rcp-badge-collection {
  position: absolute;
  top: 10px;
  left: 10px;
  background: rgba(15, 23, 42, 0.85);
  color: #ffffff;
  backdrop-filter: blur(4px);
  font-size: 0.68rem;
  font-weight: 700;
  padding: 4px 10px;
  border-radius: 999px;
  letter-spacing: 0.02em;
}
.rcp-badge-collection.gourmet {
  background: linear-gradient(135deg, rgba(15, 23, 42, 0.95), rgba(30, 41, 59, 0.9));
  color: #fbbf24;
  border: 1px solid rgba(251, 191, 36, 0.3);
}
.rcp-card-content {
  padding: 14px 16px 16px;
  display: flex;
  flex-direction: column;
  flex: 1;
}
.rcp-card-category {
  font-size: 0.72rem;
  font-weight: 700;
  color: #ff6b35;
  text-transform: uppercase;
  letter-spacing: 0.04em;
  margin-bottom: 4px;
}
.rcp-card-title {
  font-size: 1.05rem;
  font-weight: 800;
  color: #0f172a;
  margin: 0 0 6px;
  line-height: 1.3;
}
.rcp-card-sub {
  font-size: 0.82rem;
  color: #64748b;
  margin: 0 0 12px;
  line-height: 1.45;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}
.rcp-card-footer {
  margin-top: auto;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding-top: 10px;
  border-top: 1px solid #f1f5f9;
  font-size: 0.78rem;
  color: #475569;
  font-weight: 600;
}
.rcp-card-footer span i {
  color: #94a3b8;
  margin-right: 5px;
}

/* =========================================================
   TARİF DETAY SAYFASI
   ========================================================= */
.rcp-detail-hero {
  position: relative;
  width: 100%;
  aspect-ratio: 21 / 9;
  min-height: 240px;
  max-height: 400px;
  border-radius: 20px;
  overflow: hidden;
  background: #0f172a;
  margin-bottom: 24px;
  box-shadow: 0 8px 30px rgba(15, 23, 42, 0.12);
}
.rcp-detail-hero img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  display: block;
}
.rcp-detail-hero-overlay {
  position: absolute;
  inset: 0;
  background: linear-gradient(180deg, rgba(15, 23, 42, 0.1) 0%, rgba(15, 23, 42, 0.82) 100%);
  display: flex;
  flex-direction: column;
  justify-content: flex-end;
  padding: 24px 28px;
  color: #ffffff;
  box-sizing: border-box;
}
.rcp-detail-tag-row {
  display: flex;
  gap: 8px;
  margin-bottom: 8px;
}
.rcp-detail-badge {
  background: #ff6b35;
  color: #ffffff;
  font-size: 0.72rem;
  font-weight: 800;
  padding: 4px 12px;
  border-radius: 999px;
  text-transform: uppercase;
  letter-spacing: 0.04em;
}
.rcp-detail-badge.gourmet {
  background: #0f172a;
  color: #fbbf24;
  border: 1px solid rgba(251, 191, 36, 0.4);
}
.rcp-detail-title {
  font-size: 2rem;
  font-weight: 800;
  margin: 0 0 6px;
  letter-spacing: -0.02em;
  text-shadow: 0 2px 8px rgba(0, 0, 0, 0.4);
}
.rcp-detail-subtitle {
  font-size: 0.95rem;
  margin: 0;
  opacity: 0.92;
  max-width: 700px;
  line-height: 1.45;
}

/* 2 Sütunlu Detay Izgarası */
.rcp-detail-layout {
  display: grid;
  grid-template-columns: minmax(0, 1.25fr) minmax(0, 1fr);
  gap: 24px;
  align-items: start;
}

/* Beyaz Kart Panelleri */
.rcp-card-panel {
  background: #ffffff;
  border-radius: 18px;
  padding: 24px;
  border: 1px solid #e2e8f0;
  box-shadow: 0 2px 10px rgba(15, 23, 42, 0.04);
  box-sizing: border-box;
  min-width: 0;
}
.rcp-panel-heading {
  font-size: 1.15rem;
  font-weight: 800;
  color: #0f172a;
  margin: 0 0 16px;
  display: flex;
  align-items: center;
  gap: 8px;
}
.rcp-panel-heading i {
  color: #ff6b35;
}

/* Metrik Kutuları (Süre, Porsiyon vb.) */
.rcp-metrics-bar {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  border: 1px solid #e2e8f0;
  border-radius: 14px;
  margin-bottom: 20px;
  background: #fafafa;
}
.rcp-metric-item {
  padding: 12px 6px;
  text-align: center;
  min-width: 0;
}
.rcp-metric-item + .rcp-metric-item {
  border-left: 1px solid #e2e8f0;
}
.rcp-metric-item i {
  color: #ff6b35;
  font-size: 1rem;
  margin-bottom: 4px;
  display: block;
}
.rcp-metric-val {
  font-size: 0.95rem;
  font-weight: 800;
  color: #0f172a;
  display: block;
}
.rcp-metric-lbl {
  font-size: 0.7rem;
  color: #64748b;
  font-weight: 600;
}

/* Şefin Notu */
.rcp-chef-note-card {
  background: #fffbeb;
  border: 1px solid #fef08a;
  border-radius: 14px;
  padding: 14px 16px;
  margin-bottom: 20px;
  font-size: 0.88rem;
  color: #78350f;
  line-height: 1.55;
  display: flex;
  gap: 10px;
}
.rcp-chef-note-card i {
  color: #f59e0b;
  font-size: 1.1rem;
  margin-top: 2px;
  flex-shrink: 0;
}

/* Adım Adım Yapılışı */
.rcp-steps-list {
  list-style: none;
  padding: 0;
  margin: 0;
  counter-reset: step-counter;
}
.rcp-step-row {
  counter-increment: step-counter;
  display: flex;
  gap: 14px;
  padding: 14px 0;
  border-top: 1px solid #f1f5f9;
  font-size: 0.92rem;
  line-height: 1.6;
  color: #334155;
}
.rcp-step-row:first-child {
  border-top: none;
  padding-top: 0;
}
.rcp-step-num {
  content: counter(step-counter);
  flex: 0 0 30px;
  height: 30px;
  border-radius: 50%;
  background: #0f172a;
  color: #ffffff;
  font-weight: 800;
  font-size: 0.85rem;
  display: flex;
  align-items: center;
  justify-content: center;
}

/* Sağ Sütun: Malzemeler Listesi */
.rcp-sticky-side {
  position: sticky;
  top: 86px;
}
.rcp-ingredient-item {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 12px 0;
  border-top: 1px solid #f1f5f9;
  min-width: 0;
}
.rcp-ingredient-item:first-of-type {
  border-top: none;
  padding-top: 4px;
}
.rcp-ing-photo {
  width: 52px;
  height: 52px;
  border-radius: 12px;
  background: #f8fafc;
  border: 1px solid #e2e8f0;
  flex: 0 0 52px;
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
}
.rcp-ing-photo img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  display: block;
}
.rcp-ing-texts {
  flex: 1;
  min-width: 0;
}
.rcp-ing-name {
  font-size: 0.9rem;
  font-weight: 700;
  color: #0f172a;
  display: block;
  line-height: 1.25;
}
.rcp-ing-amount {
  font-size: 0.74rem;
  color: #64748b;
  display: block;
  margin-top: 2px;
}
.rcp-ing-matched-link {
  font-size: 0.74rem;
  color: #0284c7;
  text-decoration: none;
  display: block;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  margin-top: 2px;
}
.rcp-ing-matched-link:hover {
  text-decoration: underline;
  color: #ff6b35;
}
.rcp-ing-status-missing {
  font-size: 0.72rem;
  color: #94a3b8;
  font-weight: 500;
}
.rcp-ing-action-box {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 4px;
  flex: 0 0 auto;
}
.rcp-ing-price-val {
  font-size: 0.88rem;
  font-weight: 800;
  color: #0f172a;
}
.rcp-single-add-btn {
  border: 1px solid #ff6b35;
  background: #fff;
  color: #ff6b35;
  border-radius: 8px;
  padding: 4px 10px;
  font-size: 0.72rem;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.15s ease;
  white-space: nowrap;
}
.rcp-single-add-btn:hover {
  background: #ff6b35;
  color: #ffffff;
}
.rcp-single-add-btn.added {
  background: #10b981;
  border-color: #10b981;
  color: #ffffff;
}

/* Malzemeler Altı Toplam & Sepete Ekle Butonu */
.rcp-basket-summary {
  margin-top: 16px;
  padding-top: 16px;
  border-top: 1px dashed #cbd5e1;
}
.rcp-basket-total-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 12px;
  font-size: 0.9rem;
  color: #475569;
  font-weight: 600;
}
.rcp-basket-total-row strong {
  font-size: 1.2rem;
  color: #0f172a;
  font-weight: 800;
}
.rcp-btn-add-all-cart {
  width: 100%;
  border: none;
  background: #ff6b35;
  color: #ffffff;
  font-weight: 800;
  font-size: 0.95rem;
  padding: 14px;
  border-radius: 12px;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  box-shadow: 0 4px 14px rgba(255, 107, 53, 0.25);
  transition: background 0.15s, transform 0.15s;
}
.rcp-btn-add-all-cart:hover {
  background: #ea580c;
  transform: translateY(-1px);
}
.rcp-btn-add-all-cart:disabled {
  opacity: 0.6;
  cursor: not-allowed;
  transform: none;
}

/* İskelet Yükleme */
.rcp-skeleton-row {
  height: 52px;
  border-radius: 10px;
  margin-bottom: 10px;
  background: linear-gradient(90deg, #f1f5f9 25%, #e2e8f0 50%, #f1f5f9 75%);
  background-size: 200% 100%;
  animation: rcpShimmer 1.4s infinite;
}
@keyframes rcpShimmer {
  0% { background-position: 200% 0; }
  100% { background-position: -200% 0; }
}

/* =========================================================
   MOBİL OPTİMİZASYON (Taşma / Sığmama Kesin Çözüm)
   ========================================================= */
@media (max-width: 900px) {
  .rcp-detail-layout {
    grid-template-columns: minmax(0, 1fr);
    gap: 16px;
  }
  .rcp-sticky-side {
    position: static;
  }
}

@media (max-width: 576px) {
  .rcp-page {
    padding: 10px 0 120px; /* Mobilde alt navigasyonun altında kalmaması için 120px pay */
  }
  .rcp-container {
    padding: 0 12px;
  }
  .rcp-title-row h1 {
    font-size: 1.35rem;
  }
  .rcp-header-subtitle {
    font-size: 0.82rem;
    margin-bottom: 12px;
  }
  .rcp-cards-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 10px;
  }
  .rcp-card-thumb-wrap {
    aspect-ratio: 4 / 3;
  }
  .rcp-card-content {
    padding: 10px;
  }
  .rcp-card-title {
    font-size: 0.84rem;
    margin-bottom: 4px;
  }
  .rcp-card-sub {
    display: none; /* Mobilde kartları şişirmemek için kaldırıldı */
  }
  .rcp-card-footer {
    font-size: 0.68rem;
    padding-top: 6px;
  }

  /* Detay Sayfası Mobilde Sıkışıklığı Önleme */
  .rcp-detail-hero {
    aspect-ratio: 16 / 10;
    min-height: 200px;
    max-height: 240px;
    border-radius: 14px;
    margin-bottom: 14px;
  }
  .rcp-detail-hero-overlay {
    padding: 14px 16px;
  }
  .rcp-detail-title {
    font-size: 1.35rem;
  }
  .rcp-detail-subtitle {
    font-size: 0.8rem;
  }
  .rcp-card-panel {
    padding: 16px 14px;
    border-radius: 14px;
  }
  .rcp-metrics-bar {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
  .rcp-metric-item:nth-child(3) {
    border-left: none;
  }
  .rcp-metric-item:nth-child(n+3) {
    border-top: 1px solid #e2e8f0;
  }
  .rcp-step-row {
    font-size: 0.86rem;
    gap: 10px;
  }
  .rcp-step-num {
    flex-basis: 26px;
    height: 26px;
    font-size: 0.78rem;
  }
  .rcp-ing-photo {
    width: 44px;
    height: 44px;
    flex-basis: 44px;
    border-radius: 8px;
  }
  .rcp-ing-name {
    font-size: 0.82rem;
  }
  .rcp-ing-amount {
    font-size: 0.7rem;
  }
  .rcp-ing-price-val {
    font-size: 0.8rem;
  }
  .rcp-single-add-btn {
    padding: 3px 8px;
    font-size: 0.68rem;
  }
}
`;

/* =========================================================
   1. TARİF LİSTESİ BİLEŞENİ
   ========================================================= */
export function RecipesPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const initialCollection = searchParams.get("koleksiyon") || "tumu";
  const [selectedCollection, setSelectedCollection] = useState(initialCollection);
  const [selectedCategory, setSelectedCategory] = useState("Tümü");

  useEffect(() => {
    const colParam = searchParams.get("koleksiyon");
    if (colParam) setSelectedCollection(colParam);
  }, [searchParams]);

  const handleCollectionChange = (colId) => {
    setSelectedCollection(colId);
    if (colId === "tumu") {
      searchParams.delete("koleksiyon");
    } else {
      searchParams.set("koleksiyon", colId);
    }
    setSearchParams(searchParams);
  };

  const filteredRecipes = useMemo(() => {
    return RECIPES.filter((r) => {
      const matchCol =
        selectedCollection === "tumu" || r.collection === selectedCollection;
      const matchCat =
        selectedCategory === "Tümü" || r.category === selectedCategory;
      return matchCol && matchCat;
    });
  }, [selectedCollection, selectedCategory]);

  return (
    <div className="rcp-page">
      <style>{RECIPE_STYLES}</style>
      <div className="rcp-container">
        {/* Breadcrumb */}
        <div className="rcp-breadcrumb">
          <Link to="/">Anasayfa</Link>
          <i className="fas fa-angle-right" style={{ fontSize: "0.7rem" }}></i>
          <span>
            {selectedCollection === "gurme-lezzetler"
              ? "Gurme Lezzetler"
              : selectedCollection === "sef-tavsiyesi"
              ? "Şef Tavsiyesi"
              : "Yemek Tarifleri"}
          </span>
        </div>

        {/* Başlık ve Açıklama */}
        <div className="rcp-header">
          <div className="rcp-title-row">
            <h1>
              {selectedCollection === "gurme-lezzetler"
                ? "✨ Gurme Lezzetler"
                : selectedCollection === "sef-tavsiyesi"
                ? "👨‍🍳 Şef Tavsiyesi"
                : "Gölköy Gurme Tarifler"}
            </h1>
          </div>
          <p className="rcp-header-subtitle">
            Usta şeflerimizden özel tarifler; tüm malzemeleri tek tıkla sepetinize ekleyin.
          </p>

          {/* Koleksiyon Seçimi (Elit Gurme & Şef Tavsiyesi) */}
          <div className="rcp-collection-tabs">
            {RECIPE_COLLECTIONS.map((c) => {
              const isSelected = selectedCollection === c.id;
              const isGourmet = c.id === "gurme-lezzetler";
              return (
                <button
                  key={c.id}
                  type="button"
                  className={`rcp-col-tab${
                    isSelected ? (isGourmet ? " active-gourmet" : " active") : ""
                  }`}
                  onClick={() => handleCollectionChange(c.id)}
                >
                  {c.name}
                </button>
              );
            })}
          </div>

          {/* Kategori Filtresi */}
          <div className="rcp-category-chips">
            {RECIPE_CATEGORIES.map((cat) => (
              <button
                key={cat}
                type="button"
                className={`rcp-chip${selectedCategory === cat ? " active" : ""}`}
                onClick={() => setSelectedCategory(cat)}
              >
                {cat}
              </button>
            ))}
          </div>
        </div>

        {/* Tarif Kartları Grid */}
        <div className="rcp-cards-grid">
          {filteredRecipes.map((r) => {
            const isGourmet = r.collection === "gurme-lezzetler";
            return (
              <Link key={r.id} to={`/tarif/${r.id}`} className="rcp-card-item">
                <div className="rcp-card-thumb-wrap">
                  <RecipeImage src={r.image} alt={r.title} />
                  <span
                    className={`rcp-badge-collection${
                      isGourmet ? " gourmet" : ""
                    }`}
                  >
                    {isGourmet ? "✨ Gurme Seçki" : "👨‍🍳 Şef Tavsiyesi"}
                  </span>
                </div>
                <div className="rcp-card-content">
                  <span className="rcp-card-category">{r.category}</span>
                  <h3 className="rcp-card-title">{r.title}</h3>
                  <p className="rcp-card-sub">{r.subtitle}</p>
                  <div className="rcp-card-footer">
                    <span>
                      <i className="far fa-clock"></i>
                      {r.time} dk
                    </span>
                    <span>
                      <i className="fas fa-utensils"></i>
                      {r.servings} kişilik
                    </span>
                    <span>
                      <i className="fas fa-signal"></i>
                      {r.difficulty}
                    </span>
                  </div>
                </div>
              </Link>
            );
          })}
        </div>
      </div>
    </div>
  );
}

/* =========================================================
   2. TARİF DETAY VE SEPETE EKLEME BİLEŞENİ
   ========================================================= */
export function RecipeDetailPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const recipe = getRecipeById(id);
  const { addToCart } = useCart();
  const { user } = useAuth();
  const { notification, showCartSuccess, showCartError, dismiss } =
    useCartActionToast();

  const [matches, setMatches] = useState({});
  const [loading, setLoading] = useState(true);
  const [addingAll, setAddingAll] = useState(false);
  const [addedItems, setAddedItems] = useState({});

  useEffect(() => {
    window.scrollTo({ top: 0, behavior: "instant" });
  }, [id]);

  useEffect(() => {
    if (!recipe) return undefined;
    let cancelled = false;
    setLoading(true);

    (async () => {
      const result = {};
      await Promise.all(
        recipe.ingredients.map(async (ing, index) => {
          try {
            const found = await ProductService.search(ing.query, 1, 30);
            result[index] = pickBestProduct(
              Array.isArray(found) ? found : [],
              ing.query,
            );
          } catch {
            result[index] = null;
          }
        }),
      );
      if (!cancelled) {
        setMatches(result);
        setLoading(false);
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [recipe]);

  const availableProducts = useMemo(
    () => Object.values(matches).filter(Boolean),
    [matches],
  );

  const totalPrice = useMemo(
    () => availableProducts.reduce((sum, p) => sum + unitPrice(p), 0),
    [availableProducts],
  );

  if (!recipe) {
    return (
      <div className="rcp-page">
        <style>{RECIPE_STYLES}</style>
        <div className="rcp-container text-center" style={{ paddingTop: 60 }}>
          <h3 className="fw-bold mb-2">Tarif Bulunamadı</h3>
          <p className="text-muted mb-4">Aradığınız yemek tarifi yayından kaldırılmış olabilir.</p>
          <button
            className="btn btn-warning px-4 py-2 fw-bold text-white"
            onClick={() => navigate("/tarifler")}
          >
            Tüm Tariflere Dön
          </button>
        </div>
      </div>
    );
  }

  const isGourmet = recipe.collection === "gurme-lezzetler";

  const handleAddSingle = async (product, index) => {
    if (!product) return;
    const res = await addToCart(product, 1);
    if (res && res.success === false) {
      showCartError(res.error || "Ürün sepete eklenemedi.");
      return;
    }
    setAddedItems((prev) => ({ ...prev, [index]: true }));
    showCartSuccess(product, user ? "registered" : "guest");
    setTimeout(() => {
      setAddedItems((prev) => ({ ...prev, [index]: false }));
    }, 2500);
  };

  const handleAddAll = async () => {
    if (availableProducts.length === 0) return;
    setAddingAll(true);
    let failedCount = 0;
    for (const p of availableProducts) {
      // eslint-disable-next-line no-await-in-loop
      const res = await addToCart(p, 1);
      if (res && res.success === false) failedCount += 1;
    }
    setAddingAll(false);
    if (failedCount > 0) {
      showCartError(`${failedCount} ürün sepete eklenemedi.`);
    } else {
      showCartSuccess(availableProducts[0], user ? "registered" : "guest");
    }
  };

  return (
    <div className="rcp-page">
      <style>{RECIPE_STYLES}</style>
      <CartActionToast notification={notification} onDismiss={dismiss} />

      <div className="rcp-container">
        {/* Ekmek Kırıntısı */}
        <div className="rcp-breadcrumb">
          <Link to="/">Anasayfa</Link>
          <i className="fas fa-angle-right" style={{ fontSize: "0.7rem" }}></i>
          <Link
            to={
              isGourmet
                ? "/tarifler?koleksiyon=gurme-lezzetler"
                : "/tarifler?koleksiyon=sef-tavsiyesi"
            }
          >
            {isGourmet ? "Gurme Lezzetler" : "Şef Tavsiyesi"}
          </Link>
          <i className="fas fa-angle-right" style={{ fontSize: "0.7rem" }}></i>
          <span>{recipe.title}</span>
        </div>

        {/* Hero Görseli & Başlık Alanı */}
        <div className="rcp-detail-hero">
          <RecipeImage src={recipe.image} alt={recipe.title} />
          <div className="rcp-detail-hero-overlay">
            <div className="rcp-detail-tag-row">
              <span
                className={`rcp-detail-badge${isGourmet ? " gourmet" : ""}`}
              >
                {isGourmet ? "✨ Gurme Seçki" : "👨‍🍳 Şef Tavsiyesi"}
              </span>
              <span
                className="rcp-detail-badge"
                style={{ background: "rgba(255,255,255,0.2)", backdropFilter: "blur(4px)" }}
              >
                {recipe.category}
              </span>
            </div>
            <h1 className="rcp-detail-title">{recipe.title}</h1>
            <p className="rcp-detail-subtitle">{recipe.subtitle}</p>
          </div>
        </div>

        {/* 2 Sütunlu İçerik ve Malzemeler Izgarası */}
        <div className="rcp-detail-layout">
          {/* Sol Panel: Yapılışı ve İpuçları */}
          <div style={{ minWidth: 0 }}>
            <div className="rcp-card-panel">
              {/* Metrikler */}
              <div className="rcp-metrics-bar">
                <div className="rcp-metric-item">
                  <i className="far fa-clock"></i>
                  <span className="rcp-metric-val">{recipe.time} dk</span>
                  <span className="rcp-metric-lbl">Hazırlama</span>
                </div>
                <div className="rcp-metric-item">
                  <i className="fas fa-utensils"></i>
                  <span className="rcp-metric-val">{recipe.servings} Kişilik</span>
                  <span className="rcp-metric-lbl">Porsiyon</span>
                </div>
                <div className="rcp-metric-item">
                  <i className="fas fa-signal"></i>
                  <span className="rcp-metric-val">{recipe.difficulty}</span>
                  <span className="rcp-metric-lbl">Zorluk</span>
                </div>
                <div className="rcp-metric-item">
                  <i className="fas fa-fire-alt"></i>
                  <span className="rcp-metric-val">{recipe.calories} kcal</span>
                  <span className="rcp-metric-lbl">Kalori</span>
                </div>
              </div>

              {/* Şefin Notu */}
              {recipe.chefNote && (
                <div className="rcp-chef-note-card">
                  <i className="fas fa-award"></i>
                  <div>
                    <strong>Şefin Püf Noktası:</strong> {recipe.chefNote}
                  </div>
                </div>
              )}

              {/* Yapılış Adımları */}
              <h2 className="rcp-panel-heading">
                <i className="fas fa-list-ol"></i> Adım Adım Hazırlanışı
              </h2>
              <ol className="rcp-steps-list">
                {recipe.steps.map((stepText, idx) => (
                  <li key={idx} className="rcp-step-row">
                    <span className="rcp-step-num">{idx + 1}</span>
                    <div>{stepText}</div>
                  </li>
                ))}
              </ol>
            </div>
          </div>

          {/* Sağ Panel: Malzemeler ve Sepete Ekleme */}
          <div className="rcp-sticky-side" style={{ minWidth: 0 }}>
            <div className="rcp-card-panel">
              <h2 className="rcp-panel-heading">
                <i className="fas fa-shopping-basket"></i> Tarif Malzemeleri
              </h2>

              {loading ? (
                <>
                  <div className="rcp-skeleton-row" />
                  <div className="rcp-skeleton-row" />
                  <div className="rcp-skeleton-row" />
                </>
              ) : (
                recipe.ingredients.map((ing, idx) => {
                  const matched = matches[idx];
                  const isAdded = addedItems[idx];
                  return (
                    <div key={idx} className="rcp-ingredient-item">
                      <div className="rcp-ing-photo">
                        <RecipeImage
                          src={matched?.imageUrl}
                          fallback={ing.defaultImg}
                          alt={ing.name}
                        />
                      </div>
                      <div className="rcp-ing-texts">
                        <span className="rcp-ing-name">{ing.name}</span>
                        <span className="rcp-ing-amount">{ing.amount}</span>
                        {matched ? (
                          <Link
                            to={getProductDetailPath(matched)}
                            className="rcp-ing-matched-link"
                            title={matched.name}
                          >
                            <i className="fas fa-check-circle me-1 text-success"></i>
                            {matched.name}
                          </Link>
                        ) : (
                          <span className="rcp-ing-status-missing">
                            Mağazada bulunamadı
                          </span>
                        )}
                      </div>
                      {matched && (
                        <div className="rcp-ing-action-box">
                          <span className="rcp-ing-price-val">
                            {formatPrice(unitPrice(matched))}
                          </span>
                          <button
                            type="button"
                            className={`rcp-single-add-btn${isAdded ? " added" : ""}`}
                            onClick={() => handleAddSingle(matched, idx)}
                          >
                            {isAdded ? "Eklendi" : "+ Ekle"}
                          </button>
                        </div>
                      )}
                    </div>
                  );
                })
              )}

              {/* Sepet Özeti ve Toplu Ekleme Butonu */}
              {!loading && (
                <div className="rcp-basket-summary">
                  <div className="rcp-basket-total-row">
                    <span>Mevcut Malzemeler ({availableProducts.length} adet)</span>
                    <strong>{formatPrice(totalPrice)}</strong>
                  </div>
                  <button
                    type="button"
                    className="rcp-btn-add-all-cart"
                    disabled={addingAll || availableProducts.length === 0}
                    onClick={handleAddAll}
                  >
                    <i className="fas fa-cart-plus"></i>
                    {addingAll ? "Sepete Ekleniyor..." : "Tüm Malzemeleri Sepete Ekle"}
                  </button>
                </div>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
