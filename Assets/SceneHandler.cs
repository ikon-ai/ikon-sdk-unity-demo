using Ikon.Common;
using Ikon.Common.Protocol;
using Ikon.Common.ReactiveUI;
using Ikon.Sdk.DotNet;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class SceneHandler : MonoBehaviour, IIkonClientListener
{
    public TMP_InputField ChatInputField;
    public TMP_Text ChatOutputText;

    private IIkonClient _ikonClient;
    private UIController _uiController;
    private readonly ConcurrentQueue<string> _outputMessages = new();

    public async void Start()
    {
        Log.Instance.AddLogHandler(OnLogEvent);
        Log.Instance.Info($"Ikon .NET SDK, version: {Ikon.Sdk.DotNet.Version.VersionString}");

        // Get the API key from the Ikon Portal and then supply it with e.g. environment variable. Do not hardcode it.
        var apiKey = Environment.GetEnvironmentVariable("IKON_SDK_API_KEY");

        // Get the space ID from Ikon Portal. This can be hardcoded.
        var spaceId = Environment.GetEnvironmentVariable("IKON_SDK_SPACE_ID") ?? "<<SET_SPACE_ID_HERE>>";

        // Use unique ID for the player. This can be the player's ID in your game. This can be hardcoded.
        var userId = Environment.GetEnvironmentVariable("IKON_SDK_USER_ID") ?? "<<SET_USER_ID_HERE>>";

        // Set the room name to use. This can be hardcoded.
        var roomName = Environment.GetEnvironmentVariable("IKON_SDK_ROOM_NAME") ?? "<<SET_ROOM_NAME_HERE>>";

        // Use the production endpoint by default. Set to false to use the internal dev endpoint.
        var useProductionEndpointString = Environment.GetEnvironmentVariable("IKON_SDK_USE_PROD_ENDPOINT") ?? "true";
        bool useProductionEndpoint = useProductionEndpointString.Trim().Equals("true", StringComparison.InvariantCultureIgnoreCase);

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new Exception("API key is missing. Please set the 'IKON_SDK_API_KEY' environment variable.");
        }

        _ikonClient = await Sdk.CreateIkonClientAsync(apiKey, spaceId, userId, useProductionEndpoint);
        _ikonClient.AddCallback(this);
        IRoom room = _ikonClient.GetRoomByName(roomName);
        await room.ConnectAsync();
        _uiController = new UIController(room);
    }

    public async void OnApplicationQuit()
    {
        if (_ikonClient != null)
        {
            await _ikonClient.DisposeAsync();
            _ikonClient = null;
        }

        _uiController = null;
    }

    public async Task OnMessage(IRoom room, Message message)
    {
        await Task.CompletedTask;

        if (message.Opcode == Opcode.CHAT_MESSAGE_COMPLETE)
        {
            var chatMessageComplete = message.DeserializePayload<ChatMessageComplete>();
            var userName = room.GlobalState.GetUserName(chatMessageComplete.UserId);

            if (string.IsNullOrWhiteSpace(userName))
            {
                userName = "Unknown";
            }

            _outputMessages.Enqueue($"{userName}: {chatMessageComplete.GetText()}");
        }
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            using (var container = _uiController.CreateContainer())
            {
                container.AddChatTextFullFrame(ChatInputField.text);
            }

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
            case Ikon.Common.Protocol.LogType.Warning:
                Debug.LogWarning($"{logEvent.Type}: {logEvent.Message}");
                break;
            case Ikon.Common.Protocol.LogType.Error:
            case Ikon.Common.Protocol.LogType.Critical:
                Debug.LogError($"{logEvent.Type}: {logEvent.Message}");
                break;
            default:
                Debug.Log($"{logEvent.Type}: {logEvent.Message}");
                break;
        }
    }
}