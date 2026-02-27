/************************************************************
  Connect3Dp On-Machine Kiosk
  ESP32-S3 HMI Panel (LVGL v8)

  Displays real-time machine status from a Connect3Dp server
  via WebSocket using the AtAGlance subscription level.

  Required Arduino libraries (install via Library Manager):
    - ArduinoWebsockets  by Gil Maimon
    - ArduinoJson        by Benoit Blanchon  (v6.x)
    - lvgl               (v8.x)
    - Display driver:    LovyanGFX  -or-  TFT_eSPI
    - Touch driver:      see your board documentation

  See README.md for full setup instructions.
************************************************************/

#include <WiFi.h>
#include <lvgl.h>

// Project modules
#include "config.h"
#include "machine_state.h"
#include "ws_handler.h"
#include "ui_manager.h"

// ============================================================
// DISPLAY DRIVER SETUP
// ============================================================
// Uncomment ONE of the sections below and fill in the pin /
// panel settings for your specific hardware.
//
// ---------- Option A: LovyanGFX (recommended for ESP32-S3) --
// #include <LovyanGFX.hpp>
//
// class LGFX : public lgfx::LGFX_Device {
//   lgfx::Panel_ST7796  _panel;
//   lgfx::Bus_SPI       _bus;
//   lgfx::Touch_FT5x06  _touch;
// public:
//   LGFX() {
//     // SPI bus
//     auto cfg = _bus.config();
//     cfg.spi_host = SPI2_HOST;
//     cfg.sclk = 12; cfg.mosi = 11; cfg.miso = 13; cfg.dc = 10;
//     _bus.config(cfg); _panel.setBus(&_bus);
//     // Panel
//     auto pcfg = _panel.config();
//     pcfg.pin_cs   = 9;  pcfg.pin_rst = 3;
//     pcfg.panel_width = SCREEN_WIDTH; pcfg.panel_height = SCREEN_HEIGHT;
//     _panel.config(pcfg);
//     // Touch (I2C)
//     auto tcfg = _touch.config();
//     tcfg.pin_sda = 38; tcfg.pin_scl = 39; tcfg.i2c_port = 0;
//     _touch.config(tcfg); _panel.setTouch(&_touch);
//     setPanel(&_panel);
//   }
// };
// static LGFX tft;
//
// static void lvgl_flush_cb(lv_disp_drv_t* drv, const lv_area_t* area, lv_color_t* colors) {
//   uint32_t w = area->x2 - area->x1 + 1;
//   uint32_t h = area->y2 - area->y1 + 1;
//   tft.pushImageDMA(area->x1, area->y1, w, h, (lgfx::rgb565_t*)colors);
//   lv_disp_flush_ready(drv);
// }
// static void lvgl_touch_cb(lv_indev_drv_t* drv, lv_indev_data_t* data) {
//   int16_t x, y;
//   if (tft.getTouch(&x, &y)) {
//     data->state = LV_INDEV_STATE_PR; data->point.x = x; data->point.y = y;
//   } else { data->state = LV_INDEV_STATE_REL; }
// }
//
// ---------- Option B: TFT_eSPI ------------------------------
// #include <TFT_eSPI.h>
// static TFT_eSPI tft;
//
// static void lvgl_flush_cb(lv_disp_drv_t* drv, const lv_area_t* area, lv_color_t* colors) {
//   uint32_t w = area->x2 - area->x1 + 1, h = area->y2 - area->y1 + 1;
//   tft.pushColors((uint16_t*)colors, w * h, true);  // adjust if colour order differs
//   lv_disp_flush_ready(drv);
// }
// static void lvgl_touch_cb(lv_indev_drv_t* drv, lv_indev_data_t* data) {
//   uint16_t x, y;
//   bool touched = tft.getTouch(&x, &y);
//   data->state   = touched ? LV_INDEV_STATE_PR : LV_INDEV_STATE_REL;
//   data->point.x = x; data->point.y = y;
// }
// ============================================================

// ---- Shared application state ----------------------------------------------
MachineState g_machineState;

// ---- LVGL draw buffer ------------------------------------------------------
static lv_disp_draw_buf_t s_drawBuf;
static lv_color_t         s_buf[SCREEN_WIDTH * LVGL_BUF_LINES];

// ---- WebSocket handler -----------------------------------------------------
static WsHandler wsHandler;

// ---- Reconnect timing -------------------------------------------------------
static uint32_t s_lastWifiCheck = 0;
static uint32_t s_lastWsCheck   = 0;

// ---- LVGL tick task (FreeRTOS) ---------------------------------------------
static void lvgl_tick_task(void* /*arg*/) {
    for (;;) {
        vTaskDelay(pdMS_TO_TICKS(5));
        lv_tick_inc(5);
    }
}

// ============================================================
// setup
// ============================================================
void setup() {
    Serial.begin(115200);
    Serial.println("[Boot] Connect3Dp On-Machine Kiosk");

    // --- Initialise display driver ------------------------------------------
    // TODO: call tft.init() / tft.setRotation() here (see comment block above)

    // --- LVGL ---------------------------------------------------------------
    lv_init();
    lv_disp_draw_buf_init(&s_drawBuf, s_buf, nullptr, SCREEN_WIDTH * LVGL_BUF_LINES);

    static lv_disp_drv_t dispDrv;
    lv_disp_drv_init(&dispDrv);
    dispDrv.hor_res  = SCREEN_WIDTH;
    dispDrv.ver_res  = SCREEN_HEIGHT;
    dispDrv.draw_buf = &s_drawBuf;
    // TODO: uncomment after configuring your display driver above:
    // dispDrv.flush_cb = lvgl_flush_cb;
    lv_disp_drv_register(&dispDrv);

    // Touch input device (optional – kiosk is display-only, no touch actions)
    // static lv_indev_drv_t touchDrv;
    // lv_indev_drv_init(&touchDrv);
    // touchDrv.type    = LV_INDEV_TYPE_POINTER;
    // touchDrv.read_cb = lvgl_touch_cb;
    // lv_indev_drv_register(&touchDrv);

    // LVGL tick source via a dedicated FreeRTOS task
    xTaskCreate(lvgl_tick_task, "lvgl_tick", 2048, nullptr, configMAX_PRIORITIES - 1, nullptr);

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
