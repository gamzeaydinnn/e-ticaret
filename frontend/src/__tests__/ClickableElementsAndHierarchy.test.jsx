import React from "react";
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import CategorySelector from "../components/CategorySelector";
import HomeProductBlock from "../components/home/HomeProductBlock";
import CategoryTile from "../pages/components/CategoryTile";
import HeroSlider from "../components/HeroSlider";
import PromoCards from "../components/PromoCards";
import homeBlockService from "../services/homeBlockService";
import bannerService from "../services/bannerService";

// Mock homeBlockService & bannerService
jest.mock("../services/homeBlockService", () => ({
  trackPosterClick: jest.fn(),
  trackBlockView: jest.fn(),
}));

jest.mock("../services/bannerService", () => ({
  trackBannerClick: jest.fn(),
  normalizeImageUrl: (url) => url || "",
  BANNER_DIMENSIONS: { slider: {}, promo: {} },
  UPLOAD_CONFIG: { allowedMimeTypes: [] },
}));

jest.mock("../services/api", () => ({
  get: jest.fn((url) => {
    if (url === "/api/categories/root") {
      return Promise.resolve([
        { id: 1, name: "Gıda", parentId: null, isActive: true },
        { id: 2, name: "Temizlik", parentId: null, isActive: true },
      ]);
    }
    if (url.includes("/subcategories")) {
      return Promise.resolve([
        { id: 11, name: "Süt Ürünleri", parentId: 1, isActive: true },
      ]);
    }
    return Promise.resolve([]);
  }),
  post: jest.fn(() => Promise.resolve({})),
}));

// Mock IntersectionObserver
beforeAll(() => {
  global.IntersectionObserver = class {
    constructor(callback) {
      this.callback = callback;
    }
    observe() {
      // Simulate view intersection
      this.callback([{ isIntersecting: true }]);
    }
    unobserve() {}
    disconnect() {}
  };
});

describe("Phase 7 - Frontend Clickability & Category Hierarchy Tests", () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  // =========================================================================
  // 1. CategorySelector Tests
  // =========================================================================
  describe("CategorySelector Component", () => {
    it("renders categories hierarchy and filters on search input", async () => {
      const onChangeMock = jest.fn();
      render(<CategorySelector value={null} onChange={onChangeMock} name="targetCategoryId" />);

      await waitFor(() => {
        expect(screen.getByText("Gıda")).toBeInTheDocument();
        expect(screen.getByText("Temizlik")).toBeInTheDocument();
      });

      // Search input test
      const searchInput = screen.getByPlaceholderText(/Kategori veya alt kategori ara/i);
      fireEvent.change(searchInput, { target: { value: "Temiz" } });

      expect(screen.getByText("Temizlik")).toBeInTheDocument();
      expect(screen.queryByText("Gıda")).not.toBeInTheDocument();
    });

    it("triggers onChange with category id and displays breadcrumb when selected", async () => {
      const onChangeMock = jest.fn();
      const { rerender } = render(
        <CategorySelector value={null} onChange={onChangeMock} name="targetCategoryId" />
      );

      await waitFor(() => {
        expect(screen.getByText("Gıda")).toBeInTheDocument();
      });

      fireEvent.click(screen.getByText("Gıda"));
      expect(onChangeMock).toHaveBeenCalledWith({
        target: { name: "targetCategoryId", value: 1 },
      });

      // Rerender with selected parent category
      rerender(<CategorySelector value={1} onChange={onChangeMock} name="targetCategoryId" />);
      expect(screen.getByText(/Ana Kategori/i)).toBeInTheDocument();
      expect(screen.getByText(/Seçili Kategori:/i)).toBeInTheDocument();
    });
  });

  // =========================================================================
  // 2. HomeProductBlock Tests
  // =========================================================================
  describe("HomeProductBlock Component", () => {
    it("renders clickable poster with internal link and tracks click & view", async () => {
      const block = {
        id: 101,
        name: "Süt Ürünleri Bloğu",
        title: "Taze Süt Ürünleri",
        posterImageUrl: "/uploads/sut.jpg",
        posterClickTarget: "/category/sut-urunleri",
        products: [],
      };

      render(<HomeProductBlock block={block} />);

      // Verify view tracking was triggered via IntersectionObserver
      expect(homeBlockService.trackBlockView).toHaveBeenCalledWith(101);

      // Find poster link
      const posterLink = screen.getAllByRole("link")[0];
      expect(posterLink).toHaveAttribute("to", "/category/sut-urunleri");

      // Click poster and verify tracking
      fireEvent.click(posterLink);
      expect(homeBlockService.trackPosterClick).toHaveBeenCalledWith(101);
    });

    it("renders external link with target='_blank' for http targets", () => {
      const block = {
        id: 102,
        name: "Kampanya Bloğu",
        posterImageUrl: "/uploads/promo.jpg",
        posterClickTarget: "https://example.com/external-campaign",
        products: [],
      };

      render(<HomeProductBlock block={block} />);

      const posterLink = screen.getAllByRole("link")[0];
      expect(posterLink).toHaveAttribute("href", "https://example.com/external-campaign");
      expect(posterLink).toHaveAttribute("target", "_blank");
      expect(posterLink).toHaveAttribute("rel", "noopener noreferrer");
    });
  });

  // =========================================================================
  // 3. CategoryTile Tests (Mobile & Touch Feedback)
  // =========================================================================
  describe("CategoryTile Component", () => {
    it("has 44x44px min touch target size and manipulation touchAction", () => {
      const category = { id: 5, name: "Meyve & Sebze", slug: "meyve-sebze" };
      render(<CategoryTile category={category} />);

      const tile = screen.getByRole("button", { name: /Meyve & Sebze kategorisi/i });
      expect(tile).toBeInTheDocument();
      expect(tile).toHaveStyle({ minWidth: "44px", minHeight: "44px", touchAction: "manipulation" });
    });

    it("changes opacity on touchstart and resets on touchend for tactile feedback", () => {
      const category = { id: 5, name: "Meyve & Sebze", slug: "meyve-sebze" };
      render(<CategoryTile category={category} />);

      const tile = screen.getByRole("button", { name: /Meyve & Sebze kategorisi/i });
      fireEvent.touchStart(tile);
      expect(tile.style.opacity).toBe("0.7");

      fireEvent.touchEnd(tile);
      expect(tile.style.opacity).toBe("1");
    });
  });

  // =========================================================================
  // 4. HeroSlider & PromoCards Clickability Tests
  // =========================================================================
  describe("HeroSlider & PromoCards", () => {
    it("renders clickable slider slide and tracks banner click", () => {
      const banners = [
        {
          id: 201,
          title: "Büyük Bahar İndirimi",
          imageUrl: "/uploads/slider1.jpg",
          linkUrl: "/campaigns/bahar",
        },
      ];

      render(<HeroSlider banners={banners} loading={false} />);

      const link = screen.getByRole("link");
      expect(link).toHaveAttribute("to", "/campaigns/bahar");

      fireEvent.click(link);
      expect(bannerService.trackBannerClick).toHaveBeenCalledWith(201);
    });

    it("renders clickable promo cards and tracks banner click", () => {
      const promos = [
        {
          id: 301,
          title: "Gurme Peynirler",
          imageUrl: "/uploads/promo1.jpg",
          linkUrl: "https://external.com/promo",
        },
      ];

      render(<PromoCards promos={promos} loading={false} />);

      const link = screen.getByRole("article");
      expect(link).toHaveAttribute("href", "https://external.com/promo");

      fireEvent.click(link);
      expect(bannerService.trackBannerClick).toHaveBeenCalledWith(301);
    });
  });
});

