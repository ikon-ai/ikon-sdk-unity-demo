using Ikon.Common.Core;
using Ikon.Common.Core.Protocol;
using Ikon.Sdk.DotNet;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class SceneHandler : MonoBehaviour
{
    public TMP_InputField ChatInputField;
    public TMP_Text ChatOutputText;

    private IIkonClient _ikonClient;
    private Room _room;
    private readonly ConcurrentQueue<string> _outputMessages = new();

    public async void Start()
    {
        Log.Instance.AddLogHandler(OnLogEvent);
        Log.Instance.Info($"Ikon AI C# SDK, version: {Ikon.Sdk.DotNet.Version.VersionString}");

        var clientInfo = new Sdk.ClientInfo
        {
            // Get the API key from the Ikon Portal and then supply it with e.g. environment variable. Do not hardcode it.
            ApiKey = Environment.GetEnvironmentVariable("IKON_SDK_API_KEY") ??
                     throw new Exception("API key is missing. Please set the 'IKON_SDK_API_KEY' environment variable."),

            // Get the space ID from Ikon Portal. This can be hardcoded.
            SpaceId = Environment.GetEnvironmentVariable("IKON_SDK_SPACE_ID") ?? "<<SET_SPACE_ID_HERE>>",

            // Set a unique ID for the player. This can be the player's ID in your game. This can be hardcoded.
            UserId = Environment.GetEnvironmentVariable("IKON_SDK_USER_ID") ?? "<<SET_USER_ID_HERE>>",

            // Use the production endpoint by default. Set to false to use the development endpoint.
            UseProductionEndpoint = Environment.GetEnvironmentVariable("IKON_SDK_USE_PROD_ENDPOINT")?.Trim()
                .Equals("true", StringComparison.InvariantCultureIgnoreCase) ?? true,

            RequestTimeout = 5.0,

            Description = "Ikon AI SDK Unity Example",
            DeviceId = Utils.GenerateDeviceId(),
            ProductId = "Ikon.Sdk.DotNet.Examples.Unity",
            VersionId = "1",
            InstallId = "1",
            UserType = UserType.Human,
            OpcodeGroupsFromServer = Opcode.GROUP_ALL,
            OpcodeGroupsToServer = Opcode.GROUP_ALL,
        };

        // Set the room slug to use
        var roomSlug = Environment.GetEnvironmentVariable("IKON_SDK_ROOM_SLUG") ?? "<<SET_ROOM_SLUG_HERE>>";

        _ikonClient = await Sdk.CreateIkonClientAsync(clientInfo);
        _room = new Room(_ikonClient, roomSlug);
        _room.Text += RoomOnText;
        //_room.EnableServerLogRendering = true;
        await _room.ConnectAsync();
    }

    public async void OnApplicationQuit()
    {
        if (_ikonClient != null)
        {
            await _ikonClient.DisposeAsync();
            _ikonClient = null;
        }
    }

    private Task RoomOnText(object sender, Room.TextArgs e)
    {
        _outputMessages.Enqueue($"{e.UserName}: {e.Text}");
        return Task.CompletedTask;
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            _outputMessages.Enqueue($"User: {ChatInputField.text}");

            _room.SendText(ChatInputField.text);

            ChatInputField.text = string.Empty;
            ChatInputField.ActivateInputField();
        }

        while (_outputMessages.TryDequeue(out string outputMessage))
        {
            ChatOutputText.text += outputMessage + "\n\n";
        }
    }

    private void OnLogEvent(LogEvent logEvent)
    {
        switch (logEvent.Type)
        {
            case Ikon.Common.Core.Protocol.LogType.Warning:
            {
                Debug.LogWarning($"{logEvent.Type}: {logEvent.Message}");
                break;
            }

            case Ikon.Common.Core.Protocol.LogType.Error:
            case Ikon.Common.Core.Protocol.LogType.Critical:
            {
                Debug.LogError($"{logEvent.Type}: {logEvent.Message}");
                break;
            }

            default:
                Debug.Log($"{logEvent.Type}: {logEvent.Message}");
                break;
        }
    }
}