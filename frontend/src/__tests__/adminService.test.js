import adminService from "../services/adminService";

// Mock fetch
global.fetch = jest.fn();

describe("AdminService", () => {
  beforeEach(() => {
    fetch.mockClear();
  });

  test("getDashboardStats returns dashboard statistics", async () => {
    const mockStats = {
      totalUsers: 150,
      totalProducts: 45,
      totalOrders: 230,
      totalRevenue: 15420.5,
    };

    fetch.mockResolvedValueOnce({
      ok: true,
      json: async () => mockStats,
    });

    const result = await adminService.getDashboardStats();

    expect(fetch).toHaveBeenCalledWith(
      "http://localhost:5153/api/admin/dashboard/stats"
    );
    expect(result).toEqual(mockStats);
  });

  test("getProducts returns products list", async () => {
    const mockProducts = [
      { id: 1, name: "Product 1", price: 100 },
      { id: 2, name: "Product 2", price: 200 },
    ];

    fetch.mockResolvedValueOnce({
      ok: true,
      json: async () => mockProducts,
    });

    const result = await adminService.getProducts();

    expect(fetch).toHaveBeenCalledWith(
      "http://localhost:5153/api/admin/products"
    );
    expect(result).toEqual(mockProducts);
  });

  test("createProduct creates new product", async () => {
    const newProduct = {
      name: "New Product",
      price: 150,
      description: "Test description",
    };

    const createdProduct = { ...newProduct, id: 3 };

    fetch.mockResolvedValueOnce({
      ok: true,
      json: async () => createdProduct,
    });

    const result = await adminService.createProduct(newProduct);

    expect(fetch).toHaveBeenCalledWith(
      "http://localhost:5153/api/admin/products",
      {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(newProduct),
      }
    );
    expect(result).toEqual(createdProduct);
  });

  test("updateOrderStatus updates order status", async () => {
    const orderId = 1;
    const newStatus = "Shipped";

    fetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({ success: true }),
    });

    const result = await adminService.updateOrderStatus(orderId, newStatus);

    expect(fetch).toHaveBeenCalledWith(
      `http://localhost:5153/api/admin/orders/${orderId}/status`,
      {
        method: "PUT",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ status: newStatus }),
      }
    );
    expect(result).toEqual({ success: true });
  });

  test("handles API errors correctly", async () => {
    fetch.mockResolvedValueOnce({
      ok: false,
      status: 404,
      statusText: "Not Found",
    });

    await expect(adminService.getDashboardStats()).rejects.toThrow(
      "HTTP error! status: 404"
    );
  });
});
