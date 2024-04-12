using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography;
using System.Text;
using SDL2;



namespace Chip8Emulator
{
    class Program
    {
        

        static void Main(string[] args)
        {
            Dictionary<SDL.SDL_Keycode, int> keyMap = new Dictionary<SDL.SDL_Keycode, int>
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

            CPU cpu = new CPU();

            if (SDL.SDL_Init(SDL.SDL_INIT_EVERYTHING) < 0 )
            {
                Console.WriteLine("SDL could not be initialized. SDL_Error: {0}", SDL.SDL_GetError());
                return;
            }
            IntPtr window = SDL.SDL_CreateWindow("Chip-8 Emulator", SDL.SDL_WINDOWPOS_CENTERED, SDL.SDL_WINDOWPOS_CENTERED, 640, 320, SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE);

            using (BinaryReader reader = new BinaryReader(new FileStream("ROMs/MISSILE.ch8", FileMode.Open)))
            {
                List<byte> program = new List<byte>();

                while (reader.BaseStream.Position != reader.BaseStream.Length)
                {
                   // program.Add((byte)((reader.ReadByte() << 8) | reader.ReadByte()));
                   program.Add(reader.ReadByte());

                }
                cpu.LoadProgram(program.ToArray());
            }
            SDL.SDL_Event sdlEvent;
            bool running = true;
            IntPtr renderer = SDL.SDL_CreateRenderer(window, -1, SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED);

            // Assuming each CHIP-8 pixel is represented as a 10x10 rectangle on our 640x320 window
            int pixelWidth = 640 / 64;
            int pixelHeight = 320 / 32;
            SDL.SDL_Rect rect = new SDL.SDL_Rect();

            while (running)
            {
                SDL.SDL_PollEvent(out sdlEvent);
                if (sdlEvent.type == SDL.SDL_EventType.SDL_QUIT)
                {
                    running = false;
                }
                else if (sdlEvent.type == SDL.SDL_EventType.SDL_KEYDOWN)
                {
                    if (keyMap.ContainsKey(sdlEvent.key.keysym.sym))
                    {
                        cpu.KeyPress(keyMap[sdlEvent.key.keysym.sym]);
                    }
                }
                else if (sdlEvent.type == SDL.SDL_EventType.SDL_KEYUP)
                {
                    if (keyMap.ContainsKey(sdlEvent.key.keysym.sym))
                    {
                        cpu.KeyRelease(keyMap[sdlEvent.key.keysym.sym]);
                    }
                }

                cpu.Step();

                SDL.SDL_SetRenderDrawColor(renderer, 0, 0, 0, 255);  // Black background
                SDL.SDL_RenderClear(renderer);

                for (int y = 0; y < 32; y++)
                {
                    for (int x = 0; x < 64; x++)
                    {
                        if (cpu.Display[x + y * 64] == 1)  // Assuming the display is a flat array where 1 is white
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

            SDL.SDL_DestroyRenderer(renderer);
            SDL.SDL_DestroyWindow(window);
            SDL.SDL_Quit();


        }
    }

    public class CPU
    {
        public byte[] RAM = new byte[4096];
        public byte[] Registers = new byte[16];
        public ushort PC = 0;
        public ushort I = 0;
        //public ushort [] Stack = new ushort[24];
        public Stack<ushort> Stack = new Stack<ushort>();
        public byte DelayTimer;
        public byte SoundTimer;
        public bool[] Keyboard = new bool[16];
        public bool WaitingForKeyPress = false;
        public byte[] Display = new byte[64 * 32];


        public void LoadProgram(byte[] program)
        {
            RAM = new byte[4096];
            InitializeFont();
            for (int i = 0; i < program.Length; i++)
            {
                RAM[512 + i] = program[i];
            }
            PC = 512;
        }

        private Random generator = new Random(Environment.TickCount);

        private Stopwatch watch = new Stopwatch();

        private void InitializeFont()
        {
            byte[] fontData = new byte[]
            {
                0xF0, 0x90, 0x90, 0x90, 0xF0, // 0
                0x20, 0x60, 0x20, 0x20, 0x70, // 1
                0xF0, 0x10, 0xF0, 0x80, 0xF0, // 2
                0xF0, 0x10, 0xF0, 0x10, 0xF0, // 3
                0x90, 0x90, 0xF0, 0x10, 0x10, // 4
                0xF0, 0x80, 0xF0, 0x10, 0xF0, // 5
                0xF0, 0x80, 0xF0, 0x90, 0xF0, // 6
                0xF0, 0x10, 0x20, 0x40, 0x40, // 7
                0xF0, 0x90, 0xF0, 0x90, 0xF0, // 8
                0xF0, 0x90, 0xF0, 0x10, 0xF0, // 9
                0xF0, 0x90, 0xF0, 0x90, 0x90, // A
                0xE0, 0x90, 0xE0, 0x90, 0xE0, // B
                0xF0, 0x80, 0x80, 0x80, 0xF0, // C
                0xE0, 0x90, 0x90, 0x90, 0xE0, // D
                0xF0, 0x80, 0xF0, 0x80, 0xF0, // E
                0xF0, 0x80, 0xF0, 0x80, 0x80  // F
            };

            Array.Copy(fontData, 0, RAM, 0, fontData.Length);       
        }
        public void KeyPress(int key)
        {
            if (key >= 0 && key < 16)
            {
                Keyboard[key] = true;
                Console.WriteLine($"Key pressed: {key}");   
            }
        }

        public void KeyRelease(int key)
        {
            if (key >= 0 && key < 16)
            {
                Keyboard[key] = false;
                Console.WriteLine($"Key released: {key}");
            }
        }
        public void Step()
        {
            var opcode = (ushort)(RAM[PC] << 8 | RAM[PC + 1]);
            WaitingForKeyPress = false;

            if (!watch.IsRunning) watch.Start();
            if (watch.ElapsedMilliseconds > 16)
            {
                if (DelayTimer > 0) DelayTimer--;
                if (SoundTimer > 0) SoundTimer--;
                watch.Restart();
            }


            ushort opCodeNibble = (ushort)(opcode & 0xF000); // Extract the first 4 bits

            PC += 2;

            switch (opCodeNibble)
            {
                case 0x0000:
                    if (opcode == 0x00e0)
                    {
                        for (int i = 0; i < Display.Length; i++) Display[i] = 0; // Clear the display
                    }
                    else if (opcode == 0x00ee)
                    {
                        PC = Stack.Pop(); // Return from subroutine
                    }
                    else
                    {
                        throw new Exception($"Unknown opcode {opcode.ToString("X4")}");
                    }
                    break;
                case 0x1000:
                    PC = (ushort)(opcode & 0x0FFF); // Jump to address NNN
                    break;
                case 0x2000:
                    Stack.Push(PC);
                    PC = (ushort)(opcode & 0x0FFF); // Call subroutine at NNN
                    break;
                case 0x3000:
                    if (Registers[(opcode & 0x0F00) >> 8] == (opcode & 0x00FF)) // Vx == kk
                    {
                        PC += 2;
                    }
                    break;
                case 0x4000:
                    if (Registers[(opcode & 0x0F00) >> 8] != (opcode & 0x00FF)) // Vx != kk
                    {
                        PC += 2;
                    }
                    break;
                case 0x5000:
                    if ((opcode & 0x000F) == 0) // Ensure it's the 0x5XY0 variant
                    {
                        if (Registers[(opcode & 0x0F00) >> 8] == Registers[(opcode & 0x00F0) >> 4])
                        {
                            PC += 2; // Skip the next instruction
                        }
                    }
                    else
                    {
                        throw new Exception($"Unsupported variant of 0x5000 opcode: {opcode.ToString("X4")}");
                    }
                    break;
                case 0x6000:
                    Registers[(opcode & 0x0F00) >> 8] = (byte)(opcode & 0x00FF); // Vx = kk
                    break;
                case 0x7000:
                    Registers[(opcode & 0x0F00) >> 8] += (byte)(opcode & 0x00FF); // Vx += kk
                    break;
                case 0x8000:
                    int vx = (opcode & 0x0F00) >> 8;
                    int vy = (opcode & 0x00F0) >> 4;

                    switch (opcode & 0x000F)
                    {
                        case 0: Registers[vx] = Registers[vy]; break; // Vx = Vy
                        case 1: Registers[vx] |= Registers[vy]; break; // Vx = Vx OR Vy
                        case 2: Registers[vx] &= Registers[vy]; break; // Vx = Vx AND Vy
                        case 3: Registers[vx] ^= Registers[vy]; break; // Vx = Vx XOR Vy
                        case 4:
                            Registers[15] = (byte)(Registers[vx] + Registers[vy] > 255 ? 1 : 0); // Set VF to 1 if there is a carry
                            Registers[vx] += Registers[vy]; // Vx += Vy
                            break;
                        case 5:
                            //If Vx > Vy, then VF is set to 1, otherwise 0. Then Vy is subtracted from Vx, and the results stored in Vx.
                            Registers[15] = (byte)(Registers[vx] >= Registers[vy] ? 1 : 0);
                            Registers[vx] = (byte)((Registers[vx] - Registers[vy]) & 0x00FF);
                            break;
                        case 6:
                            //If the least-significant bit of Vx is 1, then VF is set to 1, otherwise 0. Then Vx is divided by 2.
                            Registers[15] = (byte)(Registers[vx] & 0x0001);
                            Registers[vx] >>= 1;
                            break;
                        case 7:
                            //If Vy > Vx, then VF is set to 1, otherwise 0. Then Vx is subtracted from Vy, and the results stored in Vx.
                            Registers[15] = (byte)(Registers[vy] >= Registers[vx] ? 1 : 0);
                            Registers[vx] = (byte)((Registers[vy] - Registers[vx]) & 0x00FF);
                            break;
                        case 14:
                            //If the most-significant bit of Vx is 1, then VF is set to 1, otherwise to 0. Then Vx is multiplied by 2.
                            Registers[15] = (byte)(((Registers[vx] & 0x80) == 0x80) ? 1 : 0);
                            Registers[vx] <<= 1;
                            break;
                        default:
                            throw new Exception($"Unsupported opcode {opcode.ToString("X4")}");
                    }
                    break;
                case 0x9000:
                    if (Registers[(opcode & 0x0F00) >> 8] != Registers[(opcode & 0x00F0) >> 4]) // Vx != Vy
                    {
                        PC += 2;
                    }
                    break;
                case 0xA000:
                    I = (ushort)(opcode & 0x0FFF); // I = NNN
                    break;
                case 0xB000:
                    PC = (ushort)((opcode & 0x0FFF) + Registers[0]);
                    break;
                case 0xC000:
                    Registers[(opcode & 0x0F00) >> 8] = (byte)(generator.Next(0, 256) & (opcode & 0x00FF)); // Vx = random byte AND kk
                    break;
                case 0xD000:
                    //Draw a sprite at position VX, VY with N bytes of sprite data starting at the address stored in I
                    //Set VF to 01 if any set pixels are changed to unset, and 00 otherwise
                    int x = Registers[(opcode & 0x0F00) >> 8] % 64;
                    int y = Registers[(opcode & 0x00F0) >> 4] % 32;
                    int height = opcode & 0x000F;
                    Registers[15] = 0;

                    for (int i = 0; i < height; i++)
                    {
                        byte spriteByte = RAM[I + i];
                        for (int j = 0; j < 8; j++)
                        {
                            byte pixel = (byte)((spriteByte >> (7 - j)) & 0x01);
                            int index = ((x + j) % 64) + ((y + i) % 32) * 64; // Wrap around the display
                            if (pixel == 1 && Display[index] == 1)
                            {
                                Registers[15] = 1;
                            }
                            Display[index] ^= pixel;
                        }
                    }
               
                    break;
                case 0xE000:
                    int vxIndex = (opcode & 0x0F00) >> 8;
                    int key = Registers[vxIndex];
                    switch (opcode & 0x00FF)
                    {
                        case 0x009E:
                            if (Keyboard[key])
                            {
                                PC += 2; // Skip the next instruction
                            }
                            break;
                        case 0x00A1:
                            if (!Keyboard[key])
                            {
                                PC += 2; // Skip the next instruction
                            }
                            break;
                        default:
                            throw new Exception($"Unknown opcode {opcode.ToString("X4")}");
                    }
                    break;
                case 0xF000:
                    int vIndex = (opcode & 0x0F00) >> 8; // Extract the Vx index
                    switch (opcode & 0x00FF) // Switch on the lower byte
                    {
                        case 0x0007: // FX07: Set Vx = delay timer value
                            Registers[vIndex] = DelayTimer;
                            break;
                        case 0x000A: // FX0A: Wait for a key press, store the value of the key in Vx
                            // Implementation depends on your input handling
                            WaitingForKeyPress = true;
                            for (int i = 0; i < Keyboard.Length; i++)
                            {
                                if (Keyboard[i])
                                {
                                    Registers[(opcode & 0x0F00) >> 8] = (byte)i;
                                    WaitingForKeyPress = false;
                                    Console.WriteLine($"Key pressed: {i}"); 
                                    break;
                                }
                            }
                            if (WaitingForKeyPress)
                            {

                                PC -= 2; // Repeat the same instruction on the next cycle
                                return; // Pause execution until a key is pressed

                            }
                            
                            break;
                        case 0x0015: // FX15: Set delay timer = Vx
                            DelayTimer = Registers[vIndex];

                            break;
                        case 0x0018: // FX18: Set sound timer = Vx
                            SoundTimer = Registers[vIndex];

                            break;
                        case 0x001E: // FX1E: Set I = I + Vx
                            I += Registers[vIndex];

                            break;
                        case 0x0029: // FX29: Set I to the location of the sprite for the character in Vx
                            I = (ushort)(Registers[vIndex] * 5); // Assuming a font set where each character is 5 bytes long

                            break;
                        case 0x0033: // FX33: Store BCD representation of Vx in memory locations I, I+1, and I+2
                            RAM[I] = (byte)(Registers[vIndex] / 100);
                            RAM[I + 1] = (byte)((Registers[vIndex] / 10) % 10);
                            RAM[I + 2] = (byte)(Registers[vIndex] % 10);

                            break;
                        case 0x0055: // FX55: Store registers V0 through Vx in memory starting at location I
                            for (int i = 0; i <= vIndex; i++)
                            {
                                RAM[I + i] = Registers[i];
                            }
                            I += (ushort)(vIndex + 1);

                            break;
                        case 0x0065: // FX65: Read registers V0 through Vx from memory starting at location I
                            for (int i = 0; i <= vIndex; i++)
                            {
                                Registers[i] = RAM[I + i];
                            }
                            I += (ushort)(vIndex + 1);

                            break;
                        default:
                            throw new Exception($"Unknown opcode {opcode:X4}");
                    }
                    break;


                default:
                    throw new Exception($"Unknown opcode {opcode.ToString("X4")}");
            }
        }


    }

}