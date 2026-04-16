/**
 * Connects to the Host, prints the initial ELEGOO state, then streams all changes.
 * Run: npm run listen
 */

import { Connect3DpClient, StateDetails } from "../src/index.js";

const HOST = "ws://localhost:5193/ws";
const MACHINE_ID = "123";

const client = new Connect3DpClient(HOST, {
  reconnect: { enabled: false },
});

client.on("connected", () => console.log(`[ws] connected → ${HOST}`));
client.on("disconnected", (reason) => console.log(`[ws] disconnected: ${reason}`));
client.on("error", (err) => console.error(`[ws] error:`, err.message));

await client.connect();

// ── All configured printers ───────────────────────────────────────────────────
// Sends: {"Action":"machine/configuration/all","Data":{},"ResponseMessageID":"..."}
const cfgs = await client.getConfigurations();
console.log("\n── machine/configuration/all result ─────────────────────");
console.log(JSON.stringify(cfgs, null, 2));

// ── Subscribe ─────────────────────────────────────────────────────────────────
console.log(`\n── Subscribing to machine "${MACHINE_ID}" ───────────────────`);
const sub = await client.subscribe(MACHINE_ID, StateDetails.Full);

if (!sub.isSuccess) {
  console.error("Subscribe failed:", sub.failureReason);
  client.disconnect();
  process.exit(1);
}

const s = sub.fullState!;
console.log(`  Nickname : ${s.nickname ?? "(none)"}`);
console.log(`  Status   : ${s.status}`);

for (const [i, ext] of Object.entries(s.extruders)) {
  console.log(`  Nozzle[${i}]: ${ext.tempC}°C  (target: ${ext.targetTempC ?? 0}°C)`);
}
for (const [name, el] of Object.entries(s.heatingElements)) {
  console.log(`  ${name.padEnd(8)}: ${el.tempC}°C  (target: ${el.targetTempC}°C)`);
}
if (s.currentJob) {
  const j = s.currentJob;
  console.log(`  Job      : ${j.name} — ${j.percentageComplete.toFixed(1)}%  (${j.remainingTime} left)`);
}

// ── Stream changes ────────────────────────────────────────────────────────────
console.log("\n── Listening for changes (Ctrl+C to stop) ───────────────\n");

client.onMachineState(MACHINE_ID, (_id, data) => {
  const c = data.fullChanges;
  if (!c?.hasChanged) return;

  const t = new Date().toLocaleTimeString();

  if (c.statusHasChanged) {
    console.log(`[${t}] status       ${c.statusPrevious} → ${c.statusNew}`);
  }

  const job = c.currentJobChanges;
  if (job?.hasChanged) {
    if (job.nameHasChanged) {
      console.log(`[${t}] job name     ${job.namePrevious} → ${job.nameNew}`);
    }
    if (job.percentageCompleteHasChanged) {
      console.log(`[${t}] progress     ${job.percentageCompletePrevious?.toFixed(1)}% → ${job.percentageCompleteNew?.toFixed(1)}%`);
    }
    if (job.remainingTimeHasChanged) {
      console.log(`[${t}] remaining    ${job.remainingTimeNew}`);
    }
    if (job.issueHasChanged && job.issueNew) {
      console.log(`[${t}] job issue    [${job.issueNew.severity}] ${job.issueNew.title}: ${job.issueNew.body}`);
    }
  }

  for (const { key, value: ext } of c.extrudersUpdated) {
    if (ext.tempCHasChanged) {
      console.log(`[${t}] nozzle[${key}]   ${ext.tempCPrevious}°C → ${ext.tempCNew}°C`);
    }
  }

  for (const { key, value: el } of c.heatingElementsUpdated) {
    console.log(`[${t}] ${key.padEnd(8)}   → ${el.tempC}°C  (target: ${el.targetTempC}°C)`);
  }

  for (const { value: notif } of c.notificationsAdded) {
    const m = notif.message;
    console.log(`[${t}] notification [${m.severity}] ${m.title}: ${m.body}`);
  }
});

// Keep the process alive — undici's WebSocket does not hold the Node.js event loop open.
await new Promise<void>(() => {});
