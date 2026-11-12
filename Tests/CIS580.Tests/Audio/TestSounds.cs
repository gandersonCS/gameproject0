using System;
using Microsoft.Xna.Framework.Audio;

namespace CIS580.Tests.Audio;

/// <summary>
/// Makes silent sounds for tests, skipping the test if the machine has no audio device
/// </summary>
internal static class TestSounds
{
    public static SoundEffect Silent(double seconds = 1)
    {
        try
        {
            int samples = (int)(22050 * seconds);
            return new SoundEffect(new byte[samples * 2], 22050, AudioChannels.Mono);
        }
        catch (Exception e) when (e is NoAudioHardwareException or DllNotFoundException or TypeInitializationException)
        {
            Assert.Skip($"No audio device: {e.Message}");
            return null;
        }
    }
}
