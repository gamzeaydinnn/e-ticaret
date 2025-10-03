import React, { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { useNavigate, useParams } from 'react-router-dom';
import { AdminService } from '../services/adminService';
import api from '../services/api';
export default function AdminProductForm(){
  const { id } = useParams();
  const { register, handleSubmit, setValue } = useForm();
  const [images, setImages] = useState([]);
  const navigate = useNavigate();
useEffect(()=>{
    if(id){
      api.get(`/admin/products/${id}`).then(r=>{
        const p = r.data;
        setValue('name', p.name);
        setValue('price', p.price);
        setValue('stock', p.stock);
        setImages(p.images || []);
      });
    }
  },[id]);
const onSubmit = async (data)=>{
    const formData = new FormData();
    formData.append('name', data.name);
    formData.append('price', data.price);
    formData.append('stock', data.stock);
    // images: files
    images.forEach((f,i)=>{
      if(f instanceof File) formData.append('images', f);
    });
try{
      if(id) await AdminService.updateProduct(id, formData);
      else await AdminService.createProduct(formData);
      navigate('/admin/products');
    }catch(err){ alert('Hata: ' + err.message); }
  }
const handleFiles = (e)=>{
    const files = Array.from(e.target.files);
    setImages(prev => [...prev, ...files]);
  }
const removeImage = (idx)=> setImages(prev => prev.filter((_,i)=>i!==idx));
return (
    <div className="p-6">
      <h1 className="text-xl font-bold mb-4">{id ? 'Ürün Düzenle' : 'Yeni Ürün'}</h1>
      <form onSubmit={handleSubmit(onSubmit)} className="max-w-xl">
        <input {...register('name', { required: true })} placeholder="Ürün adı" className="w-full border p-2 mb-2" />
        <input {...register('price', { required: true })} placeholder="Fiyat" className="w-full border p-2 mb-2" />
        <input {...register('stock', { required: true })} placeholder="Stok" className="w-full border p-2 mb-2" />
<div className="mb-2">
          <label className="block mb-1">Resimler</label>
          <input type="file" multiple accept="image/*" onChange={handleFiles} />
          <div className="flex gap-2 mt-2 flex-wrap">
            {images.map((img, idx) => (
              <div key={idx} className="w-24 h-24 border rounded overflow-hidden relative">
                <img src={typeof img === 'string' ? img : URL.createObjectURL(img)} className="w-full h-full object-cover"/>
                <button type="button" onClick={()=>removeImage(idx)} className="absolute top-1 right-1 bg-white rounded px-1">x</button>
              </div>
            ))}
          </div>
        </div>
<button className="bg-green-600 text-white px-4 py-2 rounded">Kaydet</button>
      </form>
    </div>
  );
}
