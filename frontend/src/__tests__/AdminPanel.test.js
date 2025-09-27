import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import "@testing-library/jest-dom";
import AdminPanel from "../admin/AdminPanel";

// Mock adminService
jest.mock("../services/adminService", () => ({
  getDashboardStats: jest.fn().mockResolvedValue({
    totalUsers: 150,
    totalProducts: 45,
    totalOrders: 230,
    totalRevenue: 15420.5,
  }),
  getProducts: jest.fn().mockResolvedValue([
    { id: 1, name: "Test Product 1", price: 100, stockQuantity: 50 },
    { id: 2, name: "Test Product 2", price: 200, stockQuantity: 30 },
  ]),
  getOrders: jest.fn().mockResolvedValue([
    {
      id: 1,
      userId: 1,
      totalPrice: 100,
      status: "Pending",
      createdDate: "2025-01-01",
    },
    {
      id: 2,
      userId: 2,
      totalPrice: 200,
      status: "Completed",
      createdDate: "2025-01-02",
    },
  ]),
  getUsers: jest.fn().mockResolvedValue([
    {
      id: 1,
      fullName: "Test User 1",
      email: "test1@example.com",
      isActive: true,
    },
    {
      id: 2,
      fullName: "Test User 2",
      email: "test2@example.com",
      isActive: false,
    },
  ]),
}));

describe("AdminPanel Component", () => {
  test("renders login form initially", () => {
    render(<AdminPanel />);

    expect(screen.getByText("Admin Giriş")).toBeInTheDocument();
    expect(screen.getByPlaceholderText("Kullanıcı Adı")).toBeInTheDocument();
    expect(screen.getByPlaceholderText("Şifre")).toBeInTheDocument();
    expect(screen.getByText("Giriş Yap")).toBeInTheDocument();
  });

  test("shows error message with invalid credentials", async () => {
    render(<AdminPanel />);

    const usernameInput = screen.getByPlaceholderText("Kullanıcı Adı");
    const passwordInput = screen.getByPlaceholderText("Şifre");
    const loginButton = screen.getByText("Giriş Yap");

    fireEvent.change(usernameInput, { target: { value: "wronguser" } });
    fireEvent.change(passwordInput, { target: { value: "wrongpass" } });
    fireEvent.click(loginButton);

    await waitFor(() => {
      expect(
        screen.getByText("Geçersiz kullanıcı adı veya şifre!")
      ).toBeInTheDocument();
    });
  });

  test("successful login shows admin dashboard", async () => {
    render(<AdminPanel />);

    const usernameInput = screen.getByPlaceholderText("Kullanıcı Adı");
    const passwordInput = screen.getByPlaceholderText("Şifre");
    const loginButton = screen.getByText("Giriş Yap");

    fireEvent.change(usernameInput, { target: { value: "admin" } });
    fireEvent.change(passwordInput, { target: { value: "admin123" } });
    fireEvent.click(loginButton);

    await waitFor(() => {
      expect(screen.getByText("Admin Dashboard")).toBeInTheDocument();
      expect(screen.getByText("Dashboard")).toBeInTheDocument();
      expect(screen.getByText("Ürünler")).toBeInTheDocument();
      expect(screen.getByText("Siparişler")).toBeInTheDocument();
      expect(screen.getByText("Kullanıcılar")).toBeInTheDocument();
    });
  });

  test("navigation tabs work correctly", async () => {
    render(<AdminPanel />);

    // Login first
    const usernameInput = screen.getByPlaceholderText("Kullanıcı Adı");
    const passwordInput = screen.getByPlaceholderText("Şifre");
    const loginButton = screen.getByText("Giriş Yap");

    fireEvent.change(usernameInput, { target: { value: "admin" } });
    fireEvent.change(passwordInput, { target: { value: "admin123" } });
    fireEvent.click(loginButton);

    await waitFor(() => {
      expect(screen.getByText("Admin Dashboard")).toBeInTheDocument();
    });

    // Test Products tab
    const productsTab = screen.getByText("Ürünler");
    fireEvent.click(productsTab);

    await waitFor(() => {
      expect(screen.getByText("Ürün Yönetimi")).toBeInTheDocument();
    });

    // Test Orders tab
    const ordersTab = screen.getByText("Siparişler");
    fireEvent.click(ordersTab);

    await waitFor(() => {
      expect(screen.getByText("Sipariş Yönetimi")).toBeInTheDocument();
    });
  });
});
