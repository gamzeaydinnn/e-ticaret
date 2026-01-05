// src/pages/SearchPage.jsx
import { useEffect, useState, useMemo, useCallback } from "react";
import { useSearchParams, Link } from "react-router-dom";
import { ProductService } from "../services/productService";
import { useCart } from "../contexts/CartContext";
import { useFavorites } from "../contexts/FavoriteContext";
import { useCompare } from "../contexts/CompareContext";

const SearchPage = () => {
  const [searchParams, setSearchParams] = useSearchParams();
  const [products, setProducts] = useState([]);
  const [categories, setCategories] = useState([]);
  const [loading, setLoading] = useState(true);
  const [showFilters, setShowFilters] = useState(false);
  
  const urlQuery = searchParams.get("q") || "";
  const [searchQuery, setSearchQuery] = useState(urlQuery);
  const [selectedCategory, setSelectedCategory] = useState(searchParams.get("category") || "");
  const [priceMin, setPriceMin] = useState(searchParams.get("minPrice") || "");
  const [priceMax, setPriceMax] = useState(searchParams.get("maxPrice") || "");
  const [sortBy, setSortBy] = useState(searchParams.get("sort") || "default");
  const [inStock, setInStock] = useState(searchParams.get("inStock") === "true");
  const [onSale, setOnSale] = useState(searchParams.get("onSale") === "true");

  const { addToCart } = useCart();
  const { toggleFavorite, isFavorite } = useFavorites();
  const { toggleCompare, isInCompare } = useCompare();

  useEffect(() => { setSearchQuery(urlQuery); }, [urlQuery]);

  useEffect(() => {
    const loadData = async () => {
      setLoading(true);
      try {
        const data = await ProductService.list();
        setProducts(data || []);
        const cats = [...new Set((data || []).map(p => p.categoryName).filter(Boolean))];
        setCategories(cats);
      } catch (err) {
        console.error("Ürünler yüklenirken hata:", err);
        setProducts([]);
      } finally {
        setLoading(false);
      }
    };
    loadData();
  }, []);

  const filteredProducts = useMemo(() => {
    let result = [...products];
    if (searchQuery.trim()) {
      const q = searchQuery.toLowerCase();
      result = result.filter(p => 
        p.name?.toLowerCase().includes(q) || 
        p.description?.toLowerCase().includes(q) ||
        p.categoryName?.toLowerCase().includes(q)
      );
    }
    if (selectedCategory) result = result.filter(p => p.categoryName === selectedCategory);
    if (priceMin) result = result.filter(p => (p.specialPrice || p.price) >= Number(priceMin));
    if (priceMax) result = result.filter(p => (p.specialPrice || p.price) <= Number(priceMax));
    if (inStock) result = result.filter(p => p.stockQuantity > 0);
    if (onSale) result = result.filter(p => p.discountPercentage > 0 || (p.specialPrice && p.specialPrice < p.price));

    switch (sortBy) {
      case "price-asc": result.sort((a, b) => (a.specialPrice || a.price) - (b.specialPrice || b.price)); break;
      case "price-desc": result.sort((a, b) => (b.specialPrice || b.price) - (a.specialPrice || a.price)); break;
      case "name-asc": result.sort((a, b) => a.name.localeCompare(b.name)); break;
      case "name-desc": result.sort((a, b) => b.name.localeCompare(a.name)); break;
      default: break;
    }
    return result;
  }, [products, searchQuery, selectedCategory, priceMin, priceMax, sortBy, inStock, onSale]);

  const applyFilters = useCallback(() => {
    const params = new URLSearchParams();
    if (searchQuery) params.set("q", searchQuery);
    if (selectedCategory) params.set("category", selectedCategory);
    if (priceMin) params.set("minPrice", priceMin);
    if (priceMax) params.set("maxPrice", priceMax);
    if (sortBy !== "default") params.set("sort", sortBy);
    if (inStock) params.set("inStock", "true");
    if (onSale) params.set("onSale", "true");
    setSearchParams(params);
    setShowFilters(false);
  }, [searchQuery, selectedCategory, priceMin, priceMax, sortBy, inStock, onSale, setSearchParams]);

  const clearFilters = () => {
    setSearchQuery(""); setSelectedCategory(""); setPriceMin(""); setPriceMax("");
    setSortBy("default"); setInStock(false); setOnSale(false);
    setSearchParams({});
  };

  const handleAddToCart = (product) => addToCart(product, 1);
  const handleToggleFavorite = (productId) => toggleFavorite(productId);
  const handleToggleCompare = (product) => {
    const r = toggleCompare(product);
    if (r.action === "limit") alert(r.message);
  };

  return (
    <div className="container-fluid py-3" style={{ backgroundColor: "#f5f5f5", minHeight: "80vh" }}>
      {/* Compact Header */}
      <div className="bg-white rounded-3 shadow-sm p-2 p-md-3 mb-3">
        <div className="d-flex justify-content-between align-items-center flex-wrap gap-2">
          <div className="d-flex align-items-center gap-2">
            <h5 className="mb-0 fw-bold" style={{ color: "#333" }}>
              <i className="fas fa-search me-2 text-warning"></i>Ürün Ara
            </h5>
            <span className="badge bg-warning text-dark">{filteredProducts.length} ürün</span>
          </div>
          <div className="d-flex align-items-center gap-2">
            <button 
              className="btn btn-sm btn-outline-warning d-lg-none"
              onClick={() => setShowFilters(!showFilters)}
            >
              <i className="fas fa-filter"></i>
            </button>
            <select
              className="form-select form-select-sm"
              style={{ width: "150px" }}
              value={sortBy}
              onChange={(e) => setSortBy(e.target.value)}
            >
              <option value="default">Sırala</option>
              <option value="price-asc">Fiyat ↑</option>
              <option value="price-desc">Fiyat ↓</option>
              <option value="name-asc">A-Z</option>
              <option value="name-desc">Z-A</option>
            </select>
          </div>
        </div>
      </div>

      <div className="row g-3">
        {/* Sidebar Filters */}
        <div className={`col-lg-3 ${showFilters ? '' : 'd-none d-lg-block'}`}>
          <div className="bg-white rounded-3 shadow-sm p-3 sticky-top" style={{ top: "10px" }}>
            <div className="d-flex justify-content-between align-items-center mb-3">
              <h6 className="mb-0 fw-bold text-warning">
                <i className="fas fa-sliders-h me-2"></i>Filtreler
              </h6>
              <button className="btn btn-sm btn-link text-muted p-0" onClick={clearFilters}>
                <i className="fas fa-undo me-1"></i>Temizle
              </button>
            </div>

            {/* Search */}
            <div className="mb-3">
              <input
                type="text"
                className="form-control form-control-sm"
                placeholder="Ürün ara..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                onKeyDown={(e) => e.key === "Enter" && applyFilters()}
              />
            </div>

            {/* Category - Butonlar */}
            <div className="mb-3">
              <label className="form-label small text-muted mb-1">Kategori</label>
              <div className="d-flex flex-wrap gap-1">
                <button
                  className={`btn btn-sm ${!selectedCategory ? 'btn-warning' : 'btn-outline-secondary'}`}
                  onClick={() => setSelectedCategory("")}
                  style={{ fontSize: "0.7rem", padding: "3px 8px" }}
                >
                  Tümü
                </button>
                {categories.map((cat) => (
                  <button
                    key={cat}
                    className={`btn btn-sm ${selectedCategory === cat ? 'btn-warning' : 'btn-outline-secondary'}`}
                    onClick={() => setSelectedCategory(cat)}
                    style={{ fontSize: "0.7rem", padding: "3px 8px" }}
                  >
                    {cat}
                  </button>
                ))}
              </div>
            </div>

            {/* Price Range */}
            <div className="mb-3">
              <label className="form-label small text-muted mb-1">Fiyat</label>
              <div className="d-flex gap-2">
                <input
                  type="number"
                  className="form-control form-control-sm"
                  placeholder="Min"
                  value={priceMin}
                  onChange={(e) => setPriceMin(e.target.value)}
                />
                <input
                  type="number"
                  className="form-control form-control-sm"
                  placeholder="Max"
                  value={priceMax}
                  onChange={(e) => setPriceMax(e.target.value)}
                />
              </div>
            </div>

            {/* Quick Filters */}
            <div className="mb-3">
              <div className="form-check form-check-inline">
                <input
                  type="checkbox"
                  className="form-check-input"
                  id="inStock"
                  checked={inStock}
                  onChange={(e) => setInStock(e.target.checked)}
                />
                <label className="form-check-label small" htmlFor="inStock">Stokta Var</label>
              </div>
              <div className="form-check form-check-inline">
                <input
                  type="checkbox"
                  className="form-check-input"
                  id="onSale"
                  checked={onSale}
                  onChange={(e) => setOnSale(e.target.checked)}
                />
                <label className="form-check-label small" htmlFor="onSale">İndirimli</label>
              </div>
            </div>

            <button className="btn btn-warning btn-sm w-100" onClick={applyFilters}>
              <i className="fas fa-check me-1"></i>Uygula
            </button>
          </div>
        </div>

        {/* Products Grid */}
        <div className="col-lg-9">
          {loading ? (
            <div className="text-center py-5">
              <div className="spinner-border text-warning"></div>
              <p className="mt-2 text-muted">Yükleniyor...</p>
            </div>
          ) : filteredProducts.length === 0 ? (
            <div className="bg-white rounded-3 text-center py-5">
              <i className="fas fa-search fa-3x text-muted mb-3"></i>
              <h6 className="text-muted">Ürün bulunamadı</h6>
              <button className="btn btn-outline-warning btn-sm mt-2" onClick={clearFilters}>
                Filtreleri Temizle
              </button>
            </div>
          ) : (
            <div className="row g-2 g-md-3">
              {filteredProducts.map((product) => (
                <div key={product.id} className="col-6 col-md-4 col-lg-3">
                  <div className="card h-100 border-0 shadow-sm" style={{ borderRadius: "10px", overflow: "hidden" }}>
                    {/* Discount Badge */}
                    {product.discountPercentage > 0 && (
                      <span className="badge bg-danger position-absolute top-0 start-0 m-2" style={{ zIndex: 2, fontSize: "0.7rem" }}>
                        -%{product.discountPercentage}
                      </span>
                    )}

                    {/* Actions */}
                    <div className="position-absolute top-0 end-0 m-1 d-flex flex-column gap-1" style={{ zIndex: 2 }}>
                      <button
                        className={`btn btn-sm rounded-circle ${isFavorite(product.id) ? "btn-danger" : "btn-light"}`}
                        onClick={() => handleToggleFavorite(product.id)}
                        style={{ width: "28px", height: "28px", padding: 0 }}
                      >
                        <i className={isFavorite(product.id) ? "fas fa-heart" : "far fa-heart"} style={{ fontSize: "0.7rem" }}></i>
                      </button>
                      <button
                        className={`btn btn-sm rounded-circle ${isInCompare(product.id) ? "btn-info text-white" : "btn-light"}`}
                        onClick={() => handleToggleCompare(product)}
                        style={{ width: "28px", height: "28px", padding: 0 }}
                      >
                        <i className="fas fa-balance-scale" style={{ fontSize: "0.6rem" }}></i>
                      </button>
                    </div>

                    {/* Image */}
                    <Link to={`/product/${product.id}`}>
                      <div className="d-flex align-items-center justify-content-center bg-light" style={{ height: "120px", padding: "8px" }}>
                        <img
                          src={product.imageUrl || "/images/placeholder.png"}
                          alt={product.name}
                          style={{ maxHeight: "100px", maxWidth: "100%", objectFit: "contain" }}
                        />
                      </div>
                    </Link>

                    {/* Body */}
                    <div className="card-body p-2 d-flex flex-column">
                      <small className="text-muted" style={{ fontSize: "0.65rem" }}>{product.categoryName}</small>
                      <Link to={`/product/${product.id}`} className="text-decoration-none">
                        <h6 className="card-title text-dark mb-1" style={{ fontSize: "0.8rem", minHeight: "32px", overflow: "hidden", display: "-webkit-box", WebkitLineClamp: 2, WebkitBoxOrient: "vertical" }}>
                          {product.name}
                        </h6>
                      </Link>

                      <div className="mt-auto">
                        {product.discountPercentage > 0 || (product.specialPrice && product.specialPrice < product.price) ? (
                          <div className="d-flex align-items-center gap-1 flex-wrap">
                            <span className="text-muted text-decoration-line-through" style={{ fontSize: "0.7rem" }}>₺{product.price?.toFixed(2)}</span>
                            <span className="fw-bold text-warning" style={{ fontSize: "0.9rem" }}>₺{(product.specialPrice || product.price)?.toFixed(2)}</span>
                          </div>
                        ) : (
                          <span className="fw-bold text-success" style={{ fontSize: "0.9rem" }}>₺{product.price?.toFixed(2)}</span>
                        )}

                        <button
                          className="btn btn-warning btn-sm w-100 mt-2"
                          style={{ fontSize: "0.75rem", padding: "4px 8px" }}
                          onClick={() => handleAddToCart(product)}
                        >
                          <i className="fas fa-cart-plus me-1"></i>Sepete Ekle
                        </button>
                      </div>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

export default SearchPage;
