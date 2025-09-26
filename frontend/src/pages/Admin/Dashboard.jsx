import { useState, useEffect } from "react";
//import Sidebar from "../../components/Sidebar";
//import Navbar from "../../components/Navbar";
import Sidebar from "../components/Sidebar";
import Navbar from "../components/Navbar";


export default function Dashboard() {
  const [stats, setStats] = useState({
    totalUsers: 0,
    totalOrders: 0,
    totalRevenue: 0,
  });

  useEffect(() => {
    // Burada backend API çağrısı yapılacak
    // ör: fetch("http://localhost:5000/api/admin/dashboard")
    setStats({
      totalUsers: 120,
      totalOrders: 340,
      totalRevenue: 15800,
    });
  }, []);

  return (
    <div className="flex min-h-screen bg-gray-100">
      <Sidebar />

      <div className="flex-1">
        <Navbar title="Admin Dashboard" />

        <div className="p-6 grid grid-cols-1 md:grid-cols-3 gap-6">
          <div className="bg-white shadow rounded-2xl p-6">
            <h2 className="text-lg font-semibold">Kullanıcılar</h2>
            <p className="text-2xl font-bold">{stats.totalUsers}</p>
          </div>

          <div className="bg-white shadow rounded-2xl p-6">
            <h2 className="text-lg font-semibold">Siparişler</h2>
            <p className="text-2xl font-bold">{stats.totalOrders}</p>
          </div>

          <div className="bg-white shadow rounded-2xl p-6">
            <h2 className="text-lg font-semibold">Toplam Gelir</h2>
            <p className="text-2xl font-bold">₺{stats.totalRevenue}</p>
          </div>
        </div>
      </div>
    </div>
  );
}
