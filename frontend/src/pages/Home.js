// Hero banner, kategori grid, kampanyalar, öne çıkan ürünler
import React, { useEffect, useState } from "react";
import api from "../services/api";
import { ProductService } from "../services/productService";
import { Helmet } from "react-helmet-async";
import ProductCard from "./components/ProductCard";
import CategoryTile from "./components/CategoryTile";

export default function Home() {
  const [categories, setCategories] = useState([]);
  const [featured, setFeatured] = useState([]);
  const [productLoading, setProductLoading] = useState(true);
  const [productError, setProductError] = useState(null);

  useEffect(() => {
    api
      .get("/api/Categories")
      .then((r) => setCategories(r.data))
      .catch(() => {});

    ProductService.list()
      .then((items) => setFeatured(items))
      .catch((e) => setProductError(e?.message || "Ürünler yüklenemedi"))
      .finally(() => setProductLoading(false));
  }, []);

  return (
    <div className="container mx-auto px-4 py-8">
      <Helmet>
        {(() => {
          const siteUrl =
            process.env.REACT_APP_SITE_URL ||
            (typeof window !== "undefined" ? window.location.origin : "");
          return (
            <>
              <title>Doğadan Sofranza — Taze ve doğal market ürünleri</title>
              <meta
                name="description"
                content="Doğadan Sofranza: Taze meyve, sebze, süt ürünleri ve günlük ihtiyaçlarınızı güvenle sipariş edin."
              />
              <meta
                property="og:title"
                content="Doğadan Sofranza — Taze ve doğal market ürünleri"
              />
              <meta
                property="og:description"
                content="Taze ve doğal ürünleri kapınıza getiren yerel market"
              />
              <meta
                property="og:image"
                content={`${siteUrl}/images/og-default.jpg`}
              />
              <link
                rel="canonical"
                href={`${siteUrl}${
                  typeof window !== "undefined" ? window.location.pathname : ""
                }`}
              />
            </>
          );
        })()}
      </Helmet>
      <section className="mb-8">
        <div className="bg-gradient-to-r from-sky-400 to-indigo-600 rounded-lg p-8 text-white">
          <h1 className="text-3xl font-bold">Bugün ne sipariş ediyorsun?</h1>
          <p className="mt-2">Hızlı teslimat — Taze ürünler — Güvenli ödeme</p>
        </div>
      </section>

      <section className="mb-8">
        <h2 className="text-2xl font-bold mb-6 text-center text-gray-800">
          Kategoriler
        </h2>
        <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-6">
          {categories.map((c) => (
            <CategoryTile key={c.id} category={c} />
          ))}
        </div>
      </section>

      <section>
        <h2 className="text-xl font-semibold mb-4">Öne çıkanlar</h2>
        {productLoading && (
          <div className="text-gray-500">Ürünler yükleniyor…</div>
        )}
        {productError && !productLoading && (
          <div className="text-red-600">Hata: {productError}</div>
        )}
        {!productLoading && !productError && (
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
            {featured.map((p) => (
              <ProductCard key={p.id} product={p} />
            ))}
          </div>
        )}
      </section>
    </div>
  );
}
