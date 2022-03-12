# HRDLOG-net-library

[![NuGet](https://img.shields.io/nuget/v/HRDlibrary.svg?style=flat-square)](https://www.nuget.org/packages/HRDlibrary)

This library contains a collection of methods to send data to [HRDLOG.net](https://www.hrdlog.net) / [Ham365](https://www.ham365.net)

HRDLOG.net is an independent online logbook, free for all Amateur Radio Operators.

### Installation

The library is available on [NuGet](https://www.nuget.org/packages/HRDlibrary). Just search *HRDlibrary* in the **Package Manager GUI** or run the following command in the **Package Manager Console**:

    Install-Package HRDlibrary

### Usage
```C#
var protocol = new HrdProtocol("TEST", "0000000000");

// Send QSO to HRDLOG.net
string adif = "<call:4>TEST <qso_date:8:d>20090113 <time_on:6>210237 <band:3>20m <mode:2>CW <rst_sent:3>599 <rst_rcvd:3>599 <station_callsign:11>IW1QLH/TEST <EOR>";
var response1 = await protocol.SendQsoAsync(HrdProtocol.Cmd.Insert, adif);
Console.WriteLine($"SendQso: {response1}");

// Send ON-AIR status to HRDLOG.net
var response2 = await protocol.SendOnAirAsync(7100000, "USB", "TESTING");
Console.WriteLine($"OnAir: {response2}");

```

### Tech Stack

- C#
- .NET
  
### Authors

- [iw1qlh](http://www.iw1qlh.net)


**Contribute**

The project is constantly evolving. Contributions are welcome. Feel free to file issues and pull requests on the repo and we'll address them as we can. 