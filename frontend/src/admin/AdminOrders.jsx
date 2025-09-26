import React, { useState, useEffect } from 'react';

const AdminOrders = () => {
  const [orders, setOrders] = useState([]);
  const [selectedOrder, setSelectedOrder] = useState(null);
  const [statusFilter, setStatusFilter] = useState('all');

  useEffect(() => {
    // Demo data
    setOrders([
      {
        id: 1,
        customerName: 'Ahmet Yılmaz',
        customerEmail: 'ahmet@example.com',
        total: 299.99,
        status: 'Hazırlanıyor',
        date: '2025-09-26',
        items: [
          { name: 'iPhone 15 Pro', quantity: 1, price: 299.99 }
        ]
      },
      {
        id: 2,
        customerName: 'Fatma Kaya',
        customerEmail: 'fatma@example.com',
        total: 159.50,
        status: 'Kargoda',
        date: '2025-09-25',
        items: [
          { name: 'Nike Air Max', quantity: 1, price: 159.50 }
        ]
      },
      {
        id: 3,
        customerName: 'Mehmet Demir',
        customerEmail: 'mehmet@example.com',
        total: 89.90,
        status: 'Teslim Edildi',
        date: '2025-09-24',
        items: [
          { name: 'T-Shirt', quantity: 2, price: 44.95 }
        ]
      }
    ]);
  }, []);

  const handleStatusChange = (orderId, newStatus) => {
    setOrders(orders.map(order => 
      order.id === orderId ? { ...order, status: newStatus } : order
    ));
  };

  const getStatusBadgeClass = (status) => {
    switch (status) {
      case 'Hazırlanıyor': return 'bg-info';
      case 'Kargoda': return 'bg-warning';
      case 'Teslim Edildi': return 'bg-success';
      case 'İptal Edildi': return 'bg-danger';
      default: return 'bg-secondary';
    }
  };

  const filteredOrders = statusFilter === 'all' 
    ? orders 
    : orders.filter(order => order.status === statusFilter);

  return (
    <div>
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h2 className="fw-bold text-dark">Sipariş Yönetimi</h2>
        <div className="d-flex gap-2">
          <select 
            className="form-select"
            style={{ width: '200px' }}
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value)}
          >
            <option value="all">Tüm Siparişler</option>
            <option value="Hazırlanıyor">Hazırlanıyor</option>
            <option value="Kargoda">Kargoda</option>
            <option value="Teslim Edildi">Teslim Edildi</option>
          </select>
        </div>
      </div>

      {/* Orders Table */}
      <div className="card shadow-sm border-0" style={{ borderRadius: '15px' }}>
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
                  <th>İşlemler</th>
                </tr>
              </thead>
              <tbody>
                {filteredOrders.map(order => (
                  <tr key={order.id}>
                    <td className="fw-bold">#{order.id}</td>
                    <td>
                      <div>
                        <div className="fw-bold">{order.customerName}</div>
                        <small className="text-muted">{order.customerEmail}</small>
                      </div>
                    </td>
                    <td className="fw-bold">₺{order.total}</td>
                    <td>
                      <select
                        className={`form-select form-select-sm badge ${getStatusBadgeClass(order.status)} text-white border-0`}
                        value={order.status}
                        onChange={(e) => handleStatusChange(order.id, e.target.value)}
                        style={{ width: '140px' }}
                      >
                        <option value="Hazırlanıyor">Hazırlanıyor</option>
                        <option value="Kargoda">Kargoda</option>
                        <option value="Teslim Edildi">Teslim Edildi</option>
                        <option value="İptal Edildi">İptal Edildi</option>
                      </select>
                    </td>
                    <td>{order.date}</td>
                    <td>
                      <button 
                        className="btn btn-sm btn-outline-primary"
                        onClick={() => setSelectedOrder(order)}
                      >
                        Detay
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      </div>

      {/* Order Detail Modal */}
      {selectedOrder && (
        <div className="modal fade show d-block" style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}>
          <div className="modal-dialog modal-lg">
            <div className="modal-content" style={{ borderRadius: '15px' }}>
              <div className="modal-header border-0">
                <h5 className="modal-title fw-bold">
                  Sipariş Detayı - #{selectedOrder.id}
                </h5>
                <button 
                  type="button" 
                  className="btn-close" 
                  onClick={() => setSelectedOrder(null)}
                ></button>
              </div>
              <div className="modal-body">
                <div className="row mb-4">
                  <div className="col-md-6">
                    <h6 className="fw-bold">Müşteri Bilgileri</h6>
                    <p className="mb-1"><strong>Ad:</strong> {selectedOrder.customerName}</p>
                    <p className="mb-1"><strong>E-posta:</strong> {selectedOrder.customerEmail}</p>
                    <p className="mb-0"><strong>Tarih:</strong> {selectedOrder.date}</p>
                  </div>
                  <div className="col-md-6">
                    <h6 className="fw-bold">Sipariş Bilgileri</h6>
                    <p className="mb-1"><strong>Toplam:</strong> ₺{selectedOrder.total}</p>
                    <p className="mb-0">
                      <strong>Durum:</strong> 
                      <span className={`badge ms-2 ${getStatusBadgeClass(selectedOrder.status)}`}>
                        {selectedOrder.status}
                      </span>
                    </p>
                  </div>
                </div>

                <h6 className="fw-bold mb-3">Sipariş İçeriği</h6>
                <div className="table-responsive">
                  <table className="table table-sm">
                    <thead>
                      <tr>
                        <th>Ürün</th>
                        <th>Adet</th>
                        <th>Birim Fiyat</th>
                        <th>Toplam</th>
                      </tr>
                    </thead>
                    <tbody>
                      {selectedOrder.items.map((item, index) => (
                        <tr key={index}>
                          <td>{item.name}</td>
                          <td>{item.quantity}</td>
                          <td>₺{item.price}</td>
                          <td>₺{(item.quantity * item.price).toFixed(2)}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>
              <div className="modal-footer border-0">
                <button 
                  type="button" 
                  className="btn btn-secondary" 
                  onClick={() => setSelectedOrder(null)}
                >
                  Kapat
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default AdminOrders;