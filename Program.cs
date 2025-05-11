using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using static FO2_Launcher.TUI;
using static FO2_Launcher.NetworkManager;

namespace FO2_Launcher {
    internal class Program {
        static private List<Network> networks = new List<Network> { };
        static string gameName = "flatout2.exe";

        static void Main(string[] args) {
            // Get all IPs
            networks = GetIPAddresses();
            InitTUI();
            ClearConsole();
            

            // Get the settings
            try {
                gameName = File.ReadAllText("settings.txt").Trim();
            } catch {
                var (cursorX, cursorY) = WriteLine("ERROR: Couldn't find settings.txt, creating...", false, 7, 0, defForegroundColor, ConsoleColor.DarkRed);
                File.WriteAllText("settings.txt", gameName);
                WriteLine("INFO: Created settings.txt", false, cursorX + 3, 0, defForegroundColor, ConsoleColor.DarkBlue);
            }

            // Main loop
            int selectedOption = 0;
            SelectingOptions(selectedOption);
            ConsoleKeyInfo input;
            do {
                input = Console.ReadKey();

                if (input.Key == ConsoleKey.DownArrow) { selectedOption++; }
                if (input.Key == ConsoleKey.UpArrow) { selectedOption--; }
                selectedOption &= 0b11;
                if (input.Key == ConsoleKey.Enter) { SelectOption(selectedOption); }
                SelectingOptions(selectedOption);
            } while (input.Key != ConsoleKey.Escape || input.Key != ConsoleKey.Backspace);
        }

        static void SelectOption(int option) {
            if (option == 0) {
                RunClient();
            }
            if (option == 1) {
                RunServer();
            }
        }

        static void SetGameName() {
            gameName = Console.ReadLine();
            File.WriteAllText("settings.txt", gameName);
        }

        async static void RunClient() {
            ClearConsole();
            WriteLine($"INFO: Scanning for servers. Please wait. Max wait time: 5 seconds per network.", false, 7, 0, defForegroundColor, ConsoleColor.DarkBlue);
            // Scan the networks for faltout 2 servers
            List<(string, string)> searchResults = DiscoverServersAsync(networks).GetAwaiter().GetResult();
            // Convert the searchresults to network List
            List<Network> servers = searchResults.Select(t => new Network(t.Item1, t.Item2)).ToList();

            // Clear console and writeout info that scan was sucessful
            ClearConsole();
            WriteLine($"INFO: Scan successful. Found {servers.Count} servers.", false, 7, 0, defForegroundColor, ConsoleColor.DarkBlue);

            // Writeout all the server and an option for custom ip
            int selectedOption = -1;
            SelectingOptions(selectedOption, servers, new List<string> { "Custom IP" });
            ConsoleKeyInfo input;
            do {
                input = Console.ReadKey();

                if (input.Key == ConsoleKey.Escape || input.Key == ConsoleKey.Backspace) { ClearConsole(); return; }
                if (input.Key == ConsoleKey.DownArrow) { selectedOption++; }
                if (input.Key == ConsoleKey.UpArrow) { selectedOption--; }
                if (selectedOption < -1) { selectedOption = servers.Count - 1; }
                if (selectedOption > servers.Count - 1) { selectedOption = -1; }

                SelectingOptions(selectedOption, servers, new List<string> { "Custom IP" });
            } while (input.Key != ConsoleKey.Enter);

            // If selected custom ip prompt and then try to connect to server after ip has been entered
            // else try connect to the selected server
            if (selectedOption == -1) {
                ClearConsole();
                WriteLine("Enter ip: ", false, 0, 2);

                string ip = Console.ReadLine();

                try {
                    if (ValidateIP(ip)) {
                        ClearConsole();
                        WriteLine($"INFO: Game's running.", false, 7, 0, defForegroundColor, ConsoleColor.DarkBlue);
                        LaunchGame("-join=" + ip + " -lan");
                        ClearConsole();
                    } else {
                        ClearConsole();
                        WriteLine("ERROR: Invalid IP!", false, 7, 0, defForegroundColor, ConsoleColor.DarkRed);
                    }
                } catch {
                    ClearConsole();
                    WriteLine("ERROR: Couldn't start the game! Unknown ERROR.", false, 7, 0, defForegroundColor, ConsoleColor.DarkRed);
                }
            } else {
                try {
                    ClearConsole();
                    WriteLine($"INFO: Game's running.", false, 7, 0, defForegroundColor, ConsoleColor.DarkBlue);
                    LaunchGame("-join=" + servers[selectedOption].ip + " -lan");
                } catch {
                    ClearConsole();
                    WriteLine("ERROR: Couldn't start the game! Unknown ERROR.", false, 7, 0, defForegroundColor, ConsoleColor.DarkRed);
                }
            }
        }

        static void RunServer() {
            ClearConsole();
            WriteLine($"Select an address to run the server on.", false, 0, 2);
            WriteLine($"INFO: Select an address to run the server on.", false, 7, 0, defForegroundColor, ConsoleColor.DarkBlue);

            // Writout all the available networks to run the server on
            int selectedOption = 0;
            SelectingOptions(selectedOption, networks);
            ConsoleKeyInfo input;
            do {
                input = Console.ReadKey();

                if (input.Key == ConsoleKey.Escape || input.Key == ConsoleKey.Backspace) { ClearConsole(); return; }
                if (input.Key == ConsoleKey.DownArrow) { selectedOption++; }
                if (input.Key == ConsoleKey.UpArrow) { selectedOption--; }
                if (selectedOption < 0) { selectedOption = networks.Count-1; }
                if (selectedOption > networks.Count-1) { selectedOption = 0; }

                SelectingOptions(selectedOption, networks);
            } while (input.Key != ConsoleKey.Enter);

            // Try to launch the game on the specified network
            try {
                Network network = networks[selectedOption];

                if (ValidateIP(network.ip)) {
                    ClearConsole();
                    WriteLine($"INFO: Game server launched on IP: {network.ip}", false, 7, 0, defForegroundColor, ConsoleColor.DarkBlue);
                    LaunchGame("-host -lan -private_addr=" + network.ip);
                    ClearConsole();
                } else {
                    ClearConsole();
                    WriteLine("ERROR: Invalid IP!", false, 7, 0, defForegroundColor, ConsoleColor.DarkRed);
                }
            } catch {
                ClearConsole();
                WriteLine("ERROR: Couldn't start the game! Unknown ERROR.", false, 7, 0, defForegroundColor, ConsoleColor.DarkRed);
            }
        }

        static int LaunchGame(string args) {
            int exitCode = -1;

            // Prepare the process to run
            ProcessStartInfo start = new ProcessStartInfo(gameName);
            start.Arguments = args;
            start.UseShellExecute = true;

            // Run the external process & wait for it to finish
            try {
                using (Process proc = Process.Start(start)) {
                    proc.WaitForExit();

                    // Retrieve the app's exit code
                    exitCode = proc.ExitCode;
                }
            } catch (Exception e) {
                WriteLine(e.Message, false, 7, 0, defForegroundColor, ConsoleColor.DarkRed);
            }

            return exitCode;
        }
    }
}