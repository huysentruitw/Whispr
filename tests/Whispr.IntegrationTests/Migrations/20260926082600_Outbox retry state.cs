using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Whispr.IntegrationTests.Migrations
{
    /// <inheritdoc />
    public partial class Outboxretrystate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "MessageType",
                schema: "Application",
                table: "OutboxMessage",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(250)",
                oldMaxLength: 250);

            migrationBuilder.AlterColumn<string>(
                name: "DestinationTopicName",
                schema: "Application",
                table: "OutboxMessage",
                type: "nvarchar(260)",
                maxLength: 260,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<int>(
                name: "AttemptCount",
                schema: "Application",
                table: "OutboxMessage",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "LastError",
                schema: "Application",
                table: "OutboxMessage",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NextAttemptAtUtc",
                schema: "Application",
                table: "OutboxMessage",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ParkedAtUtc",
                schema: "Application",
                table: "OutboxMessage",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_CreatedAtUtc",
                schema: "Application",
                table: "OutboxMessage",
                column: "CreatedAtUtc",
                filter: "[ProcessedAtUtc] IS NULL AND [ParkedAtUtc] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessage_CreatedAtUtc",
                schema: "Application",
                table: "OutboxMessage");

            migrationBuilder.DropColumn(
                name: "AttemptCount",
                schema: "Application",
                table: "OutboxMessage");

            migrationBuilder.DropColumn(
                name: "LastError",
                schema: "Application",
                table: "OutboxMessage");

            migrationBuilder.DropColumn(
                name: "NextAttemptAtUtc",
                schema: "Application",
                table: "OutboxMessage");

            migrationBuilder.DropColumn(
                name: "ParkedAtUtc",
                schema: "Application",
                table: "OutboxMessage");

            migrationBuilder.AlterColumn<string>(
                name: "MessageType",
                schema: "Application",
                table: "OutboxMessage",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "DestinationTopicName",
                schema: "Application",
                table: "OutboxMessage",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(260)",
                oldMaxLength: 260);
        }
    }
}
