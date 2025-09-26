import React, { useState, useEffect } from 'react';

const AdminDashboard = () => {
  const [stats, setStats] = useState({
    totalUsers: 0,
    totalProducts: 0,
    totalOrders: 0,
    todayOrders: 0,
    revenue: 0
  });
  const [recentOrders, setRecentOrders] = useState([]);

  useEffect(() => {
    // Demo data
    setStats({
      totalUsers: 156,
      totalProducts: 89,
      totalOrders: 234,
      todayOrders: 12,
      revenue: 45600
    });

    setRecentOrders([
      { id: 1, customerName: 'Ahmet Yılmaz', total: 299.99, status: 'Hazırlanıyor', date: '2025-09-26' },
      { id: 2, customerName: 'Fatma Kaya', total: 159.50, status: 'Kargoda', date: '2025-09-26' },
      { id: 3, customerName: 'Mehmet Demir', total: 89.90, status: 'Teslim Edildi', date: '2025-09-25' }
    ]);
  }, []);

  const StatCard = ({ title, value, icon, color }) => (
    <div className="col-md-3 mb-4">
      <div className="card h-100 shadow-sm border-0" style={{ borderRadius: '15px' }}>
        <div className="card-body text-center">
          <div className={`rounded-circle mx-auto mb-3 d-flex align-items-center justify-content-center`}
               style={{ width: '60px', height: '60px', backgroundColor: color }}>
            <span style={{ fontSize: '24px' }}>{icon}</span>
          </div>
          <h3 className="fw-bold text-dark">{value}</h3>
          <p className="text-muted mb-0">{title}</p>
        </div>
      </div>
    </div>
  );

  return (
    <div>
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h2 className="fw-bold text-dark">Dashboard</h2>
        <div className="text-muted">
          {new Date().toLocaleDateString('tr-TR', { 
            weekday: 'long', 
            year: 'numeric', 
            month: 'long', 
            day: 'numeric' 
          })}
        </div>
      </div>

      {/* Stats Cards */}
      <div className="row">
        <StatCard 
          title="Toplam Kullanıcı" 
          value={stats.totalUsers} 
          icon="👥" 
          color="#e3f2fd" 
        />
        <StatCard 
          title="Toplam Ürün" 
          value={stats.totalProducts} 
          icon="📦" 
          color="#fff3e0" 
        />
        <StatCard 
          title="Toplam Sipariş" 
          value={stats.totalOrders} 
          icon="🛍️" 
          color="#f3e5f5" 
        />
        <StatCard 
          title="Bugünkü Siparişler" 
          value={stats.todayOrders} 
          icon="📈" 
          color="#e8f5e8" 
        />
      </div>

      {/* Revenue Card */}
      <div className="row mb-4">
        <div className="col-12">
          <div className="card shadow-sm border-0" 
               style={{ 
                 borderRadius: '15px',
                 background: 'linear-gradient(135deg, #ff6f00 0%, #ff8f00 100%)'
               }}>
            <div className="card-body text-white text-center py-4">
              <h3 className="fw-bold mb-1">₺{stats.revenue.toLocaleString('tr-TR')}</h3>
              <p className="mb-0 opacity-75">Toplam Ciro</p>
            </div>
          </div>
        </div>
      </div>

      {/* Recent Orders */}
      <div className="card shadow-sm border-0" style={{ borderRadius: '15px' }}>
        <div className="card-header bg-white border-0 py-3">
          <h5 className="fw-bold mb-0">Son Siparişler</h5>
        </div>
        <div className="card-body">
          <div className="table-responsive">
            <table className="table table-hover">
              <thead>
                <tr>
                  <th>Sipariş No</th>
                  <th>Müşteri</th>
                  <th>Tutar</th>
                  <th>Durum</th>
                  <th>Tarih</th>
                </tr>
              </thead>
              <tbody>
                {recentOrders.map(order => (
                  <tr key={order.id}>
                    <td>#{order.id}</td>
                    <td>{order.customerName}</td>
                    <td>₺{order.total}</td>
                    <td>
                      <span className={`badge rounded-pill px-3 py-2 ${
                        order.status === 'Teslim Edildi' ? 'bg-success' :
                        order.status === 'Kargoda' ? 'bg-warning' : 'bg-info'
                      }`}>
                        {order.status}
                      </span>
                    </td>
                    <td>{order.date}</td>
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

export default AdminDashboard;