# hc2ha
Niko Home Control 2 Home Assistant

This is an MQTT-bridge which allows your Miko Home Control HobbyAPI devices to talk with Home Assistant. This is still a very early version, and still very work-in-progress. At the moment it currently supports lights and virtual outputs. You can use this application in order to control your lights and virtual outputs in Home Assistant. The project has been tested on the Niko HC Hub.

## Install
Usage through Docker is recommended:

``
docker run -e HA_MQTT_IP=xx.xx.xx.xx -e HC_IP=xx.xx.xx.xx -e HC_PASSWORD=niko_hobby_api_password mhtsbt/hc2ha
``

or using docker-compose:

```
hc2ha:
  image: mhtsbt/hc2ha
  restart: always
  environment:
    - HA_MQTT_IP=xx.xx.xx.xx
    - HC_IP=xx.xx.xx.xx
    - HC_PASSWORD=xxx
```
