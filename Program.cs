using Chip8;
using SDL2;



namespace Chip8Emulator
{
    class Emulator
    {
        static void Main(string[] args)
        {
            Sound sound = new Sound();
            CPU cpu = new CPU();
            Input input = new Input();

            if (SDL.SDL_Init(SDL.SDL_INIT_EVERYTHING) < 0)
            {
                Console.WriteLine("SDL could not be initialized. SDL_Error: {0}", SDL.SDL_GetError());
                return;
            }

            IntPtr window = SDL.SDL_CreateWindow("Chip-8 Emulator", SDL.SDL_WINDOWPOS_CENTERED, SDL.SDL_WINDOWPOS_CENTERED, 640, 320, SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE);
            Display display = new Display(window);
            

            using (BinaryReader reader = new BinaryReader(new FileStream("ROMs/BRIX.ch8", FileMode.Open)))
            {
                List<byte> program = new List<byte>();

                while (reader.BaseStream.Position != reader.BaseStream.Length)
                {
                   // program.Add((byte)((reader.ReadByte() << 8) | reader.ReadByte()));
                   program.Add(reader.ReadByte());

                }
                cpu.LoadProgram(program.ToArray());
            }
        bool running = true;
        SDL.SDL_Event sdlEvent;


        while (running)
        {
            SDL.SDL_PollEvent(out sdlEvent);
            input.HandleEvent(sdlEvent, cpu);
            if (sdlEvent.type == SDL.SDL_EventType.SDL_QUIT)
            {
                running = false;
            }

            cpu.Step();
            display.Draw(cpu);
         }
        sound.Stop();
        display.Cleanup();
        SDL.SDL_DestroyWindow(window);
        SDL.SDL_Quit();
        }
    }

    

}