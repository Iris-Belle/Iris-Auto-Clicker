namespace IAC4.Core;

internal static class ClickerManager
{
    internal static void CreateKliker(
        string name,
        ClickBind activationBind,
        ClickBind actionBind,
        ushort holdDuration,
        ushort delay,
        ushort maxDelay,
        ushort burstCount,
        bool holdMode,
        bool toggleMode,
        bool burstMode,
        bool shouldSpam = true)
    {
        if (klikers.ContainsKey(name))
        {
            Replacing = true;
            DeleteKliker(name);
            CreateKliker(name, activationBind, actionBind, holdDuration, delay, maxDelay, burstCount, holdMode, toggleMode, burstMode, shouldSpam);
            return;
        }

        ClickerConstruct clicker = new(
            name,
            activationBind,
            actionBind,
            holdDuration,
            delay,
            maxDelay,
            burstCount,
            holdMode,
            toggleMode,
            burstMode);

        klikers[name] = clicker;
        Thread thread = new(clicker.ThreadExecute)
        {
            IsBackground = false,
            Priority = ThreadPriority.Highest
        };
        klikerThreads[name] = thread;
        thread.Start();

        if (Replacing)
        {
            Replacing = false;
        }
        else if (shouldSpam)
        {
            SendLogMessage($"Kliker profile '{name}' created successfully."); //yippeeeee
        }
    }

    internal static bool UpdateKliker
    (
        string name,
        ClickBind activationBind,
        ClickBind actionBind,
        ushort holdDuration,
        ushort delay,
        ushort maxDelay,
        ushort burstCount,
        bool holdMode,
        bool toggleMode,
        bool burstMode
    )
    {
        if (!klikers.TryGetValue(name, out ClickerConstruct? existing))
            return false;

        existing.ShouldStop = true;
        if (klikerThreads.TryGetValue(name, out Thread? oldThread))
        {
            oldThread.Join();
            _ = klikerThreads.Remove(name);
        }
        existing.ShouldStop = false;
        existing.WasButtonPressed = false;
        existing.UpdateActivationBind(activationBind);
        existing.UpdateActionBind(actionBind);
        existing.HoldDuration = holdDuration;
        existing.Delay = (ushort)(maxDelay > 0 && delay == 0 ? 1 : delay);
        existing.MaxDelay = maxDelay;
        existing.BurstCount = burstCount;
        existing.HoldMode = holdMode;
        existing.ToggleMode = toggleMode;
        existing.BurstMode = burstMode;
        existing.RecalculateCaches();
        Thread thread = new(existing.ThreadExecute)
        {
            IsBackground = false,
            Priority = ThreadPriority.Highest
        };
        klikerThreads[name] = thread;
        thread.Start();
        SendLogMessage($"Kliker profile '{name}' updated successfully.");
        return true;
    }

    internal static void DeleteKliker(string name)
    {
        if (!klikers.TryGetValue(name, out ClickerConstruct? clicker))
        {
            SendLogMessage($"Kliker profile '{name}' not found."); //NOOOOOOOOOOO
            return;
        }

        clicker.ShouldStop = true;
        if (klikerThreads.TryGetValue(name, out Thread? thread))
        {
            thread.Join();
            _ = klikerThreads.Remove(name);
        }
        _ = klikers.Remove(name);
        if (!Replacing && !Loading)
            SendLogMessage($"Kliker profile '{name}' deleted."); //sob sob
    }

    internal static ClickerConstruct? GetKliker(string name)
=> klikers.TryGetValue(name, out ClickerConstruct? value) ? value : null;

    internal static IEnumerable<ClickerConstruct> GetAllKlikers() => klikers.Values;
    internal static void ClearAll()
    {
        Loading = true;
        foreach (ClickerConstruct clicker in klikers.Values)
            clicker.ShouldStop = true;
        klikers.Clear();
        klikerThreads.Clear();
        Loading = false;
        SendLogMessage("Cleared all profiles."); //BAI BAI
    }
}
