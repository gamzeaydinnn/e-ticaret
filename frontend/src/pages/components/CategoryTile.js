// CategoryTile.js
import React from "react";

export default function CategoryTile({ category }) {
  return (
    <div className="border p-4 rounded shadow hover:shadow-lg transition cursor-pointer">
      {category?.ImageUrl && (
        <img src={category.ImageUrl} alt={category.name} className="mb-2 w-full h-32 object-cover rounded" />
      )}
      <h3 className="font-semibold">{category.name}</h3>
    </div>
  );
}
