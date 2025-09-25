import React, { useEffect, useState } from "react";
import { getAllProducts } from "../services/productService";
import { addToCart } from "../services/cartService";
import { useCartCount } from "../hooks/useCartCount";

export default function ProductGrid() {
  const [data, setData] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const { refresh: refreshCart } = useCartCount();

  useEffect(() => {
    getAllProducts()
      .then(setData)
      .catch((e) => setError(e?.response?.data || "Beklenmeyen hata"))
      .finally(() => setLoading(false));
  }, []);

  if (loading) return <div className="text-center py-5">Yükleniyor...</div>;
  if (error) return <div className="alert alert-danger">{String(error)}</div>;
  if (!data.length)
    return <div className="text-center py-5">Ürün bulunamadı.</div>;

  return (
    <div className="row">
      {data.map((p) => (
        <div key={p.id} className="col-lg-3 col-md-6 mb-4">
          <div className="card h-100">
            <div
              style={{ height: 180 }}
              className="bg-light d-flex align-items-center justify-content-center"
            >
              <span style={{ fontSize: 48 }}>📦</span>
            </div>
            <div className="card-body">
              <div className="text-muted small">
                {p.categoryName || "Kategori"}
              </div>
              <h5 className="card-title mb-2">{p.name}</h5>
              <div className="d-flex align-items-center justify-content-between">
                <strong className="text-danger">
                  ₺{Number(p.price).toFixed(2)}
                </strong>
                <button
                  className="btn btn-primary btn-sm"
                  onClick={() =>
                    addToCart(p.id).then(refreshCart).catch(console.error)
                  }
                >
                  Sepete Ekle
                </button>
              </div>
            </div>
          </div>
        </div>
      ))}
    </div>
  );
}
