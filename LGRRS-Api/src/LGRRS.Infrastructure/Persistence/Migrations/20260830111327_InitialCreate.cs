using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LGRRS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppUsers",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUsers", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "AuditEvents",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActorId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEvents", x => x.EventId);
                });

            migrationBuilder.CreateTable(
                name: "DrawPeriods",
                columns: table => new
                {
                    DrawPeriodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DrawDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PrizeBudget = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CandidateSetCount = table.Column<int>(type: "int", nullable: true),
                    CandidateSetHash = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DrawPeriods", x => x.DrawPeriodId);
                });

            migrationBuilder.CreateTable(
                name: "FraudFlags",
                columns: table => new
                {
                    FlagId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityType = table.Column<int>(type: "int", nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RuleCode = table.Column<int>(type: "int", nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FraudFlags", x => x.FlagId);
                });

            migrationBuilder.CreateTable(
                name: "Merchants",
                columns: table => new
                {
                    MerchantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BusinessType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LgaCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LagosTaxIdEncrypted = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LgrrsSystemId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PhoneEncrypted = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PhoneHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    VerifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Merchants", x => x.MerchantId);
                });

            migrationBuilder.CreateTable(
                name: "Receipts",
                columns: table => new
                {
                    ReceiptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MerchantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CustomerPhoneEncrypted = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CustomerPhoneHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CustomerPhoneMasked = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ItemService = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DrawPeriodId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TransactionDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Receipts", x => x.ReceiptId);
                    table.ForeignKey(
                        name: "FK_Receipts_DrawPeriods_DrawPeriodId",
                        column: x => x.DrawPeriodId,
                        principalTable: "DrawPeriods",
                        principalColumn: "DrawPeriodId");
                    table.ForeignKey(
                        name: "FK_Receipts_Merchants_MerchantId",
                        column: x => x.MerchantId,
                        principalTable: "Merchants",
                        principalColumn: "MerchantId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReceiptDeliveries",
                columns: table => new
                {
                    DeliveryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReceiptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Channel = table.Column<int>(type: "int", nullable: false),
                    DestinationMasked = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProviderRef = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceiptDeliveries", x => x.DeliveryId);
                    table.ForeignKey(
                        name: "FK_ReceiptDeliveries_Receipts_ReceiptId",
                        column: x => x.ReceiptId,
                        principalTable: "Receipts",
                        principalColumn: "ReceiptId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RewardEntries",
                columns: table => new
                {
                    EntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReceiptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DrawPeriodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EligibilityStatus = table.Column<int>(type: "int", nullable: false),
                    RiskStatus = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RewardEntries", x => x.EntryId);
                    table.ForeignKey(
                        name: "FK_RewardEntries_DrawPeriods_DrawPeriodId",
                        column: x => x.DrawPeriodId,
                        principalTable: "DrawPeriods",
                        principalColumn: "DrawPeriodId");
                    table.ForeignKey(
                        name: "FK_RewardEntries_Receipts_ReceiptId",
                        column: x => x.ReceiptId,
                        principalTable: "Receipts",
                        principalColumn: "ReceiptId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DrawResults",
                columns: table => new
                {
                    DrawResultId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DrawPeriodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrizeTier = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PrizeAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DrawTimestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DrawResults", x => x.DrawResultId);
                    table.ForeignKey(
                        name: "FK_DrawResults_DrawPeriods_DrawPeriodId",
                        column: x => x.DrawPeriodId,
                        principalTable: "DrawPeriods",
                        principalColumn: "DrawPeriodId");
                    table.ForeignKey(
                        name: "FK_DrawResults_RewardEntries_EntryId",
                        column: x => x.EntryId,
                        principalTable: "RewardEntries",
                        principalColumn: "EntryId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PrizeClaims",
                columns: table => new
                {
                    ClaimId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DrawResultId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NameEncrypted = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhoneHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    BankNameEncrypted = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AccountNumberEncrypted = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ClaimRef = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrizeClaims", x => x.ClaimId);
                    table.ForeignKey(
                        name: "FK_PrizeClaims_DrawResults_DrawResultId",
                        column: x => x.DrawResultId,
                        principalTable: "DrawResults",
                        principalColumn: "DrawResultId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_Email",
                table: "AppUsers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_EntityType_EntityId",
                table: "AuditEvents",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_DrawResults_DrawPeriodId",
                table: "DrawResults",
                column: "DrawPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_DrawResults_EntryId",
                table: "DrawResults",
                column: "EntryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FraudFlags_EntityType_EntityId",
                table: "FraudFlags",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_Merchants_LgrrsSystemId",
                table: "Merchants",
                column: "LgrrsSystemId",
                unique: true,
                filter: "[LgrrsSystemId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Merchants_PhoneHash",
                table: "Merchants",
                column: "PhoneHash");

            migrationBuilder.CreateIndex(
                name: "IX_PrizeClaims_ClaimRef",
                table: "PrizeClaims",
                column: "ClaimRef",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrizeClaims_DrawResultId",
                table: "PrizeClaims",
                column: "DrawResultId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptDeliveries_ReceiptId",
                table: "ReceiptDeliveries",
                column: "ReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_Receipts_CustomerPhoneHash",
                table: "Receipts",
                column: "CustomerPhoneHash");

            migrationBuilder.CreateIndex(
                name: "IX_Receipts_DrawPeriodId",
                table: "Receipts",
                column: "DrawPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_Receipts_MerchantId",
                table: "Receipts",
                column: "MerchantId");

            migrationBuilder.CreateIndex(
                name: "IX_RewardEntries_DrawPeriodId",
                table: "RewardEntries",
                column: "DrawPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_RewardEntries_ReceiptId",
                table: "RewardEntries",
                column: "ReceiptId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RewardEntries_ReceiptId_DrawPeriodId",
                table: "RewardEntries",
                columns: new[] { "ReceiptId", "DrawPeriodId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppUsers");

            migrationBuilder.DropTable(
                name: "AuditEvents");

            migrationBuilder.DropTable(
                name: "FraudFlags");

            migrationBuilder.DropTable(
                name: "PrizeClaims");

            migrationBuilder.DropTable(
                name: "ReceiptDeliveries");

            migrationBuilder.DropTable(
                name: "DrawResults");

            migrationBuilder.DropTable(
                name: "RewardEntries");

            migrationBuilder.DropTable(
                name: "Receipts");

            migrationBuilder.DropTable(
                name: "DrawPeriods");

            migrationBuilder.DropTable(
                name: "Merchants");
        }
    }
}
