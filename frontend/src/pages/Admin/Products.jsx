import Sidebar from "../components/Sidebar";
import Navbar from "../components/Navbar";
import React, { useState, useEffect, useCallback } from "react";
import { ProductService } from "../../services/productService";

export default function Products() {
  const [products, setProducts] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  // Modal states
  const [showAdd, setShowAdd] = useState(false);
  const [showEdit, setShowEdit] = useState(false);
  const [showExcelImport, setShowExcelImport] = useState(false);
  const [editingProduct, setEditingProduct] = useState(null);

  // Form states
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [price, setPrice] = useState("");
  const [stock, setStock] = useState("");
  const [categoryId, setCategoryId] = useState("1");
  const [imageUrl, setImageUrl] = useState("");
  const [formError, setFormError] = useState("");
  const [formSuccess, setFormSuccess] = useState("");

  // Excel import states
  const [excelFile, setExcelFile] = useState(null);
  const [importLoading, setImportLoading] = useState(false);
  const [importResult, setImportResult] = useState(null);

  // Ürünleri yükle
  const loadProducts = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const items = await ProductService.getAll();
      setProducts(items || []);
    } catch (err) {
      console.error("Ürünler yüklenemedi:", err);
      setError(
        "Ürünler yüklenemedi. " + (err.response?.data?.message || err.message)
      );
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadProducts();
  }, [loadProducts]);

  // Form temizle
  const resetForm = () => {
    setName("");
    setDescription("");
    setPrice("");
    setStock("");
    setCategoryId("1");
    setImageUrl("");
    setFormError("");
    setFormSuccess("");
    setEditingProduct(null);
  };

  // Yeni ürün oluştur
  const handleCreate = async (e) => {
    e.preventDefault();
    setFormError("");
    setFormSuccess("");

    if (!name.trim()) return setFormError("Ürün adı zorunludur.");
    if (!price || isNaN(parseFloat(price)))
      return setFormError("Geçerli bir fiyat girin.");
    if (!stock || isNaN(parseInt(stock)))
      return setFormError("Geçerli bir stok miktarı girin.");

    try {
      await ProductService.createAdmin({
        name: name.trim(),
        description: description.trim(),
        price: parseFloat(price),
        stockQuantity: parseInt(stock),
        categoryId: parseInt(categoryId),
        imageUrl: imageUrl.trim() || null,
      });

      setFormSuccess("Ürün başarıyla eklendi!");
      setTimeout(() => {
        setShowAdd(false);
        resetForm();
        loadProducts();
      }, 1000);
    } catch (err) {
      setFormError(
        err.response?.data?.message || "Ürün eklenirken hata oluştu."
      );
    }
  };

  // Ürün düzenle
  const handleEdit = async (e) => {
    e.preventDefault();
    setFormError("");
    setFormSuccess("");

    if (!name.trim()) return setFormError("Ürün adı zorunludur.");
    if (!price || isNaN(parseFloat(price)))
      return setFormError("Geçerli bir fiyat girin.");
    if (!stock || isNaN(parseInt(stock)))
      return setFormError("Geçerli bir stok miktarı girin.");

    try {
      await ProductService.updateAdmin(editingProduct.id, {
        name: name.trim(),
        description: description.trim(),
        price: parseFloat(price),
        stockQuantity: parseInt(stock),
        categoryId: parseInt(categoryId),
        imageUrl: imageUrl.trim() || null,
      });

      setFormSuccess("Ürün başarıyla güncellendi!");
      setTimeout(() => {
        setShowEdit(false);
        resetForm();
        loadProducts();
      }, 1000);
    } catch (err) {
      setFormError(
        err.response?.data?.message || "Ürün güncellenirken hata oluştu."
      );
    }
  };

  // Ürün sil
  const handleDelete = async (productId, productName) => {
    if (
      !window.confirm(
        `"${productName}" ürününü silmek istediğinize emin misiniz?`
      )
    ) {
      return;
    }

    try {
      await ProductService.deleteAdmin(productId);
      loadProducts();
    } catch (err) {
      alert(err.response?.data?.message || "Ürün silinirken hata oluştu.");
    }
  };

  // Düzenleme modalı aç
  const openEditModal = (product) => {
    setEditingProduct(product);
    setName(product.name || "");
    setDescription(product.description || "");
    setPrice(product.price?.toString() || "");
    setStock(
      product.stockQuantity?.toString() || product.stock?.toString() || "0"
    );
    setCategoryId(product.categoryId?.toString() || "1");
    setImageUrl(product.imageUrl || "");
    setFormError("");
    setFormSuccess("");
    setShowEdit(true);
  };

  // Excel dosyası yükle
  const handleExcelUpload = async (e) => {
    e.preventDefault();
    if (!excelFile) {
      setFormError("Lütfen bir dosya seçin.");
      return;
    }

    setImportLoading(true);
    setImportResult(null);
    setFormError("");

    try {
      const result = await ProductService.importExcel(excelFile);
      setImportResult(result);
      loadProducts();
    } catch (err) {
      setFormError(
        err.response?.data?.message || "Dosya yüklenirken hata oluştu."
      );
    } finally {
      setImportLoading(false);
    }
  };

  // Excel şablonu indir
  const downloadTemplate = async () => {
    try {
      const blob = await ProductService.downloadTemplate();
      const url = window.URL.createObjectURL(new Blob([blob]));
      const link = document.createElement("a");
      link.href = url;
      link.setAttribute("download", "urun_sablonu.xlsx");
      document.body.appendChild(link);
      link.click();
      link.remove();
    } catch (err) {
      alert("Şablon indirilemedi.");
    }
  };

  return (
    <div className="flex min-h-screen bg-gray-100">
      <Sidebar />
      <div className="flex-1">
        <Navbar title="Ürün Yönetimi" />
        <div className="p-6">
          {/* Üst butonlar */}
          <div className="mb-4 flex gap-2 flex-wrap">
            <button
              onClick={() => {
                resetForm();
                setShowAdd(true);
              }}
              className="bg-green-500 text-white px-4 py-2 rounded-lg hover:bg-green-600"
            >
              ➕ Yeni Ürün Ekle
            </button>
            <button
              onClick={() => {
                setExcelFile(null);
                setImportResult(null);
                setFormError("");
                setShowExcelImport(true);
              }}
              className="bg-blue-500 text-white px-4 py-2 rounded-lg hover:bg-blue-600"
            >
              📊 Excel'den Yükle
            </button>
            <button
              onClick={downloadTemplate}
              className="bg-gray-500 text-white px-4 py-2 rounded-lg hover:bg-gray-600"
            >
              📥 Şablon İndir
            </button>
            <button
              onClick={loadProducts}
              className="bg-purple-500 text-white px-4 py-2 rounded-lg hover:bg-purple-600"
            >
              🔄 Yenile
            </button>
          </div>

          {/* Loading/Error states */}
          {loading && (
            <div className="text-center py-8">
              <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-500 mx-auto"></div>
              <p className="mt-2">Yükleniyor...</p>
            </div>
          )}

          {error && (
            <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded mb-4">
              {error}
            </div>
          )}

          {/* Ürün tablosu */}
          {!loading && !error && (
            <>
              <div className="mb-2 text-sm text-gray-600">
                Toplam {products.length} ürün
              </div>
              <div className="overflow-x-auto">
                <table className="w-full bg-white shadow rounded-2xl">
                  <thead>
                    <tr className="text-left border-b bg-gray-50">
                      <th className="p-3">ID</th>
                      <th className="p-3">Görsel</th>
                      <th className="p-3">Ürün Adı</th>
                      <th className="p-3">Kategori</th>
                      <th className="p-3">Fiyat</th>
                      <th className="p-3">Stok</th>
                      <th className="p-3">İşlemler</th>
                    </tr>
                  </thead>
                  <tbody>
                    {products.length === 0 ? (
                      <tr>
                        <td
                          colSpan="7"
                          className="p-8 text-center text-gray-500"
                        >
                          Henüz ürün bulunmuyor.
                        </td>
                      </tr>
                    ) : (
                      products.map((p) => (
                        <tr key={p.id} className="border-b hover:bg-gray-50">
                          <td className="p-3">{p.id}</td>
                          <td className="p-3">
                            {p.imageUrl ? (
                              <img
                                src={
                                  p.imageUrl.startsWith("http")
                                    ? p.imageUrl
                                    : `${window.location.origin}${p.imageUrl}`
                                }
                                alt={p.name}
                                className="w-12 h-12 object-cover rounded"
                                onError={(e) => {
                                  e.target.style.display = "none";
                                }}
                              />
                            ) : (
                              <div className="w-12 h-12 bg-gray-200 rounded flex items-center justify-center text-gray-400">
                                📷
                              </div>
                            )}
                          </td>
                          <td className="p-3">
                            <div className="font-medium">{p.name}</div>
                            {p.description && (
                              <div className="text-xs text-gray-500 truncate max-w-xs">
                                {p.description}
                              </div>
                            )}
                          </td>
                          <td className="p-3">
                            <span className="bg-blue-100 text-blue-800 text-xs px-2 py-1 rounded">
                              {p.categoryName ||
                                `Kategori #${p.categoryId || "-"}`}
                            </span>
                          </td>
                          <td className="p-3">
                            <span className="font-semibold text-green-600">
                              ₺{p.price?.toFixed(2) || "0.00"}
                            </span>
                            {p.specialPrice && (
                              <div className="text-xs text-red-500">
                                İndirimli: ₺{p.specialPrice.toFixed(2)}
                              </div>
                            )}
                          </td>
                          <td className="p-3">
                            <span
                              className={`font-medium ${
                                (p.stockQuantity || p.stock || 0) < 10
                                  ? "text-red-500"
                                  : "text-gray-700"
                              }`}
                            >
                              {p.stockQuantity ?? p.stock ?? 0}
                            </span>
                          </td>
                          <td className="p-3">
                            <button
                              onClick={() => openEditModal(p)}
                              className="mr-2 bg-yellow-500 text-white px-3 py-1 rounded hover:bg-yellow-600"
                            >
                              ✏️ Düzenle
                            </button>
                            <button
                              onClick={() => handleDelete(p.id, p.name)}
                              className="bg-red-500 text-white px-3 py-1 rounded hover:bg-red-600"
                            >
                              🗑️ Sil
                            </button>
                          </td>
                        </tr>
                      ))
                    )}
                  </tbody>
                </table>
              </div>
            </>
          )}

          {/* Yeni Ürün Modal */}
          {showAdd && (
            <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
              <div className="bg-white rounded-lg shadow p-6 w-full max-w-md max-h-[90vh] overflow-y-auto">
                <h5 className="mb-4 font-bold text-xl">➕ Yeni Ürün Ekle</h5>

                <form onSubmit={handleCreate}>
                  <div className="mb-3">
                    <label className="block text-sm font-medium mb-1">
                      Ürün Adı *
                    </label>
                    <input
                      className="w-full border rounded px-3 py-2"
                      placeholder="Örn: iPhone 15 Pro"
                      value={name}
                      onChange={(e) => setName(e.target.value)}
                    />
                  </div>

                  <div className="mb-3">
                    <label className="block text-sm font-medium mb-1">
                      Açıklama
                    </label>
                    <textarea
                      className="w-full border rounded px-3 py-2"
                      rows="3"
                      placeholder="Ürün açıklaması..."
                      value={description}
                      onChange={(e) => setDescription(e.target.value)}
                    />
                  </div>

                  <div className="mb-3 grid grid-cols-2 gap-3">
                    <div>
                      <label className="block text-sm font-medium mb-1">
                        Fiyat (₺) *
                      </label>
                      <input
                        type="number"
                        step="0.01"
                        className="w-full border rounded px-3 py-2"
                        placeholder="0.00"
                        value={price}
                        onChange={(e) => setPrice(e.target.value)}
                      />
                    </div>
                    <div>
                      <label className="block text-sm font-medium mb-1">
                        Stok *
                      </label>
                      <input
                        type="number"
                        className="w-full border rounded px-3 py-2"
                        placeholder="0"
                        value={stock}
                        onChange={(e) => setStock(e.target.value)}
                      />
                    </div>
                  </div>

                  <div className="mb-3">
                    <label className="block text-sm font-medium mb-1">
                      Kategori ID
                    </label>
                    <input
                      type="number"
                      className="w-full border rounded px-3 py-2"
                      value={categoryId}
                      onChange={(e) => setCategoryId(e.target.value)}
                    />
                  </div>

                  <div className="mb-3">
                    <label className="block text-sm font-medium mb-1">
                      Görsel URL
                    </label>
                    <input
                      className="w-full border rounded px-3 py-2"
                      placeholder="/uploads/products/ornek.jpg"
                      value={imageUrl}
                      onChange={(e) => setImageUrl(e.target.value)}
                    />
                  </div>

                  {formError && (
                    <div className="text-red-500 text-sm mb-3 p-2 bg-red-50 rounded">
                      {formError}
                    </div>
                  )}
                  {formSuccess && (
                    <div className="text-green-500 text-sm mb-3 p-2 bg-green-50 rounded">
                      {formSuccess}
                    </div>
                  )}

                  <div className="flex justify-end gap-2">
                    <button
                      className="px-4 py-2 rounded border hover:bg-gray-100"
                      type="button"
                      onClick={() => {
                        setShowAdd(false);
                        resetForm();
                      }}
                    >
                      İptal
                    </button>
                    <button
                      className="px-4 py-2 rounded bg-green-500 text-white hover:bg-green-600"
                      type="submit"
                    >
                      Oluştur
                    </button>
                  </div>
                </form>
              </div>
            </div>
          )}

          {/* Düzenle Modal */}
          {showEdit && editingProduct && (
            <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
              <div className="bg-white rounded-lg shadow p-6 w-full max-w-md max-h-[90vh] overflow-y-auto">
                <h5 className="mb-4 font-bold text-xl">✏️ Ürün Düzenle</h5>

                <form onSubmit={handleEdit}>
                  <div className="mb-3">
                    <label className="block text-sm font-medium mb-1">
                      Ürün Adı *
                    </label>
                    <input
                      className="w-full border rounded px-3 py-2"
                      value={name}
                      onChange={(e) => setName(e.target.value)}
                    />
                  </div>

                  <div className="mb-3">
                    <label className="block text-sm font-medium mb-1">
                      Açıklama
                    </label>
                    <textarea
                      className="w-full border rounded px-3 py-2"
                      rows="3"
                      value={description}
                      onChange={(e) => setDescription(e.target.value)}
                    />
                  </div>

                  <div className="mb-3 grid grid-cols-2 gap-3">
                    <div>
                      <label className="block text-sm font-medium mb-1">
                        Fiyat (₺) *
                      </label>
                      <input
                        type="number"
                        step="0.01"
                        className="w-full border rounded px-3 py-2"
                        value={price}
                        onChange={(e) => setPrice(e.target.value)}
                      />
                    </div>
                    <div>
                      <label className="block text-sm font-medium mb-1">
                        Stok *
                      </label>
                      <input
                        type="number"
                        className="w-full border rounded px-3 py-2"
                        value={stock}
                        onChange={(e) => setStock(e.target.value)}
                      />
                    </div>
                  </div>

                  <div className="mb-3">
                    <label className="block text-sm font-medium mb-1">
                      Kategori ID
                    </label>
                    <input
                      type="number"
                      className="w-full border rounded px-3 py-2"
                      value={categoryId}
                      onChange={(e) => setCategoryId(e.target.value)}
                    />
                  </div>

                  <div className="mb-3">
                    <label className="block text-sm font-medium mb-1">
                      Görsel URL
                    </label>
                    <input
                      className="w-full border rounded px-3 py-2"
                      value={imageUrl}
                      onChange={(e) => setImageUrl(e.target.value)}
                    />
                  </div>

                  {formError && (
                    <div className="text-red-500 text-sm mb-3 p-2 bg-red-50 rounded">
                      {formError}
                    </div>
                  )}
                  {formSuccess && (
                    <div className="text-green-500 text-sm mb-3 p-2 bg-green-50 rounded">
                      {formSuccess}
                    </div>
                  )}

                  <div className="flex justify-end gap-2">
                    <button
                      className="px-4 py-2 rounded border hover:bg-gray-100"
                      type="button"
                      onClick={() => {
                        setShowEdit(false);
                        resetForm();
                      }}
                    >
                      İptal
                    </button>
                    <button
                      className="px-4 py-2 rounded bg-yellow-500 text-white hover:bg-yellow-600"
                      type="submit"
                    >
                      Güncelle
                    </button>
                  </div>
                </form>
              </div>
            </div>
          )}

          {/* Excel Import Modal */}
          {showExcelImport && (
            <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
              <div className="bg-white rounded-lg shadow p-6 w-full max-w-md">
                <h5 className="mb-4 font-bold text-xl">
                  📊 Excel'den Ürün Yükle
                </h5>

                <div className="mb-4 p-3 bg-blue-50 rounded text-sm">
                  <p className="font-medium mb-1">Dosya formatı:</p>
                  <p>Excel (.xlsx, .xls) veya CSV dosyası yükleyebilirsiniz.</p>
                  <p className="mt-1">
                    Sütunlar: Ürün Adı, Açıklama, Fiyat, Stok, Kategori ID,
                    Görsel URL
                  </p>
                  <button
                    onClick={downloadTemplate}
                    className="mt-2 text-blue-600 hover:underline"
                  >
                    📥 Örnek şablonu indir
                  </button>
                </div>

                <form onSubmit={handleExcelUpload}>
                  <div className="mb-4">
                    <label className="block text-sm font-medium mb-2">
                      Dosya Seç
                    </label>
                    <input
                      type="file"
                      accept=".xlsx,.xls,.csv"
                      className="w-full border rounded px-3 py-2"
                      onChange={(e) => setExcelFile(e.target.files[0])}
                    />
                  </div>

                  {formError && (
                    <div className="text-red-500 text-sm mb-3 p-2 bg-red-50 rounded">
                      {formError}
                    </div>
                  )}

                  {importResult && (
                    <div
                      className={`text-sm mb-3 p-3 rounded ${
                        importResult.errorCount > 0
                          ? "bg-yellow-50"
                          : "bg-green-50"
                      }`}
                    >
                      <p className="font-medium">{importResult.message}</p>
                      <p>İşlenen: {importResult.totalProcessed}</p>
                      <p>Başarılı: {importResult.successCount}</p>
                      {importResult.errorCount > 0 && (
                        <>
                          <p className="text-red-500">
                            Hatalı: {importResult.errorCount}
                          </p>
                          {importResult.errors?.length > 0 && (
                            <ul className="mt-2 text-xs text-red-500">
                              {importResult.errors.map((err, i) => (
                                <li key={i}>• {err}</li>
                              ))}
                            </ul>
                          )}
                        </>
                      )}
                    </div>
                  )}

                  <div className="flex justify-end gap-2">
                    <button
                      className="px-4 py-2 rounded border hover:bg-gray-100"
                      type="button"
                      onClick={() => setShowExcelImport(false)}
                    >
                      Kapat
                    </button>
                    <button
                      className="px-4 py-2 rounded bg-blue-500 text-white hover:bg-blue-600 disabled:opacity-50"
                      type="submit"
                      disabled={importLoading || !excelFile}
                    >
                      {importLoading ? "Yükleniyor..." : "Yükle"}
                    </button>
                  </div>
                </form>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
