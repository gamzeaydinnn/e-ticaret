# Ödeme Akışı ve 3D Secure / Finansallaştırma Analizi

> **OTORİTER POLİTİKA (11 Temmuz 2026):** 3DS tutarı = sepet `FinalPrice` (marj yok).
> Capt ≤ Auth × 1.20. Auth flag: `PosnetUseAuthForWeightBasedItems` (VpnTest=true, Production=false).
> Eski “%20 şişirilmiş PreAuth” örnekleri **geçersizdir**.

Banka API (POSNET 3sapi) ve `PaymentsController`, `YapiKrediPosnetService`, `Posnet3DSecureCallbackHandler`,
`WeightBasedCapturePolicy`, `WeightBasedPaymentFlowResolver` ile akış özeti:

---

## 1. Akış Karşılaştırması (Normal vs. KG Bazlı Sipariş)

POSNET 3D Secure OOS üzerinden çalışır. Fark: **txnType** ve Capt aşaması.

### A. Normal Sipariş (`txnType = "Sale"`)
1. Sepet tutarı birebir (örn. 100 TL).
2. OOS `Amount = 10000` kuruş → TranData Sale → anında çekim (`CaptureStatus.Success`).

### B. KG — Auth açık (`PosnetUseAuthForWeightBasedItems=true`)
1. Sepet 100 TL → **Auth = 100 TL** (marj yok).
2. Callback: `PreAuthorized` / `Authorized` / `CaptureStatus.Pending`.
3. Preparing’de tartı (örn. 1100 g → 110 TL).
4. Teslimatta `Capt(110)` — limit `Auth×1.20=120`.
5. final &lt; auth → yalnız kısmi Capt (ayrı Return yok).
6. final &gt; Auth×1.20 → clamp + `DeliveryPaymentPending`.

### C. KG — Sale (`PosnetUseAuthForWeightBasedItems=false`)
1. Checkout’ta Sale ile sepet tutarı çekilir.
2. Tartı farkı karttan Capt ile çekilemez → manuel tahsilat (`WEIGHT_OVERAGE_REQUIRES_MANUAL_COLLECTION`).

---

## 2. Precision / Kuruş

- KG: `bankAmount = PreAuthAmount` (= FinalPrice).
- Normal: `bankAmount = FinalPrice` (±1 TL istemci kontrolü).
- Kuruş: `((int)(bankAmount * 100))` — decimal base-10, kayıp yok.

---

## 3. MAC

MAC: `XID + Amount + Currency + MerchantId + PosnetId`.
Callback, bankaya gönderilen `PreAuthAmount` üzerinden doğrular (sepet = PreAuth; şişirme yok).

---

## 4. Capt / HostLogKey

1. Auth → `PreAuthHostLogKey`; Capt → yeni HostLogKey (iade zinciri).
2. final &lt; auth: kısmi Capt; kalan bloke bankada serbest.
3. Sale + overage: Capt yok; manuel tahsilat.
4. `tranDateRequired=1` Capt XML’de (POSNET uyumu).

---

## Sonuç

- 3DS = sepet; Capt ≤ Auth×1.20; Sale/Auth ayrımı kodda net.
- Production Auth açmadan önce banka Auth yetkisi teyit edilmeli (`0058` riski).
- VpnTest: Auth açık; Production: varsayılan kapalı.
