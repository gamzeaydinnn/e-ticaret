import * as React from "react";
import { Admin, Resource, List, Datagrid, TextField, NumberField } from "react-admin";
import simpleRestProvider from "ra-data-simple-rest";

const dataProvider = simpleRestProvider("http://localhost:5000/api/admin/orders");

export const Orders = () => (
    <Admin dataProvider={dataProvider}>
        <Resource name="orders" list={OrderList} />
    </Admin>
);

const OrderList = (props) => (
    <List {...props}>
        <Datagrid>
            <TextField source="id" />
            <TextField source="userId" />
            <NumberField source="totalPrice" />
            <TextField source="status" />
        </Datagrid>
    </List>
);

/*src/admin/AdminOrders.jsx
import React, { useEffect, useState } from 'react';
import { AdminService } from '../services/adminService';
export default function AdminOrders(){
  const [orders, setOrders] = useState([]);
  const [selected, setSelected] = useState(null);
useEffect(()=>{ load(); }, []);
  const load = ()=> AdminService.listOrders().then(setOrders).catch(()=>{});
const updateStatus = async (orderId, status)=>{ await AdminService.updateOrderStatus(orderId, status); load(); }
return (
    <div className="p-6">
      <h1 className="text-xl font-bold mb-4">Siparişler</h1>
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div className="md:col-span-2">
          {orders.map(o => (
            <div key={o.id} className="p-3 border rounded mb-2 flex justify-between items-center">
              <div>
                <div className="font-semibold">Sipariş #{o.id} — ₺{o.totalAmount}</div>
                <div className="text-sm text-gray-600">{o.customerName} — {o.status}</div>
              </div>
              <div className="flex gap-2">
                <button onClick={()=>setSelected(o)} className="px-3 py-1 border rounded">Detay</button>
              </div>
            </div>
          ))}
        </div>
<aside>
          <h2 className="font-semibold mb-2">Detay</h2>
          {selected ? (
            <div className="bg-white p-3 rounded shadow">
              <div className="mb-2">#{selected.id} - {selected.customerName}</div>
              <div className="mb-2">Adres: {selected.address}</div>
              <div className="mb-2">Durum: {selected.status}</div>
<div className="flex gap-2 mt-3">
                <button onClick={()=>updateStatus(selected.id, 'Preparing')} className="px-3 py-1 bg-yellow-400 rounded">Hazırlanıyor</button>
                <button onClick={()=>updateStatus(selected.id, 'OnTheWay')} className="px-3 py-1 bg-blue-400 rounded">Yolda</button>
                <button onClick={()=>updateStatus(selected.id, 'Delivered')} className="px-3 py-1 bg-green-600 text-white rounded">Teslim Edildi</button>
              </div>
            </div>
          ) : (
            <div>Bir sipariş seçin.</div>
          )}
        </aside>
      </div>
    </div>
  );
}
*/