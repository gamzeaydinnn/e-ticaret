using Microsoft.AspNetCore.Identity;

var hasher = new PasswordHasher<object>();
var hash = hasher.HashPassword(null, args.Length > 0 ? args[0] : "admin123");
Console.WriteLine(hash);
