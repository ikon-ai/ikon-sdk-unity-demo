using System.Collections.Concurrent;
using UnityEngine;

public class AudioSourceHandler : MonoBehaviour
{
    public int Channels = 1;

    private readonly ConcurrentQueue<float> _audioSampleQueue = new();

    void Start()
    {
    }

    void Update()
    {
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        if (Channels == channels)
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
        else if (Channels == 1 && channels == 2)
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
        else if (Channels == 2 && channels == 1)
        {
            // TODO: Implement channel mixing from stereo to mono if needed
        }
    }

    public void AddSamples(float[] samples)
    {
        foreach (var sample in samples)
        {
            _audioSampleQueue.Enqueue(sample);
        }
    }
}