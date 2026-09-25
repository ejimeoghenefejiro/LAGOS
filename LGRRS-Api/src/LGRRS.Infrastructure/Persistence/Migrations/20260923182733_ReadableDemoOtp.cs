using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LGRRS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReadableDemoOtp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE [OtpChallenges] SET [ExpiresAt] = SYSDATETIMEOFFSET(), [ConsumedAt] = COALESCE([ConsumedAt], SYSDATETIMEOFFSET());");
            migrationBuilder.DropColumn(
                name: "CodeHash",
                table: "OtpChallenges");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "OtpChallenges",
                type: "nvarchar(6)",
                maxLength: 6,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE [OtpChallenges] SET [ExpiresAt] = SYSDATETIMEOFFSET(), [ConsumedAt] = COALESCE([ConsumedAt], SYSDATETIMEOFFSET());");
            migrationBuilder.DropColumn(
                name: "Code",
                table: "OtpChallenges");

            migrationBuilder.AddColumn<string>(
                name: "CodeHash",
                table: "OtpChallenges",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }
    }
}

