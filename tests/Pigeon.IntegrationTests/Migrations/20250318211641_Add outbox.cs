﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pigeon.IntegrationTests.Migrations
{
    /// <inheritdoc />
    public partial class Addoutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OutboxMessage",
                schema: "Application",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Envelope_Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Envelope_MessageType = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Envelope_MessageId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Envelope_CorrelationId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Envelope_DeferredUntil = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DestinationTopicName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessage", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_ProcessedAtUtc_CreatedAtUtc",
                schema: "Application",
                table: "OutboxMessage",
                columns: new[] { "ProcessedAtUtc", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OutboxMessage",
                schema: "Application");
        }
    }
}
