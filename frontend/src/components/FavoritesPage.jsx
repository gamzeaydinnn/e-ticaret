import { Link } from "react-router-dom";
import { useFavorites } from "../contexts/FavoriteContext";
import "./FavoritesPage.css";

const FavoritesPage = () => {
  const { favorites = [], loading, removeFromFavorites } = useFavorites();

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
    <div className="favorites-page-shell">
      <div className="container favorites-page-container">
        <div className="favorites-page-card shadow-lg">
          <div className="favorites-page-header">
            <h3 className="mb-0 fw-bold">
              <i className="fas fa-heart me-2" />
              Favorilerim
            </h3>
            <span className="favorites-count-badge">{favorites.length} Ürün</span>
          </div>

          <div className="favorites-page-body">
            {loading ? (
              <div className="favorites-empty-state">
                <div
                  className="spinner-border mb-3"
                  role="status"
                  style={{ width: "3rem", height: "3rem" }}
                >
                  <span className="visually-hidden">Yükleniyor</span>
                </div>
                <p className="text-muted fw-bold mb-0">Favoriler yükleniyor...</p>
              </div>
            ) : favorites.length > 0 ? (
              <ul className="favorites-list">
                {favorites.map((product) => {
                  const productId = product.id || product.productId;
                  const price = product?.specialPrice ?? product?.price ?? 0;
                  const hasDiscount =
                    product?.specialPrice &&
                    product?.price &&
                    product.specialPrice !== product.price;

                  return (
                    <li key={productId} className="favorite-item-card">
                      <div className="favorite-item-main">
                        <img
                          src={product?.imageUrl || "/images/placeholder.png"}
                          alt={product?.name || "Ürün"}
                          className="favorite-item-image"
                          onError={(e) => {
                            e.target.src = "/images/placeholder.png";
                          }}
                        />
                        <div className="favorite-item-info">
                          <h6 className="favorite-item-name">
                            {product?.name || "Ürün"}
                          </h6>
                          {(product?.categoryName || product?.brand) && (
                            <p className="favorite-item-meta">
                              {product?.categoryName || product?.brand}
                            </p>
                          )}
                          <span className="favorite-item-tag">
                            <i className="fas fa-heart me-1" />
                            Favorim
                          </span>
                        </div>
                      </div>

                      <div className="favorite-item-footer">
                        <div className="favorite-item-price">
                          <span className="favorite-price-current">
                            ₺{Number(price).toFixed(2)}
                          </span>
                          {hasDiscount && (
                            <span className="favorite-price-old">
                              ₺{Number(product.price).toFixed(2)}
                            </span>
                          )}
                        </div>
                        <div className="favorite-item-actions">
                          <button
                            type="button"
                            className="favorite-action-btn favorite-action-btn--danger"
                            onClick={() => handleRemoveFavorite(productId)}
                            aria-label="Favoriden kaldır"
                          >
                            <i className="fas fa-trash" />
                          </button>
                          <Link
                            to={`/product/${productId}`}
                            className="favorite-action-btn favorite-action-btn--primary"
                            aria-label="Ürünü görüntüle"
                          >
                            <i className="fas fa-eye" />
                          </Link>
                        </div>
                      </div>
                    </li>
                  );
                })}
              </ul>
            ) : (
              <div className="favorites-empty-state">
                <i className="fas fa-heart favorites-empty-icon" />
                <h5 className="text-muted fw-bold mb-2">
                  Henüz Favori Ürününüz Yok
                </h5>
                <p className="text-muted mb-4">
                  Beğendiğiniz ürünleri kalp butonuna tıklayarak
                  favorilerinize ekleyebilirsiniz.
                </p>
                <Link to="/" className="favorites-shop-btn">
                  <i className="fas fa-shopping-bag me-2" />
                  Alışverişe Başla
                </Link>
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};

export default FavoritesPage;
