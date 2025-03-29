using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Whispr.IntegrationTests.Migrations
{
    /// <inheritdoc />
    public partial class AddTraceParent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TraceParent",
                schema: "Application",
                table: "OutboxMessage",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TraceParent",
                schema: "Application",
                table: "OutboxMessage");
        }
    }
}
