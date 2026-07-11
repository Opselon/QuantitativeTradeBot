using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Nexus.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(NexusDbContext))]
    [Migration("20260101000000_InitialTradingState")]
    public partial class InitialTradingState : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Create accounts table
            migrationBuilder.CreateTable(
                name: "accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    broker_account_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    broker_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    balance = table.Column<decimal>(type: "numeric(20,4)", nullable: false),
                    equity = table.Column<decimal>(type: "numeric(20,4)", nullable: false),
                    margin = table.Column<decimal>(type: "numeric(20,4)", nullable: false),
                    free_margin = table.Column<decimal>(type: "numeric(20,4)", nullable: false),
                    leverage = table.Column<int>(type: "integer", nullable: false),
                    is_live = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_accounts", x => x.id);
                });

            // Unique index for broker_account_id
            migrationBuilder.CreateIndex(
                name: "IX_accounts_broker_account_id",
                table: "accounts",
                column: "broker_account_id",
                unique: true);

            // 2. Create orders table
            migrationBuilder.CreateTable(
                name: "orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ticket_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    symbol = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    direction = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    volume = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    price = table.Column<double>(type: "double precision", nullable: false),
                    stop_loss = table.Column<double>(type: "double precision", nullable: true),
                    take_profit = table.Column<double>(type: "double precision", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status_reason = table.Column<string>(type: "text", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orders", x => x.id);
                    table.ForeignKey(
                        name: "FK_orders_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            // 3. Create positions table
            migrationBuilder.CreateTable(
                name: "positions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ticket_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    symbol = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    direction = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    volume = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    entry_price = table.Column<double>(type: "double precision", nullable: false),
                    current_price = table.Column<double>(type: "double precision", nullable: false),
                    stop_loss = table.Column<double>(type: "double precision", nullable: true),
                    take_profit = table.Column<double>(type: "double precision", nullable: true),
                    unrealized_pnl = table.Column<decimal>(type: "numeric(20,4)", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "OPEN"),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    opened_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_positions", x => x.id);
                    table.ForeignKey(
                        name: "FK_positions_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            // Unique index for position ticket_id
            migrationBuilder.CreateIndex(
                name: "IX_positions_ticket_id",
                table: "positions",
                column: "ticket_id",
                unique: true);

            // 4. Create trades table
            migrationBuilder.CreateTable(
                name: "trades",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ticket_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    position_id = table.Column<Guid>(type: "uuid", nullable: true),
                    symbol = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    direction = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    volume = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    price = table.Column<double>(type: "double precision", nullable: false),
                    commission = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    swap = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    realized_pnl = table.Column<decimal>(type: "numeric(20,4)", nullable: false),
                    executed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trades", x => x.id);
                    table.ForeignKey(
                        name: "FK_trades_positions_position_id",
                        column: x => x.position_id,
                        principalTable: "positions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "trades");
            migrationBuilder.DropTable(name: "positions");
            migrationBuilder.DropTable(name: "orders");
            migrationBuilder.DropTable(name: "accounts");
        }
    }
}
