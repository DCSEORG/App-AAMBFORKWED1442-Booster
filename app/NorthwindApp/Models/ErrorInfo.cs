namespace NorthwindApp.Models;

public class ErrorInfo
{
    public string Message { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public int LineNumber { get; set; }
    public string ManagedIdentityHint { get; set; } = string.Empty;
}
