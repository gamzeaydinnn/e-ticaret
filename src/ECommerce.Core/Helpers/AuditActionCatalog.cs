using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ECommerce.Core.Helpers
{
    /// <summary>
    /// Denetim kaydı aksiyon/entity kodlarını Türkçe açıklamaya çevirir.
    /// </summary>
    public static class AuditActionCatalog
    {
        private static readonly Dictionary<string, string> Actions = new(StringComparer.OrdinalIgnoreCase)
        {
            ["PermissionAddedToRole"] = "İzin role eklendi",
            ["PermissionRemovedFromRole"] = "İzin rolden kaldırıldı",
            ["RolePermissionsUpdated"] = "Rol izinleri güncellendi",
            ["PermissionActivated"] = "İzin etkinleştirildi",
            ["PermissionDeactivated"] = "İzin pasifleştirildi",
            ["UserUpdated"] = "Kullanıcı bilgileri güncellendi",
            ["UserDeleted"] = "Kullanıcı silindi",
            ["UserRoleUpdated"] = "Kullanıcı rolü değiştirildi",
            ["UserPasswordUpdated"] = "Kullanıcı şifresi güncellendi",
            ["ProfileUpdated"] = "Profil bilgileri güncellendi",
            ["PasswordChanged"] = "Şifre değiştirildi",
            ["ProductCreated"] = "Ürün oluşturuldu",
            ["ProductUpdated"] = "Ürün güncellendi",
            ["ProductDeleted"] = "Ürün silindi",
            ["OrderUpdated"] = "Sipariş güncellendi",
            ["OrderDeleted"] = "Sipariş silindi",
            ["OrderBulkDeleted"] = "Sipariş toplu silindi",
            ["OrderStatusChanged"] = "Sipariş durumu değiştirildi",
            ["OrderCancelledWithRefund"] = "Sipariş iptal edildi ve iade başlatıldı",
            ["OrderRefunded"] = "Sipariş iadesi yapıldı",
            ["OrderItemRefunded"] = "Sipariş kalemi iade edildi",
            ["CourierAssigned"] = "Kurye atandı",
            ["RefundApproved"] = "İade talebi onaylandı",
            ["RefundRejected"] = "İade talebi reddedildi",
            ["RefundRetry"] = "İade işlemi yeniden denendi",
            ["CouponCreated"] = "Kupon oluşturuldu",
            ["CouponUpdated"] = "Kupon güncellendi",
            ["CouponDeleted"] = "Kupon silindi",
            ["BannerCreated"] = "Banner oluşturuldu",
            ["BannerUploaded"] = "Banner yüklendi",
            ["BannerImageUploaded"] = "Banner görseli yüklendi",
            ["BannerUpdated"] = "Banner güncellendi",
            ["BannerUpdatedWithImage"] = "Banner görselle güncellendi",
            ["BannerDeleted"] = "Banner silindi",
            ["BannersResetToDefault"] = "Bannerlar varsayılana sıfırlandı",
            ["NewsletterSubscriberDeleted"] = "Bülten abonesi silindi",
            ["NewsletterBulkEmailSent"] = "Toplu bülten e-postası gönderildi",
            ["PAYMENT_FAILED"] = "Ödeme başarısız oldu",
            ["PAYMENT_CANCELLED_REFUNDED"] = "Ödeme iptal edilip iade edildi",
            ["PAYMENT_REFUNDED"] = "Ödeme iade edildi",
        };

        private static readonly Dictionary<string, string> Entities = new(StringComparer.OrdinalIgnoreCase)
        {
            ["RolePermission"] = "Rol izni",
            ["Role"] = "Rol",
            ["Permission"] = "İzin",
            ["User"] = "Kullanıcı",
            ["Product"] = "Ürün",
            ["Order"] = "Sipariş",
            ["Coupon"] = "Kupon",
            ["Banner"] = "Banner",
            ["Newsletter"] = "Bülten",
            ["NewsletterSubscriber"] = "Bülten abonesi",
            ["RefundRequest"] = "İade talebi",
            ["Payments"] = "Ödeme",
            ["Payment"] = "Ödeme",
            ["Category"] = "Kategori",
            ["Cari"] = "Müşteri (ERP)",
        };

        public static string LabelAction(string? action)
        {
            if (string.IsNullOrWhiteSpace(action))
                return "İşlem";

            return Actions.TryGetValue(action.Trim(), out var label)
                ? label
                : HumanizeCode(action);
        }

        public static string LabelEntity(string? entityType)
        {
            if (string.IsNullOrWhiteSpace(entityType))
                return "Kayıt";

            return Entities.TryGetValue(entityType.Trim(), out var label)
                ? label
                : HumanizeCode(entityType);
        }

        /// <summary>
        /// newValue içine Türkçe message ekler (yoksa). Mevcut message korunur.
        /// </summary>
        public static object EnsureTurkishPayload(
            string action,
            string entityType,
            string? entityId,
            object? newValue)
        {
            var title = LabelAction(action);
            var entityLabel = LabelEntity(entityType);
            var idPart = string.IsNullOrWhiteSpace(entityId) || entityId == "0"
                ? string.Empty
                : $" (#{entityId})";

            var autoMessage = $"{title} — {entityLabel}{idPart}";

            if (newValue == null)
            {
                return new { message = autoMessage };
            }

            try
            {
                var node = JsonSerializer.SerializeToNode(newValue) as JsonObject
                           ?? new JsonObject();

                if (node["message"] == null && node["Message"] == null)
                {
                    node["message"] = autoMessage;
                }

                node["actionLabel"] = title;
                node["entityLabel"] = entityLabel;
                return node;
            }
            catch
            {
                return new { message = autoMessage, detail = newValue };
            }
        }

        private static string HumanizeCode(string code)
        {
            var spaced = System.Text.RegularExpressions.Regex
                .Replace(code.Replace('_', ' '), "([a-z])([A-Z])", "$1 $2")
                .Trim();

            return spaced.Length == 0 ? code : spaced;
        }
    }
}
