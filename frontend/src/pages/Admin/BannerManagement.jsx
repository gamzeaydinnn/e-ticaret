import React, { useEffect, useState, useRef } from "react";
import axios from "../../services/api";

const BannerManagement = () => {
  const [banners, setBanners] = useState([]);
  const [form, setForm] = useState({
    id: 0,
    title: "",
    imageUrl: "",
    linkUrl: "",
    isActive: true,
    displayOrder: 0,
  });
  const [editing, setEditing] = useState(false);
  const [feedback, setFeedback] = useState("");
  const tableRef = useRef(null);

  const fetchBanners = async () => {
    const res = await axios.get("/api/banners");
    setBanners(res.data);
  };

  useEffect(() => {
    fetchBanners();
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
        await axios.put("/api/banners", form);
        setFeedback("Banner başarıyla güncellendi.");
      } else {
        await axios.post("/api/banners", form);
        setFeedback("Banner başarıyla eklendi.");
      }
      setForm({
        id: 0,
        title: "",
        imageUrl: "",
        linkUrl: "",
        isActive: true,
        displayOrder: 0,
      });
      setEditing(false);
      await fetchBanners();
      // Tabloya scroll
      if (tableRef.current) {
        tableRef.current.scrollIntoView({ behavior: "smooth" });
      }
    } catch (err) {
      setFeedback("Bir hata oluştu. Lütfen tekrar deneyin.");
    }
    // Feedback mesajı 2.5 saniye sonra kaybolsun
    setTimeout(() => setFeedback(""), 2500);
  };

  const handleEdit = (banner) => {
    setForm(banner);
    setEditing(true);
  };

  const handleDelete = async (id) => {
    await axios.delete(`/api/banners/${id}`);
    fetchBanners();
  };

  return (
    <div>
      <h2>Banner Yönetimi</h2>
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
      <form onSubmit={handleSubmit} style={{ marginBottom: 24 }}>
        <input
          name="title"
          value={form.title}
          onChange={handleChange}
          placeholder="Başlık"
          required
        />
        <input
          name="imageUrl"
          value={form.imageUrl}
          onChange={handleChange}
          placeholder="Görsel URL"
          required
        />
        <input
          name="linkUrl"
          value={form.linkUrl}
          onChange={handleChange}
          placeholder="Tıklanınca gidilecek URL"
        />
        <input
          name="displayOrder"
          type="number"
          value={form.displayOrder}
          onChange={handleChange}
          placeholder="Sıra"
        />
        <label>
          Aktif mi?
          <input
            name="isActive"
            type="checkbox"
            checked={form.isActive}
            onChange={handleChange}
          />
        </label>
        <button type="submit">{editing ? "Güncelle" : "Ekle"}</button>
        {editing && (
          <button
            type="button"
            onClick={() => {
              setEditing(false);
              setForm({
                id: 0,
                title: "",
                imageUrl: "",
                linkUrl: "",
                isActive: true,
                displayOrder: 0,
              });
            }}
          >
            İptal
          </button>
        )}
      </form>
      <table ref={tableRef}>
        <thead>
          <tr>
            <th>ID</th>
            <th>Başlık</th>
            <th>Görsel</th>
            <th>Link</th>
            <th>Aktif</th>
            <th>Sıra</th>
            <th>İşlem</th>
          </tr>
        </thead>
        <tbody>
          {(Array.isArray(banners) ? banners : []).map((b) => (
            <tr key={b.id}>
              <td>{b.id}</td>
              <td>{b.title}</td>
              <td>
                <img src={b.imageUrl} alt={b.title} style={{ width: 80 }} />
              </td>
              <td>
                <a href={b.linkUrl} target="_blank" rel="noopener noreferrer">
                  {b.linkUrl}
                </a>
              </td>
              <td>{b.isActive ? "Evet" : "Hayır"}</td>
              <td>{b.displayOrder}</td>
              <td>
                <button onClick={() => handleEdit(b)}>Düzenle</button>
                <button onClick={() => handleDelete(b.id)}>Sil</button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
};

export default BannerManagement;
