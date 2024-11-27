using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        string server = "127.0.0.1"; // Server IP
        int port = 3000;            // Server port

        List<Packet> receivedPackets = new List<Packet>(); // Store packets

        try
        {
            // Connect to the ABX server
            using (TcpClient client = new TcpClient(server, port))
            using (NetworkStream stream = client.GetStream())
            {
                Console.WriteLine("Connected to ABX Exchange Server.");

                // Send "Stream All Packets" request
                byte[] requestPayload = new byte[] { 1, 0 }; // CallType = 1, ResendSeq = 0
                stream.Write(requestPayload, 0, requestPayload.Length);
                Console.WriteLine("Request sent to stream all packets.");

                // Receive and parse response packets
                byte[] buffer = new byte[1024]; // Use a larger buffer to accumulate data
                int bytesRead;

                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    // Accumulate data until we have at least 17 bytes for one packet
                    byte[] accumulatedData = buffer.Take(bytesRead).ToArray();
                    while (accumulatedData.Length >= 17)
                    {
                        byte[] packetData = accumulatedData.Take(17).ToArray(); // Extract one packet
                        accumulatedData = accumulatedData.Skip(17).ToArray();   // Remove extracted packet

                        try
                        {
                            Packet packet = ParsePacket(packetData);
                            receivedPackets.Add(packet);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error parsing packet: {ex.Message}");
                        }
                    }
                }
            }

            // Check for missing sequences and request them
            CheckAndRequestMissingPackets(receivedPackets);

            // Save to JSON file
            string jsonOutput = JsonConvert.SerializeObject(receivedPackets, Formatting.Indented);
            File.WriteAllText("output.json", jsonOutput);
            Console.WriteLine("Output JSON file has been saved as 'output.json'.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    // Parse the binary packet into a Packet object
    static Packet ParsePacket(byte[] data)
    {
        // Debug raw packet data
        Console.WriteLine($"Raw packet data: {BitConverter.ToString(data)}");

        // Ensure packet data is exactly 17 bytes
        if (data.Length != 17)
        {
            throw new Exception("Invalid packet size. Expected 17 bytes.");
        }

        // Parse fields from the binary data
        Packet packet = new Packet
        {
            TickerSymbol = Encoding.ASCII.GetString(data, 0, 4).Trim('\0'),
            BuySellIndicator = Encoding.ASCII.GetString(data, 4, 1),
            Quantity = BitConverter.ToInt32(data[5..9].Reverse().ToArray(), 0), // Big Endian
            Price = BitConverter.ToInt32(data[9..13].Reverse().ToArray(), 0),  // Big Endian
            Sequence = BitConverter.ToInt32(data[13..17].Reverse().ToArray(), 0) // Big Endian
        };

        // Debug parsed fields
        Console.WriteLine($"Parsed Packet: Symbol={packet.TickerSymbol}, Indicator={packet.BuySellIndicator}, Quantity={packet.Quantity}, Price={packet.Price}, Sequence={packet.Sequence}");

        return packet;
    }

    // Check for missing sequences and request them
   static void CheckAndRequestMissingPackets(List<Packet> packets)
{
    packets.Sort((x, y) => x.Sequence.CompareTo(y.Sequence)); // Sort by sequence number
    int expectedSequence = 1;

    // List to temporarily store the missing packets
    List<Packet> missingPackets = new List<Packet>();

    using (TcpClient client = new TcpClient("127.0.0.1", 3000))
    using (NetworkStream stream = client.GetStream())
    {
        foreach (var packet in packets.ToList()) // Use ToList() to avoid modifying the collection while iterating
        {
            while (expectedSequence < packet.Sequence)
            {
                Console.WriteLine($"Missing packet detected: Sequence {expectedSequence}");

                // Request missing packet
                byte[] requestPayload = new byte[] { 2, (byte)expectedSequence }; // callType = 2
                stream.Write(requestPayload, 0, requestPayload.Length);

                // Read the server response for the missing packet
                byte[] buffer = new byte[17];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);

                if (bytesRead == 17)
                {
                    try
                    {
                        Packet missingPacket = ParsePacket(buffer);
                        missingPackets.Add(missingPacket);
                        Console.WriteLine($"Retrieved missing packet: Sequence {missingPacket.Sequence}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing missing packet: {ex.Message}");
                    }
                }

                expectedSequence++;
            }

            expectedSequence = packet.Sequence + 1;
        }

        // Add missing packets after iteration
        packets.AddRange(missingPackets);
    }

    Console.WriteLine("All missing sequences detected and retrieved.");
}


}

// Define the Packet class for storing packet data
public class Packet
{
    public string? TickerSymbol { get; set; }      // Stock ticker symbol (nullable to avoid warnings)
    public string? BuySellIndicator { get; set; } // "B" for Buy, "S" for Sell (nullable to avoid warnings)
    public int Quantity { get; set; }             // Quantity of the order
    public int Price { get; set; }                // Price of the order
    public int Sequence { get; set; }             // Packet sequence number
}
