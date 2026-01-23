# 🏋️ Ağırlık Farkına Göre Ödeme Sistemi - API Referansı

## Kimlik Doğrulama

Tüm API endpoint'leri JWT Bearer token gerektirir:

```
Authorization: Bearer <token>
```

---

## Kurye API'leri

### POST `/api/courier/weight-report`

Yeni ağırlık bildirimi oluşturur.

**Yetki:** `Courier` rolü

**Request Body:**

```json
{
  "orderId": 123,
  "weightReports": [
    {
      "orderItemId": 456,
      "actualWeight": 1050,
      "weightUnit": 1
    }
  ]
}
```

**Response (200):**

```json
{
  "success": true,
  "message": "Ağırlık raporu başarıyla kaydedildi",
  "data": {
    "orderId": 123,
    "totalDifference": 25.5,
    "requiresAdminApproval": false,
    "adjustments": [
      {
        "adjustmentId": 1,
        "productName": "Elma",
        "estimatedWeight": 1000,
        "actualWeight": 1050,
        "priceDifference": 12.5,
        "status": "PendingAdditionalPayment"
      }
    ]
  }
}
```

**Hata Kodları:**

- `400`: Geçersiz istek
- `401`: Yetkisiz
- `404`: Sipariş bulunamadı
- `409`: Zaten tartım yapılmış

---

### GET `/api/courier/pending-weights`

Kuryenin bekleyen tartımlarını listeler.

**Yetki:** `Courier` rolü

**Response (200):**

```json
{
  "success": true,
  "data": [
    {
      "orderId": 123,
      "orderNumber": "ORD-2026-000123",
      "customerName": "Ahmet Yılmaz",
      "items": [
        {
          "orderItemId": 456,
          "productName": "Elma",
          "estimatedWeight": 2000,
          "weightUnit": "Kilogram"
        }
      ]
    }
  ]
}
```

---

### GET `/api/courier/orders/{orderId}/weight-status`

Siparişin ağırlık durumunu getirir.

**Yetki:** `Courier` rolü

**Parameters:**

- `orderId` (path): Sipariş ID

**Response (200):**

```json
{
  "success": true,
  "data": {
    "orderId": 123,
    "orderStatus": "OutForDelivery",
    "weightStatus": "Weighed",
    "adjustments": [
      {
        "itemId": 456,
        "productName": "Elma",
        "status": "PendingAdditionalPayment",
        "priceDifference": 25.5
      }
    ]
  }
}
```

---

## Admin API'leri

### GET `/api/admin/weight-adjustments`

Tüm ağırlık ayarlamalarını listeler.

**Yetki:** `Admin` rolü

**Query Parameters:**

- `page` (int): Sayfa numarası (varsayılan: 1)
- `pageSize` (int): Sayfa boyutu (varsayılan: 20)
- `status` (string): Durum filtresi
- `startDate` (date): Başlangıç tarihi
- `endDate` (date): Bitiş tarihi

**Response (200):**

```json
{
  "success": true,
  "data": {
    "items": [...],
    "totalCount": 150,
    "page": 1,
    "pageSize": 20,
    "totalPages": 8
  }
}
```

---

### GET `/api/admin/weight-adjustments/pending`

Onay bekleyen ayarlamaları listeler.

**Yetki:** `Admin` rolü

**Response (200):**

```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "orderId": 123,
      "orderNumber": "ORD-2026-000123",
      "productName": "Elma",
      "estimatedWeight": 1000,
      "actualWeight": 1500,
      "differencePercent": 50,
      "priceDifference": 125.0,
      "courierName": "Ali Veli",
      "createdAt": "2026-01-20T10:30:00Z"
    }
  ]
}
```

---

### POST `/api/admin/weight-adjustments/{id}/approve`

Ağırlık ayarlamasını onaylar.

**Yetki:** `Admin` rolü

**Parameters:**

- `id` (path): Ayarlama ID

**Request Body:**

```json
{
  "comment": "Kurye fotoğrafı ile doğrulandı"
}
```

**Response (200):**

```json
{
  "success": true,
  "message": "Ayarlama onaylandı",
  "data": {
    "id": 1,
    "status": "PendingAdditionalPayment",
    "nextStep": "Müşteriden ek ödeme bekleniyor"
  }
}
```

---

### POST `/api/admin/weight-adjustments/{id}/reject`

Ağırlık ayarlamasını reddeder.

**Yetki:** `Admin` rolü

**Parameters:**

- `id` (path): Ayarlama ID

**Request Body:**

```json
{
  "reason": "Tartı fotoğrafı net değil, tekrar tartım gerekiyor"
}
```

**Response (200):**

```json
{
  "success": true,
  "message": "Ayarlama reddedildi",
  "data": {
    "id": 1,
    "status": "RejectedByAdmin"
  }
}
```

---

## Müşteri API'leri

### GET `/api/customer/weight-adjustments`

Müşterinin ağırlık ayarlamalarını listeler.

**Yetki:** `Customer` rolü

**Response (200):**

```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "orderId": 123,
      "orderNumber": "ORD-2026-000123",
      "productName": "Elma",
      "estimatedWeight": 1000,
      "actualWeight": 1100,
      "priceDifference": 25.0,
      "status": "PendingAdditionalPayment",
      "statusText": "Ek ödeme bekleniyor"
    }
  ]
}
```

---

### POST `/api/customer/weight-adjustments/{id}/pay`

Ek ödeme işlemini başlatır.

**Yetki:** `Customer` rolü

**Parameters:**

- `id` (path): Ayarlama ID

**Request Body:**

```json
{
  "paymentMethod": "CreditCard",
  "cardToken": "tok_xxxx"
}
```

**Response (200):**

```json
{
  "success": true,
  "message": "Ödeme başarılı",
  "data": {
    "transactionId": "TXN-2026-001234",
    "amount": 25.0,
    "status": "Completed"
  }
}
```

**Hata Kodları:**

- `400`: Geçersiz ödeme bilgileri
- `402`: Ödeme başarısız
- `404`: Ayarlama bulunamadı

---

## DTO Şemaları

### WeightReportDto

```typescript
interface WeightReportDto {
  orderItemId: number;
  actualWeight: number;
  weightUnit: WeightUnit;
}
```

### WeightAdjustmentDto

```typescript
interface WeightAdjustmentDto {
  id: number;
  orderId: number;
  orderItemId: number;
  productId: number;
  productName: string;

  estimatedWeight: number;
  actualWeight: number;
  weightDifference: number;
  differencePercent: number;

  pricePerUnit: number;
  estimatedPrice: number;
  actualPrice: number;
  priceDifference: number;

  status: WeightAdjustmentStatus;
  statusText: string;

  courierName?: string;
  weighedAt?: string;

  adminName?: string;
  adminComment?: string;
  adminActionAt?: string;

  paymentStatus: PaymentStatus;
  paymentTransactionId?: string;

  createdAt: string;
}
```

### WeightUnit Enum

```typescript
enum WeightUnit {
  Gram = 0,
  Kilogram = 1,
}
```

### WeightAdjustmentStatus Enum

```typescript
enum WeightAdjustmentStatus {
  NotApplicable = 0,
  PendingWeighing = 1,
  Weighed = 2,
  NoDifference = 3,
  PendingAdditionalPayment = 4,
  PendingRefund = 5,
  Completed = 6,
  PendingAdminApproval = 7,
  RejectedByAdmin = 8,
  Failed = 9,
}
```

---

## Webhook Events (Opsiyonel)

Sistem, önemli olaylarda webhook bildirimleri gönderebilir:

### `weight.reported`

```json
{
  "event": "weight.reported",
  "timestamp": "2026-01-20T10:30:00Z",
  "data": {
    "orderId": 123,
    "adjustmentId": 1,
    "difference": 25.5
  }
}
```

### `weight.approved`

```json
{
  "event": "weight.approved",
  "timestamp": "2026-01-20T11:00:00Z",
  "data": {
    "adjustmentId": 1,
    "approvedBy": "admin@example.com"
  }
}
```

### `weight.payment_completed`

```json
{
  "event": "weight.payment_completed",
  "timestamp": "2026-01-20T12:00:00Z",
  "data": {
    "adjustmentId": 1,
    "transactionId": "TXN-2026-001234",
    "amount": 25.5
  }
}
```

---

## Rate Limiting

| Endpoint Grubu | Limit          |
| -------------- | -------------- |
| Kurye API      | 100 req/dakika |
| Admin API      | 200 req/dakika |
| Müşteri API    | 60 req/dakika  |

---

## Hata Yanıt Formatı

Tüm hatalar aşağıdaki formatta döner:

```json
{
  "success": false,
  "message": "Hata açıklaması",
  "errors": [
    {
      "field": "actualWeight",
      "message": "Ağırlık 0'dan büyük olmalıdır"
    }
  ],
  "errorCode": "INVALID_WEIGHT"
}
```
