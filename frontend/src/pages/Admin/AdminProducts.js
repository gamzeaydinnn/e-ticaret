/*import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { AdminService } from '../services/adminService';
export default function AdminProducts(){
  const [products, setProducts] = useState([]);
  useEffect(()=>{ AdminService.listProducts().then(setProducts).catch(()=>{}); }, []);
return (
    <div className="p-6">
      <div className="flex justify-between items-center mb-4">
        <h1 className="text-xl font-bold">Ürünler</h1>
        <Link to="/admin/products/new" className="bg-green-600 text-white px-4 py-2 rounded">Yeni Ürün</Link>
      </div>
<div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        {products.map(p => (
          <div key={p.id} className="bg-white p-4 rounded shadow">
            <img src={p.images?.[0] || p.image} className="w-full h-40 object-cover mb-2" />
            <div className="font-semibold">{p.name}</div>
            <div>₺{p.price}</div>
            <div className="mt-2 flex gap-2">
              <Link to={`/admin/products/${p.id}`} className="bg-blue-500 text-white px-3 py-1 rounded">Düzenle</Link>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
*/