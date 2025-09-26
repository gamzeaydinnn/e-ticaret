import * as React from "react";
import { Admin, Resource, ListGuesser, EditGuesser } from "react-admin";
import simpleRestProvider from "ra-data-simple-rest";

// Backend API endpoint (örnek: ASP.NET Core'da api/admin/users gibi)
const dataProvider = simpleRestProvider("http://localhost:5000/api");

const AdminApp = () => (
  <Admin dataProvider={dataProvider}>
    {/* Kullanıcılar */}
    <Resource name="users" list={ListGuesser} edit={EditGuesser} />
    
    {/* Ürünler */}
    <Resource name="products" list={ListGuesser} edit={EditGuesser} />
    
    {/* Siparişler */}
    <Resource name="orders" list={ListGuesser} edit={EditGuesser} />
  </Admin>
);

export default AdminApp;
