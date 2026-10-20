using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractWatcher.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReportDelivery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveredAt",
                table: "Reports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryError",
                table: "Reports",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reports_PendingDelivery",
                table: "Reports",
                column: "CreatedAt",
                filter: "\"DeliveredAt\" IS NULL AND \"DeliveryError\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reports_PendingDelivery",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "DeliveredAt",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "DeliveryError",
                table: "Reports");
        }
    }
}
