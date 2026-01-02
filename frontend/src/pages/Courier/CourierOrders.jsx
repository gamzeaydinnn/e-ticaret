import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import WeightApprovalWarningModal from "../../components/WeightApprovalWarningModal";
import { CourierService } from "../../services/courierService";
import "./CourierOrders.css";

export default function CourierOrders() {
  const [orders, setOrders] = useState([]);
  const [loading, setLoading] = useState(true);
  const [selectedOrder, setSelectedOrder] = useState(null);
  const [updating, setUpdating] = useState(false);
  const [weightReports, setWeightReports] = useState({});
  const [showWeightModal, setShowWeightModal] = useState(false);
  const [pendingDeliveryOrder, setPendingDeliveryOrder] = useState(null);
  const navigate = useNavigate();

  useEffect(() => {
    const courierData = localStorage.getItem("courierData");
    if (!courierData) {
      navigate("/courier/login");
      return;
    }

    const courier = JSON.parse(courierData);
    loadOrders(courier.id);
  }, [navigate]);

  const loadOrders = async (courierId) => {
    try {
      const orderData = await CourierService.getAssignedOrders(courierId);
      setOrders(orderData);

      // Her sipariş için ağırlık raporlarını yükle
      const reportsMap = {};
      for (const order of orderData) {
        try {
          const reports = await CourierService.getOrderWeightReports(order.id);
          if (reports && reports.length > 0) {
            reportsMap[order.id] = reports[0]; // İlk raporu al
          }
        } catch (error) {
          console.error(
            `Sipariş ${order.id} için ağırlık raporu yüklenemedi:`,
            error
          );
        }
      }
      setWeightReports(reportsMap);
    } catch (error) {
      console.error("Sipariş yükleme hatası:", error);
    } finally {
      setLoading(false);
    }
  };

  const handleDeliveryAttempt = (order) => {
    const report = weightReports[order.id];

    // Ağırlık raporu varsa ve onay bekleniyorsa uyarı göster
    if (report && report.status === "Pending") {
      setPendingDeliveryOrder(order);
      setShowWeightModal(true);
      return;
    }

    // Onaylı rapor varsa veya rapor yoksa modal ile bilgilendirip onayla
    setPendingDeliveryOrder(order);
    setShowWeightModal(true);
  };

  const confirmDelivery = async () => {
    if (!pendingDeliveryOrder) return;

    setUpdating(true);
    setShowWeightModal(false);

    try {
      const orderId = pendingDeliveryOrder.id;

      // Backend'e teslimat isteği gönder - ödeme otomatik yapılacak
      const response = await CourierService.updateOrderStatus(
        orderId,
        "delivered"
      );

      if (response.success) {
        // Başarılı yanıt
        let message = "✅ Teslimat Başarıyla Tamamlandı!\n\n";
        message += `Sipariş: #${orderId}\n`;
        message += `Müşteri: ${pendingDeliveryOrder.customerName}\n`;
        message += `Tutar: ${pendingDeliveryOrder.totalAmount.toFixed(2)} ₺\n`;

        if (response.paymentProcessed && response.paymentAmount > 0) {
          message += `\n💰 EK ÖDEME TAHSİLATI:\n`;
          message += `Ağırlık Farkı Ücreti: +${response.paymentAmount.toFixed(
            2
          )} ₺\n`;
          message += `\n📊 Toplam Tahsilat: ${(
            parseFloat(pendingDeliveryOrder.totalAmount) +
            parseFloat(response.paymentAmount)
          ).toFixed(2)} ₺`;

          if (response.paymentDetails && response.paymentDetails.length > 0) {
            message += `\n\nDetaylar:\n${response.paymentDetails.join("\n")}`;
          }
        }

        message += response.message ? `\n\n${response.message}` : "";

        alert(message);
      } else {
        alert(
          `⚠️ Uyarı!\n\n${
            response.message ||
            "Teslimat tamamlandı ancak bazı ödemeler başarısız oldu."
          }`
        );
      }

      // Siparişleri yeniden yükle
      const courierData = JSON.parse(localStorage.getItem("courierData"));
      await loadOrders(courierData.id);

      setSelectedOrder(null);
      setPendingDeliveryOrder(null);
    } catch (error) {
      console.error("Teslimat hatası:", error);
      alert(
        `❌ Teslimat Hatası!\n\n${
          error.response?.data?.message ||
          error.message ||
          "Bilinmeyen bir hata oluştu"
        }`
      );
    } finally {
      setUpdating(false);
    }
  };

  const updateOrderStatus = async (orderId, newStatus, notes = "") => {
    // Teslim durumu için özel kontrol
    if (newStatus === "delivered") {
      const order = orders.find((o) => o.id === orderId);
      if (order) {
        handleDeliveryAttempt(order);
        return;
      }
    }

    // Diğer durum güncellemeleri normal devam eder
    setUpdating(true);
    try {
      await CourierService.updateOrderStatus(orderId, newStatus, notes);

      const courierData = JSON.parse(localStorage.getItem("courierData"));
      await loadOrders(courierData.id);

      setSelectedOrder(null);
    } catch (error) {
      console.error("Durum güncelleme hatası:", error);
    } finally {
      setUpdating(false);
    }
  };

  const getStatusText = (status) => {
    const statusMap = {
      preparing: "Hazırlanıyor",
      ready: "Teslim Alınmaya Hazır",
      picked_up: "Teslim Alındı",
      in_transit: "Yolda",
      delivered: "Teslim Edildi",
    };
    return statusMap[status] || status;
  };

  const getStatusColor = (status) => {
    const colorMap = {
      preparing: "warning",
      ready: "info",
      picked_up: "primary",
      in_transit: "success",
      delivered: "secondary",
    };
    return colorMap[status] || "secondary";
  };

  const getNextStatus = (currentStatus) => {
    const statusFlow = {
      preparing: null, // Hazırlanıyor durumunda kurye bekler
      ready: "picked_up", // Hazır -> Teslim Al
      picked_up: "in_transit", // Teslim Alındı -> Yola Çık
      in_transit: "delivered", // Yolda -> Teslim Et
      delivered: null, // Son durum
    };
    return statusFlow[currentStatus];
  };

  const getNextStatusText = (currentStatus) => {
    const nextStatus = getNextStatus(currentStatus);
    const actionMap = {
      picked_up: "Teslim Al",
      in_transit: "Yola Çık",
      delivered: "Teslim Et",
    };
    return actionMap[nextStatus];
  };

  if (loading) {
    return (
      <div className="d-flex justify-content-center align-items-center min-vh-100">
        <div className="spinner-border text-primary"></div>
      </div>
    );
  }

  return (
    <div className="min-vh-100 bg-light">
      {/* Header */}
      <nav
        className="navbar navbar-expand-lg navbar-dark"
        style={{ background: "linear-gradient(135deg, #ff6b35, #ff8c00)" }}
      >
        <div className="container-fluid">
          <button
            onClick={() => navigate("/courier/dashboard")}
            className="btn btn-link text-white text-decoration-none"
          >
            <i className="fas fa-arrow-left me-2"></i>
            Dashboard
          </button>
          <span className="navbar-brand mb-0">Siparişlerim</span>
        </div>
      </nav>

      <div className="container-fluid p-4">
        <div className="row">
          <div className="col-12">
            <div className="card border-0 shadow-sm">
              <div className="card-header bg-white border-0 py-3">
                <h5 className="fw-bold mb-0">
                  <i className="fas fa-list-alt me-2 text-primary"></i>
                  Tüm Siparişler ({orders.length})
                </h5>
              </div>
              <div className="card-body p-0">
                {orders.length === 0 ? (
                  <div className="text-center py-5">
                    <i className="fas fa-inbox fs-1 text-muted mb-3"></i>
                    <p className="text-muted">
                      Henüz atanmış siparişiniz bulunmuyor.
                    </p>
                  </div>
                ) : (
                  <div className="table-responsive">
                    <table className="table table-hover mb-0">
                      <thead className="bg-light">
                        <tr>
                          <th>Sipariş</th>
                          <th>Müşteri</th>
                          <th>Adres</th>
                          <th>Tutar</th>
                          <th>Teslimat</th>
                          <th>Durum</th>
                          <th>İşlemler</th>
                        </tr>
                      </thead>
                      <tbody>
                        {orders.map((order) => {
                          const weightReport = weightReports[order.id];
                          const hasPendingWeight =
                            weightReport?.status === "Pending";
                          const hasApprovedWeight =
                            weightReport?.status === "Approved";

                          return (
                            <tr
                              key={order.id}
                              className={
                                hasPendingWeight
                                  ? "table-warning"
                                  : hasApprovedWeight
                                  ? "table-info"
                                  : ""
                              }
                            >
                              <td>
                                <div>
                                  <span className="fw-bold">#{order.id}</span>
                                  {hasPendingWeight && (
                                    <div className="mt-1">
                                      <span
                                        className="badge bg-warning text-dark fw-bold px-3 py-2"
                                        style={{
                                          fontSize: "0.85rem",
                                          boxShadow:
                                            "0 2px 8px rgba(255, 193, 7, 0.4)",
                                        }}
                                        title="Ağırlık fazlalığı admin onayı bekliyor"
                                      >
                                        <i className="fas fa-clock me-1"></i>
                                        ADMİN ONAYI BEKLİYOR
                                      </span>
                                    </div>
                                  )}
                                  {hasApprovedWeight && (
                                    <div className="mt-1">
                                      <span
                                        className="badge bg-success fw-bold px-3 py-2"
                                        style={{
                                          fontSize: "0.85rem",
                                          boxShadow:
                                            "0 2px 8px rgba(40, 167, 69, 0.4)",
                                        }}
                                        title="Ağırlık fazlalığı onaylandı"
                                      >
                                        <i className="fas fa-check-circle me-1"></i>
                                        ONAYLANDI +{weightReport.overageGrams}g
                                      </span>
                                    </div>
                                  )}
                                  <br />
                                  <small className="text-muted">
                                    {new Date(order.orderTime).toLocaleString(
                                      "tr-TR"
                                    )}
                                  </small>
                                </div>
                              </td>
                              <td>
                                <div>
                                  <span className="fw-semibold">
                                    {order.customerName}
                                  </span>
                                  <br />
                                  <small className="text-muted">
                                    {order.customerPhone}
                                  </small>
                                </div>
                              </td>
                              <td>
                                <span
                                  className="text-muted"
                                  title={order.address}
                                >
                                  {order.address.length > 40
                                    ? order.address.substring(0, 40) + "..."
                                    : order.address}
                                </span>
                              </td>
                              <td>
                                <span className="fw-bold text-success">
                                  {order.totalAmount.toFixed(2)} ₺
                                </span>
                                {hasApprovedWeight && (
                                  <div>
                                    <small className="text-success fw-bold">
                                      +{weightReport.overageAmount} ₺ ek
                                    </small>
                                  </div>
                                )}
                                {hasPendingWeight && (
                                  <div>
                                    <small className="text-warning">
                                      <i className="fas fa-clock"></i> Onay
                                      bekliyor
                                    </small>
                                  </div>
                                )}
                              </td>
                              <td>
                                <span className="badge bg-light text-dark border px-2 py-1">
                                  {order.shippingMethod === "car"
                                    ? "Araç"
                                    : order.shippingMethod === "motorcycle"
                                    ? "Motosiklet"
                                    : order.shippingMethod || "-"}
                                </span>
                              </td>
                              <td>
                                <span
                                  className={`badge bg-${getStatusColor(
                                    order.status
                                  )}`}
                                >
                                  {getStatusText(order.status)}
                                </span>
                              </td>
                              <td>
                                <div className="d-flex gap-2">
                                  <button
                                    onClick={() => setSelectedOrder(order)}
                                    className="btn btn-outline-primary btn-sm"
                                    title="Detayları Gör"
                                  >
                                    <i className="fas fa-eye"></i>
                                  </button>
                                  {getNextStatus(order.status) && (
                                    <button
                                      onClick={() =>
                                        updateOrderStatus(
                                          order.id,
                                          getNextStatus(order.status)
                                        )
                                      }
                                      disabled={updating}
                                      className={`btn btn-sm ${
                                        getNextStatus(order.status) ===
                                        "delivered"
                                          ? hasPendingWeight
                                            ? "btn-warning"
                                            : hasApprovedWeight
                                            ? "btn-success"
                                            : "btn-primary"
                                          : "btn-primary"
                                      }`}
                                      title={
                                        getNextStatus(order.status) ===
                                        "delivered"
                                          ? hasPendingWeight
                                            ? "⚠️ Admin onayı bekleniyor"
                                            : hasApprovedWeight
                                            ? `Teslim Et & +${weightReport.overageAmount}₺ Tahsil Et`
                                            : "Teslim Et"
                                          : getNextStatusText(order.status)
                                      }
                                    >
                                      {updating ? (
                                        <span className="spinner-border spinner-border-sm"></span>
                                      ) : (
                                        <>
                                          {getNextStatus(order.status) ===
                                          "delivered" ? (
                                            hasPendingWeight ? (
                                              <i className="fas fa-clock"></i>
                                            ) : hasApprovedWeight ? (
                                              <i className="fas fa-hand-holding-usd"></i>
                                            ) : (
                                              <i className="fas fa-check"></i>
                                            )
                                          ) : (
                                            <i className="fas fa-arrow-right"></i>
                                          )}
                                        </>
                                      )}
                                    </button>
                                  )}
                                </div>
                              </td>
                            </tr>
                          );
                        })}
                      </tbody>
                    </table>
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Sipariş Detay Modal */}
      {selectedOrder && (
        <div
          className="modal fade show d-block"
          tabIndex="-1"
          style={{ backgroundColor: "rgba(0,0,0,0.5)" }}
        >
          <div className="modal-dialog modal-lg">
            <div className="modal-content">
              <div className="modal-header">
                <h5 className="modal-title">
                  <i className="fas fa-receipt me-2"></i>
                  Sipariş #{selectedOrder.id} Detayı
                </h5>
                <button
                  onClick={() => setSelectedOrder(null)}
                  className="btn-close"
                ></button>
              </div>
              <div className="modal-body">
                {/* Ağırlık Onay Durumu - Belirgin Uyarı */}
                {(() => {
                  const report = weightReports[selectedOrder.id];
                  if (report && report.status === "Pending") {
                    return (
                      <div
                        className="alert alert-warning border-warning border-3 mb-4"
                        style={{
                          background:
                            "linear-gradient(135deg, #fff3cd 0%, #ffe69c 100%)",
                          boxShadow: "0 4px 12px rgba(255, 193, 7, 0.3)",
                        }}
                      >
                        <div className="d-flex align-items-center">
                          <div className="flex-shrink-0">
                            <i
                              className="fas fa-exclamation-triangle fa-3x text-warning"
                              style={{ animation: "pulse 2s infinite" }}
                            ></i>
                          </div>
                          <div className="flex-grow-1 ms-3">
                            <h5 className="alert-heading mb-2">
                              <i className="fas fa-clock me-2"></i>
                              ADMİN ONAYI BEKLENİYOR
                            </h5>
                            <p className="mb-2">
                              <strong>
                                Bu siparişte ağırlık fazlalığı tespit edildi!
                              </strong>
                            </p>
                            <div className="d-flex gap-3 mb-0">
                              <div>
                                <small className="text-muted">Fazlalık:</small>
                                <strong className="ms-1 text-warning">
                                  +{report.overageGrams}g
                                </strong>
                              </div>
                              <div>
                                <small className="text-muted">Ek Ücret:</small>
                                <strong className="ms-1 text-warning">
                                  +{report.overageAmount} ₺
                                </strong>
                              </div>
                            </div>
                            <hr className="my-2" />
                            <small className="text-muted">
                              <i className="fas fa-info-circle me-1"></i>
                              Admin onayından sonra teslimat yapabilir ve ek
                              ücreti tahsil edebilirsiniz.
                            </small>
                          </div>
                        </div>
                      </div>
                    );
                  } else if (report && report.status === "Approved") {
                    return (
                      <div
                        className="alert alert-success border-success border-3 mb-4"
                        style={{
                          background:
                            "linear-gradient(135deg, #d1f2eb 0%, #a8e6cf 100%)",
                          boxShadow: "0 4px 12px rgba(40, 167, 69, 0.3)",
                        }}
                      >
                        <div className="d-flex align-items-center">
                          <div className="flex-shrink-0">
                            <i className="fas fa-check-circle fa-3x text-success"></i>
                          </div>
                          <div className="flex-grow-1 ms-3">
                            <h5 className="alert-heading mb-2">
                              <i className="fas fa-thumbs-up me-2"></i>
                              ADMİN ONAYI VERİLDİ
                            </h5>
                            <p className="mb-2">
                              Ağırlık fazlalığı onaylandı. Teslimat yapıp ek
                              ücreti tahsil edebilirsiniz.
                            </p>
                            <div className="d-flex gap-3 mb-0">
                              <div>
                                <small className="text-muted">
                                  Onaylanan Fazlalık:
                                </small>
                                <strong className="ms-1 text-success">
                                  +{report.overageGrams}g
                                </strong>
                              </div>
                              <div>
                                <small className="text-muted">
                                  Tahsil Edilecek:
                                </small>
                                <strong className="ms-1 text-success">
                                  +{report.overageAmount} ₺
                                </strong>
                              </div>
                            </div>
                          </div>
                        </div>
                      </div>
                    );
                  }
                  return null;
                })()}

                <div className="row">
                  <div className="col-md-6">
                    <h6 className="fw-bold mb-3">
                      <i className="fas fa-user me-2 text-primary"></i>
                      Müşteri Bilgileri
                    </h6>
                    <div className="mb-2">
                      <small className="text-muted">Ad Soyad</small>
                      <p className="mb-0 fw-semibold">
                        {selectedOrder.customerName}
                      </p>
                    </div>
                    <div className="mb-2">
                      <small className="text-muted">Telefon</small>
                      <p className="mb-0 fw-semibold">
                        <a
                          href={`tel:${selectedOrder.customerPhone}`}
                          className="text-decoration-none"
                        >
                          <i className="fas fa-phone me-1"></i>
                          {selectedOrder.customerPhone}
                        </a>
                      </p>
                    </div>
                    <div className="mb-2">
                      <small className="text-muted">Adres</small>
                      <p className="mb-0">{selectedOrder.address}</p>
                    </div>
                  </div>
                  <div className="col-md-6">
                    <h6 className="fw-bold mb-3">
                      <i className="fas fa-box me-2 text-primary"></i>
                      Sipariş Bilgileri
                    </h6>
                    <div className="mb-2">
                      <small className="text-muted">Sipariş Zamanı</small>
                      <p className="mb-0 fw-semibold">
                        {new Date(selectedOrder.orderTime).toLocaleString(
                          "tr-TR"
                        )}
                      </p>
                    </div>
                    <div className="mb-2">
                      <small className="text-muted">Tutar</small>
                      <p className="mb-0 fw-semibold text-success">
                        {selectedOrder.totalAmount.toFixed(2)} ₺
                      </p>
                    </div>
                    <div className="mb-2">
                      <small className="text-muted">Teslimat Türü</small>
                      <p className="mb-0">
                        <span className="badge bg-light text-dark border">
                          <i
                            className={`fas fa-${
                              selectedOrder.shippingMethod === "car"
                                ? "car"
                                : "motorcycle"
                            } me-1`}
                          ></i>
                          {selectedOrder.shippingMethod === "car"
                            ? "Araç"
                            : "Motosiklet"}
                        </span>
                      </p>
                    </div>
                    <div className="mb-2">
                      <small className="text-muted">Durum</small>
                      <p className="mb-0">
                        <span
                          className={`badge bg-${getStatusColor(
                            selectedOrder.status
                          )}`}
                        >
                          {getStatusText(selectedOrder.status)}
                        </span>
                      </p>
                    </div>
                  </div>
                </div>

                <hr className="my-3" />

                <h6 className="fw-bold mb-3">
                  <i className="fas fa-shopping-basket me-2 text-primary"></i>
                  Ürünler
                </h6>
                <ul className="list-group">
                  {selectedOrder.items.map((item, index) => (
                    <li
                      key={index}
                      className="list-group-item d-flex justify-content-between align-items-center"
                    >
                      <span>
                        <i className="fas fa-check text-success me-2"></i>
                        {item}
                      </span>
                    </li>
                  ))}
                </ul>

                {getNextStatus(selectedOrder.status) && (
                  <div className="mt-4 text-center">
                    <button
                      onClick={() => {
                        setSelectedOrder(null);
                        updateOrderStatus(
                          selectedOrder.id,
                          getNextStatus(selectedOrder.status)
                        );
                      }}
                      disabled={updating}
                      className={`btn btn-lg ${
                        getNextStatus(selectedOrder.status) === "delivered"
                          ? weightReports[selectedOrder.id]?.status ===
                            "Pending"
                            ? "btn-warning"
                            : "btn-success"
                          : "btn-primary"
                      }`}
                    >
                      {updating ? (
                        <>
                          <span className="spinner-border spinner-border-sm me-2"></span>
                          İşleniyor...
                        </>
                      ) : (
                        <>
                          <i className="fas fa-arrow-right me-2"></i>
                          {getNextStatusText(selectedOrder.status)}
                        </>
                      )}
                    </button>
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Ağırlık Onay Uyarı Modal */}
      <WeightApprovalWarningModal
        isOpen={showWeightModal}
        onClose={() => {
          setShowWeightModal(false);
          setPendingDeliveryOrder(null);
        }}
        onConfirm={confirmDelivery}
        orderData={pendingDeliveryOrder}
        weightReport={
          pendingDeliveryOrder ? weightReports[pendingDeliveryOrder.id] : null
        }
      />
    </div>
  );
}
