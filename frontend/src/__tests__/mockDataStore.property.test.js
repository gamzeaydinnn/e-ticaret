/**
 * MockDataStore Property-Based Tests
 * ===================================
 * Feature: admin-mock-data-sync, homepage-poster-management
 *
 * Bu testler mockDataStore'un CRUD operasyonlarının tutarlılığını doğrular.
 * Property-based testing kullanarak çeşitli senaryoları test eder.
 */

import * as fc from "fast-check";
import mockDataStore from "../services/mockDataStore";

// Her test öncesi store'u sıfırla
beforeEach(() => {
  mockDataStore.resetToDefaults();
});

describe("MockDataStore Property-Based Tests", () => {
  /**
   * Property 1: Category Update Propagation
   * **Validates: Requirements 1.1 (admin-mock-data-sync)**
   *
   * For any category name update, all products in that category
   * SHALL have their categoryName updated to match.
   */
  test("Property 1: Category Update Propagation", () => {
    fc.assert(
      fc.property(
        fc
          .string({ minLength: 1, maxLength: 50 })
          .filter((s) => s.trim().length > 0),
        (newCategoryName) => {
          // Get first category
          const categories = mockDataStore.getAllCategories();
          if (categories.length === 0) return true;

          const category = categories[0];
          const categoryId = category.id;

          // Get products in this category before update
          const productsBefore = mockDataStore
            .getAllProducts()
            .filter((p) => p.categoryId === categoryId);

          // Update category name
          mockDataStore.updateCategory(categoryId, {
            ...category,
            name: newCategoryName,
          });

          // Check all products have updated categoryName
          const productsAfter = mockDataStore
            .getAllProducts()
            .filter((p) => p.categoryId === categoryId);

          return productsAfter.every((p) => p.categoryName === newCategoryName);
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Property 2: Category-Product Name Consistency
   * **Validates: Requirements 1.4 (admin-mock-data-sync)**
   *
   * For any product, its categoryName SHALL match the name of
   * the category with matching categoryId.
   */
  test("Property 2: Category-Product Name Consistency", () => {
    fc.assert(
      fc.property(fc.nat({ max: 100 }), () => {
        const products = mockDataStore.getAllProducts();
        const categories = mockDataStore.getAllCategories();

        return products.every((product) => {
          const category = categories.find((c) => c.id === product.categoryId);
          if (!category) return true; // Orphan product, skip
          return product.categoryName === category.name;
        });
      }),
      { numRuns: 100 }
    );
  });

  /**
   * Property 3: Product Addition Increases List Size
   * **Validates: Requirements 2.1 (admin-mock-data-sync)**
   *
   * For any valid product data, creating a product SHALL increase
   * the total product count by exactly 1.
   */
  test("Property 3: Product Addition Increases List Size", () => {
    fc.assert(
      fc.property(
        fc.record({
          name: fc
            .string({ minLength: 1, maxLength: 50 })
            .filter((s) => s.trim().length > 0),
          price: fc.float({ min: 0.01, max: 10000, noNaN: true }),
          stock: fc.nat({ max: 1000 }),
          description: fc.string({ maxLength: 200 }),
        }),
        (productData) => {
          const categories = mockDataStore.getAllCategories();
          if (categories.length === 0) return true;

          const countBefore = mockDataStore.getAllProducts().length;

          mockDataStore.createProduct({
            ...productData,
            categoryId: categories[0].id,
          });

          const countAfter = mockDataStore.getAllProducts().length;

          return countAfter === countBefore + 1;
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Property 5: Product Deletion Decreases List Size
   * **Validates: Requirements 3.1 (admin-mock-data-sync)**
   *
   * For any existing product, deleting it SHALL decrease
   * the total product count by exactly 1.
   */
  test("Property 5: Product Deletion Decreases List Size", () => {
    fc.assert(
      fc.property(fc.nat({ max: 100 }), () => {
        const products = mockDataStore.getAllProducts();
        if (products.length === 0) return true;

        const countBefore = products.length;
        const productToDelete = products[0];

        mockDataStore.deleteProduct(productToDelete.id);

        const countAfter = mockDataStore.getAllProducts().length;

        return countAfter === countBefore - 1;
      }),
      { numRuns: 100 }
    );
  });

  /**
   * Property 7: Inactive Products Not In Public List
   * **Validates: Requirements 3.3 (admin-mock-data-sync)**
   *
   * For any product with isActive=false, it SHALL NOT appear
   * in the public getProducts() list.
   */
  test("Property 7: Inactive Products Not In Public List", () => {
    fc.assert(
      fc.property(fc.nat({ max: 100 }), () => {
        const allProducts = mockDataStore.getAllProducts();
        if (allProducts.length === 0) return true;

        // Deactivate first product
        const product = allProducts[0];
        mockDataStore.updateProduct(product.id, {
          ...product,
          isActive: false,
        });

        // Check public list doesn't contain inactive product
        const publicProducts = mockDataStore.getProducts();

        return !publicProducts.some((p) => p.id === product.id);
      }),
      { numRuns: 100 }
    );
  });
});

describe("Poster Property-Based Tests", () => {
  /**
   * Property 1: Poster CRUD Persistence
   * **Validates: Requirements 1.1, 2.2, 3.2, 7.1, 9.1 (homepage-poster-management)**
   *
   * For any valid poster data, creating a poster SHALL persist it
   * and it SHALL be retrievable by ID.
   */
  test("Property 1: Poster CRUD Persistence", () => {
    fc.assert(
      fc.property(
        fc.record({
          title: fc
            .string({ minLength: 1, maxLength: 50 })
            .filter((s) => s.trim().length > 0),
          imageUrl: fc.webUrl(),
          linkUrl: fc.webUrl(),
          type: fc.constantFrom("slider", "promo"),
          displayOrder: fc.nat({ max: 100 }),
        }),
        (posterData) => {
          const created = mockDataStore.createPoster(posterData);
          const retrieved = mockDataStore.getPosterById(created.id);

          return (
            retrieved !== undefined &&
            retrieved.id === created.id &&
            retrieved.title === posterData.title.trim()
          );
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Property 2: Poster Validation
   * **Validates: Requirements 1.2 (homepage-poster-management)**
   *
   * For any poster with empty title or imageUrl, creation SHALL throw an error.
   */
  test("Property 2: Poster Validation - Empty Title Rejected", () => {
    fc.assert(
      fc.property(fc.constantFrom("", "   ", "\t", "\n"), (emptyTitle) => {
        try {
          mockDataStore.createPoster({
            title: emptyTitle,
            imageUrl: "https://example.com/image.jpg",
            type: "slider",
          });
          return false; // Should have thrown
        } catch (e) {
          return e.message.includes("başlık") || e.message.includes("zorunlu");
        }
      }),
      { numRuns: 10 }
    );
  });

  /**
   * Property 3: Poster Type Validation
   * **Validates: Requirements 1.4 (homepage-poster-management)**
   *
   * For any poster type not in ['slider', 'promo'], creation SHALL throw an error.
   */
  test("Property 3: Poster Type Validation", () => {
    fc.assert(
      fc.property(
        fc
          .string({ minLength: 1, maxLength: 20 })
          .filter((s) => !["slider", "promo"].includes(s)),
        (invalidType) => {
          try {
            mockDataStore.createPoster({
              title: "Test Poster",
              imageUrl: "https://example.com/image.jpg",
              type: invalidType,
            });
            return false; // Should have thrown
          } catch (e) {
            return e.message.includes("slider") || e.message.includes("promo");
          }
        }
      ),
      { numRuns: 50 }
    );
  });

  /**
   * Property 4: Edit Preserves ID
   * **Validates: Requirements 2.4 (homepage-poster-management)**
   *
   * For any poster update, the poster ID SHALL remain unchanged.
   */
  test("Property 4: Edit Preserves ID", () => {
    fc.assert(
      fc.property(
        fc.record({
          title: fc
            .string({ minLength: 1, maxLength: 50 })
            .filter((s) => s.trim().length > 0),
          displayOrder: fc.nat({ max: 100 }),
        }),
        (updateData) => {
          const posters = mockDataStore.getAllPosters();
          if (posters.length === 0) return true;

          const originalPoster = posters[0];
          const originalId = originalPoster.id;

          mockDataStore.updatePoster(originalId, {
            ...originalPoster,
            ...updateData,
          });

          const updatedPoster = mockDataStore.getPosterById(originalId);

          return updatedPoster.id === originalId;
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Property 5: Delete Removes Poster
   * **Validates: Requirements 3.2 (homepage-poster-management)**
   *
   * For any existing poster, deleting it SHALL remove it from the store.
   */
  test("Property 5: Delete Removes Poster", () => {
    fc.assert(
      fc.property(fc.nat({ max: 100 }), () => {
        const posters = mockDataStore.getAllPosters();
        if (posters.length === 0) return true;

        const posterToDelete = posters[0];
        const idToDelete = posterToDelete.id;

        mockDataStore.deletePoster(idToDelete);

        const afterDelete = mockDataStore.getPosterById(idToDelete);

        return afterDelete === undefined;
      }),
      { numRuns: 100 }
    );
  });

  /**
   * Property 7: Active Filtering
   * **Validates: Requirements 4.1, 8.2 (homepage-poster-management)**
   *
   * For any inactive poster, it SHALL NOT appear in getSliderPosters() or getPromoPosters().
   */
  test("Property 7: Active Filtering", () => {
    fc.assert(
      fc.property(fc.nat({ max: 100 }), () => {
        const posters = mockDataStore.getAllPosters();
        if (posters.length === 0) return true;

        // Deactivate first poster
        const poster = posters[0];
        mockDataStore.updatePoster(poster.id, { ...poster, isActive: false });

        // Check filtered lists
        const sliderPosters = mockDataStore.getSliderPosters();
        const promoPosters = mockDataStore.getPromoPosters();

        return (
          !sliderPosters.some((p) => p.id === poster.id) &&
          !promoPosters.some((p) => p.id === poster.id)
        );
      }),
      { numRuns: 100 }
    );
  });

  /**
   * Property 10: Display Order Sorting
   * **Validates: Requirements 5.3, 9.2, 9.3 (homepage-poster-management)**
   *
   * For any list of posters, they SHALL be sorted by displayOrder ascending.
   */
  test("Property 10: Display Order Sorting", () => {
    fc.assert(
      fc.property(fc.nat({ max: 100 }), () => {
        const sliderPosters = mockDataStore.getSliderPosters();
        const promoPosters = mockDataStore.getPromoPosters();

        const isSorted = (arr) => {
          for (let i = 1; i < arr.length; i++) {
            if (arr[i].displayOrder < arr[i - 1].displayOrder) {
              return false;
            }
            // If same displayOrder, check ID ordering
            if (
              arr[i].displayOrder === arr[i - 1].displayOrder &&
              arr[i].id < arr[i - 1].id
            ) {
              return false;
            }
          }
          return true;
        };

        return isSorted(sliderPosters) && isSorted(promoPosters);
      }),
      { numRuns: 100 }
    );
  });
});
