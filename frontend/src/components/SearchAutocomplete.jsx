// src/components/SearchAutocomplete.jsx
import { useState, useEffect, useRef, useCallback } from "react";
import { useNavigate, Link } from "react-router-dom";
import { ProductService } from "../services/productService";

const SearchAutocomplete = () => {
  const [query, setQuery] = useState("");
  const [products, setProducts] = useState([]);
  const [suggestions, setSuggestions] = useState([]);
  const [showDropdown, setShowDropdown] = useState(false);
  const [loading, setLoading] = useState(false);
  const [productsLoaded, setProductsLoaded] = useState(false);
  const [highlightIndex, setHighlightIndex] = useState(-1);
  const navigate = useNavigate();
  const wrapperRef = useRef(null);
  const inputRef = useRef(null);

  // Ürünleri yükle (bir kez)
  useEffect(() => {
    ProductService.list()
      .then((data) => {
        console.log("Ürünler yüklendi:", data?.length || 0, "adet");
        setProducts(data || []);
        setProductsLoaded(true);
      })
      .catch((err) => {
        console.error("Ürünler yüklenemedi, mock data kullanılıyor:", err);
        // Mock data - API gelmezse
        const mockProducts = [
          { id: 1, name: "Salatalık Kg", categoryName: "Meyve & Sebze", price: 6.5, imageUrl: "/images/products/salatalik.jpg" },
          { id: 2, name: "Domates Kg", categoryName: "Meyve & Sebze", price: 8.0, imageUrl: "/images/products/domates.jpg" },
          { id: 3, name: "Patlıcan Kg", categoryName: "Meyve & Sebze", price: 12.0, imageUrl: "/images/products/patlican.jpg" },
          { id: 4, name: "Biber Kg", categoryName: "Meyve & Sebze", price: 10.0, imageUrl: "/images/products/biber.jpg" },
          { id: 5, name: "Elma Kg", categoryName: "Meyve & Sebze", price: 15.0, imageUrl: "/images/products/elma.jpg" },
          { id: 6, name: "Portakal Kg", categoryName: "Meyve & Sebze", price: 18.0, imageUrl: "/images/products/portakal.jpg" },
          { id: 7, name: "Salatalık Turşusu", categoryName: "Temel Gıda", price: 25.0, imageUrl: "/images/products/tursu.jpg" },
          { id: 8, name: "Süt 1L", categoryName: "Süt Ürünleri", price: 22.0, imageUrl: "/images/products/sut.jpg" },
          { id: 9, name: "Yoğurt 1Kg", categoryName: "Süt Ürünleri", price: 28.0, imageUrl: "/images/products/yogurt.jpg" },
          { id: 10, name: "Ekmek", categoryName: "Temel Gıda", price: 5.0, imageUrl: "/images/products/ekmek.jpg" },
        ];
        setProducts(mockProducts);
        setProductsLoaded(true);
      });
  }, []);

  // Debounce ile arama
  useEffect(() => {
    if (!query.trim() || query.length < 2) {
      setSuggestions([]);
      setShowDropdown(false);
      return;
    }

    // Ürünler henüz yüklenmediyse bekle
    if (!productsLoaded) {
      setLoading(true);
      return;
    }

    setLoading(true);
    const timer = setTimeout(() => {
      const q = query.toLowerCase();
      console.log("Arama yapılıyor:", q, "- Toplam ürün:", products.length);
      const filtered = products
        .filter(
          (p) =>
            p.name?.toLowerCase().includes(q) ||
            p.categoryName?.toLowerCase().includes(q)
        )
        .slice(0, 8);
      console.log("Bulunan ürünler:", filtered.length, filtered.map(p => p.name));
      setSuggestions(filtered);
      setShowDropdown(filtered.length > 0 || query.length >= 2);
      setLoading(false);
      setHighlightIndex(-1);
    }, 150);

    return () => clearTimeout(timer);
  }, [query, products, productsLoaded]);

  // Dışarı tıklayınca kapat
  useEffect(() => {
    const handleClickOutside = (e) => {
      if (wrapperRef.current && !wrapperRef.current.contains(e.target)) {
        setShowDropdown(false);
      }
    };
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  const handleSubmit = useCallback(
    (e) => {
      e?.preventDefault();
      if (query.trim()) {
        navigate(`/search?q=${encodeURIComponent(query.trim())}`);
        setShowDropdown(false);
        setQuery("");
      } else {
        navigate("/search");
      }
    },
    [query, navigate]
  );

  const handleKeyDown = (e) => {
    if (!showDropdown || suggestions.length === 0) {
      if (e.key === "Enter") handleSubmit(e);
      return;
    }

    switch (e.key) {
      case "ArrowDown":
        e.preventDefault();
        setHighlightIndex((prev) =>
          prev < suggestions.length - 1 ? prev + 1 : 0
        );
        break;
      case "ArrowUp":
        e.preventDefault();
        setHighlightIndex((prev) =>
          prev > 0 ? prev - 1 : suggestions.length - 1
        );
        break;
      case "Enter":
        e.preventDefault();
        if (highlightIndex >= 0 && suggestions[highlightIndex]) {
          navigate(`/product/${suggestions[highlightIndex].id}`);
          setShowDropdown(false);
          setQuery("");
        } else {
          handleSubmit(e);
        }
        break;
      case "Escape":
        setShowDropdown(false);
        break;
      default:
        break;
    }
  };

  const handleSelect = (product) => {
    navigate(`/product/${product.id}`);
    setShowDropdown(false);
    setQuery("");
  };

  // Eşleşen metni vurgula
  const highlightMatch = (text, q) => {
    if (!q || !text) return text;
    const idx = text.toLowerCase().indexOf(q.toLowerCase());
    if (idx === -1) return text;
    return (
      <>
        {text.slice(0, idx)}
        <strong style={{ color: "#ff6b35" }}>{text.slice(idx, idx + q.length)}</strong>
        {text.slice(idx + q.length)}
      </>
    );
  };

  return (
    <div ref={wrapperRef} className="search-autocomplete position-relative w-100">
      <form onSubmit={handleSubmit}>
        <div className="position-relative">
          <input
            ref={inputRef}
            type="text"
            className="form-control border-0"
            placeholder="Eve ne lazım? Ürün ara..."
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            onFocus={() => query.length >= 2 && suggestions.length > 0 && setShowDropdown(true)}
            onKeyDown={handleKeyDown}
            autoComplete="off"
            style={{
              borderRadius: "25px",
              fontSize: "0.9rem",
              paddingLeft: "20px",
              paddingRight: "45px",
              height: "42px",
              backgroundColor: "white",
              border: "2px solid #ff6b35",
              boxShadow: "0 2px 8px rgba(255,107,53,0.15)",
            }}
          />
          <button
            type="submit"
            className="btn border-0 d-flex align-items-center justify-content-center"
            style={{
              position: "absolute",
              right: "5px",
              top: "50%",
              transform: "translateY(-50%)",
              background: "transparent",
              height: "30px",
              width: "30px",
              padding: "0",
              fontSize: "0.9rem",
              color: "#ff6b35",
              zIndex: 10,
            }}
          >
            {loading ? (
              <span className="spinner-border spinner-border-sm" />
            ) : (
              <i className="fas fa-search" />
            )}
          </button>
        </div>
      </form>

      {/* Dropdown */}
      {showDropdown && query.length >= 2 && (
        <div
          className="search-dropdown position-absolute w-100 bg-white shadow-lg"
          style={{
            top: "calc(100% + 5px)",
            left: 0,
            borderRadius: "12px",
            zIndex: 9999,
            maxHeight: "400px",
            overflowY: "auto",
            border: "2px solid #ff6b35",
            boxShadow: "0 4px 20px rgba(255,107,53,0.25)",
          }}
        >
          {loading ? (
            <div className="p-4 text-center">
              <div className="spinner-border text-warning spinner-border-sm me-2" role="status">
                <span className="visually-hidden">Yükleniyor...</span>
              </div>
              <span className="text-muted">Aranıyor...</span>
            </div>
          ) : suggestions.length === 0 ? (
            <div className="p-3 text-center text-muted">
              <i className="fas fa-search me-2"></i>
              "{query}" ile eşleşen ürün bulunamadı
            </div>
          ) : (
            <>
              <div className="px-3 py-2 border-bottom" style={{ backgroundColor: "#fafafa" }}>
                <small className="text-muted fw-bold">
                  <i className="fas fa-lightbulb me-1"></i>
                  Önerilen Ürünler ({suggestions.length})
                </small>
              </div>
              {suggestions.map((product, idx) => (
                <div
                  key={product.id}
                  className={`search-suggestion d-flex align-items-center p-2 cursor-pointer ${
                    idx === highlightIndex ? "bg-light" : ""
                  }`}
                  style={{
                    cursor: "pointer",
                    borderBottom: "1px solid #f0f0f0",
                    transition: "background 0.15s",
                  }}
                  onMouseEnter={() => setHighlightIndex(idx)}
                  onClick={() => handleSelect(product)}
                >
                  <img
                    src={product.imageUrl || "/images/placeholder.png"}
                    alt={product.name}
                    style={{
                      width: "45px",
                      height: "45px",
                      objectFit: "contain",
                      borderRadius: "8px",
                      backgroundColor: "#f8f9fa",
                      padding: "3px",
                    }}
                  />
                  <div className="ms-3 flex-grow-1">
                    <div style={{ fontSize: "0.9rem" }}>
                      {highlightMatch(product.name, query)}
                    </div>
                    <small className="text-muted">{product.categoryName}</small>
                  </div>
                  <div className="text-end">
                    {product.discountPercentage > 0 || product.specialPrice < product.price ? (
                      <>
                        <div
                          className="text-decoration-line-through text-muted"
                          style={{ fontSize: "0.75rem" }}
                        >
                          ₺{product.price?.toFixed(2)}
                        </div>
                        <div className="fw-bold" style={{ color: "#ff6b35" }}>
                          ₺{(product.specialPrice || product.price)?.toFixed(2)}
                        </div>
                      </>
                    ) : (
                      <div className="fw-bold" style={{ color: "#28a745" }}>
                        ₺{product.price?.toFixed(2)}
                      </div>
                    )}
                  </div>
                </div>
              ))}
              <Link
                to={`/search?q=${encodeURIComponent(query)}`}
                className="d-block text-center py-2 text-decoration-none"
                style={{ color: "#ff6b35", backgroundColor: "#fff8f5" }}
                onClick={() => setShowDropdown(false)}
              >
                <i className="fas fa-search me-1"></i>
                Tüm sonuçları gör
              </Link>
            </>
          )}
        </div>
      )}
    </div>
  );
};

export default SearchAutocomplete;
