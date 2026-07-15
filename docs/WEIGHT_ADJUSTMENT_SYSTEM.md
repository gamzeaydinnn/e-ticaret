# Ağırlık Farkına Göre Ödeme Sistemi

> **OTORİTER POLİTİKA (11 Temmuz 2026 — önceki “%20 marjlı PreAuth” açıklamalarının yerine geçer)**
>
> - **3DS tutarı = sepet `FinalPrice`** (marj/şişirme yok).
> - KG ürünlerde işlem tipi config ile belirlenir: `PosnetUseAuthForWeightBasedItems`.
>   - `true` (VpnTest/Dev): **Auth** → tartı sonrası **Capt ≤ Auth × 1.20**
>   - `false` (Production varsayılan): **Sale** → checkout’ta tam çekim; tartı farkı karttan otomatik çekilemez (manuel tahsilat).
> - Manuel tartı **yalnız `Preparing`** aşamasında (`WeightBasedWeighingGate`).
> - Sipariş onayı **Admin → Siparişler**; Ağırlık Raporları panelinde **Onayla yok**.
> - Tek banka çekim yolu: `PaymentCaptureService.CapturePaymentAsync` (teslimatta).
> - Politika tek kaynak: `WeightBasedCapturePolicy`, akış ayrımı: `WeightBasedPaymentFlowResolver`.

## Genel Bakış

Kg/gram ürünlerde tahmini ağırlık ile gerçek tartı farkına göre sipariş tutarı güncellenir; banka tahsilatı Auth→Capt veya Sale+manuel akışına göre yapılır.

## İş Akışı (hedef)

```
[Checkout FinalPrice]
    → [3DS Amount = FinalPrice]
    → [Auth (VpnTest) | Sale (Prod)]
    → [Admin Onayla → Confirmed]
    → [Hazırlamaya Başla → Preparing]
    → [Ağırlık Raporları: tartı gir]
    → [Hazır → Kurye → Teslim]
    → [Capt final | Sale’de fark varsa manuel]
```

### 1. Sipariş Oluşturma

- Tahmini ağırlık üzerinden fiyat hesaplanır
- `PreAuthAmount = FinalPrice` (marjsız)
- `TolerancePercentage = 0.20` yalnız Capt aşımı için saklanır

### 2. Tartım (Preparing)

- Admin/mağaza görevlisi gerçek gramı girer (`PATCH .../manual-weight`)
- `ActualPrice` / `PriceDifference` / `FinalAmount` güncellenir
- Auth×1.20 aşımı veya Sale farkı → UI’da manuel tahsilat uyarısı

### 3. Ödeme Finalizasyonu

| Akış | Davranış |
|------|----------|
| **AuthCapture** | Teslimatta `Capt(min(final, Auth×1.20))`; aşım → `DeliveryPaymentPending` |
| **SaleImmediate** | Checkout’ta çekilmiş; fazla gram → manuel tahsilat; Capt overage yok |

### 4. Admin Onayı

- Sipariş onayı: Admin sipariş ekranı
- WeightReport `/approve` kapalı (çift çekim önlemi)
- Auth×1.20 üstü / Sale farkı: `DeliveryPaymentPending` + admin müdahalesi

## Durum Akışı

```
KG Auth:  Pending → PreAuthorized → Confirmed → Preparing → Ready → … → Delivered (+Capt)
KG Sale:  Pending → Paid → Confirmed → Preparing → Ready → … → Delivered (Sale; fark manuel)
```

**WeightAdjustmentStatus:** `PendingWeighing` → tartı sonrası `Weighed` / `NoDifference` / `PendingAdditionalPayment` / …

## API (canlı yol)

| Method | Endpoint | Açıklama |
|--------|----------|----------|
| PATCH | `/api/weight-adjustment/admin/orders/{orderId}/items/{orderItemId}/manual-weight` | Preparing’de tartı |
| POST | `/api/store-attendant/orders/{id}/confirm` | Sipariş onayla |
| POST | `/api/store-attendant/orders/{id}/start-preparing` | Hazırlamaya başla |
| POST | `/api/store-attendant/orders/{id}/mark-ready` | Hazır |

## Frontend

- `/admin/weight-management` → `StoreAttendantDashboard` (`weightOnly`) — yalnız Preparing tartı
- `/admin/orders` — Onayla / Hazırlamaya Başla

## Konfigürasyon

```json
{
  "PaymentSettings": {
    "PosnetUseAuthForWeightBasedItems": true
  }
}
```

- VpnTest / Development: `true`
- Production: banka Auth yetkisi teyit edilmeden `false` bırakın

## İlgili kod

- `WeightBasedCapturePolicy`, `WeightBasedWeighingGate`, `WeightBasedPaymentFlowResolver`
- `OrderManager.CheckoutAsync`, `PaymentsControllers.PosnetInitiate3DSecure`
- `PaymentCaptureService.CapturePaymentAsync`, `CourierOrderManager.MarkDeliveredAsync`
