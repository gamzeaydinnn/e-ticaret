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
      <div className="flex-1 p-6 bg-gray-50 min-h-screen">
        <h1 className="text-2xl font-bold mb-4">Micro ERP Yönetim Paneli</h1>

        <div className="flex gap-4 mb-6">
          <button
            onClick={syncProducts}
            className="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700"
          >
            Ürünleri Senkronize Et
          </button>
          <button
            onClick={syncStocksFromERP}
            className="px-4 py-2 bg-indigo-600 text-white rounded hover:bg-indigo-700"
          >
            Stokları ERP'den Çek
          </button>
          <button
            onClick={syncPricesFromERP}
            className="px-4 py-2 bg-rose-600 text-white rounded hover:bg-rose-700"
          >
            Fiyatları ERP'den Çek
          </button>
          <button
            onClick={fetchProducts}
            className="px-4 py-2 bg-green-600 text-white rounded hover:bg-green-700"
          >
            Ürünleri Getir
          </button>
          <button
            onClick={fetchStocks}
            className="px-4 py-2 bg-yellow-600 text-white rounded hover:bg-yellow-700"
          >
            Stokları Getir
          </button>
          <button
            onClick={exportOrders}
            className="px-4 py-2 bg-purple-600 text-white rounded hover:bg-purple-700"
          >
            Siparişleri ERP’ye Gönder
          </button>
        </div>

        {loading && <p className="mb-4 text-gray-700">Yükleniyor...</p>}
        {message && <p className="mb-4 text-gray-800 font-medium">{message}</p>}

        <h2 className="text-xl font-semibold mb-2">Ürünler</h2>
        <div className="overflow-x-auto mb-6">
          <table className="min-w-full border border-gray-300">
            <thead>
              <tr className="bg-gray-200">
                <th className="border px-2 py-1">ID</th>
                <th className="border px-2 py-1">Ürün Adı</th>
                <th className="border px-2 py-1">Fiyat</th>
                <th className="border px-2 py-1">Kategori</th>
              </tr>
            </thead>
            <tbody>
              {products.length === 0 ? (
                <tr>
                  <td colSpan="4" className="text-center p-2">
                    Ürün bulunamadı
                  </td>
                </tr>
              ) : (
                products.map((p) => (
                  <tr key={p.id}>
                    <td className="border px-2 py-1">{p.id}</td>
                    <td className="border px-2 py-1">{p.name}</td>
                    <td className="border px-2 py-1">{p.price}</td>
                    <td className="border px-2 py-1">{p.category}</td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        <h2 className="text-xl font-semibold mb-2">Stoklar</h2>
        <div className="overflow-x-auto">
          <table className="min-w-full border border-gray-300">
            <thead>
              <tr className="bg-gray-200">
                <th className="border px-2 py-1">Ürün ID</th>
                <th className="border px-2 py-1">Stok Miktarı</th>
              </tr>
            </thead>
            <tbody>
              {stocks.length === 0 ? (
                <tr>
                  <td colSpan="2" className="text-center p-2">
                    Stok bulunamadı
                  </td>
                </tr>
              ) : (
                stocks.map((s) => (
                  <tr key={s.productId}>
                    <td className="border px-2 py-1">{s.productId}</td>
                    <td className="border px-2 py-1">{s.quantity}</td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
