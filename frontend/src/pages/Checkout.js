// Adres, kargo, ödeme adımları
import React, { useEffect, useState } from "react";
import api from "../services/api";
import { useNavigate } from "react-router-dom";

export default function Checkout() {
  const [form, setForm] = useState({
    name: "",
    phone: "",
    email: "",
    address: "",
  });
  const [paymentMethod, setPaymentMethod] = useState("card");
  const navigate = useNavigate();

  useEffect(() => {
    api
      .get("/auth/me")
      .then((r) => {
        if (r.data) {
          setForm((prev) => ({
            ...prev,
            name: r.data.fullName,
            email: r.data.email,
          }));
        }
      })
      .catch(() => {});
  }, []);

  const submit = async (e) => {
    e.preventDefault();
    const payload = { ...form, paymentMethod };
    try {
      const res = await api.post("/orders", payload);
      alert("Sipariş alındı: " + res.data.id);
      navigate("/");
    } catch (err) {
      alert("Hata: " + err.message);
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
        <textarea
          required
          placeholder="Adres"
          value={form.address}
          onChange={(e) => setForm({ ...form, address: e.target.value })}
          className="w-full mb-2 border p-2"
        />
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
    </div>
  );
}
