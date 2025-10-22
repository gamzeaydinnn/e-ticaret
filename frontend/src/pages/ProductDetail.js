/*
1. Amaç
Kullanıcının seçtiği ürünün detaylarını göstermek.
Ürün adı, açıklama, fiyat, stok durumu, görsel.
Ürünü sepete ekleme butonu.
İsteğe bağlı olarak ürün yorumları, benzer ürünler veya kategori bilgisi.*/
import React, { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { ProductService } from "../services/productService";
import { CartService } from "../services/cartService";
import getProductCategoryRules from "../config/productCategoryRules";
import reviewService from "../services/reviewService"; // ✅ yorum servisi
import ReviewList from "../components/ReviewList"; // ✅ yorumları listeleyen component
import ReviewForm from "../components/ReviewForm"; // ✅ yorum formu
import { useAuth } from "../hooks/useAuth"; // ✅ kullanıcı giriş kontrolü

export default function ProductDetail() {
  const { id } = useParams();
  const [product, setProduct] = useState(null);
  const [reviews, setReviews] = useState([]);
  const { user } = useAuth(); // kullanıcı giriş yapmış mı kontrol eder

  // Ürün detayını getir
  useEffect(() => {
    ProductService.getById(id)
      .then(setProduct)
      .catch(() => {});
  }, [id]);

  // Yorumları getir
  const fetchReviews = async () => {
    try {
      const response = await reviewService.getReviewsByProductId(id);
      setReviews(response.data);
    } catch (error) {
      console.error("Yorumlar getirilirken hata oluştu:", error);
    }
  };

  useEffect(() => {
    fetchReviews();
  }, [id]);

  // Yorum gönderilince listeyi yenile
  const handleReviewSubmitted = () => {
    fetchReviews();
  };

  const addToCart = () => {
    CartService.add(product.id, quantity)
      .then(() => alert("Sepete eklendi"))
      .catch(() => alert("Hata oluştu"));
  };

  const [quantity, setQuantity] = useState(1);
  const [rule, setRule] = useState(null);
  const [error, setError] = useState("");

  useEffect(() => {
    let mounted = true;
    (async () => {
      try {
        const rules = await getProductCategoryRules();
        if (!mounted) return;
        let match = (rules || []).find((r) => {
          const examples = (r.examples || []).map((e) =>
            String(e).toLowerCase()
          );
          const pname = (product.name || "").toLowerCase();
          return (
            (r.category || "").toLowerCase().includes(pname) ||
            examples.some((ex) => pname.includes(ex) || ex.includes(pname))
          );
        });
        const pcat = (product.categoryName || "").toLowerCase();
        if (
          !match &&
          (pcat.includes("meyve") ||
            pcat.includes("sebze") ||
            pcat.includes("et") ||
            pcat.includes("tavuk") ||
            pcat.includes("balık") ||
            pcat.includes("balik"))
        ) {
          match =
            (rules || []).find((r) => (r.unit || "").toLowerCase() === "kg") ||
            null;
        }
        // categories that should be sold as units with min 1 max 10
        const unitLimitCats = [
          "süt",
          "süt ürünleri",
          "süt urunleri",
          "temel gıda",
          "temel gida",
          "temizlik",
          "içecek",
          "icecek",
          "atıştırmalık",
          "atistirmalik",
        ];
        if (!match && unitLimitCats.some((tok) => pcat.includes(tok))) {
          match = {
            category: "Kategori adedi sınırı",
            unit: "adet",
            min_quantity: 1,
            max_quantity: 10,
            step: 1,
          };
        }
        setRule(match || null);
        if (match) {
          // set sensible default quantity respecting min and step
          const defaultQ = match.min_quantity || 1;
          setQuantity(defaultQ);
        }
      } catch (e) {
        setRule(null);
      }
    })();
    return () => (mounted = false);
  }, [product]);

  const validateAndAdd = () => {
    setError("");
    const q = parseFloat(quantity);
    if (isNaN(q) || q <= 0) return setError("Geçerli miktar girin.");
    if (rule) {
      const min = parseFloat(rule.min_quantity ?? -Infinity);
      const max = parseFloat(rule.max_quantity ?? Infinity);
      const step = parseFloat(rule.step ?? (rule.unit === "kg" ? 0.25 : 1));
      if (q < min) return setError(`Minimum ${min} ${rule.unit} olmalıdır.`);
      if (q > max)
        return setError(`Maksimum ${max} ${rule.unit} ile sınırlıdır.`);
      // step validation: allow small float rounding
      const remainder = Math.abs(
        (q - min) / step - Math.round((q - min) / step)
      );
      if (remainder > 1e-6)
        return setError(`Miktar ${step} ${rule.unit} adımlarıyla olmalıdır.`);
    } else {
      // default business rule: do not allow >5 units for regular (non-weighted) items
      if (!product.isWeighted && q > 5)
        return setError("Bu üründen en fazla 5 adet ekleyebilirsiniz.");
    }
    // pass validation
    CartService.add(product.id, q)
      .then(() => alert("Sepete eklendi"))
      .catch(() => alert("Hata oluştu"));
  };

  if (!product) return <p>Yükleniyor...</p>;

  return (
    <div className="container mx-auto px-4 py-8">
      {/* ÜRÜN DETAYI */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6 mb-10">
        <img
          src={product.imageUrl}
          alt={product.name}
          className="w-full rounded shadow"
        />
        <div>
          <h1 className="text-2xl font-bold mb-4">{product.name}</h1>
          <p className="mb-4">{product.description}</p>
          <div className="text-xl font-semibold mb-4">₺{product.price}</div>

          <div className="flex items-center gap-3 mb-3">
            <label className="block text-sm">Miktar</label>
            <input
              type="number"
              step={rule ? rule.step ?? (rule.unit === "kg" ? 0.25 : 1) : 1}
              value={quantity}
              onChange={(e) => setQuantity(e.target.value)}
              className="border rounded px-2 py-1"
              style={{ width: 120 }}
            />
            <div className="text-sm text-gray-600">
              {rule ? rule.unit : product.unit || "adet"}
            </div>
          </div>

          {rule && (
            <div className="text-sm text-muted mb-2">
              Min: {rule.min_quantity} — Max: {rule.max_quantity} — Adım:{" "}
              {rule.step ?? (rule.unit === "kg" ? 0.25 : 1)} {rule.unit}
            </div>
          )}

          {error && <div className="text-sm text-danger mb-2">{error}</div>}

          <button
            onClick={validateAndAdd}
            className="bg-blue-500 text-white px-4 py-2 rounded hover:bg-blue-600"
          >
            Sepete Ekle
          </button>
        </div>
      </div>

      {/* YORUMLAR */}
      <div className="mt-8">
        <h2 className="text-xl font-semibold mb-4">Ürün Yorumları</h2>
        <ReviewList reviews={reviews} />

        {user ? (
          <div className="mt-6">
            <h3 className="text-lg font-medium mb-2">Yorum Yap</h3>
            <ReviewForm
              productId={id}
              onReviewSubmitted={handleReviewSubmitted}
            />
          </div>
        ) : (
          <p className="text-gray-500 italic mt-4">
            Yorum yapmak için giriş yapmalısınız.
          </p>
        )}
      </div>
    </div>
  );
}
