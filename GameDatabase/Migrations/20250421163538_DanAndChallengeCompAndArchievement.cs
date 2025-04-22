using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameDatabase.Migrations
{
    /// <inheritdoc />
    public partial class DanAndChallengeCompAndArchievement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChallengeCompeteArchievementData",
                columns: table => new
                {
                    Baid = table.Column<uint>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChallengeCount = table.Column<uint>(type: "INTEGER", nullable: false),
                    ChallengeWin = table.Column<uint>(type: "INTEGER", nullable: false),
                    ChallengeLose = table.Column<uint>(type: "INTEGER", nullable: false),
                    CompeteCount = table.Column<uint>(type: "INTEGER", nullable: false),
                    CompeteGold = table.Column<uint>(type: "INTEGER", nullable: false),
                    CompeteSilver = table.Column<uint>(type: "INTEGER", nullable: false),
                    CompeteCopper = table.Column<uint>(type: "INTEGER", nullable: false),
                    OfficialCompeteCount = table.Column<uint>(type: "INTEGER", nullable: false),
                    OfficialCompeteGold = table.Column<uint>(type: "INTEGER", nullable: false),
                    OfficialCompeteSilver = table.Column<uint>(type: "INTEGER", nullable: false),
                    OfficialCompeteCopper = table.Column<uint>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChallengeCompeteArchievementData", x => x.Baid);
                });

            migrationBuilder.CreateTable(
                name: "ChallengeCompeteData",
                columns: table => new
                {
                    CompId = table.Column<uint>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CompeteMode = table.Column<uint>(type: "INTEGER", nullable: false),
                    State = table.Column<uint>(type: "INTEGER", nullable: false),
                    Baid = table.Column<uint>(type: "INTEGER", nullable: false),
                    CompeteName = table.Column<string>(type: "TEXT", nullable: false),
                    CompeteDescribe = table.Column<string>(type: "TEXT", nullable: false),
                    MaxParticipant = table.Column<uint>(type: "INTEGER", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "datetime", nullable: false),
                    ExpireTime = table.Column<DateTime>(type: "datetime", nullable: false),
                    BeginTime = table.Column<DateTime>(type: "datetime", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime", nullable: false),
                    LastFor = table.Column<uint>(type: "INTEGER", nullable: false),
                    RequireTitle = table.Column<uint>(type: "INTEGER", nullable: false),
                    OnlyPlayOnce = table.Column<bool>(type: "INTEGER", nullable: false),
                    Share = table.Column<uint>(type: "INTEGER", nullable: false),
                    CompeteTarget = table.Column<uint>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChallengeCompeteData", x => x.CompId);
                });

            migrationBuilder.CreateTable(
                name: "DanInfo",
                columns: table => new
                {
                    DanId = table.Column<uint>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DanInfo", x => x.DanId);
                });

            migrationBuilder.CreateTable(
                name: "ChallengeCompeteParticipantData",
                columns: table => new
                {
                    CompId = table.Column<uint>(type: "INTEGER", nullable: false),
                    Baid = table.Column<uint>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChallengeCompeteParticipantData", x => new { x.CompId, x.Baid });
                    table.ForeignKey(
                        name: "FK_ChallengeCompeteParticipantData_ChallengeCompeteData_CompId",
                        column: x => x.CompId,
                        principalTable: "ChallengeCompeteData",
                        principalColumn: "CompId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChallengeCompeteParticipantData_UserData_Baid",
                        column: x => x.Baid,
                        principalTable: "UserData",
                        principalColumn: "Baid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChallengeCompeteSongData",
                columns: table => new
                {
                    CompId = table.Column<uint>(type: "INTEGER", nullable: false),
                    SongIndex = table.Column<uint>(type: "INTEGER", nullable: false),
                    SongId = table.Column<uint>(type: "INTEGER", nullable: false),
                    Difficulty = table.Column<uint>(type: "INTEGER", nullable: false),
                    Speed = table.Column<uint>(type: "INTEGER", nullable: true),
                    IsVanishOn = table.Column<bool>(type: "INTEGER", nullable: true),
                    IsInverseOn = table.Column<bool>(type: "INTEGER", nullable: true),
                    RandomType = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChallengeCompeteSongData", x => new { x.CompId, x.SongIndex });
                    table.ForeignKey(
                        name: "FK_ChallengeCompeteSongData_ChallengeCompeteData_CompId",
                        column: x => x.CompId,
                        principalTable: "ChallengeCompeteData",
                        principalColumn: "CompId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DanBorder",
                columns: table => new
                {
                    DanId = table.Column<uint>(type: "INTEGER", nullable: false),
                    BorderIdx = table.Column<uint>(type: "INTEGER", nullable: false),
                    OdaiType = table.Column<int>(type: "INTEGER", nullable: false),
                    BorderType = table.Column<int>(type: "INTEGER", nullable: false),
                    RedBorderTotal = table.Column<uint>(type: "INTEGER", nullable: false),
                    GoldBorderTotal = table.Column<uint>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DanBorder", x => new { x.DanId, x.BorderIdx });
                    table.ForeignKey(
                        name: "FK_DanBorder_DanInfo_DanId",
                        column: x => x.DanId,
                        principalTable: "DanInfo",
                        principalColumn: "DanId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DanSlot",
                columns: table => new
                {
                    DanId = table.Column<uint>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BindDanId = table.Column<uint>(type: "INTEGER", nullable: false),
                    VerupNo = table.Column<uint>(type: "INTEGER", nullable: false),
                    DanType = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DanSlot", x => x.DanId);
                    table.ForeignKey(
                        name: "FK_DanSlot_DanInfo_BindDanId",
                        column: x => x.BindDanId,
                        principalTable: "DanInfo",
                        principalColumn: "DanId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DanSong",
                columns: table => new
                {
                    DanId = table.Column<uint>(type: "INTEGER", nullable: false),
                    SongIdx = table.Column<uint>(type: "INTEGER", nullable: false),
                    SongNo = table.Column<uint>(type: "INTEGER", nullable: false),
                    Level = table.Column<uint>(type: "INTEGER", nullable: false),
                    IsHiddenSongName = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DanSong", x => new { x.DanId, x.SongIdx });
                    table.ForeignKey(
                        name: "FK_DanSong_DanInfo_DanId",
                        column: x => x.DanId,
                        principalTable: "DanInfo",
                        principalColumn: "DanId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChallengeCompeteBestData",
                columns: table => new
                {
                    BestId = table.Column<uint>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CompId = table.Column<uint>(type: "INTEGER", nullable: false),
                    SongIndex = table.Column<uint>(type: "INTEGER", nullable: false),
                    Baid = table.Column<uint>(type: "INTEGER", nullable: false),
                    SongId = table.Column<uint>(type: "INTEGER", nullable: false),
                    Difficulty = table.Column<uint>(type: "INTEGER", nullable: false),
                    Crown = table.Column<uint>(type: "INTEGER", nullable: false),
                    Score = table.Column<uint>(type: "INTEGER", nullable: false),
                    ScoreRate = table.Column<uint>(type: "INTEGER", nullable: false),
                    ScoreRank = table.Column<uint>(type: "INTEGER", nullable: false),
                    GoodCount = table.Column<uint>(type: "INTEGER", nullable: false),
                    OkCount = table.Column<uint>(type: "INTEGER", nullable: false),
                    MissCount = table.Column<uint>(type: "INTEGER", nullable: false),
                    ComboCount = table.Column<uint>(type: "INTEGER", nullable: false),
                    HitCount = table.Column<uint>(type: "INTEGER", nullable: false),
                    DrumrollCount = table.Column<uint>(type: "INTEGER", nullable: false),
                    Skipped = table.Column<bool>(type: "INTEGER", nullable: false),
                    PlayCount = table.Column<uint>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChallengeCompeteBestData", x => x.BestId);
                    table.ForeignKey(
                        name: "FK_ChallengeCompeteBestData_ChallengeCompeteSongData_CompId_SongIndex",
                        columns: x => new { x.CompId, x.SongIndex },
                        principalTable: "ChallengeCompeteSongData",
                        principalColumns: new[] { "CompId", "SongIndex" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChallengeCompeteBestData_UserData_Baid",
                        column: x => x.Baid,
                        principalTable: "UserData",
                        principalColumn: "Baid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeCompeteBestData_Baid",
                table: "ChallengeCompeteBestData",
                column: "Baid");

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeCompeteBestData_CompId_SongIndex",
                table: "ChallengeCompeteBestData",
                columns: new[] { "CompId", "SongIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeCompeteParticipantData_Baid",
                table: "ChallengeCompeteParticipantData",
                column: "Baid");

            migrationBuilder.CreateIndex(
                name: "IX_DanSlot_BindDanId",
                table: "DanSlot",
                column: "BindDanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChallengeCompeteArchievementData");

            migrationBuilder.DropTable(
                name: "ChallengeCompeteBestData");

            migrationBuilder.DropTable(
                name: "ChallengeCompeteParticipantData");

            migrationBuilder.DropTable(
                name: "DanBorder");

            migrationBuilder.DropTable(
                name: "DanSlot");

            migrationBuilder.DropTable(
                name: "DanSong");

            migrationBuilder.DropTable(
                name: "ChallengeCompeteSongData");

            migrationBuilder.DropTable(
                name: "DanInfo");

            migrationBuilder.DropTable(
                name: "ChallengeCompeteData");
        }
    }
}
