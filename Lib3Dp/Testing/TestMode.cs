using Lib3Dp.Connectors;
using Lib3Dp.State;
using System.Text.Json;

namespace Lib3Dp.Testing;

/// <summary>
/// Opt-in test harness that subscribes to <see cref="MachineConnection.OnChanges"/> and logs
/// state diffs to the console. A background key-reading loop routes keypresses to existing
/// public APIs. Default behavior is completely unchanged when Test Mode is off.
/// </summary>
public class TestMode : IAsyncDisposable
{
	private readonly MachineConnection _connection;
	private CancellationTokenSource? _keyCts;
	private Task? _keyLoopTask;
	private string _lastLoggedOutput = string.Empty;

	public bool IsEnabled { get; private set; }

	public TestMode(MachineConnection connection)
	{
		_connection = connection;
	}

	/// <summary>Subscribes to OnChanges and starts the background key loop.</summary>
	public void Enable()
	{
		if (IsEnabled) return;
		IsEnabled = true;
		_connection.OnChanges += OnStateChanged;
		_keyCts = new CancellationTokenSource();
		_keyLoopTask = Task.Run(() => KeyLoop(_keyCts.Token));
	}

	/// <summary>Unsubscribes from OnChanges and stops the key loop.</summary>
	public void Disable()
	{
		if (!IsEnabled) return;
		IsEnabled = false;
		_connection.OnChanges -= OnStateChanged;
		_keyCts?.Cancel();
	}

	private void OnStateChanged(MachineConnection connection, MachineStateChanges changes)
	{
		var formatted = StateChangeDiffLogger.Format(changes);
		if (string.IsNullOrEmpty(formatted)) return;
		// Dedup: skip if output is identical to the last logged line (prevents spam on identical repeats)
		if (formatted == _lastLoggedOutput) return;
		_lastLoggedOutput = formatted;
		Console.WriteLine(formatted);
	}

	/// <summary>Dumps the full machine state as a JSON object to the console without mutating anything.</summary>
	public void LogFullState()
	{
		var json = JsonSerializer.Serialize(_connection.State, new JsonSerializerOptions { WriteIndented = true });
		Console.WriteLine($"=== FULL STATE JSON [{DateTime.Now:HH:mm:ss}] ===");
		Console.WriteLine(json);
		Console.WriteLine("=== END STATE JSON ===");
	}

	private async Task KeyLoop(CancellationToken ct)
	{
		while (!ct.IsCancellationRequested)
		{
			try
			{
				if (Console.KeyAvailable)
				{
					var key = Console.ReadKey(true);
					await HandleKey(key.Key);
				}
				await Task.Delay(50, ct);
			}
			catch (OperationCanceledException)
			{
				break;
			}
			catch (InvalidOperationException)
			{
				// Console not attached or stdin is redirected — stop the key loop gracefully
				break;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[TestMode] Key loop error: {ex.Message}");
			}
		}
	}

	private async Task HandleKey(ConsoleKey key)
	{
		try
		{
			switch (key)
			{
				case ConsoleKey.S:
					Console.WriteLine("[TestMode] → Stop");
					var stopResult = await _connection.Stop();
					if (!stopResult.Success)
						Console.WriteLine($"[TestMode] Stop failed: {stopResult.Reasoning?.Body}");
					break;

				case ConsoleKey.P:
					Console.WriteLine("[TestMode] → Pause");
					var pauseResult = await _connection.Pause();
					if (!pauseResult.Success)
						Console.WriteLine($"[TestMode] Pause failed: {pauseResult.Reasoning?.Body}");
					break;

				case ConsoleKey.R:
					Console.WriteLine("[TestMode] → Resume");
					var resumeResult = await _connection.Resume();
					if (!resumeResult.Success)
						Console.WriteLine($"[TestMode] Resume failed: {resumeResult.Reasoning?.Body}");
					break;

				case ConsoleKey.L:
					Console.WriteLine("[TestMode] → Toggle Light");
					var lights = _connection.State.Lights;
					if (lights.Count == 0)
					{
						Console.WriteLine("[TestMode] No lights available");
						break;
					}
					bool anyOn = lights.Values.Any(v => v);
					foreach (var fixtureName in lights.Keys.ToList())
					{
						var lightResult = await _connection.ToggleLight(fixtureName, !anyOn);
						if (!lightResult.Success)
							Console.WriteLine($"[TestMode] ToggleLight({fixtureName}) failed: {lightResult.Reasoning?.Body}");
					}
					break;

				case ConsoleKey.D:
					LogFullState();
					break;
			}
		}
		catch (NotImplementedException)
		{
			Console.WriteLine($"[TestMode] Key '{key}': operation not implemented on this connector");
		}
		catch (NotSupportedException ex)
		{
			Console.WriteLine($"[TestMode] Key '{key}': not supported — {ex.Message}");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"[TestMode] Key '{key}' error: {ex.Message}");
		}
	}

	public async ValueTask DisposeAsync()
	{
		Disable();
		if (_keyLoopTask != null)
		{
			try { await _keyLoopTask; }
			catch { }
		}
		_keyCts?.Dispose();
	}
}
