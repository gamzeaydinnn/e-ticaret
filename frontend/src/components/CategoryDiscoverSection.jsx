import React, { memo, useMemo } from "react";
import PropTypes from "prop-types";
import { Link } from "react-router-dom";
import { getApiBaseUrl } from "../config/apiConfig";
import { normalizeCategorySlug } from "../services/categoryServiceReal";
import { useInViewMotion } from "../hooks/useAutoSlideCarousel";
import "./CategoryDiscoverSection.css";

/** Admin görseli yoksa slug bazlı varsayılan görseller */
const IMG_VER = "v3";
const catImg = (file) => `/images/categories/${file}?${IMG_VER}`;

const DEFAULT_CATEGORY_IMAGES = {
  "et-ve-et-urunleri": catImg("et.jpg"),
  "et-tavuk-balik": catImg("et.jpg"),
  "et-tavuk": catImg("et.jpg"),
  "sut-ve-sut-urunleri": catImg("sut.jpg"),
  "sut-urunleri": catImg("sut.jpg"),
  "meyve-ve-sebze": catImg("meyve-sebze.jpg"),
  "meyve-sebze": catImg("meyve-sebze.jpg"),
  icecekler: catImg("icecek.jpg"),
  icecek: catImg("icecek.jpg"),
  atistirmalik: catImg("atistirmalik.jpg"),
  kuruyemis: catImg("atistirmalik.jpg"),
  "dondurma-ve-dondurulmus-gida": catImg("dondurma.jpg"),
  dondurma: catImg("dondurma.jpg"),
  temizlik: catImg("temizlik.jpg"),
  "temizlik-urunleri": catImg("temizlik.jpg"),
  "kagit-islak-mendil": catImg("temizlik.jpg"),
  "temel-gida": catImg("temel-gida.jpg"),
  "hazir-yemek-meze": catImg("temel-gida.jpg"),
  "organik-gurme": catImg("kahvalti.jpg"),
  "ev-ve-mutfak": catImg("ev-mutfak.jpg"),
  "ev-mutfak": catImg("ev-mutfak.jpg"),
  elektronik: catImg("ev-mutfak.jpg"),
  "pet-shop": catImg("default.jpg"),
  "bebek-oyuncak": catImg("kisisel-bakim.jpg"),
  kahvaltilik: catImg("kahvalti.jpg"),
  "firin-pastane": catImg("firin.jpg"),
  "firin-unlu-mamuller": catImg("firin.jpg"),
  "kisisel-bakim": catImg("kisisel-bakim.jpg"),
};

const NAME_IMAGE_HINTS = [
  { keys: ["temizlik", "deterjan", "kağıt", "kagit", "mendil"], image: catImg("temizlik.jpg") },
  { keys: ["ev & mutfak", "ev ve mutfak", "ev yaşam", "ev yasam", "mutfak", "züccaciye"], image: catImg("ev-mutfak.jpg") },
  { keys: ["et ve", "tavuk", "balık", "balik", "kasap"], image: catImg("et.jpg") },
  { keys: ["süt", "sut ürün", "peynir", "yoğurt", "yogurt"], image: catImg("sut.jpg") },
  { keys: ["meyve", "sebze", "manav"], image: catImg("meyve-sebze.jpg") },
  { keys: ["içecek", "icecek", "kola", "çay", "kahve"], image: catImg("icecek.jpg") },
  { keys: ["atıştırmalık", "atistirmalik", "cips", "çikolata", "kuruyemiş", "kuruyemis"], image: catImg("atistirmalik.jpg") },
  { keys: ["dondurma", "dondurulmuş", "dondurulmus"], image: catImg("dondurma.jpg") },
  { keys: ["temel gıda", "temel gida", "bakliyat", "bulgur", "hazır yemek", "meze"], image: catImg("temel-gida.jpg") },
  { keys: ["kahvaltı", "kahvalti", "organik", "gurme"], image: catImg("kahvalti.jpg") },
  { keys: ["fırın", "firin", "pastane", "ekmek"], image: catImg("firin.jpg") },
  { keys: ["kişisel", "kisisel", "kozmetik", "bebek"], image: catImg("kisisel-bakim.jpg") },
];

const FALLBACK_DEFAULT_IMAGE = catImg("default.jpg");

const DISCOUNTED_TILE = {
  id: "indirimli-urunler",
  name: "İndirimli Ürünler",
  slug: "indirimli-urunler",
  imageUrl: catImg("indirimli.jpg"),
  path: "/indirimli-urunler",
  isPromoTile: true,
};

const resolveMediaUrl = (url) => {
  if (!url) return "";
  if (
    url.startsWith("http://") ||
    url.startsWith("https://") ||
    url.startsWith("data:") ||
    url.startsWith("/")
  ) {
    return url;
  }
  const base = getApiBaseUrl().replace(/\/api\/?$/, "");
  return `${base}/${url}`;
};

const getDefaultByName = (name) => {
  const lower = String(name || "").toLocaleLowerCase("tr-TR");
  for (const hint of NAME_IMAGE_HINTS) {
    if (hint.keys.some((k) => lower.includes(k))) {
      return hint.image;
    }
  }
  return "";
};

export const getCategoryDiscoverImage = (category) => {
  const raw = String(
    category?.imageUrl ||
      category?.ImageUrl ||
      category?.image ||
      category?.Image ||
      "",
  ).trim();

  const slug = normalizeCategorySlug(category?.slug || category?.Slug || "");
  const name = category?.name || category?.Name || "";

  if (raw && !/placeholder/i.test(raw)) {
    return resolveMediaUrl(raw);
  }

  if (slug && DEFAULT_CATEGORY_IMAGES[slug]) {
    return DEFAULT_CATEGORY_IMAGES[slug];
  }

  const byName = getDefaultByName(name);
  if (byName) return byName;

  return FALLBACK_DEFAULT_IMAGE;
};

const getCategoryPath = (category) => {
  if (category?.path) return category.path;
  if (category?.isPromoTile) return "/indirimli-urunler";

  const rawSlug = category?.slug || category?.Slug || "";
  const slug = normalizeCategorySlug(rawSlug);
  if (slug) return `/category/${slug}`;
  if (category?.id || category?.Id) {
    return `/category/${category.id || category.Id}`;
  }
  return "/";
};

const CategoryDiscoverItem = memo(function CategoryDiscoverItem({
  category,
  index,
  animate,
}) {
  const imageUrl = category?.isPromoTile
    ? category.imageUrl || catImg("indirimli.jpg")
    : getCategoryDiscoverImage(category);
  const name = category?.name || category?.Name || "Kategori";
  const path = getCategoryPath(category);
  const enterDelayMs = Math.min(index, 20) * 20;
  const isDiscountTile = Boolean(category?.isPromoTile);

  return (
    <div className="category-discover-item-shell">
      <Link
        to={path}
        className={`category-discover-item${isDiscountTile ? " category-discover-item--discount" : ""}${animate ? " category-discover-item--enter is-animated" : ""}`}
        style={{ "--enter-delay": `${enterDelayMs}ms` }}
        aria-label={isDiscountTile ? "İndirimli ürünler" : `${name} kategorisi`}
        title={name}
      >
        <div className="category-discover-icon-wrap">
          {isDiscountTile && (
            <span className="category-discover-discount-badge" aria-hidden="true">
              %
            </span>
          )}
          <img
            src={imageUrl}
            alt={name}
            className="category-discover-image"
            loading={index < 14 ? "eager" : "lazy"}
            onError={(e) => {
              if (e.currentTarget.dataset.fallback === "1") return;
              e.currentTarget.dataset.fallback = "1";
              e.currentTarget.src = isDiscountTile
                ? catImg("indirimli.jpg")
                : FALLBACK_DEFAULT_IMAGE;
            }}
          />
        </div>
        <span className="category-discover-label">{name}</span>
      </Link>
    </div>
  );
});

CategoryDiscoverItem.propTypes = {
  category: PropTypes.object.isRequired,
  index: PropTypes.number.isRequired,
  animate: PropTypes.bool.isRequired,
};

function CategoryDiscoverSection({
  categories = [],
  loading = false,
  title = "Gölköy Gurme'yi Keşfet",
}) {
  const { sectionRef, hasEntered } = useInViewMotion({ threshold: 0.02 });

  // Hem ağaç yapısını hem de düz listeyi destekle: Ana kategorileri çıkar, alt kategorileri içine bağla
  const rootCategories = useMemo(() => {
    if (!Array.isArray(categories) || categories.length === 0) return [];

    const activeList = categories.filter(
      (c) =>
        c &&
        (c.isActive ?? c.IsActive) !== false &&
        String(c.slug ?? c.Slug ?? "").toLowerCase() !== "diger",
    );

    const byParent = new Map();
    for (const item of activeList) {
      const pid = item.parentId ?? item.ParentId ?? null;
      if (pid != null) {
        if (!byParent.has(pid)) byParent.set(pid, []);
        byParent.get(pid).push(item);
      }
    }

    const roots = activeList.filter(
      (c) => (c.parentId ?? c.ParentId ?? null) == null && Number(c.depth || 0) === 0,
    );

    return roots
      .map((root) => {
        const id = root.id ?? root.Id;
        const existingSubs =
          root.subCategories || root.children || root.SubCategories || root.Children || [];
        const attachedSubs = byParent.get(id) || [];
        const mergedSubsMap = new Map();
        for (const s of [...existingSubs, ...attachedSubs]) {
          const sid = s?.id ?? s?.Id ?? s?.slug;
          if (sid != null && (s?.isActive ?? s?.IsActive) !== false) {
            mergedSubsMap.set(sid, s);
          }
        }
        const mergedSubs = Array.from(mergedSubsMap.values()).sort(
          (a, b) => Number(a?.sortOrder ?? 0) - Number(b?.sortOrder ?? 0),
        );

        return {
          ...root,
          subCategories: mergedSubs,
          children: mergedSubs,
        };
      })
      .sort((a, b) => Number(a?.sortOrder ?? 0) - Number(b?.sortOrder ?? 0));
  }, [categories]);

  if (loading) {
    return (
      <section className="category-discover-section">
        <div className="category-discover-inner">
          <div className="category-discover-header">
            <h2 className="category-discover-title">{title}</h2>
          </div>
          <div className="category-discover-scroller category-discover-scroller--loading">
            {Array.from({ length: 8 }).map((_, i) => (
              <div key={`sk-${i}`} className="category-discover-skeleton" />
            ))}
          </div>
        </div>
      </section>
    );
  }

  if (!rootCategories || rootCategories.length === 0) {
    return null;
  }

  const tiles = [DISCOUNTED_TILE, ...rootCategories];

  return (
    <section
      ref={sectionRef}
      className={`category-discover-section${hasEntered ? " is-in-view" : ""}`}
      aria-label={title}
    >
      <div className="category-discover-inner">
        <div className="category-discover-header">
          <h2 className="category-discover-title">{title}</h2>
        </div>

        <div className="category-discover-scroller" role="list">
          {tiles.map((category, index) => (
            <div
              key={category.id ?? category.Id ?? category.slug ?? index}
              className="category-discover-cell"
              role="listitem"
            >
              <CategoryDiscoverItem
                category={category}
                index={index}
                animate={hasEntered}
              />
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}

CategoryDiscoverSection.propTypes = {
  categories: PropTypes.arrayOf(PropTypes.object),
  loading: PropTypes.bool,
  title: PropTypes.string,
};

export default memo(CategoryDiscoverSection);
