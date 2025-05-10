using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using static FO2_Launcher.TUI;

namespace FO2_Launcher {
    struct Network {
        public string ip;
        public string name;

        public Network(string ip, string name) : this() {
            this.ip = ip;
            this.name = name;
        }
    }

    internal class Program {
        static private List<Network> networks = new List<Network> { };
        static int consoleLines = 0;
        static string gameName = "flatout2.exe";
        static string settings;

        static string command;

        static void Main(string[] args) {
            // Get all IPs
            GetIPAddresses();
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

        static void RunClient() {
            ClearConsole();
            WriteLine("Enter server IP:", false, 0, 2);
            string ip = Console.ReadLine();

            if (ValidateIP(ip)) {
                ClearConsole();
                WriteLine($"INFO: Game's running.", false, 7, 0, defForegroundColor, ConsoleColor.DarkBlue);
                LaunchGame("-join=" + ip + " -lan");
                ClearConsole();
            } else {
                ClearConsole();
                WriteLine("ERROR: Invalid IP!", false, 7, 0, defForegroundColor, ConsoleColor.DarkRed);
            }
        }

        static void RunServer() {
            ClearConsole();
            WriteLine($"Select an address to run the server on.", false, 0, 2);
            WriteLine($"INFO: Select an address to run the server on.", false, 7, 0, defForegroundColor, ConsoleColor.DarkBlue);

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

        static void GetIPAddresses() {
            // Get a list of all network interfaces (usually one per network card, dialup, and VPN connection) 
            NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (NetworkInterface network in networkInterfaces) {
                // Read the IP configuration for each network 
                IPInterfaceProperties properties = network.GetIPProperties();

                // Each network interface may have multiple IP addresses 
                foreach (IPAddressInformation address in properties.UnicastAddresses) {
                    // We're only interested in IPv4 addresses for now 
                    if (address.Address.AddressFamily != AddressFamily.InterNetwork)
                        continue;

                    // Ignore loopback addresses (e.g., 127.0.0.1) 
                    if (IPAddress.IsLoopback(address.Address))
                        continue;

                    networks.Add(new Network(address.Address.ToString(), network.Name));
                }
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

        static bool ValidateIP(string ip) {
            int count = 0;
            string[] words = ip.Split('.');

            foreach (string word in words) {
                count++;

                try {
                    int temp = Convert.ToInt32(word);

                    if (temp < 0 || temp > 255) {
                        return false;
                    }
                } catch (Exception e) {
                    return false;
                }

            }

            if (count != 4) {
                return false;
            }

            return true;
        }

    }
}