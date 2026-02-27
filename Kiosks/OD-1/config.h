#pragma once

// ============================================================
// WiFi Configuration
// ============================================================
#define WIFI_SSID     "YourNetworkSSID"
#define WIFI_PASSWORD "YourNetworkPassword"

// ============================================================
// Connect3Dp WebSocket Server
// ============================================================
#define C3DP_HOST "192.168.1.100"   // IP address or hostname of the Connect3Dp server
#define C3DP_PORT 5000              // Port the ASP.NET Core host listens on
#define C3DP_PATH "/ws"             // WebSocket endpoint path (default: /ws)

// ============================================================
// Machine to Monitor
// ============================================================
// Must match the MachineConnectionConfiguration.ID set in the Connect3Dp server config.
#define C3DP_MACHINE_ID "your-machine-id"

// ============================================================
// Display Configuration – adjust to match your panel
// ============================================================
#define SCREEN_WIDTH    480
#define SCREEN_HEIGHT   320
#define SCREEN_ROTATION 0   // 0 = landscape, 1 = portrait (hardware-dependent)

// LVGL draw buffer height in lines (1/10 of screen height is a safe default)
#define LVGL_BUF_LINES  (SCREEN_HEIGHT / 10)

// ============================================================
// Reconnection Intervals (milliseconds)
// ============================================================
#define WIFI_RECONNECT_INTERVAL_MS  5000
#define WS_RECONNECT_INTERVAL_MS    5000
