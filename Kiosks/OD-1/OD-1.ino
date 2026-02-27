/************************************************************
  Connect3Dp OD-1 Kiosk
  Adafruit Qualia ESP32-S3 (LVGL v8)

  Displays real-time machine status from a Connect3Dp server
  via WebSocket using the AtAGlance subscription level.

  Required Arduino libraries (install via Library Manager):
    - ArduinoWebsockets  by Gil Maimon
    - ArduinoJson        by Benoit Blanchon  (v6.x)
    - lvgl               (v8.x)

  The Adafruit Qualia ESP32-S3 BSP automatically registers the
  RGB panel display driver and touch controller with LVGL –
  no manual flush_cb or draw buffer configuration is required.

  See README.md for full setup instructions.
************************************************************/

#include <WiFi.h>
#include <lvgl.h>

// Project modules
#include "config.h"
#include "machine_state.h"
#include "ws_handler.h"
#include "ui_manager.h"

// ---- Shared application state ----------------------------------------------
MachineState g_machineState;

// ---- WebSocket handler -----------------------------------------------------
static WsHandler wsHandler;

// ---- Reconnect timing -------------------------------------------------------
static uint32_t s_lastWifiCheck = 0;
static uint32_t s_lastWsCheck   = 0;

// ============================================================
// setup
// ============================================================
void setup() {
    Serial.begin(115200);
    Serial.println("[Boot] Connect3Dp OD-1 Kiosk");

    // --- LVGL ---------------------------------------------------------------
    // The Adafruit Qualia ESP32-S3 BSP registers the RGB panel display driver
    // and tick source automatically; lv_init() is all that is needed here.
    lv_init();

    // --- Build UI -----------------------------------------------------------
    ui_init();
    ui_update(g_machineState);

    // --- Connect to WiFi ----------------------------------------------------
    Serial.print("[WiFi] Connecting to ");
    Serial.println(WIFI_SSID);
    WiFi.begin(WIFI_SSID, WIFI_PASSWORD);
    while (WiFi.status() != WL_CONNECTED) {
        delay(300);
        lv_timer_handler();  // keep the display alive during WiFi connect
    }
    Serial.print("[WiFi] IP: ");
    Serial.println(WiFi.localIP());

    // --- Connect WebSocket --------------------------------------------------
    wsHandler.begin();
    wsHandler.connect();
}

// ============================================================
// loop
// ============================================================
static MachineState s_lastRendered;
static bool         s_firstRender = true;

void loop() {
    const uint32_t now = millis();

    // --- WiFi watchdog ------------------------------------------------------
    if (now - s_lastWifiCheck >= WIFI_RECONNECT_INTERVAL_MS) {
        s_lastWifiCheck = now;
        if (WiFi.status() != WL_CONNECTED) {
            Serial.println("[WiFi] Reconnecting...");
            WiFi.reconnect();
        }
    }

    // --- WebSocket poll / reconnect -----------------------------------------
    if (WiFi.status() == WL_CONNECTED) {
        if (wsHandler.connected) {
            wsHandler.poll();
        } else if (now - s_lastWsCheck >= WS_RECONNECT_INTERVAL_MS) {
            s_lastWsCheck = now;
            Serial.println("[WS] Reconnecting...");
            wsHandler.connect();
        }
    }

    // --- LVGL handler -------------------------------------------------------
    lv_timer_handler();

    // --- Refresh display only when state changes ----------------------------
    const bool changed =
        s_firstRender ||
        g_machineState.wsConnected            != s_lastRendered.wsConnected     ||
        g_machineState.status                 != s_lastRendered.status          ||
        g_machineState.nickname               != s_lastRendered.nickname        ||
        g_machineState.hasJob                 != s_lastRendered.hasJob          ||
        g_machineState.job.name               != s_lastRendered.job.name        ||
        g_machineState.job.percentageComplete != s_lastRendered.job.percentageComplete ||
        g_machineState.job.remainingTime      != s_lastRendered.job.remainingTime;

    if (changed) {
        ui_update(g_machineState);
        s_lastRendered = g_machineState;
        s_firstRender  = false;
    }
}
