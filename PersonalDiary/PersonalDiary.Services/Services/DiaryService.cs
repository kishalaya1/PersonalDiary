using Microsoft.EntityFrameworkCore;
using PersonalDiary.Services.Data;
using PersonalDiary.Services.Models;

namespace PersonalDiary.Services;

public interface IDiaryService
{
    Task<IReadOnlyList<DiaryEntry>> GetEntriesAsync(string userId, CancellationToken cancellationToken = default);
    Task<DiaryEntry?> GetEntryAsync(string userId, DateOnly date, CancellationToken cancellationToken = default);
    Task<bool> CreateEntryAsync(string userId, DateOnly date, string notes, CancellationToken cancellationToken = default);
    Task<DiaryUpdateResult> UpdateEntryAsync(string userId, DateOnly date, string notes, CancellationToken cancellationToken = default);
}

public enum DiaryUpdateResult
{
    Updated,
    NotFound,
    EditLimitReached
}

public sealed class DiaryService(ApplicationDbContext dbContext) : IDiaryService
{
    public const int MaximumEdits = 5;

    public async Task<IReadOnlyList<DiaryEntry>> GetEntriesAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.DiaryEntries
            .AsNoTracking()
            .Where(entry => entry.UserId == userId)
            .OrderByDescending(entry => entry.EntryDate)
            .ToListAsync(cancellationToken);
    }

    public Task<DiaryEntry?> GetEntryAsync(
        string userId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        return dbContext.DiaryEntries
            .AsNoTracking()
            .SingleOrDefaultAsync(
                entry => entry.UserId == userId && entry.EntryDate == date,
                cancellationToken);
    }

    public async Task<bool> CreateEntryAsync(
        string userId,
        DateOnly date,
        string notes,
        CancellationToken cancellationToken = default)
    {
        var alreadyExists = await dbContext.DiaryEntries
            .AnyAsync(entry => entry.UserId == userId && entry.EntryDate == date, cancellationToken);

        if (alreadyExists)
        {
            return false;
        }

        var now = DateTime.UtcNow;
        dbContext.DiaryEntries.Add(new DiaryEntry
        {
            UserId = userId,
            EntryDate = date,
            Notes = notes.Trim(),
            CreatedUtc = now,
            UpdatedUtc = now
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<DiaryUpdateResult> UpdateEntryAsync(
        string userId,
        DateOnly date,
        string notes,
        CancellationToken cancellationToken = default)
    {
        var entry = await dbContext.DiaryEntries
            .SingleOrDefaultAsync(
                diaryEntry => diaryEntry.UserId == userId && diaryEntry.EntryDate == date,
                cancellationToken);

        if (entry is null)
        {
            return DiaryUpdateResult.NotFound;
        }

        if (entry.EditCount >= MaximumEdits)
        {
            return DiaryUpdateResult.EditLimitReached;
        }

        entry.Notes = notes.Trim();
        entry.EditCount++;
        entry.UpdatedUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return DiaryUpdateResult.Updated;
    }
}
