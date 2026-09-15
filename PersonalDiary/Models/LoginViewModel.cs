using System.ComponentModel.DataAnnotations;

namespace PersonalDiary.Models;

public class LoginViewModel
{
    [Required]
    [Display(Name = "User ID")]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
