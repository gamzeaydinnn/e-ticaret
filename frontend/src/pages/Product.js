// Ürün detay + varyant seçici + add to cart
import React, { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import api from "../services/api";

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

  useEffect(() => {
    api
      .get(`/api/Products/${id}`)
      .then((r) => setProduct(r.data))
      .catch(() => {});
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
        <p className="mt-4">{product.description}</p>
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
          className="w-full bg-green-600 text-white p-3 rounded"
        >
          Sepete Ekle
        </button>
      </aside>
    </div>
  );
}
