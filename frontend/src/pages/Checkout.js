// Adres, kargo, ödeme adımları
import React, { useEffect, useState } from "react";
import api from "../services/api";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../contexts/AuthContext";
import LoginModal from "../components/LoginModal";

export default function Checkout() {
  const [form, setForm] = useState({
    name: "",
    phone: "",
    email: "",
    address: "",
  });
  const [paymentMethod, setPaymentMethod] = useState("card");
  const [shippingMethod, setShippingMethod] = useState("car"); // car veya motorcycle
  const [shippingCost, setShippingCost] = useState(30); // Varsayılan araç ücreti
  const [showLoginModal, setShowLoginModal] = useState(false);
  const navigate = useNavigate();
  const { user } = useAuth();

  useEffect(() => {
    // Kargo ücretini hesapla
    setShippingCost(shippingMethod === "motorcycle" ? 20 : 30);
  }, [shippingMethod]);

  useEffect(() => {
    if (user) {
      setForm((prev) => ({
        ...prev,
        name: user.fullName || `${user.firstName} ${user.lastName}`,
        email: user.email,
      }));
    }
  }, [user]);

  const submit = async (e) => {
    e.preventDefault();

    if (!user) {
      setShowLoginModal(true);
      return;
    }

    try {
      const payload = {
        customerName: form.name,
        customerPhone: form.phone,
        customerEmail: form.email,
        shippingAddress: form.address,
        shippingCity: form.city || "",
        paymentMethod,
        shippingMethod,
        shippingCost,
      };

      const res = await api.post("/orders", payload);
      if (res.success) {
        alert("Sipariş alındı!");
        navigate("/orders");
      }
    } catch (err) {
      alert("Hata: " + (err.message || "Sipariş başarısız"));
    }
  };

  return (
    <div className="container mx-auto px-4 py-8">
      <h1 className="text-2xl font-bold mb-4">Ödeme ve Adres</h1>
      <form onSubmit={submit} className="max-w-xl">
        <input
          required
          placeholder="Ad Soyad"
          value={form.name}
          onChange={(e) => setForm({ ...form, name: e.target.value })}
          className="w-full mb-2 border p-2"
        />
        <input
          required
          placeholder="Telefon"
          value={form.phone}
          onChange={(e) => setForm({ ...form, phone: e.target.value })}
          className="w-full mb-2 border p-2"
        />
        <input
          required
          placeholder="E-posta"
          value={form.email}
          onChange={(e) => setForm({ ...form, email: e.target.value })}
          className="w-full mb-2 border p-2"
        />
        <input
          required
          placeholder="İl"
          value={form.city || ""}
          onChange={(e) => setForm({ ...form, city: e.target.value })}
          className="w-full mb-2 border p-2"
        />
        <textarea
          required
          placeholder="Adres"
          value={form.address}
          onChange={(e) => setForm({ ...form, address: e.target.value })}
          className="w-full mb-2 border p-2"
        />

        {/* Kargo Seçeneği */}
        <div className="mb-4">
          <label
            className="block mb-2 font-semibold"
            style={{ color: "#FF8C00" }}
          >
            🚚 Kargo Tipi Seçin
          </label>
          <div className="d-flex gap-3">
            <div
              onClick={() => setShippingMethod("car")}
              className="flex-fill p-3 border rounded cursor-pointer"
              style={{
                borderColor: shippingMethod === "car" ? "#FF8C00" : "#ddd",
                borderWidth: shippingMethod === "car" ? "3px" : "1px",
                backgroundColor: shippingMethod === "car" ? "#FFF5E6" : "white",
                borderRadius: "15px",
                cursor: "pointer",
                transition: "all 0.3s",
              }}
            >
              <div className="text-center">
                <div style={{ fontSize: "2rem" }}>🚗</div>
                <div className="fw-bold mt-2">Araç</div>
                <div className="text-muted small">30 ₺</div>
              </div>
            </div>
            <div
              onClick={() => setShippingMethod("motorcycle")}
              className="flex-fill p-3 border rounded cursor-pointer"
              style={{
                borderColor:
                  shippingMethod === "motorcycle" ? "#FF8C00" : "#ddd",
                borderWidth: shippingMethod === "motorcycle" ? "3px" : "1px",
                backgroundColor:
                  shippingMethod === "motorcycle" ? "#FFF5E6" : "white",
                borderRadius: "15px",
                cursor: "pointer",
                transition: "all 0.3s",
              }}
            >
              <div className="text-center">
                <div style={{ fontSize: "2rem" }}>🏍️</div>
                <div className="fw-bold mt-2">Motosiklet</div>
                <div className="text-muted small">20 ₺</div>
              </div>
            </div>
          </div>
          <div className="mt-2 text-end">
            <strong style={{ color: "#FF8C00" }}>
              Kargo Ücreti: {shippingCost} ₺
            </strong>
          </div>
        </div>

        <div className="mb-4">
          <label className="block mb-1">Ödeme Yöntemi</label>
          <select
            value={paymentMethod}
            onChange={(e) => setPaymentMethod(e.target.value)}
            className="border p-2 w-full"
          >
            <option value="card">Kart ile öde</option>
            <option value="cash">Kapıda ödeme</option>
          </select>
        </div>
        <button className="bg-green-600 text-white p-3 rounded">
          Siparişi Onayla
        </button>
      </form>
      <LoginModal
        show={showLoginModal}
        onClose={() => setShowLoginModal(false)}
      />
    </div>
  );
}
