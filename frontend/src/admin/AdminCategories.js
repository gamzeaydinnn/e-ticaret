/*import React, { useEffect, useState } from 'react';
import { AdminService } from '../services/adminService';
import { toSlug } from '../utils/string';
function buildTree(list){
  const map = {};
  list.forEach(i => map[i.id] = {...i, children: []});
  const roots = [];
  list.forEach(i => {
    if(i.parentId) map[i.parentId]?.children.push(map[i.id]);
    else roots.push(map[i.id]);
  });
  return roots;
}
export default function AdminCategories(){
  const [categories, setCategories] = useState([]);
  const [tree, setTree] = useState([]);
  const [editing, setEditing] = useState(null);
  const [form, setForm] = useState({ name:'', description:'', parentId:null, slug:'', isActive:true, imageFile:null });
useEffect(()=>{ load(); }, []);
  async function load(){
    const list = await AdminService.getCategories();
    setCategories(list);
    setTree(buildTree(list));
  }
function handleDragStart(e, id){
    e.dataTransfer.setData('text/plain', id);
    e.dataTransfer.effectAllowed = 'move';
  }
async function handleDrop(e, targetId){
    const id = Number(e.dataTransfer.getData('text/plain'));
    if(id === targetId) return;
    // basit: drop edilen kategoriyi target'ın parent'ı yap
    await AdminService.updateCategory(id, { parentId: targetId });
    load();
  }
function renderNode(node, depth = 0){
    return (
      <div key={node.id} className="p-2 border rounded mb-2 bg-white">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div draggable onDragStart={(e)=>handleDragStart(e, node.id)} onDragOver={(e)=>e.preventDefault()} onDrop={(e)=>handleDrop(e, node.id)} className="cursor-move">☰</div>
            <div>
              <div className="font-semibold">{node.name}</div>
              <div className="text-sm text-gray-500">/{node.slug}</div>
            </div>
          </div>
<div className="flex items-center gap-2">
            <label className="flex items-center gap-1 text-sm">
              <input type="checkbox" checked={node.isActive} onChange={async ()=>{ await AdminService.updateCategory(node.id, { isActive: !node.isActive }); load(); }} />
              Aktif
            </label>
            <button onClick={()=>{ setEditing(node); setForm({...node}); }} className="text-sm text-blue-600">Düzenle</button>
            <button onClick={async ()=>{ if(confirm('Silinsin mi?')){ await AdminService.deleteCategory(node.id); load(); } }} className="text-sm text-red-600">Sil</button>
          </div>
        </div>
{node.children?.length > 0 && (
          <div className="ml-6 mt-2">
            {node.children.map(ch => renderNode(ch))}
          </div>
        )}
      </div>
    );
  }
const handleChange = (k,v) => setForm(f=>({...f, [k]: v}));
const save = async (e) => {
    e.preventDefault();
    const payload = { name: form.name, description: form.description, parentId: form.parentId, slug: form.slug || toSlug(form.name), isActive: !!form.isActive };
    // image upload handled via FormData if file exists
    if(form.imageFile){
      const fd = new FormData();
      fd.append('image', form.imageFile);
      for(const k in payload) fd.append(k, payload[k]);
      if(editing) await AdminService.updateCategory(editing.id, fd);
      else await AdminService.createCategory(fd);
    } else {
      if(editing) await AdminService.updateCategory(editing.id, payload);
      else await AdminService.createCategory(payload);
    }
    setForm({ name:'', description:'', parentId:null, slug:'', isActive:true, imageFile:null });
    setEditing(null);
    load();
  }
return (
    <div className="p-6 grid grid-cols-1 md:grid-cols-3 gap-6">
      <div className="md:col-span-2">
        <h2 className="text-xl font-bold mb-4">Kategori Ağacı</h2>
        <div>
          {tree.map(root => renderNode(root))}
        </div>
      </div>
<aside className="bg-white p-4 rounded shadow">
        <h3 className="font-semibold mb-2">{editing ? 'Kategori Düzenle' : 'Yeni Kategori'}</h3>
        <form onSubmit={save}>
          <input required placeholder="Ad" value={form.name} onChange={e=>handleChange('name', e.target.value)} className="w-full border p-2 mb-2" />
          <input placeholder="Slug (otomatik oluşturulur)" value={form.slug} onChange={e=>handleChange('slug', e.target.value)} className="w-full border p-2 mb-2" />
          <select value={form.parentId || ''} onChange={e=>handleChange('parentId', e.target.value || null)} className="w-full border p-2 mb-2">
            <option value="">Üst kategori yok</option>
            {categories.map(c => <option value={c.id} key={c.id}>{c.name}</option>)}
          </select>
          <textarea placeholder="Açıklama" value={form.description} onChange={e=>handleChange('description', e.target.value)} className="w-full border p-2 mb-2" />
<div className="mb-2">
            <label className="block mb-1">Görsel</label>
            <input type="file" accept="image/*" onChange={e=>handleChange('imageFile', e.target.files[0])} />
          </div>
<label className="flex items-center gap-2 mb-4">
            <input type="checkbox" checked={form.isActive} onChange={e=>handleChange('isActive', e.target.checked)} /> Aktif
          </label>
<div className="flex gap-2">
            <button className="bg-green-600 text-white px-3 py-2 rounded">Kaydet</button>
            {editing && <button type="button" onClick={()=>{ setEditing(null); setForm({ name:'', description:'', parentId:null, slug:'', isActive:true }); }} className="px-3 py-2 border rounded">İptal</button>}
          </div>
        </form>
      </aside>
    </div>
  );
}*/
