// CategoryTile.js
import React from "react";

export default function CategoryTile({ category }) {
  return (
    <div className="border p-6 rounded-lg shadow hover:shadow-lg transition cursor-pointer text-center bg-white">
      {category?.ImageUrl && (
        <img
          src={category.ImageUrl}
          alt={category.name}
          className="mb-4 w-full h-40 object-cover rounded-lg mx-auto"
        />
      )}
      <h3 className="font-semibold text-lg text-gray-800">{category.name}</h3>
    </div>
  );
}
