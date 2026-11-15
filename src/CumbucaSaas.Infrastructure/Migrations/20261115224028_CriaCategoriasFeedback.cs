using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CumbucaSaas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CriaCategoriasFeedback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Categoria",
                table: "Feedbacks");

            migrationBuilder.AddColumn<int>(
                name: "CategoriaFeedbackId",
                table: "Feedbacks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "CategoriasFeedback",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoriasFeedback", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "CategoriasFeedback",
                columns: new[] { "Id", "Descricao", "Nome" },
                values: new object[,]
                {
                    { 1, "Variedade, sabor e opções do cardápio.", "Cardápio" },
                    { 2, "Tempo de fila e organização da entrada.", "Filas e espera" },
                    { 3, "Educação e agilidade da equipe e reposição da linha.", "Atendimento" },
                    { 4, "Limpeza de mesas, talheres e salão.", "Higiene" },
                    { 5, "Mesas, ventilação e espaço do salão.", "Estrutura" },
                    { 6, "Valor cobrado de estudantes e do público externo.", "Preço" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_CategoriaFeedbackId",
                table: "Feedbacks",
                column: "CategoriaFeedbackId");

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_EstudanteId",
                table: "Feedbacks",
                column: "EstudanteId");

            migrationBuilder.CreateIndex(
                name: "IX_CategoriasFeedback_Nome",
                table: "CategoriasFeedback",
                column: "Nome",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Feedbacks_CategoriasFeedback_CategoriaFeedbackId",
                table: "Feedbacks",
                column: "CategoriaFeedbackId",
                principalTable: "CategoriasFeedback",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Feedbacks_Estudantes_EstudanteId",
                table: "Feedbacks",
                column: "EstudanteId",
                principalTable: "Estudantes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Feedbacks_CategoriasFeedback_CategoriaFeedbackId",
                table: "Feedbacks");

            migrationBuilder.DropForeignKey(
                name: "FK_Feedbacks_Estudantes_EstudanteId",
                table: "Feedbacks");

            migrationBuilder.DropTable(
                name: "CategoriasFeedback");

            migrationBuilder.DropIndex(
                name: "IX_Feedbacks_CategoriaFeedbackId",
                table: "Feedbacks");

            migrationBuilder.DropIndex(
                name: "IX_Feedbacks_EstudanteId",
                table: "Feedbacks");

            migrationBuilder.DropColumn(
                name: "CategoriaFeedbackId",
                table: "Feedbacks");

            migrationBuilder.AddColumn<string>(
                name: "Categoria",
                table: "Feedbacks",
                type: "character varying(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "");
        }
    }
}
