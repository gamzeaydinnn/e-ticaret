// src/pages/ComparePage.jsx
import { Link } from "react-router-dom";
import { useCompare } from "../contexts/CompareContext";
import { useCart } from "../contexts/CartContext";

const ComparePage = () => {
  const { compareItems, removeFromCompare, clearCompare } = useCompare();
  const { addToCart } = useCart();

  if (compareItems.length === 0) {
    return (
      <div className="container py-5">
        <div className="text-center py-5">
          <div 
            className="d-inline-flex align-items-center justify-content-center mb-4"
            style={{ width: "100px", height: "100px", borderRadius: "50%", backgroundColor: "#fff5f0" }}
          >
            <i className="fas fa-balance-scale fa-3x" style={{ color: "#ff6b35" }}></i>
          </div>
          <h3 className="mb-2">Karşılaştırma Listeniz Boş</h3>
          <p className="text-muted mb-4">Ürünleri karşılaştırmak için ürün kartlarındaki <i className="fas fa-balance-scale"></i> ikonuna tıklayın.</p>
          <Link 
            to="/" 
            className="btn px-4 py-2 text-white"
            style={{ background: "linear-gradient(135deg, #ff6b35 0%, #ff8c5a 100%)", borderRadius: "25px" }}
          >
            <i className="fas fa-shopping-bag me-2"></i>Alışverişe Başla
          </Link>
        </div>
      </div>
    );
  }

  const features = [
    { key: "price", label: "Normal Fiyat", icon: "fa-tag", format: (v) => v ? `₺${Number(v).toFixed(2)}` : "-" },
    { key: "specialPrice", label: "İndirimli Fiyat", icon: "fa-fire", format: (v) => v ? `₺${Number(v).toFixed(2)}` : "-" },
    { key: "discountPercentage", label: "İndirim Oranı", icon: "fa-percent", format: (v) => v ? `%${v}` : "-" },
    { key: "categoryName", label: "Kategori", icon: "fa-folder", format: (v) => v || "-" },
    { key: "stockQuantity", label: "Stok Durumu", icon: "fa-box", format: (v) => v !== undefined ? (v > 0 ? `${v} adet` : "Stokta yok") : "-" },
  ];

  const handleAddToCart = (product) => addToCart(product, 1);

  return (
    <div className="container-fluid py-4" style={{ backgroundColor: "#f8f9fa", minHeight: "80vh" }}>
      {/* Header */}
      <div className="bg-white rounded-3 shadow-sm p-4 mb-4">
        <div className="d-flex justify-content-between align-items-center flex-wrap gap-3">
          <div className="d-flex align-items-center gap-3">
            <div 
              className="d-flex align-items-center justify-content-center"
              style={{ width: "50px", height: "50px", borderRadius: "12px", background: "linear-gradient(135deg, #ff6b35 0%, #ff8c5a 100%)" }}
            >
              <i className="fas fa-balance-scale fa-lg text-white"></i>
            </div>
            <div>
              <h4 className="mb-0">Ürün Karşılaştırma</h4>
              <small className="text-muted">En fazla 4 ürün karşılaştırabilirsiniz</small>
            </div>
          </div>
          <div className="d-flex gap-2">
            <span className="badge fs-6 py-2 px-3" style={{ backgroundColor: "#ff6b35" }}>
              {compareItems.length}/4 ürün
            </span>
            <button 
              className="btn btn-outline-danger rounded-pill px-3"
              onClick={clearCompare}
            >
              <i className="fas fa-trash-alt me-1"></i>Temizle
            </button>
          </div>
        </div>
      </div>

      {/* Compare Cards */}
      <div className="bg-white rounded-3 shadow-sm overflow-hidden">
        {/* Products Header */}
        <div className="row g-0 border-bottom" style={{ backgroundColor: "#fafafa" }}>
          <div className="col-3 col-lg-2 p-3 border-end d-flex align-items-center">
            <span className="fw-bold text-muted">Özellikler</span>
          </div>
          {compareItems.map((product) => (
            <div 
              key={product.id} 
              className="col p-3 text-center border-end position-relative"
              style={{ minWidth: "180px" }}
            >
              <button
                className="btn btn-sm btn-outline-danger rounded-circle position-absolute"
                style={{ top: "10px", right: "10px", width: "28px", height: "28px", padding: 0 }}
                onClick={() => removeFromCompare(product.id)}
              >
                <i className="fas fa-times" style={{ fontSize: "0.75rem" }}></i>
              </button>
              <Link to={`/product/${product.id}`} className="text-decoration-none">
                <div 
                  className="mx-auto mb-3 d-flex align-items-center justify-content-center"
                  style={{ width: "100px", height: "100px", backgroundColor: "#fff", borderRadius: "12px" }}
                >
                  <img
                    src={product.imageUrl || "/images/placeholder.png"}
                    alt={product.name}
                    style={{ maxHeight: "80px", maxWidth: "80px", objectFit: "contain" }}
                  />
                </div>
                <h6 className="text-dark mb-0" style={{ fontSize: "0.9rem" }}>{product.name}</h6>
              </Link>
            </div>
          ))}
        </div>

        {/* Features Rows */}
        {features.map((feature, idx) => (
          <div 
            key={feature.key} 
            className="row g-0 border-bottom"
            style={{ backgroundColor: idx % 2 === 0 ? "#fff" : "#fcfcfc" }}
          >
            <div className="col-3 col-lg-2 p-3 border-end d-flex align-items-center">
              <span className="text-muted">
                <i className={`fas ${feature.icon} me-2`} style={{ color: "#ff6b35", width: "16px" }}></i>
                {feature.label}
              </span>
            </div>
            {compareItems.map((product) => (
              <div key={product.id} className="col p-3 text-center border-end d-flex align-items-center justify-content-center">
                <span 
                  className={`fw-semibold ${
                    feature.key === "specialPrice" && product[feature.key] ? "text-danger" : 
                    feature.key === "stockQuantity" && product[feature.key] > 0 ? "text-success" : ""
                  }`}
                >
                  {feature.format(product[feature.key])}
                </span>
              </div>
            ))}
          </div>
        ))}

        {/* Action Row */}
        <div className="row g-0" style={{ backgroundColor: "#fff5f0" }}>
          <div className="col-3 col-lg-2 p-3 border-end d-flex align-items-center">
            <span className="fw-bold" style={{ color: "#ff6b35" }}>
              <i className="fas fa-shopping-cart me-2"></i>İşlem
            </span>
          </div>
          {compareItems.map((product) => (
            <div key={product.id} className="col p-3 text-center border-end d-flex align-items-center justify-content-center">
              <button
                className="btn text-white rounded-pill px-4"
                style={{ background: "linear-gradient(135deg, #ff6b35 0%, #ff8c5a 100%)" }}
                onClick={() => handleAddToCart(product)}
              >
                <i className="fas fa-cart-plus me-1"></i>Sepete Ekle
              </button>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};

export default ComparePage;
