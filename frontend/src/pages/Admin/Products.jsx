import Sidebar from "../components/Sidebar";
import Navbar from "../components/Navbar";
import React, { useState } from "react";
import getProductCategoryRules from "../../config/productCategoryRules";
import { useEffect } from "react";

export default function Products() {
  const [showAdd, setShowAdd] = useState(false);
  const [selectedCategory, setSelectedCategory] = useState("");
  const [suggestion, setSuggestion] = useState(null);
  const [rules, setRules] = useState([]);
  const [name, setName] = useState("");
  const [quantity, setQuantity] = useState("");
  const [unit, setUnit] = useState("");
  const [minQ, setMinQ] = useState("");
  const [maxQ, setMaxQ] = useState("");
  const [stepQ, setStepQ] = useState("");
  const [formError, setFormError] = useState("");
  const [products, setProducts] = useState([
    { id: 1, name: "Laptop", price: 15000, stock: 25 },
    {
      id: 2,
      name: "Domates",
      price: 45.9,
      stock: 120,
      category: "Kg ile satılan taze ürünler",
    },
  ]);

  useEffect(() => {
    let mounted = true;
    (async () => {
      try {
        const r = await getProductCategoryRules();
        if (mounted) setRules(r || []);
      } catch (e) {
        if (mounted) setRules([]);
      }
    })();
    return () => (mounted = false);
  }, []);

  const onCategoryChange = (cat) => {
    setSelectedCategory(cat);
    const found = rules.find(
      (r) => r.category === cat || r.examples?.includes(cat)
    );
    setSuggestion(found || null);
    if (found) {
      setUnit(found.unit || "");
      setMinQ(found.min_quantity || "");
      setMaxQ(found.max_quantity || "");
      setStepQ(found.step || "");
    } else {
      setUnit("");
      setMinQ("");
      setMaxQ("");
      setStepQ("");
    }
  };

  const parseLeadingNumber = (s) => {
    if (!s) return null;
    const m = String(s).match(/[0-9]+(?:\.[0-9]+)?/);
    return m ? parseFloat(m[0]) : null;
  };

  const handleCreate = (e) => {
    e.preventDefault();
    setFormError("");

    const minN = parseLeadingNumber(minQ);
    const maxN = parseLeadingNumber(maxQ);
    const stepN = parseLeadingNumber(stepQ);
    const qtyN = parseFloat(quantity);

    if (!name) return setFormError("Ürün adı girin.");
    if (!selectedCategory) return setFormError("Kategori/örnek girin.");
    if (quantity === "") return setFormError("Miktar girin.");

    if (!isNaN(qtyN) && minN !== null && qtyN < minN) {
      return setFormError(`Minimum ${minQ} ${unit || ""} olmalıdır.`);
    }
    if (!isNaN(qtyN) && maxN !== null && qtyN > maxN) {
      return setFormError(`Maksimum ${maxQ} ${unit || ""} aşılmamalı.`);
    }
    if (!isNaN(qtyN) && stepN !== null && minN !== null) {
      const diff = Math.abs((qtyN - minN) / stepN);
      const near = Math.round(diff);
      if (Math.abs(diff - near) > 1e-6) {
        return setFormError(`Miktar ${stepQ} adımlarında olmalıdır.`);
      }
    }

    const newProd = {
      id: Date.now(),
      name,
      price: 0,
      stock: isNaN(parseFloat(quantity)) ? 0 : parseFloat(quantity),
      category: selectedCategory,
      ruleApplied: suggestion?.category || null,
    };
    setProducts((p) => [newProd, ...p]);
    setShowAdd(false);
    setName("");
    setSelectedCategory("");
    setQuantity("");
    setSuggestion(null);
    setUnit("");
    setMinQ("");
    setMaxQ("");
    setStepQ("");
    setFormError("");
  };
  return (
    <div className="flex min-h-screen bg-gray-100">
      <Sidebar />
      <div className="flex-1">
        <Navbar title="Ürün Yönetimi" />
        <div className="p-6">
          <button
            onClick={() => setShowAdd(true)}
            className="mb-4 bg-green-500 text-white px-4 py-2 rounded-lg hover:bg-green-600"
          >
            Yeni Ürün Ekle
          </button>

          {/* Minimal Add Product Modal (client-side only, shows suggestions from rules) */}
          {showAdd && (
            <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
              <div className="bg-white rounded-lg shadow p-6 w-96">
                <h5 className="mb-3 font-bold">Yeni Ürün Ekle</h5>

                <form onSubmit={handleCreate}>
                  <div className="mb-2">
                    <label className="block text-sm mb-1">Ürün Adı</label>
                    <input
                      className="w-full border rounded px-2 py-1"
                      placeholder="Domates"
                      value={name}
                      onChange={(e) => setName(e.target.value)}
                    />
                  </div>

                  <div className="mb-2">
                    <label className="block text-sm mb-1">
                      Kategori / Örnek
                    </label>
                    <input
                      className="w-full border rounded px-2 py-1"
                      placeholder="Örnek: Domates veya Kg ile satılan taze ürünler"
                      value={selectedCategory}
                      onChange={(e) => onCategoryChange(e.target.value)}
                    />
                  </div>

                  <div className="mb-2 grid grid-cols-2 gap-2">
                    <div>
                      <label className="block text-sm mb-1">Miktar</label>
                      <input
                        className="w-full border rounded px-2 py-1"
                        placeholder={stepQ || "Örn: 0.25"}
                        value={quantity}
                        onChange={(e) => setQuantity(e.target.value)}
                      />
                    </div>
                    <div>
                      <label className="block text-sm mb-1">Birim</label>
                      <input
                        className="w-full border rounded px-2 py-1"
                        value={unit}
                        onChange={(e) => setUnit(e.target.value)}
                      />
                    </div>
                  </div>

                  {suggestion ? (
                    <div className="p-3 bg-gray-50 rounded mt-2 text-sm">
                      <div>
                        <strong>Öneri:</strong> {suggestion.category}
                      </div>
                      <div>Unit: {suggestion.unit}</div>
                      <div>
                        Min: {suggestion.min_quantity} — Max:{" "}
                        {suggestion.max_quantity}
                      </div>
                      <div>Step: {suggestion.step}</div>
                      <div className="text-muted mt-1">
                        Not: {suggestion.note}
                      </div>
                    </div>
                  ) : (
                    <div className="text-sm text-muted mt-2">
                      Kategoriye göre öneri gösterilecektir.
                    </div>
                  )}

                  {formError && (
                    <div className="text-danger text-sm mt-2">{formError}</div>
                  )}

                  <div className="mt-4 flex justify-end gap-2">
                    <button
                      className="px-3 py-1 rounded border"
                      type="button"
                      onClick={() => setShowAdd(false)}
                    >
                      Kapat
                    </button>
                    <button
                      className="px-3 py-1 rounded bg-green-500 text-white"
                      type="submit"
                    >
                      Oluştur
                    </button>
                  </div>
                </form>
              </div>
            </div>
          )}

          <table className="w-full bg-white shadow rounded-2xl">
            <thead>
              <tr className="text-left border-b">
                <th className="p-3">ID</th>
                <th className="p-3">Ürün Adı</th>
                <th className="p-3">Fiyat</th>
                <th className="p-3">Stok</th>
                <th className="p-3">İşlemler</th>
              </tr>
            </thead>
            <tbody>
              {products.map((p) => (
                <tr key={p.id} className="border-b">
                  <td className="p-3">{p.id}</td>
                  <td className="p-3">
                    {p.name}
                    {p.ruleApplied && (
                      <div className="text-xs text-muted">
                        Kural: {p.ruleApplied}
                      </div>
                    )}
                  </td>
                  <td className="p-3">{p.price ? `₺${p.price}` : "-"}</td>
                  <td className="p-3">{p.stock}</td>
                  <td className="p-3">
                    <button className="mr-2 bg-yellow-500 text-white px-3 py-1 rounded hover:bg-yellow-600">
                      Düzenle
                    </button>
                    <button className="bg-red-500 text-white px-3 py-1 rounded hover:bg-red-600">
                      Sil
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
