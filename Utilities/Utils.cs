namespace IAC4.Utilities;

internal class Utils
{
    internal static readonly Random RandomGenerator = new();

    internal static bool
        TransitionDone = true,
        CoinReady = true,
        AllowUpdates = false,
        UseCustomTitles = false,
        Replacing = false,
        NameExists = false,
        TextNull = false,
        Loading = false,
        SavePressed = false,
        LoadPressed = false,
        ResetPressed = false,
        DiscardChangesPressed = false,
        SaveChangesPressed = false,
        SaveUnsavedChangesPressed = false,
        ClearAllPressed = false,
        AddProfilePressed = false,
        DeleteProfilePressed = false,
        CoinFlipPressed = false,
        GithubPressed = false,
        GoogleDocPressed = false;

    internal static Dictionary<string, ClickerConstruct> klikers = [];
    internal static Dictionary<string, Thread> klikerThreads = [];

    internal static void SendLogMessage(string message) => Application.Current.Dispatcher.Invoke(() =>
    {
        if (Application.Current.MainWindow is MainWindow mainWindow)
        {
            TextBox textBox = mainWindow.LogOutputTx;
            if (textBox == null)
                return;

            ScrollViewer? scrollViewer = GetScrollViewer(textBox);
            bool isAtBottom = scrollViewer != null && scrollViewer.VerticalOffset >= scrollViewer.ScrollableHeight - 1;
            textBox.AppendText($"[{DateTime.Now:HH:mm:ss:fff}] {message}\n");
            if (isAtBottom)
            {
                textBox.ScrollToEnd();
            }
        }
    });
    internal static bool ClickBindEquals(ClickBind a, ClickBind b) => a.IsKey == b.IsKey &&
               a.Key == b.Key &&
               a.Mouse == b.Mouse;

    internal static void UpdateProfiles()
    {
        ClickerConstruct[] clickers = GetAllKlikers().ToArray();
        foreach (ClickerConstruct? clicker in clickers)
        {
            clicker.UpdateActionBind(clicker.ActionBind);
            clicker.UpdateActivationBind(clicker.ActivationBind);
        }
    }

    private static ScrollViewer? GetScrollViewer(DependencyObject parent)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, i);
            if (child is ScrollViewer viewer)
                return viewer;

            ScrollViewer? result = GetScrollViewer(child);
            if (result != null)
                return result;
        }
        return null;
    }
}