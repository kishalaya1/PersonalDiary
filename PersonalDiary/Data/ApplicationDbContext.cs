using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PersonalDiary.Models;

namespace PersonalDiary.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext(options)
{
    public DbSet<DiaryEntry> DiaryEntries => Set<DiaryEntry>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<DiaryEntry>(entity =>
        {
            entity.HasIndex(entry => new { entry.UserId, entry.EntryDate })
                .IsUnique();

            entity.Property(entry => entry.EntryDate)
                .HasConversion(
                    date => date.ToDateTime(TimeOnly.MinValue),
                    value => DateOnly.FromDateTime(value));

            entity.Property(entry => entry.Notes)
                .IsRequired();
        });
    }
}
