# Connect3Dp

**Design a vendor-agnostic remote management framework for 3D Printing.**

A Purdue Northwest Undergraduate Research Proposal conducted by Aaron Jung, Justin Meng, and Jih Bin Luo.

## Components of Project

### Connect3Dp

The Connect3Dp App.

* Utilizes Lib3Dp
* Authorization / Teams.
* Exposes control functionality via HTTP
* Real-time updates via WebRTC.
* Stores the configurations and files from 3D Printers.

### Lib3Dp

A C# Library providing the states, objects, and drivers to communicate with the supported 3D Printers. 

#### Reverse Engineering Documents

[Bambu Lab](./Research/BambuLab/README.md)

[Creality K1C](./Research/Creality/CrealityK1C.md)

[ELEGOO Centauri Carbon / SDCP](./Research/Elegoo/README.md)
