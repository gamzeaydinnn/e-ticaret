/*1. Amaç
Kullanıcının daha önce verdiği siparişleri listelemek.
Siparişin tarihini, toplam tutarını, durumunu göstermek.
İstenirse sipariş detayına tıklama imkânı sağlamak (örn. fatura indirme, ürün detayları).*/

import React, { useEffect, useState } from "react";
import { OrderService } from "../services/orderService";

export default function OrderHistory() {
  const [orders, setOrders] = useState([]);

  useEffect(() => {
    // Kullanıcının siparişlerini çek
    OrderService.list(/* userId */)
      .then(setOrders)
      .catch(() => {});
  }, []);

  return (
    <div className="container mx-auto px-4 py-8">
      <h1 className="text-2xl font-bold mb-6">Sipariş Geçmişi</h1>
      {orders.length === 0 ? (
        <p>Henüz siparişiniz yok.</p>
      ) : (
        <ul className="space-y-4">
          {orders.map((order) => (
            <li key={order.id} className="p-4 border rounded shadow">
              <div>Sipariş #{order.id}</div>
              <div>Tutar: ₺{order.totalAmount}</div>
              <div>Durum: {order.status}</div>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
