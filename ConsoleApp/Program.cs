using AdvancedSharpAdbClient;

AdbClient client = new();

// Connect to the device
client.Connect("127.0.0.1:62001");

// Get the connected device
DeviceData? device = client.GetDevices().FirstOrDefault();

// Print the device status
Console.WriteLine("Device status: " + device?.State);

// Get all running tasks
var runningTasks = await GetAllRunningTasksAsync(client, device);

// Kill all the running tasks
if (runningTasks.Count > 0)
{
    foreach (var task in runningTasks)
    {
        await KillRunningTaskAsync(client, device, task);
    }
}
else
{
    Console.WriteLine("No running tasks found");
    Console.ReadKey();
    return;
}

Console.WriteLine("Press any key to exit...");
Console.ReadKey();

// Get all running tasks
static async Task<List<string>> GetAllRunningTasksAsync(AdbClient client, DeviceData? device)
{
    List<string> runningTasks = new();
    Console.WriteLine("Getting all running tasks...");

    try
    {
        ConsoleOutputReceiver receiver = new();
        await client.ExecuteRemoteCommandAsync("dumpsys activity activities", device, receiver);
        string output = receiver.ToString();

        string[] lines = output.Split('\n')
                             .Where(x => x.Contains("(fullscreen)"))
                             .Where(x => x.Contains("com.").Equals(true))
                             .ToArray();

        for (int i = 0; i < lines.Length; i++)
        {
            int index = lines[i].IndexOf(':');
            lines[i] = lines[i][(index + 1)..];
            index = lines[i].IndexOf('}');
            lines[i] = lines[i][..index];
        }

        runningTasks = lines.ToList();
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
    }

    Console.WriteLine("Completed getting all running tasks");
    return runningTasks;
}

// Kill one running task
static async Task KillRunningTaskAsync(AdbClient client, DeviceData? device, string appName)
{
    Console.WriteLine($"Killing {appName}...");

    try
    {
        await client.ExecuteRemoteCommandAsync($"am force-stop {appName}", device, null);
        await client.ExecuteRemoteCommandAsync($"pm clear {appName}", device, null);
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
    }

    Console.WriteLine($"Completed killing {appName}");
    await Task.CompletedTask;
}