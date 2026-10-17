using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sso.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address_Country",
                table: "AspNetUsers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address_Locality",
                table: "AspNetUsers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address_PostalCode",
                table: "AspNetUsers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address_Region",
                table: "AspNetUsers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address_StreetAddress",
                table: "AspNetUsers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "Birthdate",
                table: "AspNetUsers",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FamilyName",
                table: "AspNetUsers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GivenName",
                table: "AspNetUsers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address_Country",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Address_Locality",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Address_PostalCode",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Address_Region",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Address_StreetAddress",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Birthdate",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "FamilyName",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "GivenName",
                table: "AspNetUsers");
        }
    }
}
