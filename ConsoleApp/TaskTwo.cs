using AdvancedSharpAdbClient;
using System.Xml;
using System.Xml.Linq;

namespace ConsoleApp;

public class TaskTwo
{
    public static async Task TaskTwoAsync(AdbClient client, DeviceData device)
    {
        // Open Chrome
        var doc = await GetScreenParsedXmlAsync(client, device);

        var chromeElement = doc.Descendants("node")
            .FirstOrDefault(e => e.Attribute("text")?.Value == "Chrome");

        if (chromeElement is null)
        {
            Console.WriteLine("Chrome element not found");
            return;
        }

        // Click on Chrome
        Console.WriteLine("Clicking on Chrome element...");
        await ClickOnElementAsync(client, device, chromeElement);

        // Try to find the search element or skip the welcome screen
        doc = await GetScreenParsedXmlAsync(client, device);
        
        var searchElement = doc.Descendants("node")
            .FirstOrDefault(e => e.Attribute("text")?
            .Value == "Search or type URL" || e.Attribute("text")?
            .Value == "Search or type web address");

        if (searchElement is null)
        {
            Console.WriteLine("Skipping Chrome welcome screen...");
            await SkipChromeWelcomeScreen(client, device, doc);

            doc = await GetScreenParsedXmlAsync(client, device);

            searchElement = doc.Descendants("node")
                .FirstOrDefault(e => e.Attribute("text")?
                .Value == "Search or type URL" || e.Attribute("text")?
                .Value == "Search or type web address");
        }

        // Finally, search for "my ip address" and get the result
        if (searchElement is not null)
        {
            Console.WriteLine("Clicking on Chrome search element...");
            await ClickOnElementAsync(client, device, searchElement);

            Console.WriteLine("Searching for 'my ip address'...");
            await SearchAndGettingResultAsync(client, device);
        }

        Console.WriteLine("**** Task Two is DONE ****");
    }

    /// <summary>
    /// Search for "my ip address" and get the result
    /// </summary>
    /// <returns></returns>
    public static async Task SearchAndGettingResultAsync(AdbClient client, DeviceData device)
    {
        string searchQuery = "my ip address";
        await client.ExecuteRemoteCommandAsync($"input text ' {searchQuery}'", device, new ConsoleOutputReceiver());
        await client.ExecuteRemoteCommandAsync("input keyevent 66", device, new ConsoleOutputReceiver());

        var elements = client.FindElements(device, "//node[starts-with(@resource-id, 'sa-tpcc')]", TimeSpan.FromSeconds(5));
        elements = elements.Last().Children.First().Children.Take(2);

        foreach (var element in elements)
        {
            Console.WriteLine(element.Attributes.Where(x => x.Key.Equals("text")).First().Value);
        }
    }

    /// <summary>
    /// Click on the element
    /// </summary>
    /// <returns></returns>
    public static async Task ClickOnElementAsync(AdbClient client, DeviceData device, XElement element)
    {
        string bounds = element.Attribute("bounds")?.Value ?? string.Empty;
        var boundsArray = bounds.Split(new char[] { '[', ']', ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(int.Parse).ToArray();

        (int x, int y) = CenterOfCoordinate(boundsArray[0], boundsArray[1], boundsArray[2], boundsArray[3]);

        await client.ExecuteRemoteCommandAsync($"input tap {x} {y}", device, new ConsoleOutputReceiver());
    }

    /// <summary>
    /// Get the center of the coordinate for click
    /// </summary>
    /// <returns></returns>
    public static (int, int) CenterOfCoordinate(int x1, int y1, int x2, int y2)
    {
        int centerX = (x1 + x2) / 2;
        int centerY = (y1 + y2) / 2;

        return (centerX, centerY);
    }

    /// <summary>
    /// Skip the welcome screen on Chrome
    /// </summary>
    /// <returns></returns>
    public static async Task SkipChromeWelcomeScreen(AdbClient client, DeviceData device, XDocument doc)
    {
        var _element = doc.Descendants("node")
            .FirstOrDefault(e => e.Attribute("text")?.Value == "Accept & continue");

        if (_element is not null)
        {
            await ClickOnElementAsync(client, device, _element);
        }

        doc = await GetScreenParsedXmlAsync(client, device);
        _element = doc.Descendants("node")
            .FirstOrDefault(e => e.Attribute("text")?.Value == "No thanks");

        while (_element is not null)
        {
            await ClickOnElementAsync(client, device, _element);
            await Task.Delay(TimeSpan.FromSeconds(5));

            doc = await GetScreenParsedXmlAsync(client, device);
            _element = doc.Descendants("node")
                .FirstOrDefault(e => e.Attribute("text")?.Value == "No thanks");
        }
    }

    /// <summary>
    /// Parse the XML content
    /// </summary>
    /// <returns></returns>
    public static async Task<XDocument> GetScreenParsedXmlAsync(AdbClient client, DeviceData device)
    {
        var dumpReceiver = new ConsoleOutputReceiver();
        await client.ExecuteRemoteCommandAsync("uiautomator dump", device, dumpReceiver);

        using SyncService sync = new(client, device);
        using Stream stream = new MemoryStream();
        await sync.PullAsync("/sdcard/window_dump.xml", stream, null, CancellationToken.None);

        stream.Position = 0;
        using XmlReader reader = new XmlTextReader(stream);

        return XDocument.Load(reader);
    }
}
