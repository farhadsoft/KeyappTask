using AdvancedSharpAdbClient;
using System.Xml;
using System.Xml.Linq;

namespace ConsoleApp;

public class TaskTwo
{
    public static async Task TaskTwoAsync(AdbClient client, DeviceData device)
    {
        var doc = await GetScreenParsedXmlAsync(client, device);

        XElement? element = doc.Descendants("node")
            .FirstOrDefault(e => e.Attribute("text")?.Value == "Chrome");

        if (element is not null)
        {
            Console.WriteLine("Clicking on Chrome element...");
            await ClickOnElementAsync(client, device, element);
        }
        else
        {
            Console.WriteLine("Chrome element not found");
            return;
        }

        doc = await GetScreenParsedXmlAsync(client, device);
        element = doc.Descendants("node")
            .FirstOrDefault(e => e.Attribute("text")?.Value == "Search or type URL" || e.Attribute("text")?.Value == "Search or type web address");

        if (element is null)
        {
            element = doc.Descendants("node")
                .FirstOrDefault(e => e.Attribute("text")?.Value == "Accept & continue");
            if (element is not null)
            {
                await SkipChromeWelcomeScreen(client, device, doc);
                doc = await GetScreenParsedXmlAsync(client, device);
                element = doc.Descendants("node")
                    .FirstOrDefault(e => e.Attribute("text")?.Value == "Search or type URL" || e.Attribute("text")?.Value == "Search or type web address");
            }
        }

        if (element is not null)
        {
            Console.WriteLine("Clicking on Chrome search element...");
            await ClickOnElementAsync(client, device, element);

            Console.WriteLine("Searching for 'my ip address'...");
            await SearchAndGettingResultAsync(client, device);
        }

        Console.WriteLine("**** DONE ****");
    }

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

    public static async Task ClickOnElementAsync(AdbClient client, DeviceData device, XElement element)
    {
        string bounds = element.Attribute("bounds")?.Value ?? string.Empty;
        var boundsArray = bounds.Split(new char[] { '[', ']', ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(int.Parse).ToArray();

        (int x, int y) = CenterOfCoordinate(boundsArray[0], boundsArray[1], boundsArray[2], boundsArray[3]);

        await client.ExecuteRemoteCommandAsync($"input tap {x} {y}", device, new ConsoleOutputReceiver());
    }

    public static (int, int) CenterOfCoordinate(int x1, int y1, int x2, int y2)
    {
        int centerX = (x1 + x2) / 2;
        int centerY = (y1 + y2) / 2;

        return (centerX, centerY);
    }

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
    /// <param name="client"></param>
    /// <param name="device"></param>
    /// <returns></returns>
    public static async Task<XDocument> GetScreenParsedXmlAsync(AdbClient client, DeviceData device)
    {
        var dumpReceiver = new ConsoleOutputReceiver();
        await client.ExecuteRemoteCommandAsync("uiautomator dump -D", device, dumpReceiver);

        using SyncService sync = new(client, device);
        using Stream stream = new MemoryStream();
        await sync.PullAsync("/sdcard/window_dump.xml", stream, null, CancellationToken.None);

        stream.Position = 0;
        using XmlReader reader = new XmlTextReader(stream);

        return XDocument.Load(reader);
    }
}
