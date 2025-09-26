import Sidebar from "../components/Sidebar";
import Navbar from "../components/Navbar";


export default function Products() {
  return (
    <div className="flex min-h-screen bg-gray-100">
      <Sidebar />
      <div className="flex-1">
        <Navbar title="Ürün Yönetimi" />
        <div className="p-6">
          <button className="mb-4 bg-green-500 text-white px-4 py-2 rounded-lg hover:bg-green-600">
            Yeni Ürün Ekle
          </button>

          <table className="w-full bg-white shadow rounded-2xl">
            <thead>
              <tr className="text-left border-b">
                <th className="p-3">ID</th>
                <th className="p-3">Ürün Adı</th>
                <th className="p-3">Fiyat</th>
                <th className="p-3">Stok</th>
                <th className="p-3">İşlemler</th>
              </tr>
            </thead>
            <tbody>
              <tr className="border-b">
                <td className="p-3">1</td>
                <td className="p-3">Laptop</td>
                <td className="p-3">₺15.000</td>
                <td className="p-3">25</td>
                <td className="p-3">
                  <button className="mr-2 bg-yellow-500 text-white px-3 py-1 rounded hover:bg-yellow-600">
                    Düzenle
                  </button>
                  <button className="bg-red-500 text-white px-3 py-1 rounded hover:bg-red-600">
                    Sil
                  </button>
                </td>
              </tr>
              {/* API'den gelen ürünler burada map ile listelenecek */}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
