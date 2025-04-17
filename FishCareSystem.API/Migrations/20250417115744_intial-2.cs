using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FishCareSystem.API.Migrations
{
    /// <inheritdoc />
    public partial class intial2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Alerts_Tanks_TankId",
                table: "Alerts");

            migrationBuilder.DropForeignKey(
                name: "FK_Devices_Tanks_TankId",
                table: "Devices");

            migrationBuilder.DropForeignKey(
                name: "FK_SensorReadings_Tanks_TankId",
                table: "SensorReadings");

            migrationBuilder.DropForeignKey(
                name: "FK_Tanks_Farms_FarmId",
                table: "Tanks");

            migrationBuilder.AddForeignKey(
                name: "FK_Alerts_Tanks_TankId",
                table: "Alerts",
                column: "TankId",
                principalTable: "Tanks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Devices_Tanks_TankId",
                table: "Devices",
                column: "TankId",
                principalTable: "Tanks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SensorReadings_Tanks_TankId",
                table: "SensorReadings",
                column: "TankId",
                principalTable: "Tanks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tanks_Farms_FarmId",
                table: "Tanks",
                column: "FarmId",
                principalTable: "Farms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Alerts_Tanks_TankId",
                table: "Alerts");

            migrationBuilder.DropForeignKey(
                name: "FK_Devices_Tanks_TankId",
                table: "Devices");

            migrationBuilder.DropForeignKey(
                name: "FK_SensorReadings_Tanks_TankId",
                table: "SensorReadings");

            migrationBuilder.DropForeignKey(
                name: "FK_Tanks_Farms_FarmId",
                table: "Tanks");

            migrationBuilder.AddForeignKey(
                name: "FK_Alerts_Tanks_TankId",
                table: "Alerts",
                column: "TankId",
                principalTable: "Tanks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Devices_Tanks_TankId",
                table: "Devices",
                column: "TankId",
                principalTable: "Tanks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SensorReadings_Tanks_TankId",
                table: "SensorReadings",
                column: "TankId",
                principalTable: "Tanks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Tanks_Farms_FarmId",
                table: "Tanks",
                column: "FarmId",
                principalTable: "Farms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
