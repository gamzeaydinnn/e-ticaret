import React, { useState, useEffect } from 'react';

const AdminProducts = () => {
  const [products, setProducts] = useState([]);
  const [showModal, setShowModal] = useState(false);
  const [editingProduct, setEditingProduct] = useState(null);
  const [formData, setFormData] = useState({
    name: '',
    price: '',
    category: '',
    stock: '',
    description: ''
  });

  useEffect(() => {
    // Demo data
    setProducts([
      { id: 1, name: 'iPhone 15 Pro', price: 45999, category: 'Elektronik', stock: 25, description: 'Son model iPhone' },
      { id: 2, name: 'Samsung TV 55"', price: 12999, category: 'Elektronik', stock: 8, description: '4K Smart TV' },
      { id: 3, name: 'Nike Air Max', price: 899, category: 'Spor', stock: 15, description: 'Koşu ayakkabısı' }
    ]);
  }, []);

  const handleSubmit = (e) => {
    e.preventDefault();
    if (editingProduct) {
      // Update product
      setProducts(products.map(p => 
        p.id === editingProduct.id 
          ? { ...editingProduct, ...formData, price: parseFloat(formData.price), stock: parseInt(formData.stock) }
          : p
      ));
    } else {
      // Add new product
      const newProduct = {
        id: Date.now(),
        ...formData,
        price: parseFloat(formData.price),
        stock: parseInt(formData.stock)
      };
      setProducts([...products, newProduct]);
    }
    handleCloseModal();
  };

  const handleEdit = (product) => {
    setEditingProduct(product);
    setFormData({
      name: product.name,
      price: product.price.toString(),
      category: product.category,
      stock: product.stock.toString(),
      description: product.description
    });
    setShowModal(true);
  };

  const handleDelete = (id) => {
    if (window.confirm('Bu ürünü silmek istediğinizden emin misiniz?')) {
      setProducts(products.filter(p => p.id !== id));
    }
  };

  const handleCloseModal = () => {
    setShowModal(false);
    setEditingProduct(null);
    setFormData({ name: '', price: '', category: '', stock: '', description: '' });
  };

  return (
    <div>
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h2 className="fw-bold text-dark">Ürün Yönetimi</h2>
        <button 
          className="btn text-white fw-bold px-4"
          style={{ 
            background: 'linear-gradient(45deg, #ff6f00, #ff8f00)',
            borderRadius: '10px',
            border: 'none'
          }}
          onClick={() => setShowModal(true)}
        >
          + Yeni Ürün
        </button>
      </div>

      {/* Products Table */}
      <div className="card shadow-sm border-0" style={{ borderRadius: '15px' }}>
        <div className="card-body">
          <div className="table-responsive">
            <table className="table table-hover">
              <thead>
                <tr>
                  <th>Ürün Adı</th>
                  <th>Kategori</th>
                  <th>Fiyat</th>
                  <th>Stok</th>
                  <th>İşlemler</th>
                </tr>
              </thead>
              <tbody>
                {products.map(product => (
                  <tr key={product.id}>
                    <td>
                      <div>
                        <div className="fw-bold">{product.name}</div>
                        <small className="text-muted">{product.description}</small>
                      </div>
                    </td>
                    <td>
                      <span className="badge bg-secondary rounded-pill">{product.category}</span>
                    </td>
                    <td className="fw-bold">₺{product.price.toLocaleString('tr-TR')}</td>
                    <td>
                      <span className={`badge rounded-pill ${product.stock > 10 ? 'bg-success' : product.stock > 0 ? 'bg-warning' : 'bg-danger'}`}>
                        {product.stock} adet
                      </span>
                    </td>
                    <td>
                      <button 
                        className="btn btn-sm btn-outline-primary me-2"
                        onClick={() => handleEdit(product)}
                      >
                        Düzenle
                      </button>
                      <button 
                        className="btn btn-sm btn-outline-danger"
                        onClick={() => handleDelete(product.id)}
                      >
                        Sil
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      </div>

      {/* Modal */}
      {showModal && (
        <div className="modal fade show d-block" style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}>
          <div className="modal-dialog modal-lg">
            <div className="modal-content" style={{ borderRadius: '15px' }}>
              <div className="modal-header border-0">
                <h5 className="modal-title fw-bold">
                  {editingProduct ? 'Ürün Düzenle' : 'Yeni Ürün Ekle'}
                </h5>
                <button type="button" className="btn-close" onClick={handleCloseModal}></button>
              </div>
              <form onSubmit={handleSubmit}>
                <div className="modal-body">
                  <div className="row">
                    <div className="col-md-6 mb-3">
                      <label className="form-label fw-bold">Ürün Adı</label>
                      <input
                        type="text"
                        className="form-control"
                        value={formData.name}
                        onChange={(e) => setFormData({...formData, name: e.target.value})}
                        required
                      />
                    </div>
                    <div className="col-md-6 mb-3">
                      <label className="form-label fw-bold">Kategori</label>
                      <select
                        className="form-select"
                        value={formData.category}
                        onChange={(e) => setFormData({...formData, category: e.target.value})}
                        required
                      >
                        <option value="">Kategori Seçin</option>
                        <option value="Elektronik">Elektronik</option>
                        <option value="Giyim">Giyim</option>
                        <option value="Spor">Spor</option>
                        <option value="Ev & Yaşam">Ev & Yaşam</option>
                      </select>
                    </div>
                    <div className="col-md-6 mb-3">
                      <label className="form-label fw-bold">Fiyat (₺)</label>
                      <input
                        type="number"
                        step="0.01"
                        className="form-control"
                        value={formData.price}
                        onChange={(e) => setFormData({...formData, price: e.target.value})}
                        required
                      />
                    </div>
                    <div className="col-md-6 mb-3">
                      <label className="form-label fw-bold">Stok Miktarı</label>
                      <input
                        type="number"
                        className="form-control"
                        value={formData.stock}
                        onChange={(e) => setFormData({...formData, stock: e.target.value})}
                        required
                      />
                    </div>
                    <div className="col-12 mb-3">
                      <label className="form-label fw-bold">Açıklama</label>
                      <textarea
                        className="form-control"
                        rows="3"
                        value={formData.description}
                        onChange={(e) => setFormData({...formData, description: e.target.value})}
                      />
                    </div>
                  </div>
                </div>
                <div className="modal-footer border-0">
                  <button type="button" className="btn btn-secondary" onClick={handleCloseModal}>
                    İptal
                  </button>
                  <button 
                    type="submit" 
                    className="btn text-white fw-bold"
                    style={{ 
                      background: 'linear-gradient(45deg, #ff6f00, #ff8f00)',
                      border: 'none'
                    }}
                  >
                    {editingProduct ? 'Güncelle' : 'Ekle'}
                  </button>
                </div>
              </form>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default AdminProducts;