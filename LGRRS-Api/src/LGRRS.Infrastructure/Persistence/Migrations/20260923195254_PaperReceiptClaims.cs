using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LGRRS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PaperReceiptClaims : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PaperClaimExpiresAt",
                table: "Receipts",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaperClaimHash",
                table: "Receipts",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PaperClaimedAt",
                table: "Receipts",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Receipts_PaperClaimHash",
                table: "Receipts",
                column: "PaperClaimHash",
                unique: true,
                filter: "[PaperClaimHash] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Receipts_PaperClaimHash",
                table: "Receipts");

            migrationBuilder.DropColumn(
                name: "PaperClaimExpiresAt",
                table: "Receipts");

            migrationBuilder.DropColumn(
                name: "PaperClaimHash",
                table: "Receipts");

            migrationBuilder.DropColumn(
                name: "PaperClaimedAt",
                table: "Receipts");
        }
    }
}
