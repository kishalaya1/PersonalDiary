using System.ComponentModel.DataAnnotations;

namespace PersonalDiary.Models;

public class DiaryEntryInputModel
{
    [DataType(DataType.Date)]
    [Display(Name = "Date")]
    public DateOnly EntryDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Required]
    [StringLength(20_000, MinimumLength = 1)]
    [Display(Name = "Notes")]
    public string Notes { get; set; } = string.Empty;
}
