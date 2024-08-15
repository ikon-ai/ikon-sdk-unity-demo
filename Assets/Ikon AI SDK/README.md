# Ikon AI C# SDK

Welcome to the Ikon AI C# SDK. This SDK is designed to help developers integrate and interact with Ikon's services easily using C#.
The SDK provides a straightforward API to manage channels, handle events, and communicate within the Ikon platform.

## Features

- Initialize client with developer credentials
- Connect to and manage multiple channels
- Send and receive messages within channels
- Event handling before and after connections, and on messages

## Installation

To start using the Ikon AI C# SDK, include the libraries in your C# project. Ensure that your project is compatible with .NET 8.0 or .NET Standard 2.1.

## Usage

### Creating an Ikon Client

To create an Ikon client instance, you need an API key, a space ID, and a user ID.

```csharp
var clientInfo = new Sdk.ClientInfo
{
    ApiKey = Environment.GetEnvironmentVariable("IKON_SDK_API_KEY") ?? throw new Exception("API key is missing. Please set the 'IKON_SDK_API_KEY' environment variable."),
    SpaceId = Environment.GetEnvironmentVariable("IKON_SDK_SPACE_ID") ?? "<<SET_SPACE_ID_HERE>>",
    UserId = Environment.GetEnvironmentVariable("IKON_SDK_USER_ID") ?? "<<SET_USER_ID_HERE>>",
    UseProductionEndpoint = Environment.GetEnvironmentVariable("IKON_SDK_USE_PROD_ENDPOINT")?.Trim().Equals("true", StringComparison.InvariantCultureIgnoreCase) ?? true,
    Description = "Example",
    DeviceId = Utils.GenerateDeviceId(),
    ProductId = "Ikon.Sdk.DotNet.Example",
    VersionId = Version.VersionString,
    InstallId = "1",
    UserType = UserType.Human,
    OpcodeGroupsFromServer = Opcode.GROUP_ALL,
    OpcodeGroupsToServer = Opcode.GROUP_ALL,
};

var ikonClient = await Sdk.CreateIkonClientAsync(clientInfo);
```

### Managing Channels

Once the client is initialized, you can connect to a channel, send messages, and handle various channel-related events.

```csharp
var channelKey = Environment.GetEnvironmentVariable("IKON_SDK_CHANNEL_KEY") ?? "<<SET_CHANNEL_KEY_HERE>>";
var channel = new Channel(ikonClient, channelKey);
await channel.ConnectAsync();
```

### Sending Messages

To send messages to a channel:

```csharp
channel.SendText("Hello, Ikon!");
```

### Handling Events

Implement the event handlers to manage different channel events, e.g. Start, Shutdown, Text, and more.

```csharp
private async Task OnChannelText(object sender, Channel.TextArgs e)
{
    await Task.CompletedTask;
    Console.WriteLine($"\n{e.UserName}: {e.Text}\n");
}

// Assign the event handler
channel.Text += OnChannelText;
```

### Helper Functions

```csharp
channel.SetState("TestVariable", 1234); // Set any variable defined in the Input section
channel.ClearState(); // Clear all variables from the state
channel.GenerateAnswer(); // Generate an answer without providing any input
channel.ClearMessageHistory(); // Clear the message history of the whole channel
```

### Example Program

For an up-to-date example program, see the included `Ikon.Sdk.DotNet.Examples.Chat/Program.cs` source file.

## License

This SDK is licensed under the Ikon AI SDK License. See the LICENSE file for more details.

## Support

For support, please open an issue on our GitHub repository or contact our support team through our support channel.
