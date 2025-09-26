import React, { useState, useEffect } from 'react';
import { getUserOrders } from '../services/orderService';

const OrderTracking = () => {
  const [orders, setOrders] = useState([]);
  const [loading, setLoading] = useState(true);
  const [selectedOrder, setSelectedOrder] = useState(null);
  const [trackingCode, setTrackingCode] = useState('');

  useEffect(() => {
    loadOrders();
  }, []);

  const loadOrders = async () => {
    try {
      const userOrders = await getUserOrders();
      setOrders(userOrders || []);
    } catch (error) {
      console.error('Siparişler yüklenemedi:', error);
    } finally {
      setLoading(false);
    }
  };

  const getOrderStatusInfo = (status) => {
    const statusMap = {
      'pending': { color: '#ffc107', icon: 'fa-clock', text: 'Beklemede' },
      'confirmed': { color: '#17a2b8', icon: 'fa-check-circle', text: 'Onaylandı' },
      'preparing': { color: '#fd7e14', icon: 'fa-box', text: 'Hazırlanıyor' },
      'shipped': { color: '#6f42c1', icon: 'fa-truck', text: 'Kargoda' },
      'delivered': { color: '#28a745', icon: 'fa-check-double', text: 'Teslim Edildi' },
      'cancelled': { color: '#dc3545', icon: 'fa-times-circle', text: 'İptal Edildi' }
    };
    return statusMap[status] || statusMap['pending'];
  };

  const trackOrderByCode = () => {
    const order = orders.find(o => o.trackingCode === trackingCode);
    if (order) {
      setSelectedOrder(order);
    } else {
      alert('Sipariş bulunamadı! Lütfen takip kodunu kontrol edin.');
    }
  };

  if (loading) {
    return (
      <div className="text-center py-5">
        <div 
          className="spinner-border mb-3" 
          role="status"
          style={{ color: '#ff8f00', width: '3rem', height: '3rem' }}
        >
          <span className="visually-hidden">Loading...</span>
        </div>
        <p className="text-muted fw-bold">Siparişleriniz yükleniyor...</p>
      </div>
    );
  }

  return (
    <div 
      style={{ 
        minHeight: '100vh', 
        background: 'linear-gradient(135deg, #fff3e0 0%, #ffe0b2 50%, #ffcc80 100%)',
        paddingTop: '2rem',
        paddingBottom: '2rem'
      }}
    >
      <div className="container">
        {/* Takip Kodu ile Arama */}
        <div className="card shadow-lg border-0 mb-4" style={{ borderRadius: '20px' }}>
          <div 
            className="card-header text-white border-0"
            style={{ 
              background: 'linear-gradient(45deg, #ff6f00, #ff8f00, #ffa000)',
              borderTopLeftRadius: '20px',
              borderTopRightRadius: '20px',
              padding: '1.5rem'
            }}
          >
            <h4 className="mb-0 fw-bold">
              <i className="fas fa-search me-2"></i>Sipariş Takibi
            </h4>
          </div>
          <div className="card-body" style={{ padding: '2rem' }}>
            <div className="row">
              <div className="col-md-8">
                <input
                  type="text"
                  className="form-control form-control-lg border-0 shadow-sm"
                  style={{ 
                    backgroundColor: '#fff8f0',
                    borderRadius: '15px',
                    padding: '1rem 1.5rem'
                  }}
                  placeholder="Takip kodunuzu girin (örn: TK123456789)"
                  value={trackingCode}
                  onChange={(e) => setTrackingCode(e.target.value)}
                />
              </div>
              <div className="col-md-4">
                <button
                  className="btn btn-lg w-100 text-white fw-bold shadow-lg border-0"
                  style={{ 
                    background: 'linear-gradient(45deg, #ff6f00, #ff8f00, #ffa000)',
                    borderRadius: '15px',
                    padding: '1rem'
                  }}
                  onClick={trackOrderByCode}
                >
                  <i className="fas fa-search me-2"></i>Takip Et
                </button>
              </div>
            </div>
          </div>
        </div>

        {/* Seçilen Sipariş Detayı */}
        {selectedOrder && (
          <div className="card shadow-lg border-0 mb-4" style={{ borderRadius: '20px' }}>
            <div 
              className="card-header text-white border-0"
              style={{ 
                background: 'linear-gradient(45deg, #17a2b8, #20c997)',
                borderTopLeftRadius: '20px',
                borderTopRightRadius: '20px',
                padding: '1.5rem'
              }}
            >
              <h5 className="mb-0 fw-bold">
                <i className="fas fa-package me-2"></i>
                Sipariş #{selectedOrder.orderNumber}
              </h5>
            </div>
            <div className="card-body" style={{ padding: '2rem' }}>
              <OrderTrackingTimeline order={selectedOrder} />
            </div>
          </div>
        )}

        {/* Tüm Siparişler */}
        <div className="card shadow-lg border-0" style={{ borderRadius: '20px' }}>
          <div 
            className="card-header text-white border-0"
            style={{ 
              background: 'linear-gradient(45deg, #6f42c1, #e83e8c)',
              borderTopLeftRadius: '20px',
              borderTopRightRadius: '20px',
              padding: '1.5rem'
            }}
          >
            <h4 className="mb-0 fw-bold">
              <i className="fas fa-list me-2"></i>Tüm Siparişlerim
            </h4>
          </div>
          <div className="card-body" style={{ padding: '2rem' }}>
            {orders.length === 0 ? (
              <div className="text-center py-5">
                <div 
                  className="p-4 rounded-circle mx-auto mb-4 shadow-lg"
                  style={{ 
                    backgroundColor: '#fff8f0',
                    width: '120px',
                    height: '120px',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center'
                  }}
                >
                  <i className="fas fa-shopping-bag text-warning" style={{ fontSize: '3rem' }}></i>
                </div>
                <h4 className="text-warning fw-bold mb-3">Henüz Siparişiniz Yok</h4>
                <p className="text-muted fs-5">İlk siparişinizi vermek için alışverişe başlayın!</p>
              </div>
            ) : (
              <div className="row">
                {orders.map(order => {
                  const statusInfo = getOrderStatusInfo(order.status);
                  return (
                    <div key={order.id} className="col-md-6 mb-4">
                      <div 
                        className="card shadow-sm border-0 h-100"
                        style={{ borderRadius: '15px' }}
                      >
                        <div className="card-body" style={{ padding: '1.5rem' }}>
                          <div className="d-flex justify-content-between align-items-start mb-3">
                            <h6 className="fw-bold mb-0">
                              Sipariş #{order.orderNumber}
                            </h6>
                            <span 
                              className="badge px-3 py-2"
                              style={{ 
                                backgroundColor: statusInfo.color,
                                borderRadius: '20px'
                              }}
                            >
                              <i className={`fas ${statusInfo.icon} me-1`}></i>
                              {statusInfo.text}
                            </span>
                          </div>
                          
                          <p className="text-muted mb-2">
                            <i className="fas fa-calendar me-2"></i>
                            {new Date(order.orderDate).toLocaleDateString('tr-TR')}
                          </p>
                          
                          <p className="text-muted mb-2">
                            <i className="fas fa-barcode me-2"></i>
                            Takip: {order.trackingCode}
                          </p>
                          
                          <p className="fw-bold text-warning mb-3">
                            <i className="fas fa-tag me-2"></i>
                            ₺{Number(order.totalAmount).toFixed(2)}
                          </p>
                          
                          <button
                            className="btn btn-outline-warning btn-sm fw-bold w-100"
                            style={{ borderRadius: '15px' }}
                            onClick={() => setSelectedOrder(order)}
                          >
                            <i className="fas fa-eye me-2"></i>
                            Detayları Görüntüle
                          </button>
                        </div>
                      </div>
                    </div>
                  );
                })}
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};

// Sipariş Takip Timeline Component
const OrderTrackingTimeline = ({ order }) => {
  const getTimelineSteps = () => {
    return [
      { 
        key: 'pending', 
        title: 'Sipariş Alındı', 
        description: 'Siparişiniz başarıyla oluşturuldu',
        completed: true 
      },
      { 
        key: 'confirmed', 
        title: 'Sipariş Onaylandı', 
        description: 'Siparişiniz mağaza tarafından onaylandı',
        completed: ['confirmed', 'preparing', 'shipped', 'delivered'].includes(order.status)
      },
      { 
        key: 'preparing', 
        title: 'Hazırlanıyor', 
        description: 'Siparişiniz hazırlanıyor ve paketleniyor',
        completed: ['preparing', 'shipped', 'delivered'].includes(order.status)
      },
      { 
        key: 'shipped', 
        title: 'Kargoya Verildi', 
        description: 'Siparişiniz kargo firmasına teslim edildi',
        completed: ['shipped', 'delivered'].includes(order.status)
      },
      { 
        key: 'delivered', 
        title: 'Teslim Edildi', 
        description: 'Siparişiniz başarıyla teslim edildi',
        completed: order.status === 'delivered'
      }
    ];
  };

  const timelineSteps = getTimelineSteps();

  return (
    <div>
      <div className="row mb-4">
        <div className="col-md-6">
          <h6 className="text-warning fw-bold">Sipariş Bilgileri</h6>
          <p className="mb-1"><strong>Sipariş No:</strong> {order.orderNumber}</p>
          <p className="mb-1"><strong>Takip Kodu:</strong> {order.trackingCode}</p>
          <p className="mb-1"><strong>Toplam Tutar:</strong> ₺{Number(order.totalAmount).toFixed(2)}</p>
          <p className="mb-1"><strong>Sipariş Tarihi:</strong> {new Date(order.orderDate).toLocaleDateString('tr-TR')}</p>
        </div>
        <div className="col-md-6">
          <h6 className="text-warning fw-bold">Teslimat Bilgileri</h6>
          <p className="mb-1"><strong>Adres:</strong> {order.deliveryAddress}</p>
          <p className="mb-1"><strong>Kargo Firması:</strong> {order.shippingCompany || 'Yurtiçi Kargo'}</p>
          {order.estimatedDeliveryDate && (
            <p className="mb-1">
              <strong>Tahmini Teslimat:</strong> {new Date(order.estimatedDeliveryDate).toLocaleDateString('tr-TR')}
            </p>
          )}
        </div>
      </div>

      <h6 className="text-warning fw-bold mb-4">Sipariş Durumu</h6>
      
      <div className="timeline">
        {timelineSteps.map((step, index) => (
          <div key={step.key} className="timeline-item d-flex align-items-start mb-4">
            <div 
              className={`timeline-marker rounded-circle d-flex align-items-center justify-content-center me-3 ${
                step.completed ? 'bg-success' : 'bg-light'
              }`}
              style={{ width: '40px', height: '40px', minWidth: '40px' }}
            >
              {step.completed ? (
                <i className="fas fa-check text-white"></i>
              ) : (
                <span 
                  className="rounded-circle bg-warning"
                  style={{ width: '12px', height: '12px' }}
                ></span>
              )}
            </div>
            
            <div className="timeline-content">
              <h6 className={`fw-bold mb-1 ${step.completed ? 'text-success' : 'text-muted'}`}>
                {step.title}
              </h6>
              <p className={`mb-2 small ${step.completed ? 'text-dark' : 'text-muted'}`}>
                {step.description}
              </p>
              {step.completed && step.key === order.status && (
                <small className="text-warning fw-bold">
                  <i className="fas fa-clock me-1"></i>Güncel durum
                </small>
              )}
            </div>
            
            {index < timelineSteps.length - 1 && (
              <div 
                className={`timeline-line ${step.completed ? 'bg-success' : 'bg-light'}`}
                style={{ 
                  position: 'absolute',
                  left: '19px',
                  top: '40px',
                  width: '2px',
                  height: '60px',
                  marginLeft: '1rem'
                }}
              ></div>
            )}
          </div>
        ))}
      </div>

      {/* Sipariş Ürünleri */}
      {order.items && order.items.length > 0 && (
        <div className="mt-4">
          <h6 className="text-warning fw-bold mb-3">Sipariş Ürünleri</h6>
          <div className="row">
            {order.items.map(item => (
              <div key={item.id} className="col-md-6 mb-3">
                <div 
                  className="card border-0 shadow-sm"
                  style={{ borderRadius: '10px' }}
                >
                  <div className="card-body p-3">
                    <div className="d-flex align-items-center">
                      <div 
                        className="me-3 d-flex align-items-center justify-content-center"
                        style={{ 
                          width: '60px', 
                          height: '60px',
                          backgroundColor: '#fff8f0',
                          borderRadius: '10px'
                        }}
                      >
                        📦
                      </div>
                      <div className="flex-grow-1">
                        <h6 className="mb-1 fw-bold">{item.productName}</h6>
                        <p className="mb-1 text-muted small">
                          Adet: {item.quantity} x ₺{Number(item.unitPrice).toFixed(2)}
                        </p>
                        <p className="mb-0 fw-bold text-warning">
                          ₺{Number(item.quantity * item.unitPrice).toFixed(2)}
                        </p>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
};

export default OrderTracking;