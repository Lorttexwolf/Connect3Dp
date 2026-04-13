import websocket
import json
import uuid
import time

def toggle_light(printer_ip, on=True):
    ws = websocket.create_connection(f"ws://{printer_ip}:3030/websocket", timeout=5)
    
    # RGB light: white (255,255,255) for on, black (0,0,0) for off
    rgb = [255, 255, 255] if on else [0, 0, 0]
    
    msg = {
        "Id": "",
        "Data": {
            "Cmd": 403,
            "Data": {
                "LightStatus": {
                    "SecondLight": 1 if on else 0,
                    "RgbLight": rgb
                }
            },
            "RequestID": uuid.uuid4().hex,
            "MainboardID": "",
            "TimeStamp": int(time.time() * 1000),
            "From": 1
        }
    }
    
    ws.send(json.dumps(msg))
    print(f"Sent: SecondLight={1 if on else 0}, RgbLight={rgb}")
    
    try:
        response = ws.recv()
        ws.close()
        return json.loads(response)
    except:
        ws.close()
        return {"status": "sent, no response"}

# Usage - change to on=False to turn off
# response = toggle_light("192.168.137.22", on=True)   # Light ON
response = toggle_light("192.168.137.22", on=False)  # Light OFF
print("Response:", response)