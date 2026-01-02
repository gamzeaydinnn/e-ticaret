import { useEffect, useState } from "react";
import { OrderService } from "../services/orderService";
import OrderDetailModal from "./OrderDetailModal";

async function cancelOrder(orderId, onSuccess, onError) {
  try {
    await OrderService.cancel(orderId);
    onSuccess && onSuccess();
  } catch (err) {
    onError && onError(err);
  }
}

export default function OrderHistory() {
  const [orders, setOrders] = useState([]);
  const [selectedOrder, setSelectedOrder] = useState(null);
  const [modalOpen, setModalOpen] = useState(false);
  const [loadingDetail, setLoadingDetail] = useState(false);

  useEffect(() => {
    OrderService.list()
      .then(setOrders)
      .catch(() => {});
  }, []);

  const handleShowDetail = async (orderId) => {
    setLoadingDetail(true);
    try {
      const { data } = await OrderService.getById(orderId);
      setSelectedOrder(data);
      setModalOpen(true);
    } catch {
      setSelectedOrder(null);
      setModalOpen(false);
    } finally {
      setLoadingDetail(false);
    }
  };

  return (
    <div className="container mx-auto px-4 py-8">
      <h1 className="text-2xl font-bold mb-6">Sipariş Geçmişi</h1>
      {orders.length === 0 ? (
        <p>Henüz siparişiniz yok.</p>
      ) : (
        <ul className="space-y-4">
          {orders.map((order) => (
            <li key={order.id} className="p-4 border rounded shadow">
              <div className="d-flex justify-content-between align-items-center">
                <div>
                  <div>Sipariş #{order.id}</div>
                  <div>Tutar: ₺{order.totalAmount}</div>
                  <div>Durum: {order.status}</div>
                </div>
                <div>
                  <button
                    className="btn btn-info btn-sm me-2"
                    onClick={() => handleShowDetail(order.id)}
                  >
                    Detay
                  </button>
                  {["Pending", "Processing"].includes(order.status) && (
                    <button
                      className="btn btn-danger btn-sm"
                      onClick={async () => {
                        await cancelOrder(order.id, () => {
                          setOrders((prev) =>
                            prev.map((o) =>
                              o.id === order.id
                                ? { ...o, status: "Cancelled" }
                                : o
                            )
                          );
                        });
                      }}
                    >
                      İptal Et
                    </button>
                  )}
                </div>
              </div>
            </li>
          ))}
        </ul>
      )}

      <OrderDetailModal
        show={modalOpen}
        onHide={() => {
          setModalOpen(false);
          setSelectedOrder(null);
        }}
        order={selectedOrder}
      />
      {loadingDetail && (
        <div
          className="position-fixed top-0 start-0 w-100 h-100 d-flex align-items-center justify-content-center"
          style={{ background: "rgba(255,255,255,0.5)", zIndex: 9999 }}
        >
          <div
            className="spinner-border text-warning"
            style={{ width: "4rem", height: "4rem" }}
          ></div>
        </div>
      )}
    </div>
  );
}
