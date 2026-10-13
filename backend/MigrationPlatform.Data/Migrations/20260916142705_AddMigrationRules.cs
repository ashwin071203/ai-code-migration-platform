using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MigrationPlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMigrationRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MigrationRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SourceTechnology = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TargetTechnology = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Pattern = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Replacement = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MigrationRules", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MigrationRules_SourceTechnology_TargetTechnology_Enabled",
                table: "MigrationRules",
                columns: new[] { "SourceTechnology", "TargetTechnology", "Enabled" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MigrationRules");
        }
    }
}
