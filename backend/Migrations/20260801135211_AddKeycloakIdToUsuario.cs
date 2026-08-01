using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GerenciamentoEndereco.API.Migrations
{
    /// <inheritdoc />
    public partial class AddKeycloakIdToUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "KeycloakId",
                table: "Usuarios",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KeycloakId",
                table: "Usuarios");
        }
    }
}
