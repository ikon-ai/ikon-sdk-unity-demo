# Ikon AI C# SDK

Welcome to the Ikon AI C# SDK – a fast, flexible way to talk to Ikon spaces from any .NET 8 / .NET Standard 2.1 project (including Unity).  
The SDK wraps the low-level protocol into an “easy-mode” `Channel` class, features automatic audio encoding/decoding, raw-message hooks, function calls, state management and much more.

---

## Features

- Create clients with rich connection, debugging & performance options  
- Automatic caching of channel instances (`Channel.Create`)  
- Text messaging with optional UI-chat generation & echo‐back  
- Audio streaming  
  - Opus encode / decode included  
  - Three streaming modes (`Streaming`, `DelayUntilTotalDurationKnown`, `DelayUntilIsLast`)  
  - Runtime **sample-rate override** & **streaming-mode override** on the receiver side  
  - Runtime **fade-out** of server-side audio (`FadeoutAudio`)  
- Register **custom functions** callable from the AI agent (0–4 parameters, any return type)  
- Real-time **speech-to-text** callbacks  
- Classification, usage, analytics & special-log callbacks (server log rendering supported)  
- Fine-grained **raw protocol access** – inspect or filter every incoming `ProtocolMessage` (`ReceiveMessage` event)  
- Send **custom protocol payloads** of your own (`SendMessage<T>`)  
- Manage LLM-shader state, trigger answer generation, clear chat history  
- Full control over opcode groups & payload format (`MemoryPack` or `MsgPack`)  

---

## Installation

Target **.NET 8.0** or **.NET Standard 2.1** and add the NuGet package:

```bash
dotnet add package Ikon.Sdk.DotNet
```

---

## Usage

### 1. Creating an Ikon client

```csharp
var ikonClientInfo = new Sdk.IkonClientInfo
{
    // --- Mandatory credentials ( NEVER hard-code the API key ) ---
    ApiKey         = Environment.GetEnvironmentVariable("IKON_SDK_API_KEY")
                     ?? throw new Exception("Set IKON_SDK_API_KEY."),
    SpaceId        = Environment.GetEnvironmentVariable("IKON_SDK_SPACE_ID") ?? "<<SPACE_ID>>",
    ExternalUserId = Environment.GetEnvironmentVariable("IKON_SDK_USER_ID") ?? "<<USER_ID>>",

    // --- Connection / infrastructure ---
    UseProductionEndpoint = (Environment.GetEnvironmentVariable("IKON_SDK_USE_PROD_ENDPOINT") ?? "true")
                            .Equals("true", StringComparison.OrdinalIgnoreCase),
    HasInput              = false,     // set ‘true’ if you plan to immediately send input after connect
    ReceiveAllMessages    = false,     // set ‘true’ to get every message, not only the ones addressed to you
    BackendRequestTimeout       = 10,
    BackendChannelConnectTimeout = 10,
    ServerChannelConnectTimeout  = 10,

    // --- Debug / performance ---
    EnableServerLogRendering = false,  // render server logs (dev environment only)
    LogAllBackendRequests    = false,
    PayloadType              = PayloadType.MemoryPack, // MemoryPack (default) or MsgPack
    OpcodeGroupsFromServer   = Opcode.GROUP_ALL,
    OpcodeGroupsToServer     = Opcode.GROUP_ALL,

    // --- Identification ---
    Description = "Ikon AI C# SDK Example",
    DeviceId    = Utils.GenerateDeviceId(),
    ProductId   = "Ikon.Sdk.DotNet.Example",
    Version     = 1,
    UserType    = UserType.Human,
    ClientType  = ClientType.Unknown,
    Locale      = "en-us",
    UserAgent   = "ikon-sdk-example/1.0.0",
};

await using IIkonClient ikonClient = await Sdk.CreateIkonClientAsync(ikonClientInfo);
```

> Internal testing helpers  
> • `UseLocalIkonServer`, `LocalIkonServerHost`, `LocalIkonServerPort`, `LocalIkonServerUserId` allow you to redirect a client to a locally running Ikon server build.

---

### 2. Managing channels

```csharp
string channelKey = Environment.GetEnvironmentVariable("IKON_SDK_CHANNEL_KEY") ?? "<<CHANNEL_KEY>>";

// Channel.Create automatically caches (ikonClient, channelKey)
Channel channel = Channel.Create(ikonClient, channelKey);

// -----------------------------------------------------------------
// Subscribe to the events *you* care about
// -----------------------------------------------------------------
channel.Connected            += OnConnected;
channel.Stopping             += OnStopping;
channel.Disconnected         += OnDisconnected;
channel.ReceiveMessage       += OnRawMessage;             // NEW: raw protocol hook
channel.Text                 += OnText;
channel.SpeechRecognized     += OnSpeech;
channel.ClassificationResult += OnClassificationResult;
channel.SpecialLog           += OnSpecialLog;
channel.Usage                += OnUsage;
channel.AudioStreamBegin     += OnAudioStreamBegin;
channel.AudioFrame           += OnAudioFrame;
channel.AudioStreamEnd       += OnAudioStreamEnd;

// Connect & wait for the space
await channel.ConnectAsync();
channel.SignalReady();                 // tell other clients you are ready
await channel.WaitForAIAgentAsync();   // wait for the AI agent to join
```

---

### 3. Text messaging

```csharp
// Send a message – let the server build chat-history entries (UI) and also echo it back
channel.SendText("Hello, Ikon!", generateChatMessage: true, sendBackToSender: true);

private async Task OnText(object s, Channel.TextArgs e)
{
    await Task.CompletedTask;
    Console.WriteLine($"{e.UserName}: {e.Text}");
}
```

---

### 4. Audio streaming

#### Sending audio

```csharp
ReadOnlyMemory<float> samples = GetMicrophoneBuffer(); // any length
int sampleRate = 48_000;
int channels   = 1;

// isFirst / isLast allow either streaming or one-shot clips
channel.SendAudio(samples, sampleRate, channels,
                  isFirst: true, isLast: true, id: "mic_stream");

// Ask the server to gracefully fade-out any currently playing streams
channel.FadeoutAudio(1.5f);   // seconds
```

#### Receiving audio

```csharp
private async Task OnAudioStreamBegin(object s, Channel.AudioStreamBeginArgs e)
{
    // --- Optional runtime overrides ---
    // e.SampleRate    = 44_100;                                // resample
    // e.StreamingMode = AudioStreamingMode.DelayUntilIsLast;   // buffer & play later
    await Task.CompletedTask;
}

private async Task OnAudioFrame(object s, Channel.AudioFrameArgs e)
{
    await Task.CompletedTask;
    // e.Samples             : decoded pcm
    // e.IsFirst / e.IsLast
    // e.DurationSinceIsFirst
    // e.TotalDuration       : 0 until known (depends on streaming mode)
}

private async Task OnAudioStreamEnd(object s, Channel.AudioStreamEndArgs e)
{
    await Task.CompletedTask;
}
```

`AudioStreamingMode` cheat-sheet:

| Mode | Behaviour |
|------|-----------|
| `Streaming` | Forward frames immediately (lowest latency) |
| `DelayUntilTotalDurationKnown` | Buffer until the server sends the total length, then play with original timing |
| `DelayUntilIsLast` | Buffer everything, play in one go when the last frame arrives |

---

### 5. Real-time speech recognition

```csharp
private async Task OnSpeech(object s, Channel.SpeechRecognizedArgs e)
{
    await Task.CompletedTask;
    Console.WriteLine($"Speech recognized: success={e.WasSuccessful}, text='{e.Text}'");
}
```

---

### 6. Registering functions callable by the AI agent

```csharp
// Up to 4 parameters – type order: <T1,[T2,T3,T4], TResult>
channel.RegisterFunction<int, string, string>("example_function", ExampleFunction);

private async Task<string> ExampleFunction(int number, string text)
{
    await Task.CompletedTask;
    return $"Echo: {number} / {text}";
}
```

The SDK serialises/validates parameters, invokes your callback and returns the JSON result to the agent automatically.

---

### 7. State & history management

```csharp
// LLM-shader state variables (only keys defined in the shader’s Input section)
channel.SetState("Health", 42);
channel.ClearState();

// Trigger answer generation even when you have no new input
channel.GenerateAnswer();

// Remove the complete chat history for this channel
channel.ClearChatMessageHistory();
```

---

### 8. Low-level protocol access

Need full control? Hook `ReceiveMessage` or send custom payloads:

```csharp
private async Task OnRawMessage(object s, MessageEventArgs e)
{
    await Task.CompletedTask;
    Console.WriteLine($"[RAW] {e.Message.Opcode} len={e.Message.DataLength}");
}

// Send your own Ikon.Common.Core.Protocol payload
channel.SendMessage(new CustomPayload { /* … */ });
```

---

### 9. Handling other channel events

```csharp
private async Task OnConnected(object s, Channel.ConnectedArgs e)
{
    await Task.CompletedTask;
    Console.WriteLine("Channel connected");
}

private async Task OnStopping(object s, Channel.StoppingArgs e)
{
    await Task.CompletedTask;
    Console.WriteLine("Channel stopping – still allowed to send msgs");
}

private async Task OnDisconnected(object s, Channel.DisconnectedArgs e)
{
    await Task.CompletedTask;
    Console.WriteLine("Channel disconnected");
}

private async Task OnClassificationResult(object s, Channel.ClassificationResultArgs e)
{
    await Task.CompletedTask;
    Console.WriteLine($"Classification result: {e.Result}");
}

private async Task OnSpecialLog(object s, Channel.SpecialLogArgs e)
{
    await Task.CompletedTask;
    Console.WriteLine($"[SpecialLog] {e.Title}\n{e.Message}");
}

private async Task OnUsage(object s, Channel.UsageArgs e)
{
    await Task.CompletedTask;
    Console.WriteLine($"Usage '{e.UsageName}': {e.Usage:F2}");
}
```

---

### 10. Example program

A complete command-line chat sample is included at  
`examples/Ikon.Sdk.DotNet.Examples.Chat/Program.cs`.

It showcases:

- Client creation & configuration  
- Connecting, signalling readiness, waiting for the AI agent  
- Text & audio send / receive, fade-out  
- Speech-recognition callbacks  
- Function registration  
- State management & chat-history control  
- Raw message inspection  
- Graceful shutdown  

Run it:

```bash
dotnet run --project examples/Ikon.Sdk.DotNet.Examples.Chat
```

(Ensure `IKON_SDK_API_KEY`, `IKON_SDK_SPACE_ID`, `IKON_SDK_CHANNEL_KEY` and `IKON_SDK_USER_ID` are set.)

---

## License

This SDK is licensed under the Ikon AI SDK License – see `LICENSE` for details.

## Support

Open an issue on GitHub or contact Ikon support.