using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OCULIS.Migrations
{
    public partial class DodanaCijenaDostaveNarudzbi : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "CijenaDostave",
                table: "Narudzba",
                type: "float",
                nullable: false,
                defaultValue: 0d);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CijenaDostave",
                table: "Narudzba");
        }
    }
}
