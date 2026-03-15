using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class OutboxPattern : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_InventoryItems",
                table: "InventoryItems");

            migrationBuilder.DropIndex(
                name: "IX_InventoryItems_ItemId",
                table: "InventoryItems");

            migrationBuilder.AddPrimaryKey(
                name: "PK_InventoryItems",
                table: "InventoryItems",
                columns: new[] { "ItemId", "UserId" });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OccurredOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Error = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_UserId",
                table: "InventoryItems",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OutboxMessages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_InventoryItems",
                table: "InventoryItems");

            migrationBuilder.DropIndex(
                name: "IX_InventoryItems_UserId",
                table: "InventoryItems");

            migrationBuilder.AddPrimaryKey(
                name: "PK_InventoryItems",
                table: "InventoryItems",
                columns: new[] { "UserId", "ItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_ItemId",
                table: "InventoryItems",
                column: "ItemId");
        }
    }
}
