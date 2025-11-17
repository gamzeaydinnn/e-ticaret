**Mimari İnceleme ve Eksikler (Genel Özet)**

Bu dosya proje genelindeki eksiklikleri, tutarsızlıkları ve ileride ele alınması gereken iyileştirmeleri listeler. Hem backend, hem frontend, hem de DB seviyesinde eksik/yarım kalmış, mantık hatası olabilecek alanlar aşağıdadır.

**Kısa Önceliklendirme**
- **Yüksek:** Ödeme + stok bütünlüğü (transaction/idempotency), kimlik doğrulama, temel güvenlik (secret yönetimi), kritik model eksiklikleri.
- **Orta:** API doğrulama/DTO, testler, entegrasyonlar (kargo, SMS/eposta), monitoring.
- **Düşük:** Performans optimizasyonları, ön yüz UX iyileştirmeleri ve SEO.

**Entities / DB (Model seviyesinde eksikler ve öneriler)**
- **Order model tutarsızlıkları:** `Order` içinde hem `Items` hem `OrderItems` koleksiyonları var — duplicate property. Bu kafa karıştırır; tek bir ilişki kullanın. Ayrıca `TotalPrice` var ama ödeme durumu (`PaymentStatus`), rezervasyon ID's (ör. `ReservationId` GUID?), fatura/adres normalizasyonu (Address tablosu/`AddressId`) yok.
- **Normalizasyon:** `ShippingAddress` gibi string'ler doğrudan entityde saklanmış. Adresleri normalize edip `Addresses` tablosu/`AddressId` ile ilişkilendirin — bir kullanıcı birden fazla adrese sahip olabilir.
- **Ödeme tablosu eksik:** Ödeme işlemleri için ayrı `Payments` tablosu (provider, amount, currency, providerTransactionId, PaymentStatus, RefundedAmount, CreatedAt, GatewayResponse) yok. Refund, chargeback, partial refund mantığı eksik.
- **Sipariş durum geçmişi (audit):** `OrderStatus` enumu var ama geçmiş tutulmuyor. `OrderStatusHistory` tablosu (kimin, hangi zamanda, neden) ekleyin.
- **Stok rezervasyonu ve tutarlılık:** Projede `StockReservation` referansları var gibi görünüyor. Ancak stok tutarlılığı için rezervasyon süresi, expirations, rollback mantığı, cleanup job ve distributed lock/optimistic concurrency yok veya eksik.
- **Concurrency & Tekrarlanabilirlik:** Ödeme + stok decrement işleminde transaction ve idempotency key yok. Aynı ödeme webhook'ı birden çok gelirse double-charge riski var.
- **Index & Performans:** Önemli sorgular (OrderNumber, UserId, CreatedAt, foreign keyler) için eksik indeksler olabilir. Büyük tablolar için composite index planlayın.
- **Audit ve Soft Delete:** `BaseEntity` yapısında `CreatedAt`, `UpdatedAt`, `DeletedAt` varsa iyi; yoksa ekleyin. Soft delete ve audit trail eksikse eklenmeli.

**Backend (API, servisler, repository, mantık)**
- **DTO / Validation:** Controller'larda gelen DTO'lar için kapsamlı validation (FluentValidation veya data annotations) eksik olabilir. `OrdersController` örneğinde `dto.OrderItems` kontrolü var, ama daha kapsamlı validasyon ve model binding hataları yönetimi gerekiyor.
- **Transaction boundary:** Checkout akışı (create order, reserve stock, create payment, confirm payment, reduce stock, mark order paid) için tekil transaction ya da saga pattern/kompensasyon mekanizması yok.
- **Idempotency:** Payment webhook ve checkout çağrıları için idempotency-key header desteği yok — tekrar eden çağrılarda güvenli davranış eksik.
- **Error handling & HTTP responses:** Tutarlı hata dönüşleri (problem details), merkezi exception handling, spesifik hata kodları eksik veya düzensiz.
- **Logging & Telemetry:** Critical path (payment, stock operations, courier callbacks) için yapılandırılmış loglama (correlation id, request id) ve telemetry (Application Insights/Prometheus) eksik.
- **Service katman ayrımı:** Controller içinde iş mantığı varsa service katmanı eksik/karışmış olabilir. İş mantığını servislerde toplayın, controller sadece orchestration/validation yapsın.
- **Background jobs:** Stok temizleme, rezervasyon timeouts, retry mekanizmaları, webhook retry scheduler gibi job'lar `StockReservationCleanupJob` eklenmiş olabilir — bu doğru ama kapsam ve hataya dayanıklılık kontrol edilmeli.
- **Event-driven mimari:** Sipariş durumu, stok değişikliği ve ödeme olayları için domain event / message bus (RabbitMQ/Kafka) yoksa ölçekleme ve dış sistem entegrasyonları zorlaşır.
- **Payment provider abstraction:** Payment gateway entegrasyonları için soyutlama, test double ve simulasyon yoksa riskli.

**Frontend (eksik/yarım kalan akışlar ve iyileştirmeler)**
- **Checkout akışı:** Tam bir checkout flow (adres yönetimi, kargo seçenekleri ve fiyatları, ödeme sayfası, 3D Secure redirect, ödeme başarılı/başarısız geri dönüşleri, sipariş özeti, idempotency) eksik olabilir.
- **Kullanıcı hesap & oturum:** Sosyal login, password reset, e‑posta doğrulama, adres defteri, sipariş geçmişi sayfası, kullanıcı profil sayfası eksikleri olabilir.
- **Cart & stok görünürlüğü:** Sepette ürünün stok kontrolü, snapshot fiyat, kupon uygulama, vergi ve kargo hesaplama anlık mantığı net değil.
- **Order tracking:** Siparişin durumu, takip numarası, kurye güncellemeleri, bildirimler yok veya yetersiz.
- **Admin tarafı:** Ürün yönetimi, stok yönetimi, sipariş yönetimi, iade yönetimi, raporlama, kullanıcı yetkilendirme (RBAC) tam değilse tamamlanmalı.
- **İ18n ve locale:** `Currency` backend'de var ama frontend'de çoklu dil/para formatlama, tarih formatları, RTL/locale eksik olabilir.
- **E2E ve UI testleri:** Critical flows için Cypress/Playwright ile e2e testleri eksik.

**Entegrasyonlar (kargo, ödeme, bildirimler)**
- **Kargo sağlayıcıları:** Kargo API adaptörleri, rate-limits, test/sandbox, signature doğrulama ve webhook işleme (retries, verification) kontrol edilmeli.
- **Ödeme sağlayıcıları:** Gateway'ler (iyzico/stripe/paytr vb.) için sandbox, webhook signature, retry, chargeback handling ve settlement reconciliation eksik olabilir.
- **Mail & SMS:** Sipariş onayı, kargo bildirimi, 2FA için mail/SMS entegrasyonu yoksa ekleyin. Queue ile gönderim ve retry mantığı olmalı.
- **3. parti microservice'ler:** Harici servis çağrıları için circuit breaker, timeout ve bulkhead pattern'leri yoksa implement edin.

**Güvenlik**
- **Secret yönetimi:** API key, payment secret, SMTP password vs. `.env` veya config içinde açıksa, gizli yönetimi (vault/KeyVault) yok.
- **Auth & RBAC:** JWT expiry, refresh token, revoke, role-based access control (admin vs user vs courier) eksik veya yetersiz.
- **Input validation & SQL injection:** ORM kullanılıyor olabilir, ama raw SQL varsa parametrize kontrolü; ayrıca XSS/CSRF korumaları frontend/backend uyumlu olmalı.
- **Rate limiting:** API için rate limit yoksa brute-force/DoS riski var.

**Test / CI / CD**
- **Unit & Integration tests:** Services, repositories ve critical business logic için test eksikliği. Payment ve courier entegrasyonları için integration test harness gerekli.
- **Pipeline:** Otomatik build, test ve deploy pipeline (YAML/GitHub Actions/ Azure DevOps) görünmüyorsa ekleyin.
- **DB migrations:** Migration script'leri (EF Migrations veya SQL) ve migration yönetimi (versioning) kontrol edilmeli.

**Monitoring / Observability**
- **Centralized logging:** Correlation id, request id, structured logs, error levels ve log shipping eksik.
- **Metrics & Alerts:** Error rate, latency, payment failure rate, stock reservation failures için metrik ve alarmlar yok.

**Diğer (performans / UX / ops)**
- **Cache:** Ürün listeleri ve katalog için cache (Redis) ve cache invalidation stratejisi yoksa eklenmeli.
- **Pagination & search:** Large dataset için pagination, full-text search (Elastic, DB based), faceted search eksik olabilir.
- **Static assets & CDN:** Frontend için CDN, asset hashing, cache-control eksikleri performansı etkiler.

**Spesifik Kod Bazlı Gözlemler (bulduğum örnekler - ayrıntıları kendi kod tabanınızda kontrol edin)**
- `Order.cs`: Duplicate/karışık collection isimleri (`Items` vs `OrderItems`) — netleştirin.
- `Order.cs`: `TotalPrice` alanı var ancak ödeme durumu (`PaymentStatus`) ve ödeme sağlayıcıya ait referanslar yok.
- `Order.cs`: `ShippingCost` default 30m olarak sabit; bu business rule olarak sabitlenmemeli. Kargo hesaplama dinamik olmalı.
- Controller'larda DTO validation ve error handling merkezi değilse hatalara açık.

**Önerilen İlk 8 Adım (uygulaması pratik ve öncelikli)**
1. `Order` modelini normalize edin: tek `OrderItems`, `ReservationId` (GUID?), `PaymentStatus` ekleyin ve `AddressId` ile `Addresses` tablosuna taşıyın.
2. Checkout akışına idempotency-key ekleyin ve payment webhook'ları için tekrar idempotent davranışı sağlayın.
3. Ödeme işlemleri için `Payments` tablosu oluşturun ve reconcile/partial refund senaryolarını tanımlayın.
4. Stock reservation: rezervasyon timeout, cleanup job (retry/backoff), optimistic concurrency veya distributed lock ekleyin.
5. API tarafında global exception middleware, problem details formatı, ve merkezi logging kurun.
6. Çok kritik path'ler (payment, stock decrement) için unit/integration testler yazın ve CI pipeline içine ekleyin.
7. Secret yönetimi için vault/KeyVault kullanımına geçin. Ödeme key'leri ve SMTP bilgileri orada tutulmalı.
8. Frontend: tam checkout sayfası, adres yönetimi, order history ve order tracking akışlarını eksiksiz tamamlayın; E2E test ekleyin.

**Sonuç ve sonraki adımlar**
- Bu dosya ilk genel inceleme. İsterseniz şimdi daha dar odaklı bir çalışma yapalım: örn. `Orders` & `Payments` tabanlı kodu detaylı inceleyip migration ve DTO önerileri hazırlayayım, ya da `frontend` checkout akışını adım adım tamamlayayım.

Dosya oluşturuldu: `ARCHITECTURE_REVIEW_TODO.md` — hangi alanı önce ele almamı istersiniz? (backend: ödeme/rezervasyon veya frontend: checkout?)
