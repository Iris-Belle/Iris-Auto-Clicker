namespace IAC4.Core;

internal static class GoogleDoc
{
    private static readonly char[] Separators = ['\r', '\n'];

    internal static async Task GetTitleAsync()
    {
        const string rawUrl = "https://docs.google.com/document/d/1wtMBiTUCG-tkKZLOrKfAzlp2NkPbEyrz6Jtmb8w-Hhg/export?format=txt";

        string[] titles = await FetchTitlesAsync(rawUrl);

        string title = titles.Length > 0
            ? titles[new Random().Next(titles.Length)]
            : "IAC4 (Failed to fetch google doc)"; //bleh

        Application.Current.Dispatcher.Invoke(() =>
        {
            Application.Current.MainWindow.Title = title;
        });
    }

    private static async Task<string[]> FetchTitlesAsync(string fileUrl)
    {
        try
        {
            fileUrl = fileUrl.Trim();

            using HttpClient client = new();
            string content = await client.GetStringAsync(fileUrl);

            string[] lines = [.. content.Split(Separators, StringSplitOptions.RemoveEmptyEntries).Select(line => line.Trim()).Where(line => line.Length > 0).Skip(1)]; //looooonngggggg line

            return lines.Length > 0 ? lines : ["IAC4 (There arent any titles in the doc :c)"];
        }
        catch (Exception ex)
        {
            SendLogMessage($"Error fetching Google Doc titles: {ex}");
            return ["IAC4 (Check Log. Failed to fetch google doc)"];
        }
    }
}
