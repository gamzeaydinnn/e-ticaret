// CategoryTile.js
import React, { useState, useEffect, useRef } from "react";
import { Link, useNavigate } from "react-router-dom";

export default function CategoryTile({ category }) {
  const [showMegamenu, setShowMegamenu] = useState(false);
  const [isMobile, setIsMobile] = useState(false);
  const timeoutRef = useRef(null);
  const menuRef = useRef(null);
  const navigate = useNavigate();

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
  const hasSubcategories = category?.subCategories?.length > 0;

  useEffect(() => {
    const checkMobile = () => setIsMobile(window.innerWidth < 768);
    checkMobile();
    window.addEventListener("resize", checkMobile);
    return () => window.removeEventListener("resize", checkMobile);
  }, []);

  useEffect(() => {
    if (!showMegamenu) return;
    const handleClickOutside = (e) => {
      if (menuRef.current && !menuRef.current.contains(e.target)) {
        setShowMegamenu(false);
      }
    };
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, [showMegamenu]);

  const handleMouseEnter = () => {
    if (isMobile || !hasSubcategories) return;
    clearTimeout(timeoutRef.current);
    timeoutRef.current = setTimeout(() => setShowMegamenu(true), 200);
  };

  const handleMouseLeave = () => {
    if (isMobile || !hasSubcategories) return;
    clearTimeout(timeoutRef.current);
    timeoutRef.current = setTimeout(() => setShowMegamenu(false), 300);
  };

  const handleClick = (e) => {
    if (!hasSubcategories) {
      navigate(`/kategoriler/${slug}`);
      return;
    }
    if (isMobile) {
      e.preventDefault();
      setShowMegamenu(true);
    }
  };

  const handleKeyDown = (e) => {
    if (e.key === "Enter" || e.key === " ") {
      e.preventDefault();
      if (hasSubcategories) {
        setShowMegamenu(true);
      } else {
        navigate(`/kategoriler/${slug}`);
      }
    }
    if (e.key === "Escape") {
      setShowMegamenu(false);
    }
  };

  return (
    <div
      ref={menuRef}
      style={{ position: "relative" }}
      onMouseEnter={handleMouseEnter}
      onMouseLeave={handleMouseLeave}
    >
      <div
        onClick={handleClick}
        onKeyDown={handleKeyDown}
        tabIndex={0}
        role="button"
        aria-expanded={showMegamenu}
        aria-label={`${category?.name} kategorisi${hasSubcategories ? " - alt kategoriler mevcut" : ""}`}
        className="border p-3 rounded shadow-sm hover:shadow transition cursor-pointer text-center bg-white h-100"
        style={{
          outline: "none",
          textDecoration: "none",
        }}
      >
        {(category?.imageUrl || category?.ImageUrl) && (
          <img
            src={category?.imageUrl || category?.ImageUrl}
            alt={category?.name}
            className="mb-2"
            style={{
              width: "100%",
              height: "70px",
              objectFit: "cover",
              borderRadius: "6px",
              display: "block",
            }}
          />
        )}
        <h3
          className="font-weight-600 text-gray-800"
          style={{ fontSize: "0.8rem", margin: 0 }}
        >
          {category?.name || "Kategori"}
          {hasSubcategories && (
            <i
              className="fas fa-chevron-down ms-1"
              style={{ fontSize: "0.65rem", opacity: 0.6 }}
            ></i>
          )}
        </h3>
      </div>

      {hasSubcategories && showMegamenu && (
        <>
          {isMobile && (
            <div
              style={{
                position: "fixed",
                top: 0,
                left: 0,
                right: 0,
                bottom: 0,
                backgroundColor: "rgba(0, 0, 0, 0.5)",
                zIndex: 999,
              }}
              onClick={() => setShowMegamenu(false)}
            />
          )}
          <div
            style={{
              position: isMobile ? "fixed" : "absolute",
              top: isMobile ? "50%" : "100%",
              left: isMobile ? "50%" : 0,
              transform: isMobile ? "translate(-50%, -50%)" : "translateY(8px)",
              backgroundColor: "white",
              borderRadius: 8,
              boxShadow: "0 4px 12px rgba(0, 0, 0, 0.15)",
              padding: 16,
              zIndex: 1000,
              width: isMobile ? "90%" : "auto",
              minWidth: isMobile ? "auto" : 280,
              maxWidth: isMobile ? "90vw" : 400,
              maxHeight: isMobile ? "80vh" : "80vh",
              overflowY: "auto",
              animation: isMobile ? "none" : "megamenuFadeIn 200ms ease-out",
            }}
            onClick={(e) => e.stopPropagation()}
          >
            {isMobile && (
              <button
                onClick={() => setShowMegamenu(false)}
                aria-label="Kapat"
                style={{
                  position: "absolute",
                  top: 8,
                  right: 8,
                  background: "none",
                  border: "none",
                  fontSize: "1.25rem",
                  cursor: "pointer",
                  color: "#6b7280",
                  padding: 4,
                }}
              >
                <i className="fas fa-times"></i>
              </button>
            )}
            <div
              style={{
                display: "grid",
                gridTemplateColumns: isMobile
                  ? "1fr"
                  : "repeat(auto-fill, minmax(140px, 1fr))",
                gap: 8,
                marginTop: isMobile ? 24 : 0,
              }}
            >
              {category.subCategories.map((sub) => {
                const subSlug = sub?.slug || slugify(sub?.name);
                return (
                  <Link
                    key={sub.id}
                    to={`/kategoriler/${subSlug}`}
                    onClick={() => setShowMegamenu(false)}
                    style={{
                      textDecoration: "none",
                      color: "#374151",
                      padding: "8px 12px",
                      borderRadius: 6,
                      transition: "background-color 150ms",
                      display: "block",
                    }}
                    onMouseEnter={(e) => {
                      e.currentTarget.style.backgroundColor = "#f3f4f6";
                    }}
                    onMouseLeave={(e) => {
                      e.currentTarget.style.backgroundColor = "transparent";
                    }}
                  >
                    {sub.name}
                  </Link>
                );
              })}
            </div>
          </div>
        </>
      )}

      <style>{`
        @keyframes megamenuFadeIn {
          from {
            opacity: 0;
            transform: translateY(-2px);
          }
          to {
            opacity: 1;
            transform: translateY(8px);
          }
        }
      `}</style>
    </div>
  );
}
