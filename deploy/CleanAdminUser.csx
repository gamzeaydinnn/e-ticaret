// Eski admin kullanıcıyı temizleme script
using Microsoft.Data.SqlClient;

var connectionString = "Server=localhost,1435;Database=ECommerceDb;User Id=sa;Password=ECom1234;TrustServerCertificate=True;";

Console.WriteLine("🧹 Eski admin kullanıcıları temizleniyor...");

try
{
    using var connection = new SqlClient Connection(connectionString);
    await connection.OpenAsync();

    var sql = @"
        DELETE FROM RefreshTokens WHERE UserId IN (SELECT Id FROM Users WHERE Email IN ('admin@local', 'admin@admin.com'));
        DELETE FROM AspNetUserRoles WHERE UserId IN (SELECT Id FROM Users WHERE Email IN ('admin@local', 'admin@admin.com'));
        DELETE FROM Users WHERE Email IN ('admin@local', 'admin@admin.com');
        DELETE FROM AspNetUsers WHERE Email IN ('admin@local', 'admin@admin.com');
    ";

    using var command = new SqlCommand(sql, connection);
    var rowsAffected = await command.ExecuteNonQueryAsync();

    Console.WriteLine($"✅ Temizleme tamamlandı! {rowsAffected} kayıt silindi.");
    Console.WriteLine("\nŞimdi backend'i yeniden başlatın!");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Hata: {ex.Message}");
}
