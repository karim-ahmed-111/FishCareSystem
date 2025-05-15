# iot_actuator.py
import paho.mqtt.client as mqtt
# Simulate IoT actuator receiving commands
def on_connect(client, userdata, flags, reason_code, properties=None):
    print(f"Connected with result code {reason_code}")
    client.subscribe("fishcare/tank/1/device/#")

def on_message(client, userdata, msg, properties=None):
    status = msg.payload.decode()
    print(f"Received command for {msg.topic}: {status}")
    # Placeholder for actual actuator logic
    # TODO: Replace with hardware control (e.g., turn on/off relay on ESP32)
    print(f"Actuating device: {status}")

client = mqtt.Client(mqtt.CallbackAPIVersion.VERSION2)
client.on_connect = on_connect
client.on_message = on_message
client.connect("0.0.0.0", 1883)
client.loop_forever()