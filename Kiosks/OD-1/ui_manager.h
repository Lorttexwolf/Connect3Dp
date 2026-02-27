#pragma once

// ---------------------------------------------------------------------------
// ui_manager.h – LVGL v8 UI for the Connect3Dp On-Machine Kiosk
//
// Layout (820 × 320, wide landscape):
//
//   ┌──────────────────────────────────────────────────────────────────────┐
//   │  [ STATUS LABEL           (colour-coded bar, full width, 50 px)  ]  │
//   │  Nickname / Machine ID                        [Mark as Idle]         │
//   │  ┌───────────────────────────────────────────────────────────────┐   │
//   │  │ Job: <name>                                                   │   │
//   │  │ ████████████████░░░░░░░░░░░░  45%                            │   │
//   │  │ Remaining: 00:30:00 / 01:30:00                                │   │
//   │  └───────────────────────────────────────────────────────────────┘   │
//   │                                                       ⊕ Connected    │
//   └──────────────────────────────────────────────────────────────────────┘
//
// [Mark as Idle] is only shown when status is Printed or Canceled.
// Button style: black background, white border, white text.
//
// Screen: Rectangle Bar RGB TTL TFT Display – 3.2" 320×820 (820 wide, 320 tall)
//
// Required LVGL fonts – enable in lv_conf.h:
//   #define LV_FONT_MONTSERRAT_12  1
//   #define LV_FONT_MONTSERRAT_14  1
//   #define LV_FONT_MONTSERRAT_16  1
//   #define LV_FONT_MONTSERRAT_20  1
//   #define LV_FONT_MONTSERRAT_28  1
// ---------------------------------------------------------------------------

#include <lvgl.h>
#include "config.h"
#include "machine_state.h"

// WsHandler is defined in ws_handler.h (included before ui_manager.h in OD-1.ino).
class WsHandler;
extern WsHandler wsHandler;

// ---- Widget handles (static → file-local) ----------------------------------
static lv_obj_t* s_scrMain       = nullptr;
static lv_obj_t* s_statusBar     = nullptr;
static lv_obj_t* s_lblStatus     = nullptr;
static lv_obj_t* s_lblNickname   = nullptr;
static lv_obj_t* s_btnMarkIdle   = nullptr;
static lv_obj_t* s_containerJob  = nullptr;
static lv_obj_t* s_lblJobName    = nullptr;
static lv_obj_t* s_barProgress   = nullptr;
static lv_obj_t* s_lblPercent    = nullptr;
static lv_obj_t* s_lblRemaining  = nullptr;
static lv_obj_t* s_lblConnStatus = nullptr;
#ifdef C3DP_DEV_MODE
static lv_obj_t* s_lblDevStats   = nullptr;
#endif

// ---- Colour palette --------------------------------------------------------
static inline lv_color_t _statusColor(MachineStatus s) {
    switch (s) {
        case MachineStatus::Idle:         return lv_color_hex(0x27AE60);  // green
        case MachineStatus::Printing:     return lv_color_hex(0x2980B9);  // blue
        case MachineStatus::Paused:       return lv_color_hex(0xF39C12);  // orange
        case MachineStatus::Printed:      return lv_color_hex(0x8E44AD);  // purple
        case MachineStatus::Canceled:     return lv_color_hex(0xE74C3C);  // red
        case MachineStatus::Connecting:   return lv_color_hex(0x95A5A6);  // grey
        default:                          return lv_color_hex(0x555555);  // dark grey
    }
}

// ---- Button event callback -------------------------------------------------
static void _onMarkIdleClicked(lv_event_t* /*e*/) {
    wsHandler.sendMarkAsIdle();
}

// ---- Build the UI ----------------------------------------------------------

void ui_init() {
    s_scrMain = lv_scr_act();
    lv_obj_set_style_bg_color(s_scrMain, lv_color_black(), LV_PART_MAIN);
    lv_obj_set_style_bg_opa(s_scrMain, LV_OPA_COVER, LV_PART_MAIN);

    // ---- Status bar (top, full width, 50 px tall) --------------------------
    s_statusBar = lv_obj_create(s_scrMain);
    lv_obj_set_size(s_statusBar, SCREEN_WIDTH, 50);
    lv_obj_align(s_statusBar, LV_ALIGN_TOP_MID, 0, 0);
    lv_obj_set_style_radius(s_statusBar, 0, LV_PART_MAIN);
    lv_obj_set_style_border_width(s_statusBar, 0, LV_PART_MAIN);
    lv_obj_set_style_pad_all(s_statusBar, 0, LV_PART_MAIN);
    lv_obj_set_style_bg_color(s_statusBar, _statusColor(MachineStatus::Disconnected), LV_PART_MAIN);

    s_lblStatus = lv_label_create(s_statusBar);
    lv_label_set_text(s_lblStatus, "Disconnected");
    lv_obj_set_style_text_color(s_lblStatus, lv_color_white(), LV_PART_MAIN);
    lv_obj_set_style_text_font(s_lblStatus, &lv_font_montserrat_20, LV_PART_MAIN);
    lv_obj_center(s_lblStatus);

#ifdef C3DP_DEV_MODE
    // ---- Dev stats overlay (top-left of status bar, always visible) --------
    s_lblDevStats = lv_label_create(s_statusBar);
    lv_label_set_text(s_lblDevStats, "Heap: --  Loop: --");
    lv_obj_set_style_text_color(s_lblDevStats, lv_color_white(), LV_PART_MAIN);
    lv_obj_set_style_text_font(s_lblDevStats, &lv_font_montserrat_12, LV_PART_MAIN);
    lv_obj_align(s_lblDevStats, LV_ALIGN_LEFT_MID, 8, 0);
#endif

    // ---- Nickname / Machine ID (below status bar, left) --------------------
    s_lblNickname = lv_label_create(s_scrMain);
    lv_label_set_text(s_lblNickname, C3DP_MACHINE_ID);
    lv_obj_set_style_text_color(s_lblNickname, lv_color_hex(0xBDC3C7), LV_PART_MAIN);
    lv_obj_set_style_text_font(s_lblNickname, &lv_font_montserrat_16, LV_PART_MAIN);
    lv_obj_align(s_lblNickname, LV_ALIGN_TOP_LEFT, 12, 58);

    // ---- Mark as Idle button (below status bar, right; hidden initially) ---
    s_btnMarkIdle = lv_btn_create(s_scrMain);
    lv_obj_set_size(s_btnMarkIdle, 160, 36);
    lv_obj_align(s_btnMarkIdle, LV_ALIGN_TOP_RIGHT, -12, 52);
    lv_obj_set_style_bg_color(s_btnMarkIdle, lv_color_black(), LV_PART_MAIN);
    lv_obj_set_style_bg_color(s_btnMarkIdle, lv_color_black(), LV_STATE_PRESSED);
    lv_obj_set_style_border_color(s_btnMarkIdle, lv_color_white(), LV_PART_MAIN);
    lv_obj_set_style_border_width(s_btnMarkIdle, 2, LV_PART_MAIN);
    lv_obj_set_style_radius(s_btnMarkIdle, 4, LV_PART_MAIN);
    lv_obj_add_event_cb(s_btnMarkIdle, _onMarkIdleClicked, LV_EVENT_CLICKED, nullptr);
    lv_obj_add_flag(s_btnMarkIdle, LV_OBJ_FLAG_HIDDEN);

    lv_obj_t* lblBtn = lv_label_create(s_btnMarkIdle);
    lv_label_set_text(lblBtn, "Mark as Idle");
    lv_obj_set_style_text_color(lblBtn, lv_color_white(), LV_PART_MAIN);
    lv_obj_set_style_text_font(lblBtn, &lv_font_montserrat_14, LV_PART_MAIN);
    lv_obj_center(lblBtn);

    // ---- Job container (hidden until a job is active) ----------------------
    s_containerJob = lv_obj_create(s_scrMain);
    lv_obj_set_size(s_containerJob, SCREEN_WIDTH - 24, 160);
    lv_obj_align(s_containerJob, LV_ALIGN_BOTTOM_MID, 0, -28);
    lv_obj_set_style_bg_color(s_containerJob, lv_color_hex(0x1C1C1C), LV_PART_MAIN);
    lv_obj_set_style_border_width(s_containerJob, 0, LV_PART_MAIN);
    lv_obj_set_style_radius(s_containerJob, 8, LV_PART_MAIN);
    lv_obj_set_style_pad_all(s_containerJob, 10, LV_PART_MAIN);
    lv_obj_add_flag(s_containerJob, LV_OBJ_FLAG_HIDDEN);

    // Job name (scrolling label)
    s_lblJobName = lv_label_create(s_containerJob);
    lv_label_set_long_mode(s_lblJobName, LV_LABEL_LONG_SCROLL_CIRCULAR);
    lv_obj_set_width(s_lblJobName, lv_pct(80));
    lv_label_set_text(s_lblJobName, "");
    lv_obj_set_style_text_color(s_lblJobName, lv_color_white(), LV_PART_MAIN);
    lv_obj_set_style_text_font(s_lblJobName, &lv_font_montserrat_16, LV_PART_MAIN);
    lv_obj_align(s_lblJobName, LV_ALIGN_TOP_LEFT, 0, 0);

    // Progress bar
    s_barProgress = lv_bar_create(s_containerJob);
    lv_obj_set_size(s_barProgress, lv_pct(92), 18);
    lv_obj_align(s_barProgress, LV_ALIGN_TOP_MID, 0, 32);
    lv_bar_set_range(s_barProgress, 0, 100);
    lv_bar_set_value(s_barProgress, 0, LV_ANIM_OFF);
    lv_obj_set_style_bg_color(s_barProgress, lv_color_hex(0x444444), LV_PART_MAIN);
    lv_obj_set_style_bg_color(s_barProgress, lv_color_hex(0x2980B9), LV_PART_INDICATOR);

    // Percentage label (large, centred under bar)
    s_lblPercent = lv_label_create(s_containerJob);
    lv_label_set_text(s_lblPercent, "0%");
    lv_obj_set_style_text_color(s_lblPercent, lv_color_white(), LV_PART_MAIN);
    lv_obj_set_style_text_font(s_lblPercent, &lv_font_montserrat_28, LV_PART_MAIN);
    lv_obj_align(s_lblPercent, LV_ALIGN_TOP_MID, 0, 56);

    // Remaining / total time label
    s_lblRemaining = lv_label_create(s_containerJob);
    lv_label_set_text(s_lblRemaining, "");
    lv_obj_set_style_text_color(s_lblRemaining, lv_color_hex(0x95A5A6), LV_PART_MAIN);
    lv_obj_set_style_text_font(s_lblRemaining, &lv_font_montserrat_14, LV_PART_MAIN);
    lv_obj_align(s_lblRemaining, LV_ALIGN_BOTTOM_LEFT, 0, 0);

    // ---- WebSocket connection status (bottom-right corner) -----------------
    s_lblConnStatus = lv_label_create(s_scrMain);
    lv_label_set_text(s_lblConnStatus, LV_SYMBOL_WIFI " --");
    lv_obj_set_style_text_color(s_lblConnStatus, lv_color_hex(0x7F8C8D), LV_PART_MAIN);
    lv_obj_set_style_text_font(s_lblConnStatus, &lv_font_montserrat_12, LV_PART_MAIN);
    lv_obj_align(s_lblConnStatus, LV_ALIGN_BOTTOM_RIGHT, -8, -4);
}

// ---- Refresh the UI from the latest MachineState --------------------------

void ui_update(const MachineState& ms) {
    if (!s_scrMain) return;

    // WebSocket indicator
    if (ms.wsConnected) {
        lv_label_set_text(s_lblConnStatus, LV_SYMBOL_WIFI " Connected");
        lv_obj_set_style_text_color(s_lblConnStatus, lv_color_hex(0x2ECC71), LV_PART_MAIN);
    } else {
        lv_label_set_text(s_lblConnStatus, LV_SYMBOL_WIFI " No server");
        lv_obj_set_style_text_color(s_lblConnStatus, lv_color_hex(0xE74C3C), LV_PART_MAIN);
    }

    // Status bar
    lv_label_set_text(s_lblStatus, machineStatusLabel(ms.status));
    lv_obj_set_style_bg_color(s_statusBar, _statusColor(ms.status), LV_PART_MAIN);

    // Nickname (fall back to machine ID if not set)
    if (ms.nickname.length() > 0) {
        lv_label_set_text(s_lblNickname, ms.nickname.c_str());
    } else {
        lv_label_set_text(s_lblNickname, C3DP_MACHINE_ID);
    }

    // Mark as Idle button – only when Printed or Canceled
    const bool showMarkIdle =
        ms.status == MachineStatus::Printed ||
        ms.status == MachineStatus::Canceled;

    if (showMarkIdle) {
        lv_obj_clear_flag(s_btnMarkIdle, LV_OBJ_FLAG_HIDDEN);
    } else {
        lv_obj_add_flag(s_btnMarkIdle, LV_OBJ_FLAG_HIDDEN);
    }

    // Show the job panel when there is an active job
    const bool showJob = ms.hasJob && (
        ms.status == MachineStatus::Printing ||
        ms.status == MachineStatus::Paused   ||
        ms.status == MachineStatus::Printed
    );

    if (showJob) {
        lv_obj_clear_flag(s_containerJob, LV_OBJ_FLAG_HIDDEN);

        lv_label_set_text(s_lblJobName, ms.job.name.c_str());
        lv_bar_set_value(s_barProgress, ms.job.percentageComplete, LV_ANIM_ON);

        char pctBuf[8];
        snprintf(pctBuf, sizeof(pctBuf), "%d%%", ms.job.percentageComplete);
        lv_label_set_text(s_lblPercent, pctBuf);

        if (ms.job.remainingTime.length() > 0) {
            String remText = String("Remaining: ") + ms.job.remainingTime;
            if (ms.job.totalTime.length() > 0) {
                remText += String("  /  ") + ms.job.totalTime;
            }
            lv_label_set_text(s_lblRemaining, remText.c_str());
        } else {
            lv_label_set_text(s_lblRemaining, "");
        }
    } else {
        lv_obj_add_flag(s_containerJob, LV_OBJ_FLAG_HIDDEN);
    }
}

// ---- Dev-mode stats refresh (no-op unless C3DP_DEV_MODE is defined) --------

#ifdef C3DP_DEV_MODE
// freeHeap / totalHeap in bytes; loopMs is the last loop()-to-loop() duration.
void ui_update_dev_stats(uint32_t freeHeap, uint32_t totalHeap, uint32_t loopMs) {
    if (!s_lblDevStats) return;
    char buf[40];
    snprintf(buf, sizeof(buf), "Heap: %uK/%uK  Loop: %ums",
             freeHeap / 1024, totalHeap / 1024, loopMs);
    lv_label_set_text(s_lblDevStats, buf);
}
#endif
