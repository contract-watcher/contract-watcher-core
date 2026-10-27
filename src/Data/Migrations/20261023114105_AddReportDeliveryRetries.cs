using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractWatcher.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReportDeliveryRetries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DeliveryAttempts",
                table: "Reports",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextAttemptAt",
                table: "Reports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reports_Parked",
                table: "Reports",
                column: "NextAttemptAt",
                filter: "\"DeliveryError\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reports_Parked",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "DeliveryAttempts",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "NextAttemptAt",
                table: "Reports");
        }
    }
}
