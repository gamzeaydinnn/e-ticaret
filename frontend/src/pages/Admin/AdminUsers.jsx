import React, { useState, useEffect } from "react";
import AdminLayout from "../../components/AdminLayout";
import { AdminService } from "../../services/adminService";

const AdminUsers = () => {
  const [users, setUsers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    const fetchUsers = async () => {
      try {
        setLoading(true);
        const usersData = await AdminService.getUsers();
        setUsers(usersData);
      } catch (err) {
        setError("Kullanıcılar yüklenirken hata oluştu");
        console.error("Kullanıcılar yükleme hatası:", err);
      } finally {
        setLoading(false);
      }
    };

    fetchUsers();
  }, []);

  if (loading) {
    return (
      <AdminLayout>
        <div
          className="d-flex justify-content-center align-items-center"
          style={{ height: "400px" }}
        >
          <div className="spinner-border text-primary" role="status">
            <span className="visually-hidden">Yükleniyor...</span>
          </div>
        </div>
      </AdminLayout>
    );
  }

  if (error) {
    return (
      <AdminLayout>
        <div className="alert alert-danger" role="alert">
          {error}
        </div>
      </AdminLayout>
    );
  }

  return (
    <AdminLayout>
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h2>Kullanıcı Yönetimi</h2>
        <button className="btn btn-success">
          <i className="fas fa-plus me-2"></i>
          Yeni Kullanıcı
        </button>
      </div>

      <div className="card">
        <div className="card-header">
          <h5 className="card-title mb-0">Kullanıcılar</h5>
        </div>
        <div className="card-body">
          <div className="table-responsive">
            <table className="table table-striped">
              <thead>
                <tr>
                  <th>ID</th>
                  <th>Ad Soyad</th>
                  <th>E-posta</th>
                  <th>Telefon</th>
                  <th>Rol</th>
                  <th>Kayıt Tarihi</th>
                  <th>Durum</th>
                  <th>İşlemler</th>
                </tr>
              </thead>
              <tbody>
                {users.map((user) => (
                  <tr key={user.id}>
                    <td>{user.id}</td>
                    <td>
                      <div className="d-flex align-items-center">
                        <div className="me-2">
                          <div
                            className="rounded-circle bg-primary d-flex align-items-center justify-content-center text-white"
                            style={{ width: "40px", height: "40px" }}
                          >
                            {user.firstName?.charAt(0)}
                            {user.lastName?.charAt(0)}
                          </div>
                        </div>
                        <div>
                          <div className="fw-bold">
                            {user.firstName} {user.lastName}
                          </div>
                          <small className="text-muted">@{user.username}</small>
                        </div>
                      </div>
                    </td>
                    <td>{user.email}</td>
                    <td>{user.phoneNumber || "-"}</td>
                    <td>
                      <span
                        className={`badge ${
                          user.role === "Admin" ? "bg-danger" : "bg-primary"
                        }`}
                      >
                        {user.role}
                      </span>
                    </td>
                    <td>
                      {new Date(user.createdAt).toLocaleDateString("tr-TR")}
                    </td>
                    <td>
                      <span
                        className={`badge ${
                          user.isActive ? "bg-success" : "bg-secondary"
                        }`}
                      >
                        {user.isActive ? "Aktif" : "Pasif"}
                      </span>
                    </td>
                    <td>
                      <div className="btn-group" role="group">
                        <button
                          className="btn btn-sm btn-outline-primary"
                          title="Düzenle"
                        >
                          <i className="fas fa-edit"></i>
                        </button>
                        <button
                          className="btn btn-sm btn-outline-info"
                          title="Detay"
                        >
                          <i className="fas fa-eye"></i>
                        </button>
                        <button
                          className="btn btn-sm btn-outline-danger"
                          title="Sil"
                        >
                          <i className="fas fa-trash"></i>
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {users.length === 0 && (
            <div className="text-center py-4">
              <i className="fas fa-users fa-3x text-muted mb-3"></i>
              <p className="text-muted">Henüz kullanıcı bulunmuyor.</p>
            </div>
          )}
        </div>
      </div>
    </AdminLayout>
  );
};

export default AdminUsers;
