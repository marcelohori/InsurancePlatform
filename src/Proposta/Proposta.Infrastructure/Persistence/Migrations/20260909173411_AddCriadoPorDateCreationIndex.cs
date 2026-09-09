using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proposta.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCriadoPorDateCreationIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_propostas_criado_por_data_criacao",
                table: "propostas",
                columns: new[] { "criado_por", "data_criacao" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_propostas_criado_por_data_criacao",
                table: "propostas");
        }
    }
}
