using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Data.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// Store Attendant ve Dispatcher sistemi için gerekli alanları ekler.
    /// 
    /// Orders tablosu:
    /// - PreparedBy: Siparişi hazırlayan görevli
    /// - PreparingStartedAt: Hazırlamaya başlama zamanı
    /// - ReadyAt: Hazır olma zamanı
    /// - WeightInGrams: Tartılan ağırlık (gram)
    /// 
    /// Couriers tablosu:
    /// - IsOnline: Kurye çevrimiçi mi
    /// - LastSeenAt: Son görülme zamanı
    /// - VehicleType: Araç tipi
    /// </summary>
    public partial class AddStoreDispatcherSystemFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Orders tablosuna yeni alanlar ekle
            migrationBuilder.AddColumn<string>(
                name: "PreparedBy",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PreparingStartedAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReadyAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WeightInGrams",
                table: "Orders",
                type: "int",
                nullable: true);

            // Couriers tablosuna yeni alanlar ekle (IF NOT EXISTS ile güvenli şekilde)
            // IsOnline alanı zaten varsa ekleme yapılmaz
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Couriers]') AND name = 'IsOnline')
                BEGIN
                    ALTER TABLE [Couriers] ADD [IsOnline] bit NOT NULL DEFAULT CAST(0 AS bit);
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Couriers]') AND name = 'LastSeenAt')
                BEGIN
                    ALTER TABLE [Couriers] ADD [LastSeenAt] datetime2 NULL;
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Couriers]') AND name = 'VehicleType')
                BEGIN
                    ALTER TABLE [Couriers] ADD [VehicleType] nvarchar(max) NULL;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreparedBy",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PreparingStartedAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ReadyAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "WeightInGrams",
                table: "Orders");

            // Couriers alanları güvenli şekilde kaldır
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Couriers]') AND name = 'IsOnline')
                BEGIN
                    ALTER TABLE [Couriers] DROP COLUMN [IsOnline];
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Couriers]') AND name = 'LastSeenAt')
                BEGIN
                    ALTER TABLE [Couriers] DROP COLUMN [LastSeenAt];
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Couriers]') AND name = 'VehicleType')
                BEGIN
                    ALTER TABLE [Couriers] DROP COLUMN [VehicleType];
                END
            ");
        }
    }
}
