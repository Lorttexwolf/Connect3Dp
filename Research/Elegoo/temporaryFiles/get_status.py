import websocket
import json
import uuid
import time

def get_status(printer_ip):
    ws = websocket.create_connection(f"ws://{printer_ip}:3030/websocket", timeout=10)
    
    msg = {
        "Id": "",
        "Data": {
            "Cmd": 0,  # GET_PRINTER_STATUS
            "Data": {},
            "RequestID": uuid.uuid4().hex,
            "MainboardID": "",
            "TimeStamp": int(time.time() * 1000),
            "From": 1
        }
    }
    
    ws.send(json.dumps(msg))
    
    # Read messages to find the status
    for i in range(5):
        try:
            response = ws.recv()
            data = json.loads(response)
            
            if "Status" in data:
                s = data["Status"]
                print()
                print("╔══════════════════════════════════════════════════════════════╗")
                print("║              ELEGOO CENTAURI BLACK STATUS                    ║")
                print("╠══════════════════════════════════════════════════════════════╣")
                
                # Current Status
                status_map = {0: "Idle", 1: "Printing", 8: "Transferring"}
                status_code = s.get("CurrentStatus", [0])[0]
                status_text = status_map.get(status_code, f"Unknown ({status_code})")
                print(f"║  🖨️  Status:          {status_text:<38} ║")
                
                # Temperatures
                print("╠══════════════════════════════════════════════════════════════╣")
                print("║  🌡️  TEMPERATURES                                            ║")
                print(f"║      Hotbed:          {s.get('TempOfHotbed', 0):.1f}°C  (target: {s.get('TempTargetHotbed', 0):.0f}°C)".ljust(63) + "║")
                print(f"║      Nozzle:          {s.get('TempOfNozzle', 0):.1f}°C  (target: {s.get('TempTargetNozzle', 0):.0f}°C)".ljust(63) + "║")
                print(f"║      Chamber:         {s.get('TempOfBox', 0):.1f}°C  (target: {s.get('TempTargetBox', 0):.0f}°C)".ljust(63) + "║")
                
                # Fans
                fans = s.get("CurrentFanSpeed", {})
                print("╠══════════════════════════════════════════════════════════════╣")
                print("║  🌀  FANS                                                    ║")
                print(f"║      Model Fan:       {fans.get('ModelFan', 0)}%".ljust(63) + "║")
                print(f"║      Auxiliary Fan:   {fans.get('AuxiliaryFan', 0)}%".ljust(63) + "║")
                print(f"║      Box Fan:         {fans.get('BoxFan', 0)}%".ljust(63) + "║")
                
                # Lights
                lights = s.get("LightStatus", {})
                print("╠══════════════════════════════════════════════════════════════╣")
                print("║  💡  LIGHTS                                                  ║")
                print(f"║      Second Light:    {'ON' if lights.get('SecondLight', 0) else 'OFF'}".ljust(63) + "║")
                rgb = lights.get('RgbLight', [0,0,0])
                print(f"║      RGB Light:       R:{rgb[0]} G:{rgb[1]} B:{rgb[2]}".ljust(63) + "║")
                
                # Position
                print("╠══════════════════════════════════════════════════════════════╣")
                print("║  📍  POSITION                                                ║")
                print(f"║      Coordinates:     {s.get('CurrenCoord', '0,0,0')}".ljust(63) + "║")
                print(f"║      Z Offset:        {s.get('ZOffset', 0)}".ljust(63) + "║")
                
                # Print Info
                pi = s.get("PrintInfo", {})
                if pi.get("Filename"):
                    print("╠══════════════════════════════════════════════════════════════╣")
                    print("║  🖨️  PRINT JOB                                               ║")
                    print(f"║      File:            {pi.get('Filename', '')[:35]}".ljust(63) + "║")
                    print(f"║      Progress:        {pi.get('Progress', 0)}%".ljust(63) + "║")
                    print(f"║      Layer:           {pi.get('CurrentLayer', 0)} / {pi.get('TotalLayer', 0)}".ljust(63) + "║")
                    print(f"║      Speed:           {pi.get('PrintSpeedPct', 100)}%".ljust(63) + "║")
                
                print("╚══════════════════════════════════════════════════════════════╝")
                print()
                break
                
        except Exception as e:
            print(f"Error: {e}")
            break
    
    ws.close()

get_status("192.168.137.22")
