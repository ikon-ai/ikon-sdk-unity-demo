
# Ikon AI C# SDK

Welcome to the Ikon AI C# SDK. This SDK is designed to help developers integrate and interact with Ikon's services easily using C#.
The SDK provides a straightforward API to manage rooms, handle events, and communicate within the Ikon platform.

## Features

- Initialize client with developer credentials
- Connect to and manage multiple rooms
- Send and receive messages within rooms
- Event handling before and after connections, and on messages

## Installation

To start using the Ikon AI C# SDK, include the libraries in your C# project. Ensure that your project is compatible with .NET Standard 2.1 or later.

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
    RequestTimeout = 5.0,
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

### Managing Rooms

Once the client is initialized, you can connect to a room, send messages, and handle various room-related events.

```csharp
var roomSlug = Environment.GetEnvironmentVariable("IKON_SDK_ROOM_SLUG") ?? "<<SET_ROOM_SLUG_HERE>>";
var room = new Room(ikonClient, roomSlug);
await room.ConnectAsync();
```

### Sending Messages

To send messages to a room:

```csharp
room.SendText("Hello, Ikon!");
```

### Handling Events

Implement the event handler to manage different room events, e.g. OnText, OnConnect, OnDisconnect, etc.

```csharp
private async Task Room_OnText(OnTextArgs e)
{
    await Task.CompletedTask;
    Console.WriteLine($"{e.UserName}: {e.Text}");
}

// Assign the event handler
room.OnText += Room_OnText;
```

### Helper Functions

```csharp
room.SetState("TestVariable", 1234); // Set any variable defined in the Input section
room.GenerateAnswer(); // Generate an answer without providing any input
room.ClearMessageHistory(); // Clear the message history of the whole room
room.EnableServerLogRendering = true; // Enable server log rendering (works only in dev environment)
```

### Example Program

For an up-to-date example program, see the included `Ikon.Sdk.DotNet.Examples.Chat/Program.cs` source file.

## License

This SDK is licensed under the Ikon AI SDK License. See the LICENSE file for more details.

## Support

For support, please open an issue on our GitHub repository or contact our support team through our support channel.
