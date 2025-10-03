//Kategoriye göre ürün listeleme, filtre sidebar
import React, { useEffect, useState } from "react";
import api from "../api/client";
import ProductGrid from "../components/ProductGrid";
import { useParams } from "react-router-dom";

export default function Category() {
  const { slug } = useParams();
  const [products, setProducts] = useState([]);
  const [category, setCategory] = useState(null);

  useEffect(() => {
  api
    .get(`/api/Categories/slug/${slug}`)
    .then((r) => setCategory(r.data))
    .catch(() => {});
  api
    .get(`/api/Products?categorySlug=${slug}`)
    .then((r) => setProducts(r.data))
    .catch(() => {});
}, [slug]);


  return (
    <div className="container mx-auto px-4 py-8">
      <h1 className="text-2xl font-bold mb-6">
        {category?.name || "Kategori"}
      </h1>
      <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
        <aside className="md:col-span-1">
          <div className="bg-white p-4 rounded shadow">Filtreler (örnek)</div>
        </aside>
        <main className="md:col-span-3">
          <ProductGrid products={products} />
        </main>
      </div>
    </div>
  );
}
