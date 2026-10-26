using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractWatcher.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EmailConfirmationExpiresAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailConfirmationSentAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailConfirmationTokenHash",
                table: "Users",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailConfirmedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FailureNotificationPending",
                table: "Reports",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "EmailMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    To = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Subject = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    NextAttemptAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastError = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationEmailNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IntegrationId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastEmailAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationEmailNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntegrationEmailNotifications_Integrations_IntegrationId",
                        column: x => x.IntegrationId,
                        principalTable: "Integrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_EmailConfirmationTokenHash",
                table: "Users",
                column: "EmailConfirmationTokenHash",
                unique: true,
                filter: "\"EmailConfirmationTokenHash\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_FailureNotificationPending",
                table: "Reports",
                column: "Timestamp",
                filter: "\"FailureNotificationPending\"");

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_Pending",
                table: "EmailMessages",
                column: "NextAttemptAt",
                filter: "\"SentAt\" IS NULL AND \"FailedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationEmailNotifications_IntegrationId",
                table: "IntegrationEmailNotifications",
                column: "IntegrationId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailMessages");

            migrationBuilder.DropTable(
                name: "IntegrationEmailNotifications");

            migrationBuilder.DropIndex(
                name: "IX_Users_EmailConfirmationTokenHash",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Reports_FailureNotificationPending",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "EmailConfirmationExpiresAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmailConfirmationSentAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmailConfirmationTokenHash",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmailConfirmedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "FailureNotificationPending",
                table: "Reports");
        }
    }
}
