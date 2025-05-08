using static System.Net.Mime.MediaTypeNames;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net;
using System;

namespace FO2_Launcher
{
    internal class Program
    {
        static private List<string> ipList;
        static ConsoleColor defBackgroundColor;
        static ConsoleColor defForegroundColor;
        static string gameName = "flatout2.exe";

        static string command;

        static void Main(string[] args)
        {
            ipList = new List<string>();
            defBackgroundColor = Console.BackgroundColor; 
            defForegroundColor = Console.ForegroundColor;

            GetIPAddresses();

            Console.WriteLine("");
            Console.WriteLine("Commands: run client; run server; set gamename");

            try
            {
                gameName = File.ReadAllText("settings.txt").Trim();
            }
            catch
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.BackgroundColor = ConsoleColor.Red;
                Console.WriteLine("Can't find settings.txt; creating...");
                Console.ForegroundColor = defForegroundColor;
                Console.BackgroundColor = defBackgroundColor;

                File.WriteAllText("settings.txt", gameName);
            }

            while (true)
            {
                command = Console.ReadLine();

                if (command == "run client")
                {
                    RunClient();
                }
                else if (command == "run server")
                {
                    RunServer();
                }
                else if (command == "set gamename")
                {
                    Console.WriteLine("current game name is: " + gameName + "\nset game name:");
                    SetGameName();
                    Console.WriteLine("game name has been saved and set to: " + gameName);
                }
            }
        }
        
        static void SetGameName()
        {
            gameName = Console.ReadLine();
            File.WriteAllText("settings.txt", gameName);
        }

        static void RunClient()
        {
            Console.WriteLine("Please enter server IP:");
            string ip = Console.ReadLine();

            if (ValidateIP(ip))
            {
                LaunchGame("-join=" + ip + " -lan");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.BackgroundColor = ConsoleColor.Red;
                Console.WriteLine("Invalid IP!");
                Console.ForegroundColor = defForegroundColor;
                Console.BackgroundColor = defBackgroundColor;
            }
        }

        static void RunServer()
        {
            Console.WriteLine($"Select an address (from 0 to {numberOfIPs-1}):");

            try
            {
                string ip = ipList[Int32.Parse(Console.ReadLine())];

                if (ValidateIP(ip))
                {
                    LaunchGame("-host -lan -private_addr=" + ip);
                }
                else
                {
                    Console.WriteLine("Invalid IP!");
                }
            }
            catch
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.BackgroundColor = ConsoleColor.Red;
                Console.WriteLine("WRONG INDEX OF AN IP ADDRESS");
                Console.ForegroundColor = defForegroundColor;
                Console.BackgroundColor = defBackgroundColor;
            }
        }

        static int numberOfIPs = 0;
        static void GetIPAddresses()
        {
            Console.WriteLine("ALL ADDRESSES");

            // Get a list of all network interfaces (usually one per network card, dialup, and VPN connection) 
            NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (NetworkInterface network in networkInterfaces)
            {
                // Read the IP configuration for each network 
                IPInterfaceProperties properties = network.GetIPProperties();

                // Each network interface may have multiple IP addresses 
                foreach (IPAddressInformation address in properties.UnicastAddresses)
                {
                    // We're only interested in IPv4 addresses for now 
                    if (address.Address.AddressFamily != AddressFamily.InterNetwork)
                        continue;

                    // Ignore loopback addresses (e.g., 127.0.0.1) 
                    if (IPAddress.IsLoopback(address.Address))
                        continue;

                    Console.WriteLine(numberOfIPs + ". " + address.Address.ToString() + " (" + network.Name + ")");
                    ipList.Add(address.Address.ToString());
                    numberOfIPs++;
                }
            }
        }

        static int LaunchGame(string args)
        {
            int exitCode = -1;

            // Prepare the process to run
            ProcessStartInfo start = new ProcessStartInfo(gameName);
            start.Arguments = args;
            start.UseShellExecute = true;

            // Run the external process & wait for it to finish
            try
            {
                using (Process proc = Process.Start(start))
                {
                    proc.WaitForExit();

                    // Retrieve the app's exit code
                    exitCode = proc.ExitCode;
                }
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.BackgroundColor = ConsoleColor.Red;
                Console.WriteLine(e.Message);
                Console.ForegroundColor = defForegroundColor;
                Console.BackgroundColor = defBackgroundColor;
            }

            return exitCode;
        }

        static bool ValidateIP(string ip)
        {
            int count = 0;
            string[] words = ip.Split('.');

            foreach (string word in words)
            {
                count++;

                try
                {
                    int temp = Convert.ToInt32(word);

                    if (temp < 0 || temp > 255)
                    {
                        return false;
                    }
                }
                catch (Exception e)
                {
                    return false;
                }

            }

            if (count != 4)
            {
                return false;
            }

            return true;
        }

    }
}