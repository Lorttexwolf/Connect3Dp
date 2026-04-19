using Lib3Dp;
using Lib3Dp.Cameras;
using Lib3Dp.Connectors;
using Lib3Dp.State;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Connect3Dp.Relays.MediaMTX
{
	/// <summary>
	/// Hosted service that keeps MediaMTX paths in sync with the live set of machine connections.
	/// Watches <see cref="MachineConnectionCollection"/> and per-connection status changes; on connect
	/// it registers one MediaMTX path per quality preset and populates <see cref="MachineState.StreamingURLs"/>,
	/// on disconnect it tears all paths (and any in-process publishers) down.
	/// </summary>
	public class MediaMTXCameraCoordinator : IHostedService
	{
		private readonly MachineConnectionCollection Machines;
		private readonly IMediaMTXRelay Relay;
		private readonly ILogger<MediaMTXCameraCoordinator> Logger;

		private readonly ConcurrentDictionary<string, CameraSession> Active = new();
		private readonly CancellationTokenSource _serviceCts = new();

		public MediaMTXCameraCoordinator(MachineConnectionCollection machines, IMediaMTXRelay relay, ILogger<MediaMTXCameraCoordinator> logger)
		{
			Machines = machines;
			Relay = relay;
			Logger = logger;
		}

		public Task StartAsync(CancellationToken cancellationToken)
		{
			Machines.OnMachineAdded += OnMachineAdded;
			Machines.OnMachineRemoved += OnMachineRemoved;
			Machines.OnStateChange += OnStateChange;

			foreach (var (_, connection) in Machines.Connections)
			{
				Sync(connection);
			}

			_ = RunHealthCheckAsync(_serviceCts.Token);

			return Task.CompletedTask;
		}

		public async Task StopAsync(CancellationToken cancellationToken)
		{
			_serviceCts.Cancel();

			Machines.OnMachineAdded -= OnMachineAdded;
			Machines.OnMachineRemoved -= OnMachineRemoved;
			Machines.OnStateChange -= OnStateChange;

			foreach (var (id, _) in Active.ToArray())
			{
				await TeardownAsync(id);
			}

			_serviceCts.Dispose();
		}

		private void OnMachineAdded(object? sender, OnMachineAddedArgs e)
		{
			if (Machines.Connections.TryGetValue(e.MachineID, out var connection)) Sync(connection);
		}

		private async void OnMachineRemoved(object? sender, OnMachineRemovedArgs e)
		{
			await TeardownAsync(e.MachineID);
		}

		private void OnStateChange(object? sender, OnMachineStateUpdatedArgs e)
		{
			if (!e.Changes.StatusHasChanged) return;
			if (Machines.Connections.TryGetValue(e.MachineID, out var connection)) Sync(connection);
		}

		private void Sync(MachineConnection connection)
		{
			var connected = connection.State.Status is not MachineStatus.Disconnected;

			if (connected && !Active.ContainsKey(connection.ID))
			{
				_ = SetupAsync(connection);
			}
			else if (!connected && Active.ContainsKey(connection.ID))
			{
				_ = TeardownAsync(connection.ID);
			}
		}

		private async Task SetupAsync(MachineConnection connection)
		{
			var source = connection.GetCameraSource();
			if (source is CameraSource.NoCamera) return;

			var baseName = MakePathName(connection);
			var session = new CameraSession { Source = source };
			if (!Active.TryAdd(connection.ID, session)) return;

			try
			{
				MachineStreamingURLs urls;

				switch (source)
				{
					case CameraSource.PullCameraSource pull:
						await Relay.AddPullPath(baseName, pull.Upstream);
						var pullStream = new CameraStream(Relay.GetWebRTCUrl(baseName).ToString(), pull.Spec);
						urls = new MachineStreamingURLs(pullStream, Glance: null);
						break;

					case CameraSource.PublisherCameraSource publisher:
						var fullPath   = $"{baseName}_full";
						var glancePath = $"{baseName}_glance";
						await Relay.AddPublishPath(fullPath);

						session.Tracks.Add(new CameraTrack(fullPath,
							Task.Run(() => RunPublisherWithRestartAsync(connection, fullPath,
								Relay.GetRtspPublishUrl(fullPath), publisher.Full, session.Cts.Token))));

						CameraStream glanceStream;
						if (publisher.Glance != null)
						{
							await Relay.AddPublishPath(glancePath);
							session.Tracks.Add(new CameraTrack(glancePath,
								Task.Run(() => RunPublisherWithRestartAsync(connection, glancePath,
									Relay.GetRtspPublishUrl(glancePath), publisher.Glance, session.Cts.Token))));
							glanceStream = new CameraStream(Relay.GetWebRTCUrl(glancePath).ToString(), publisher.GlanceSpec);
						}
						else
						{
							glanceStream = new CameraStream(Relay.GetWebRTCUrl(fullPath).ToString(), publisher.FullSpec);
						}

						urls = new MachineStreamingURLs(
							new CameraStream(Relay.GetWebRTCUrl(fullPath).ToString(), publisher.FullSpec),
							glanceStream);

						break;

					default:
						return;
				}

				connection.SetStreamingURLs(urls);
			}
			catch (Exception ex)
			{
				Logger.LogError(ex, "Failed to register MediaMTX path for {MachineID}", connection.ID);
				Active.TryRemove(connection.ID, out _);
				session.Cts.Cancel();
			}
		}

		private async Task TeardownAsync(string machineID)
		{
			if (!Active.TryRemove(machineID, out var session)) return;

			session.Cts.Cancel();

			foreach (var track in session.Tracks)
			{
				try { await track.PublisherTask; }
				catch { /* cancellation expected */ }

				await Relay.RemovePath(track.PathName);
			}

			if (Machines.Connections.TryGetValue(machineID, out var connection))
			{
				foreach (var track in session.Tracks)
					connection.RemoveNotification($"camera.publisher.crashed.{track.PathName}");

				connection.SetStreamingURLs(null);
			}

			session.Cts.Dispose();
		}

		private async Task RunPublisherWithRestartAsync(
			MachineConnection connection,
			string pathName,
			Uri rtspTarget,
			StreamPublisherOptions options,
			CancellationToken ct)
		{
			var notifId = $"camera.publisher.crashed.{pathName}";
			var delay = TimeSpan.FromSeconds(2);

			while (!ct.IsCancellationRequested)
			{
				try
				{
					await connection.RunRTSPCameraPublisher(rtspTarget, options, ct);
					// Clean exit — clear any previous error notification, brief pause, then restart
					connection.RemoveNotification(notifId);
					delay = TimeSpan.FromSeconds(2);
				}
				catch (OperationCanceledException) when (ct.IsCancellationRequested)
				{
					return;
				}
				catch (Exception ex)
				{
					Logger.LogError(ex, "Camera publisher crashed for path {Path}", pathName);
					connection.AddNotification(new MachineMessage(
						notifId,
						"Camera Stream Interrupted",
						$"The camera publisher for '{pathName}' crashed and will retry: {ex.Message}",
						MachineMessageSeverity.Warning,
						MachineMessageActions.None,
						default));
					try { await Task.Delay(delay, ct); }
					catch (OperationCanceledException) { return; }
					delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, 60));
					continue;
				}

				try { await Task.Delay(TimeSpan.FromSeconds(1), ct); }
				catch (OperationCanceledException) { return; }
			}
		}

		private async Task RunHealthCheckAsync(CancellationToken ct)
		{
			using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
			while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
			{
				foreach (var (machineID, session) in Active.ToArray())
				{
					if (!Machines.Connections.TryGetValue(machineID, out var connection)) continue;
					try
					{
						switch (session.Source)
						{
							case CameraSource.PullCameraSource pull:
								await Relay.AddPullPath(MakePathName(connection), pull.Upstream, ct);
								break;
							case CameraSource.PublisherCameraSource:
								foreach (var track in session.Tracks)
									await Relay.AddPublishPath(track.PathName, ct);
								break;
						}
					}
					catch (Exception ex)
					{
						Logger.LogWarning(ex, "Health check re-registration failed for {MachineID}", machineID);
					}
				}
			}
		}

		private static string MakePathName(MachineConnection c)
		{
			var raw = c.ID;
			var chars = raw.Select(ch => char.IsLetterOrDigit(ch) || ch is '_' or '-' ? ch : '_').ToArray();
			return new string(chars);
		}

		private sealed class CameraSession
		{
			public CancellationTokenSource Cts { get; } = new();
			public List<CameraTrack> Tracks { get; } = [];
			public required CameraSource Source { get; init; }
		}

		private sealed record CameraTrack(string PathName, Task PublisherTask);
	}
}
