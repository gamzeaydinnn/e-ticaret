// src/pages/SearchPage.jsx
import { useEffect, useState, useMemo } from "react";
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
  const [showMobileFilters, setShowMobileFilters] = useState(false);
  
  // URL'den gelen query
  const urlQuery = searchParams.get("q") || "";
  
  // Filtreler
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

  // URL değişince searchQuery'yi güncelle
  useEffect(() => {
    setSearchQuery(urlQuery);
  }, [urlQuery]);

  // Ürünleri yükle
  useEffect(() => {
    const loadData = async () => {
      setLoading(true);
      try {
        const [productsData] = await Promise.all([
          ProductService.list(),
        ]);
        setProducts(productsData || []);
        
        // Kategorileri ürünlerden çıkar
        const uniqueCategories = [...new Set(productsData.map(p => p.categoryName).filter(Boolean))];
        setCategories(uniqueCategories);
      } catch (error) {
        console.error("Ürünler yüklenirken hata, mock data kullanılıyor:", error);
        // Mock data - API gelmezse
        const mockProducts = [
          { id: 1, name: "Salatalık Kg", categoryName: "Meyve & Sebze", price: 6.5, specialPrice: null, discountPercentage: 0, stockQuantity: 50, imageUrl: "/images/products/salatalik.jpg" },
          { id: 2, name: "Domates Kg", categoryName: "Meyve & Sebze", price: 8.0, specialPrice: 7.0, discountPercentage: 12, stockQuantity: 80, imageUrl: "/images/products/domates.jpg" },
          { id: 3, name: "Patlıcan Kg", categoryName: "Meyve & Sebze", price: 12.0, specialPrice: null, discountPercentage: 0, stockQuantity: 30, imageUrl: "/images/products/patlican.jpg" },
          { id: 4, name: "Biber Kg", categoryName: "Meyve & Sebze", price: 10.0, specialPrice: 8.5, discountPercentage: 15, stockQuantity: 40, imageUrl: "/images/products/biber.jpg" },
          { id: 5, name: "Elma Kg", categoryName: "Meyve & Sebze", price: 15.0, specialPrice: null, discountPercentage: 0, stockQuantity: 100, imageUrl: "/images/products/elma.jpg" },
          { id: 6, name: "Portakal Kg", categoryName: "Meyve & Sebze", price: 18.0, specialPrice: 16.0, discountPercentage: 11, stockQuantity: 60, imageUrl: "/images/products/portakal.jpg" },
          { id: 7, name: "Salatalık Turşusu", categoryName: "Temel Gıda", price: 25.0, specialPrice: null, discountPercentage: 0, stockQuantity: 25, imageUrl: "/images/products/tursu.jpg" },
          { id: 8, name: "Süt 1L", categoryName: "Süt Ürünleri", price: 22.0, specialPrice: 20.0, discountPercentage: 9, stockQuantity: 120, imageUrl: "/images/products/sut.jpg" },
          { id: 9, name: "Yoğurt 1Kg", categoryName: "Süt Ürünleri", price: 28.0, specialPrice: null, discountPercentage: 0, stockQuantity: 45, imageUrl: "/images/products/yogurt.jpg" },
          { id: 10, name: "Ekmek", categoryName: "Temel Gıda", price: 5.0, specialPrice: null, discountPercentage: 0, stockQuantity: 200, imageUrl: "/images/products/ekmek.jpg" },
          { id: 11, name: "Zeytinyağı 1L", categoryName: "Temel Gıda", price: 120.0, specialPrice: 99.0, discountPercentage: 17, stockQuantity: 15, imageUrl: "/images/products/zeytinyagi.jpg" },
          { id: 12, name: "Peynir 500g", categoryName: "Süt Ürünleri", price: 85.0, specialPrice: null, discountPercentage: 0, stockQuantity: 35, imageUrl: "/images/products/peynir.jpg" },
        ];
        setProducts(mockProducts);
        const uniqueCategories = [...new Set(mockProducts.map(p => p.categoryName).filter(Boolean))];
        setCategories(uniqueCategories);
      } finally {
        setLoading(false);
      }
    };
    loadData();
  }, []);

  // Filtrelenmiş ve sıralanmış ürünler
  const filteredProducts = useMemo(() => {
    let result = [...products];

    // Metin araması
    if (searchQuery.trim()) {
      const query = searchQuery.toLowerCase();
      result = result.filter(
        (p) =>
          p.name?.toLowerCase().includes(query) ||
          p.description?.toLowerCase().includes(query) ||
          p.categoryName?.toLowerCase().includes(query)
      );
    }

    // Kategori filtresi
    if (selectedCategory) {
      result = result.filter((p) => p.categoryName === selectedCategory);
    }

    // Fiyat aralığı
    if (priceMin) {
      result = result.filter((p) => (p.specialPrice || p.price) >= Number(priceMin));
    }
    if (priceMax) {
      result = result.filter((p) => (p.specialPrice || p.price) <= Number(priceMax));
    }

    // Stokta var
    if (inStock) {
      result = result.filter((p) => p.stockQuantity > 0);
    }

    // İndirimde
    if (onSale) {
      result = result.filter((p) => p.discountPercentage > 0 || p.specialPrice < p.price);
    }

    // Sıralama
    switch (sortBy) {
      case "price-asc":
        result.sort((a, b) => (a.specialPrice || a.price) - (b.specialPrice || b.price));
        break;
      case "price-desc":
        result.sort((a, b) => (b.specialPrice || b.price) - (a.specialPrice || a.price));
        break;
      case "name-asc":
        result.sort((a, b) => a.name.localeCompare(b.name));
        break;
      case "name-desc":
        result.sort((a, b) => b.name.localeCompare(a.name));
        break;
      case "rating":
        result.sort((a, b) => (b.rating || 0) - (a.rating || 0));
        break;
      case "discount":
        result.sort((a, b) => (b.discountPercentage || 0) - (a.discountPercentage || 0));
        break;
      default:
        break;
    }

    return result;
  }, [products, searchQuery, selectedCategory, priceMin, priceMax, sortBy, inStock, onSale]);

  // URL'i güncelle
  const updateFilters = () => {
    const params = new URLSearchParams();
    if (searchQuery) params.set("q", searchQuery);
    if (selectedCategory) params.set("category", selectedCategory);
    if (priceMin) params.set("minPrice", priceMin);
    if (priceMax) params.set("maxPrice", priceMax);
    if (sortBy !== "default") params.set("sort", sortBy);
    if (inStock) params.set("inStock", "true");
    if (onSale) params.set("onSale", "true");
    setSearchParams(params);
  };

  // Filtreleri temizle
  const clearFilters = () => {
    setSearchQuery("");
    setSelectedCategory("");
    setPriceMin("");
    setPriceMax("");
    setSortBy("default");
    setInStock(false);
    setOnSale(false);
    setSearchParams({});
  };

  const handleAddToCart = (product) => {
    addToCart(product, 1);
  };

  const handleToggleFavorite = (productId) => {
    toggleFavorite(productId);
  };

  const handleToggleCompare = (product) => {
    const result = toggleCompare(product);
    if (result.action === "limit") {
      alert(result.message);
    }
  };

  return (
    <div className="container-fluid py-4" style={{ backgroundColor: "#f8f9fa", minHeight: "80vh" }}>
      {/* Header Bar */}
      <div className="bg-white rounded-3 shadow-sm p-3 mb-4">
        <div className="d-flex justify-content-between align-items-center flex-wrap gap-3">
          <div className="d-flex align-items-center gap-3">
            <h4 className="mb-0" style={{ color: "#333" }}>
              <i className="fas fa-search me-2" style={{ color: "#ff6b35" }}></i>
              Ürün Ara
            </h4>
            <span className="badge" style={{ backgroundColor: "#ff6b35", fontSize: "0.9rem" }}>
              {filteredProducts.length} ürün
            </span>
          </div>
          <div className="d-flex align-items-center gap-3 flex-wrap">
            <button 
              className="btn btn-outline-secondary d-lg-none"
              onClick={() => setShowMobileFilters(!showMobileFilters)}
            >
              <i className="fas fa-filter me-1"></i>Filtreler
            </button>
            <div className="d-flex align-items-center gap-2">
              <i className="fas fa-sort-amount-down text-muted"></i>
              <select
                className="form-select form-select-sm border-0"
                style={{ width: "180px", backgroundColor: "#f8f9fa" }}
                value={sortBy}
                onChange={(e) => setSortBy(e.target.value)}
              >
                <option value="default">Varsayılan Sıralama</option>
                <option value="price-asc">Fiyat: Düşük → Yüksek</option>
                <option value="price-desc">Fiyat: Yüksek → Düşük</option>
                <option value="name-asc">İsim: A-Z</option>
                <option value="name-desc">İsim: Z-A</option>
                <option value="discount">En Çok İndirimli</option>
              </select>
            </div>
          </div>
        </div>
      </div>

      <div className="row g-4">
        {/* Sidebar Filters */}
        <div className={`col-lg-3 ${showMobileFilters ? '' : 'd-none d-lg-block'}`}>
          <div className="bg-white rounded-3 shadow-sm overflow-hidden sticky-top" style={{ top: "20px" }}>
            {/* Filter Header */}
            <div 
              className="px-4 py-3 d-flex justify-content-between align-items-center"
              style={{ background: "linear-gradient(135deg, #ff6b35 0%, #ff8c5a 100%)" }}
            >
              <h6 className="mb-0 text-white fw-bold">
                <i className="fas fa-sliders-h me-2"></i>Filtreler
              </h6>
              <button 
                className="btn btn-sm btn-light px-2 py-1"
                onClick={clearFilters}
                style={{ fontSize: "0.75rem" }}
              >
                <i className="fas fa-undo me-1"></i>Temizle
              </button>
            </div>

            <div className="p-3">
              {/* Search Input */}
              <div className="mb-4">
                <label className="form-label small text-muted fw-bold mb-2">
                  <i className="fas fa-search me-1"></i>Ürün Adı
                </label>
                <input
                  type="text"
                  className="form-control rounded-pill"
                  placeholder="Ürün ara..."
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  onKeyDown={(e) => e.key === "Enter" && updateFilters()}
                  style={{ fontSize: "0.9rem", border: "1px solid #eee" }}
                />
              </div>

              {/* Category Select */}
              <div className="mb-4">
                <label className="form-label small text-muted fw-bold mb-2">
                  <i className="fas fa-tags me-1"></i>Kategori
                </label>
                <select
                  className="form-select rounded-pill"
                  value={selectedCategory}
                  onChange={(e) => setSelectedCategory(e.target.value)}
                  style={{ fontSize: "0.9rem", border: "1px solid #eee" }}
                >
                  <option value="">Tüm Kategoriler</option>
                  {categories.map((cat) => (
                    <option key={cat} value={cat}>{cat}</option>
                  ))}
                </select>
              </div>

              {/* Price Range */}
              <div className="mb-4">
                <label className="form-label small text-muted fw-bold mb-2">
                  <i className="fas fa-lira-sign me-1"></i>Fiyat Aralığı
                </label>
                <div className="d-flex gap-2 align-items-center">
                  <input
                    type="number"
                    className="form-control rounded-pill text-center"
                    placeholder="Min"
                    value={priceMin}
                    onChange={(e) => setPriceMin(e.target.value)}
                    style={{ fontSize: "0.85rem", border: "1px solid #eee" }}
                  />
                  <span className="text-muted">-</span>
                  <input
                    type="number"
                    className="form-control rounded-pill text-center"
                    placeholder="Max"
                    value={priceMax}
                    onChange={(e) => setPriceMax(e.target.value)}
                    style={{ fontSize: "0.85rem", border: "1px solid #eee" }}
                  />
                </div>
              </div>

              {/* Quick Filters */}
              <div className="mb-4">
                <label className="form-label small text-muted fw-bold mb-2">
                  <i className="fas fa-bolt me-1"></i>Hızlı Filtreler
                </label>
                <div className="d-flex flex-column gap-2">
                  <label 
                    className={`d-flex align-items-center gap-2 p-2 rounded-3 cursor-pointer ${inStock ? 'bg-success bg-opacity-10' : 'bg-light'}`}
                    style={{ cursor: "pointer", transition: "all 0.2s" }}
                  >
                    <input
                      type="checkbox"
                      className="form-check-input m-0"
                      checked={inStock}
                      onChange={(e) => setInStock(e.target.checked)}
                      style={{ accentColor: "#28a745" }}
                    />
                    <span style={{ fontSize: "0.9rem" }}>
                      <i className="fas fa-box text-success me-1"></i>Stokta Var
                    </span>
                  </label>
                  <label 
                    className={`d-flex align-items-center gap-2 p-2 rounded-3 cursor-pointer ${onSale ? 'bg-danger bg-opacity-10' : 'bg-light'}`}
                    style={{ cursor: "pointer", transition: "all 0.2s" }}
                  >
                    <input
                      type="checkbox"
                      className="form-check-input m-0"
                      checked={onSale}
                      onChange={(e) => setOnSale(e.target.checked)}
                      style={{ accentColor: "#dc3545" }}
                    />
                    <span style={{ fontSize: "0.9rem" }}>
                      <i className="fas fa-percent text-danger me-1"></i>İndirimli
                    </span>
                  </label>
                </div>
              </div>

              {/* Apply Button */}
              <button 
                className="btn w-100 text-white fw-bold rounded-pill"
                onClick={updateFilters}
                style={{ background: "linear-gradient(135deg, #ff6b35 0%, #ff8c5a 100%)" }}
              >
                <i className="fas fa-check-circle me-2"></i>Uygula
              </button>
            </div>
          </div>
        </div>

        {/* Products Grid */}
        <div className="col-lg-9">
          {loading ? (
            <div className="text-center py-5">
              <div className="spinner-border" style={{ color: "#ff6b35" }} role="status">
                <span className="visually-hidden">Yükleniyor...</span>
              </div>
              <p className="mt-3 text-muted">Ürünler yükleniyor...</p>
            </div>
          ) : filteredProducts.length === 0 ? (
            <div className="bg-white rounded-3 shadow-sm text-center py-5">
              <i className="fas fa-search fa-4x mb-3" style={{ color: "#ddd" }}></i>
              <h5 className="text-muted">Aramanızla eşleşen ürün bulunamadı</h5>
              <p className="text-muted small">Farklı filtreler deneyin veya arama teriminizi değiştirin.</p>
              <button className="btn btn-outline-warning mt-2" onClick={clearFilters}>
                <i className="fas fa-undo me-2"></i>Filtreleri Temizle
              </button>
            </div>
          ) : (
            <div className="row g-3">
              {filteredProducts.map((product) => (
                <div key={product.id} className="col-xl-3 col-lg-4 col-md-6 col-6">
                  <div
                    className="card h-100 bg-white border-0 shadow-sm product-card"
                    style={{ borderRadius: "12px", overflow: "hidden", transition: "all 0.25s ease" }}
                    onMouseEnter={(e) => {
                      e.currentTarget.style.transform = "translateY(-5px)";
                      e.currentTarget.style.boxShadow = "0 12px 25px rgba(0,0,0,0.12)";
                    }}
                    onMouseLeave={(e) => {
                      e.currentTarget.style.transform = "translateY(0)";
                      e.currentTarget.style.boxShadow = "0 0.125rem 0.25rem rgba(0,0,0,0.075)";
                    }}
                  >
                    {/* Badges */}
                    {product.discountPercentage > 0 && (
                      <div className="position-absolute top-0 start-0 m-2" style={{ zIndex: 2 }}>
                        <span className="badge bg-danger rounded-pill px-2">
                          -%{product.discountPercentage}
                        </span>
                      </div>
                    )}

                    {/* Action Buttons */}
                    <div className="position-absolute top-0 end-0 m-2 d-flex flex-column gap-1" style={{ zIndex: 2 }}>
                      <button
                        className={`btn btn-sm rounded-circle shadow-sm ${
                          isFavorite(product.id) ? "btn-danger" : "btn-light"
                        }`}
                        onClick={() => handleToggleFavorite(product.id)}
                        style={{ width: "32px", height: "32px", padding: 0 }}
                      >
                        <i className={isFavorite(product.id) ? "fas fa-heart" : "far fa-heart"} style={{ fontSize: "0.8rem" }}></i>
                      </button>
                      <button
                        className={`btn btn-sm rounded-circle shadow-sm ${
                          isInCompare(product.id) ? "btn-info text-white" : "btn-light"
                        }`}
                        onClick={() => handleToggleCompare(product)}
                        style={{ width: "32px", height: "32px", padding: 0 }}
                      >
                        <i className="fas fa-balance-scale" style={{ fontSize: "0.7rem" }}></i>
                      </button>
                    </div>

                    {/* Image */}
                    <Link to={`/product/${product.id}`}>
                      <div
                        className="d-flex align-items-center justify-content-center"
                        style={{ height: "140px", backgroundColor: "#fafafa", padding: "10px" }}
                      >
                        <img
                          src={product.imageUrl || "/images/placeholder.png"}
                          alt={product.name}
                          style={{ maxHeight: "120px", maxWidth: "100%", objectFit: "contain" }}
                        />
                      </div>
                    </Link>

                    {/* Body */}
                    <div className="card-body p-3 d-flex flex-column">
                      <small className="text-muted" style={{ fontSize: "0.7rem" }}>{product.categoryName}</small>
                      <Link to={`/product/${product.id}`} className="text-decoration-none">
                        <h6 
                          className="card-title mt-1 text-dark" 
                          style={{ 
                            fontSize: "0.85rem", 
                            minHeight: "36px", 
                            overflow: "hidden",
                            display: "-webkit-box",
                            WebkitLineClamp: 2,
                            WebkitBoxOrient: "vertical"
                          }}
                        >
                          {product.name}
                        </h6>
                      </Link>

                      <div className="mt-auto">
                        {product.discountPercentage > 0 ? (
                          <div className="d-flex align-items-center gap-2">
                            <span className="text-muted text-decoration-line-through" style={{ fontSize: "0.8rem" }}>
                              ₺{product.price?.toFixed(2)}
                            </span>
                            <span className="fw-bold" style={{ color: "#ff6b35", fontSize: "1rem" }}>
                              ₺{(product.specialPrice || product.price)?.toFixed(2)}
                            </span>
                          </div>
                        ) : (
                          <span className="fw-bold" style={{ color: "#28a745", fontSize: "1rem" }}>
                            ₺{product.price?.toFixed(2)}
                          </span>
                        )}

                        <button
                          className="btn btn-warning w-100 mt-2 rounded-pill"
                          style={{ fontSize: "0.8rem", padding: "6px" }}
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
