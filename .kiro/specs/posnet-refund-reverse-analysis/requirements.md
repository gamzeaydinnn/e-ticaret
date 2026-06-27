# Requirements Document

## Introduction

Bu doküman, Yapı Kredi Bankası POSNET ödeme sistemi için para iadesi (refund/return) ve iptal (reverse) işlemlerinin kapsamlı analiz sisteminin gereksinimlerini tanımlar. Sistem, mevcut YapiKrediPosnetService implementasyonunu POSNET XML API dokümantasyonu ile karşılaştırarak eksiklikleri tespit edecek, iş kurallarını doğrulayacak ve test senaryoları oluşturacaktır.

## Glossary

- **POSNET_System**: Yapı Kredi Bankası POSNET XML API servisi
- **Reverse_Operation**: Gün içi iptal işlemi (finansal değer kazanmadan geri alma)
- **Return_Operation**: Gün sonu sonrası iade işlemi (kart ekstresinde görünen iade)
- **HostLogKey**: POSNET sisteminde işlemin tekil kimliği
- **OrderID**: Alışveriş sipariş numarası (24 karakter alphanumeric)
- **OrderID_Parameter**: İşyeri için aktif/pasif edilebilen sipariş numarası özelliği
- **Transaction_Type**: İşlem tipi (sale, auth, capt, return, pointUsage, vftTransaction)
- **AuthCode**: Sistem yetkilendirme kodu (Vade Farklı İşlemlerde zorunlu)
- **Group_Closing**: Gün sonu işlemi (grup kapama) - satışların finansallaşma zamanı
- **Partial_Refund**: Kısmi iade işlemi (orijinal tutardan daha az iade)
- **Full_Refund**: Tam iade işlemi (orijinal tutarın tamamının iadesi)
- **Unmatched_Return**: Eşleniksiz iade (orijinal işlem referansı olmadan yapılan iade)
- **3DS_Transaction**: 3D Secure işlemi (TDS\_ prefix ile işaretlenir)
- **Provision_Chain**: Provizyon + Finansallaştırma zinciri
- **YKB_Card**: Yapı Kredi Bankası kartları (iadenin iptali sadece bu kartlarda mümkün)
- **Refund_Cancellation**: İadenin iptali (sadece YKB kartları için, aynı gün içinde)
- **Analysis_Engine**: İşlem analiz motoru
- **Validation_Rules**: Doğrulama kuralları kümesi
- **Documentation_Compliance**: Dokümantasyon uygunluk kontrolü

## Requirements

### Requirement 1: İptal (Reverse) İşlemi Analizi

**User Story:** As a developer, I want to analyze reverse operations comprehensively, so that I can ensure all cancellation scenarios are correctly implemented and comply with POSNET specifications.

#### Acceptance Criteria

1. THE Analysis_Engine SHALL identify all reversible transaction types (sale, auth, capt, return, pointUsage, vftTransaction)
2. WHEN a reverse operation is requested, THE Analysis_Engine SHALL verify the transaction is from the same day
3. THE Analysis_Engine SHALL validate that no partial refunds exist on the transaction before allowing reversal
4. WHEN OrderID_Parameter is active, THE Analysis_Engine SHALL verify orderID and orderDate are provided together
5. WHEN OrderID_Parameter is passive, THE Analysis_Engine SHALL verify HostLogKey is used for identification
6. THE Analysis_Engine SHALL detect if a transaction has already been group-closed and prevent reversal
7. WHEN a VFT transaction is reversed, THE Analysis_Engine SHALL verify AuthCode is provided
8. THE Analysis_Engine SHALL validate that reversed transactions do not appear in customer statements

### Requirement 2: İade (Refund/Return) İşlemi Analizi

**User Story:** As a developer, I want to analyze refund operations comprehensively, so that I can ensure all refund scenarios (full/partial) are correctly implemented and tracked.

#### Acceptance Criteria

1. THE Analysis_Engine SHALL calculate the total refunded amount for each original transaction
2. WHEN a refund is requested, THE Analysis_Engine SHALL verify the refund amount does not exceed (original_amount - previous_refunds)
3. THE Analysis_Engine SHALL distinguish between same-day refunds and post-group-closing refunds
4. WHEN multiple partial refunds exist, THE Analysis_Engine SHALL validate the sum does not exceed the original amount
5. THE Analysis_Engine SHALL identify the correct HostLogKey for refund (capture HostLogKey for provision chains)
6. WHEN OrderID_Parameter is active, THE Analysis_Engine SHALL verify orderID and orderDate match the original transaction
7. THE Analysis*Engine SHALL validate that refunds for 3DS transactions use TDS* prefix format
8. THE Analysis_Engine SHALL track refund records separately in the Payments table with OriginalPaymentId linkage

### Requirement 3: Eşleniksiz İade (Unmatched Return) Analizi

**User Story:** As a developer, I want to analyze unmatched return operations, so that I can ensure refunds without original transaction references are properly handled.

#### Acceptance Criteria

1. THE Analysis_Engine SHALL validate card number and expiration date for unmatched returns
2. WHEN an unmatched return is processed, THE Analysis_Engine SHALL verify the merchant has unmatched return authorization
3. THE Analysis_Engine SHALL generate a unique OrderID for unmatched return transactions
4. THE Analysis_Engine SHALL validate currency code is provided for unmatched returns
5. THE Analysis_Engine SHALL ensure unmatched returns have proper audit trail

### Requirement 4: OrderID Parametre Yönetimi Analizi

**User Story:** As a developer, I want to analyze OrderID parameter behavior, so that I can ensure transaction identification works correctly in both active and passive states.

#### Acceptance Criteria

1. THE Analysis_Engine SHALL detect whether OrderID_Parameter is active or passive for the merchant
2. WHEN OrderID_Parameter is active, THE Analysis_Engine SHALL validate orderID is between 1-24 characters
3. WHEN OrderID_Parameter is active, THE Analysis_Engine SHALL require orderDate in YYYYAAGG format for reverse and refund operations
4. WHEN OrderID_Parameter is passive, THE Analysis_Engine SHALL validate orderID is exactly 24 characters
5. THE Analysis_Engine SHALL verify the same orderID can be reused on different dates when parameter is active
6. WHEN OrderID_Parameter is passive, THE Analysis_Engine SHALL prevent reuse of the same orderID regardless of date

### Requirement 5: 3D Secure İşlem İade Yönetimi

**User Story:** As a developer, I want to analyze 3D Secure transaction refunds, so that I can ensure proper handling of TDS\_ prefixed order IDs.

#### Acceptance Criteria

1. THE Analysis*Engine SHALL detect 3D Secure transactions by checking for TDS* prefix or 3DS metadata
2. WHEN refunding a 3DS transaction by OrderID, THE Analysis*Engine SHALL verify TDS* prefix is added to 20-character order IDs
3. THE Analysis*Engine SHALL validate TDS* prefixed OrderIDs are exactly 24 characters
4. THE Analysis_Engine SHALL recommend using HostLogKey over OrderID for 3DS refunds
5. THE Analysis_Engine SHALL verify oosTranData HostLogKey is stored and retrievable for refunds

### Requirement 6: Provizyon + Finansallaştırma Zinciri İade Analizi

**User Story:** As a developer, I want to analyze provision-capture chain refunds, so that I can ensure the correct HostLogKey (capture, not auth) is used for refunds.

#### Acceptance Criteria

1. THE Analysis_Engine SHALL identify provision-capture transaction chains
2. WHEN a refund is requested for a captured transaction, THE Analysis_Engine SHALL retrieve the capture HostLogKey
3. THE Analysis_Engine SHALL fall back to auth HostLogKey only if no capture record exists
4. THE Analysis_Engine SHALL validate that auth-only transactions cannot be refunded until captured
5. THE Analysis_Engine SHALL detect weight-based orders (KGL) and apply special capture HostLogKey logic

### Requirement 7: İadenin İptali (Refund Cancellation) Analizi

**User Story:** As a developer, I want to analyze refund cancellation operations, so that I can ensure this YKB-card-only feature is properly restricted and implemented.

#### Acceptance Criteria

1. THE Analysis_Engine SHALL verify refund cancellation is only allowed for YKB cards
2. WHEN canceling a refund, THE Analysis_Engine SHALL verify the refund was made on the same day
3. THE Analysis_Engine SHALL validate the transaction type is "return" before allowing cancellation
4. THE Analysis_Engine SHALL use reverse operation with transaction="return" for refund cancellation
5. THE Analysis_Engine SHALL reject refund cancellation attempts for non-YKB cards with appropriate error message

### Requirement 8: Dokümantasyon Uygunluk Kontrolü

**User Story:** As a developer, I want to compare current implementation against POSNET documentation, so that I can identify gaps and compliance issues.

#### Acceptance Criteria

1. THE Analysis_Engine SHALL parse POSNET XML API documentation (bankaapi file)
2. WHEN comparing implementation, THE Analysis_Engine SHALL check XML request structure matches documentation
3. THE Analysis_Engine SHALL verify all required fields are present in reverse and return operations
4. THE Analysis_Engine SHALL validate XML response parsing handles all documented response codes
5. THE Analysis_Engine SHALL identify missing error code handlers in current implementation
6. THE Analysis_Engine SHALL verify currency code normalization (TL/YT, US/EU) matches POSNET requirements

### Requirement 9: Hata Kodu Yönetimi ve Doğrulama

**User Story:** As a developer, I want to analyze error code handling, so that I can ensure all POSNET error scenarios are properly managed.

#### Acceptance Criteria

1. THE Analysis_Engine SHALL extract all error codes from POSNET documentation (0007, 0012, 0122, 0123, 0127, etc.)
2. WHEN error code 0122 is received, THE Analysis_Engine SHALL identify it as "reverse attempted after 1 week"
3. WHEN error code 0123 is received, THE Analysis_Engine SHALL identify it as "original transaction not found"
4. WHEN error code 0211 is received, THE Analysis_Engine SHALL identify it as "group closed, use refund instead"
5. THE Analysis_Engine SHALL verify current implementation maps all documented error codes to PosnetErrorCode enum
6. THE Analysis_Engine SHALL identify missing error code mappings

### Requirement 10: Tutar Validasyonu ve Kuruş Dönüşümü

**User Story:** As a developer, I want to validate amount handling, so that I can ensure correct kuruş conversion and amount limits are enforced.

#### Acceptance Criteria

1. THE Analysis_Engine SHALL verify amounts are converted to kuruş (TL to kuruş: multiply by 100)
2. WHEN a refund amount is provided, THE Analysis_Engine SHALL validate it does not exceed available refundable amount
3. THE Analysis_Engine SHALL calculate available_refund as (original_amount - sum_of_previous_refunds)
4. THE Analysis_Engine SHALL validate negative amounts are rejected
5. THE Analysis_Engine SHALL verify zero-amount refunds are rejected

### Requirement 11: Sipariş Tarihi (OrderDate) Format ve Saat Dilimi Yönetimi

**User Story:** As a developer, I want to validate OrderDate format and timezone handling, so that I can ensure date parameters are correctly formatted for POSNET.

#### Acceptance Criteria

1. THE Analysis_Engine SHALL verify OrderDate is formatted as YYYYAAGG (Year-Month-Day)
2. WHEN OrderDate is required, THE Analysis_Engine SHALL validate the format before sending to POSNET
3. THE Analysis_Engine SHALL verify UTC to Turkey timezone conversion is applied (MADDE 8 reference)
4. THE Analysis_Engine SHALL validate Order.OrderDate is converted using ToTurkeyDateString helper
5. THE Analysis_Engine SHALL reject invalid date formats with appropriate error message

### Requirement 12: Payment Record Tracking ve Audit Trail

**User Story:** As a developer, I want to analyze payment record management, so that I can ensure complete audit trail for all reverse and refund operations.

#### Acceptance Criteria

1. THE Analysis_Engine SHALL verify reverse operations update payment status to "Cancelled"
2. THE Analysis_Engine SHALL verify refund operations create new payment records with status "Refunded"
3. WHEN a refund is recorded, THE Analysis_Engine SHALL verify OriginalPaymentId links to the original transaction
4. THE Analysis_Engine SHALL validate TransactionType is set correctly ("return" for refunds, original type for reverses)
5. THE Analysis_Engine SHALL verify HostLogKey, AuthCode, and TransactionId are stored for audit purposes

### Requirement 13: Karma İşlem (Mixed Transaction) İade Analizi

**User Story:** As a developer, I want to analyze mixed transaction refunds, so that I can handle transactions with both card payment and WorldPuan usage.

#### Acceptance Criteria

1. THE Analysis_Engine SHALL identify karma transactions (mixed card + point payments)
2. WHEN refunding a karma transaction, THE Analysis_Engine SHALL verify point refund rules are applied
3. THE Analysis_Engine SHALL validate proportional refund calculations for partial karma refunds
4. THE Analysis_Engine SHALL ensure karma refund cancellation follows special rules
5. THE Analysis_Engine SHALL verify mixed transaction refunds are tracked separately

### Requirement 14: Test Senaryo Üretimi

**User Story:** As a developer, I want to generate comprehensive test scenarios, so that I can validate all reverse and refund operations systematically.

#### Acceptance Criteria

1. THE Analysis_Engine SHALL generate test scenarios for same-day reversal success cases
2. THE Analysis_Engine SHALL generate test scenarios for group-closed reversal failure cases
3. THE Analysis_Engine SHALL generate test scenarios for partial refund sequences
4. THE Analysis_Engine SHALL generate test scenarios for refund amount exceeding original transaction
5. THE Analysis_Engine SHALL generate test scenarios for OrderID parameter active vs passive modes
6. THE Analysis*Engine SHALL generate test scenarios for 3DS transaction refunds with TDS* prefix
7. THE Analysis_Engine SHALL generate test scenarios for provision-capture chain refunds
8. THE Analysis_Engine SHALL generate test scenarios for refund cancellation on YKB vs non-YKB cards

### Requirement 15: Analiz Raporu Üretimi

**User Story:** As a developer, I want to generate analysis reports, so that I can document findings, gaps, and recommendations.

#### Acceptance Criteria

1. THE Analysis_Engine SHALL generate a compliance report comparing current code vs POSNET documentation
2. THE Analysis_Engine SHALL produce an error handling coverage report
3. THE Analysis_Engine SHALL create a business rules validation report
4. THE Analysis_Engine SHALL generate a test scenario catalog with expected outcomes
5. THE Analysis_Engine SHALL provide remediation recommendations for identified gaps
6. THE Analysis_Engine SHALL export reports in Markdown format for documentation
