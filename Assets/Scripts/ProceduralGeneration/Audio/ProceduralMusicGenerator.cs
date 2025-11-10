using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class ProceduralMusicGenerator : MonoBehaviour
{
    [Header("Music Settings")]
    public float baseFrequency = 220f;  // Base tone (A3)
    public float bpm = 100f;
    public float volume = 0.1f;
    public int difficulty = 1;

    [Header("Tension Settings")]
    public float maxFrequencyMultiplier = 4f;  // How high pitch can go at max tension
    public float noiseAmount = 0.01f;          // Adds distortion/noise at high tension

    private float sampleRate;
    private float nextNoteTime = 0f;
    private float phase = 0f;
    private float frequency = 0f;
    private System.Random rand = new System.Random();

    // Minor scale intervals in semitones
    private readonly float[] minorScale = { 0, 2, 3, 5, 7, 8, 10 };

    void Start()
    {
        sampleRate = AudioSettings.outputSampleRate;
        ScheduleNextNote();
    }

    void Update()
    {
        // Schedule next note based on BPM
        if (Time.time >= nextNoteTime)
        {
            ScheduleNextNote();
        }
    }

    void ScheduleNextNote()
    {
        // Pick random note from the scale
        float step = minorScale[rand.Next(minorScale.Length)];

        // Increase pitch range as difficulty rises
        float difficultyPitchFactor = 1f + ((difficulty - 1) * 0.1f);
        float semitoneOffset = step + rand.Next(0, difficulty); // higher tension -> higher notes
        frequency = baseFrequency * Mathf.Pow(2f, semitoneOffset / 12f) * difficultyPitchFactor;

        // Shorter notes as tension increases
        float beatDuration = 60f / bpm;
        float noteInterval = Mathf.Lerp(1f, 0.25f, Mathf.Clamp01((difficulty - 1) / 5f));
        nextNoteTime = Time.time + beatDuration * noteInterval;
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        for (int i = 0; i < data.Length; i += channels)
        {
            // Generate sine wave tone
            phase += 2 * Mathf.PI * frequency / sampleRate;
            float sample = Mathf.Sin(phase) * volume;
            
            if (difficulty > 2)
            {
                float noise = ((float)rand.NextDouble() * 2f - 1f) * noiseAmount * (difficulty / 5f);
                sample += noise;
            }

            // Apply clipping for tense feel at high difficulty
            if (difficulty >= 4)
                sample = (float)System.Math.Tanh(sample * (1f + difficulty * 0.5f));

            // Output to all channels
            for (int c = 0; c < channels; c++)
                data[i + c] = sample;
        }
    }

    // Called when difficulty increases
    public void SetDifficulty(int newDifficulty)
    {
        difficulty = newDifficulty;
    }
}
