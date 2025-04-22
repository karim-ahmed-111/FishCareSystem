import requests
import time
import urllib3
import random
import logging

# Configure logging
logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(levelname)s - %(message)s')
logger = logging.getLogger(__name__)

urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

def get_token():
    url = "https://localhost:7215/api/auth/login"
    data = {
        "userName": "iot@fishcare.com",
        "password": "IoT@123"
    }
    logger.info("Requesting authentication token")
    response = requests.post(url, json=data, verify=False)
    if response.status_code == 200:
        logger.info("Successfully obtained token")
        return response.json()["accessToken"]
    logger.error(f"Failed to get token: Status {response.status_code}, Response: {response.text}")
    raise Exception("Failed to get token")

# Initialize token
token = get_token()
url = "https://localhost:7215/api/sensor-readings"
headers = {
    "Authorization": f"Bearer {token}",
    "Content-Type": "application/json"
}

# Simulate dynamic sensor readings (placeholder for hardware sensor)
def get_sensor_reading():
    # Simulate temperature between 25°C and 35°C
    temperature = round(random.uniform(25.0, 35.0), 1)
    return {
        "tankId": 1,
        "type": "Temperature",
        "value": temperature,
        "unit": "°C"
    }

while True:
    try:
        sensor_data = get_sensor_reading()
        logger.info(f"Sending sensor data: {sensor_data}")
        response = requests.post(url, json=sensor_data, headers=headers, verify=False)
        if response.status_code == 200:
            logger.info(f"Successfully posted sensor reading: Status {response.status_code}, Response: {response.json()}")
        else:
            logger.warning(f"Failed to post sensor reading: Status {response.status_code}, Response: {response.text}")
    except Exception as e:
        logger.error(f"Error posting sensor reading: {e}")
        
        # Refresh token if expired
        logger.info("Refreshing authentication token")
        token = get_token()
        headers["Authorization"] = f"Bearer {token}"
    time.sleep(60)