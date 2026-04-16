import type { MessageToClient, MessageToServer } from "./types/common.js";
import { Topics } from "./types/common.js";
import type { BroadcastedMachineStateUpdateData } from "./types/changes.js";
import type {
  StateDetails,
  SubscribeActionResult,
  WebSocketClientActionResult,
  ClientMessageMachineOperationResult,
} from "./types/actions.js";
import type { LogEntry, LogHistoryParams, LogHistoryResult } from "./types/logging.js";
import type {
  MachineFileStoreTotalUsageResult,
  MachineFileStoreMachineUsageResult,
} from "./types/file-store.js";
import type { AllMachineConfigurationsResult } from "./types/configuration.js";

// ---------------------------------------------------------------------------
// Options
// ---------------------------------------------------------------------------

export interface ReconnectOptions {
  /** Enable automatic reconnection. Default: true. */
  enabled?: boolean;
  /** Delay before first reconnect attempt in ms. Default: 1000. */
  initialDelayMs?: number;
  /** Maximum reconnect delay after backoff. Default: 30000. */
  maxDelayMs?: number;
  /** Maximum number of reconnect attempts. Default: Infinity. */
  maxAttempts?: number;
}

export interface Connect3DpClientOptions {
  /**
   * Timeout for subscribe/query requests in ms.
   * Default: 10000.
   */
  requestTimeoutMs?: number;
  /**
   * Timeout for control operations (pause/resume/stop/markAsIdle) in ms.
   * Print commands can take up to 30 seconds before the machine acknowledges.
   * Default: 35000.
   */
  controlRequestTimeoutMs?: number;
  reconnect?: ReconnectOptions;
}

// ---------------------------------------------------------------------------
// Event map
// ---------------------------------------------------------------------------

export type Connect3DpEventMap = {
  /** WebSocket opened successfully. */
  connected: [];
  /** WebSocket closed — either intentionally or due to an error. */
  disconnected: [reason: string];
  /** Attempting to reconnect after an unexpected disconnect. */
  reconnecting: [attempt: number, delayMs: number];
  /** Successfully reconnected and re-subscribed to all machines. */
  reconnected: [];
  /** Any error emitted by the WebSocket or the library internals. */
  error: [error: Error];
  /** Every raw inbound message before topic dispatch. */
  message: [msg: MessageToClient<unknown>];
};

type EventHandler<K extends keyof Connect3DpEventMap> = (
  ...args: Connect3DpEventMap[K]
) => void;

// ---------------------------------------------------------------------------
// Internal helpers
// ---------------------------------------------------------------------------

function generateId(): string {
  if (typeof crypto !== "undefined" && typeof crypto.randomUUID === "function") {
    return crypto.randomUUID();
  }
  // Fallback for environments without crypto.randomUUID (older React Native)
  return `${Date.now().toString(36)}-${Math.random().toString(36).slice(2)}-${Math.random().toString(36).slice(2)}`;
}

function sleep(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

// ---------------------------------------------------------------------------
// Connect3DpClient
// ---------------------------------------------------------------------------

export class Connect3DpClient {
  private readonly _url: string;
  private readonly _opts: Required<Omit<Connect3DpClientOptions, "reconnect">> & {
    reconnect: Required<ReconnectOptions>;
  };

  private _ws: WebSocket | null = null;
  private _intentionalClose = false;

  /** Pending request map: correlationId → { resolve, reject, timer } */
  private readonly _pending = new Map<
    string,
    { resolve: (msg: MessageToClient<unknown>) => void; reject: (err: Error) => void; timer: ReturnType<typeof setTimeout> }
  >();

  /** Topic → set of handlers for broadcast topics. */
  private readonly _topicHandlers = new Map<string, Set<(msg: MessageToClient<unknown>) => void>>();

  /** Typed event emitter storage. */
  private readonly _eventHandlers = new Map<
    keyof Connect3DpEventMap,
    Set<(...args: unknown[]) => void>
  >();

  /** Active machine subscriptions to restore on reconnect. */
  private readonly _subscriptions = new Map<string, StateDetails>();

  constructor(url: string, options: Connect3DpClientOptions = {}) {
    this._url = url;
    this._opts = {
      requestTimeoutMs: options.requestTimeoutMs ?? 10_000,
      controlRequestTimeoutMs: options.controlRequestTimeoutMs ?? 35_000,
      reconnect: {
        enabled: options.reconnect?.enabled ?? true,
        initialDelayMs: options.reconnect?.initialDelayMs ?? 1_000,
        maxDelayMs: options.reconnect?.maxDelayMs ?? 30_000,
        maxAttempts: options.reconnect?.maxAttempts ?? Infinity,
      },
    };
  }

  // -------------------------------------------------------------------------
  // Lifecycle
  // -------------------------------------------------------------------------

  /** Opens the WebSocket connection. Resolves when the socket is open. */
  connect(): Promise<void> {
    this._intentionalClose = false;
    return this._openSocket();
  }

  /** Closes the connection and stops reconnection. */
  disconnect(): void {
    this._intentionalClose = true;
    this._ws?.close(1000, "Client disconnected");
    this._ws = null;
    this._rejectAllPending(new Error("Client disconnected"));
  }

  // -------------------------------------------------------------------------
  // Event emitter
  // -------------------------------------------------------------------------

  on<K extends keyof Connect3DpEventMap>(
    event: K,
    handler: EventHandler<K>
  ): () => void {
    if (!this._eventHandlers.has(event)) {
      this._eventHandlers.set(event, new Set());
    }
    this._eventHandlers.get(event)!.add(handler as (...args: unknown[]) => void);
    return () => this._eventHandlers.get(event)?.delete(handler as (...args: unknown[]) => void);
  }

  private _emit<K extends keyof Connect3DpEventMap>(
    event: K,
    ...args: Connect3DpEventMap[K]
  ): void {
    this._eventHandlers.get(event)?.forEach((h) => h(...args));
  }

  // -------------------------------------------------------------------------
  // Machine subscriptions
  // -------------------------------------------------------------------------

  /**
   * Subscribe to a machine's state updates.
   * The returned result contains the full initial state in the requested detail level.
   */
  async subscribe(machineId: string, detail: StateDetails): Promise<SubscribeActionResult> {
    const result = await this._request<SubscribeActionResult>(
      Topics.Machine.Subscribe,
      { MachineID: machineId, DetailOfState: detail }
    );
    if (result.isSuccess) {
      this._subscriptions.set(machineId, detail);
    }
    return result;
  }

  /** Unsubscribe from a machine's state updates. */
  async unsubscribe(machineId: string): Promise<WebSocketClientActionResult> {
    const result = await this._request<WebSocketClientActionResult>(
      Topics.Machine.Unsubscribe,
      { MachineID: machineId }
    );
    if (result.isSuccess) {
      this._subscriptions.delete(machineId);
    }
    return result;
  }

  /**
   * Listen for state update broadcasts for a specific machine.
   * Returns an unsubscribe function.
   *
   * Note: you must call `subscribe()` first to start receiving updates.
   */
  onMachineState(
    machineId: string,
    handler: (machineId: string, data: BroadcastedMachineStateUpdateData) => void
  ): () => void {
    const topic = Topics.Machine.StateUpdated(machineId);
    return this._onTopic(topic, (msg) => {
      handler(machineId, msg.data as BroadcastedMachineStateUpdateData);
    });
  }

  // -------------------------------------------------------------------------
  // Machine control
  // -------------------------------------------------------------------------

  pause(machineId: string): Promise<ClientMessageMachineOperationResult> {
    return this._request(Topics.Machine.Pause, { MachineID: machineId }, this._opts.controlRequestTimeoutMs);
  }

  resume(machineId: string): Promise<ClientMessageMachineOperationResult> {
    return this._request(Topics.Machine.Resume, { MachineID: machineId }, this._opts.controlRequestTimeoutMs);
  }

  stop(machineId: string): Promise<ClientMessageMachineOperationResult> {
    return this._request(Topics.Machine.Stop, { MachineID: machineId }, this._opts.controlRequestTimeoutMs);
  }

  markAsIdle(machineId: string): Promise<ClientMessageMachineOperationResult> {
    return this._request(Topics.Machine.MarkAsIdle, { MachineID: machineId }, this._opts.controlRequestTimeoutMs);
  }

  /**
   * Turn a light fixture on or off. `fixtureName` must match a key in {@link IMachineState.lights}
   * (e.g. `"Chamber"` on many ELEGOO printers).
   */
  toggleLight(
    machineId: string,
    fixtureName: string,
    isOn: boolean
  ): Promise<ClientMessageMachineOperationResult> {
    return this._request(
      Topics.Machine.ToggleLight,
      { MachineID: machineId, FixtureName: fixtureName, IsOn: isOn },
      this._opts.controlRequestTimeoutMs
    );
  }

  /**
   * Set a fan speed to 0–100. `fanName` must match a key in {@link IMachineState.fans}
   * (e.g. `"ModelFan"`, `"AuxiliaryFan"`, `"BoxFan"` on ELEGOO).
   */
  setFanSpeed(
    machineId: string,
    fanName: string,
    speedPercent: number
  ): Promise<ClientMessageMachineOperationResult> {
    return this._request(
      Topics.Machine.SetFanSpeed,
      { MachineID: machineId, FanName: fanName, SpeedPercent: speedPercent },
      this._opts.controlRequestTimeoutMs
    );
  }

  // -------------------------------------------------------------------------
  // Log streaming
  // -------------------------------------------------------------------------

  /**
   * Subscribe to log broadcasts.
   * @param minIntervalMs Minimum milliseconds between batched log broadcasts.
   */
  async subscribeLogs(minIntervalMs: number): Promise<WebSocketClientActionResult> {
    const duration = msToDuration(minIntervalMs);
    return this._request(Topics.Logging.Subscribe, { MinInterval: duration });
  }

  unsubscribeLogs(): Promise<WebSocketClientActionResult> {
    return this._request(Topics.Logging.Unsubscribe, {});
  }

  getLogHistory(params: LogHistoryParams): Promise<LogHistoryResult> {
    return this._request(Topics.Logging.History, {
      Max: params.Max,
      Before: params.Before ?? null,
      After: params.After ?? null,
      MinLevel: params.MinLevel,
    });
  }

  /**
   * Listen for incoming log broadcast batches.
   * Returns an unsubscribe function.
   *
   * Note: call `subscribeLogs()` first.
   */
  onLogs(handler: (entries: LogEntry[]) => void): () => void {
    return this._onTopic(Topics.Logging.Logs, (msg) => {
      handler(msg.data as LogEntry[]);
    });
  }

  // -------------------------------------------------------------------------
  // File store
  // -------------------------------------------------------------------------

  getTotalFileStoreUsage(): Promise<MachineFileStoreTotalUsageResult> {
    return this._request(Topics.MachineFileStore.TotalUsage, {});
  }

  getMachineFileStoreUsage(machineId: string): Promise<MachineFileStoreMachineUsageResult> {
    return this._request(Topics.MachineFileStore.MachineUsage, { MachineID: machineId });
  }

  /**
   * List machine configurations known to the Connect3dp server (e.g. discovered on the LAN).
   * Requires an open WebSocket; connect with {@link connect} first.
   */
  getAllMachineConfigurations(): Promise<AllMachineConfigurationsResult> {
    return this._request(Topics.Machine.Configurations.All, {});
  }

  // -------------------------------------------------------------------------
  // Internal: socket management
  // -------------------------------------------------------------------------

  private _openSocket(): Promise<void> {
    return new Promise((resolve, reject) => {
      try {
        const ws = new WebSocket(this._url);
        this._ws = ws;

        ws.onopen = () => {
          this._emit("connected");
          resolve();
        };

        ws.onmessage = (event) => {
          this._handleMessage(event.data as string);
        };

        ws.onerror = (event) => {
          const err = new Error("WebSocket error");
          this._emit("error", err);
          reject(err);
        };

        ws.onclose = (event) => {
          const reason = event.reason || `Code ${event.code}`;
          this._emit("disconnected", reason);
          this._rejectAllPending(new Error(`WebSocket closed: ${reason}`));

          if (!this._intentionalClose && this._opts.reconnect.enabled) {
            this._reconnectLoop().catch(() => {});
          }
        };
      } catch (err) {
        reject(err instanceof Error ? err : new Error(String(err)));
      }
    });
  }

  private async _reconnectLoop(): Promise<void> {
    const { initialDelayMs, maxDelayMs, maxAttempts } = this._opts.reconnect;
    let attempt = 0;
    let delay = initialDelayMs;

    while (attempt < maxAttempts && !this._intentionalClose) {
      attempt++;
      // Add up to 20% jitter to avoid thundering herd
      const jitter = delay * 0.2 * Math.random();
      const actualDelay = Math.min(delay + jitter, maxDelayMs);
      this._emit("reconnecting", attempt, Math.round(actualDelay));
      await sleep(actualDelay);

      if (this._intentionalClose) break;

      try {
        await this._openSocket();
        // Restore all active subscriptions
        for (const [machineId, detail] of this._subscriptions) {
          try {
            await this._request<SubscribeActionResult>(
              Topics.Machine.Subscribe,
              { MachineID: machineId, DetailOfState: detail }
            );
          } catch {
            // Best-effort resubscription — don't fail the whole reconnect
          }
        }
        this._emit("reconnected");
        return;
      } catch {
        // Keep trying
      }

      // Exponential backoff, capped at maxDelayMs
      delay = Math.min(delay * 2, maxDelayMs);
    }
  }

  // -------------------------------------------------------------------------
  // Internal: message handling
  // -------------------------------------------------------------------------

  private _handleMessage(raw: string): void {
    let msg: MessageToClient<unknown>;
    try {
      msg = JSON.parse(raw) as MessageToClient<unknown>;
    } catch {
      this._emit("error", new Error(`Failed to parse message: ${raw}`));
      return;
    }

    this._emit("message", msg);

    // Correlated response
    if (msg.messageID != null) {
      const pending = this._pending.get(msg.messageID);
      if (pending) {
        clearTimeout(pending.timer);
        this._pending.delete(msg.messageID);
        pending.resolve(msg);
        return;
      }
    }

    // Broadcast dispatch
    const handlers = this._topicHandlers.get(msg.topic);
    if (handlers) {
      handlers.forEach((h) => h(msg));
    }
  }

  private _onTopic(
    topic: string,
    handler: (msg: MessageToClient<unknown>) => void
  ): () => void {
    if (!this._topicHandlers.has(topic)) {
      this._topicHandlers.set(topic, new Set());
    }
    this._topicHandlers.get(topic)!.add(handler);
    return () => this._topicHandlers.get(topic)?.delete(handler);
  }

  // -------------------------------------------------------------------------
  // Internal: request/response correlation
  // -------------------------------------------------------------------------

  private _request<TResult>(
    action: string,
    data: unknown,
    timeoutMs: number = this._opts.requestTimeoutMs
  ): Promise<TResult> {
    return new Promise((resolve, reject) => {
      if (!this._ws || this._ws.readyState !== WebSocket.OPEN) {
        reject(new Error(`Cannot send "${action}": WebSocket is not open`));
        return;
      }

      const correlationId = generateId();

      const timer = setTimeout(() => {
        this._pending.delete(correlationId);
        reject(new Error(`Request "${action}" timed out after ${timeoutMs}ms`));
      }, timeoutMs);

      this._pending.set(correlationId, {
        resolve: (msg) => resolve(msg.data as TResult),
        reject,
        timer,
      });

      const envelope: MessageToServer<unknown> = {
        Action: action,
        ResponseMessageID: correlationId,
        Data: data,
      };

      try {
        this._ws.send(JSON.stringify(envelope));
      } catch (err) {
        clearTimeout(timer);
        this._pending.delete(correlationId);
        reject(err instanceof Error ? err : new Error(String(err)));
      }
    });
  }

  private _rejectAllPending(err: Error): void {
    for (const [id, { reject, timer }] of this._pending) {
      clearTimeout(timer);
      reject(err);
    }
    this._pending.clear();
  }
}

// ---------------------------------------------------------------------------
// Utility: convert milliseconds to ISO 8601 duration
// ---------------------------------------------------------------------------

function msToDuration(ms: number): string {
  if (ms <= 0) return "PT0S";

  const totalSeconds = Math.floor(ms / 1000);
  const hours = Math.floor(totalSeconds / 3600);
  const minutes = Math.floor((totalSeconds % 3600) / 60);
  const seconds = totalSeconds % 60;

  let result = "PT";
  if (hours > 0) result += `${hours}H`;
  if (minutes > 0) result += `${minutes}M`;
  if (seconds > 0 || result === "PT") result += `${seconds}S`;
  return result;
}
