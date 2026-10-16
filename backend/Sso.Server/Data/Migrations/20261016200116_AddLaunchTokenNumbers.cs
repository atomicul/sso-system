using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sso.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLaunchTokenNumbers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "LaunchTokenNumbers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropSequence(
                name: "LaunchTokenNumbers");
        }
    }
}
