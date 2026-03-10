using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Scv.Db.Migrations
{
    public partial class SignalROutbox : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "signalr_outbox_message",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    channel = table.Column<string>(type: "text", nullable: false),
                    user_id = table.Column<string>(type: "text", nullable: true),
                    envelope_json = table.Column<string>(type: "text", nullable: false),
                    ack_required = table.Column<bool>(type: "boolean", nullable: false),
                    ack_guid = table.Column<Guid>(type: "uuid", nullable: true),
                    acked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    acked_by = table.Column<string>(type: "text", nullable: true),
                    offline_minutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    delivered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    delivered_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_signalr_outbox_message", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_signalr_outbox_message_channel_delivered_created",
                table: "signalr_outbox_message",
                columns: new[] { "channel", "delivered_at", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_signalr_outbox_message_ack_guid",
                table: "signalr_outbox_message",
                column: "ack_guid",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "signalr_outbox_message");
        }
    }
}
