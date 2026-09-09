using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proposta.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPropostaOwner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "criado_por",
                table: "propostas",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "legacy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "criado_por",
                table: "propostas");
        }
    }
}
