using System;
using System.IO;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography;
using System.Text;

namespace Chip8Emulator
{
    class Program
    {
        static void Main(string[] args)
        {
            CPU cpu = new CPU();

            using (BinaryReader reader = new BinaryReader(new FileStream("ROMs/test_opcode.ch8", FileMode.Open)))
            {
                List<byte> program = new List<byte>();

                while (reader.BaseStream.Position != reader.BaseStream.Length)
                {
                   // program.Add((byte)((reader.ReadByte() << 8) | reader.ReadByte()));
                   program.Add(reader.ReadByte());

                }
                cpu.LoadProgram(program.ToArray());
            }

            while (true)
            {
                //try
                {
                    cpu.Step();
                    //cpu.DrawDisplay();

                }
                /*catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    break;
                }*/
            }


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
            for (int i = 0; i < program.Length; i++)
            {
                RAM[512 + i] = program[i];
            }
            PC = 512;
        }

        private Random generator = new Random(Environment.TickCount);

        public void Step()
        {
            var opcode = (ushort)(RAM[PC] << 8 | RAM[PC + 1]);
            //Console.WriteLine($"Executing opcode {opcode.ToString("X4")}");

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
                    if (Registers[(opcode & 0x0F00) >> 8] == Registers[(opcode & 0x00F0) >> 4]) // Vx == Vy
                    {
                        PC += 2;
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
                    DrawDisplay();
                    break;
                case 0xE000:
                    int vxIndex = (opcode & 0x0F00) >> 8;
                    int key = Registers[vxIndex];
                    switch (opcode & 0x00FF)
                    {
                        case 0x009E:
                            if (Keyboard[key] == true)
                            {
                                PC += 4;
                            }
                            else
                            {
                                PC += 2;
                            }
                            break;
                        case 0x00A1:
                            if (Keyboard[key] == false)
                            {
                                PC += 4;
                            }
                            else
                            {
                                PC += 2;
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
                            bool keyPress = false;
                            for (int i = 0; i < Keyboard.Length; i++)
                            {
                                if (Keyboard[i])
                                {
                                    Registers[vIndex] = (byte)i;
                                    keyPress = true;
                                    break;
                                }
                            }
                            if (!keyPress)
                            {
                                return; // Pause execution until a key is pressed
                            }
                            PC -= 2;
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
        public void DrawDisplay()
        {
            StringBuilder sb = new StringBuilder(64 * 32 + 32); // +32 for new lines

            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    sb.Append(Display[x + y * 64] == 0 ? ' ' : '#');
                }
                sb.AppendLine(); // Adds a newline character
            }

            Console.SetCursorPosition(0, 0);
            Console.Write(sb.ToString()); // Write the entire frame at once
        }


    }

}