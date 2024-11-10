# Ikon AI C# SDK

Welcome to the Ikon AI C# SDK. This SDK is designed to help developers integrate and interact with Ikon's services easily using C#.
The SDK provides a straightforward API to manage channels, handle events, communicate, and interact with the AI agent within the Ikon platform.

## Features

- Initialize clients with developer credentials
- Connect to and manage multiple channels
- Send and receive text messages within channels
- Send and receive audio streams
- Register and handle custom functions callable from the AI agent
- Manage state variables and message history
- Event handling for various channel events, including connection, disconnection, and message events

## Installation

To start using the Ikon AI C# SDK, include the libraries in your C# project. Ensure that your project is compatible with .NET 8.0 or .NET Standard 2.1.

## Usage

### Creating an Ikon Client

To create an Ikon client instance, you need an API key, a space ID, and a user ID.

```csharp
var ikonClientInfo = new Sdk.IkonClientInfo
{
    // Get the API key from the Ikon Portal and then supply it with e.g. environment variable. Do not hardcode it.
    ApiKey = Environment.GetEnvironmentVariable("IKON_SDK_API_KEY") ?? throw new Exception("API key is missing. Please set the 'IKON_SDK_API_KEY' environment variable."),

    // Get the space ID from Ikon Portal. This can be hardcoded.
    SpaceId = Environment.GetEnvironmentVariable("IKON_SDK_SPACE_ID") ?? "<<SET_SPACE_ID_HERE>>",

    // Set a unique ID for the player. This can be the player's ID in your game. This can be hardcoded.
    UserId = Environment.GetEnvironmentVariable("IKON_SDK_USER_ID") ?? "<<SET_USER_ID_HERE>>",

    // Use the production endpoint by default. Set to false to use the development endpoint.
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

// Create an Ikon client
var ikonClient = await Sdk.CreateIkonClientAsync(ikonClientInfo);
```

### Managing Channels

Once the client is initialized, you can connect to a channel, send messages, and handle various channel-related events.

```csharp
// Set the channel key to use
var channelKey = Environment.GetEnvironmentVariable("IKON_SDK_CHANNEL_KEY") ?? "<<SET_CHANNEL_KEY_HERE>>";

// Create or get a channel instance
var channel = Channel.Create(ikonClient, channelKey);

// Subscribe to channel events
channel.Connected += OnChannelConnected;
channel.Stopping += OnChannelStopping;
channel.Disconnected += OnChannelDisconnected;
// Subscribe to other events as needed...

// Connect to the channel
await channel.ConnectAsync();

// Signal readiness to other clients in the channel
channel.SignalReady();

// Wait for the AI agent to become ready
await channel.WaitForAIAgentAsync();
```

### Sending and Receiving Text Messages

To send text messages to a channel and receive messages:

```csharp
// Sending a text message
channel.SendText("Hello, Ikon!", generateChatMessage: true, sendBackToSender: true);

// Handling received text messages
channel.Text += OnChannelText;

private async Task OnChannelText(object sender, Channel.TextArgs e)
{
    await Task.CompletedTask;
    Console.WriteLine($"\n{e.UserName}: {e.Text}\n");
}
```

### Sending and Receiving Audio Streams

You can send and receive audio streams in real-time.

#### Sending Audio

To send audio to the channel:

```csharp
// Prepare your audio samples as a float array
float[] samples = ...; // Your audio samples
int sampleRate = 48000; // Sample rate of your audio
int channels = 1; // Number of audio channels

// Send the audio samples
channel.SendAudio(samples, sampleRate, channels, isFirst: true, isLast: true);

// Optionally, fade out any ongoing audio streams
channel.FadeoutAudio(2.0f); // Fade out over 2 seconds
```

#### Receiving Audio

Handle audio stream events to receive audio from the channel:

```csharp
channel.AudioStreamBegin += OnChannelAudioStreamBegin;
channel.AudioFrame += OnChannelAudioFrame;
channel.AudioStreamEnd += OnChannelAudioStreamEnd;

private async Task OnChannelAudioStreamBegin(object sender, Channel.AudioStreamBeginArgs e)
{
    await Task.CompletedTask;
    Console.WriteLine($"Audio Stream Begin: StreamId={e.StreamId}, SampleRate={e.SampleRate}, Channels={e.Channels}");
}

private async Task OnChannelAudioFrame(object sender, Channel.AudioFrameArgs e)
{
    await Task.CompletedTask;
    // Process the received audio samples
    var samples = e.Samples;
    // ...
}

private async Task OnChannelAudioStreamEnd(object sender, Channel.AudioStreamEndArgs e)
{
    await Task.CompletedTask;
    Console.WriteLine($"Audio Stream End: StreamId={e.StreamId}");
}
```

### Registering Functions for the AI Agent

You can register functions in your code that the AI agent can call during conversation. This allows for dynamic interactions where the AI agent can execute code in your application.

#### Register a Function

```csharp
channel.RegisterFunction<int, string, string>("example_function", ExampleFunction);
```

#### Define the Function

```csharp
private async Task<string> ExampleFunction(int argument1, string argument2)
{
    await Task.CompletedTask;
    // Implement your logic here
    return $"Function result: {argument1}, {argument2}";
}
```

### Managing State and Message History

You can set and manage state variables, control message history, and control the AI agent's response generation.

```csharp
// Set a state variable
channel.SetState("ExampleVariable", 123);

// Clear all state variables
channel.ClearState();

// Instruct the AI agent to generate an answer without providing new input
channel.GenerateAnswer();

// Clear the message history of the whole channel
channel.ClearMessageHistory();
```

### Handling Channel Events

Implement event handlers to manage different channel events, such as connection status, messages, and custom events.

```csharp
// Subscribe to events
channel.Connected += OnChannelConnected;
channel.Stopping += OnChannelStopping;
channel.Disconnected += OnChannelDisconnected;
channel.Text += OnChannelText;
channel.ClassificationResult += OnChannelClassificationResult;
channel.SpecialLog += OnChannelSpecialLog;
channel.Usage += OnChannelUsage;
channel.AudioStreamBegin += OnChannelAudioStreamBegin;
channel.AudioFrame += OnChannelAudioFrame;
channel.AudioStreamEnd += OnChannelAudioStreamEnd;

// Event handler examples
private async Task OnChannelConnected(object sender, Channel.ConnectedArgs e)
{
    await Task.CompletedTask;
    Console.WriteLine("Connected to the channel");
}

private async Task OnChannelStopping(object sender, Channel.StoppingArgs e)
{
    await Task.CompletedTask;
    Console.WriteLine("Channel is stopping");
}

private async Task OnChannelDisconnected(object sender, Channel.DisconnectedArgs e)
{
    await Task.CompletedTask;
    Console.WriteLine("Disconnected from the channel");
}

private async Task OnChannelClassificationResult(object sender, Channel.ClassificationResultArgs e)
{
    await Task.CompletedTask;
    Console.WriteLine($"Classification Result: {e.Result}");
}

private async Task OnChannelSpecialLog(object sender, Channel.SpecialLogArgs e)
{
    await Task.CompletedTask;
    Console.WriteLine($"Special Log - {e.Title}:\n{e.Message}");
}

private async Task OnChannelUsage(object sender, Channel.UsageArgs e)
{
    await Task.CompletedTask;
    Console.WriteLine($"Usage - {e.UsageName}: {e.Usage}");
}
```

### Helper Functions

The SDK provides several helper functions to manage state and control the AI agent's behavior:

```csharp
channel.SetState("TestVariable", 1234); // Set a variable defined in the Input section
channel.ClearState(); // Clear all variables from the state
channel.GenerateAnswer(); // Generate an answer without providing any input
channel.ClearMessageHistory(); // Clear the message history of the whole channel
channel.FadeoutAudio(2.0f); // Send a signal to fade out audio streams over 2 seconds
```

### Example Program

An up-to-date example program is included in the `Ikon.Sdk.DotNet.Examples.Chat/Program.cs` source file. This example demonstrates how to:

- Initialize the Ikon client
- Connect to a channel
- Send and receive text messages
- Handle various events
- Register custom functions callable by the AI agent
- Manage state variables and message history
- Send and receive audio streams

## License

This SDK is licensed under the Ikon AI SDK License. See the LICENSE file for more details.

## Support

For support, please open an issue on our GitHub repository or contact our support team through our support channel.