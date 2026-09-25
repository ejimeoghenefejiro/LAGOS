using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LGRRS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PosReceiptIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PosApiKeyExpiresAt",
                table: "Merchants",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PosApiKeyHash",
                table: "Merchants",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PosApiKeyPrefix",
                table: "Merchants",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PosSubmissions",
                columns: table => new
                {
                    PosSubmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MerchantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalSaleId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PayloadHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ResponseJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PosSubmissions", x => x.PosSubmissionId);
                    table.ForeignKey(
                        name: "FK_PosSubmissions_Merchants_MerchantId",
                        column: x => x.MerchantId,
                        principalTable: "Merchants",
                        principalColumn: "MerchantId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Merchants_PosApiKeyHash",
                table: "Merchants",
                column: "PosApiKeyHash",
                unique: true,
                filter: "[PosApiKeyHash] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PosSubmissions_MerchantId_ExternalSaleId",
                table: "PosSubmissions",
                columns: new[] { "MerchantId", "ExternalSaleId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PosSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_Merchants_PosApiKeyHash",
                table: "Merchants");

            migrationBuilder.DropColumn(
                name: "PosApiKeyExpiresAt",
                table: "Merchants");

            migrationBuilder.DropColumn(
                name: "PosApiKeyHash",
                table: "Merchants");

            migrationBuilder.DropColumn(
                name: "PosApiKeyPrefix",
                table: "Merchants");
        }
    }
}
