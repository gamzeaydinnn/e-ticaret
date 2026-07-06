import { render, screen, fireEvent } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import AccountHub from "../components/account/AccountHub";

const mockOpenLoginModal = jest.fn();
const mockLogout = jest.fn();

jest.mock("../contexts/LoginModalContext", () => ({
  useLoginModal: () => ({
    openLoginModal: mockOpenLoginModal,
  }),
}));

jest.mock("../contexts/AuthContext", () => ({
  useAuth: () => ({
    user: null,
    logout: mockLogout,
  }),
}));

jest.mock("../contexts/FavoriteContext", () => ({
  useFavorites: () => ({ favorites: [] }),
}));

describe("AccountHub", () => {
  beforeEach(() => {
    mockOpenLoginModal.mockClear();
    mockLogout.mockClear();
  });

  test("guest sees login and register actions", () => {
    render(
      <MemoryRouter>
        <AccountHub />
      </MemoryRouter>,
    );

    expect(screen.getByRole("button", { name: /Giriş Yap/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /Kayıt Ol/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /Sipariş Takibi/i })).toBeInTheDocument();
  });

  test("guest login button opens login modal", () => {
    render(
      <MemoryRouter>
        <AccountHub />
      </MemoryRouter>,
    );

    fireEvent.click(screen.getByRole("button", { name: /Giriş Yap/i }));
    expect(mockOpenLoginModal).toHaveBeenCalledWith("login");
  });
});
