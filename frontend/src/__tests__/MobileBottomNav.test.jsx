/**
 * MobileBottomNav Unit Tests
 *
 * Mobil alt navigasyon bileşeni için unit testler.
 *
 * Requirements: 1.3, 1.4, 1.6
 */

import React from "react";
import { render, screen, fireEvent } from "@testing-library/react";
import { BrowserRouter, MemoryRouter } from "react-router-dom";
import MobileBottomNav from "../components/MobileBottomNav";

// Mock useCartCount hook
jest.mock("../hooks/useCartCount", () => ({
  useCartCount: () => ({ count: 3 }),
}));

// Mock useNavigate
const mockNavigate = jest.fn();
jest.mock("react-router-dom", () => ({
  ...jest.requireActual("react-router-dom"),
  useNavigate: () => mockNavigate,
}));

describe("MobileBottomNav", () => {
  beforeEach(() => {
    mockNavigate.mockClear();
  });

  /**
   * Render testi - Bileşenin doğru şekilde render edildiğini doğrular
   */
  describe("Render Tests", () => {
    test("renders navigation component", () => {
      render(
        <MemoryRouter>
          <MobileBottomNav />
        </MemoryRouter>
      );

      expect(screen.getByRole("navigation")).toBeInTheDocument();
    });

    test("renders all 5 navigation items", () => {
      render(
        <MemoryRouter>
          <MobileBottomNav />
        </MemoryRouter>
      );

      expect(screen.getByText("Anasayfa")).toBeInTheDocument();
      expect(screen.getByText("Kategoriler")).toBeInTheDocument();
      expect(screen.getByText("Sepetim")).toBeInTheDocument();
      expect(screen.getByText("Kampanyalar")).toBeInTheDocument();
      expect(screen.getByText("Hesabım")).toBeInTheDocument();
    });

    test("renders cart badge with count", () => {
      render(
        <MemoryRouter>
          <MobileBottomNav />
        </MemoryRouter>
      );

      // Cart badge should show count of 3 (from mock)
      expect(screen.getByText("3")).toBeInTheDocument();
    });
  });

  /**
   * Navigation item click testi - Tıklama ile doğru sayfaya yönlendirme
   */
  describe("Navigation Click Tests", () => {
    test("navigates to home when Anasayfa is clicked", () => {
      render(
        <MemoryRouter>
          <MobileBottomNav />
        </MemoryRouter>
      );

      fireEvent.click(screen.getByText("Anasayfa"));
      expect(mockNavigate).toHaveBeenCalledWith("/");
    });

    test("navigates to categories when Kategoriler is clicked", () => {
      render(
        <MemoryRouter>
          <MobileBottomNav />
        </MemoryRouter>
      );

      fireEvent.click(screen.getByText("Kategoriler"));
      expect(mockNavigate).toHaveBeenCalledWith("/category");
    });

    test("navigates to cart when Sepetim is clicked", () => {
      render(
        <MemoryRouter>
          <MobileBottomNav />
        </MemoryRouter>
      );

      fireEvent.click(screen.getByText("Sepetim"));
      expect(mockNavigate).toHaveBeenCalledWith("/cart");
    });

    test("navigates to campaigns when Kampanyalar is clicked", () => {
      render(
        <MemoryRouter>
          <MobileBottomNav />
        </MemoryRouter>
      );

      fireEvent.click(screen.getByText("Kampanyalar"));
      expect(mockNavigate).toHaveBeenCalledWith("/campaigns");
    });

    test("navigates to profile when Hesabım is clicked", () => {
      render(
        <MemoryRouter>
          <MobileBottomNav />
        </MemoryRouter>
      );

      fireEvent.click(screen.getByText("Hesabım"));
      expect(mockNavigate).toHaveBeenCalledWith("/profile");
    });
  });

  /**
   * Active state testi - Aktif sayfanın vurgulanması
   */
  describe("Active State Tests", () => {
    test("home button is active when on home page", () => {
      render(
        <MemoryRouter initialEntries={["/"]}>
          <MobileBottomNav />
        </MemoryRouter>
      );

      const homeButton = screen.getByLabelText("Anasayfa");
      expect(homeButton).toHaveClass("active");
    });

    test("categories button is active when on category page", () => {
      render(
        <MemoryRouter initialEntries={["/category/meyve-sebze"]}>
          <MobileBottomNav />
        </MemoryRouter>
      );

      const categoriesButton = screen.getByLabelText("Kategoriler");
      expect(categoriesButton).toHaveClass("active");
    });

    test("cart button is active when on cart page", () => {
      render(
        <MemoryRouter initialEntries={["/cart"]}>
          <MobileBottomNav />
        </MemoryRouter>
      );

      const cartButton = screen.getByLabelText("Sepetim");
      expect(cartButton).toHaveClass("active");
    });

    test("campaigns button is active when on campaigns page", () => {
      render(
        <MemoryRouter initialEntries={["/campaigns"]}>
          <MobileBottomNav />
        </MemoryRouter>
      );

      const campaignsButton = screen.getByLabelText("Kampanyalar");
      expect(campaignsButton).toHaveClass("active");
    });

    test("account button is active when on profile page", () => {
      render(
        <MemoryRouter initialEntries={["/profile"]}>
          <MobileBottomNav />
        </MemoryRouter>
      );

      const accountButton = screen.getByLabelText("Hesabım");
      expect(accountButton).toHaveClass("active");
    });

    test("only one button is active at a time", () => {
      render(
        <MemoryRouter initialEntries={["/cart"]}>
          <MobileBottomNav />
        </MemoryRouter>
      );

      const buttons = screen.getAllByRole("button");
      const activeButtons = buttons.filter((btn) =>
        btn.classList.contains("active")
      );
      expect(activeButtons).toHaveLength(1);
    });
  });

  /**
   * Accessibility Tests
   */
  describe("Accessibility Tests", () => {
    test("has proper aria-label on navigation", () => {
      render(
        <MemoryRouter>
          <MobileBottomNav />
        </MemoryRouter>
      );

      expect(screen.getByLabelText("Mobil navigasyon")).toBeInTheDocument();
    });

    test('active button has aria-current="page"', () => {
      render(
        <MemoryRouter initialEntries={["/"]}>
          <MobileBottomNav />
        </MemoryRouter>
      );

      const homeButton = screen.getByLabelText("Anasayfa");
      expect(homeButton).toHaveAttribute("aria-current", "page");
    });
  });
});
