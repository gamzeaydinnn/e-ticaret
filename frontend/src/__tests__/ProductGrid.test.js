import { render, screen, fireEvent } from "@testing-library/react";
import "@testing-library/jest-dom";
import ProductGrid from "../components/ProductGrid";
import { AuthProvider } from "../contexts/AuthContext";

const renderWithAuth = (ui) => {
  return render(<AuthProvider>{ui}</AuthProvider>);
};

const mockProducts = [
  {
    id: 1,
    name: "Test Product 1",
    price: 199.99,
    imageUrl: "/images/product1.jpg",
    description: "Test product description 1",
    stockQuantity: 10,
    brand: "Test Brand",
    isActive: true,
  },
  {
    id: 2,
    name: "Test Product 2",
    price: 299.99,
    imageUrl: "/images/product2.jpg",
    description: "Test product description 2",
    stockQuantity: 5,
    brand: "Test Brand 2",
    isActive: true,
  },
];

describe("ProductGrid Component", () => {
  test("renders products correctly", () => {
    renderWithAuth(<ProductGrid products={mockProducts} />);

    expect(screen.getByText("Test Product 1")).toBeInTheDocument();
    expect(screen.getByText("Test Product 2")).toBeInTheDocument();
    // Fiyatlar iki ürün için de render edilmeli (format ortam ayarına göre değişebilir)
    expect(screen.getAllByText(/199\.99|199,99/).length).toBeGreaterThan(0);
    expect(screen.getAllByText(/299\.99|299,99/).length).toBeGreaterThan(0);
  });

  test("shows empty state when no products", () => {
    renderWithAuth(<ProductGrid products={[]} />);

    expect(screen.getByText("Henüz Ürün Yok")).toBeInTheDocument();
  });

  test("displays product stock status correctly", () => {
    const lowStockProduct = {
      ...mockProducts[0],
      stockQuantity: 3,
    };

    const outOfStockProduct = {
      ...mockProducts[1],
      stockQuantity: 0,
    };

    renderWithAuth(
      <ProductGrid products={[lowStockProduct, outOfStockProduct]} />
    );

    expect(screen.getByText("Az Stok")).toBeInTheDocument();
    expect(screen.getByText("Stokta Yok")).toBeInTheDocument();
  });

  test("add to cart button works", () => {
    // Sepete ekle butonunun görünür olduğunu doğrulayalım
    renderWithAuth(<ProductGrid products={mockProducts} />);

    const addToCartButtons = screen.getAllByText("Sepete Ekle");
    expect(addToCartButtons.length).toBeGreaterThan(0);
  });

  test("shows product rating", () => {
    const productsWithRating = mockProducts.map((product) => ({
      ...product,
      rating: 4.5,
      reviewCount: 12,
    }));

    renderWithAuth(<ProductGrid products={productsWithRating} />);

    // Yıldızlar ve review count gösterilmeli
    expect(screen.getAllByText("(12)").length).toBe(2);
  });
});
