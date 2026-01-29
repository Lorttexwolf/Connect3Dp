# Lib3Dp Oven Media Engine Plugin

This plugin enables Lib3Dp to relay live camera feeds from supported 3D printers into Oven Media Engine streams (origins).

> [Oven Media Engine](https://docs.ovenmediaengine.com/) is a Sub-Second Latency Live Streaming Server with Large-Scale and High-Definition. With OME, you can create platforms/services/systems that transmit high-definition video to hundreds-thousand viewers with sub-second latency and be scalable, depending on the number of concurrent viewers.

Supported machines with cameras will automatically publish their live feed to the following OME path: `machine/${UID}`.

## Getting Started

1. Review the [OME Getting Started Guide](https://docs.ovenmediaengine.com/getting-started) and follow the Using Docker setup instructions.
1. By default, the OME access token is set to: `ome-access-token`.
1. In Lib3Dp, configure the following environment variables: `OME_ACCESS_TOKEN` `OME_HOST_IP`.

## OME, Using Docker

Run the following from the root directory of this project.

1. Replace `${OME_HOST_IP}` with your host machine’s IP Address.
1. (Optional) Update the `origin_conf` volume path if you’ve moved it.

```
docker run --name ome -d \
  -e OME_HOST_IP=${OME_HOST_IP} \
  -p 1935:1935 \
  -p 9999:9999/udp \
  -p 9000:9000 \
  -p 3333:3333 \
  -p 3478:3478 \
  -p 10000-10009:10000-10009/udp \
  -p 8081:8081 \
  -p 8082:8082 \
  -p 20080:20080 \
  -p 20081:20081 \
  -v ./Lib3Dp/Plugins/OME/origin_conf:/opt/ovenmediaengine/bin/origin_conf \
  airensoft/ovenmediaengine:latest
```