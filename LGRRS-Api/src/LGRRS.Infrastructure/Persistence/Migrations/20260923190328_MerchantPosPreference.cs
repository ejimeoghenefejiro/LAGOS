using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LGRRS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MerchantPosPreference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PosIntegrationEnabled",
                table: "Merchants",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PosIntegrationEnabled",
                table: "Merchants");
        }
    }
}
