using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SecureAuthPortal.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RoleMaster",
                columns: table => new
                {
                    RoleId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleName = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleMaster", x => x.RoleId);
                });

            migrationBuilder.CreateTable(
                name: "UserMaster",
                columns: table => new
                {
                    UserId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FullName = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Username = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Password = table.Column<string>(type: "text", nullable: false),
                    EmailId = table.Column<string>(type: "varchar(100)", nullable: false),
                    MobileNo = table.Column<string>(type: "varchar(10)", nullable: false),
                    DOB = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Gender = table.Column<string>(type: "varchar(10)", nullable: false),
                    RoleId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserMaster", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_UserMaster_RoleMaster_RoleId",
                        column: x => x.RoleId,
                        principalTable: "RoleMaster",
                        principalColumn: "RoleId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DocumentMaster",
                columns: table => new
                {
                    DocumentId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    DocumentType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    DocumentPath = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    FileName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    VerificationNotes = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    UploadDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ApprovedBy = table.Column<long>(type: "bigint", nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentMaster", x => x.DocumentId);
                    table.ForeignKey(
                        name: "FK_DocumentMaster_UserMaster_ApprovedBy",
                        column: x => x.ApprovedBy,
                        principalTable: "UserMaster",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DocumentMaster_UserMaster_UserId",
                        column: x => x.UserId,
                        principalTable: "UserMaster",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentMaster_ApprovedBy",
                table: "DocumentMaster",
                column: "ApprovedBy");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentMaster_UserId",
                table: "DocumentMaster",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserMaster_EmailId",
                table: "UserMaster",
                column: "EmailId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserMaster_MobileNo",
                table: "UserMaster",
                column: "MobileNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserMaster_RoleId",
                table: "UserMaster",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserMaster_Username",
                table: "UserMaster",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentMaster");

            migrationBuilder.DropTable(
                name: "UserMaster");

            migrationBuilder.DropTable(
                name: "RoleMaster");
        }
    }
}
