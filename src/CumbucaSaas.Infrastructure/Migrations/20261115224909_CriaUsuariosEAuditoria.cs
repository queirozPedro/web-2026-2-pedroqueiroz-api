using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CumbucaSaas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CriaUsuariosEAuditoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UsuarioId",
                table: "Estudantes",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdExterno = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    PerfilId = table.Column<int>(type: "integer", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HistoricoAlteracoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UsuarioId = table.Column<int>(type: "integer", nullable: true),
                    Tabela = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AcaoId = table.Column<int>(type: "integer", nullable: false),
                    DataHora = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ValorAnterior = table.Column<string>(type: "jsonb", nullable: true),
                    ValorNovo = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoricoAlteracoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistoricoAlteracoes_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Estudantes_UsuarioId",
                table: "Estudantes",
                column: "UsuarioId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HistoricoAlteracoes_DataHora",
                table: "HistoricoAlteracoes",
                column: "DataHora");

            migrationBuilder.CreateIndex(
                name: "IX_HistoricoAlteracoes_UsuarioId",
                table: "HistoricoAlteracoes",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Email",
                table: "Usuarios",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_IdExterno",
                table: "Usuarios",
                column: "IdExterno",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Estudantes_Usuarios_UsuarioId",
                table: "Estudantes",
                column: "UsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Estudantes_Usuarios_UsuarioId",
                table: "Estudantes");

            migrationBuilder.DropTable(
                name: "HistoricoAlteracoes");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_Estudantes_UsuarioId",
                table: "Estudantes");

            migrationBuilder.DropColumn(
                name: "UsuarioId",
                table: "Estudantes");
        }
    }
}
