#pragma once
#include <Arduino.h>

// ---------------------------------------------------------------------------
// MachineStatus
// Mirrors the server-side MachineStatus [Flags] enum (MachineStatus.cs).
// The server serialises it with JsonStringEnumConverter, so values arrive as
// strings such as "Idle", "Printing", or combined flags "Printing, Paused".
// ---------------------------------------------------------------------------
enum class MachineStatus : uint8_t {
    Unknown,
    Disconnected,
    Connecting,
    Idle,
    Printing,
    Printed,
    Paused,
    Canceled
};

// ---------------------------------------------------------------------------
// PrintJobState – subset of IMachinePrintJob used by the AtAGlance view
// ---------------------------------------------------------------------------
struct PrintJobState {
    String name;
    int    percentageComplete = 0;
    // TimeSpan values are serialised by .NET as "[-][d.]hh:mm:ss[.fffffff]"
    // e.g. "00:30:00" or "1.02:30:00". Stored as raw strings for display.
    String remainingTime;
    String totalTime;
    String subStage;
};

// ---------------------------------------------------------------------------
// MachineState – top-level application state updated by ws_handler
// ---------------------------------------------------------------------------
struct MachineState {
    bool          wsConnected = false;
    MachineStatus status      = MachineStatus::Disconnected;
    String        nickname;
    bool          hasJob      = false;
    PrintJobState job;
};

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

// Parse a status string from the server.  Because MachineStatus is a [Flags]
// enum, combined values like "Printing, Paused" are possible; we return the
// most "active" single status for display purposes.
inline MachineStatus parseMachineStatus(const char* s) {
    if (!s || s[0] == '\0') return MachineStatus::Unknown;
    if (strstr(s, "Printing"))     return MachineStatus::Printing;
    if (strstr(s, "Paused"))       return MachineStatus::Paused;
    if (strstr(s, "Printed"))      return MachineStatus::Printed;
    if (strstr(s, "Canceled"))     return MachineStatus::Canceled;
    if (strstr(s, "Idle"))         return MachineStatus::Idle;
    if (strstr(s, "Connecting"))   return MachineStatus::Connecting;
    if (strstr(s, "Disconnected")) return MachineStatus::Disconnected;
    return MachineStatus::Unknown;
}

inline const char* machineStatusLabel(MachineStatus s) {
    switch (s) {
        case MachineStatus::Disconnected: return "Disconnected";
        case MachineStatus::Connecting:   return "Connecting...";
        case MachineStatus::Idle:         return "Idle";
        case MachineStatus::Printing:     return "Printing";
        case MachineStatus::Printed:      return "Print Complete";
        case MachineStatus::Paused:       return "Paused";
        case MachineStatus::Canceled:     return "Canceled";
        default:                          return "Unknown";
    }
}
