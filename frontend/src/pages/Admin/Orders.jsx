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
