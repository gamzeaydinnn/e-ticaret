# İzin Eşleştirme Tablosu

Bu doküman frontend PERMISSIONS sabitleri, backend Permissions.cs özellikleri ve veritabanı değerleri arasındaki eşleştirmeyi gösterir.

## Adlandırma Standardı

| Katman            | Format               | Örnek          |
| ----------------- | -------------------- | -------------- |
| Frontend Constant | SCREAMING_SNAKE_CASE | `REPORTS_VIEW` |
| Backend Property  | PascalCase           | `Reports.View` |
| Database Value    | lowercase.action     | `reports.view` |

---

## Dashboard Modülü

| Frontend Constant      | Backend Property             | Database Value         |
| ---------------------- | ---------------------------- | ---------------------- |
| `DASHBOARD_VIEW`       | `Dashboard.View`             | `dashboard.view`       |
| `DASHBOARD_STATISTICS` | `Dashboard.ViewStatistics`   | `dashboard.statistics` |
| `DASHBOARD_REVENUE`    | `Dashboard.ViewRevenueChart` | `dashboard.revenue`    |

## Products (Ürünler) Modülü

| Frontend Constant  | Backend Property         | Database Value     |
| ------------------ | ------------------------ | ------------------ |
| `PRODUCTS_VIEW`    | `Products.View`          | `products.view`    |
| `PRODUCTS_CREATE`  | `Products.Create`        | `products.create`  |
| `PRODUCTS_UPDATE`  | `Products.Update`        | `products.update`  |
| `PRODUCTS_DELETE`  | `Products.Delete`        | `products.delete`  |
| `PRODUCTS_STOCK`   | `Products.ManageStock`   | `products.stock`   |
| `PRODUCTS_PRICING` | `Products.ManagePricing` | `products.pricing` |
| `PRODUCTS_IMPORT`  | `Products.Import`        | `products.import`  |
| `PRODUCTS_EXPORT`  | `Products.Export`        | `products.export`  |

## Categories (Kategoriler) Modülü

| Frontend Constant   | Backend Property    | Database Value      |
| ------------------- | ------------------- | ------------------- |
| `CATEGORIES_VIEW`   | `Categories.View`   | `categories.view`   |
| `CATEGORIES_CREATE` | `Categories.Create` | `categories.create` |
| `CATEGORIES_UPDATE` | `Categories.Update` | `categories.update` |
| `CATEGORIES_DELETE` | `Categories.Delete` | `categories.delete` |

## Orders (Siparişler) Modülü

| Frontend Constant       | Backend Property          | Database Value          |
| ----------------------- | ------------------------- | ----------------------- |
| `ORDERS_VIEW`           | `Orders.View`             | `orders.view`           |
| `ORDERS_DETAILS`        | `Orders.ViewDetails`      | `orders.details`        |
| `ORDERS_STATUS`         | `Orders.UpdateStatus`     | `orders.status`         |
| `ORDERS_CANCEL`         | `Orders.Cancel`           | `orders.cancel`         |
| `ORDERS_REFUND`         | `Orders.ProcessRefund`    | `orders.refund`         |
| `ORDERS_ASSIGN_COURIER` | `Orders.AssignCourier`    | `orders.assign_courier` |
| `ORDERS_CUSTOMER_INFO`  | `Orders.ViewCustomerInfo` | `orders.customer_info`  |
| `ORDERS_EXPORT`         | `Orders.Export`           | `orders.export`         |

## Users (Kullanıcılar) Modülü

| Frontend Constant | Backend Property          | Database Value    |
| ----------------- | ------------------------- | ----------------- |
| `USERS_VIEW`      | `Users.View`              | `users.view`      |
| `USERS_CREATE`    | `Users.Create`            | `users.create`    |
| `USERS_UPDATE`    | `Users.Update`            | `users.update`    |
| `USERS_DELETE`    | `Users.Delete`            | `users.delete`    |
| `USERS_ROLES`     | `Users.ManageRoles`       | `users.roles`     |
| `USERS_SENSITIVE` | `Users.ViewSensitiveData` | `users.sensitive` |
| `USERS_EXPORT`    | `Users.Export`            | `users.export`    |

## Roles (Roller) Modülü

| Frontend Constant   | Backend Property          | Database Value      |
| ------------------- | ------------------------- | ------------------- |
| `ROLES_VIEW`        | `Roles.View`              | `roles.view`        |
| `ROLES_CREATE`      | `Roles.Create`            | `roles.create`      |
| `ROLES_UPDATE`      | `Roles.Update`            | `roles.update`      |
| `ROLES_DELETE`      | `Roles.Delete`            | `roles.delete`      |
| `ROLES_PERMISSIONS` | `Roles.ManagePermissions` | `roles.permissions` |

## Campaigns (Kampanyalar) Modülü

| Frontend Constant  | Backend Property   | Database Value     |
| ------------------ | ------------------ | ------------------ |
| `CAMPAIGNS_VIEW`   | `Campaigns.View`   | `campaigns.view`   |
| `CAMPAIGNS_CREATE` | `Campaigns.Create` | `campaigns.create` |
| `CAMPAIGNS_UPDATE` | `Campaigns.Update` | `campaigns.update` |
| `CAMPAIGNS_DELETE` | `Campaigns.Delete` | `campaigns.delete` |

## Coupons (Kuponlar) Modülü

| Frontend Constant | Backend Property | Database Value   |
| ----------------- | ---------------- | ---------------- |
| `COUPONS_VIEW`    | `Coupons.View`   | `coupons.view`   |
| `COUPONS_CREATE`  | `Coupons.Create` | `coupons.create` |
| `COUPONS_UPDATE`  | `Coupons.Update` | `coupons.update` |
| `COUPONS_DELETE`  | `Coupons.Delete` | `coupons.delete` |

## Couriers (Kuryeler) Modülü

| Frontend Constant | Backend Property           | Database Value         |
| ----------------- | -------------------------- | ---------------------- |
| `COURIERS_VIEW`   | `Couriers.View`            | `couriers.view`        |
| `COURIERS_CREATE` | `Couriers.Create`          | `couriers.create`      |
| `COURIERS_UPDATE` | `Couriers.Update`          | `couriers.update`      |
| `COURIERS_DELETE` | `Couriers.Delete`          | `couriers.delete`      |
| `COURIERS_ASSIGN` | `Couriers.AssignOrders`    | `couriers.assign`      |
| -                 | `Couriers.ViewPerformance` | `couriers.performance` |

## Shipping (Kargo/Teslimat) Modülü

| Frontend Constant          | Backend Property                | Database Value      |
| -------------------------- | ------------------------------- | ------------------- |
| `SHIPPING_VIEW`            | `Shipping.ViewPendingShipments` | `shipping.pending`  |
| `SHIPPING_UPDATE_STATUS`   | `Shipping.UpdateTrackingNumber` | `shipping.tracking` |
| `SHIPPING_TRACK`           | `Shipping.MarkAsShipped`        | `shipping.ship`     |
| `SHIPPING_WEIGHT_APPROVAL` | `Shipping.MarkAsDelivered`      | `shipping.deliver`  |

## Reports (Raporlar) Modülü

| Frontend Constant   | Backend Property        | Database Value      |
| ------------------- | ----------------------- | ------------------- |
| `REPORTS_VIEW`      | `Reports.View`          | `reports.view`      |
| `REPORTS_SALES`     | `Reports.ViewSales`     | `reports.sales`     |
| `REPORTS_INVENTORY` | `Reports.ViewInventory` | `reports.inventory` |
| `REPORTS_CUSTOMERS` | `Reports.ViewCustomers` | `reports.customers` |
| -                   | `Reports.ViewFinancial` | `reports.financial` |
| `REPORTS_WEIGHT`    | `Reports.ViewWeight`    | `reports.weight`    |
| `REPORTS_EXPORT`    | `Reports.Export`        | `reports.export`    |

## Banners (Bannerlar) Modülü

| Frontend Constant | Backend Property | Database Value   |
| ----------------- | ---------------- | ---------------- |
| `BANNERS_VIEW`    | `Banners.View`   | `banners.view`   |
| `BANNERS_CREATE`  | `Banners.Create` | `banners.create` |
| `BANNERS_UPDATE`  | `Banners.Update` | `banners.update` |
| `BANNERS_DELETE`  | `Banners.Delete` | `banners.delete` |

## Brands (Markalar) Modülü

| Frontend Constant | Backend Property | Database Value  |
| ----------------- | ---------------- | --------------- |
| `BRANDS_VIEW`     | `Brands.View`    | `brands.view`   |
| `BRANDS_CREATE`   | `Brands.Create`  | `brands.create` |
| `BRANDS_UPDATE`   | `Brands.Update`  | `brands.update` |
| `BRANDS_DELETE`   | `Brands.Delete`  | `brands.delete` |

## Settings (Ayarlar) Modülü

| Frontend Constant | Backend Property         | Database Value     |
| ----------------- | ------------------------ | ------------------ |
| `SETTINGS_VIEW`   | `Settings.View`          | `settings.view`    |
| `SETTINGS_UPDATE` | `Settings.Update`        | `settings.update`  |
| `SETTINGS_SYSTEM` | `Settings.System`        | `settings.system`  |
| -                 | `Settings.ManagePayment` | `settings.payment` |
| -                 | `Settings.ManageSms`     | `settings.sms`     |
| -                 | `Settings.ManageEmail`   | `settings.email`   |

## Logs (Loglar) Modülü

| Frontend Constant | Backend Property | Database Value |
| ----------------- | ---------------- | -------------- |
| `LOGS_VIEW`       | `Logs.View`      | `logs.view`    |
| `LOGS_AUDIT`      | `Logs.ViewAudit` | `logs.audit`   |
| `LOGS_ERROR`      | `Logs.ViewError` | `logs.error`   |
| -                 | `Logs.ViewSync`  | `logs.sync`    |
| `LOGS_EXPORT`     | `Logs.Export`    | `logs.export`  |

---

## Özet İstatistikler

| Modül      | Frontend | Backend | Database |
| ---------- | -------- | ------- | -------- |
| Dashboard  | 3        | 3       | 3        |
| Products   | 8        | 8       | 8        |
| Categories | 4        | 4       | 4        |
| Orders     | 8        | 8       | 8        |
| Users      | 7        | 7       | 7        |
| Roles      | 5        | 5       | 5        |
| Campaigns  | 4        | 4       | 4        |
| Coupons    | 4        | 4       | 4        |
| Couriers   | 5        | 6       | 6        |
| Shipping   | 4        | 4       | 4        |
| Reports    | 6        | 7       | 7        |
| Banners    | 4        | 4       | 4        |
| Brands     | 4        | 4       | 4        |
| Settings   | 3        | 6       | 6        |
| Logs       | 4        | 5       | 5        |
| **Toplam** | **73**   | **79**  | **79**   |

---

## Notlar

1. Frontend'de tanımlı olmayan backend izinleri `-` ile gösterilmiştir
2. Tüm izin değerleri case-sensitive'dir
3. Yeni izin eklerken her üç katmanı da güncellemeyi unutmayın
4. `seed-rbac-data.sql` dosyasında veritabanı değerleri kullanılır
