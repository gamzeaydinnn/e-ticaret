import React, { useEffect, useState, useRef } from "react";
import axios from "../../services/api";
import BannerManagement from "./BannerManagement";

const CouponManagement = () => {
  const [coupons, setCoupons] = useState([]);
  const [form, setForm] = useState({
    id: 0,
    code: "",
    isPercentage: false,
    value: "",
    expirationDate: "",
    minOrderAmount: "",
    usageLimit: 1,
    isActive: true,
  });
  const [editing, setEditing] = useState(false);
  const [feedback, setFeedback] = useState("");
  const tableRef = useRef(null);
  const [search, setSearch] = useState("");

  const fetchCoupons = async () => {
    try {
      const res = await axios.get("/api/admin/coupons");
      setCoupons(res.data);
    } catch (err) {
      setFeedback(
        "Kuponlar yüklenemedi: " +
          (err?.response?.data?.message || "Sunucu hatası.")
      );
      setTimeout(() => setFeedback(""), 2500);
    }
  };

  useEffect(() => {
    fetchCoupons();
  }, []);

  const handleChange = (e) => {
    const { name, value, type, checked } = e.target;
    setForm((prev) => ({
      ...prev,
      [name]: type === "checkbox" ? checked : value,
    }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    try {
      if (editing) {
        await axios.put(`/api/admin/coupons/${form.id}`, form);
        setFeedback("Kupon başarıyla güncellendi.");
      } else {
        await axios.post("/api/admin/coupons", form);
        setFeedback("Kupon başarıyla eklendi.");
      }
      setForm({
        id: 0,
        code: "",
        isPercentage: false,
        value: "",
        expirationDate: "",
        minOrderAmount: "",
        usageLimit: 1,
        isActive: true,
      });
      setEditing(false);
      await fetchCoupons();
      if (tableRef.current) {
        tableRef.current.scrollIntoView({ behavior: "smooth" });
      }
    } catch (err) {
      setFeedback(
        "Bir hata oluştu. " +
          (err?.response?.data?.message || "Lütfen tekrar deneyin.")
      );
    }
    setTimeout(() => setFeedback(""), 2500);
  };

  const handleEdit = (coupon) => {
    setForm({ ...coupon });
    setEditing(true);
  };

  const handleDelete = async (id) => {
    if (!window.confirm("Kuponu silmek istediğinize emin misiniz?")) return;
    try {
      await axios.delete(`/api/admin/coupons/${id}`);
      fetchCoupons();
    } catch (err) {
      setFeedback(
        "Silme hatası: " +
          (err?.response?.data?.message || "Lütfen tekrar deneyin.")
      );
      setTimeout(() => setFeedback(""), 2500);
    }
  };

  // Arama ve filtreleme
  const filteredCoupons = Array.isArray(coupons)
    ? coupons.filter(
        (c) =>
          c.code.toLowerCase().includes(search.toLowerCase()) ||
          (search === "aktif" && c.isActive) ||
          (search === "pasif" && !c.isActive)
      )
    : [];

  // İstatistik
  const totalCount = Array.isArray(coupons) ? coupons.length : 0;
  const activeCount = Array.isArray(coupons)
    ? coupons.filter((c) => c.isActive).length
    : 0;

  return (
    <div style={{ maxWidth: 1200, margin: "0 auto", padding: 24 }}>
      <div style={{ display: "flex", gap: 32, flexWrap: "wrap" }}>
        {/* Kupon Paneli */}
        <div style={{ flex: 1, minWidth: 350 }}>
          <h2 style={{ marginBottom: 8 }}>Kupon Yönetimi</h2>
          <div
            style={{
              display: "flex",
              alignItems: "center",
              gap: 24,
              marginBottom: 16,
            }}
          >
            <input
              type="text"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Kupon kodu, aktif/pasif..."
              style={{
                padding: 8,
                borderRadius: 8,
                border: "1px solid #ccc",
                minWidth: 200,
              }}
            />
            <span style={{ fontWeight: "bold" }}>Toplam: {totalCount}</span>
            <span style={{ color: "#388e3c", fontWeight: "bold" }}>
              Aktif: {activeCount}
            </span>
          </div>
          {feedback && (
            <div
              style={{
                marginBottom: 12,
                color: feedback.includes("hata") ? "#d32f2f" : "#388e3c",
                fontWeight: "bold",
              }}
            >
              {feedback}
            </div>
          )}
          <form
            onSubmit={handleSubmit}
            style={{
              marginBottom: 24,
              display: "flex",
              flexWrap: "wrap",
              gap: 12,
              alignItems: "center",
            }}
          >
            <input
              name="code"
              value={form.code}
              onChange={handleChange}
              placeholder="Kupon Kodu"
              required
              style={{ flex: 1, minWidth: 120 }}
            />
            <input
              name="value"
              type="number"
              value={form.value}
              onChange={handleChange}
              placeholder="İndirim"
              required
              style={{ flex: 1, minWidth: 80 }}
            />
            <label style={{ display: "flex", alignItems: "center", gap: 4 }}>
              Yüzde mi?
              <input
                name="isPercentage"
                type="checkbox"
                checked={form.isPercentage}
                onChange={handleChange}
              />
            </label>
            <input
              name="expirationDate"
              type="date"
              value={form.expirationDate?.slice(0, 10)}
              onChange={handleChange}
              required
              style={{ flex: 1, minWidth: 120 }}
            />
            <input
              name="minOrderAmount"
              type="number"
              value={form.minOrderAmount}
              onChange={handleChange}
              placeholder="Minimum Sipariş Tutarı"
              style={{ flex: 1, minWidth: 120 }}
            />
            <input
              name="usageLimit"
              type="number"
              value={form.usageLimit}
              onChange={handleChange}
              placeholder="Kullanım Limiti"
              style={{ flex: 1, minWidth: 80 }}
            />
            <label style={{ display: "flex", alignItems: "center", gap: 4 }}>
              Aktif mi?
              <input
                name="isActive"
                type="checkbox"
                checked={form.isActive}
                onChange={handleChange}
              />
            </label>
            <button
              type="submit"
              style={{
                background: "#388e3c",
                color: "white",
                border: "none",
                borderRadius: 8,
                padding: "8px 16px",
                fontWeight: "bold",
              }}
            >
              {editing ? "Güncelle" : "Ekle"}
            </button>
            {editing && (
              <button
                type="button"
                onClick={() => {
                  setEditing(false);
                  setForm({
                    id: 0,
                    code: "",
                    isPercentage: false,
                    value: "",
                    expirationDate: "",
                    minOrderAmount: "",
                    usageLimit: 1,
                    isActive: true,
                  });
                }}
                style={{
                  background: "#d32f2f",
                  color: "white",
                  border: "none",
                  borderRadius: 8,
                  padding: "8px 16px",
                  fontWeight: "bold",
                }}
              >
                İptal
              </button>
            )}
          </form>
          <table
            ref={tableRef}
            style={{
              width: "100%",
              borderCollapse: "collapse",
              background: "white",
              borderRadius: 12,
              overflow: "hidden",
              boxShadow: "0 2px 8px #eee",
            }}
          >
            <thead style={{ background: "#f5f5f5" }}>
              <tr>
                <th style={{ padding: 8 }}>ID</th>
                <th style={{ padding: 8 }}>Kod</th>
                <th style={{ padding: 8 }}>İndirim</th>
                <th style={{ padding: 8 }}>Yüzde mi?</th>
                <th style={{ padding: 8 }}>Bitiş</th>
                <th style={{ padding: 8 }}>Min Tutar</th>
                <th style={{ padding: 8 }}>Kullanım</th>
                <th style={{ padding: 8 }}>Aktif</th>
                <th style={{ padding: 8 }}>İşlem</th>
              </tr>
            </thead>
            <tbody>
              {filteredCoupons.map((c) => (
                <tr key={c.id} style={{ borderBottom: "1px solid #eee" }}>
                  <td style={{ padding: 8 }}>{c.id}</td>
                  <td style={{ padding: 8 }}>{c.code}</td>
                  <td style={{ padding: 8 }}>{c.value}</td>
                  <td style={{ padding: 8 }}>
                    {c.isPercentage ? "Evet" : "Hayır"}
                  </td>
                  <td style={{ padding: 8 }}>
                    {c.expirationDate?.slice(0, 10)}
                  </td>
                  <td style={{ padding: 8 }}>{c.minOrderAmount}</td>
                  <td style={{ padding: 8 }}>{c.usageLimit}</td>
                  <td style={{ padding: 8 }}>
                    {c.isActive ? "Evet" : "Hayır"}
                  </td>
                  <td style={{ padding: 8 }}>
                    <button
                      onClick={() => handleEdit(c)}
                      style={{
                        marginRight: 8,
                        background: "#1976d2",
                        color: "white",
                        border: "none",
                        borderRadius: 6,
                        padding: "4px 10px",
                        fontWeight: "bold",
                      }}
                    >
                      Düzenle
                    </button>
                    <button
                      onClick={() => handleDelete(c.id)}
                      style={{
                        background: "#d32f2f",
                        color: "white",
                        border: "none",
                        borderRadius: 6,
                        padding: "4px 10px",
                        fontWeight: "bold",
                      }}
                    >
                      Sil
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {filteredCoupons.length === 0 && (
            <div style={{ marginTop: 24, color: "#888", textAlign: "center" }}>
              Hiç kupon bulunamadı.
            </div>
          )}
        </div>
        {/* Banner Paneli */}
        <div
          style={{
            flex: 1,
            minWidth: 350,
            background: "#fff",
            borderRadius: 12,
            boxShadow: "0 2px 8px #eee",
            padding: 24,
          }}
        >
          <h2 style={{ marginBottom: 8 }}>Poster / Banner Yönetimi</h2>
          <BannerManagement />
        </div>
      </div>
    </div>
  );
};

export default CouponManagement;
