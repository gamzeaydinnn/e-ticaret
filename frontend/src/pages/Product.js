// Ürün detay + varyant seçici + add to cart
import React, { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import api from "../services/api";
import reviewService from "../services/reviewService";
import ReviewList from "../components/ReviewList";
import ReviewForm from "../components/ReviewForm";
import { useAuth } from "../contexts/AuthContext";
import { subscribeStockUpdates, joinProduct, leaveProduct } from "../services/stockHub";

// ProductGallery component'ini burada tanımlayalım
const ProductGallery = ({ images = [] }) => {
  const [active, setActive] = useState(0);

  if (images.length === 0) {
    return (
      <div className="w-full h-64 bg-gray-100 rounded flex items-center justify-center">
        <span className="text-gray-500">Görsel bulunamadı</span>
      </div>
    );
  }

  return (
    <div>
      <div className="w-full h-96 rounded overflow-hidden">
        <img
          src={images[active]}
          alt="main product"
          className="w-full h-full object-contain"
        />
      </div>
      <div className="flex gap-2 mt-2">
        {images.map((src, i) => (
          <button
            key={i}
            onClick={() => setActive(i)}
            className={`w-20 h-20 border rounded overflow-hidden ${
              i === active ? "ring-2 ring-indigo-500" : ""
            }`}
          >
            <img
              src={src}
              alt={`product ${i + 1}`}
              className="w-full h-full object-cover"
            />
          </button>
        ))}
      </div>
    </div>
  );
};

export default function Product() {
  const { id } = useParams();
  const [product, setProduct] = useState(null);
  const [quantity, setQuantity] = useState(1);
  const [selectedVariant, setSelectedVariant] = useState(null);
  const [reviews, setReviews] = useState([]);
  const { user } = useAuth();

  useEffect(() => {
    api
      .get(`/api/Products/${id}`)
      .then((r) => setProduct(r.data))
      .catch(() => {});
  }, [id]);

  // Load reviews for the product
  useEffect(() => {
    if (!id) return;
    reviewService
      .getReviewsByProductId(id)
      .then((r) => setReviews(r.data))
      .catch(() => setReviews([]));
  }, [id]);

  const handleReviewSubmitted = async () => {
    try {
      const r = await reviewService.getReviewsByProductId(id);
      setReviews(r.data);
    } catch {}
  };

  // Realtime stock updates for this product
  useEffect(() => {
    if (!id) return;
    let unsub = () => {};
    (async () => {
      try { await joinProduct(id); } catch {}
      unsub = subscribeStockUpdates(({ productId, quantity }) => {
        if (String(productId) === String(id)) {
          setProduct((prev) => (prev ? { ...prev, stockQuantity: quantity } : prev));
        }
      });
    })();
    return () => {
      unsub();
      leaveProduct(id);
    };
  }, [id]);

  const addToCart = async () => {
    try {
      await api.post("/api/CartItems", {
        productId: product.id,
        qty: quantity,
        variant: selectedVariant,
      });
      window.dispatchEvent(new CustomEvent("cart:updated"));
      alert("Sepete eklendi");
    } catch (error) {
      alert("Sepete eklenirken hata oluştu");
    }
  };

  if (!product) return <div className="p-8">Yükleniyor...</div>;

  return (
    <div className="container mx-auto px-4 py-8 grid grid-cols-1 md:grid-cols-3 gap-6">
      <div className="md:col-span-2">
        <ProductGallery images={product.images || []} />
        <h1 className="text-2xl font-bold mt-4">{product.name}</h1>
        <p className="mt-2 text-lg font-semibold">₺{product.price}</p>
        <p className="mt-1 text-sm">
          Stok: {typeof product.stockQuantity === 'number' ? product.stockQuantity : '—'}
        </p>
        <p className="mt-4">{product.description}</p>

        {/* Reviews Section */}
        <div className="mt-10">
          <h2 className="text-xl font-semibold mb-4">Ürün Yorumları</h2>
          <ReviewList reviews={reviews} />
          {user ? (
            <div className="mt-6">
              <h3 className="text-lg font-medium mb-2">Yorum Yap</h3>
              <ReviewForm productId={id} onReviewSubmitted={handleReviewSubmitted} />
            </div>
          ) : (
            <p className="text-gray-500 italic mt-4">Yorum yapmak için giriş yapmalısınız.</p>
          )}
        </div>
      </div>

      <aside className="p-4 bg-white rounded shadow">
        <div className="mb-4">
          <label className="block text-sm">Adet</label>
          <input
            type="number"
            value={quantity}
            min={1}
            onChange={(e) => setQuantity(Number(e.target.value))}
            className="border p-2 w-full"
          />
        </div>

        {product.variants?.length > 0 && (
          <div className="mb-4">
            <label className="block text-sm">Varyant</label>
            <select
              className="border p-2 w-full"
              onChange={(e) => setSelectedVariant(e.target.value)}
            >
              <option value="">Seçiniz</option>
              {product.variants.map((v) => (
                <option key={v.id} value={v.id}>
                  {v.name}
                </option>
              ))}
            </select>
          </div>
        )}

        <button
          onClick={addToCart}
          disabled={typeof product.stockQuantity === 'number' && product.stockQuantity <= 0}
          className={`w-full p-3 rounded ${
            typeof product.stockQuantity === 'number' && product.stockQuantity <= 0
              ? 'bg-gray-400 text-white cursor-not-allowed'
              : 'bg-green-600 text-white'
          }`}
        >
          {typeof product.stockQuantity === 'number' && product.stockQuantity <= 0
            ? 'Stokta Yok'
            : 'Sepete Ekle'}
        </button>
      </aside>
    </div>
  );
}
