using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Contratacao.Infrastructure.Persistence.Migrations;

public partial class AddIdempotencyRequestHash : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "hash_requisicao",
            table: "idempotency_records",
            type: "character varying(64)",
            maxLength: 64,
            nullable: false,
            defaultValue: "");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "hash_requisicao",
            table: "idempotency_records");
    }
}
