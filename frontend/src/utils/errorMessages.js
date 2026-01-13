// ============================================================================
// errorMessages.js - Türkçe Hata Mesajları Çeviri Servisi
// ============================================================================
// Bu modül, backend'den gelen İngilizce hata mesajlarını Türkçe'ye çevirir.
// Identity, Permission ve genel hata mesajlarını kapsar.
// ============================================================================

/**
 * Hata mesajları çeviri sözlüğü
 * Key: Backend'den gelen hata kodu veya mesajı
 * Value: Türkçe karşılığı
 */
export const ERROR_MESSAGES = {
  // ============================================================================
  // Identity / Şifre Hataları
  // ============================================================================
  PasswordRequiresNonAlphanumeric:
    "Şifre en az bir özel karakter içermelidir (!@#$%^&* vb.)",
  PasswordRequiresDigit: "Şifre en az bir rakam içermelidir (0-9)",
  PasswordRequiresUpper: "Şifre en az bir büyük harf içermelidir (A-Z)",
  PasswordRequiresLower: "Şifre en az bir küçük harf içermelidir (a-z)",
  PasswordTooShort: "Şifre en az 6 karakter olmalıdır",
  PasswordMismatch: "Şifreler eşleşmiyor",
  InvalidPassword: "Geçersiz şifre",
  PasswordRequiresUniqueChars:
    "Şifre daha fazla benzersiz karakter içermelidir",

  // ============================================================================
  // Identity / Kullanıcı Hataları
  // ============================================================================
  DuplicateEmail: "Bu email adresi zaten kullanılıyor",
  DuplicateUserName: "Bu kullanıcı adı zaten kullanılıyor",
  InvalidEmail: "Geçersiz email adresi",
  InvalidUserName: "Geçersiz kullanıcı adı",
  UserNotFound: "Kullanıcı bulunamadı",
  UserAlreadyHasPassword: "Kullanıcının zaten bir şifresi var",
  UserAlreadyInRole: "Kullanıcı zaten bu role sahip",
  UserNotInRole: "Kullanıcı bu role sahip değil",
  UserLockoutNotEnabled: "Kullanıcı kilitleme özelliği etkin değil",
  UserLockedOut:
    "Hesabınız geçici olarak kilitlendi. Lütfen daha sonra tekrar deneyin",
  LoginAlreadyAssociated:
    "Bu giriş yöntemi başka bir hesapla ilişkilendirilmiş",

  // ============================================================================
  // Identity / Token Hataları
  // ============================================================================
  InvalidToken: "Geçersiz veya süresi dolmuş token",
  TokenExpired: "Oturum süresi doldu. Lütfen tekrar giriş yapın",
  InvalidRefreshToken: "Geçersiz yenileme token'ı",
  RefreshTokenExpired: "Yenileme token'ı süresi doldu",

  // ============================================================================
  // Kimlik Doğrulama Hataları
  // ============================================================================
  InvalidCredentials: "Geçersiz email veya şifre",
  AccountNotConfirmed: "Email adresiniz henüz doğrulanmamış",
  AccountDisabled: "Hesabınız devre dışı bırakılmış",
  AccountLocked: "Hesabınız kilitlenmiş. Lütfen yönetici ile iletişime geçin",
  TwoFactorRequired: "İki faktörlü doğrulama gerekli",
  InvalidTwoFactorCode: "Geçersiz doğrulama kodu",

  // ============================================================================
  // Yetkilendirme / İzin Hataları
  // ============================================================================
  AccessDenied: "Bu işlem için yetkiniz bulunmamaktadır",
  Forbidden: "Erişim reddedildi",
  Unauthorized: "Oturum açmanız gerekmektedir",
  InsufficientPermissions: "Bu işlem için yeterli izniniz yok",
  RoleNotFound: "Rol bulunamadı",
  PermissionNotFound: "İzin bulunamadı",
  CannotModifySuperAdmin: "SuperAdmin hesabı değiştirilemez",
  CannotDeleteSelf: "Kendi hesabınızı silemezsiniz",
  CannotChangeOwnRole: "Kendi rolünüzü değiştiremezsiniz",

  // ============================================================================
  // Genel Sunucu Hataları
  // ============================================================================
  ServerError: "Sunucu hatası oluştu. Lütfen daha sonra tekrar deneyin",
  InternalServerError: "Dahili sunucu hatası",
  ServiceUnavailable: "Servis şu anda kullanılamıyor",
  NetworkError: "Bağlantı hatası. İnternet bağlantınızı kontrol edin",
  TimeoutError: "İstek zaman aşımına uğradı",
  BadRequest: "Geçersiz istek",
  NotFound: "Kaynak bulunamadı",
  Conflict: "Çakışma hatası - kaynak zaten mevcut",

  // ============================================================================
  // Validasyon Hataları
  // ============================================================================
  ValidationError: "Girilen bilgilerde hata var",
  RequiredField: "Bu alan zorunludur",
  InvalidFormat: "Geçersiz format",
  MinLength: "Minimum karakter sayısına ulaşılmadı",
  MaxLength: "Maksimum karakter sayısı aşıldı",
  InvalidRange: "Değer geçerli aralıkta değil",

  // ============================================================================
  // İş Mantığı Hataları
  // ============================================================================
  EntityNotFound: "Kayıt bulunamadı",
  DuplicateEntry: "Bu kayıt zaten mevcut",
  OperationFailed: "İşlem başarısız oldu",
  InvalidOperation: "Geçersiz işlem",
  ConcurrencyError: "Kayıt başka bir kullanıcı tarafından değiştirilmiş",
};

/**
 * Hata mesajını Türkçe'ye çevirir
 * @param {string|object|Error} error - Çevrilecek hata
 * @returns {string} Türkçe hata mesajı
 */
export const translateError = (error) => {
  // Null/undefined kontrolü
  if (!error) {
    return "Bilinmeyen hata oluştu";
  }

  // String ise direkt çevir
  if (typeof error === "string") {
    // Önce tam eşleşme ara
    if (ERROR_MESSAGES[error]) {
      return ERROR_MESSAGES[error];
    }

    // Kısmi eşleşme ara (hata mesajı içinde anahtar kelime)
    for (const [key, value] of Object.entries(ERROR_MESSAGES)) {
      if (error.toLowerCase().includes(key.toLowerCase())) {
        return value;
      }
    }

    // Eşleşme yoksa orijinal mesajı döndür
    return error;
  }

  // Error objesi ise
  if (error instanceof Error) {
    return translateError(error.message);
  }

  // API response objesi ise
  if (typeof error === "object") {
    // Hata kodu varsa
    if (error.code) {
      const translated = ERROR_MESSAGES[error.code];
      if (translated) return translated;
    }

    // Hata mesajı varsa
    if (error.message) {
      return translateError(error.message);
    }

    // errors dizisi varsa (validation errors)
    if (Array.isArray(error.errors)) {
      return error.errors.map((e) => translateError(e)).join(", ");
    }

    // description varsa
    if (error.description) {
      return translateError(error.description);
    }
  }

  return "Bilinmeyen hata oluştu";
};

/**
 * API hata yanıtını işler ve Türkçe mesaj döndürür
 * @param {object} response - Axios veya fetch response
 * @returns {string} Türkçe hata mesajı
 */
export const handleApiError = (response) => {
  // HTTP durum koduna göre genel mesaj
  const statusMessages = {
    400: "Geçersiz istek",
    401: "Oturum açmanız gerekmektedir",
    403: "Bu işlem için yetkiniz bulunmamaktadır",
    404: "Kaynak bulunamadı",
    409: "Çakışma hatası",
    422: "Doğrulama hatası",
    429: "Çok fazla istek gönderildi. Lütfen bekleyin",
    500: "Sunucu hatası oluştu",
    502: "Sunucu yanıt vermiyor",
    503: "Servis şu anda kullanılamıyor",
    504: "Sunucu zaman aşımı",
  };

  // Response data'dan mesaj çıkar
  if (response?.data) {
    const data = response.data;

    // Backend'den gelen mesaj varsa çevir
    if (data.message) {
      return translateError(data.message);
    }

    // Errors dizisi varsa
    if (data.errors && Array.isArray(data.errors)) {
      return data.errors
        .map((e) => translateError(e.description || e))
        .join(", ");
    }
  }

  // HTTP durum koduna göre mesaj
  if (response?.status && statusMessages[response.status]) {
    return statusMessages[response.status];
  }

  return "Bir hata oluştu. Lütfen tekrar deneyin";
};

/**
 * Identity hatalarını toplu çevirir
 * @param {Array} errors - Identity error array
 * @returns {string} Birleştirilmiş Türkçe hata mesajları
 */
export const translateIdentityErrors = (errors) => {
  if (!Array.isArray(errors)) {
    return translateError(errors);
  }

  return errors
    .map((err) => {
      // Identity error objesi: { code: "...", description: "..." }
      if (err.code) {
        return ERROR_MESSAGES[err.code] || err.description || err.code;
      }
      return translateError(err);
    })
    .join(". ");
};

export default {
  ERROR_MESSAGES,
  translateError,
  handleApiError,
  translateIdentityErrors,
};
