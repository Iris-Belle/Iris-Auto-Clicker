namespace IAC4.Core;

internal class UpdateChecker
{
    internal static async Task<bool?> CheckForUpdates()
    {
        string currentVersionRaw = string.Empty;
        Application app = Application.Current;
        if (app?.MainWindow != null)
        {
            app.Dispatcher.Invoke(() =>
            {
                Window mw = app.MainWindow;
                object label = mw.FindName("VersionLabel");
                if (label is Label l)
                    currentVersionRaw = l.Content?.ToString() ?? string.Empty;
            });
        }
        Version? currentVersion = ParseVersionFromLabel(currentVersionRaw);
        if (currentVersion == null)
            return null;

        try
        {
            using HttpClient http = new();
            http.DefaultRequestHeaders.UserAgent.ParseAdd("request");
            http.Timeout = TimeSpan.FromSeconds(10);

            string url = "https://api.github.com/repos/Iris-Belle/Iris-Auto-Clicker/releases";
            string json = await http.GetStringAsync(url);
            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement firstRelease = doc.RootElement[0];
            string? tag = firstRelease.GetProperty("tag_name").GetString();
            if (tag?.StartsWith("v", StringComparison.OrdinalIgnoreCase) == true)
                tag = tag[1..];
            return Version.TryParse(tag, out Version? latest) && latest > currentVersion;
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (TaskCanceledException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch
        {
            return null;
        }
    }

    private static Version? ParseVersionFromLabel(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;
        Match m = Regex.Match(raw, @"\d+(\.\d+)+");
        if (m.Success && Version.TryParse(m.Value, out Version? v))
            return v;
        raw = raw.Trim();
        if (raw.StartsWith("IAC4 ", StringComparison.OrdinalIgnoreCase))
            raw = raw[5..].Trim();
        if (raw.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            raw = raw[1..];
        return Version.TryParse(raw, out Version? v2) ? v2 : null;
    }
}