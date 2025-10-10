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
    CartService.add(product.id, 1)
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
          <button
            onClick={addToCart}
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
