// Ürün detay sayfasındaki görseller
import React, { useState } from "react";

export default function ProductGallery({ images = [] }) {
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
}
