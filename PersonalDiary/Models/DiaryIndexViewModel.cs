namespace PersonalDiary.Models;

public class DiaryIndexViewModel
{
    public IReadOnlyList<DiaryEntry> Entries { get; init; } = [];

    public int MaximumEdits { get; init; }
}
