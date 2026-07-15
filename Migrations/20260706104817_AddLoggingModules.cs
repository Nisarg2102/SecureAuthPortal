using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SecureAuthPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddLoggingModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DocumentMaster_UserMaster_ApprovedBy",
                table: "DocumentMaster");

            migrationBuilder.DropForeignKey(
                name: "FK_DocumentMaster_UserMaster_UserId",
                table: "DocumentMaster");

            migrationBuilder.CreateTable(
                name: "ActivityLog",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Username = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    Activity = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IpAddress = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ErrorLog",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Username = table.Column<string>(type: "text", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: false),
                    Controller = table.Column<string>(type: "text", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    StackTrace = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IpAddress = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErrorLog", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentMaster_UserMaster_ApprovedBy",
                table: "DocumentMaster",
                column: "ApprovedBy",
                principalTable: "UserMaster",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentMaster_UserMaster_UserId",
                table: "DocumentMaster",
                column: "UserId",
                principalTable: "UserMaster",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DocumentMaster_UserMaster_ApprovedBy",
                table: "DocumentMaster");

            migrationBuilder.DropForeignKey(
                name: "FK_DocumentMaster_UserMaster_UserId",
                table: "DocumentMaster");

            migrationBuilder.DropTable(
                name: "ActivityLog");

            migrationBuilder.DropTable(
                name: "ErrorLog");

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentMaster_UserMaster_ApprovedBy",
                table: "DocumentMaster",
                column: "ApprovedBy",
                principalTable: "UserMaster",
                principalColumn: "UserId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentMaster_UserMaster_UserId",
                table: "DocumentMaster",
                column: "UserId",
                principalTable: "UserMaster",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
