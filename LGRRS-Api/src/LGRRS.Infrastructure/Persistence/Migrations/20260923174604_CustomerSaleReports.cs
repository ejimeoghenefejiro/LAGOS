using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LGRRS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CustomerSaleReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SaleReports",
                columns: table => new
                {
                    SaleReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReporterPhoneHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Fingerprint = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    BusinessName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BusinessLocation = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    LgaCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PurchaseDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Issue = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Details = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ReviewNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaleReports", x => x.SaleReportId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SaleReports_Fingerprint",
                table: "SaleReports",
                column: "Fingerprint",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SaleReports_ReporterPhoneHash_CreatedAt",
                table: "SaleReports",
                columns: new[] { "ReporterPhoneHash", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SaleReports");
        }
    }
}
