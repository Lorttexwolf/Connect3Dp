// ---------------------------------------------------------------------------
// OD-1 PC Simulator  (LVGL v8 + SDL2)
//
// Build:
//   cd Kiosks/OD-1/simulator
//   cmake -B build -DCMAKE_BUILD_TYPE=Release
//   cmake --build build
//   ./build/OD1Simulator        # Linux / macOS
//   build\Release\OD1Simulator  # Windows
//
// The window cycles through every MachineStatus variant every 3 seconds so
// the full UI can be reviewed without any hardware or server.
// ---------------------------------------------------------------------------

#include <cstdio>
#include <cstring>
#include <cstdlib>
#include <string>

// ── Minimal Arduino String shim ──────────────────────────────────────────────
// machine_state.h and ui_manager.h rely on Arduino's String class.  This thin
// wrapper over std::string provides exactly the interface those headers need.

class String {
    std::string _s;
public:
    String() = default;
    String(const char* s)  : _s(s ? s : "") {}
    explicit String(std::string s) : _s(std::move(s)) {}
    explicit String(int v) { char b[16]; snprintf(b, sizeof(b), "%d", v); _s = b; }

    size_t      length() const { return _s.size(); }
    const char* c_str()  const { return _s.c_str(); }

    String& operator=(const char* s)       { _s = s ? s : ""; return *this; }
    String& operator+=(const String& o)    { _s += o._s;       return *this; }
    String& operator+=(const char* s)      { _s += s ? s : ""; return *this; }
    String  operator+(const String& o) const { return String(_s + o._s); }
    String  operator+(const char*   s) const { return String(_s + (s ? s : "")); }

    bool operator==(const String& o) const { return _s == o._s; }
    bool operator==(const char*   s) const { return _s == (s ? s : ""); }
    bool operator!=(const String& o) const { return !(*this == o); }
    bool operator!=(const char*   s) const { return !(*this == s); }
};

inline String operator+(const char* a, const String& b) {
    return String(std::string(a ? a : "") + b.c_str());
}

// ── Kiosk headers ─────────────────────────────────────────────────────────────
#include "../config.h"
#include "../machine_state.h"

// Stub WsHandler — ui_manager.h declares an extern and calls sendMarkAsIdle()
// when the "Mark as Idle" button is tapped.  The simulator just ignores it.
class WsHandler {
public:
    void sendMarkAsIdle() {}
};
WsHandler wsHandler;

// Include the UI implementation (depends on String, config.h, machine_state.h,
// and the WsHandler stub declared above).
#include "../ui_manager.h"

// ── SDL2 ──────────────────────────────────────────────────────────────────────
#include <SDL2/SDL.h>

static SDL_Window*   g_window   = nullptr;
static SDL_Renderer* g_renderer = nullptr;
static SDL_Texture*  g_texture  = nullptr;

// LVGL display flush callback — accumulates dirty rectangles into the SDL
// texture and presents the complete frame only after the last flush call.
static void sdl_flush_cb(lv_disp_drv_t* drv, const lv_area_t* area, lv_color_t* px_map) {
    SDL_Rect r = {
        (int)area->x1,
        (int)area->y1,
        (int)(area->x2 - area->x1 + 1),
        (int)(area->y2 - area->y1 + 1)
    };
    SDL_UpdateTexture(g_texture, &r, px_map, r.w * (int)sizeof(lv_color_t));

    // Present only after all dirty areas for the current frame have been flushed.
    if (lv_disp_flush_is_last(drv)) {
        SDL_RenderCopy(g_renderer, g_texture, nullptr, nullptr);
        SDL_RenderPresent(g_renderer);
    }

    lv_disp_flush_ready(drv);
}

// LVGL pointer-input read callback — forwards SDL mouse state.
static void sdl_mouse_read_cb(lv_indev_drv_t* /*drv*/, lv_indev_data_t* data) {
    int mx = 0, my = 0;
    Uint32 btn = SDL_GetMouseState(&mx, &my);
    data->point.x = (lv_coord_t)mx;
    data->point.y = (lv_coord_t)my;
    data->state   = (btn & SDL_BUTTON(SDL_BUTTON_LEFT))
                    ? LV_INDEV_STATE_PRESSED
                    : LV_INDEV_STATE_RELEASED;
}

// ── Demo states ────────────────────────────────────────────────────────────────
// Cycles through every MachineStatus so every UI branch can be inspected.

static MachineState demo_states[] = {
    // Disconnected (no server connection yet)
    { false, MachineStatus::Disconnected, "",            false, {} },
    // Connecting
    { false, MachineStatus::Connecting,   "My Printer",  false, {} },
    // Idle
    { true,  MachineStatus::Idle,         "My Printer",  false, {} },
    // Printing – job panel visible, 45 %
    { true,  MachineStatus::Printing,     "My Printer",  true,
        { "benchy.3mf", 45, "00:45:00", "01:30:00", "" } },
    // Paused – job panel visible, 32 %
    { true,  MachineStatus::Paused,       "My Printer",  true,
        { "benchy.3mf", 32, "01:01:00", "01:30:00", "" } },
    // Printed – job panel visible + Mark-as-Idle button shown
    { true,  MachineStatus::Printed,      "My Printer",  true,
        { "benchy.3mf", 100, "00:00:00", "01:30:00", "" } },
    // Canceled – Mark-as-Idle button shown, no job panel
    { true,  MachineStatus::Canceled,     "My Printer",  false, {} },
};

static const int DEMO_STATE_COUNT =
    (int)(sizeof(demo_states) / sizeof(demo_states[0]));

// Milliseconds each state is shown before advancing to the next.
static const uint32_t STATE_DWELL_MS = 3000;

// ── Entry point ───────────────────────────────────────────────────────────────
int main(int /*argc*/, char** /*argv*/) {

    // ---- SDL2 init -----------------------------------------------------------
    if (SDL_Init(SDL_INIT_VIDEO | SDL_INIT_TIMER | SDL_INIT_EVENTS) != 0) {
        fprintf(stderr, "SDL_Init error: %s\n", SDL_GetError());
        return 1;
    }

    g_window = SDL_CreateWindow(
        "Connect3Dp OD-1 Simulator",
        SDL_WINDOWPOS_CENTERED, SDL_WINDOWPOS_CENTERED,
        SCREEN_WIDTH, SCREEN_HEIGHT,
        SDL_WINDOW_SHOWN);
    if (!g_window) {
        fprintf(stderr, "SDL_CreateWindow error: %s\n", SDL_GetError());
        SDL_Quit();
        return 1;
    }

    g_renderer = SDL_CreateRenderer(
        g_window, -1,
        SDL_RENDERER_ACCELERATED | SDL_RENDERER_PRESENTVSYNC);
    if (!g_renderer)
        g_renderer = SDL_CreateRenderer(g_window, -1, 0);

    g_texture = SDL_CreateTexture(
        g_renderer,
        SDL_PIXELFORMAT_ARGB8888,
        SDL_TEXTUREACCESS_STREAMING,
        SCREEN_WIDTH, SCREEN_HEIGHT);

    // ---- LVGL init -----------------------------------------------------------
    lv_init();

    // Display buffer (40 lines of the screen width)
    static lv_disp_draw_buf_t draw_buf;
    static lv_color_t         buf[SCREEN_WIDTH * 40];
    lv_disp_draw_buf_init(&draw_buf, buf, nullptr, SCREEN_WIDTH * 40);

    static lv_disp_drv_t disp_drv;
    lv_disp_drv_init(&disp_drv);
    disp_drv.draw_buf = &draw_buf;
    disp_drv.flush_cb = sdl_flush_cb;
    disp_drv.hor_res  = SCREEN_WIDTH;
    disp_drv.ver_res  = SCREEN_HEIGHT;
    lv_disp_drv_register(&disp_drv);

    static lv_indev_drv_t indev_drv;
    lv_indev_drv_init(&indev_drv);
    indev_drv.type    = LV_INDEV_TYPE_POINTER;
    indev_drv.read_cb = sdl_mouse_read_cb;
    lv_indev_drv_register(&indev_drv);

    // ---- Build UI and show first state --------------------------------------
    ui_init();
    ui_update(demo_states[0]);

    // ---- Main loop -----------------------------------------------------------
    int      stateIdx   = 0;
    uint32_t lastSwitch = SDL_GetTicks();

    bool running = true;
    while (running) {
        // Handle SDL events (window close, keyboard)
        SDL_Event ev;
        while (SDL_PollEvent(&ev)) {
            if (ev.type == SDL_QUIT)
                running = false;
            if (ev.type == SDL_KEYDOWN && ev.key.keysym.sym == SDLK_ESCAPE)
                running = false;
        }

        // Advance to the next demo state after the dwell period
        uint32_t now = SDL_GetTicks();
        if (now - lastSwitch >= STATE_DWELL_MS) {
            lastSwitch = now;
            stateIdx   = (stateIdx + 1) % DEMO_STATE_COUNT;
            ui_update(demo_states[stateIdx]);
        }

        lv_timer_handler();
        SDL_Delay(5);
    }

    // ---- Cleanup -------------------------------------------------------------
    SDL_DestroyTexture(g_texture);
    SDL_DestroyRenderer(g_renderer);
    SDL_DestroyWindow(g_window);
    SDL_Quit();
    return 0;
}
