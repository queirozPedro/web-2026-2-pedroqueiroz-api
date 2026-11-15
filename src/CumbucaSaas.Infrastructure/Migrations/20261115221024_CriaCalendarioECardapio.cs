using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CumbucaSaas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CriaCalendarioECardapio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CalendarioRefeicoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    TurnoId = table.Column<int>(type: "integer", nullable: false),
                    Abertura = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    Fechamento = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    Observacoes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarioRefeicoes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Cardapios",
                columns: table => new
                {
                    CalendarioRefeicaoId = table.Column<int>(type: "integer", nullable: false),
                    Itens = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CategoriaNutricional = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    PrecoSubsidiado = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    PrecoIntegral = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cardapios", x => x.CalendarioRefeicaoId);
                    table.ForeignKey(
                        name: "FK_Cardapios_CalendarioRefeicoes_CalendarioRefeicaoId",
                        column: x => x.CalendarioRefeicaoId,
                        principalTable: "CalendarioRefeicoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CalendarioRefeicoes_Data_TurnoId",
                table: "CalendarioRefeicoes",
                columns: new[] { "Data", "TurnoId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cardapios");

            migrationBuilder.DropTable(
                name: "CalendarioRefeicoes");
        }
    }
}
