using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractWatcher.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReportTimestamp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "Timestamp",
                table: "Reports",
                type: "timestamp with time zone",
                nullable: true);

            // Отчёты, принятые до появления времени в SDK: ближайшее известное время — приём в Core
            migrationBuilder.Sql("UPDATE \"Reports\" SET \"Timestamp\" = \"CreatedAt\";");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Timestamp",
                table: "Reports",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Timestamp",
                table: "Reports");
        }
    }
}
