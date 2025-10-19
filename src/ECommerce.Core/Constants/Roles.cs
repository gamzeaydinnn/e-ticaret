namespace ECommerce.Core.Constants
{
    public static class Roles
    {
        public const string SuperAdmin = "SuperAdmin";
        public const string Admin = "Admin";
        public const string Manager = "Manager";
        public const string Editor = "Editor";
        public const string User = "User";

        // Sık kullanılan birleşik rol setleri
        public const string AdminAll = SuperAdmin + "," + Admin + "," + Manager + "," + Editor;
    }
}

