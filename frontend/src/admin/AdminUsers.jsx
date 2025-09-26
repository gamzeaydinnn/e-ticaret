import React, { useState, useEffect } from "react";

const AdminUsers = () => {
  const [users, setUsers] = useState([]);
  const [searchTerm, setSearchTerm] = useState("");

  useEffect(() => {
    // Demo data
    setUsers([
      {
        id: 1,
        name: "Ahmet Yılmaz",
        email: "ahmet@example.com",
        phone: "0532 123 45 67",
        joinDate: "2025-01-15",
        orderCount: 5,
        totalSpent: 1299.99,
        status: "active",
      },
      {
        id: 2,
        name: "Fatma Kaya",
        email: "fatma@example.com",
        phone: "0543 234 56 78",
        joinDate: "2025-02-20",
        orderCount: 3,
        totalSpent: 899.5,
        status: "active",
      },
      {
        id: 3,
        name: "Mehmet Demir",
        email: "mehmet@example.com",
        phone: "0555 345 67 89",
        joinDate: "2025-03-10",
        orderCount: 8,
        totalSpent: 2156.75,
        status: "active",
      },
    ]);
  }, []);

  const handleStatusChange = (userId, newStatus) => {
    setUsers(
      users.map((user) =>
        user.id === userId ? { ...user, status: newStatus } : user
      )
    );
  };

  const filteredUsers = users.filter(
    (user) =>
      user.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
      user.email.toLowerCase().includes(searchTerm.toLowerCase())
  );

  return (
    <div>
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h2 className="fw-bold text-dark">Kullanıcı Yönetimi</h2>
        <div className="d-flex gap-2">
          <input
            type="text"
            className="form-control"
            placeholder="Kullanıcı ara..."
            style={{ width: "250px" }}
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
          />
        </div>
      </div>

      {/* Stats Cards */}
      <div className="row mb-4">
        <div className="col-md-4">
          <div
            className="card shadow-sm border-0"
            style={{ borderRadius: "15px" }}
          >
            <div className="card-body text-center py-4">
              <div className="text-primary mb-2">
                <i className="fas fa-users fa-2x"></i>
              </div>
              <h4 className="fw-bold text-dark">{users.length}</h4>
              <p className="text-muted mb-0">Toplam Kullanıcı</p>
            </div>
          </div>
        </div>
        <div className="col-md-4">
          <div
            className="card shadow-sm border-0"
            style={{ borderRadius: "15px" }}
          >
            <div className="card-body text-center py-4">
              <div className="text-success mb-2">
                <i className="fas fa-user-check fa-2x"></i>
              </div>
              <h4 className="fw-bold text-dark">
                {users.filter((u) => u.status === "active").length}
              </h4>
              <p className="text-muted mb-0">Aktif Kullanıcı</p>
            </div>
          </div>
        </div>
        <div className="col-md-4">
          <div
            className="card shadow-sm border-0"
            style={{ borderRadius: "15px" }}
          >
            <div className="card-body text-center py-4">
              <div className="text-warning mb-2">
                <i className="fas fa-chart-line fa-2x"></i>
              </div>
              <h4 className="fw-bold text-dark">
                ₺
                {users
                  .reduce((total, user) => total + user.totalSpent, 0)
                  .toLocaleString("tr-TR")}
              </h4>
              <p className="text-muted mb-0">Toplam Harcama</p>
            </div>
          </div>
        </div>
      </div>

      {/* Users Table */}
      <div className="card shadow-sm border-0" style={{ borderRadius: "15px" }}>
        <div className="card-body">
          <div className="table-responsive">
            <table className="table table-hover">
              <thead>
                <tr>
                  <th>Kullanıcı</th>
                  <th>İletişim</th>
                  <th>Kayıt Tarihi</th>
                  <th>Sipariş Sayısı</th>
                  <th>Toplam Harcama</th>
                  <th>Durum</th>
                  <th>İşlemler</th>
                </tr>
              </thead>
              <tbody>
                {filteredUsers.map((user) => (
                  <tr key={user.id}>
                    <td>
                      <div className="d-flex align-items-center">
                        <div
                          className="rounded-circle bg-primary text-white d-flex align-items-center justify-content-center me-3"
                          style={{ width: "40px", height: "40px" }}
                        >
                          {user.name.charAt(0)}
                        </div>
                        <div>
                          <div className="fw-bold">{user.name}</div>
                          <small className="text-muted">ID: #{user.id}</small>
                        </div>
                      </div>
                    </td>
                    <td>
                      <div>
                        <div>{user.email}</div>
                        <small className="text-muted">{user.phone}</small>
                      </div>
                    </td>
                    <td>{user.joinDate}</td>
                    <td>
                      <span className="badge bg-info rounded-pill">
                        {user.orderCount} sipariş
                      </span>
                    </td>
                    <td className="fw-bold">
                      ₺{user.totalSpent.toLocaleString("tr-TR")}
                    </td>
                    <td>
                      <select
                        className={`form-select form-select-sm ${
                          user.status === "active"
                            ? "text-success"
                            : "text-danger"
                        }`}
                        value={user.status}
                        onChange={(e) =>
                          handleStatusChange(user.id, e.target.value)
                        }
                        style={{ width: "120px" }}
                      >
                        <option value="active">Aktif</option>
                        <option value="inactive">Pasif</option>
                        <option value="banned">Yasaklı</option>
                      </select>
                    </td>
                    <td>
                      <div className="dropdown">
                        <button
                          className="btn btn-sm btn-outline-secondary dropdown-toggle"
                          type="button"
                          data-bs-toggle="dropdown"
                        >
                          İşlemler
                        </button>
                        <ul className="dropdown-menu">
                          <li>
                            <button className="dropdown-item">
                              Profil Görüntüle
                            </button>
                          </li>
                          <li>
                            <button className="dropdown-item">
                              Siparişleri Görüntüle
                            </button>
                          </li>
                          <li>
                            <hr className="dropdown-divider" />
                          </li>
                          <li>
                            <button className="dropdown-item text-danger">
                              Kullanıcıyı Sil
                            </button>
                          </li>
                        </ul>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
  );
};

export default AdminUsers;
