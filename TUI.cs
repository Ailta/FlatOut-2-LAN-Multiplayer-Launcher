using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FO2_Launcher {
    internal class TUI {
        public static ConsoleColor defBackgroundColor;
        public static ConsoleColor defForegroundColor;

        public static void InitTUI() {
            defForegroundColor = Console.ForegroundColor;
            defBackgroundColor = Console.BackgroundColor;
        }

        public static void ClearConsole() {
            Console.Clear();
            WriteLine("FO2L", true, 0, 0, defBackgroundColor, defForegroundColor);
            WriteLine(" ", true, 0, Console.CursorTop);
        }

        public static (int, int) WriteLine(string text, bool toTheEnd = false, int x = 0, int y = 0, ConsoleColor? foreground = null, ConsoleColor? background = null) {
            // Set the default colors if not defined
            if (foreground == null) { foreground = defForegroundColor; }
            if (background == null) { background = defBackgroundColor; }
            int consoleWidth = Console.WindowWidth;
            int cursorX, cursorY;

            // Set colors to the console, set cursors position and writout text
            // then write to the end if said so
            Console.ForegroundColor = (ConsoleColor)foreground;
            Console.BackgroundColor = (ConsoleColor)background;
            Console.SetCursorPosition(x, y);
            Console.Write(text);
            (cursorX, cursorY) = Console.GetCursorPosition();
            if (toTheEnd) {
                for (int cX = x + text.Length; cX < consoleWidth; cX++) {
                    Console.Write(' ');
                }
            }
            Console.WriteLine();
            Console.ForegroundColor = defForegroundColor;
            Console.BackgroundColor = defBackgroundColor;

            return (cursorX, cursorY);
        }

        public static void SelectingOptions(int selectedOption) {
            if (selectedOption == 0) { WriteLine("run client", false, 0, 2, defBackgroundColor, defForegroundColor); } else { WriteLine("run client", false, 0, 2); }
            if (selectedOption == 1) { WriteLine("run server", false, 0, 3, defBackgroundColor, defForegroundColor); } else { WriteLine("run server", false, 0, 3); }
            if (selectedOption == 2) { WriteLine("find games executable in the same directory", false, 0, 4, defBackgroundColor, ConsoleColor.DarkGray); } else { WriteLine("find games executable in the same directory", false, 0, 4, ConsoleColor.DarkGray); }
            if (selectedOption == 3) { WriteLine("find flatout2 servers", false, 0, 5, defBackgroundColor, ConsoleColor.DarkGray); } else { WriteLine("find flatout2 servers", false, 0, 5, ConsoleColor.DarkGray); }
        }

        public static void SelectingOptions(int selectedOption, List<Network> networks, List<string>? customPrompts = null) {
            // If there's a custom prompt, it will offset all the actual networks down by the amount of custom prompts
            int cursorOffset = 0;
            if (customPrompts != null) {
                cursorOffset += customPrompts.Count;
                // Go through each prompt and write it out, if it is selected, invert colors
                for (int i = 0; i < customPrompts.Count(); i++) {
                    string customPrompt = customPrompts[i];
                    Debug.WriteLine((-(selectedOption)) - 1);
                    if ((-(selectedOption))-1 == i) { WriteLine(customPrompt, false, 0, i+3, defBackgroundColor, defForegroundColor); } 
                    else { WriteLine(customPrompt, false, 0, i + 3); }
                }
            }
            // Go through each network and write it out, if it is selected, invert colors
            for (int i = 0; i < networks.Count(); i++) {
                Network network = networks[i];
                if (selectedOption == i) { WriteLine($"{network.ip} ({network.name})", false, 0, i + 3 + cursorOffset, defBackgroundColor, defForegroundColor); }
                else { WriteLine($"{network.ip} ({network.name})", false, 0, i+3+cursorOffset); }
            }        
        }
    }
}
