// ==========================================================================
// DevicePlatform.cs - Cihaz Platform Enum
// ==========================================================================
// Push notification token'ları için cihaz platformu tanımlaması.
// ==========================================================================

namespace ECommerce.Entities.Enums
{
    /// <summary>
    /// Push notification token cihaz platformları
    /// </summary>
    public enum DevicePlatform
    {
        /// <summary>
        /// iOS cihazlar (iPhone, iPad)
        /// </summary>
        iOS = 0,

        /// <summary>
        /// Android cihazlar
        /// </summary>
        Android = 1,

        /// <summary>
        /// Web tarayıcıları (Web Push)
        /// </summary>
        Web = 2,

        /// <summary>
        /// Windows cihazlar
        /// </summary>
        Windows = 3,

        /// <summary>
        /// macOS cihazlar
        /// </summary>
        MacOS = 4
    }
}
