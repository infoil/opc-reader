# OPC reader

An OPC-UA command-line (CLI) client able to read values from an OPC UA server.

The point of this software is to fetch values from an OPC server and save them to a file, so another program (the invoker) can process the file.

## Features
- Query online values from an OPC server.
- Query historical values from an OPC server.
- Write queried values to a CSV file.

## Getting Started

### Prerequisites

You need .NET Core 2.1 to run this program.

### Installation

There is no installation required.

### Quickstart

1. Build.
2. Publish to a folder
3. Run: `dotnet opcreader.dll (parameters)` (see examples folder)

## Resources

- Microsoft's [IoT edge OPC client](https://github.com/Azure-Samples/iot-edge-opc-client).
- OPC Foundation's [OPC UA .NET reference stack](https://github.com/OPCFoundation/UA-.NETStandard)
- OpenOpcUa's [simulation server](http://www.openopcua.org/)
- Prosys' [OPC UA simulation server](https://prosysopc.com/products/opc-ua-simulation-server/)