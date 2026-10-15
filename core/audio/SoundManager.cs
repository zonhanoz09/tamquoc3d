using Godot;

namespace TamQuoc3D.Core.Audio;

/// <summary>
/// SoundManager - Tu dong tong hop am thanh Retro Chiptune / 16-bit
/// Tao am thanh bang AudioStreamWav thu cong khong phu thuoc file ngoai.
/// </summary>
[GlobalClass]
public partial class SoundManager : Node
{
    public static SoundManager Instance { get; private set; }

    private const int PoolSize = 8;
    private readonly AudioStreamPlayer[] _audioPlayers = new AudioStreamPlayer[PoolSize];
    private int _poolIdx = 0;

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _Ready()
    {
        for (int i = 0; i < PoolSize; i++)
        {
            var p = new AudioStreamPlayer();
            AddChild(p);
            _audioPlayers[i] = p;
        }
    }

    private AudioStreamPlayer GetPlayer()
    {
        var p = _audioPlayers[_poolIdx];
        _poolIdx = (_poolIdx + 1) % PoolSize;
        return p;
    }

    private AudioStreamWav MakeToneWav(float freqStart, float freqEnd, float duration, string waveType = "square")
    {
        int sampleRate = 22050;
        int numSamples = (int)(sampleRate * duration);
        byte[] byteArray = new byte[numSamples];

        float phase = 0.0f;
        for (int i = 0; i < numSamples; i++)
        {
            float t = (float)i / numSamples;
            float freq = freqStart * (1.0f - t) + freqEnd * t;
            phase += 2.0f * Mathf.Pi * freq / sampleRate;
            if (phase > 2.0f * Mathf.Pi)
            {
                phase -= 2.0f * Mathf.Pi;
            }

            // Envelope (fade out at end)
            float env = 1.0f - t;
            if (t < 0.05f)
            {
                env = t / 0.05f;
            }

            float sampleVal = waveType switch
            {
                "square" => Mathf.Sin(phase) > 0.0f ? 1.0f : -1.0f,
                "triangle" => Mathf.Asin(Mathf.Sin(phase)) * (2.0f / Mathf.Pi),
                "noise" => (float)GD.RandRange(-1.0, 1.0),
                _ => Mathf.Sin(phase)
            };

            byte byteVal = (byte)Mathf.Clamp((int)(128 + sampleVal * env * 110), 0, 255);
            byteArray[i] = byteVal;
        }

        return new AudioStreamWav
        {
            Format = AudioStreamWav.FormatEnum.Format8Bits,
            MixRate = sampleRate,
            Stereo = false,
            Data = byteArray
        };
    }

    public void PlaySlash()
    {
        var wav = MakeToneWav(800.0f, 150.0f, 0.12f, "noise");
        var p = GetPlayer();
        p.Stream = wav;
        p.VolumeDb = -4.0f;
        p.PitchScale = (float)GD.RandRange(0.9, 1.2);
        p.Play();
    }

    public void PlayHit()
    {
        var wav = MakeToneWav(350.0f, 80.0f, 0.14f, "triangle");
        var p = GetPlayer();
        p.Stream = wav;
        p.VolumeDb = -2.0f;
        p.PitchScale = (float)GD.RandRange(0.95, 1.1);
        p.Play();
    }

    public void PlayCrit()
    {
        var wav = MakeToneWav(900.0f, 60.0f, 0.25f, "square");
        var p = GetPlayer();
        p.Stream = wav;
        p.VolumeDb = 0.0f;
        p.PitchScale = 1.0f;
        p.Play();
    }

    public void PlayBlock()
    {
        var wav = MakeToneWav(1200.0f, 600.0f, 0.15f, "triangle");
        var p = GetPlayer();
        p.Stream = wav;
        p.VolumeDb = -3.0f;
        p.PitchScale = 1.1f;
        p.Play();
    }

    public void PlayDodge()
    {
        var wav = MakeToneWav(300.0f, 850.0f, 0.12f, "sine");
        var p = GetPlayer();
        p.Stream = wav;
        p.VolumeDb = -5.0f;
        p.PitchScale = 1.0f;
        p.Play();
    }

    public void PlayMagic()
    {
        var wav = MakeToneWav(400.0f, 1100.0f, 0.28f, "square");
        var p = GetPlayer();
        p.Stream = wav;
        p.VolumeDb = -4.0f;
        p.PitchScale = (float)GD.RandRange(0.95, 1.15);
        p.Play();
    }

    public void PlayHeal()
    {
        var wav = MakeToneWav(440.0f, 880.0f, 0.35f, "sine");
        var p = GetPlayer();
        p.Stream = wav;
        p.VolumeDb = -3.0f;
        p.PitchScale = 1.0f;
        p.Play();
    }

    public void PlayRage()
    {
        var wav = MakeToneWav(200.0f, 1400.0f, 0.45f, "square");
        var p = GetPlayer();
        p.Stream = wav;
        p.VolumeDb = 2.0f;
        p.PitchScale = 1.0f;
        p.Play();
    }

    public void PlayVictory()
    {
        var wav = MakeToneWav(523.25f, 1046.5f, 0.6f, "square");
        var p = GetPlayer();
        p.Stream = wav;
        p.VolumeDb = 1.0f;
        p.PitchScale = 1.0f;
        p.Play();
    }
}
