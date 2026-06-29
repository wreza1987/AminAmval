using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetKeeper.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDescriptionFromAssetHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "AssetHistory");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "AssetHistory",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                collation: "Persian_100_CI_AI");
        }
    }
}
