namespace MrLee.Web.Models;

public class EmptyMessageVm
{
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public string? SupportText { get; set; }
    public string? ActionText { get; set; }
    public string? ActionUrl { get; set; }
    public string Variant { get; set; } = "default";
}
