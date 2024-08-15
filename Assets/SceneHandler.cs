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
    private Channel _channel;
    private readonly ConcurrentQueue<Action> _mainThreadActions = new();
    private readonly Dictionary<string, AudioStream> _audioStreams = new();
    private AudioClip _recordingAudioClip;
    private bool _shouldStartRecording;
    private bool _shouldStopRecording;
    private bool _areFirstSamples;
    private int _previousMicrophonePosition;
    private int _recordingAudioChannels;

    private const int PlaybackAudioSampleRate = 48000;
    private const int RecordingAudioSampleRate = 24000;

    private class AudioStream
    {
        public GameObject AudioSourceObject;
        public AudioSourceHandler AudioSourceHandler;
    }

    public void Awake()
    {
        Application.targetFrameRate = 30;

        var audioConfig = AudioSettings.GetConfiguration();
        audioConfig.sampleRate = PlaybackAudioSampleRate;
        AudioSettings.Reset(audioConfig);
    }

    public async void Start()
    {
        Log.Instance.LogEvent += OnLogEvent;
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

            Description = "Ikon AI SDK Unity Example",
            DeviceId = Utils.GenerateDeviceId(),
            ProductId = "Ikon.Sdk.DotNet.Examples.Unity",
            VersionId = "1",
            InstallId = "1",
            UserType = UserType.Human,
            OpcodeGroupsFromServer = Opcode.GROUP_ALL,
            OpcodeGroupsToServer = Opcode.GROUP_ALL,
        };

        // Set the channel key to use
        var channelKey = Environment.GetEnvironmentVariable("IKON_SDK_CHANNEL_KEY") ?? "<<SET_CHANNEL_KEY_HERE>>";

        _ikonClient = await Sdk.CreateIkonClientAsync(clientInfo);

        _channel = new Channel(_ikonClient, channelKey);
        _channel.Text += OnChannelText;
        _channel.AudioStreamBegin += OnAudioStreamBegin;
        _channel.AudioFrame += OnAudioFrame;
        _channel.AudioStreamEnd += OnAudioStreamEnd;

        await _channel.ConnectAsync();
        _channel.SignalReady();
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
            _recordingAudioClip = Microphone.Start(null, false, 60, RecordingAudioSampleRate);
            _recordingAudioChannels = _recordingAudioClip.channels;
            _areFirstSamples = true;
            _previousMicrophonePosition = 0;
            _shouldStartRecording = false;
            SendButton.GetComponent<Image>().color = Color.green;
        }
        else if (_recordingAudioClip != null)
        {
            int currentMicrophonePosition = Microphone.GetPosition(null);
            int samplesLength = currentMicrophonePosition - _previousMicrophonePosition;

            // Sometimes the Unity microphone does not work (usually the first recording), so stop recording
            if (samplesLength < 0)
            {
                samplesLength = 0;
                _shouldStopRecording = true;
            }

            if (samplesLength > 0)
            {
                float[] samples = new float[samplesLength * _recordingAudioChannels];
                _recordingAudioClip.GetData(samples, _previousMicrophonePosition);
                _previousMicrophonePosition = currentMicrophonePosition;
                _channel.SendAudio(samples, RecordingAudioSampleRate, _recordingAudioChannels, _areFirstSamples, _shouldStopRecording); // First samples should be sent with IsFirst=true
                _areFirstSamples = false;
            }

            // If any samples were sent, then it should be made sure that the last samples are sent with IsLast=true
            if (samplesLength == 0 && _shouldStopRecording && !_areFirstSamples)
            {
                _channel.SendAudio(new float[_recordingAudioChannels], RecordingAudioSampleRate, _recordingAudioChannels, false, true);
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
        _channel.SendText(ChatInputField.text, sendBackToSender: true);
        ChatInputField.text = string.Empty;
        ChatInputField.ActivateInputField();
    }

    private async Task OnChannelText(object sender, Channel.TextArgs e)
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

    private async Task OnAudioStreamBegin(object sender, Channel.AudioStreamBeginArgs e)
    {
        await Task.CompletedTask;

        e.SampleRate = PlaybackAudioSampleRate;

        if (!_audioStreams.ContainsKey(e.StreamId))
        {
            _mainThreadActions.Enqueue(() =>
            {
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

    private async Task OnAudioFrame(object sender, Channel.AudioFrameArgs e)
    {
        await Task.CompletedTask;

        if (_audioStreams.TryGetValue(e.StreamId, out var audioStream))
        {
            audioStream.AudioSourceHandler.AddSamples(e.Samples);
        }
    }

    private async Task OnAudioStreamEnd(object sender, Channel.AudioStreamEndArgs e)
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

    private void OnLogEvent(object sender, LogEvent logEvent)
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