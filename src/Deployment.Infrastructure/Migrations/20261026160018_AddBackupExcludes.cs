using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Deployment.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBackupExcludes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BackupExcludes",
                table: "Targets",
                type: "TEXT",
                nullable: false,
                defaultValue: "log;logs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BackupExcludes",
                table: "Targets");
        }
    }
}
