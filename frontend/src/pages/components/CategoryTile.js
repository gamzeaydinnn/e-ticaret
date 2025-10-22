// CategoryTile.js
import React from "react";
import { Link } from "react-router-dom";

export default function CategoryTile({ category }) {
  const slugify = (text) =>
    (text || "")
      .toString()
      .toLowerCase()
      .normalize("NFD")
      .replace(/[\u0300-\u036f]/g, "")
      .replace(/&/g, "-")
      .replace(/[^a-z0-9]+/g, "-")
      .replace(/(^-|-$)/g, "")
      .replace(/-+/g, "-");

  const slug = category?.slug || slugify(category?.name);

  return (
    <Link to={slug ? `/category/${slug}` : "#"} className="text-decoration-none">
      <div className="border p-6 rounded-lg shadow hover:shadow-lg transition cursor-pointer text-center bg-white">
        {category?.ImageUrl && (
          <img
            src={category.ImageUrl}
            alt={category.name}
            className="mb-4 w-full h-40 object-cover rounded-lg mx-auto"
          />
        )}
        <h3 className="font-semibold text-lg text-gray-800">{category?.name || "Kategori"}</h3>
      </div>
    </Link>
  );
}
