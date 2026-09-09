using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Analise.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIndexesAndConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_analises_score_risco_range",
                table: "analises",
                sql: "\"score_risco\" IS NULL OR (\"score_risco\" >= 0 AND \"score_risco\" <= 100)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_analises_score_risco_range",
                table: "analises");
        }
    }
}
