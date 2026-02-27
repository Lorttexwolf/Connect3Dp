#pragma once

// ---------------------------------------------------------------------------
// ws_handler.h – WebSocket connection and AtAGlance message parsing
//
// Depends on:
//   - ArduinoWebsockets by Gil Maimon  (Library Manager: "ArduinoWebsockets")
//   - ArduinoJson      by Benoit Blanchon (Library Manager: "ArduinoJson" v6)
//
// WebSocket protocol (Connect3Dp server):
//
//   Client → Server  (subscribe):
//     { "Action": "machine/subscribe",
//       "Data":   { "MachineID": "<id>", "DetailOfState": "AtAGlance" } }
//
//   Server → Client  (initial state, Topic = "machine/subscribe"):
//     { "MessageID": "...", "Time": "...", "Topic": "machine/subscribe",
//       "Data": { "AtAGlanceState": { "Status": "Idle",
//                                     "Capabilities": "Control, Lighting",
//                                     "Nickname": "My Printer",
//                                     "Job": { "Name": "...",
//                                              "PercentageComplete": 45,
//                                              "RemainingTime": "00:30:00",
//                                              "TotalTime": "01:30:00" } } } }
//
//   Server → Client  (state update, Topic = "machine/<id>/state"):
//     { "Time": "...", "Topic": "machine/<id>/state",
//       "Data": { "AtAGlanceChanges": {
//           "StatusHasChanged": <bool>,   "StatusNew": "<str>|null",
//           "CapabilitiesHasChanged": <bool>,
//           "NicknameHasChanged": <bool>, "NicknameNew": "<str>|null",
//           "CurrentJobChanges": {
//               "NameHasChanged":              <bool>,  "NameNew":              "<str>|null",
//               "PercentageCompleteHasChanged": <bool>, "PercentageCompleteNew": <int>|null,
//               "RemainingTimeHasChanged":     <bool>,  "RemainingTimeNew":      "<str>|null",
//               "TotalTimeHasChanged":         <bool>,  "TotalTimeNew":          "<str>|null",
//               "SubStageHasChanged":          <bool>,  "SubStageNew":           "<str>|null"
//           } } } }
// ---------------------------------------------------------------------------

#include <ArduinoWebsockets.h>
#include <ArduinoJson.h>
#include "config.h"
#include "machine_state.h"

using namespace websockets;

// g_machineState is defined in OnMachine.ino and shared across all headers.
extern MachineState g_machineState;

// ---- Internal helpers (static = file-local linkage in a header-only unit) --

// Populate a MachineState from an AtAGlanceMachineState JSON object.
static void _parseAtAGlanceState(JsonObjectConst state, MachineState& ms) {
    const char* statusStr = state["Status"];
    if (statusStr) {
        ms.status = parseMachineStatus(statusStr);
    }

    const char* nick = state["Nickname"];
    ms.nickname = nick ? nick : "";

    JsonVariantConst jobVar = state["Job"];
    if (!jobVar.isNull() && jobVar.is<JsonObject>()) {
        JsonObjectConst job = jobVar.as<JsonObjectConst>();
        ms.hasJob = true;

        const char* name = job["Name"];
        ms.job.name = name ? name : "";

        ms.job.percentageComplete = job["PercentageComplete"] | 0;

        const char* rem = job["RemainingTime"];
        ms.job.remainingTime = rem ? rem : "";

        const char* tot = job["TotalTime"];
        ms.job.totalTime = tot ? tot : "";

        const char* sub = job["SubStage"];
        ms.job.subStage = sub ? sub : "";
    } else {
        ms.hasJob = false;
        ms.job    = PrintJobState{};
    }
}

// Apply an AtAGlanceMachineStateChanges delta to the current MachineState.
static void _applyAtAGlanceChanges(JsonObjectConst changes, MachineState& ms) {
    // --- Status ---
    if (changes["StatusHasChanged"] | false) {
        const char* newStatus = changes["StatusNew"];
        if (newStatus) {
            ms.status = parseMachineStatus(newStatus);
        }
    }

    // --- Nickname ---
    if (changes["NicknameHasChanged"] | false) {
        const char* newNick = changes["NicknameNew"];
        ms.nickname = newNick ? newNick : "";
    }

    // --- CurrentJobChanges (generated PrintJobChanges struct) ---
    JsonVariantConst jobChangesVar = changes["CurrentJobChanges"];
    if (jobChangesVar.isNull() || !jobChangesVar.is<JsonObject>()) {
        return;
    }
    JsonObjectConst jc = jobChangesVar.as<JsonObjectConst>();

    // If the job name changed to non-null, a new job started or continued.
    // If it changed to null, the job ended.
    if (jc["NameHasChanged"] | false) {
        const char* newName = jc["NameNew"];
        if (newName && newName[0] != '\0') {
            ms.hasJob = true;
            ms.job.name = newName;
        } else {
            ms.hasJob = false;
            ms.job    = PrintJobState{};
            return;  // no more job fields to update
        }
    }

    if (!ms.hasJob) return;

    if (jc["PercentageCompleteHasChanged"] | false) {
        ms.job.percentageComplete = jc["PercentageCompleteNew"] | ms.job.percentageComplete;
    }
    if (jc["RemainingTimeHasChanged"] | false) {
        const char* t = jc["RemainingTimeNew"];
        if (t) ms.job.remainingTime = t;
    }
    if (jc["TotalTimeHasChanged"] | false) {
        const char* t = jc["TotalTimeNew"];
        if (t) ms.job.totalTime = t;
    }
    if (jc["SubStageHasChanged"] | false) {
        const char* t = jc["SubStageNew"];
        ms.job.subStage = t ? t : "";
    }
}

// ---- WsHandler class -------------------------------------------------------

class WsHandler {
public:
    WebsocketsClient client;
    bool             connected = false;

    // Call once in setup() to register callbacks.
    void begin() {
        client.onMessage([this](WebsocketsMessage msg) {
            _onMessage(msg.data());
        });

        client.onEvent([this](WebsocketsEvent event, String /*data*/) {
            if (event == WebsocketsEvent::ConnectionOpened) {
                Serial.println("[WS] Connected");
                connected                  = true;
                g_machineState.wsConnected = true;
                _sendSubscribe();
            } else if (event == WebsocketsEvent::ConnectionClosed) {
                Serial.println("[WS] Disconnected");
                connected                  = false;
                g_machineState.wsConnected = false;
                g_machineState.status      = MachineStatus::Disconnected;
            }
        });
    }

    // Attempt to connect.  Returns true if the TCP handshake succeeded.
    bool connect() {
        String url = String("ws://") + C3DP_HOST + ":" + C3DP_PORT + C3DP_PATH;
        Serial.print("[WS] Connecting to ");
        Serial.println(url);
        return client.connect(url);
    }

    // Must be called every loop iteration while connected.
    void poll() {
        client.poll();
    }

private:
    // Send the AtAGlance subscription request.
    void _sendSubscribe() {
        StaticJsonDocument<192> doc;
        doc["Action"]                   = "machine/subscribe";
        JsonObject data                 = doc.createNestedObject("Data");
        data["MachineID"]               = C3DP_MACHINE_ID;
        data["DetailOfState"]           = "AtAGlance";

        String msg;
        serializeJson(doc, msg);
        Serial.print("[WS] Subscribe → ");
        Serial.println(msg);
        client.send(msg);
    }

    // Dispatch an incoming text frame.
    void _onMessage(const String& payload) {
        // 4 KiB is comfortably large for AtAGlance messages.
        DynamicJsonDocument doc(4096);
        DeserializationError err = deserializeJson(doc, payload);
        if (err) {
            Serial.print("[WS] JSON parse error: ");
            Serial.println(err.c_str());
            return;
        }

        const char* topic = doc["Topic"];
        if (!topic) return;

        JsonVariantConst dataVar = doc["Data"];
        if (dataVar.isNull() || !dataVar.is<JsonObject>()) return;
        JsonObjectConst data = dataVar.as<JsonObjectConst>();

        // ---- Subscribe response: Topic == "machine/subscribe" ----
        if (strcmp(topic, "machine/subscribe") == 0) {
            const char* failure = data["FailureReason"];
            if (failure && failure[0] != '\0') {
                Serial.print("[WS] Subscribe failed: ");
                Serial.println(failure);
                return;
            }
            JsonVariantConst stateVar = data["AtAGlanceState"];
            if (!stateVar.isNull() && stateVar.is<JsonObject>()) {
                _parseAtAGlanceState(stateVar.as<JsonObjectConst>(), g_machineState);
                Serial.println("[WS] Initial AtAGlance state applied");
            }
            return;
        }

        // ---- State update broadcast: Topic == "machine/<id>/state" ----
        // Build the expected topic string from the configured machine ID.
        // sizeof(C3DP_MACHINE_ID) already includes the null terminator, so the
        // total buffer = "machine/"(8) + ID(sizeof-1) + "/state"(6) + NUL(1) =
        // sizeof(C3DP_MACHINE_ID) + 14.
        char expectedTopic[sizeof(C3DP_MACHINE_ID) + 14];
        snprintf(expectedTopic, sizeof(expectedTopic),
                 "machine/%s/state", C3DP_MACHINE_ID);
        if (strcmp(topic, expectedTopic) != 0) return;

        JsonVariantConst changesVar = data["AtAGlanceChanges"];
        if (!changesVar.isNull() && changesVar.is<JsonObject>()) {
            _applyAtAGlanceChanges(changesVar.as<JsonObjectConst>(), g_machineState);
            Serial.println("[WS] State delta applied");
        }
    }
};
