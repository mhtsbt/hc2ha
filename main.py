import time
import paho.mqtt.client as mqtt


class HomeAssistantClient:

    def __init__(self):
        self.connected = False

        while not self.connected:
            try:
                self.client = mqtt.Client(client_id="Velo")
                self.client.connect("localhost", 1883)
                self.client.loop_start()
                self.connected = True
            except:
                print("Failed to connect to MQTT server")
                time.sleep(30)

    def _send_message(self, topic, content):
        self.client.publish(topic, payload=content)
        print(topic, content)

    def register_device(self):
        self._send_message(topic="homeassistant/binary_sensor/garden2/config", content='{"name": "garden2", "device_class": "motion", "state_topic": "homeassistant/binary_sensor/garden2/state"}')


class HomeControlClient:
    def __init__(self):
        pass