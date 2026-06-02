using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutomotiveWorkshop.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkOrderPartLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PartId",
                table: "WorkOrderItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PartsIssued",
                table: "WorkOrderItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderItems_PartId",
                table: "WorkOrderItems",
                column: "PartId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkOrderItems_Parts_PartId",
                table: "WorkOrderItems",
                column: "PartId",
                principalTable: "Parts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkOrderItems_Parts_PartId",
                table: "WorkOrderItems");

            migrationBuilder.DropIndex(
                name: "IX_WorkOrderItems_PartId",
                table: "WorkOrderItems");

            migrationBuilder.DropColumn(
                name: "PartId",
                table: "WorkOrderItems");

            migrationBuilder.DropColumn(
                name: "PartsIssued",
                table: "WorkOrderItems");
        }
    }
}
