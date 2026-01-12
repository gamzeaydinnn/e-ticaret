using System;
using Microsoft.AspNetCore.Identity;
using ECommerce.Entities.Concrete;

namespace HashPasswordTool
{
    class Program
    {
        static void Main(string[] args)
        {
            var user = new User { UserName = "admin@admin.com" };
            var hasher = new PasswordHasher<User>();
            var hash = hasher.HashPassword(user, "admin123");
            Console.WriteLine(hash);
        }
    }
}
