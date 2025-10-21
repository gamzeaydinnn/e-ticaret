import { useState } from "react";
import { MicroService } from "../../services/microService";
import Sidebar from "../components/Sidebar";

export default function AdminMicro() {
  const [products, setProducts] = useState([]);
  const [stocks, setStocks] = useState([]);
  const [message, setMessage] = useState("");
  const [loading, setLoading] = useState(false);

  const syncProducts = async () => {
    setLoading(true);
    try {
      const res = await MicroService.syncProducts();
      setMessage(res.message);
    } catch (err) {
      setMessage("Bir hata oluştu");
    }
    setLoading(false);
  };

  const fetchProducts = async () => {
    setLoading(true);
    try {
      const res = await MicroService.getProducts();
      setProducts(res);
    } catch (err) {
      setMessage("Ürünler getirilemedi");
    }
    setLoading(false);
  };

  const fetchStocks = async () => {
    setLoading(true);
    try {
      const res = await MicroService.getStocks();
      setStocks(res);
    } catch (err) {
      setMessage("Stoklar getirilemedi");
    }
    setLoading(false);
  };

  const exportOrders = async () => {
    setLoading(true);
    try {
      const orders = []; // Admin buradan seçebilir
      const res = await MicroService.exportOrders(orders);
      setMessage(res.message);
    } catch (err) {
      setMessage("Siparişler ERP'ye gönderilemedi");
    }
    setLoading(false);
  };

  const syncStocksFromERP = async () => {
    setLoading(true);
    try {
      const res = await MicroService.syncStocksFromERP();
      setMessage(res.message || "Stoklar ERP'den güncellendi");
    } catch (err) {
      setMessage("Stok senkronizasyonu başarısız");
    }
    setLoading(false);
  };

  const syncPricesFromERP = async () => {
    setLoading(true);
    try {
      const res = await MicroService.syncPricesFromERP();
      setMessage(res.message || "Fiyatlar ERP'den güncellendi");
    } catch (err) {
      setMessage("Fiyat senkronizasyonu başarısız");
    }
    setLoading(false);
  };

  return (
    <div className="flex">
      <Sidebar />
      <div className="flex-1 p-0 bg-gray-50 min-h-screen">
        <div className="max-w-5xl mx-auto py-6 px-4">
          <div className="bg-white rounded-xl shadow-lg p-6">
            <div className="flex items-center justify-between mb-4 pb-2 border-b border-gray-100">
              <div className="flex items-center gap-2">
                <i
                  className="fas fa-cogs text-2xl"
                  style={{ color: "#f57c00" }}
                ></i>
                <span className="text-2xl font-bold text-gray-800">
                  Micro ERP Yönetim Paneli
                </span>
              </div>
              <span className="text-sm text-gray-400">
                ERP ile ürün, stok ve fiyat yönetimi
              </span>
            </div>
            <div className="flex flex-wrap gap-2 mb-6">
              <button
                onClick={syncProducts}
                className="px-3 py-2 rounded bg-blue-600 text-white text-sm font-medium shadow hover:bg-blue-700 flex items-center gap-1"
              >
                <i className="fas fa-sync-alt"></i>Ürünleri Senkronize Et
              </button>
              <button
                onClick={syncStocksFromERP}
                className="px-3 py-2 rounded bg-indigo-600 text-white text-sm font-medium shadow hover:bg-indigo-700 flex items-center gap-1"
              >
                <i className="fas fa-boxes"></i>Stokları ERP'den Çek
              </button>
              <button
                onClick={syncPricesFromERP}
                className="px-3 py-2 rounded bg-rose-600 text-white text-sm font-medium shadow hover:bg-rose-700 flex items-center gap-1"
              >
                <i className="fas fa-tags"></i>Fiyatları ERP'den Çek
              </button>
              <button
                onClick={fetchProducts}
                className="px-3 py-2 rounded bg-green-600 text-white text-sm font-medium shadow hover:bg-green-700 flex items-center gap-1"
              >
                <i className="fas fa-box"></i>Ürünleri Getir
              </button>
              <button
                onClick={fetchStocks}
                className="px-3 py-2 rounded bg-yellow-500 text-white text-sm font-medium shadow hover:bg-yellow-600 flex items-center gap-1"
              >
                <i className="fas fa-warehouse"></i>Stokları Getir
              </button>
              <button
                onClick={exportOrders}
                className="px-3 py-2 rounded bg-purple-600 text-white text-sm font-medium shadow hover:bg-purple-700 flex items-center gap-1"
              >
                <i className="fas fa-paper-plane"></i>Siparişleri ERP’ye Gönder
              </button>
            </div>
            {loading && (
              <div className="mb-4 flex items-center text-orange-600 text-sm">
                <i className="fas fa-spinner fa-spin mr-2"></i>Yükleniyor...
              </div>
            )}
            {message && (
              <div className="mb-4 px-3 py-2 rounded bg-orange-50 text-orange-800 font-medium shadow text-sm">
                {message}
              </div>
            )}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <div>
                <div className="flex items-center gap-2 mb-2">
                  <i className="fas fa-box text-lg text-blue-500"></i>
                  <span className="text-lg font-semibold text-gray-700">
                    Ürünler
                  </span>
                </div>
                <div className="bg-gray-50 rounded-lg shadow border border-gray-100 overflow-x-auto">
                  <table className="min-w-full text-sm">
                    <thead>
                      <tr className="bg-gray-100">
                        <th className="border px-3 py-2 text-left">ID</th>
                        <th className="border px-3 py-2 text-left">Ürün Adı</th>
                        <th className="border px-3 py-2 text-left">Fiyat</th>
                        <th className="border px-3 py-2 text-left">Kategori</th>
                      </tr>
                    </thead>
                    <tbody>
                      {products.length === 0 ? (
                        <tr>
                          <td
                            colSpan="4"
                            className="text-center p-6 text-gray-400"
                          >
                            <i className="fas fa-box-open fa-2x mb-2"></i>
                            <div>Ürün bulunamadı</div>
                          </td>
                        </tr>
                      ) : (
                        products.map((p) => (
                          <tr key={p.id} className="hover:bg-gray-100">
                            <td className="border px-3 py-2">{p.id}</td>
                            <td className="border px-3 py-2">{p.name}</td>
                            <td className="border px-3 py-2">{p.price}</td>
                            <td className="border px-3 py-2">{p.category}</td>
                          </tr>
                        ))
                      )}
                    </tbody>
                  </table>
                </div>
              </div>
              <div>
                <div className="flex items-center gap-2 mb-2">
                  <i className="fas fa-warehouse text-lg text-yellow-600"></i>
                  <span className="text-lg font-semibold text-gray-700">
                    Stoklar
                  </span>
                </div>
                <div className="bg-gray-50 rounded-lg shadow border border-gray-100 overflow-x-auto">
                  <table className="min-w-full text-sm">
                    <thead>
                      <tr className="bg-gray-100">
                        <th className="border px-3 py-2 text-left">Ürün ID</th>
                        <th className="border px-3 py-2 text-left">
                          Stok Miktarı
                        </th>
                      </tr>
                    </thead>
                    <tbody>
                      {stocks.length === 0 ? (
                        <tr>
                          <td
                            colSpan="2"
                            className="text-center p-6 text-gray-400"
                          >
                            <i className="fas fa-box-open fa-2x mb-2"></i>
                            <div>Stok bulunamadı</div>
                          </td>
                        </tr>
                      ) : (
                        stocks.map((s) => (
                          <tr key={s.productId} className="hover:bg-gray-100">
                            <td className="border px-3 py-2">{s.productId}</td>
                            <td className="border px-3 py-2">{s.quantity}</td>
                          </tr>
                        ))
                      )}
                    </tbody>
                  </table>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
