// ==========================================================================
// WeightDifferenceSummary.jsx - Ağırlık Fark Özeti Komponenti
// ==========================================================================
// Siparişin toplam ağırlık farkını ve tutarını gösteren özet kartı.
// Kurye panelinde teslimat onaylamadan önce gösterilir.
//
// Özellikler:
// - Toplam tahmini vs gerçek ağırlık karşılaştırması
// - Fark tutarı ve yüzdesi
// - Ödeme tipine göre farklı mesajlar (kart/nakit)
// - Admin onayı gerekip gerekmediği bilgisi
// ==========================================================================

import React, { useMemo } from 'react';
import PropTypes from 'prop-types';

/**
 * WeightDifferenceSummary - Sipariş ağırlık fark özeti
 * 
 * @param {Object} summary - Backend'den gelen sipariş özeti
 * @param {string} paymentMethod - Ödeme yöntemi (Cash/Card/Online)
 * @param {boolean} loading - Yükleniyor durumu
 */
export default function WeightDifferenceSummary({ 
  summary, 
  paymentMethod = 'Cash',
  loading = false 
}) {
  // =========================================================================
  // HESAPLAMALAR
  // =========================================================================

  /**
   * Fark durumuna göre renk ve ikon
   */
  const differenceStyle = useMemo(() => {
    if (!summary) return { color: 'secondary', icon: 'fa-minus', text: 'Hesaplanıyor' };
    
    const diff = summary.totalDifference || 0;
    if (diff > 0) {
      return { 
        color: 'success', 
        icon: 'fa-arrow-up', 
        text: 'Ek Ödeme',
        bgColor: '#e8f5e9',
        borderColor: '#a5d6a7'
      };
    } else if (diff < 0) {
      return { 
        color: 'warning', 
        icon: 'fa-arrow-down', 
        text: 'İade',
        bgColor: '#fff3e0',
        borderColor: '#ffcc80'
      };
    }
    return { 
      color: 'secondary', 
      icon: 'fa-equals', 
      text: 'Fark Yok',
      bgColor: '#f5f5f5',
      borderColor: '#e0e0e0'
    };
  }, [summary]);

  /**
   * Ödeme yöntemine göre açıklama metni
   */
  const paymentExplanation = useMemo(() => {
    if (!summary) return '';
    
    const diff = summary.totalDifference || 0;
    
    if (paymentMethod === 'Cash') {
      if (diff > 0) {
        return `Müşteriden ${Math.abs(diff).toFixed(2)} ₺ ek ödeme alınacak.`;
      } else if (diff < 0) {
        return `Müşteriye ${Math.abs(diff).toFixed(2)} ₺ iade verilecek.`;
      }
      return 'Ödeme tutarında değişiklik yok.';
    } else if (paymentMethod === 'Card' || paymentMethod === 'Online') {
      if (diff > 0) {
        return `Karttan ${Math.abs(diff).toFixed(2)} ₺ ek çekim yapılacak.`;
      } else if (diff < 0) {
        return `Karta ${Math.abs(diff).toFixed(2)} ₺ iade yapılacak.`;
      }
      return 'Kart işleminde değişiklik yok.';
    }
    return '';
  }, [summary, paymentMethod]);

  /**
   * Tartılmamış ürün sayısı
   */
  const pendingCount = useMemo(() => {
    if (!summary) return 0;
    return (summary.weightBasedItemCount || 0) - (summary.weighedItemCount || 0);
  }, [summary]);

  // =========================================================================
  // LOADING STATE
  // =========================================================================
  if (loading) {
    return (
      <div 
        className="weight-summary-card p-4 rounded-3 mb-3"
        style={{ backgroundColor: '#f8f9fa', border: '1px solid #e9ecef' }}
      >
        <div className="d-flex align-items-center justify-content-center">
          <div className="spinner-border spinner-border-sm text-primary me-2"></div>
          <span className="text-muted">Fark hesaplanıyor...</span>
        </div>
      </div>
    );
  }

  if (!summary) {
    return null;
  }

  // =========================================================================
  // RENDER
  // =========================================================================
  return (
    <div 
      className="weight-summary-card p-3 rounded-3 mb-3"
      style={{ 
        backgroundColor: differenceStyle.bgColor, 
        border: `2px solid ${differenceStyle.borderColor}`,
        transition: 'all 0.3s ease'
      }}
    >
      {/* Başlık */}
      <div className="d-flex justify-content-between align-items-center mb-3">
        <h6 className="mb-0 fw-bold d-flex align-items-center">
          <i className="fas fa-balance-scale me-2" style={{ color: '#ff6b35' }}></i>
          Ağırlık Fark Özeti
        </h6>
        
        {/* Durum Badge */}
        <span className={`badge bg-${differenceStyle.color}`}>
          <i className={`fas ${differenceStyle.icon} me-1`}></i>
          {differenceStyle.text}
        </span>
      </div>

      {/* Tartılmamış ürün uyarısı */}
      {pendingCount > 0 && (
        <div 
          className="alert alert-warning d-flex align-items-center mb-3 py-2"
          style={{ borderRadius: '10px', fontSize: '14px' }}
        >
          <i className="fas fa-exclamation-triangle me-2"></i>
          <span>
            <strong>{pendingCount} ürün</strong> henüz tartılmadı!
          </span>
        </div>
      )}

      {/* Özet Bilgiler */}
      <div className="row g-2 mb-3">
        {/* Tahmini Toplam */}
        <div className="col-6">
          <div 
            className="p-2 rounded text-center"
            style={{ backgroundColor: 'rgba(255,255,255,0.7)' }}
          >
            <div className="text-muted" style={{ fontSize: '11px' }}>
              Tahmini Toplam
            </div>
            <div className="fw-bold" style={{ fontSize: '16px' }}>
              {(summary.estimatedTotal || 0).toFixed(2)} ₺
            </div>
          </div>
        </div>

        {/* Gerçek Toplam */}
        <div className="col-6">
          <div 
            className="p-2 rounded text-center"
            style={{ backgroundColor: 'rgba(255,255,255,0.7)' }}
          >
            <div className="text-muted" style={{ fontSize: '11px' }}>
              Gerçek Toplam
            </div>
            <div className="fw-bold" style={{ fontSize: '16px', color: '#ff6b35' }}>
              {(summary.actualTotal || 0).toFixed(2)} ₺
            </div>
          </div>
        </div>
      </div>

      {/* Fark Tutarı - Büyük Gösterim */}
      <div 
        className="text-center p-3 rounded-3 mb-3"
        style={{ 
          backgroundColor: 'white',
          boxShadow: '0 2px 8px rgba(0,0,0,0.1)'
        }}
      >
        <div className="text-muted mb-1" style={{ fontSize: '12px' }}>
          Toplam Fark
        </div>
        <div 
          className={`fw-bold text-${differenceStyle.color}`}
          style={{ fontSize: '28px' }}
        >
          {(summary.totalDifference || 0) >= 0 ? '+' : ''}
          {(summary.totalDifference || 0).toFixed(2)} ₺
        </div>
        {summary.differencePercent !== undefined && (
          <div className="text-muted" style={{ fontSize: '13px' }}>
            %{(summary.differencePercent || 0).toFixed(1)} 
            {summary.differencePercent >= 0 ? ' fazla' : ' az'}
          </div>
        )}
      </div>

      {/* Ödeme Açıklaması */}
      <div 
        className="d-flex align-items-start p-2 rounded"
        style={{ backgroundColor: 'rgba(255,255,255,0.7)' }}
      >
        <i 
          className={`fas ${paymentMethod === 'Cash' ? 'fa-money-bill-wave' : 'fa-credit-card'} me-2 mt-1`}
          style={{ color: '#6c757d' }}
        ></i>
        <div style={{ fontSize: '13px' }}>
          <strong>{paymentMethod === 'Cash' ? 'Nakit Ödeme' : 'Kart Ödemesi'}</strong>
          <div className="text-muted">{paymentExplanation}</div>
        </div>
      </div>

      {/* Admin Onay Uyarısı */}
      {summary.hasAdminPendingApproval && (
        <div 
          className="alert alert-info d-flex align-items-center mt-3 mb-0 py-2"
          style={{ borderRadius: '10px', fontSize: '13px' }}
        >
          <i className="fas fa-user-shield me-2"></i>
          <span>
            <strong>Admin onayı bekleniyor.</strong> Yüksek fark nedeniyle teslimat admin tarafından onaylanmalı.
          </span>
        </div>
      )}

      {/* Detay Listesi */}
      {summary.adjustments && summary.adjustments.length > 0 && (
        <details className="mt-3">
          <summary 
            className="text-muted cursor-pointer"
            style={{ fontSize: '13px', cursor: 'pointer' }}
          >
            <i className="fas fa-list me-1"></i>
            Ürün Bazlı Detaylar ({summary.adjustments.length} ürün)
          </summary>
          <div className="mt-2">
            {summary.adjustments.map((adj, index) => (
              <div 
                key={index}
                className="d-flex justify-content-between align-items-center py-2 border-bottom"
                style={{ fontSize: '12px' }}
              >
                <span className="text-truncate" style={{ maxWidth: '60%' }}>
                  {adj.productName}
                </span>
                <span className={`badge bg-${adj.differenceAmount >= 0 ? 'success' : 'warning'}`}>
                  {adj.differenceAmount >= 0 ? '+' : ''}{(adj.differenceAmount || 0).toFixed(2)} ₺
                </span>
              </div>
            ))}
          </div>
        </details>
      )}

      {/* İstatistik Satırı */}
      <div className="d-flex justify-content-between text-muted mt-3 pt-2 border-top" style={{ fontSize: '11px' }}>
        <span>
          <i className="fas fa-box me-1"></i>
          {summary.weighedItemCount}/{summary.weightBasedItemCount} tartıldı
        </span>
        <span>
          <i className="fas fa-clock me-1"></i>
          {summary.lastUpdated ? new Date(summary.lastUpdated).toLocaleTimeString('tr-TR') : '-'}
        </span>
      </div>
    </div>
  );
}

WeightDifferenceSummary.propTypes = {
  summary: PropTypes.shape({
    orderId: PropTypes.number,
    orderNumber: PropTypes.string,
    weightBasedItemCount: PropTypes.number,
    weighedItemCount: PropTypes.number,
    allItemsWeighed: PropTypes.bool,
    estimatedTotal: PropTypes.number,
    actualTotal: PropTypes.number,
    totalDifference: PropTypes.number,
    differencePercent: PropTypes.number,
    overallStatus: PropTypes.string,
    hasAdminPendingApproval: PropTypes.bool,
    adjustments: PropTypes.array,
    lastUpdated: PropTypes.string
  }),
  paymentMethod: PropTypes.oneOf(['Cash', 'Card', 'Online']),
  loading: PropTypes.bool
};
