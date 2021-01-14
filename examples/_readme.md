# Parameter and configuration examples

## Query online values
openopcua.bat contains parameters suitable to query a local [OpenOpcUa simulation server](http://www.openopcua.org/). It uses the config file `openopcua-config.json`, which doesn't request to query historical values because this server doesn't support them.

## Query historical values
prosys.bat contains parameters suitable to query a local [Prosys OPC UA simulation server](https://prosysopc.com/products/opc-ua-simulation-server/). It uses the config file `prosys-config.json`, which exemplifies how to query historical values.

The attributes are defined by OPC-UA specification, but here's a little help:
* startTime / endTime: specify the period to query, can be `null`.
* isReadModified: when `false`, the server answer the measured values only (A, B); when `true`, the modified values only (B'). AFAIK there's no way to query both (A, B, B'), nor query non-modified plus modified values (A, B').
* numValuesPerNode: limit the quantity of results (zero for no limit).
* returnBounds: when false, any measured value at start or end time is not returned by the server.

## Deprecated functionalty

continuous-config.json
>This file exemplifies the configuration required to query an ID at a recurring interval.
This feature was in Microsoft's opcclient but doesn't match the concept of opcreader (be called from another program and write the queried values to a file, so the caller can read it), so it can be removed in a future version.

testconnectivity / testunsecureconnectivity parameters
>As these parameters add "tests" (recurring queries), are also deprecated and may be removed in the future too.