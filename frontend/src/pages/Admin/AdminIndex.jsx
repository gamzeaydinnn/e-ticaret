import { useEffect } from "react";
import { Navigate } from "react-router-dom";
import { useAuth } from "../../contexts/AuthContext";

export default function AdminIndex() {
  const { user, loading, setUser } = useAuth();

  useEffect(() => {
    if (!user) {
      try {
        const stored = localStorage.getItem("user");
        const token = localStorage.getItem("authToken") || localStorage.getItem("adminToken") || localStorage.getItem("token");
        if (stored && token) {
          const parsed = JSON.parse(stored);
          if (parsed && (parsed.isAdmin || parsed.role === "Admin")) {
            setUser?.(parsed);
          }
        }
      } catch {}
    }
  }, [user, setUser]);

  if (loading) {
    return (
      <div className="d-flex justify-content-center align-items-center" style={{ minHeight: "60vh" }}>
        <div className="spinner-border text-primary"></div>
      </div>
    );
  }

  if (user && (user.isAdmin || user.role === "Admin")) {
    return <Navigate to="/admin/dashboard" replace />;
  }

  return <Navigate to="/admin/login" replace />;
}

