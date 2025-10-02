import React, { useEffect, useState } from "react";
import Sidebar from "../components/Sidebar";
import Navbar from "../components/Navbar";

const Users = () => {
  const [users, setUsers] = useState([]);
  const [loading, setLoading] = useState(true);

  // Kullanıcıları backend'den çekme (örnek API)
  const fetchUsers = async () => {
  const token = localStorage.getItem("token"); // token al
  try {
    const response = await fetch("/api/admin/users", {
      headers: {
        Authorization: `Bearer ${token}`
      }
    });
    const data = await response.json();
    setUsers(data);
  } catch (error) {
    console.error("Kullanıcıları çekerken hata:", error);
  } finally {
    setLoading(false);
  }
};


  useEffect(() => {
    fetchUsers();
  }, []);

  if (loading) return <p>Yükleniyor...</p>;

  return (
    <div className="p-4">
      <h1 className="text-2xl font-bold mb-4">Kullanıcılar</h1>
      <table className="min-w-full bg-white border">
        <thead>
          <tr>
            <th className="py-2 px-4 border">ID</th>
            <th className="py-2 px-4 border">Ad</th>
            <th className="py-2 px-4 border">Email</th>
            <th className="py-2 px-4 border">İşlemler</th>
          </tr>
        </thead>
        <tbody>
          {users.map((user) => (
            <tr key={user.id}>
              <td className="py-2 px-4 border">{user.id}</td>
              <td className="py-2 px-4 border">{user.name}</td>
              <td className="py-2 px-4 border">{user.email}</td>
              <td className="py-2 px-4 border">
                <button className="bg-blue-500 text-white px-3 py-1 rounded mr-2">
                  Düzenle
                </button>
                <button className="bg-red-500 text-white px-3 py-1 rounded">
                  Sil
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
};

export default Users;
