// Sepetteki ürünler
import React, { useEffect, useState } from "react";
import api from "../services/api";
import { Link } from "react-router-dom";
import { useAuth } from "../contexts/AuthContext";
import LoginModal from "../components/LoginModal";

export default function Cart() {
  const [items, setItems] = useState([]);
  const [showLoginModal, setShowLoginModal] = useState(false);
  const { user } = useAuth();

  const load = () =>
    api
      .get("/cart")
      .then((r) => setItems(r.data))
      .catch(() => {});

  useEffect(() => {
    load();
    window.addEventListener("cart:updated", load);
    return () => window.removeEventListener("cart:updated", load);
  }, []);

  const updateQty = (id, qty) =>
    api.put(`/cart/items/${id}`, { qty }).then(load);
  const remove = (id) => api.delete(`/cart/items/${id}`).then(load);
  const total = items.reduce((s, i) => s + i.unitPrice * i.qty, 0);

  const handleCheckout = () => {
    if (!user) {
      setShowLoginModal(true);
    }
  };

  return (
    <div className="container mx-auto px-4 py-8">
      <h1 className="text-2xl font-bold mb-4">Sepetiniz</h1>
      {items.length === 0 ? (
        <div>
          Sepetiniz boş. <Link to="/">Alışverişe başla</Link>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          <div className="md:col-span-2">
            {items.map((it) => (
              <div
                key={it.id}
                className="flex items-center gap-4 p-4 border rounded mb-2"
              >
                <img
                  src={it.product.image}
                  className="w-20 h-20 object-cover"
                  alt={it.product.name}
                />
                <div className="flex-1">
                  <div className="font-semibold">{it.product.name}</div>
                  <div>₺{it.unitPrice}</div>
                </div>
                <input
                  type="number"
                  value={it.qty}
                  min={1}
                  onChange={(e) => updateQty(it.id, Number(e.target.value))}
                  className="w-20 border p-1"
                />
                <button onClick={() => remove(it.id)} className="text-red-500">
                  Sil
                </button>
              </div>
            ))}
          </div>
          <aside className="p-4 bg-white rounded shadow">
            <div className="mb-2">
              Ara Toplam: <strong>₺{total.toFixed(2)}</strong>
            </div>
            {user ? (
              <Link
                to="/checkout"
                className="block bg-blue-600 text-white p-3 text-center rounded"
              >
                Ödemeye geç
              </Link>
            ) : (
              <button
                onClick={handleCheckout}
                className="block w-full bg-blue-600 text-white p-3 text-center rounded"
              >
                Ödemeye geç
              </button>
            )}
          </aside>
        </div>
      )}
      <LoginModal
        show={showLoginModal}
        onClose={() => setShowLoginModal(false)}
      />
    </div>
  );
}
