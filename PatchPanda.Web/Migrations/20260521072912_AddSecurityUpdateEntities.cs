using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PatchPanda.Web.Migrations
{
    /// <inheritdoc />
    internal partial class AddSecurityUpdateEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            migrationBuilder.DropColumn(
                name: "AIBreaking",
                table: "AppVersions");

            migrationBuilder.DropColumn(
                name: "AISummary",
                table: "AppVersions");

            migrationBuilder.DropColumn(
                name: "Body",
                table: "AppVersions");

            migrationBuilder.DropColumn(
                name: "Breaking",
                table: "AppVersions");

            migrationBuilder.DropColumn(
                name: "DateDiscovered",
                table: "AppVersions");

            migrationBuilder.DropColumn(
                name: "Ignored",
                table: "AppVersions");

            migrationBuilder.DropColumn(
                name: "IsSuspectedMalicious",
                table: "AppVersions");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "AppVersions");

            migrationBuilder.DropColumn(
                name: "Notified",
                table: "AppVersions");

            migrationBuilder.DropColumn(
                name: "Prerelease",
                table: "AppVersions");

            migrationBuilder.DropColumn(
                name: "SecurityAnalysis",
                table: "AppVersions");

            migrationBuilder.DropColumn(
                name: "VersionNumber",
                table: "AppVersions");

            migrationBuilder.DropColumn(
                name: "Value",
                table: "AppSettings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            migrationBuilder.AddColumn<bool>(
                name: "AIBreaking",
                table: "AppVersions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AISummary",
                table: "AppVersions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Body",
                table: "AppVersions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "Breaking",
                table: "AppVersions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateDiscovered",
                table: "AppVersions",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "Ignored",
                table: "AppVersions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSuspectedMalicious",
                table: "AppVersions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "AppVersions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "Notified",
                table: "AppVersions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Prerelease",
                table: "AppVersions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SecurityAnalysis",
                table: "AppVersions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VersionNumber",
                table: "AppVersions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Value",
                table: "AppSettings",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: "");
        }
    }
}
