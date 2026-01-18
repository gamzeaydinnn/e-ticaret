// ============================================================
// ASSIGN COURIER DTO - Kurye Atama Data Transfer Object
// ============================================================
// Bu DTO, siparişe kurye atama işlemi için kullanılır.
// Sadece kurye ID'si gereklidir.
// ============================================================

using System.ComponentModel.DataAnnotations;

namespace ECommerce.Core.DTOs.Order
{
    /// <summary>
    /// Siparişe kurye atama için gerekli bilgiler.
    /// </summary>
    public class AssignCourierDto
    {
        /// <summary>
        /// Atanacak kuryenin ID'si.
        /// Zorunlu alan - 0'dan büyük olmalı.
        /// </summary>
        [Required(ErrorMessage = "Kurye ID'si zorunludur.")]
        [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir kurye ID'si giriniz.")]
        public int CourierId { get; set; }
    }
}
