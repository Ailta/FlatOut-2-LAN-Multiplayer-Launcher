using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace FO2_Launcher {
    struct Network {
        public string ip;
        public string name;

        public Network(string ip, string name) : this() {
            this.ip = ip;
            this.name = name;
        }
    }

    internal class NetworkManager {
        static int port = 23756;
        // This message was obtained by getting the first packet that is sent to the server by the client
        // then it was stripped down to this
        static string discoveryMessage = "000000000000bb217741464f3134000000000000000007";
        public static async Task<List<(string, string)>> DiscoverServersAsync(List<Network> networks) {
            List<(string IP, string NetworkName)> ipAddresses = new List<(string, string)>();
            // Go through each network and send a broadcast udp packet to flatout 2 port
            for (int i = 0; i < networks.Count; i++) {
                Network network = networks[i];
                Debug.WriteLine($"Scanning network {network.name}");
                // Split the ip address by '.', then join the first three elements, result=(172.0.0) and add '.', result=(172.0.0.1)
                string subnet = string.Join(".", network.ip.Split(new[] { "." }, StringSplitOptions.None).Take(3)) + ".";


                using (var udpClient = new UdpClient()) {
                    udpClient.EnableBroadcast = true;

                    var broadcastEndpoint = new IPEndPoint(IPAddress.Parse(subnet + "255"), port);
                    var sendBytes = HexStringToByteArray(discoveryMessage);

                    Debug.WriteLine($"Sending broadcast discovery packet to {broadcastEndpoint}");

                    try {
                        await udpClient.SendAsync(sendBytes, sendBytes.Length, broadcastEndpoint);
                    
                        Debug.WriteLine("Broadcast sent. Listening for responses...");

                        var responses = await ReceiveResponsesAsync(udpClient, TimeSpan.FromSeconds(5));

                        for (int ri = 0; ri < responses.Count; ri++) {
                            UdpReceiveResult respons = responses[ri];
                            string ip = $"{respons.RemoteEndPoint}".Split(':')[0];

                            if (!ipAddresses.Any(t => t.IP == ip)) {
                                ipAddresses.Add(new(ip, network.name));
                            }
                        }
                    } catch {
                        Debug.WriteLine("A socket operation was attempted to an unreachable network.");
                    }
                }
            }
            // Return all the server ip addresses and network names after finishing scanning
            return ipAddresses;
        }
        public static List<Network> GetIPAddresses() {
            List<Network> networks = new List<Network>();

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

            return networks;
        }
        public static bool ValidateIP(string ip) {
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

        static async Task<List<UdpReceiveResult>> ReceiveResponsesAsync(UdpClient udpClient, TimeSpan timeout) {
            var startTime = DateTime.UtcNow;
            List<UdpReceiveResult> responses = new List<UdpReceiveResult> { };

            while (DateTime.UtcNow - startTime < timeout) {
                if (udpClient.Available > 0) {
                    try {
                        var result = await udpClient.ReceiveAsync();
                        string responseHex = BitConverter.ToString(result.Buffer).Replace("-", "");
                        Debug.WriteLine($"Response from {result.RemoteEndPoint}: {responseHex}");
                        responses.Add(result);
                    } catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut) {
                        // Timeout, continue waiting until overall timeout expires
                    }
                } else {
                    // No data available, small delay to avoid busy loop
                    await Task.Delay(100);
                }
            }

            Debug.WriteLine("Finished listening for responses.");
            return responses;
        }

        public static byte[] HexStringToByteArray(string hex) {
            if (hex.Length % 2 != 0)
                throw new ArgumentException("Hex string must have an even length");

            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++) {
                string byteValue = hex.Substring(i * 2, 2);
                bytes[i] = Convert.ToByte(byteValue, 16);
            }
            return bytes;
        }
    }
}
