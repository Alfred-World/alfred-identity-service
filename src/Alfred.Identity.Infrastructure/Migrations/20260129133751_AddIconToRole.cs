using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Alfred.Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIconToRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Icon",
                table: "roles",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Icon",
                table: "roles");
        }
    }
}
