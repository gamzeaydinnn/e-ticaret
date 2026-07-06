import { Navigate } from "react-router-dom";
import { useAuth } from "../../contexts/AuthContext";
import { resolveAdminLandingPath } from "../../utils/adminNavigation";

const ADMIN_PANEL_ROLES = [
  "SuperAdmin",
  "Admin",
  "StoreManager",
  "CustomerSupport",
  "Logistics",
  "StoreAttendant",
  "Dispatcher",
];

export default function AdminIndex() {
  const {
    user,
    loading,
    permissions,
    hasPermission,
    hasAnyPermission,
  } = useAuth();

  if (loading) {
    return (
      <div
        className="d-flex justify-content-center align-items-center"
        style={{ minHeight: "60vh" }}
      >
        <div className="spinner-border text-primary"></div>
      </div>
    );
  }

  const isAdminUser =
    user &&
    (user.isAdmin || ADMIN_PANEL_ROLES.includes(user.role));

  if (isAdminUser) {
    return (
      <Navigate
        to={resolveAdminLandingPath({
          user,
          permissions,
          hasPermission,
          hasAnyPermission,
        })}
        replace
      />
    );
  }

  return <Navigate to="/admin/login" replace />;
}
