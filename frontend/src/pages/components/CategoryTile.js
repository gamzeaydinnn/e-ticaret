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
    <Link
      to={slug ? `/category/${slug}` : "#"}
      className="text-decoration-none"
    >
      <div className="border p-3 rounded shadow-sm hover:shadow transition cursor-pointer text-center bg-white h-100">
        {(category?.imageUrl || category?.ImageUrl) && (
          <img
            src={category?.imageUrl || category?.ImageUrl}
            alt={category?.name}
            className="mb-2"
            style={{
              width: "100%",
              height: "100px",
              objectFit: "cover",
              borderRadius: "6px",
              display: "block"
            }}
          />
        )}
        <h3 className="font-weight-600 text-gray-800" style={{ fontSize: "0.95rem" }}>
          {category?.name || "Kategori"}
        </h3>
      </div>
    </Link>
  );
}
