using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SDL2;

namespace Chip8Emulator
{
    public class Input
    {
        private Dictionary<SDL.SDL_Keycode, int> keyMap;

        public Input()
        {
            keyMap = new Dictionary<SDL.SDL_Keycode, int>
            {
                [SDL.SDL_Keycode.SDLK_1] = 0x1,
                [SDL.SDL_Keycode.SDLK_2] = 0x2,
                [SDL.SDL_Keycode.SDLK_3] = 0x3,
                [SDL.SDL_Keycode.SDLK_4] = 0xC,
                [SDL.SDL_Keycode.SDLK_q] = 0x4,
                [SDL.SDL_Keycode.SDLK_w] = 0x5,
                [SDL.SDL_Keycode.SDLK_e] = 0x6,
                [SDL.SDL_Keycode.SDLK_r] = 0xD,
                [SDL.SDL_Keycode.SDLK_a] = 0x7,
                [SDL.SDL_Keycode.SDLK_s] = 0x8,
                [SDL.SDL_Keycode.SDLK_d] = 0x9,
                [SDL.SDL_Keycode.SDLK_f] = 0xE,
                [SDL.SDL_Keycode.SDLK_z] = 0xA,
                [SDL.SDL_Keycode.SDLK_x] = 0x0,
                [SDL.SDL_Keycode.SDLK_c] = 0xB,
                [SDL.SDL_Keycode.SDLK_v] = 0xF
            };
        }
        public void HandleEvent(SDL.SDL_Event sdlEvent, CPU cpu)
        {
            switch (sdlEvent.type)
            {
                case SDL.SDL_EventType.SDL_KEYDOWN:
                    if (keyMap.TryGetValue(sdlEvent.key.keysym.sym, out int keycodeDown))
                    {
                        cpu.KeyPress(keycodeDown);
                    }
                    break;
                case SDL.SDL_EventType.SDL_KEYUP:
                    if (keyMap.TryGetValue(sdlEvent.key.keysym.sym, out int keycodeUp))
                    {
                        cpu.KeyRelease(keycodeUp);
                    }
                    break;
            }
        }
    }
}
