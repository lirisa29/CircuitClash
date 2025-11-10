using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class ProceduralPercussionGenerator : MonoBehaviour
{
    [Header("Percussion Settings")]
    public float bpm = 100f;
    public float volume = 0.6f;
    public int difficulty = 1;

    private float sampleRate;
    private double nextBeatDspTime;
    private int beatCount = 0;

    private float kickPhase;
    private float kickTime;
    private float snareTime;
    private float hatTime;

    private System.Random rand;

    void Start()
    {
        sampleRate = AudioSettings.outputSampleRate;
        nextBeatDspTime = AudioSettings.dspTime;
        rand = new System.Random();
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        double dspTime = AudioSettings.dspTime;
        double beatInterval = 60.0 / bpm;

        for (int i = 0; i < data.Length; i += channels)
        {
            float sample = 0f;

            if (dspTime >= nextBeatDspTime)
            {
                if (beatCount % 4 == 0)
                    kickTime = 0.05f;
                else if (beatCount % 4 == 2 && difficulty > 1)
                    snareTime = 0.05f;
                else if (difficulty >= 3)
                    hatTime = 0.02f;

                beatCount++;
                nextBeatDspTime += beatInterval / Mathf.Lerp(1f, 2f, (difficulty - 1) / 5f);
            }

            sample += GenerateKick();
            sample += GenerateSnare();
            sample += GenerateHiHat();

            sample *= volume * 3f;
            for (int c = 0; c < channels; c++)
                data[i + c] = sample;

            dspTime += 1.0 / sampleRate;
        }
    }

    float GenerateKick()
    {
        if (kickTime <= 0f) return 0f;
        float freq = 80f;
        kickPhase += 2 * Mathf.PI * freq / sampleRate;
        float sample = Mathf.Sin(kickPhase) * Mathf.Exp(-30f * (0.05f - kickTime));
        kickTime -= 1f / sampleRate;
        return sample;
    }

    float GenerateSnare()
    {
        if (snareTime <= 0f) return 0f;
        float noise = (float)(rand.NextDouble() * 2.0 - 1.0);
        float sample = noise * Mathf.Exp(-50f * (0.05f - snareTime));
        snareTime -= 1f / sampleRate;
        return sample * 0.6f;
    }

    float GenerateHiHat()
    {
        if (hatTime <= 0f) return 0f;
        float noise = (float)(rand.NextDouble() * 2.0 - 1.0);
        float sample = noise * Mathf.Exp(-200f * (0.02f - hatTime));
        hatTime -= 1f / sampleRate;
        return sample * 0.3f;
    }

    public void SetDifficulty(int newDifficulty)
    {
        difficulty = newDifficulty;
    }
}
