/** Admin log ekranlarında İngilizce kodları Türkçe açıklamaya çevirir */

const ENTITY_LABELS = {
  Cari: "Müşteri (ERP)",
  Order: "Sipariş",
  Siparis: "Sipariş",
  Invoice: "Fatura",
  Fatura: "Fatura",
  Stock: "Stok",
  Stok: "Stok",
  Price: "Fiyat",
  Fiyat: "Fiyat",
  Product: "Ürün",
  User: "Kullanıcı",
  Category: "Kategori",
  Coupon: "Kupon",
  Role: "Rol",
  RolePermission: "Rol izni",
  Permission: "İzin",
  Banner: "Banner",
  Newsletter: "Bülten",
  NewsletterSubscriber: "Bülten abonesi",
  RefundRequest: "İade talebi",
  Payments: "Ödeme",
  Payment: "Ödeme",
  // Backend artık Türkçe yazıyor; eski kayıtlar + yeni kayıtlar
  "Rol izni": "Rol izni",
  Kullanıcı: "Kullanıcı",
  Sipariş: "Sipariş",
  Ürün: "Ürün",
  İzin: "İzin",
};

const STATUS_LABELS = {
  Success: "Başarılı",
  Completed: "Tamamlandı",
  Failed: "Başarısız",
  Pending: "Bekliyor",
  DeadLetter: "Manuel müdahale gerekli",
  Retrying: "Yeniden deneniyor",
};

const DIRECTION_LABELS = {
  ToERP: "Site → ERP",
  FromERP: "ERP → Site",
  Inbound: "Gelen",
  Outbound: "Giden",
};

const ACTION_LABELS = {
  PermissionAddedToRole: "İzin role eklendi",
  PermissionRemovedFromRole: "İzin rolden kaldırıldı",
  RolePermissionsUpdated: "Rol izinleri güncellendi",
  PermissionActivated: "İzin etkinleştirildi",
  PermissionDeactivated: "İzin pasifleştirildi",
  UserUpdated: "Kullanıcı bilgileri güncellendi",
  UserDeleted: "Kullanıcı silindi",
  UserRoleUpdated: "Kullanıcı rolü değiştirildi",
  UserPasswordUpdated: "Kullanıcı şifresi güncellendi",
  ProfileUpdated: "Profil bilgileri güncellendi",
  PasswordChanged: "Şifre değiştirildi",
  ProductCreated: "Ürün oluşturuldu",
  ProductUpdated: "Ürün güncellendi",
  ProductDeleted: "Ürün silindi",
  OrderUpdated: "Sipariş güncellendi",
  OrderDeleted: "Sipariş silindi",
  OrderBulkDeleted: "Sipariş toplu silindi",
  OrderStatusChanged: "Sipariş durumu değiştirildi",
  OrderCancelledWithRefund: "Sipariş iptal edildi ve iade başlatıldı",
  OrderRefunded: "Sipariş iadesi yapıldı",
  OrderItemRefunded: "Sipariş kalemi iade edildi",
  CourierAssigned: "Kurye atandı",
  RefundApproved: "İade talebi onaylandı",
  RefundRejected: "İade talebi reddedildi",
  RefundRetry: "İade işlemi yeniden denendi",
  CouponCreated: "Kupon oluşturuldu",
  CouponUpdated: "Kupon güncellendi",
  CouponDeleted: "Kupon silindi",
  BannerCreated: "Banner oluşturuldu",
  BannerUploaded: "Banner yüklendi",
  BannerImageUploaded: "Banner görseli yüklendi",
  BannerUpdated: "Banner güncellendi",
  BannerUpdatedWithImage: "Banner görselle güncellendi",
  BannerDeleted: "Banner silindi",
  BannersResetToDefault: "Bannerlar varsayılana sıfırlandı",
  NewsletterSubscriberDeleted: "Bülten abonesi silindi",
  NewsletterBulkEmailSent: "Toplu bülten e-postası gönderildi",
  PAYMENT_FAILED: "Ödeme başarısız oldu",
  PAYMENT_CANCELLED_REFUNDED: "Ödeme iptal edilip iade edildi",
  PAYMENT_REFUNDED: "Ödeme iade edildi",
  StockAdjusted: "Stok düzeltildi",
  StockReserved: "Stok rezerve edildi",
  StockReleased: "Stok serbest bırakıldı",
  Increase: "Stok artırıldı",
  Decrease: "Stok azaltıldı",
  Set: "Stok ayarlandı",
};

const MESSAGE_REPLACEMENTS = [
  [/Max retry exceeded/gi, "Maksimum deneme sayısına ulaşıldı"],
  [/MikroAPI false döndürdü/gi, "ERP müşteri kaydı kabul etmedi"],
  [
    /Dead Letter:\s*(\d+)\s*başarısız deneme sonrası/gi,
    "$1 başarısız denemeden sonra manuel müdahale bekliyor",
  ],
  [/Max\s+(\d+)\s+deneme aşıldı/gi, "$1 deneme sonrası senkronizasyon durduruldu"],
  [/HTTP\s+(\d+):\s*Bad Request/gi, "HTTP $1: Geçersiz istek"],
  [/HTTP\s+(\d+):\s*Unauthorized/gi, "HTTP $1: Yetkisiz"],
  [/HTTP\s+(\d+):\s*Forbidden/gi, "HTTP $1: Erişim engellendi"],
  [/HTTP\s+(\d+):\s*Not Found/gi, "HTTP $1: Kayıt bulunamadı"],
  [/HTTP\s+(\d+):\s*Internal Server Error/gi, "HTTP $1: ERP sunucu hatası"],
  [/Unhandled exception/gi, "Beklenmeyen hata"],
];

function humanizeCamelCase(value) {
  return String(value || "")
    .replace(/_/g, " ")
    .replace(/([a-z])([A-Z])/g, "$1 $2")
    .replace(/\s+/g, " ")
    .trim();
}

export function labelEntity(value) {
  if (!value) return "-";
  if (ENTITY_LABELS[value]) return ENTITY_LABELS[value];
  // Zaten Türkçe ise olduğu gibi bırak
  if (/[çğıöşüÇĞİÖŞÜ]/.test(value) || value.includes(" ")) return value;
  return humanizeCamelCase(value);
}

export function labelStatus(value) {
  if (!value) return "-";
  return STATUS_LABELS[value] || value;
}

export function labelDirection(value) {
  if (!value) return "-";
  return DIRECTION_LABELS[value] || value;
}

export function labelAction(value) {
  if (!value) return "-";
  if (ACTION_LABELS[value]) return ACTION_LABELS[value];
  if (/[çğıöşüÇĞİÖŞÜ]/.test(value) || (value.includes(" ") && !/^[A-Z][a-z]+[A-Z]/.test(value))) {
    return value;
  }
  return ACTION_LABELS[value] || humanizeCamelCase(value);
}

export function statusChipColor(value) {
  const v = String(value || "").toLowerCase();
  if (v === "success" || v === "completed") return "success";
  if (v === "failed" || v === "deadletter") return "error";
  if (v === "pending" || v === "retrying") return "warning";
  return "default";
}

export function humanizeLogMessage(text) {
  if (!text) return "";
  let result = String(text);
  for (const [pattern, replacement] of MESSAGE_REPLACEMENTS) {
    result = result.replace(pattern, replacement);
  }
  return result;
}

/** newValues JSON içinden Türkçe açıklama çıkarır */
export function extractAuditDescription(log) {
  const actionLabel = labelAction(log?.action);
  const entityLabel = labelEntity(log?.entityType);
  const id = log?.entityId && String(log.entityId) !== "0" ? ` #${log.entityId}` : "";

  const raw = log?.newValues;
  if (!raw) {
    return `${actionLabel} — ${entityLabel}${id}`;
  }

  try {
    const parsed = typeof raw === "string" ? JSON.parse(raw) : raw;
    if (parsed?.message) return humanizeLogMessage(parsed.message);
    if (parsed?.Message) return humanizeLogMessage(parsed.Message);
    if (parsed?.actionLabel) {
      return `${parsed.actionLabel} — ${parsed.entityLabel || entityLabel}${id}`;
    }
  } catch {
    // ignore
  }

  return `${actionLabel} — ${entityLabel}${id}`;
}
