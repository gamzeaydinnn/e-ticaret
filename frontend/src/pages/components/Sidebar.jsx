import { Link } from "react-router-dom";

export default function Sidebar() {
  return (
    <div className="w-64 bg-white shadow-md min-h-screen p-6">
      <h1 className="text-2xl font-bold mb-8">Admin Panel</h1>
      <nav className="flex flex-col space-y-4">
        <Link to="/admin/dashboard" className="hover:text-blue-600">Dashboard</Link>
        <Link to="/admin/products" className="hover:text-blue-600">Ürünler</Link>
        <Link to="/admin/orders" className="hover:text-blue-600">Siparişler</Link>
        <Link to="/admin/users" className="hover:text-blue-600">Kullanıcılar</Link>
        <Link to="/admin/micro" className="hover:text-blue-600">Micro ERP</Link> {/* Yeni link */}
      </nav>
    </div>
  );
}
