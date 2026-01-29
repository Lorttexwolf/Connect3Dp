# Connect3Dp

**Design a vendor-agnostic remote management framework for 3D Printing.**

A Purdue Northwest Undergraduate Research Proposal conducted by Aaron Jung, Justin Meng, and Jih Bin Luo.

## Components of this Project

### [Connect3Dp](./Connect3Dp/README.md)

* Utilizes Lib3Dp
* Exposes control functionality via HTTP
* Real-time updates via WebRTC.
* Authorization / Teams.
* Stores the configurations and files from 3D Printers.

### [Lib3Dp](./Lib3Dp/README.md)

Lib3Dp is a C# Library that provides a unified and extensible framework for communicating with and managing supported 3D printers. It defines strongly typed machine states, device capabilities, and hardware abstractions, enabling reliable control, monitoring, and automation across multiple printer ecosystems.

The library enables full remote interaction with supported printers, including starting, pausing, resuming, stopping, and monitoring print jobs. Machine settings can be queried and modified programmatically without requiring access to the printer’s physical interface, and both locally available prints and historical print data are exposed through the API.

Lib3Dp maintains comprehensive, real-time machine state tracking, encompassing printing and operational states, brand and device metadata, detailed logs, traces, and status messages. It also provides monitoring and control of nozzles, fans, lighting, and other supported peripherals, allowing fine-grained visibility into machine behavior.

Support for advanced multi-material systems is provided through a unified abstraction layer, including support for systems such as the Bambu Lab AMS. The library exposes descriptive material and material unit states, supports remote and scheduled material unit heating, and tracks material locations across nozzles and material units.

To simplify setup and deployment, Lib3Dp includes LAN-based discovery tools that automatically detect supported printers on the network, significantly reducing manual configuration and setup effort.

## Apps utilizing Connect3Dp

### [Handy3Dp](https://github.com/Lorttexwolf/Handy3Dp)

An open-source, mobile-first app to monitor your 3D Printer. Similar to [Prusa Connect](https://connect.prusa3d.com/), and [Bambu Handy](https://bambulab.com/en-us/download/app) but designed to interact with Connect3Dp.

### Farm3Dp

A upcoming solution to manage 3D Printers at a large-scale.