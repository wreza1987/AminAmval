using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetKeeper.Migrations
{
    /// <inheritdoc />
    public partial class AddSenderInfoToUserRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SenderEmployeeId",
                table: "UserRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_UserRequests_SenderEmployeeId",
                table: "UserRequests",
                column: "SenderEmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserRequests_Employees_SenderEmployeeId",
                table: "UserRequests",
                column: "SenderEmployeeId",
                principalTable: "Employees",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserRequests_Employees_SenderEmployeeId",
                table: "UserRequests");

            migrationBuilder.DropIndex(
                name: "IX_UserRequests_SenderEmployeeId",
                table: "UserRequests");

            migrationBuilder.DropColumn(
                name: "SenderEmployeeId",
                table: "UserRequests");
        }
    }
}
