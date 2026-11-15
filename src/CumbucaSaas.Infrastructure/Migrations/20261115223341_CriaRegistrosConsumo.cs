using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CumbucaSaas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CriaRegistrosConsumo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RegistrosConsumo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CalendarioRefeicaoId = table.Column<int>(type: "integer", nullable: false),
                    TipoClienteId = table.Column<int>(type: "integer", nullable: false),
                    EstudanteId = table.Column<int>(type: "integer", nullable: true),
                    Horario = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    ValorPago = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    FormaPagamentoId = table.Column<int>(type: "integer", nullable: true),
                    FormaIdentificacaoId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosConsumo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegistrosConsumo_CalendarioRefeicoes_CalendarioRefeicaoId",
                        column: x => x.CalendarioRefeicaoId,
                        principalTable: "CalendarioRefeicoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RegistrosConsumo_Estudantes_EstudanteId",
                        column: x => x.EstudanteId,
                        principalTable: "Estudantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosConsumo_CalendarioRefeicaoId",
                table: "RegistrosConsumo",
                column: "CalendarioRefeicaoId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosConsumo_EstudanteId",
                table: "RegistrosConsumo",
                column: "EstudanteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RegistrosConsumo");
        }
    }
}
