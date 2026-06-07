using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EditAiTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "ai",
                table: "saved_searches");

            migrationBuilder.DropColumn(
                name: "Filters",
                schema: "ai",
                table: "saved_searches");

            migrationBuilder.DropColumn(
                name: "LastMatchedAt",
                schema: "ai",
                table: "saved_searches");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "ai",
                table: "ai_search_queries");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                schema: "ai",
                table: "ai_search_queries");

            migrationBuilder.DropColumn(
                name: "SessionId",
                schema: "ai",
                table: "ai_search_queries");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "ai",
                table: "ai_search_queries");

            migrationBuilder.RenameColumn(
                name: "ResultsCount",
                schema: "ai",
                table: "ai_search_queries",
                newName: "TokensUsed");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                schema: "ai",
                table: "saved_searches",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "NotifyOnMatch",
                schema: "ai",
                table: "saved_searches",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                schema: "ai",
                table: "saved_searches",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldDefaultValueSql: "NEWSEQUENTIALID()");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                schema: "ai",
                table: "saved_searches",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ParsedFilters",
                schema: "ai",
                table: "saved_searches",
                type: "nvarchar(Max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RawQuery",
                schema: "ai",
                table: "saved_searches",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "RawQuery",
                schema: "ai",
                table: "ai_search_queries",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "ParsedFilters",
                schema: "ai",
                table: "ai_search_queries",
                type: "nvarchar(Max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                schema: "ai",
                table: "ai_search_queries",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldDefaultValueSql: "NEWSEQUENTIALID()");

            migrationBuilder.AddColumn<string>(
                name: "Language",
                schema: "ai",
                table: "ai_search_queries",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "ParsingConfidence",
                schema: "ai",
                table: "ai_search_queries",
                type: "decimal(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "ResultCount",
                schema: "ai",
                table: "ai_search_queries",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "LeaseExtractionRequests",
                schema: "ai",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OverallConfidence = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    RawAiResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaseExtractionRequests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_saved_searches_active_notify",
                schema: "ai",
                table: "saved_searches",
                columns: new[] { "IsActive", "NotifyOnMatch" });

            migrationBuilder.CreateIndex(
                name: "ix_saved_searches_user",
                schema: "ai",
                table: "saved_searches",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "ix_ai_search_queries_date",
                schema: "ai",
                table: "ai_search_queries",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "ix_ai_search_queries_user",
                schema: "ai",
                table: "ai_search_queries",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LeaseExtractionRequests",
                schema: "ai");

            migrationBuilder.DropIndex(
                name: "ix_saved_searches_active_notify",
                schema: "ai",
                table: "saved_searches");

            migrationBuilder.DropIndex(
                name: "ix_saved_searches_user",
                schema: "ai",
                table: "saved_searches");

            migrationBuilder.DropIndex(
                name: "ix_ai_search_queries_date",
                schema: "ai",
                table: "ai_search_queries");

            migrationBuilder.DropIndex(
                name: "ix_ai_search_queries_user",
                schema: "ai",
                table: "ai_search_queries");

            migrationBuilder.DropColumn(
                name: "IsActive",
                schema: "ai",
                table: "saved_searches");

            migrationBuilder.DropColumn(
                name: "ParsedFilters",
                schema: "ai",
                table: "saved_searches");

            migrationBuilder.DropColumn(
                name: "RawQuery",
                schema: "ai",
                table: "saved_searches");

            migrationBuilder.DropColumn(
                name: "Language",
                schema: "ai",
                table: "ai_search_queries");

            migrationBuilder.DropColumn(
                name: "ParsingConfidence",
                schema: "ai",
                table: "ai_search_queries");

            migrationBuilder.DropColumn(
                name: "ResultCount",
                schema: "ai",
                table: "ai_search_queries");

            migrationBuilder.RenameColumn(
                name: "TokensUsed",
                schema: "ai",
                table: "ai_search_queries",
                newName: "ResultsCount");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                schema: "ai",
                table: "saved_searches",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<bool>(
                name: "NotifyOnMatch",
                schema: "ai",
                table: "saved_searches",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                schema: "ai",
                table: "saved_searches",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "NEWSEQUENTIALID()",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "ai",
                table: "saved_searches",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Filters",
                schema: "ai",
                table: "saved_searches",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastMatchedAt",
                schema: "ai",
                table: "saved_searches",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RawQuery",
                schema: "ai",
                table: "ai_search_queries",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AlterColumn<string>(
                name: "ParsedFilters",
                schema: "ai",
                table: "ai_search_queries",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(Max)");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                schema: "ai",
                table: "ai_search_queries",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "NEWSEQUENTIALID()",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "ai",
                table: "ai_search_queries",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                schema: "ai",
                table: "ai_search_queries",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SessionId",
                schema: "ai",
                table: "ai_search_queries",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                schema: "ai",
                table: "ai_search_queries",
                type: "datetime2",
                nullable: true);
        }
    }
}
