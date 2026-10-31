using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTargetCategoryIdToHomeProductBlock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "HomeProductBlocks",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClickCount",
                table: "HomeProductBlocks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PosterLinkUrl",
                table: "HomeProductBlocks",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TargetCategoryId",
                table: "HomeProductBlocks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ViewCount",
                table: "HomeProductBlocks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Geriye dönük uyumluluk: Mevcut CategoryId verilerini TargetCategoryId alanına aktar
            migrationBuilder.Sql(@"UPDATE HomeProductBlocks SET TargetCategoryId = CategoryId WHERE CategoryId IS NOT NULL");

            // Categories tablosunda kolonlar veritabanında zaten varsa hata almamak için güvenli kontrol
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Categories]') AND name = 'IsMikroSynced')
                    ALTER TABLE [Categories] ADD [IsMikroSynced] bit NOT NULL DEFAULT CAST(0 AS bit);

                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Categories]') AND name = 'LastMikroSyncAt')
                    ALTER TABLE [Categories] ADD [LastMikroSyncAt] datetime2 NULL;

                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Categories]') AND name = 'MikroAltGrupKod')
                    ALTER TABLE [Categories] ADD [MikroAltGrupKod] nvarchar(25) NULL;

                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Categories]') AND name = 'MikroAnaGrupKod')
                    ALTER TABLE [Categories] ADD [MikroAnaGrupKod] nvarchar(25) NULL;

                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Categories]') AND name = 'MikroIsim')
                    ALTER TABLE [Categories] ADD [MikroIsim] nvarchar(100) NULL;

                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Categories]') AND name = 'NavbarGroupKey')
                    ALTER TABLE [Categories] ADD [NavbarGroupKey] nvarchar(60) NULL;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[Categories]') AND name = 'IX_Categories_MikroKod')
                    CREATE INDEX [IX_Categories_MikroKod] ON [Categories] ([MikroAnaGrupKod], [MikroAltGrupKod]);
            ");

            migrationBuilder.CreateIndex(
                name: "IX_HomeProductBlocks_Active",
                table: "HomeProductBlocks",
                columns: new[] { "IsActive", "DisplayOrder", "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_HomeProductBlocks_Analytics",
                table: "HomeProductBlocks",
                columns: new[] { "ClickCount", "ViewCount" });

            migrationBuilder.CreateIndex(
                name: "IX_HomeProductBlocks_TargetCategoryId",
                table: "HomeProductBlocks",
                column: "TargetCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_HomeProductBlocks_Categories_TargetCategoryId",
                table: "HomeProductBlocks",
                column: "TargetCategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HomeProductBlocks_Categories_TargetCategoryId",
                table: "HomeProductBlocks");

            migrationBuilder.DropIndex(
                name: "IX_HomeProductBlocks_Active",
                table: "HomeProductBlocks");

            migrationBuilder.DropIndex(
                name: "IX_HomeProductBlocks_Analytics",
                table: "HomeProductBlocks");

            migrationBuilder.DropIndex(
                name: "IX_HomeProductBlocks_TargetCategoryId",
                table: "HomeProductBlocks");

            migrationBuilder.DropColumn(
                name: "ClickCount",
                table: "HomeProductBlocks");

            migrationBuilder.DropColumn(
                name: "PosterLinkUrl",
                table: "HomeProductBlocks");

            migrationBuilder.DropColumn(
                name: "TargetCategoryId",
                table: "HomeProductBlocks");

            migrationBuilder.DropColumn(
                name: "ViewCount",
                table: "HomeProductBlocks");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "HomeProductBlocks",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);
        }
    }
}
