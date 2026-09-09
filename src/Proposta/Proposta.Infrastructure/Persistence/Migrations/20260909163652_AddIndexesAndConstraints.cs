using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proposta.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIndexesAndConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_propostas_documento_segurado",
                table: "propostas",
                column: "documento_segurado");

            migrationBuilder.AddCheckConstraint(
                name: "CK_propostas_valor_cobertura_positivo",
                table: "propostas",
                sql: "\"valor_cobertura\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_propostas_valor_premio_positivo",
                table: "propostas",
                sql: "\"valor_premio\" >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_propostas_documento_segurado",
                table: "propostas");

            migrationBuilder.DropCheckConstraint(
                name: "CK_propostas_valor_cobertura_positivo",
                table: "propostas");

            migrationBuilder.DropCheckConstraint(
                name: "CK_propostas_valor_premio_positivo",
                table: "propostas");
        }
    }
}
