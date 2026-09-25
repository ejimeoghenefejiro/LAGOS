using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LGRRS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AuditActorIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ActorName",
                table: "AuditEvents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActorRole",
                table: "AuditEvents",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActorName",
                table: "AuditEvents");

            migrationBuilder.DropColumn(
                name: "ActorRole",
                table: "AuditEvents");
        }
    }
}
