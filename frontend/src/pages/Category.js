//kategoriye göre ürün listeleme, filtre sidebar

/*import React, { useEffect, useState } from 'react';
import api from '../services/api';
import ProductCard from '../components/ProductCard';
import { useParams } from 'react-router-dom';
export default function Category(){
  const { slug } = useParams();
  const [products, setProducts] = useState([]);
  const [category, setCategory] = useState(null);
useEffect(()=>{
    api.get(`/categories/slug/${slug}`).then(r=>setCategory(r.data)).catch(()=>{});
    api.get(`/products?category=${slug}`).then(r=>setProducts(r.data)).catch(()=>{});
  },[slug]);
return (
    <div className="container mx-auto px-4 py-8">
      <h1 className="text-2xl font-bold mb-6">{category?.name || 'Kategori'}</h1>
      <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
        <aside className="md:col-span-1">
          <div className="bg-white p-4 rounded shadow">Filtreler (örnek)</div>
        </aside>
        <main className="md:col-span-3">
          <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4">
            {products.map(p => <ProductCard key={p.id} product={p} />)}
          </div>
        </main>
      </div>
    </div>
  );
}
*/
