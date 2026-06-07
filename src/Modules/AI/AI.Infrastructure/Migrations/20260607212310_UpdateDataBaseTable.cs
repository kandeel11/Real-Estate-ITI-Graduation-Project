using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDataBaseTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ai_chatbot_messages_ai_chatbot_sessions_SessionId",
                schema: "ai",
                table: "ai_chatbot_messages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ai_chatbot_sessions",
                schema: "ai",
                table: "ai_chatbot_sessions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ai_chatbot_messages",
                schema: "ai",
                table: "ai_chatbot_messages");

            migrationBuilder.RenameTable(
                name: "ai_chatbot_sessions",
                schema: "ai",
                newName: "AiChatbotSessions",
                newSchema: "ai");

            migrationBuilder.RenameTable(
                name: "ai_chatbot_messages",
                schema: "ai",
                newName: "AiChatbotMessages",
                newSchema: "ai");

            migrationBuilder.RenameIndex(
                name: "IX_ai_chatbot_sessions_SessionToken",
                schema: "ai",
                table: "AiChatbotSessions",
                newName: "IX_AiChatbotSessions_SessionToken");

            migrationBuilder.RenameIndex(
                name: "IX_ai_chatbot_messages_SessionId",
                schema: "ai",
                table: "AiChatbotMessages",
                newName: "IX_AiChatbotMessages_SessionId");

            migrationBuilder.AlterColumn<string>(
                name: "SessionToken",
                schema: "ai",
                table: "AiChatbotSessions",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                schema: "ai",
                table: "AiChatbotSessions",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldDefaultValueSql: "NEWSEQUENTIALID()");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                schema: "ai",
                table: "AiChatbotMessages",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldDefaultValueSql: "NEWSEQUENTIALID()");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AiChatbotSessions",
                schema: "ai",
                table: "AiChatbotSessions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AiChatbotMessages",
                schema: "ai",
                table: "AiChatbotMessages",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_AiChatbotSessions_UserId",
                schema: "ai",
                table: "AiChatbotSessions",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_AiChatbotMessages_AiChatbotSessions_SessionId",
                schema: "ai",
                table: "AiChatbotMessages",
                column: "SessionId",
                principalSchema: "ai",
                principalTable: "AiChatbotSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AiChatbotMessages_AiChatbotSessions_SessionId",
                schema: "ai",
                table: "AiChatbotMessages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AiChatbotSessions",
                schema: "ai",
                table: "AiChatbotSessions");

            migrationBuilder.DropIndex(
                name: "IX_AiChatbotSessions_UserId",
                schema: "ai",
                table: "AiChatbotSessions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AiChatbotMessages",
                schema: "ai",
                table: "AiChatbotMessages");

            migrationBuilder.RenameTable(
                name: "AiChatbotSessions",
                schema: "ai",
                newName: "ai_chatbot_sessions",
                newSchema: "ai");

            migrationBuilder.RenameTable(
                name: "AiChatbotMessages",
                schema: "ai",
                newName: "ai_chatbot_messages",
                newSchema: "ai");

            migrationBuilder.RenameIndex(
                name: "IX_AiChatbotSessions_SessionToken",
                schema: "ai",
                table: "ai_chatbot_sessions",
                newName: "IX_ai_chatbot_sessions_SessionToken");

            migrationBuilder.RenameIndex(
                name: "IX_AiChatbotMessages_SessionId",
                schema: "ai",
                table: "ai_chatbot_messages",
                newName: "IX_ai_chatbot_messages_SessionId");

            migrationBuilder.AlterColumn<string>(
                name: "SessionToken",
                schema: "ai",
                table: "ai_chatbot_sessions",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                schema: "ai",
                table: "ai_chatbot_sessions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "NEWSEQUENTIALID()",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                schema: "ai",
                table: "ai_chatbot_messages",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "NEWSEQUENTIALID()",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ai_chatbot_sessions",
                schema: "ai",
                table: "ai_chatbot_sessions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ai_chatbot_messages",
                schema: "ai",
                table: "ai_chatbot_messages",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ai_chatbot_messages_ai_chatbot_sessions_SessionId",
                schema: "ai",
                table: "ai_chatbot_messages",
                column: "SessionId",
                principalSchema: "ai",
                principalTable: "ai_chatbot_sessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
