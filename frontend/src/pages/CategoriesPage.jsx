import React, { useEffect, useState } from "react";
import { Helmet } from "react-helmet-async";
import categoryServiceReal from "../services/categoryServiceReal";
import CategoryDiscoverSection from "../components/CategoryDiscoverSection";

/**
 * /kategoriler — Google sitelink ve footer için gerçek kategori listesi.
 */
export default function CategoriesPage() {
  const [categories, setCategories] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let mounted = true;
    (async () => {
      try {
        const tree = await categoryServiceReal.getCategoryTree();
        const list = Array.isArray(tree) ? tree : [];
        if (mounted) setCategories(list);
      } catch (err) {
        console.error("[CategoriesPage] yüklenemedi:", err);
        if (mounted) setCategories([]);
      } finally {
        if (mounted) setLoading(false);
      }
    })();
    return () => {
      mounted = false;
    };
  }, []);

  return (
    <div style={{ maxWidth: "1200px", margin: "0 auto", padding: "16px" }}>
      <Helmet>
        <title>Kategoriler | Gölköy Gurme</title>
        <meta
          name="description"
          content="Gölköy Gurme ürün kategorileri: meyve-sebze, süt ürünleri, et, temel gıda ve daha fazlası."
        />
      </Helmet>
      <CategoryDiscoverSection
        categories={categories}
        loading={loading}
        title="Kategoriler"
      />
    </div>
  );
}
