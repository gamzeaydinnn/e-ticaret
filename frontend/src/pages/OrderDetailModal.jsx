import React from "react";

export default function OrderDetailModal({ show, onHide, order }) {
  if (!show || !order) return null;

  return (
    <div
      className="modal fade show d-block"
      tabIndex="-1"
      style={{ backgroundColor: "rgba(0,0,0,0.5)" }}
      onClick={(e) => e.target === e.currentTarget && onHide()}
    >
      <div className="modal-dialog modal-lg modal-dialog-centered">
        <div
          className="modal-content border-0 shadow-lg"
          style={{ borderRadius: "20px" }}
        >
          <div className="modal-header border-0 p-0">
            <div
              className="w-100 d-flex justify-content-between align-items-center p-3"
              style={{
                background: "linear-gradient(135deg, #ff6b35, #ff8c00)",
                borderRadius: "20px 20px 0 0",
              }}
            >
              <h5 className="modal-title text-white fw-bold mb-0">
                <i className="fas fa-info-circle me-2"></i>
                Sipariş Detayları
              </h5>
              <button
                type="button"
                className="btn btn-link text-white p-0 border-0"
                onClick={onHide}
                style={{
                  fontSize: "1.5rem",
                  textDecoration: "none",
                  opacity: 0.9,
                }}
              >
                <i className="fas fa-times"></i>
              </button>
            </div>
          </div>
          <div className="modal-body px-4 pb-4">
            <div className="row mb-3">
              <div className="col-md-6">
                <div className="mb-2">
                  <strong>Sipariş No:</strong> {order.id}
                </div>
                <div className="mb-2">
                  <strong>Tarih:</strong>{" "}
                  {order.orderDate
                    ? new Date(order.orderDate).toLocaleString()
                    : "-"}
                </div>
                <div className="mb-2">
                  <strong>Durum:</strong> {order.status}
                </div>
                <div className="mb-2">
                  <strong>Tutar:</strong> ₺{order.totalAmount}
                </div>
                <div className="mb-2">
                  <strong>Kargo:</strong> {order.shippingMethod} (
                  {order.shippingCost ? `₺${order.shippingCost}` : "-"})
                </div>
                <div className="mb-2">
                  <strong>Adres:</strong> {order.shippingAddress}
                </div>
                <div className="mb-2">
                  <strong>Müşteri:</strong> {order.customerName}{" "}
                  {order.customerPhone && `(${order.customerPhone})`}
                </div>
                {order.deliveryNotes && (
                  <div className="mb-2">
                    <strong>Not:</strong> {order.deliveryNotes}
                  </div>
                )}
              </div>
              <div className="col-md-6">
                <div className="fw-bold mb-2">Ürünler</div>
                <ul className="list-group">
                  {(order.orderItems || []).map((item, idx) => (
                    <li
                      key={idx}
                      className="list-group-item d-flex justify-content-between align-items-center"
                    >
                      <span>{item.productName}</span>
                      <span className="badge bg-secondary rounded-pill">
                        x{item.quantity}
                      </span>
                      <span>₺{item.unitPrice}</span>
                    </li>
                  ))}
                </ul>
              </div>
            </div>
            <div className="d-flex justify-content-end mt-4">
              <button
                className="btn btn-outline-secondary"
                onClick={onHide}
                style={{ borderRadius: "12px" }}
              >
                Kapat
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
