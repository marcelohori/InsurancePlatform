using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Contratacao.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIndexesAndConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_contratacoes_numero_apolice",
                table: "contratacoes",
                column: "numero_apolice",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_contratacoes_valor_premio_positivo",
                table: "contratacoes",
                sql: "\"valor_premio\" >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_contratacoes_numero_apolice",
                table: "contratacoes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_contratacoes_valor_premio_positivo",
                table: "contratacoes");
        }
    }
}
