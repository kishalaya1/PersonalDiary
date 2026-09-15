using System.ComponentModel.DataAnnotations;

namespace PersonalDiary.Models;

public class DiaryEntry
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public DateOnly EntryDate { get; set; }

    [Required]
    [StringLength(20_000, MinimumLength = 1)]
    public string Notes { get; set; } = string.Empty;

    public int EditCount { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime UpdatedUtc { get; set; }
}
