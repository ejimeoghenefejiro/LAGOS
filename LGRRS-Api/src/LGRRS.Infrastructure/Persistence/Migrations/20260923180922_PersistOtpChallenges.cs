using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LGRRS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PersistOtpChallenges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OtpChallenges",
                columns: table => new
                {
                    OtpChallengeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MerchantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PhoneHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CodeHash = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ConsumedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FailedAttempts = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OtpChallenges", x => x.OtpChallengeId);
                    table.ForeignKey(
                        name: "FK_OtpChallenges_Merchants_MerchantId",
                        column: x => x.MerchantId,
                        principalTable: "Merchants",
                        principalColumn: "MerchantId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_OtpChallenges_MerchantId",
                table: "OtpChallenges",
                column: "MerchantId");

            migrationBuilder.CreateIndex(
                name: "IX_OtpChallenges_PhoneHash_Purpose",
                table: "OtpChallenges",
                columns: new[] { "PhoneHash", "Purpose" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OtpChallenges");
        }
    }
}
