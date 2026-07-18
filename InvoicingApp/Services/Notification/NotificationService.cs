
namespace InvoicingApp.Services.Notification;

public class NotificationService
{
    public List<ToastMessage> Toasts { get; } = new();
    
    public event Action? OnNotify;

    public void ShowToast(string message, ToastLevel level = ToastLevel.Info)
    {
        var toast = new ToastMessage { Message = message, Level = level };
        Toasts.Add(toast);
        
        OnNotify?.Invoke();

        _ = RemoveToastAfterDelayAsync(toast);
    }

    private async Task RemoveToastAfterDelayAsync(ToastMessage toast)
    {
        await Task.Delay(4000);
        Toasts.Remove(toast);
        OnNotify?.Invoke();
    }
}
