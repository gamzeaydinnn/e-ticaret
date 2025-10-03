/*
4️⃣ Backend ile İlişki
OrderService veya AdminService üzerinden kullanıcının siparişleri çekilir.
Status güncellemeleri admin tarafından yapılır, kullanıcı sadece takip eder.

Özetle: OrderTracking.js müşteriye sipariş durumunu takip etme ve 
görsel olarak bilgilendirme sayfasıdır.
*/
// /src/pages/components/OrderTracking.js
import React, { useEffect, useState } from "react";
import { OrderService } from "../../services/orderService";

export default function OrderTracking({ orderId }) {
  const [order, setOrder] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!orderId) return;
    setLoading(true);
    OrderService.getById(orderId)
      .then((data) => setOrder(data))
      .finally(() => setLoading(false));
  }, [orderId]);

  if (loading) return <div>Yükleniyor...</div>;
  if (!order) return <div>Sipariş bulunamadı.</div>;

  const steps = [
    { status: "Pending", label: "Sipariş Alındı" },
    { status: "Preparing", label: "Hazırlanıyor" },
    { status: "OnTheWay", label: "Yolda" },
    { status: "Delivered", label: "Teslim Edildi" },
  ];

  return (
    <div className="p-4 bg-white rounded shadow">
      <h2 className="text-xl font-bold mb-4">Sipariş Takibi #{order.id}</h2>
      <div className="flex flex-col gap-2">
        {steps.map((step, index) => {
          const completed =
            steps.findIndex((s) => s.status === order.status) >= index;
          return (
            <div
              key={step.status}
              className={`flex items-center gap-2 ${
                completed ? "text-green-600 font-semibold" : "text-gray-400"
              }`}
            >
              <div
                className={`w-6 h-6 rounded-full border flex items-center justify-center ${
                  completed ? "bg-green-600 text-white" : "border-gray-400"
                }`}
              >
                {completed ? "✓" : index + 1}
              </div>
              <span>{step.label}</span>
            </div>
          );
        })}
      </div>
      <div className="mt-4 text-sm text-gray-600">
        Durum: <strong>{order.status}</strong>
      </div>
    </div>
  );
}
