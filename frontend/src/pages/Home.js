// Hero banner, kategori grid, kampanyalar, öne çıkan ürünler
import React, { useEffect, useState } from "react";
import api from "../services/api";
import ProductCard from "./components/ProductCard";
import CategoryTile from "./components/CategoryTile";

export default function Home() {
  const [categories, setCategories] = useState([]);
  const [featured, setFeatured] = useState([]);

  useEffect(() => {
    api
      .get("/categories")
      .then((r) => setCategories(r.data))
      .catch(() => {});
    api
      .get("/products?featured=true")
      .then((r) => setFeatured(r.data))
      .catch(() => {});
  }, []);

  return (
    <div className="container mx-auto px-4 py-8">
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
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          {featured.map((p) => (
            <ProductCard key={p.id} product={p} />
          ))}
        </div>
      </section>
    </div>
  );
}
