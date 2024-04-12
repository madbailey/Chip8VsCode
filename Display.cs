using SDL2;

namespace Chip8Emulator
{
    class Display
    {
        private IntPtr renderer;
        private int pixelWidth;
        private int pixelHeight;
        private SDL.SDL_Rect rect;

        public Display(IntPtr window)
        {
            renderer = SDL.SDL_CreateRenderer(window, -1, SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED);
            pixelWidth = 640 / 64;
            pixelHeight = 320 / 32;
            rect = new SDL.SDL_Rect();
        }

        public void Draw(CPU cpu)
        {
            SDL.SDL_SetRenderDrawColor(renderer, 0, 0, 0, 255); // Black background
            SDL.SDL_RenderClear(renderer);

            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    if (cpu.Display[x + y * 64] == 1) // Assuming the display is a flat array where 1 is white
                    {
                        SDL.SDL_SetRenderDrawColor(renderer, 255, 255, 255, 255); // White color for 'on' pixels
                    }
                    else
                    {
                        SDL.SDL_SetRenderDrawColor(renderer, 0, 0, 0, 255); // Black color for 'off' pixels
                    }

                    rect.x = x * pixelWidth;
                    rect.y = y * pixelHeight;
                    rect.w = pixelWidth;
                    rect.h = pixelHeight;
                    SDL.SDL_RenderFillRect(renderer, ref rect);
                }
            }

            SDL.SDL_RenderPresent(renderer);
        }

        public void Cleanup()
        {
            SDL.SDL_DestroyRenderer(renderer);
        }
    }
}
