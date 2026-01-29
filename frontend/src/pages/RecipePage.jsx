/**
 * RecipePage.jsx - Yemek Tarifi Detay Sayfası
 *
 * Bu sayfa, şef tavsiyesi posterlerine tıklandığında gösterilir.
 * 3 farklı tarif içerir ve her sayfa yüklemesinde rastgele bir tarif gösterilir.
 *
 * Özellikler:
 * - Rastgele tarif seçimi (her ziyarette farklı)
 * - Malzeme listesi
 * - Hazırlanış adımları
 * - Responsive tasarım
 * - Sepete ekleme entegrasyonu (gelecek özellik)
 *
 * @author Senior Developer
 * @version 1.0.0
 */

import React, { useState, useEffect, useMemo } from "react";
import { Link, useParams, useNavigate } from "react-router-dom";
import { Helmet } from "react-helmet-async";

// ============================================
// TARİF VERİLERİ
// ============================================

/**
 * 3 adet örnek yemek tarifi
 * Her tarif: başlık, açıklama, malzemeler, hazırlanış adımları içerir
 */
const RECIPES = [
  {
    id: 1,
    title: "Tavuk Sote",
    subtitle: "Sebzeli ve Lezzetli",
    description:
      "Yumuşacık tavuk parçaları ile rengarenk sebzelerin muhteşem uyumu. Pratik ve doyurucu bir ana yemek.",
    prepTime: "15 dk",
    cookTime: "35 dk",
    servings: "4 kişilik",
    difficulty: "Kolay",
    image:
      "https://images.unsplash.com/photo-1598515214211-89d3c73ae83b?w=800&q=80",
    ingredients: [
      { name: "Tavuk göğsü", amount: "500 g", category: "Et" },
      { name: "Yeşil biber", amount: "2 adet", category: "Sebze" },
      { name: "Domates", amount: "3 adet", category: "Sebze" },
      { name: "Soğan", amount: "1 adet (orta boy)", category: "Sebze" },
      { name: "Sarımsak", amount: "3 diş", category: "Sebze" },
      { name: "Zeytinyağı", amount: "3 yemek kaşığı", category: "Yağ" },
      { name: "Tuz", amount: "1 tatlı kaşığı", category: "Baharat" },
      { name: "Karabiber", amount: "1 çay kaşığı", category: "Baharat" },
      { name: "Pul biber", amount: "1 çay kaşığı", category: "Baharat" },
      { name: "Kekik", amount: "1 çay kaşığı", category: "Baharat" },
    ],
    steps: [
      "Tavuk göğsünü kuşbaşı doğrayın ve bir kaseye alın.",
      "Tuz, karabiber ve kekik ile marine edin, 10 dakika bekletin.",
      "Geniş bir tavada zeytinyağını kızdırın.",
      "Marine edilmiş tavukları ekleyip orta ateşte suyunu çekene kadar kavurun.",
      "Soğanları ay doğrayın, sarımsakları ince kıyın ve tavuklara ekleyin.",
      "Soğanlar yumuşayana kadar karıştırarak pişirin.",
      "Domatesleri küp küp doğrayıp ekleyin.",
      "Yeşil biberleri halka halka kesip en son ekleyin.",
      "Kapağını kapatıp kısık ateşte 15-20 dakika pişirin.",
      "Pul biber serpin ve sıcak servis yapın. Afiyet olsun!",
    ],
    tips: [
      "Tavukları çok sık karıştırmayın, güzel kızarması için bekletin.",
      "Sebzeleri çok ince doğramayın, formu korusunlar.",
      "Yanında pilav veya bulgur pilavı ile servis edebilirsiniz.",
    ],
    color: "#ff6b35", // Turuncu tema
  },
  {
    id: 2,
    title: "Mercimek Çorbası",
    subtitle: "Geleneksel Türk Lezzeti",
    description:
      "Her sofraya yakışan, vitamin deposu enfes bir çorba. Kış aylarının vazgeçilmezi.",
    prepTime: "10 dk",
    cookTime: "40 dk",
    servings: "6 kişilik",
    difficulty: "Çok Kolay",
    image:
      "https://images.unsplash.com/photo-1547592166-23ac45744acd?w=800&q=80",
    ingredients: [
      {
        name: "Kırmızı mercimek",
        amount: "1.5 su bardağı",
        category: "Baklagil",
      },
      { name: "Soğan", amount: "1 adet (büyük)", category: "Sebze" },
      { name: "Havuç", amount: "1 adet", category: "Sebze" },
      { name: "Patates", amount: "1 adet (küçük)", category: "Sebze" },
      { name: "Su", amount: "8 su bardağı", category: "Sıvı" },
      { name: "Tereyağı", amount: "2 yemek kaşığı", category: "Yağ" },
      { name: "Un", amount: "1 yemek kaşığı", category: "Diğer" },
      { name: "Tuz", amount: "1 tatlı kaşığı", category: "Baharat" },
      { name: "Kimyon", amount: "1 çay kaşığı", category: "Baharat" },
      { name: "Pul biber", amount: "Servis için", category: "Baharat" },
      { name: "Limon", amount: "Servis için", category: "Meyve" },
    ],
    steps: [
      "Mercimekleri yıkayıp süzün.",
      "Soğanı, havucu ve patatesi küp küp doğrayın.",
      "Derin bir tencereye tüm sebzeleri ve mercimeği alın.",
      "8 bardak su ekleyip kaynatın.",
      "Kaynamaya başlayınca ateşi kısın ve 35-40 dakika pişirin.",
      "Mercimekler ve sebzeler yumuşadığında ocaktan alın.",
      "Blender ile pürüzsüz kıvama gelene kadar çekin.",
      "Ayrı bir tavada tereyağını eritin, unu ekleyip kavurun.",
      "Pul biber ekleyip çorbanın üzerine gezdirin.",
      "Limon ve ekstra pul biber ile sıcak servis yapın.",
    ],
    tips: [
      "Daha yoğun bir kıvam için su miktarını azaltabilirsiniz.",
      "Nane-sarımsaklı yoğurt eşliğinde servis edebilirsiniz.",
      "Bayat ekmek krutonları ile mükemmel ikili oluşturur.",
    ],
    color: "#d97706", // Amber tema
  },
  {
    id: 3,
    title: "Fırında Sebzeli Köfte",
    subtitle: "Pratik ve Sağlıklı",
    description:
      "Fırında pişirilen hafif köfteler ve mevsim sebzeleri. Yağsız, lezzetli ve doyurucu.",
    prepTime: "20 dk",
    cookTime: "45 dk",
    servings: "4 kişilik",
    difficulty: "Orta",
    image:
      "https://images.unsplash.com/photo-1529042410759-befb1204b468?w=800&q=80",
    ingredients: [
      { name: "Dana kıyma", amount: "400 g", category: "Et" },
      { name: "Soğan", amount: "1 adet (rendelenmiş)", category: "Sebze" },
      { name: "Sarımsak", amount: "2 diş", category: "Sebze" },
      { name: "Galeta unu", amount: "3 yemek kaşığı", category: "Diğer" },
      { name: "Yumurta", amount: "1 adet", category: "Diğer" },
      { name: "Patates", amount: "3 adet (orta boy)", category: "Sebze" },
      { name: "Biber", amount: "2 adet (yeşil/kırmızı)", category: "Sebze" },
      { name: "Domates", amount: "2 adet", category: "Sebze" },
      { name: "Zeytinyağı", amount: "4 yemek kaşığı", category: "Yağ" },
      {
        name: "Tuz, karabiber, kimyon",
        amount: "Damak tadına göre",
        category: "Baharat",
      },
    ],
    steps: [
      "Fırını 200°C'ye ön ısıtmaya alın.",
      "Kıymayı geniş bir kaseye alın.",
      "Rendelenmiş soğan, ezilmiş sarımsak, galeta unu ve yumurtayı ekleyin.",
      "Tuz ve baharatları ekleyip iyice yoğurun.",
      "Ceviz büyüklüğünde köfteler şekillendirin.",
      "Patatesleri yuvarlak dilimler halinde kesin.",
      "Fırın tepsisine patatesleri dizin, üzerine zeytinyağı gezdirin.",
      "Köfteleri patateslerin üzerine yerleştirin.",
      "Domates ve biberleri dilimleyip boşluklara yerleştirin.",
      "200°C fırında 40-45 dakika pişirin. Ara ara kontrol edin.",
      "Üzeri kızardığında çıkarın ve sıcak servis yapın.",
    ],
    tips: [
      "Köfte harcını 30 dakika buzdolabında dinlendirirseniz daha iyi şekillenir.",
      "Patateslerin altına az su eklerseniz daha yumuşak olur.",
      "Yanında cacık veya yoğurt ile servis edin.",
    ],
    color: "#059669", // Yeşil tema
  },
];

// ============================================
// ANA BİLEŞEN
// ============================================

export default function RecipePage() {
  const { id } = useParams();
  const navigate = useNavigate();

  // ============================================
  // RASTGELE TARİF SEÇİMİ
  // ============================================

  /**
   * Her sayfa yüklemesinde rastgele bir tarif seç
   * ID parametresi verilmişse o tarifi göster, yoksa random
   */
  const recipe = useMemo(() => {
    // URL'de ID varsa ve geçerli bir tarif ID'si ise onu göster
    const recipeId = parseInt(id);
    if (recipeId && recipeId >= 1 && recipeId <= RECIPES.length) {
      return RECIPES.find((r) => r.id === recipeId) || RECIPES[0];
    }

    // ID yoksa veya geçersizse rastgele seç
    const randomIndex = Math.floor(Math.random() * RECIPES.length);
    return RECIPES[randomIndex];
  }, [id]);

  // ============================================
  // DİĞER TARİFLER (Mevcut tarif hariç)
  // ============================================

  const otherRecipes = useMemo(() => {
    return RECIPES.filter((r) => r.id !== recipe.id);
  }, [recipe.id]);

  // ============================================
  // RENDER
  // ============================================

  return (
    <div style={{ backgroundColor: "#faf5f0", minHeight: "100vh" }}>
      {/* SEO Helmet */}
      <Helmet>
        <title>{recipe.title} Tarifi | Doğadan Sofranza</title>
        <meta name="description" content={recipe.description} />
      </Helmet>

      {/* ========== HERO BANNER ========== */}
      <div
        style={{
          position: "relative",
          width: "100%",
          height: "350px",
          overflow: "hidden",
        }}
      >
        <img
          src={recipe.image}
          alt={recipe.title}
          style={{
            width: "100%",
            height: "100%",
            objectFit: "cover",
            filter: "brightness(0.7)",
          }}
        />
        <div
          style={{
            position: "absolute",
            inset: 0,
            background: `linear-gradient(to top, ${recipe.color}dd, transparent)`,
            display: "flex",
            flexDirection: "column",
            justifyContent: "flex-end",
            padding: "40px",
          }}
        >
          {/* Geri Butonu */}
          <button
            onClick={() => navigate(-1)}
            style={{
              position: "absolute",
              top: "20px",
              left: "20px",
              background: "rgba(255,255,255,0.9)",
              border: "none",
              borderRadius: "50%",
              width: "44px",
              height: "44px",
              cursor: "pointer",
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              boxShadow: "0 2px 8px rgba(0,0,0,0.15)",
              transition: "transform 0.2s",
            }}
            onMouseEnter={(e) =>
              (e.currentTarget.style.transform = "scale(1.1)")
            }
            onMouseLeave={(e) => (e.currentTarget.style.transform = "scale(1)")}
          >
            <i
              className="fas fa-arrow-left"
              style={{ fontSize: "1.1rem", color: "#333" }}
            ></i>
          </button>

          <span
            style={{
              backgroundColor: "rgba(255,255,255,0.2)",
              color: "white",
              padding: "4px 12px",
              borderRadius: "20px",
              fontSize: "0.8rem",
              fontWeight: "500",
              marginBottom: "8px",
              width: "fit-content",
            }}
          >
            {recipe.subtitle}
          </span>
          <h1
            style={{
              color: "white",
              fontSize: "2.5rem",
              fontWeight: "800",
              margin: 0,
              textShadow: "0 2px 10px rgba(0,0,0,0.3)",
            }}
          >
            {recipe.title}
          </h1>
        </div>
      </div>

      {/* ========== ANA İÇERİK ========== */}
      <div
        style={{ maxWidth: "900px", margin: "0 auto", padding: "24px 16px" }}
      >
        {/* Tarif Bilgileri Kartları */}
        <div
          style={{
            display: "grid",
            gridTemplateColumns: "repeat(auto-fit, minmax(120px, 1fr))",
            gap: "12px",
            marginBottom: "32px",
            marginTop: "-60px",
            position: "relative",
            zIndex: 10,
          }}
        >
          {[
            { icon: "fa-clock", label: "Hazırlık", value: recipe.prepTime },
            { icon: "fa-fire", label: "Pişirme", value: recipe.cookTime },
            { icon: "fa-users", label: "Porsiyon", value: recipe.servings },
            { icon: "fa-signal", label: "Zorluk", value: recipe.difficulty },
          ].map((info, index) => (
            <div
              key={index}
              style={{
                backgroundColor: "white",
                padding: "16px",
                borderRadius: "12px",
                textAlign: "center",
                boxShadow: "0 4px 12px rgba(0,0,0,0.08)",
              }}
            >
              <i
                className={`fas ${info.icon}`}
                style={{
                  fontSize: "1.5rem",
                  color: recipe.color,
                  marginBottom: "8px",
                  display: "block",
                }}
              ></i>
              <div
                style={{
                  fontSize: "0.75rem",
                  color: "#6b7280",
                  marginBottom: "2px",
                }}
              >
                {info.label}
              </div>
              <div
                style={{
                  fontSize: "0.95rem",
                  fontWeight: "600",
                  color: "#1f2937",
                }}
              >
                {info.value}
              </div>
            </div>
          ))}
        </div>

        {/* Açıklama */}
        <div
          style={{
            backgroundColor: "white",
            padding: "24px",
            borderRadius: "16px",
            marginBottom: "24px",
            boxShadow: "0 2px 8px rgba(0,0,0,0.05)",
          }}
        >
          <p
            style={{
              fontSize: "1.1rem",
              color: "#374151",
              lineHeight: "1.7",
              margin: 0,
            }}
          >
            {recipe.description}
          </p>
        </div>

        {/* İki Sütunlu Layout: Malzemeler + Hazırlanış */}
        <div
          className="recipe-content-grid"
          style={{
            display: "grid",
            gridTemplateColumns: "1fr 2fr",
            gap: "24px",
          }}
        >
          {/* ========== MALZEMELER ========== */}
          <div
            style={{
              backgroundColor: "white",
              padding: "24px",
              borderRadius: "16px",
              boxShadow: "0 2px 8px rgba(0,0,0,0.05)",
              height: "fit-content",
            }}
          >
            <h2
              style={{
                fontSize: "1.25rem",
                fontWeight: "700",
                marginBottom: "20px",
                display: "flex",
                alignItems: "center",
                gap: "10px",
                color: recipe.color,
              }}
            >
              <i className="fas fa-shopping-basket"></i>
              Malzemeler
            </h2>
            <ul style={{ listStyle: "none", padding: 0, margin: 0 }}>
              {recipe.ingredients.map((ing, index) => (
                <li
                  key={index}
                  style={{
                    display: "flex",
                    justifyContent: "space-between",
                    alignItems: "center",
                    padding: "10px 0",
                    borderBottom:
                      index < recipe.ingredients.length - 1
                        ? "1px solid #f3f4f6"
                        : "none",
                  }}
                >
                  <span style={{ color: "#374151", fontWeight: "500" }}>
                    {ing.name}
                  </span>
                  <span style={{ color: "#6b7280", fontSize: "0.9rem" }}>
                    {ing.amount}
                  </span>
                </li>
              ))}
            </ul>
          </div>

          {/* ========== HAZIRLANIŞI ========== */}
          <div
            style={{
              backgroundColor: "white",
              padding: "24px",
              borderRadius: "16px",
              boxShadow: "0 2px 8px rgba(0,0,0,0.05)",
            }}
          >
            <h2
              style={{
                fontSize: "1.25rem",
                fontWeight: "700",
                marginBottom: "20px",
                display: "flex",
                alignItems: "center",
                gap: "10px",
                color: recipe.color,
              }}
            >
              <i className="fas fa-list-ol"></i>
              Hazırlanışı
            </h2>
            <ol
              style={{
                padding: "0 0 0 0",
                margin: 0,
                counterReset: "step-counter",
              }}
            >
              {recipe.steps.map((step, index) => (
                <li
                  key={index}
                  style={{
                    display: "flex",
                    alignItems: "flex-start",
                    gap: "16px",
                    marginBottom: "16px",
                    listStyle: "none",
                  }}
                >
                  <span
                    style={{
                      backgroundColor: recipe.color,
                      color: "white",
                      width: "28px",
                      height: "28px",
                      borderRadius: "50%",
                      display: "flex",
                      alignItems: "center",
                      justifyContent: "center",
                      fontWeight: "700",
                      fontSize: "0.85rem",
                      flexShrink: 0,
                    }}
                  >
                    {index + 1}
                  </span>
                  <p
                    style={{
                      margin: 0,
                      color: "#374151",
                      lineHeight: "1.6",
                      flex: 1,
                    }}
                  >
                    {step}
                  </p>
                </li>
              ))}
            </ol>
          </div>
        </div>

        {/* ========== İPUÇLARI ========== */}
        <div
          style={{
            backgroundColor: `${recipe.color}15`,
            padding: "24px",
            borderRadius: "16px",
            marginTop: "24px",
            border: `1px solid ${recipe.color}30`,
          }}
        >
          <h3
            style={{
              fontSize: "1.1rem",
              fontWeight: "700",
              marginBottom: "16px",
              display: "flex",
              alignItems: "center",
              gap: "10px",
              color: recipe.color,
            }}
          >
            <i className="fas fa-lightbulb"></i>
            Şefin İpuçları
          </h3>
          <ul style={{ margin: 0, paddingLeft: "20px" }}>
            {recipe.tips.map((tip, index) => (
              <li
                key={index}
                style={{
                  color: "#374151",
                  marginBottom: index < recipe.tips.length - 1 ? "8px" : 0,
                  lineHeight: "1.6",
                }}
              >
                {tip}
              </li>
            ))}
          </ul>
        </div>

        {/* ========== DİĞER TARİFLER ========== */}
        <div style={{ marginTop: "48px" }}>
          <h3
            style={{
              fontSize: "1.25rem",
              fontWeight: "700",
              marginBottom: "20px",
              display: "flex",
              alignItems: "center",
              gap: "10px",
              color: "#374151",
            }}
          >
            <i className="fas fa-utensils" style={{ color: "#ff6b35" }}></i>
            Diğer Tarifler
          </h3>
          <div
            style={{
              display: "grid",
              gridTemplateColumns: "repeat(auto-fit, minmax(250px, 1fr))",
              gap: "16px",
            }}
          >
            {otherRecipes.map((otherRecipe) => (
              <Link
                key={otherRecipe.id}
                to={`/tarif/${otherRecipe.id}`}
                style={{
                  textDecoration: "none",
                  backgroundColor: "white",
                  borderRadius: "12px",
                  overflow: "hidden",
                  boxShadow: "0 2px 8px rgba(0,0,0,0.08)",
                  transition: "transform 0.3s, box-shadow 0.3s",
                }}
                onMouseEnter={(e) => {
                  e.currentTarget.style.transform = "translateY(-4px)";
                  e.currentTarget.style.boxShadow =
                    "0 8px 20px rgba(0,0,0,0.12)";
                }}
                onMouseLeave={(e) => {
                  e.currentTarget.style.transform = "translateY(0)";
                  e.currentTarget.style.boxShadow =
                    "0 2px 8px rgba(0,0,0,0.08)";
                }}
              >
                <img
                  src={otherRecipe.image}
                  alt={otherRecipe.title}
                  style={{
                    width: "100%",
                    height: "140px",
                    objectFit: "cover",
                  }}
                />
                <div style={{ padding: "16px" }}>
                  <h4
                    style={{
                      margin: "0 0 4px",
                      fontSize: "1rem",
                      fontWeight: "600",
                      color: "#1f2937",
                    }}
                  >
                    {otherRecipe.title}
                  </h4>
                  <p
                    style={{ margin: 0, fontSize: "0.85rem", color: "#6b7280" }}
                  >
                    {otherRecipe.subtitle}
                  </p>
                </div>
              </Link>
            ))}
          </div>
        </div>

        {/* ========== ANA SAYFAYA DÖN BUTONU ========== */}
        <div
          style={{
            textAlign: "center",
            marginTop: "48px",
            marginBottom: "32px",
          }}
        >
          <Link
            to="/"
            style={{
              display: "inline-flex",
              alignItems: "center",
              gap: "8px",
              backgroundColor: recipe.color,
              color: "white",
              padding: "14px 28px",
              borderRadius: "12px",
              textDecoration: "none",
              fontWeight: "600",
              fontSize: "1rem",
              transition: "transform 0.2s, box-shadow 0.2s",
              boxShadow: "0 4px 12px rgba(0,0,0,0.15)",
            }}
            onMouseEnter={(e) => {
              e.currentTarget.style.transform = "translateY(-2px)";
              e.currentTarget.style.boxShadow = "0 6px 16px rgba(0,0,0,0.2)";
            }}
            onMouseLeave={(e) => {
              e.currentTarget.style.transform = "translateY(0)";
              e.currentTarget.style.boxShadow = "0 4px 12px rgba(0,0,0,0.15)";
            }}
          >
            <i className="fas fa-home"></i>
            Ana Sayfaya Dön
          </Link>
        </div>
      </div>

      {/* Responsive CSS */}
      <style>{`
        @media (max-width: 768px) {
          .recipe-content-grid {
            grid-template-columns: 1fr !important;
          }
        }
      `}</style>
    </div>
  );
}
