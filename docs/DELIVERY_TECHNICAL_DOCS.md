# Teslimat Yönetim Sistemi - Teknik Dokümantasyon

## 📋 İçindekiler

1. [Mimari Genel Bakış](#mimari-genel-bakış)
2. [Servisler ve Bileşenler](#servisler-ve-bileşenler)
3. [API Endpoint'leri](#api-endpointleri)
4. [Veritabanı Şeması](#veritabanı-şeması)
5. [Background Jobs](#background-jobs)
6. [SignalR Hub'ları](#signalr-hubları)
7. [Güvenlik](#güvenlik)
8. [Test Stratejisi](#test-stratejisi)
9. [Deployment](#deployment)

---

## Mimari Genel Bakış

### Katmanlı Mimari

```
┌─────────────────────────────────────────────────────────────┐
│                    Presentation Layer                        │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐ │
│  │   Admin     │  │   Courier   │  │   Customer Mobile   │ │
│  │   Panel     │  │   Mobile    │  │      (Web/App)      │ │
│  └─────────────┘  └─────────────┘  └─────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│                      API Layer                               │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  ECommerce.API                                        │   │
│  │  - Controllers (Admin, Courier, Customer)             │   │
│  │  - Middleware (Auth, RateLimit, ErrorHandling)        │   │
│  │  - SignalR Hubs (Delivery, Admin, Courier)            │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│                    Business Layer                            │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  ECommerce.Business                                   │   │
│  │  - Services (Delivery, Courier, Assignment)           │   │
│  │  - Managers (DeliveryTask, CourierAssignment)         │   │
│  │  - Validators (Address, DeliveryTask)                 │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│                Infrastructure Layer                          │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  ECommerce.Infrastructure                             │   │
│  │  - Background Jobs (Timeout, Offline Handler)         │   │
│  │  - External Services (SMS, Push, Geocoding)           │   │
│  │  - Caching, Logging                                   │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│                     Data Layer                               │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  ECommerce.Data                                       │   │
│  │  - ECommerceDbContext                                 │   │
│  │  - Repositories (Generic, Specialized)                │   │
│  │  - Migrations                                         │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

### Proje Yapısı

```
src/
├── ECommerce.API/
│   ├── Controllers/
│   │   ├── Admin/
│   │   │   ├── AdminDeliveryTaskController.cs
│   │   │   └── DeliveryReportController.cs
│   │   └── Courier/
│   │       ├── CourierDeliveryController.cs
│   │       ├── CourierLocationController.cs
│   │       └── DeliveryProofController.cs
│   ├── Authorization/
│   │   ├── AdminOnlyAttribute.cs
│   │   ├── CourierOnlyAttribute.cs
│   │   └── CourierDataIsolationAttribute.cs
│   ├── Hubs/
│   │   ├── DeliveryHub.cs
│   │   └── AdminNotificationHub.cs
│   └── Program.cs
│
├── ECommerce.Business/
│   └── Services/
│       ├── Interfaces/
│       │   ├── IDeliveryTaskService.cs
│       │   ├── ICourierAssignmentService.cs
│       │   ├── IAddressValidationService.cs
│       │   ├── IOrderCancellationHandler.cs
│       │   └── IRetryDeliveryService.cs
│       └── Managers/
│           ├── DeliveryTaskManager.cs
│           ├── CourierAssignmentManager.cs
│           ├── AddressValidationManager.cs
│           ├── OrderCancellationHandler.cs
│           └── RetryDeliveryManager.cs
│
├── ECommerce.Infrastructure/
│   └── Services/
│       └── BackgroundJobs/
│           ├── DeliveryTimeoutJob.cs
│           └── CourierOfflineHandler.cs
│
├── ECommerce.Entities/
│   └── Concrete/
│       ├── DeliveryTask.cs
│       ├── Courier.cs
│       ├── CourierLocation.cs
│       └── DeliveryProof.cs
│
└── ECommerce.Tests/
    ├── Business/
    │   └── Services/
    │       ├── DeliveryTaskManagerTests.cs
    │       ├── CourierAssignmentManagerTests.cs
    │       └── AddressValidationManagerTests.cs
    └── Integration/
        └── DeliveryFlowIntegrationTests.cs
```

---

## Servisler ve Bileşenler

### 1. IDeliveryTaskService

Teslimat görevi yaşam döngüsü yönetimi.

```csharp
public interface IDeliveryTaskService
{
    // CRUD Operations
    Task<DeliveryTask> GetByIdAsync(int id);
    Task<IEnumerable<DeliveryTask>> GetByStatusAsync(DeliveryStatus status);
    Task<IEnumerable<DeliveryTask>> GetByCourierAsync(int courierId, DateTime? date = null);
    Task<IEnumerable<DeliveryTask>> GetByDateRangeAsync(DateTime start, DateTime end);

    // Lifecycle Operations
    Task<DeliveryTask> CreateFromOrderAsync(int orderId, int createdByUserId);
    Task<DeliveryTask> AssignAsync(int taskId, int courierId, int assignedByUserId);
    Task<DeliveryTask> AcceptAsync(int taskId, int courierId);
    Task<DeliveryTask> RejectAsync(int taskId, int courierId, string reason);
    Task<DeliveryTask> UpdateStatusAsync(int taskId, DeliveryStatus status, int actorId, ActorType actorType);
    Task<DeliveryTask> CancelAsync(int taskId, string reason, int cancelledByUserId);
    Task<DeliveryTask> ReassignAsync(int taskId, int newCourierId, string reason, int reassignedByUserId);

    // POD Operations
    Task<DeliveryTask> CompleteWithProofAsync(int taskId, int courierId, ProofOfDelivery pod);
    Task<DeliveryTask> MarkAsFailedAsync(int taskId, int courierId, string reason);
}
```

### 2. ICourierAssignmentService

Akıllı kurye atama algoritması.

```csharp
public interface ICourierAssignmentService
{
    /// <summary>
    /// En uygun kuryeyi bulur ve atar
    /// </summary>
    Task<DeliveryTask> AutoAssignAsync(int taskId);

    /// <summary>
    /// Belirli koordinatlar için uygun kuryeleri listeler
    /// </summary>
    Task<IEnumerable<CourierCandidate>> GetAvailableCouriersAsync(double latitude, double longitude);

    /// <summary>
    /// Kurye puanını hesaplar
    /// </summary>
    Task<double> CalculateCourierScoreAsync(int courierId, double taskLatitude, double taskLongitude);
}

public class CourierCandidate
{
    public int CourierId { get; set; }
    public string Name { get; set; }
    public double Distance { get; set; }       // km
    public int ActiveTaskCount { get; set; }
    public double AverageDeliveryTime { get; set; } // dakika
    public double Score { get; set; }          // Hesaplanan puan (yüksek = daha iyi)
}
```

**Puanlama Algoritması:**

```
Score = (10 - Distance) * 0.4 +
        (5 - ActiveTasks) * 0.3 +
        (60 - AvgDeliveryTime) * 0.3

Kısıtlamalar:
- Distance ≤ 10 km
- ActiveTasks ≤ 5
- CourierStatus = Active
```

### 3. IAddressValidationService

Adres doğrulama ve zenginleştirme.

```csharp
public interface IAddressValidationService
{
    /// <summary>
    /// Adresi doğrular ve koordinatlarla zenginleştirir
    /// </summary>
    Task<AddressValidationResult> ValidateAndEnrichAsync(string address);

    /// <summary>
    /// Toplu adres doğrulama
    /// </summary>
    Task<IEnumerable<AddressValidationResult>> ValidateBatchAsync(IEnumerable<string> addresses);
}

public class AddressValidationResult
{
    public bool IsValid { get; set; }
    public string OriginalAddress { get; set; }
    public string NormalizedAddress { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? ErrorMessage { get; set; }
    public AddressComponents? Components { get; set; }
}
```

### 4. IOrderCancellationHandler

Sipariş iptali işleme.

```csharp
public interface IOrderCancellationHandler
{
    /// <summary>
    /// Sipariş iptalini işler ve ilgili teslimat görevlerini iptal eder
    /// </summary>
    Task HandleOrderCancellationAsync(int orderId, string reason, int cancelledByUserId);
}
```

### 5. IRetryDeliveryService

Başarısız teslimat yeniden deneme yönetimi.

```csharp
public interface IRetryDeliveryService
{
    /// <summary>
    /// Başarısız teslimat için yeniden deneme planlar
    /// </summary>
    Task<DeliveryTask> ScheduleRetryAsync(int taskId, DateTime? retryAt = null);

    /// <summary>
    /// İade görevi oluşturur
    /// </summary>
    Task<DeliveryTask> CreateReturnTaskAsync(int taskId);

    /// <summary>
    /// Görev yeniden denenebilir mi kontrol eder
    /// </summary>
    Task<bool> CanRetryAsync(int taskId);
}
```

---

## API Endpoint'leri

### Admin API

| Method | Endpoint                                              | Açıklama          | İzin          |
| ------ | ----------------------------------------------------- | ----------------- | ------------- |
| GET    | `/api/admin/delivery-tasks`                           | Görevleri listele | Orders.View   |
| GET    | `/api/admin/delivery-tasks/{id}`                      | Görev detayı      | Orders.View   |
| POST   | `/api/admin/delivery-tasks`                           | Görev oluştur     | Orders.Manage |
| POST   | `/api/admin/delivery-tasks/{id}/assign/{courierId}`   | Kurye ata         | Orders.Manage |
| POST   | `/api/admin/delivery-tasks/{id}/reassign/{courierId}` | Yeniden ata       | Orders.Manage |
| POST   | `/api/admin/delivery-tasks/{id}/cancel`               | İptal et          | Orders.Manage |
| POST   | `/api/admin/delivery-tasks/{id}/auto-assign`          | Otomatik ata      | Orders.Manage |

### Courier API

| Method | Endpoint                                  | Açıklama            |
| ------ | ----------------------------------------- | ------------------- |
| GET    | `/api/courier/deliveries`                 | Görevlerimi listele |
| GET    | `/api/courier/deliveries/active`          | Aktif görevler      |
| GET    | `/api/courier/deliveries/{id}`            | Görev detayı        |
| POST   | `/api/courier/deliveries/{id}/accept`     | Görevi kabul et     |
| POST   | `/api/courier/deliveries/{id}/reject`     | Görevi reddet       |
| POST   | `/api/courier/deliveries/{id}/pickup`     | Paketi al           |
| POST   | `/api/courier/deliveries/{id}/in-transit` | Yola çık            |
| POST   | `/api/courier/deliveries/{id}/complete`   | Teslim et           |
| POST   | `/api/courier/deliveries/{id}/fail`       | Başarısız           |
| POST   | `/api/courier/location`                   | Konum güncelle      |
| POST   | `/api/courier/pod/photo`                  | Fotoğraf yükle      |
| POST   | `/api/courier/pod/otp`                    | OTP doğrula         |

### Response Formatı

```json
// Başarılı yanıt
{
  "success": true,
  "data": { ... },
  "message": "İşlem başarılı"
}

// Hata yanıtı
{
  "success": false,
  "error": {
    "code": "INVALID_STATUS_TRANSITION",
    "message": "Pending durumundan PickedUp durumuna geçiş yapılamaz"
  }
}
```

---

## Veritabanı Şeması

### DeliveryTask

```sql
CREATE TABLE DeliveryTasks (
    Id INT PRIMARY KEY IDENTITY,
    OrderId INT NOT NULL REFERENCES Orders(Id),
    AssignedCourierId INT NULL REFERENCES Couriers(Id),
    Status INT NOT NULL DEFAULT 0, -- DeliveryStatus enum

    -- Adres Bilgileri
    DeliveryAddress NVARCHAR(500) NOT NULL,
    DeliveryLatitude FLOAT NULL,
    DeliveryLongitude FLOAT NULL,

    -- Zaman Damgaları
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    AssignedAt DATETIME2 NULL,
    AcceptedAt DATETIME2 NULL,
    PickedUpAt DATETIME2 NULL,
    DeliveredAt DATETIME2 NULL,

    -- Retry & Return
    RetryCount INT NOT NULL DEFAULT 0,
    RetryScheduledAt DATETIME2 NULL,
    IsReturnTask BIT NOT NULL DEFAULT 0,
    ParentDeliveryTaskId INT NULL REFERENCES DeliveryTasks(Id),

    -- Metadata
    Notes NVARCHAR(1000) NULL,
    NotesInternal NVARCHAR(1000) NULL,
    FailureReason NVARCHAR(500) NULL,
    CreatedByUserId INT NOT NULL,
    LastModifiedByUserId INT NULL
);
```

### CourierLocation

```sql
CREATE TABLE CourierLocations (
    Id INT PRIMARY KEY IDENTITY,
    CourierId INT NOT NULL REFERENCES Couriers(Id),
    Latitude FLOAT NOT NULL,
    Longitude FLOAT NOT NULL,
    Accuracy FLOAT NULL,
    RecordedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    INDEX IX_CourierLocations_CourierId_RecordedAt (CourierId, RecordedAt DESC)
);
```

### DeliveryProof

```sql
CREATE TABLE DeliveryProofs (
    Id INT PRIMARY KEY IDENTITY,
    DeliveryTaskId INT NOT NULL REFERENCES DeliveryTasks(Id),
    ProofType INT NOT NULL, -- 0: Photo, 1: OTP, 2: Signature
    PhotoUrl NVARCHAR(500) NULL,
    OtpCode NVARCHAR(10) NULL,
    SignatureData NVARCHAR(MAX) NULL,
    VerifiedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    VerifiedByCourierId INT NOT NULL REFERENCES Couriers(Id)
);
```

---

## Background Jobs

### 1. DeliveryTimeoutJob

**Amaç:** Kuryelerin 60 saniye içinde kabul etmediği görevleri yeniden atar.

```csharp
public class DeliveryTimeoutJob : BackgroundService
{
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(10);
    private readonly TimeSpan _acceptanceTimeout = TimeSpan.FromSeconds(60);
    private const int MaxAssignmentRetries = 3;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessTimeoutTasksAsync();
            await Task.Delay(_checkInterval, stoppingToken);
        }
    }
}
```

**İş Akışı:**

1. `Assigned` durumundaki görevleri kontrol et
2. `AssignedAt + 60s < Now` ise timeout
3. Retry sayısını artır
4. Retry < 3 ise: Yeni kurye ata, SignalR bildirimi gönder
5. Retry >= 3 ise: `Pending` durumuna al, admin alarmı oluştur

### 2. CourierOfflineHandler

**Amaç:** Offline kuryeleri tespit eder ve aktif görevlerini yönetir.

```csharp
public class CourierOfflineHandler : BackgroundService
{
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);
    private readonly TimeSpan _offlineThreshold = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await DetectOfflineCouriersAsync();
            await Task.Delay(_checkInterval, stoppingToken);
        }
    }
}
```

**İş Akışı:**

1. Son 5 dakikada konum göndermeyen kuryeleri bul
2. Alarm kaydı oluştur
3. Admin paneline SignalR bildirimi gönder
4. Opsiyonel: Aktif görevleri başka kuryeye ata

---

## SignalR Hub'ları

### DeliveryHub

Kurye ve müşteri gerçek zamanlı iletişimi.

```csharp
[Authorize]
public class DeliveryHub : Hub
{
    // Kurye bir gruba katılır (kendi görevleri)
    public async Task JoinCourierGroup(int courierId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"courier_{courierId}");
    }

    // Müşteri bir siparişi takip eder
    public async Task TrackDelivery(int orderId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"order_{orderId}");
    }
}
```

**Events:**

- `TaskAssigned(taskId, taskDetails)` - Kuryeye görev atandı
- `TaskStatusChanged(taskId, newStatus)` - Görev durumu değişti
- `CourierLocationUpdated(orderId, lat, lng)` - Kurye konumu güncellendi
- `DeliveryCompleted(orderId, proofUrl)` - Teslimat tamamlandı

### AdminNotificationHub

Admin paneli bildirimleri.

```csharp
[Authorize(Policy = "AdminOnly")]
public class AdminNotificationHub : Hub
{
    public async Task JoinAdminGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "admins");
    }
}
```

**Events:**

- `CourierOffline(courierId, courierName, lastSeenAt)`
- `TaskTimeout(taskId, orderId, retryCount)`
- `DeliveryFailed(taskId, orderId, reason)`
- `NewAlarm(alarmType, message, details)`

---

## Güvenlik

### Token Türleri

| Token Türü     | Claim                  | Erişim            |
| -------------- | ---------------------- | ----------------- |
| Admin Token    | `token_type: admin`    | Admin API'leri    |
| Courier Token  | `token_type: courier`  | Courier API'leri  |
| Customer Token | `token_type: customer` | Customer API'leri |

### Custom Attributes

```csharp
// Sadece admin token'larını kabul eder
[AdminOnly]
public class AdminDeliveryTaskController : ControllerBase { }

// Sadece kurye token'larını kabul eder
[CourierOnly]
public class CourierDeliveryController : ControllerBase { }

// Kurye sadece kendi görevlerini görebilir
[CourierDataIsolation]
public class CourierDeliveryController : ControllerBase { }
```

### Rate Limiting

```csharp
// Program.cs
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("admin", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 1000;
    });

    options.AddFixedWindowLimiter("courier", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 500;
    });
});
```

---

## Test Stratejisi

### Unit Tests

```csharp
// DeliveryTaskManagerTests.cs
[Fact]
public async Task UpdateStatusAsync_FromPickedUpToInTransit_ShouldSucceed()
{
    // Arrange
    var task = CreateTaskWithStatus(DeliveryStatus.PickedUp);

    // Act
    var result = await _service.UpdateStatusAsync(
        task.Id,
        DeliveryStatus.InTransit,
        _courierId,
        ActorType.Courier);

    // Assert
    Assert.Equal(DeliveryStatus.InTransit, result.Status);
}
```

### Integration Tests

```csharp
// DeliveryFlowIntegrationTests.cs
[Fact]
public async Task FullDeliveryFlow_FromOrderToDelivery_ShouldComplete()
{
    // 1. Sipariş oluştur
    var order = await CreateOrderAsync();

    // 2. Teslimat görevi oluştur
    var task = await _taskService.CreateFromOrderAsync(order.Id, _adminUserId);

    // 3. Kurye ata
    task = await _taskService.AssignAsync(task.Id, _courierId, _adminUserId);

    // 4. Kurye kabul et
    task = await _taskService.AcceptAsync(task.Id, _courierId);

    // 5. Paket al
    task = await _taskService.UpdateStatusAsync(task.Id, DeliveryStatus.PickedUp, ...);

    // 6. Yola çık
    task = await _taskService.UpdateStatusAsync(task.Id, DeliveryStatus.InTransit, ...);

    // 7. Teslim et
    task = await _taskService.CompleteWithProofAsync(task.Id, _courierId, _pod);

    // Assert
    Assert.Equal(DeliveryStatus.Delivered, task.Status);
    Assert.NotNull(task.DeliveredAt);
}
```

### Test Çalıştırma

```bash
# Tüm testleri çalıştır
dotnet test

# Belirli bir test dosyasını çalıştır
dotnet test --filter "FullyQualifiedName~DeliveryTaskManagerTests"

# Coverage ile çalıştır
dotnet test --collect:"XPlat Code Coverage"
```

---

## Deployment

### Docker Compose

```yaml
version: "3.8"
services:
  api:
    build:
      context: .
      dockerfile: src/ECommerce.API/Dockerfile
    ports:
      - "5000:80"
    environment:
      - ConnectionStrings__DefaultConnection=...
      - JWT__SecretKey=...
    depends_on:
      - db

  db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=...
```

### Environment Variables

| Variable                               | Açıklama           | Örnek               |
| -------------------------------------- | ------------------ | ------------------- |
| `ConnectionStrings__DefaultConnection` | DB connection      | `Server=...`        |
| `JWT__SecretKey`                       | JWT secret key     | `SuperSecretKey123` |
| `JWT__Issuer`                          | Token issuer       | `eticaret.com`      |
| `JWT__Audience`                        | Token audience     | `eticaret-api`      |
| `DeliveryTimeout__AcceptanceSeconds`   | Kabul timeout      | `60`                |
| `DeliveryTimeout__MaxRetries`          | Max atama denemesi | `3`                 |

### Health Checks

```
GET /health         - Genel sağlık durumu
GET /health/db      - Veritabanı bağlantısı
GET /health/ready   - Uygulama hazır mı
```

---

## Versiyon Geçmişi

| Versiyon | Tarih | Değişiklikler           |
| -------- | ----- | ----------------------- |
| 1.0.0    | 2025  | İlk sürüm               |
| 1.1.0    | 2025  | Retry sistemi eklendi   |
| 1.2.0    | 2025  | Offline handler eklendi |

---

_Son Güncelleme: 2025_
