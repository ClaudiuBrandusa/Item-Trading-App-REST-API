using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Item_Trading_App_REST_API.Data.Migrations
{
    /// <inheritdoc />
    public partial class TradeContentHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TradeContentHistory",
                columns: table => new
                {
                    TradeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ItemId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ItemName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradeContentHistory", x => new { x.ItemId, x.TradeId });
                    table.ForeignKey(
                        name: "FK_TradeContentHistory_Trades_TradeId",
                        column: x => x.TradeId,
                        principalTable: "Trades",
                        principalColumn: "TradeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TradeContentHistory_TradeId",
                table: "TradeContentHistory",
                column: "TradeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TradeContentHistory");
        }
    }
}
