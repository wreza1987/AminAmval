using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetKeeper.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomFieldsToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdentityUserId",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: true,
                collation: "Persian_100_CI_AI");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AspNetUserTokens",
                type: "nvarchar(450)",
                nullable: false,
                collation: "Persian_100_CI_AI",
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128,
                oldCollation: "Persian_100_CI_AI");

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserTokens",
                type: "nvarchar(450)",
                nullable: false,
                collation: "Persian_100_CI_AI",
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128,
                oldCollation: "Persian_100_CI_AI");

            migrationBuilder.AddColumn<int>(
                name: "EmployeeId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PersonnelCode",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true,
                collation: "Persian_100_CI_AI");

            migrationBuilder.AlterColumn<string>(
                name: "ProviderKey",
                table: "AspNetUserLogins",
                type: "nvarchar(450)",
                nullable: false,
                collation: "Persian_100_CI_AI",
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128,
                oldCollation: "Persian_100_CI_AI");

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserLogins",
                type: "nvarchar(450)",
                nullable: false,
                collation: "Persian_100_CI_AI",
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128,
                oldCollation: "Persian_100_CI_AI");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IdentityUserId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "EmployeeId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PersonnelCode",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AspNetUserTokens",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                collation: "Persian_100_CI_AI",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldCollation: "Persian_100_CI_AI");

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserTokens",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                collation: "Persian_100_CI_AI",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldCollation: "Persian_100_CI_AI");

            migrationBuilder.AlterColumn<string>(
                name: "ProviderKey",
                table: "AspNetUserLogins",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                collation: "Persian_100_CI_AI",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldCollation: "Persian_100_CI_AI");

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserLogins",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                collation: "Persian_100_CI_AI",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldCollation: "Persian_100_CI_AI");
        }
    }
}
