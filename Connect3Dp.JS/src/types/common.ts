/**
 * Envelope for every message sent FROM the server TO the client.
 * Property names are camelCase — the server applies JsonNamingPolicy.CamelCase.
 */
export interface MessageToClient<T> {
  /** Correlation ID — set when this message is a reply to a client request; null for broadcasts. */
  messageID: string | null;
  /** ISO 8601 timestamp of when the server emitted this message. */
  time: string;
  /** Topic string identifying the message type (e.g. "machine/abc/state", "logs"). */
  topic: string;
  /** The payload. Shape depends on topic. */
  data: T;
}

/**
 * Envelope for every message sent FROM the client TO the server.
 * The server reads "Action", "ResponseMessageID", and "Data" case-sensitively via JsonDocument,
 * so these envelope keys MUST remain PascalCase exactly as shown.
 * Only the data payload inside Data is deserialized case-insensitively.
 */
export interface MessageToServer<T> {
  Action: string;
  ResponseMessageID: string | null;
  Data: T;
}

/** All known WebSocket topics/action strings, mirroring Connect3DpWebSocketExtensions.Topics. */
export const Topics = {
  Machine: {
    Subscribe: "machine/subscribe",
    Unsubscribe: "machine/unsubscribe",
    MarkAsIdle: "machine/markAsIdle",
    Pause: "machine/pause",
    Resume: "machine/resume",
    Stop: "machine/stop",
    FindMatchingSpools: "machine/findMatchingSpools",
    StateUpdated: (machineId: string) => `machine/${machineId}/state`,
    Configurations: {
      All: "machine/configuration/all",
    },
  },
  Logging: {
    Subscribe: "log/subscribe",
    Unsubscribe: "log/unsubscribe",
    History: "log/history",
    Logs: "logs",
  },
  MachineFileStore: {
    TotalUsage: "machineFileStore/totalUsage",
    MachineUsage: "machineFileStore/machineUsage",
  },
} as const;
