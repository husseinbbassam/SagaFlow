using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SagaFlow.OrderService.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderItemsJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OrderItemsJson",
                schema: "saga",
                table: "OrderStates",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrderItemsJson",
                schema: "saga",
                table: "OrderStates");
        }
    }
}
