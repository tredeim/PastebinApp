using System.ComponentModel.DataAnnotations;

namespace PastebinApp.Api.Models.Requests;

public class CreatePasteRequest
{
    [Required(ErrorMessage = "Content is required")]
    [StringLength(524288, MinimumLength = 1, ErrorMessage = "Content must be between 1 and 524288 characters (512 KB)")]
    public string Content { get; set; } = string.Empty;
    
    [Range(1, int.MaxValue, ErrorMessage = "Expiration hours must be greater than or equal to 1")]
    public int ExpirationHours { get; set; } = 24;
    
    [StringLength(50, ErrorMessage = "Language cannot exceed 50 characters")]
    public string? Language { get; set; }
    
    [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
    public string? Title { get; set; }
}