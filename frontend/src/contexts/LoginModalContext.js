import React, {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useState,
} from "react";
import LoginModal from "../components/LoginModal";

const LoginModalContext = createContext(null);

export function LoginModalProvider({ children }) {
  const [show, setShow] = useState(false);
  const [initialMode, setInitialMode] = useState("login");

  const openLoginModal = useCallback((mode = "login") => {
    setInitialMode(mode === "register" ? "register" : "login");
    setShow(true);
  }, []);

  const closeLoginModal = useCallback(() => {
    setShow(false);
  }, []);

  const value = useMemo(
    () => ({ openLoginModal, closeLoginModal }),
    [openLoginModal, closeLoginModal],
  );

  return (
    <LoginModalContext.Provider value={value}>
      {children}
      <LoginModal
        show={show}
        onHide={closeLoginModal}
        onLoginSuccess={closeLoginModal}
        initialMode={initialMode}
      />
    </LoginModalContext.Provider>
  );
}

export function useLoginModal() {
  const context = useContext(LoginModalContext);
  if (!context) {
    throw new Error("useLoginModal must be used within LoginModalProvider");
  }
  return context;
}

export default LoginModalContext;
