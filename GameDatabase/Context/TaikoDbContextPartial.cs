using GameDatabase.Entities;
using Microsoft.EntityFrameworkCore;
using SharedProject.Enums;

namespace GameDatabase.Context;

public partial class TaikoDbContext
{
    public virtual DbSet<DanScoreDatum> DanScoreData { get; set; } = null!;
    public virtual DbSet<DanStageScoreDatum> DanStageScoreData { get; set; } = null!;
    public virtual DbSet<AiScoreDatum> AiScoreData { get; set; } = null!;
    public virtual DbSet<AiSectionScoreDatum> AiSectionScoreData { get; set; } = null!;
    public virtual DbSet<Token> Tokens { get; set; } = null!;

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DanScoreDatum>(entity =>
        {
            entity.HasKey(e => new { e.Baid, e.DanId, e.DanType });

            entity.HasOne(d => d.Ba)
                .WithMany()
                .HasPrincipalKey(p => p.Baid)
                .HasForeignKey(d => d.Baid)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.ClearState).HasConversion<uint>().HasDefaultValue(DanClearState.NotClear);
            entity.Property(e => e.DanType).HasConversion<int>().HasDefaultValue(DanType.Normal).HasSentinel((DanType)0);
        });

        modelBuilder.Entity<DanStageScoreDatum>(entity =>
        {
            entity.HasKey(e => new { e.Baid, e.DanId, e.DanType, e.SongNumber });

            entity.HasOne(d => d.Parent)
                .WithMany(p => p.DanStageScoreData)
                .HasForeignKey(d => new { d.Baid, d.DanId, d.DanType })
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.DanType).HasConversion<int>().HasDefaultValue(DanType.Normal).HasSentinel((DanType)0);
        });

        modelBuilder.Entity<AiScoreDatum>(entity =>
        {
            entity.HasKey(e => new { e.Baid, e.SongId, e.Difficulty });

            entity.HasOne(d => d.Ba)
                .WithMany()
                .HasPrincipalKey(p => p.Baid)
                .HasForeignKey(d => d.Baid)
                .OnDelete(DeleteBehavior.Cascade);
        });


        modelBuilder.Entity<AiSectionScoreDatum>(entity =>
        {
            entity.HasKey(e => new { e.Baid, e.SongId, e.Difficulty, e.SectionIndex });

            entity.HasOne(d => d.Parent)
                .WithMany(p => p.AiSectionScoreData)
                .HasPrincipalKey(p => new { p.Baid, p.SongId, p.Difficulty })
                .HasForeignKey(d => new { d.Baid, d.SongId, d.Difficulty })
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Token>(entity =>
        {
            entity.HasKey(e => new { e.Baid, e.Id });

            entity.HasOne(d => d.Datum)
                .WithMany(p => p.Tokens)
                .HasPrincipalKey(p => p.Baid)
                .HasForeignKey(d => d.Baid)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DanSlot>(entity =>
        {
            entity.HasKey(e => e.DanId);

            entity.HasOne(d => d.DanInfo)
                .WithMany()
                .HasPrincipalKey(p => p.DanId)
                .HasForeignKey(d => d.BindDanId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<DanInfo>(entity =>
        {
            entity.HasKey(e => e.DanId);
        });

        modelBuilder.Entity<DanSong>(entity =>
        {
            entity.HasKey(e => new { e.DanId, e.SongIdx });

            entity.HasOne(d => d.DanInfo)
                .WithMany(p => p.AryOdaiSong)
                .HasPrincipalKey(p => p.DanId)
                .HasForeignKey(d => d.DanId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DanBorder>(entity =>
        {
            entity.HasKey(e => new { e.DanId, e.BorderIdx });

            entity.HasOne(d => d.DanInfo)
                .WithMany(p => p.AryOdaiBorder)
                .HasPrincipalKey(p => p.DanId)
                .HasForeignKey(d => d.DanId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}