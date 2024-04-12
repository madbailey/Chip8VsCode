using System;
using System.Runtime.InteropServices;
using SDL2;

namespace Chip8
{
    public class Sound
    {
        private uint audioDevice;
        private SDL.SDL_AudioSpec audioSpec;

        public Sound()
        {
            SDL.SDL_Init(SDL.SDL_INIT_AUDIO);
            
            audioSpec.freq = 22050;
            audioSpec.format = SDL.AUDIO_S8;
            audioSpec.channels = 1;
            audioSpec.samples = 2048;
            audioSpec.callback = null;

            audioDevice = SDL.SDL_OpenAudioDevice(null, 0, ref audioSpec, out audioSpec, 0);
            SDL.SDL_PauseAudioDevice(audioDevice, 0);  // Start audio device
        }

        public void PlayBloop(double frequency, int duration)
        {
            int sampleRate = audioSpec.freq;
            int totalSamples = sampleRate * duration;
            byte[] audioBuffer = new byte[totalSamples];
            double decayRate = 0.0001;

            for (int i = 0; i < totalSamples; i++)
            {
                double angle = 2.0 * Math.PI * i * frequency / sampleRate;
                double amplitude = Math.Exp(-decayRate * i);
                audioBuffer[i] = (byte)(128 + amplitude * Math.Sin(angle) * 127); // 8-bit centered on 128 (0-255)
            }

            GCHandle handle = GCHandle.Alloc(audioBuffer, GCHandleType.Pinned);
            IntPtr bufferHandle = handle.AddrOfPinnedObject();
            SDL.SDL_QueueAudio(audioDevice, bufferHandle, (uint)audioBuffer.Length);
            SDL.SDL_Delay((uint)(1000 * duration));  // Wait for the duration of the sound
            handle.Free();  // Free the pinned memory
        }

        public void Stop()
        {
            SDL.SDL_CloseAudioDevice(audioDevice);
            SDL.SDL_Quit();
        }
    }
}
