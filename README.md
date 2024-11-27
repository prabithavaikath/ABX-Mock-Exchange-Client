# **ABX Mock Exchange Client**

A C# client application for interacting with the ABX Mock Exchange Server. The ABX server simulates a stock exchange environment and allows clients to request and retrieve stock ticker data. This client processes server responses, handles missing packets, and generates an output JSON file.


## **Features**
- Connects to the ABX Mock Exchange Server using TCP protocol.
- Sends requests to stream all available packets or to resend specific packets.
- Parses and processes fixed-size binary packets in **Big Endian** format.
- Handles missing sequence numbers by requesting specific packets from the server.
- Outputs parsed data into a structured JSON file.

## **Requirements**
- **C#**
  - .NET SDK 6.0 or higher
- **Server**
  - Node.js v16.17.0 or higher
- **Dependencies**
  - [Newtonsoft.Json](https://www.nuget.org/packages/Newtonsoft.Json/) for JSON serialization.
- **Development Environment**
  - Visual Studio or VS Code with C# extension.

## **Getting Started**

### **1. Setup the Server**
1. Clone the repository and navigate to the server directory.
2. Start the ABX Mock Exchange Server:
 
    node main.js
 
   TCP server started on port 3000.   

### **2. Setup the Client**
1. Clone the client repository.
2. Open the project in your IDE.
3. Install the required NuGet packages:
   
   dotnet add package Newtonsoft.Json
  
4. Build and run the client:
  
   dotnet run


## **Server Specification**

### **Server Connection**
- Protocol: TCP
- Host: `127.0.0.1`
- Port: `3000`

---

## **Request Payload Format**

The client sends a binary payload with the following structure:
- **`callType`** (1 byte):  
  - `1`: Stream All Packets  
  - `2`: Resend Packet  
- **`resendSeq`** (1 byte):  
  - Sequence number for a packet to be resent (only for `callType` = `2`).

---

## **Response Payload Format**

The server responds with fixed-size binary packets:
- **Symbol** (4 bytes): Stock ticker symbol (e.g., `MSFT`).  
- **Buy/Sell Indicator** (1 byte):  
  - `B`: Buy  
  - `S`: Sell  
- **Quantity** (4 bytes): Quantity of the order.  
- **Price** (4 bytes): Price of the order.  
- **Packet Sequence** (4 bytes): Unique sequence number for the packet.  
- **Endianness**: Big Endian for all numeric fields.

---

## **Handling Responses**

1. **Parsing Packets**:
   - Each packet is 17 bytes in length.
   - Extract and decode the fields based on the specification.

2. **Handling Missing Packets**:
   - Detect missing sequence numbers during parsing.
   - Request missing packets using `callType = 2` with the missing sequence number.

---

## **Output**

The client generates an `output.json` file containing an array of objects with the parsed packet data. 
[
  {
    "TickerSymbol": "MSFT",
    "BuySellIndicator": "B",
    "Quantity": 50,
    "Price": 100,
    "Sequence": 1
  },
  {
    "TickerSymbol": "AAPL",
    "BuySellIndicator": "S",
    "Quantity": 30,
    "Price": 120,
    "Sequence": 2
  }
]


