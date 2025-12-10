using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShadowFox.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUsageTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UsageSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProfileId = table.Column<int>(type: "INTEGER", nullable: false),
                    StartTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UserAgent = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IpAddress = table.Column<string>(type: "TEXT", maxLength: 45, nullable: true),
                    SessionNotes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsageSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsageSessions_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UsageSessions_EndTime",
                table: "UsageSessions",
                column: "EndTime");

            migrationBuilder.CreateIndex(
                name: "IX_UsageSessions_ProfileId",
                table: "UsageSessions",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_UsageSessions_StartTime",
                table: "UsageSessions",
                column: "StartTime");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UsageSessions");
        }
    }
}