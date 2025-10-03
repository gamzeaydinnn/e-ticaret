// src/pages/components/CartPage.js
import React, { useEffect, useState } from "react";
import { CartService } from "../services/cartService";
import { Link } from "react-router-dom";

export default function CartPage({ userId }) {
  const [cart, setCart] = useState({ items: [], total: 0 });

  const loadCart = async () => {
    try {
      const data = await CartService.getCart(userId);
      setCart(data);
    } catch (err) {
      console.error("Sepet yüklenemedi", err);
    }
  };

  useEffect(() => {
    loadCart();
    window.addEventListener("cart:updated", loadCart);
    return () => window.removeEventListener("cart:updated", loadCart);
  }, [userId]);

  const updateQty = async (itemId, qty) => {
    await CartService.updateItem(userId, itemId, qty);
    loadCart();
  };

  const removeItem = async (itemId) => {
    await CartService.removeItem(userId, itemId);
    loadCart();
  };

  const total = cart.items.reduce((sum, i) => sum + i.product.price * i.quantity, 0);

  return (
    <div className="container mx-auto px-4 py-8">
      <h1 className="text-2xl font-bold mb-4">Sepetiniz</h1>
      {cart.items.length === 0 ? (
        <div>
          Sepetiniz boş. <Link to="/">Alışverişe başla</Link>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          <div className="md:col-span-2">
            {cart.items.map((it) => (
              <div key={it.id} className="flex items-center gap-4 p-4 border rounded mb-2">
                <img src={it.product.imageUrl} className="w-20 h-20 object-cover" alt={it.product.name} />
                <div className="flex-1">
                  <div className="font-semibold">{it.product.name}</div>
                  <div>₺{it.product.price}</div>
                </div>
                <input
                  type="number"
                  value={it.quantity}
                  min={1}
                  onChange={(e) => updateQty(it.id, Number(e.target.value))}
                  className="w-20 border p-1"
                />
                <button onClick={() => removeItem(it.id)} className="text-red-500">Sil</button>
              </div>
            ))}
          </div>
          <aside className="p-4 bg-white rounded shadow">
            <div className="mb-2">
              Ara Toplam: <strong>₺{total.toFixed(2)}</strong>
            </div>
            <Link to="/checkout" className="block bg-blue-600 text-white p-3 text-center rounded">
              Ödemeye geç
            </Link>
          </aside>
        </div>
      )}
    </div>
  );
}
