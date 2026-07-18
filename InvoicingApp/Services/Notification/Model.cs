
namespace InvoicingApp.Services.Notification;

public class ToastMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Message { get; set; } = string.Empty;
    public ToastLevel Level { get; set; } = ToastLevel.Info;
}

public enum ToastLevel
{
    Info,
    Success,
    Warning,
    Error
}
