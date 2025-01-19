using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DemocraticTapON.Migrations
{
    /// <inheritdoc />
    public partial class PhonetoEmailVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsPhoneVerified",
                table: "Accounts",
                newName: "IsEmailVerified");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsEmailVerified",
                table: "Accounts",
                newName: "IsPhoneVerified");
        }
    }
}
