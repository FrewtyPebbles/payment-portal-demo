
namespace InvoicingApp.Services.Notification;

public class NotificationService
{
    public List<ToastMessage> Toasts { get; } = [];
    
    public event Action? OnNotify;

    public void ShowToast(string message, ToastLevel level = ToastLevel.Info)
    {
        var toast = new ToastMessage { Message = message, Level = level };
        lock (Toasts)
        {
            Toasts.Add(toast);
        }
        
        OnNotify?.Invoke();

        _ = RemoveToastAfterDelayAsync(toast);
    }

    public void RemoveToast(ToastMessage toast)
    {
        lock (Toasts)
        {
            if (Toasts.Remove(toast))
            {
                OnNotify?.Invoke();
            }
        }
    }


    private async Task RemoveToastAfterDelayAsync(ToastMessage toast)
    {
        await Task.Delay(4000);
        RemoveToast(toast);
    }
}
