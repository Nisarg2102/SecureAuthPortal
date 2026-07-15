using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecureAuthPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddFileDataToDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "DocumentMaster",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "FileData",
                table: "DocumentMaster",
                type: "bytea",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "DocumentMaster");

            migrationBuilder.DropColumn(
                name: "FileData",
                table: "DocumentMaster");
        }
    }
}
