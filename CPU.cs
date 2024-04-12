using System.Diagnostics;

namespace Chip8Emulator
{
    public class CPU
    {
        public byte[] RAM = new byte[4096];
        public byte[] Registers = new byte[16];
        public ushort PC = 0;
        public ushort I = 0;
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
                    int x = Registers[(opcode & 0x0F00) >> 8] % 64; // VX register
                    int y = Registers[(opcode & 0x00F0) >> 4] % 32; // VY register
                    int height = opcode & 0x000F; // N bytes of sprite
                    Registers[15] = 0; // VF register for collision detection

                    for (int row = 0; row < height; row++)
                    {
                        byte spriteByte = RAM[I + row]; // Get sprite byte from memory
                        for (int col = 0; col < 8; col++) // 8 pixels per byte (each bit is a pixel)
                        {
                            byte pixel = (byte)((spriteByte >> (7 - col)) & 0x01);
                            int index = ((x + col) % 64) + ((y + row) % 32) * 64; // Wrap around the display
                            if (pixel == 1 && Display[index] == 1)
                            {
                                Registers[15] = 1; // Set VF to 1 if any pixel is unset (collision detection)
                            }
                            Display[index] ^= pixel; // XOR to toggle the pixel
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