using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CumbucaSaas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DadosDoRestaurante : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Cidade",
                table: "Tenants",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CorPrincipal",
                table: "Tenants",
                type: "character(7)",
                fixedLength: true,
                maxLength: 7,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CorSecundaria",
                table: "Tenants",
                type: "character(7)",
                fixedLength: true,
                maxLength: 7,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Endereco",
                table: "Tenants",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FotoChave",
                table: "Tenants",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Uf",
                table: "Tenants",
                type: "character(2)",
                fixedLength: true,
                maxLength: 2,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "HorariosFuncionamento",
                columns: table => new
                {
                    DiaSemana = table.Column<int>(type: "integer", nullable: false),
                    TurnoId = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    Abertura = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    Fechamento = table.Column<TimeOnly>(type: "time without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HorariosFuncionamento", x => new { x.TenantId, x.DiaSemana, x.TurnoId });
                    table.ForeignKey(
                        name: "FK_HorariosFuncionamento_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "HorariosFuncionamento",
                columns: new[] { "DiaSemana", "TenantId", "TurnoId", "Abertura", "Fechamento" },
                values: new object[,]
                {
                    { 1, 1, 1, new TimeOnly(11, 0, 0), new TimeOnly(13, 30, 0) },
                    { 1, 1, 2, new TimeOnly(17, 0, 0), new TimeOnly(19, 0, 0) },
                    { 2, 1, 1, new TimeOnly(11, 0, 0), new TimeOnly(13, 30, 0) },
                    { 2, 1, 2, new TimeOnly(17, 0, 0), new TimeOnly(19, 0, 0) },
                    { 3, 1, 1, new TimeOnly(11, 0, 0), new TimeOnly(13, 30, 0) },
                    { 3, 1, 2, new TimeOnly(17, 0, 0), new TimeOnly(19, 0, 0) },
                    { 4, 1, 1, new TimeOnly(11, 0, 0), new TimeOnly(13, 30, 0) },
                    { 4, 1, 2, new TimeOnly(17, 0, 0), new TimeOnly(19, 0, 0) },
                    { 5, 1, 1, new TimeOnly(11, 0, 0), new TimeOnly(13, 30, 0) },
                    { 5, 1, 2, new TimeOnly(17, 0, 0), new TimeOnly(19, 0, 0) }
                });

            migrationBuilder.UpdateData(
                table: "Tenants",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Cidade", "CorPrincipal", "CorSecundaria", "Endereco", "FotoChave", "Uf" },
                values: new object[] { "Pau dos Ferros", "#1b4535", "#c2571e", null, null, "RN" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HorariosFuncionamento");

            migrationBuilder.DropColumn(
                name: "Cidade",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "CorPrincipal",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "CorSecundaria",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Endereco",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "FotoChave",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Uf",
                table: "Tenants");
        }
    }
}
