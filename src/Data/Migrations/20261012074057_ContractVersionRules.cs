using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractWatcher.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class ContractVersionRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Старые JSON Schema не переводятся в правила, поэтому колонка пересоздаётся
            migrationBuilder.DropColumn(
                name: "Schema",
                table: "ContractVersions");

            migrationBuilder.AddColumn<string>(
                name: "Rules",
                table: "ContractVersions",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedAt",
                table: "ContractVersions",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PublishedAt",
                table: "ContractVersions");

            migrationBuilder.DropColumn(
                name: "Rules",
                table: "ContractVersions");

            migrationBuilder.AddColumn<string>(
                name: "Schema",
                table: "ContractVersions",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");
        }
    }
}
