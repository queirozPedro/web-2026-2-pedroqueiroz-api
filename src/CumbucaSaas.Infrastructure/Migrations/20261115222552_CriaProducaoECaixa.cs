using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CumbucaSaas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CriaProducaoECaixa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FechamentosCaixa",
                columns: table => new
                {
                    CalendarioRefeicaoId = table.Column<int>(type: "integer", nullable: false),
                    ValorDinheiro = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    ValorPix = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FechamentosCaixa", x => x.CalendarioRefeicaoId);
                    table.ForeignKey(
                        name: "FK_FechamentosCaixa_CalendarioRefeicoes_CalendarioRefeicaoId",
                        column: x => x.CalendarioRefeicaoId,
                        principalTable: "CalendarioRefeicoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Producoes",
                columns: table => new
                {
                    CalendarioRefeicaoId = table.Column<int>(type: "integer", nullable: false),
                    QuantidadeProduzida = table.Column<int>(type: "integer", nullable: false),
                    Observacoes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Producoes", x => x.CalendarioRefeicaoId);
                    table.ForeignKey(
                        name: "FK_Producoes_CalendarioRefeicoes_CalendarioRefeicaoId",
                        column: x => x.CalendarioRefeicaoId,
                        principalTable: "CalendarioRefeicoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FechamentosCaixa");

            migrationBuilder.DropTable(
                name: "Producoes");
        }
    }
}
