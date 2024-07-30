using Ikon.Common.Core;
using Ikon.Common.Core.Protocol;
using Ikon.Sdk.DotNet;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SceneHandler : MonoBehaviour
{
    public TMP_InputField ChatInputField;
    public TMP_Text ChatOutputText;
    public TMP_Text SendButtonText;
    public ButtonHandler SendButtonHandler;
    public GameObject SendButton;

    private IIkonClient _ikonClient;
    private Room _room;
    private readonly ConcurrentQueue<Action> _mainThreadActions = new();
    private readonly Dictionary<string, AudioStream> _audioStreams = new();
    private AudioClip _recordingAudioClip;
    private bool _shouldStartRecording;
    private bool _shouldStopRecording;
    private bool _areFirstSamples;
    private int _previousMicrophonePosition;
    private int _recordingChannels;

    private class AudioStream
    {
        public GameObject AudioSourceObject;
        public AudioSourceHandler AudioSourceHandler;
    }

    public async void Start()
    {
        Log.Instance.AddLogHandler(OnLogEvent);
        Log.Instance.Info($"Ikon AI C# SDK, version: {Ikon.Sdk.DotNet.Version.VersionString}");

        SendButtonHandler.PressStart += OnSendButtonPressStart;
        SendButtonHandler.PressStop += OnSendButtonPressStop;

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
        _room.Text += OnRoomText;
        _room.AudioStreamBegin += OnAudioStreamBegin;
        _room.AudioFrame += OnAudioFrame;
        _room.AudioStreamEnd += OnAudioStreamEnd;

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

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (!string.IsNullOrWhiteSpace(ChatInputField.text))
            {
                SendCurrentInput();
            }
        }

        while (_mainThreadActions.TryDequeue(out var action))
        {
            action.Invoke();
        }

        SendButtonText.text = string.IsNullOrWhiteSpace(ChatInputField.text) ? "Record" : "Send";

        HandleAudioRecording();
    }

    private void HandleAudioRecording()
    {
        if (_shouldStartRecording)
        {
            _recordingAudioClip = Microphone.Start(null, false, 60, 24000);
            _recordingChannels = _recordingAudioClip.channels;
            _areFirstSamples = true;
            _previousMicrophonePosition = 0;
            _shouldStartRecording = false;
            SendButton.GetComponent<Image>().color = Color.green;
        }
        else if (_recordingAudioClip != null)
        {
            int currentMicrophonePosition = Microphone.GetPosition(null);
            int samplesLength = currentMicrophonePosition - _previousMicrophonePosition;

            if (samplesLength < 0)
            {
                samplesLength = 0;
                _shouldStopRecording = true;
            }

            if (samplesLength > 0)
            {
                float[] samples = new float[samplesLength * _recordingChannels];
                _recordingAudioClip.GetData(samples, _previousMicrophonePosition);
                _previousMicrophonePosition = currentMicrophonePosition;
                _room.SendAudio(samples, 24000, _recordingChannels, _areFirstSamples, _shouldStopRecording);
                _areFirstSamples = false;
            }

            if (samplesLength == 0 && _shouldStopRecording && !_areFirstSamples)
            {
                _room.SendAudio(new float[_recordingChannels], 24000, _recordingChannels, _areFirstSamples, _shouldStopRecording);
            }

            if (_shouldStopRecording)
            {
                Microphone.End(null);
                _recordingAudioClip = null;
                _shouldStopRecording = false;
                SendButton.GetComponent<Image>().color = Color.white;
            }
        }
    }

    private void SendCurrentInput()
    {
        ChatOutputText.text += $"User: {ChatInputField.text}\n\n";
        _room.SendText(ChatInputField.text);
        ChatInputField.text = string.Empty;
        ChatInputField.ActivateInputField();
    }

    private async Task OnRoomText(object sender, Room.TextArgs e)
    {
        await Task.CompletedTask;

        _mainThreadActions.Enqueue(() =>
        {
            ChatOutputText.text += $"{e.UserName}: {e.Text}\n\n";
        });
    }

    private void OnSendButtonPressStart()
    {
        if (string.IsNullOrWhiteSpace(ChatInputField.text))
        {
            _shouldStartRecording = true;
            _shouldStopRecording = false;
        }
    }

    private void OnSendButtonPressStop()
    {
        if (!string.IsNullOrWhiteSpace(ChatInputField.text))
        {
            SendCurrentInput();
        }
        else
        {
            _shouldStopRecording = true;
        }
    }

    private async Task OnAudioStreamBegin(object sender, Room.AudioStreamBeginArgs e)
    {
        await Task.CompletedTask;

        if (!_audioStreams.ContainsKey(e.StreamId))
        {
            _mainThreadActions.Enqueue(() =>
            {
                var audioConfig = AudioSettings.GetConfiguration();
                audioConfig.sampleRate = e.SampleRate;
                AudioSettings.Reset(audioConfig);

                string audioSourceName = $"AudioSource{_audioStreams.Count + 1}";
                var audioSourceObject = new GameObject(audioSourceName);
                var audioSource = audioSourceObject.AddComponent<AudioSource>();
                var audioSourceHandler = audioSourceObject.AddComponent<AudioSourceHandler>();
                audioSourceHandler.Channels = e.Channels;
                audioSource.clip = AudioClip.Create(audioSourceName, e.SampleRate * e.Channels * 10, e.Channels, e.SampleRate, true);
                audioSource.loop = true;
                audioSource.Play();

                _audioStreams[e.StreamId] = new AudioStream
                {
                    AudioSourceObject = audioSourceObject,
                    AudioSourceHandler = audioSourceHandler,
                };
            });
        }
    }

    private async Task OnAudioFrame(object sender, Room.AudioFrameArgs e)
    {
        await Task.CompletedTask;

        if (_audioStreams.TryGetValue(e.StreamId, out var audioStream))
        {
            audioStream.AudioSourceHandler.AddSamples(e.Samples);
        }
    }

    private async Task OnAudioStreamEnd(object sender, Room.AudioStreamEndArgs e)
    {
        await Task.CompletedTask;

        if (_audioStreams.TryGetValue(e.StreamId, out var audioStream))
        {
            _mainThreadActions.Enqueue(() =>
            {
                Destroy(audioStream.AudioSourceObject);
                _audioStreams.Remove(e.StreamId);
            });
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