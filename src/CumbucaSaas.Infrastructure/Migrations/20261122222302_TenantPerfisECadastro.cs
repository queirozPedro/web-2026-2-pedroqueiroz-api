using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CumbucaSaas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TenantPerfisECadastro : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Estudantes_Email",
                table: "Estudantes");

            migrationBuilder.DropIndex(
                name: "IX_Estudantes_Matricula",
                table: "Estudantes");

            migrationBuilder.DropIndex(
                name: "IX_CalendarioRefeicoes_Data_TurnoId",
                table: "CalendarioRefeicoes");

            migrationBuilder.AlterColumn<string>(
                name: "Nome",
                table: "Usuarios",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AddColumn<DateTime>(
                name: "CadastroConcluidoEm",
                table: "Usuarios",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DataNascimento",
                table: "Usuarios",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Usuarios",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "RegistrosConsumo",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "HistoricoAlteracoes",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Feedbacks",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Estudantes",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "CalendarioRefeicoes",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    NomeResponsavel = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Tenants",
                columns: new[] { "Id", "Nome", "NomeResponsavel" },
                values: new object[] { 1, "Restaurante Universitário: Ufersa - Campus Pau dos Ferros", "Pedro Andrade" });

            // O Id 1 foi inserido à mão: avança a sequência da identidade para o próximo tenant não repetir o 1.
            migrationBuilder.Sql(
                """SELECT setval(pg_get_serial_sequence('"Tenants"', 'Id'), (SELECT MAX("Id") FROM "Tenants"));""");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_TenantId",
                table: "Usuarios",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosConsumo_TenantId",
                table: "RegistrosConsumo",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoricoAlteracoes_TenantId",
                table: "HistoricoAlteracoes",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_TenantId",
                table: "Feedbacks",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Estudantes_TenantId_Email",
                table: "Estudantes",
                columns: new[] { "TenantId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Estudantes_TenantId_Matricula",
                table: "Estudantes",
                columns: new[] { "TenantId", "Matricula" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CalendarioRefeicoes_TenantId_Data_TurnoId",
                table: "CalendarioRefeicoes",
                columns: new[] { "TenantId", "Data", "TurnoId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CalendarioRefeicoes_Tenants_TenantId",
                table: "CalendarioRefeicoes",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Estudantes_Tenants_TenantId",
                table: "Estudantes",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Feedbacks_Tenants_TenantId",
                table: "Feedbacks",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_HistoricoAlteracoes_Tenants_TenantId",
                table: "HistoricoAlteracoes",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RegistrosConsumo_Tenants_TenantId",
                table: "RegistrosConsumo",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_Tenants_TenantId",
                table: "Usuarios",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CalendarioRefeicoes_Tenants_TenantId",
                table: "CalendarioRefeicoes");

            migrationBuilder.DropForeignKey(
                name: "FK_Estudantes_Tenants_TenantId",
                table: "Estudantes");

            migrationBuilder.DropForeignKey(
                name: "FK_Feedbacks_Tenants_TenantId",
                table: "Feedbacks");

            migrationBuilder.DropForeignKey(
                name: "FK_HistoricoAlteracoes_Tenants_TenantId",
                table: "HistoricoAlteracoes");

            migrationBuilder.DropForeignKey(
                name: "FK_RegistrosConsumo_Tenants_TenantId",
                table: "RegistrosConsumo");

            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_Tenants_TenantId",
                table: "Usuarios");

            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_Usuarios_TenantId",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_RegistrosConsumo_TenantId",
                table: "RegistrosConsumo");

            migrationBuilder.DropIndex(
                name: "IX_HistoricoAlteracoes_TenantId",
                table: "HistoricoAlteracoes");

            migrationBuilder.DropIndex(
                name: "IX_Feedbacks_TenantId",
                table: "Feedbacks");

            migrationBuilder.DropIndex(
                name: "IX_Estudantes_TenantId_Email",
                table: "Estudantes");

            migrationBuilder.DropIndex(
                name: "IX_Estudantes_TenantId_Matricula",
                table: "Estudantes");

            migrationBuilder.DropIndex(
                name: "IX_CalendarioRefeicoes_TenantId_Data_TurnoId",
                table: "CalendarioRefeicoes");

            migrationBuilder.DropColumn(
                name: "CadastroConcluidoEm",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "DataNascimento",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "RegistrosConsumo");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "HistoricoAlteracoes");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Feedbacks");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Estudantes");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "CalendarioRefeicoes");

            migrationBuilder.AlterColumn<string>(
                name: "Nome",
                table: "Usuarios",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Estudantes_Email",
                table: "Estudantes",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Estudantes_Matricula",
                table: "Estudantes",
                column: "Matricula",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CalendarioRefeicoes_Data_TurnoId",
                table: "CalendarioRefeicoes",
                columns: new[] { "Data", "TurnoId" },
                unique: true);
        }
    }
}
