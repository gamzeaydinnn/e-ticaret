import { Link } from "react-router-dom";
import { useFavorites } from "../contexts/FavoriteContext";

/**
 * FavoritesPage.jsx
 * - Backend API entegrasyonlu favori yönetimi
 * - Hem misafir hem kayıtlı kullanıcı için çalışır
 * - Ürün bilgileri backend'den gelir (ProductListDto)
 */

const FavoritesPage = () => {
  // FavoriteContext'ten direkt ürün objelerini al
  const {
    favorites = [], // ProductListDto array - tam ürün bilgileri
    favoriteIds = [], // Sadece ID'ler
    loading,
    removeFromFavorites,
  } = useFavorites();

  const handleRemoveFavorite = async (productId) => {
    try {
      const result = await removeFromFavorites(productId);
      if (result && result.success === false) {
        alert("Favori silinirken bir hata oluştu.");
      }
    } catch (error) {
      console.error("Favori silinirken hata:", error);
      alert("Favori silinirken bir hata oluştu.");
    }
  };

  return (
    <div
      style={{
        minHeight: "100vh",
        padding: "2rem 0",
        background:
          "linear-gradient(135deg,#fff3e0 0%,#ffe0b2 50%,#ffcc80 100%)",
      }}
    >
      <div className="container">
        <div className="row">
          <div className="col-md-10 mx-auto">
            <div
              className="card shadow-lg border-0"
              style={{ borderRadius: 20 }}
            >
              <div
                className="card-header d-flex justify-content-between align-items-center border-0 text-white"
                style={{
                  background: "linear-gradient(45deg,#e91e63,#ad1457,#880e4f)",
                  borderTopLeftRadius: 20,
                  borderTopRightRadius: 20,
                  padding: "1.5rem 2rem",
                }}
              >
                <h3 className="mb-0 fw-bold">
                  <i className="fas fa-heart me-3"></i>Favorilerim
                </h3>
                <span
                  className="badge fs-6 fw-bold px-3 py-2"
                  style={{
                    backgroundColor: "rgba(255,255,255,0.2)",
                    borderRadius: 50,
                  }}
                >
                  {favorites.length} Ürün
                </span>
              </div>

              <div className="card-body" style={{ padding: "2rem" }}>
                {loading ? (
                  <div className="text-center py-5">
                    <div
                      className="spinner-border mb-3"
                      role="status"
                      style={{ width: "3rem", height: "3rem" }}
                    >
                      <span className="visually-hidden">Loading...</span>
                    </div>
                    <p className="text-muted fw-bold">
                      Favoriler yükleniyor...
                    </p>
                  </div>
                ) : favorites.length > 0 ? (
                  <>
                    <div
                      className="row pb-3 mb-4 fw-bold"
                      style={{
                        borderBottom: "2px solid #fce4ec",
                        color: "#e91e63",
                      }}
                    >
                      <div className="col-md-8">
                        <h6>
                          <i className="fas fa-heart me-2"></i>Ürün
                        </h6>
                      </div>
                      <div className="col-md-2 text-center">
                        <h6>
                          <i className="fas fa-tag me-2"></i>Fiyat
                        </h6>
                      </div>
                      <div className="col-md-2 text-center">
                        <h6>
                          <i className="fas fa-cog me-2"></i>İşlemler
                        </h6>
                      </div>
                    </div>

                    {/* favorites artık ProductListDto array - direkt ürün objesi */}
                    {favorites.map((product) => {
                      const productId = product.id || product.productId;
                      return (
                        <div
                          key={productId}
                          className="row align-items-center py-3 border-bottom"
                        >
                          <div className="col-md-8">
                            <div className="d-flex align-items-center">
                              <img
                                src={
                                  product?.imageUrl || "/images/placeholder.png"
                                }
                                alt={product?.name || "Ürün"}
                                style={{
                                  width: 80,
                                  height: 80,
                                  objectFit: "contain",
                                  borderRadius: 15,
                                  padding: 8,
                                  border: "2px solid #fce4ec",
                                }}
                                className="me-3"
                                onError={(e) => {
                                  e.target.src = "/images/placeholder.png";
                                }}
                              />
                              <div>
                                <h6 className="fw-bold mb-1">
                                  {product?.name || "Ürün"}
                                </h6>
                                <p className="text-muted small mb-0">
                                  {product?.categoryName ||
                                    product?.brand ||
                                    ""}
                                </p>
                                <span
                                  className="badge text-white fw-bold px-2 py-1"
                                  style={{
                                    fontSize: "0.7rem",
                                    borderRadius: 8,
                                    background:
                                      "linear-gradient(135deg,#e91e63,#ad1457)",
                                  }}
                                >
                                  <i className="fas fa-heart me-1"></i>Favorim
                                </span>
                              </div>
                            </div>
                          </div>

                          <div className="col-md-2 text-center">
                            <div className="d-flex flex-column align-items-center">
                              <p className="fw-bold text-success mb-1">
                                ₺
                                {(
                                  product?.specialPrice ??
                                  product?.price ??
                                  0
                                ).toFixed(2)}
                              </p>
                              {product?.specialPrice &&
                                product?.price &&
                                product.specialPrice !== product.price && (
                                  <p className="text-muted text-decoration-line-through small mb-0">
                                    ₺{product.price.toFixed(2)}
                                  </p>
                                )}
                            </div>
                          </div>

                          <div className="col-md-2 text-center">
                            <div className="d-flex justify-content-center gap-2">
                              <button
                                className="btn btn-outline-danger btn-sm"
                                onClick={() => handleRemoveFavorite(productId)}
                                style={{ borderRadius: 10 }}
                              >
                                <i className="fas fa-trash"></i>
                              </button>
                              <Link
                                to={`/product/${productId}`}
                                className="btn btn-outline-primary btn-sm"
                                style={{ borderRadius: 10 }}
                              >
                                <i className="fas fa-eye"></i>
                              </Link>
                            </div>
                          </div>
                        </div>
                      );
                    })}
                  </>
                ) : (
                  <div className="text-center py-5">
                    <i
                      className="fas fa-heart text-muted mb-3"
                      style={{ fontSize: "4rem", opacity: 0.3 }}
                    ></i>
                    <h5 className="text-muted fw-bold mb-2">
                      Henüz Favori Ürününüz Yok
                    </h5>
                    <p className="text-muted mb-4">
                      Beğendiğiniz ürünleri kalp butonuna tıklayarak
                      favorilerinize ekleyebilirsiniz!
                    </p>
                    <Link
                      to="/"
                      className="btn btn-lg fw-semibold"
                      style={{
                        borderRadius: 25,
                        padding: "12px 30px",
                        background: "linear-gradient(135deg,#e91e63,#ad1457)",
                        color: "white",
                        border: "none",
                      }}
                    >
                      <i className="fas fa-shopping-bag me-2"></i>Alışverişe
                      Başla
                    </Link>
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default FavoritesPage;
