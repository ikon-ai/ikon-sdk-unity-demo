# Ikon C# SDK Documentation

Welcome to the Ikon C# SDK documentation. This SDK is designed to help developers integrate and interact with Ikon's services easily using C#. The SDK provides a straightforward API to manage rooms, handle events, and communicate within the Ikon platform.

## Features

- Initialize client with developer credentials
- Connect to and manage multiple rooms
- Send and receive messages within rooms
- Event handling before and after connections, and on messages

## Installation

To start using the Ikon C# SDK, include it in your C# project. Ensure that your project is compatible with Net 8.0 and NET Standard 2.1 or later.

```bash
# You can clone the SDK from our repository (URL to repository)
git clone https://github.com/ikonlive/ikon-platform/tree/main/Ikon.SDK.DotNet

# Include the SDK in your project
```

## Usage

### Creating an Ikon Client

To create an Ikon client instance, you need a space ID, an API key, and a user ID. These credentials are necessary for initializing and authenticating your SDK instance.

```csharp
var spaceId = "your_space_id";
var apiKey = "your_api_key";
var userId = "your_user_id";

var ikonClient = await SDK.CreateIkonClientAsync(spaceId, apiKey, userId);
```

### Managing Rooms

Once the client is initialized, you can connect to rooms, send messages, and handle various room-related events.

```csharp
foreach (var (id, room) in ikonClient.Rooms)
{
    await room.ConnectAsync();
    Console.WriteLine($"Connected to room: {room.Name}, Id={id}");
}
```

### Sending Messages

To send messages to a room:

```csharp
var message = new ReadOnlyMessage("Hello, Ikon!");
room.SendMessage(message);
```

### Handling Events

Implement the `IIkonClientListener` to manage different room events.

```csharp
public class RoomEventHandler : IIkonClientListener
{
    public Task OnBeforeConnect(IRoom room) => Task.CompletedTask;
    public Task OnAfterConnect(IRoom room) => Task.CompletedTask;
    public Task OnAfterJoin(IRoom room) => Task.CompletedTask;
    public Task OnMessage(IRoom room, Message message) => Task.CompletedTask;
    public Task OnDisconnect(IRoom room) => Task.CompletedTask;
    public Task OnBeforeStop(IRoom room) => Task.CompletedTask;
}
```

Add this handler to your Ikon client:

```csharp
ikonClient.AddCallback(new RoomEventHandler());
```

## Contributing

Contributions to the Ikon SDK are welcome! Please read our contributing guidelines on our repository to learn how to contribute effectively.

## License

This SDK is licensed under the Ikon AI SDK License. See the LICENSE file in the repository for more details.

## Support

For support, please open an issue on our GitHub repository or contact our support team through our support channel. 
