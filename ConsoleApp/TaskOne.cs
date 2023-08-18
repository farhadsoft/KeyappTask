using AdvancedSharpAdbClient;

namespace ConsoleApp;

public static class TaskOne
{
    public static async Task TaskOneAsync(AdbClient client, DeviceData device)
    {
        var runningTasks = await GetAllRunningTasksAsync(client, device);

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
            return;
        }

        Console.WriteLine("**** DONE ****");
    }

    /// <summary>
    /// Get all running tasks
    /// </summary>
    /// <param name="client">AdbClient instance</param>
    /// <param name="device">Connected device</param>
    /// <returns></returns>
    public static async Task<List<string>> GetAllRunningTasksAsync(AdbClient client, DeviceData device)
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

    /// <summary>
    /// Kill one running task
    /// </summary>
    /// <param name="client">AdbClient instance</param>
    /// <param name="device">Connected device</param>
    /// <param name="appName">Application name</param>
    /// <returns></returns>
    public static async Task KillRunningTaskAsync(AdbClient client, DeviceData device, string appName)
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
}
