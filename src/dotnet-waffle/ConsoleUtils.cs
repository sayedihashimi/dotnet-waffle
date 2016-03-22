﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace dotnet_waffle
{
    public class ConsoleUtils
    {
        public static int ReadInt(int max) {
            var buffer = new List<char>();

            while (true) {
                // TODO: Add support for selecting via arrow keys (uparrow +1 to max, downarrow -1 to 0)

                var key = Console.ReadKey(true);

                if (char.IsDigit(key.KeyChar)) {
                    Console.Write(key.KeyChar);
                    buffer.Add(key.KeyChar);
                }
                else if (key.Key == ConsoleKey.Backspace) {
                    if (buffer.Count > 0) {
                        buffer.RemoveAt(buffer.Count - 1);
                        Backspace(1);
                    }
                }
                else if (key.Key == ConsoleKey.Enter) {
                    if (!buffer.Any()) {
                        // No chars entered so just return 1
                        Console.WriteLine();
                        return 1;
                    }

                    int selected;
                    if (int.TryParse(new string(buffer.ToArray()), out selected) && selected <= max) {
                        // Number entered is valid so return it
                        Console.WriteLine();
                        return selected;
                    }

                    // Number entered is invalid, clear the selection
                    Backspace(buffer.Count);
                    buffer.Clear();
                }
            }
        }

        public static void Backspace(int length) {
            Console.Write(new string('\b', length));
            Console.Write(new string(' ', length));
            Console.Write(new string('\b', length));

            // BUG: Following code is throwing System.IO.IOException: The handle is invalid
            //Console.SetCursorPosition(Console.CursorLeft - buffer.Count, Console.CursorTop);
            //Console.Write(new string (' ', buffer.Count));
            //Console.SetCursorPosition(Console.CursorLeft - buffer.Count, Console.CursorTop);
        }
    }
}
