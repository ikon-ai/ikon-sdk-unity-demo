using Ikon.Sdk.DotNet;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using UnityEngine;

public class AudioOutputHandler : MonoBehaviour
{
    private AudioSource _audioSource;
    private readonly ConcurrentQueue<float> _audioSampleQueue = new();
    private readonly ConcurrentQueue<Action> _mainThreadActions = new();
    private int _channels;

    void Start()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        while (_mainThreadActions.TryDequeue(out var action))
        {
            action.Invoke();
        }
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        if (_channels == channels)
        {
            for (int i = 0; i < data.Length; i++)
            {
                if (!_audioSampleQueue.TryDequeue(out float sample))
                {
                    sample = 0.0f;
                }

                data[i] = sample;
            }
        }
        else if (_channels == 1 && channels == 2)
        {
            for (int i = 0; i < data.Length - 1; i += 2)
            {
                if (!_audioSampleQueue.TryDequeue(out float sample))
                {
                    sample = 0.0f;
                }

                data[i] = sample;
                data[i + 1] = sample;
            }
        }
        else if (_channels == 2 && channels == 1)
        {
            // TODO: Implement channel mixing from stereo to mono if needed
        }
    }

    public async Task OnAudioStreamBegin(object sender, Room.AudioStreamBeginArgs e)
    {
        await Task.CompletedTask;

        _channels = e.Channels;

        _mainThreadActions.Enqueue(() =>
        {
            AudioSettings.outputSampleRate = e.SampleRate;

            _audioSource.Stop();
            _audioSource.clip = AudioClip.Create("DynamicClip", e.SampleRate * e.Channels * 10, e.Channels, e.SampleRate, true);
            _audioSource.loop = true;
            _audioSource.Play();
        });
    }

    public async Task OnAudioFrame(object sender, Room.AudioFrameArgs e)
    {
        await Task.CompletedTask;

        foreach (var sample in e.Samples)
        {
            _audioSampleQueue.Enqueue(sample);
        }
    }

    public async Task OnAudioStreamEnd(object sender, Room.AudioStreamEndArgs e)
    {
        await Task.CompletedTask;
    }
}