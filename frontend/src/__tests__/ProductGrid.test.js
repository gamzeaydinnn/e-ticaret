import { render, screen, fireEvent } from "@testing-library/react";
import "@testing-library/jest-dom";
import ProductGrid from "../components/ProductGrid";

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
    render(<ProductGrid products={mockProducts} />);

    expect(screen.getByText("Test Product 1")).toBeInTheDocument();
    expect(screen.getByText("Test Product 2")).toBeInTheDocument();
    expect(screen.getByText("₺199,99")).toBeInTheDocument();
    expect(screen.getByText("₺299,99")).toBeInTheDocument();
  });

  test("shows empty state when no products", () => {
    render(<ProductGrid products={[]} />);

    expect(screen.getByText("Ürün bulunamadı")).toBeInTheDocument();
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

    render(<ProductGrid products={[lowStockProduct, outOfStockProduct]} />);

    expect(screen.getByText("Az Stok")).toBeInTheDocument();
    expect(screen.getByText("Stokta Yok")).toBeInTheDocument();
  });

  test("add to cart button works", () => {
    const mockAddToCart = jest.fn();

    render(<ProductGrid products={mockProducts} onAddToCart={mockAddToCart} />);

    const addToCartButtons = screen.getAllByText("Sepete Ekle");
    fireEvent.click(addToCartButtons[0]);

    expect(mockAddToCart).toHaveBeenCalledWith(mockProducts[0]);
  });

  test("shows product rating", () => {
    const productsWithRating = mockProducts.map((product) => ({
      ...product,
      rating: 4.5,
      reviewCount: 12,
    }));

    render(<ProductGrid products={productsWithRating} />);

    expect(screen.getAllByText("4.5")).toHaveLength(2);
    expect(screen.getAllByText("(12 değerlendirme)")).toHaveLength(2);
  });
});
