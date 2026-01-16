/**
 * NewsletterForm Unit Tests
 *
 * Newsletter abonelik formu için unit testler.
 *
 * Requirements: 3.2, 3.3, 3.5, 3.6
 */

import React from "react";
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import NewsletterForm from "../components/NewsletterForm";

// LocalStorage mock
const localStorageMock = {
  getItem: jest.fn(),
  setItem: jest.fn(),
  removeItem: jest.fn(),
  clear: jest.fn(),
};
Object.defineProperty(window, "localStorage", { value: localStorageMock });

describe("NewsletterForm", () => {
  beforeEach(() => {
    jest.clearAllMocks();
    localStorageMock.getItem.mockReturnValue(null);
  });

  /**
   * Render testi - Bileşenin doğru şekilde render edildiğini doğrular
   */
  describe("Render Tests", () => {
    test("renders newsletter form", () => {
      render(<NewsletterForm />);

      expect(screen.getByText("Bültenimize Abone Ol")).toBeInTheDocument();
    });

    test("renders email input field", () => {
      render(<NewsletterForm />);

      expect(
        screen.getByPlaceholderText("E-posta adresiniz")
      ).toBeInTheDocument();
    });

    test("renders submit button", () => {
      render(<NewsletterForm />);

      expect(screen.getByText("Abone Ol")).toBeInTheDocument();
    });

    test("renders privacy note", () => {
      render(<NewsletterForm />);

      expect(screen.getByText(/E-posta adresiniz güvende/)).toBeInTheDocument();
    });

    test("renders subtitle text", () => {
      render(<NewsletterForm />);

      expect(
        screen.getByText(/Kampanya ve güncellemelerden ilk siz haberdar olun/)
      ).toBeInTheDocument();
    });
  });

  /**
   * E-posta validasyon testi
   */
  describe("Email Validation Tests", () => {
    test("shows error for empty email", async () => {
      render(<NewsletterForm />);

      const submitButton = screen.getByText("Abone Ol");
      fireEvent.click(submitButton);

      await waitFor(() => {
        expect(
          screen.getByText("Lütfen e-posta adresinizi girin")
        ).toBeInTheDocument();
      });
    });

    test("shows error for invalid email format", async () => {
      render(<NewsletterForm />);

      const input = screen.getByPlaceholderText("E-posta adresiniz");
      await userEvent.type(input, "invalid-email");

      const submitButton = screen.getByText("Abone Ol");
      fireEvent.click(submitButton);

      await waitFor(() => {
        expect(
          screen.getByText("Geçerli bir e-posta adresi girin")
        ).toBeInTheDocument();
      });
    });

    test("shows error for email without domain", async () => {
      render(<NewsletterForm />);

      const input = screen.getByPlaceholderText("E-posta adresiniz");
      await userEvent.type(input, "test@");

      const submitButton = screen.getByText("Abone Ol");
      fireEvent.click(submitButton);

      await waitFor(() => {
        expect(
          screen.getByText("Geçerli bir e-posta adresi girin")
        ).toBeInTheDocument();
      });
    });

    test("shows error for email without @ symbol", async () => {
      render(<NewsletterForm />);

      const input = screen.getByPlaceholderText("E-posta adresiniz");
      await userEvent.type(input, "testexample.com");

      const submitButton = screen.getByText("Abone Ol");
      fireEvent.click(submitButton);

      await waitFor(() => {
        expect(
          screen.getByText("Geçerli bir e-posta adresi girin")
        ).toBeInTheDocument();
      });
    });

    test("clears error when user starts typing after error", async () => {
      render(<NewsletterForm />);

      // First trigger an error
      const submitButton = screen.getByText("Abone Ol");
      fireEvent.click(submitButton);

      await waitFor(() => {
        expect(
          screen.getByText("Lütfen e-posta adresinizi girin")
        ).toBeInTheDocument();
      });

      // Then start typing
      const input = screen.getByPlaceholderText("E-posta adresiniz");
      await userEvent.type(input, "t");

      await waitFor(() => {
        expect(
          screen.queryByText("Lütfen e-posta adresinizi girin")
        ).not.toBeInTheDocument();
      });
    });
  });

  /**
   * Form submission testi
   */
  describe("Form Submission Tests", () => {
    test("shows success message for valid email", async () => {
      render(<NewsletterForm />);

      const input = screen.getByPlaceholderText("E-posta adresiniz");
      await userEvent.type(input, "test@example.com");

      const submitButton = screen.getByText("Abone Ol");
      fireEvent.click(submitButton);

      await waitFor(
        () => {
          expect(
            screen.getByText("Bültenimize başarıyla abone oldunuz!")
          ).toBeInTheDocument();
        },
        { timeout: 2000 }
      );
    });

    test("saves subscription to localStorage on success", async () => {
      render(<NewsletterForm />);

      const input = screen.getByPlaceholderText("E-posta adresiniz");
      await userEvent.type(input, "test@example.com");

      const submitButton = screen.getByText("Abone Ol");
      fireEvent.click(submitButton);

      await waitFor(
        () => {
          expect(localStorageMock.setItem).toHaveBeenCalled();
        },
        { timeout: 2000 }
      );
    });

    test("clears input after successful submission", async () => {
      render(<NewsletterForm />);

      const input = screen.getByPlaceholderText("E-posta adresiniz");
      await userEvent.type(input, "test@example.com");

      const submitButton = screen.getByText("Abone Ol");
      fireEvent.click(submitButton);

      await waitFor(
        () => {
          expect(input).toHaveValue("");
        },
        { timeout: 2000 }
      );
    });

    test("shows loading state during submission", async () => {
      render(<NewsletterForm />);

      const input = screen.getByPlaceholderText("E-posta adresiniz");
      await userEvent.type(input, "test@example.com");

      const submitButton = screen.getByText("Abone Ol");
      fireEvent.click(submitButton);

      // Should show spinner during loading
      expect(screen.getByRole("button", { name: /abone ol/i })).toBeDisabled();
    });

    test("disables input during submission", async () => {
      render(<NewsletterForm />);

      const input = screen.getByPlaceholderText("E-posta adresiniz");
      await userEvent.type(input, "test@example.com");

      const submitButton = screen.getByText("Abone Ol");
      fireEvent.click(submitButton);

      expect(input).toBeDisabled();
    });
  });

  /**
   * Already subscribed state tests
   */
  describe("Already Subscribed Tests", () => {
    test("shows already subscribed message when localStorage has subscription", () => {
      localStorageMock.getItem.mockReturnValue(
        JSON.stringify({
          email: "test@example.com",
          subscribedAt: new Date().toISOString(),
        })
      );

      render(<NewsletterForm />);

      expect(screen.getByText("Zaten Abonesiniz!")).toBeInTheDocument();
    });

    test("does not show form when already subscribed", () => {
      localStorageMock.getItem.mockReturnValue(
        JSON.stringify({
          email: "test@example.com",
          subscribedAt: new Date().toISOString(),
        })
      );

      render(<NewsletterForm />);

      expect(
        screen.queryByPlaceholderText("E-posta adresiniz")
      ).not.toBeInTheDocument();
    });
  });

  /**
   * Accessibility Tests
   */
  describe("Accessibility Tests", () => {
    test("input has proper aria-label", () => {
      render(<NewsletterForm />);

      expect(screen.getByLabelText("E-posta adresi")).toBeInTheDocument();
    });

    test("button has proper aria-label", () => {
      render(<NewsletterForm />);

      expect(screen.getByLabelText("Abone ol")).toBeInTheDocument();
    });

    test('error message has role="alert"', async () => {
      render(<NewsletterForm />);

      const submitButton = screen.getByText("Abone Ol");
      fireEvent.click(submitButton);

      await waitFor(() => {
        expect(screen.getByRole("alert")).toBeInTheDocument();
      });
    });

    test("input has aria-invalid when error", async () => {
      render(<NewsletterForm />);

      const submitButton = screen.getByText("Abone Ol");
      fireEvent.click(submitButton);

      await waitFor(() => {
        const input = screen.getByPlaceholderText("E-posta adresiniz");
        expect(input).toHaveAttribute("aria-invalid", "true");
      });
    });
  });
});
