using AdvancedSharpAdbClient;
using ConsoleApp;

AdbClient client = new(); // Create a new AdbClient instance
client.Connect("127.0.0.1:62001"); // Connect to the device
DeviceData? device = client.GetDevices().FirstOrDefault(); // Get the connected device

Console.WriteLine("Device status: " + device?.State); // Print the device status

if (device is not null)
{
    await TaskOne.TaskOneAsync(client, device);
    await TaskTwo.TaskTwoAsync(client, device);
}

Console.WriteLine("Press any key to exit..."); // Wait for user input
Console.ReadKey();