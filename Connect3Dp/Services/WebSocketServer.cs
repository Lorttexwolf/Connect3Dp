using Lib3Dp;
using Lib3Dp.Extensions;
using Lib3Dp.State;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Connect3Dp.Services
{
    public class WebSocketServer<C>(ILogger<WebSocketServer<C>> logger, Func<string, WebSocket, C> connectionCtor) where C : IWebSocketClient
	{
		private static readonly JsonSerializerOptions JsonOptions = new()
		{
			PropertyNameCaseInsensitive = true,
			NumberHandling = JsonNumberHandling.Strict
		};

		private readonly ILogger<WebSocketServer<C>> Logger = logger;
		private readonly Func<string, WebSocket, C> ConnectionCtor = connectionCtor;
        private readonly ConcurrentDictionary<string, C> _Clients = new();
		private readonly ConcurrentDictionary<string, ActionDefinition> _mappedActions = new();

		public IReadOnlyDictionary<string, C> Clients => _Clients;

		private record ActionDefinition(string ActionName, Type? RequiredDataType, Type ResultType, Func<C, Task<IWebSocketClientActionResult>>? InvokeAsync, Func<C, object, Task<IWebSocketClientActionResult>>? InvokeWithDataAsync);

		public bool MapAction<AD, R>(string actionStr, Func<C, AD, Task<R>> actionWithData)
			where AD : notnull
			where R : IWebSocketClientActionResult
		{
			async Task<IWebSocketClientActionResult> WrappedAction(C connection, object obj)
			{
				if (obj is not AD typedObj)
				{
					// Return a failure result when payload is invalid
					return WebSocketClientActionResult.Failure($"Invalid payload type for action '{actionStr}'");
				}

				return await actionWithData(connection, typedObj);
			}

			Logger.LogTrace("Mapped Action {}; Requires {} and returns {}", actionStr, typeof(AD).Name, typeof(R).Name);

			var def = new ActionDefinition(actionStr, typeof(AD), typeof(R), null, WrappedAction);
			return _mappedActions.TryAdd(actionStr, def);
		}

		public bool MapAction<R>(string actionStr, Func<C, Task<R>> action) where R : IWebSocketClientActionResult
		{
			async Task<IWebSocketClientActionResult> WrappedAction(C connection)
			{
				return await action(connection);
			}

			Logger.LogTrace("Mapped Action {}; Returns {}", actionStr, typeof(R).Name);

			var def = new ActionDefinition(actionStr, null, typeof(R), WrappedAction, null);
			return _mappedActions.TryAdd(actionStr, def);
		}

		public async Task AcceptWebSocketAsync(HttpContext http)
        {
            if (!http.WebSockets.IsWebSocketRequest)
            {
				Logger.LogTrace("WebSocket endpoint received non socket-request, ignored.");
                http.Response.StatusCode = 400;
                return;
            }

			Logger.LogTrace("WebSocket endpoint received a socket-request.");

            using var ws = await http.WebSockets.AcceptWebSocketAsync();
            var clientId = Guid.NewGuid().ToString();
            var client = this.ConnectionCtor.Invoke(clientId, ws);

            _Clients[clientId] = client;
            Logger.LogInformation("WS connected {clientId}", clientId);

            try
            {
                await ReceiveLoopAsync(client);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Logger.LogError(ex, "WS receive error");
            }
            finally
            {
                _Clients.TryRemove(clientId, out _);
                try { await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "closing", CancellationToken.None); } catch { }
                Logger.LogInformation("WS disconnected {clientId}", clientId);
            }
        }

		private async Task ReceiveLoopAsync(C client)
		{
			var buffer = new byte[16 * 1024];
			var ws = client.WebSocket;

			while (ws.State == WebSocketState.Open)
			{
				var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

				if (result.MessageType == WebSocketMessageType.Close) break;

				if (result.MessageType != WebSocketMessageType.Text) continue;

				var text = Encoding.UTF8.GetString(buffer, 0, result.Count);

				try
				{
					using var doc = JsonDocument.Parse(text);
					var root = doc.RootElement;

					Logger.LogTrace("Received message from client {}", JsonSerializer.Serialize(root, JsonOptions));

					if (!root.TryGetProperty("Action", out var actionProp)) continue;

					var actionName = actionProp.GetString();
					if (string.IsNullOrWhiteSpace(actionName)) continue;

					if (!_mappedActions.TryGetValue(actionName, out var mapped))
					{
						Logger.LogWarning("Unknown action {}, available: {}", actionName, string.Join(',', _mappedActions.Keys));

						continue;
					}

					root.TryGetString(out string? responseMessageID, "ResponseMessageID");

					object resultObj;

					// Invoke without Data

					if (mapped.InvokeAsync is not null)
					{
						try
						{
							resultObj = await mapped.InvokeAsync(client);
						}
						catch (Exception ex)
						{
							Logger.LogError(ex, "Action execution failed for {action}", actionName);
							continue;
						}
					}
					else if (mapped.RequiredDataType is not null && mapped.InvokeWithDataAsync is not null)
					{
						if (!root.TryGetProperty("Data", out var dataProp))
						{
							Logger.LogWarning("Missing Data for action {action}", actionName);
							continue;
						}

						// Invoke with Data

						object? dataObj;

						try
						{
							dataObj = JsonSerializer.Deserialize(dataProp, mapped.RequiredDataType, JsonOptions);
						}
						catch (Exception ex)
						{
							var failedToDeserialize = new MessageToClient<WebSocketClientActionResult>(
								responseMessageID,
								DateTimeOffset.UtcNow,
								actionName,
								WebSocketClientActionResult.Failure($"Failed to Deserialize; {ex.Message}"));

							await SendMessageToClientAsync(failedToDeserialize, client);

							continue;
						}

						if (dataObj == null) continue;

						try
						{
							resultObj = await mapped.InvokeWithDataAsync(client, dataObj);
						}
						catch (Exception ex)
						{
							Logger.LogError(ex, "Action execution failed for {action} with data {}", actionName, dataObj);
							continue;
						}
					}
					else
					{
						continue;
					}

					// Do not switch from <object>; this forces System.Text.Json to check the runtime type.
					var response = new MessageToClient<object>(responseMessageID, DateTimeOffset.Now, actionName, resultObj);

					await SendMessageToClientAsync(response, client);
				}
				catch (JsonException)
				{
					Logger.LogWarning("Invalid JSON received");
				}
			}

			client.Dispose();
		}

		public Task SendMessageToClientAsync<T>(MessageToClient<T> messageToClient, IWebSocketClient connection) where T : notnull
		{
			string jsonOfMessage;

			try
			{
				jsonOfMessage = JsonSerializer.Serialize(messageToClient, messageToClient.GetType());
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Failed to serialize data {} being sent to a client", messageToClient);
				throw;
			}
			var bytesOfJson = Encoding.UTF8.GetBytes(jsonOfMessage);

			return connection.WebSocket.SendAsync(new ArraySegment<byte>(bytesOfJson), WebSocketMessageType.Text, true, CancellationToken.None);
		}

		public static async Task BroadcastMessageAsync<T>(MessageToClient<T> messageToBroadcast, IEnumerable<IWebSocketClient> connections) where T : notnull
		{
			var jsonOfMessage = JsonSerializer.Serialize(messageToBroadcast, messageToBroadcast.GetType());
			var bytesOfJson = Encoding.UTF8.GetBytes(jsonOfMessage);

			foreach (var connection in connections)
			{
				await connection.WebSocket.SendAsync(new ArraySegment<byte>(bytesOfJson), WebSocketMessageType.Text, true, CancellationToken.None);
			}
		}
    }

	public interface IWebSocketClient : IDisposable
	{
		public string ID { get; }
		public WebSocket WebSocket { get; }
	}

	public interface IWebSocketClientActionResult
	{
		public bool IsSuccess { get; }
		public string? FailureReason { get; }
	}

	public record struct WebSocketClientActionResult(bool IsSuccess, string? FailureReason) : IWebSocketClientActionResult
	{
		public static WebSocketClientActionResult Success() => new(true, null);
		public static WebSocketClientActionResult Failure(string failureReason) => new(false, failureReason);
	}

	public record struct MessageToClient<T>(string? MessageID, DateTimeOffset Time, string Topic, T Data) where T : notnull;

	public record struct MessageToServer<T>(string Action, string? ResponseMessageID, T Data) where T : notnull;

	public record struct ClientMessageMachineOperationResult(bool IsSuccess, string? FailureReason, MachineMessage? FailureMessage) : IWebSocketClientActionResult
	{
		public static ClientMessageMachineOperationResult Of(MachineOperationResult operationResult)
		{
			if (operationResult.Success)
				return new ClientMessageMachineOperationResult(true, null, null);
			else
			{
				return new ClientMessageMachineOperationResult(
					false,
					$"{operationResult.Reasoning!.Value.Title}; {operationResult.Reasoning.Value.Body}",
					operationResult.Reasoning.Value);
			}
		}
	}

	public partial class MessageToServerJsonConverter
	{
		public static JsonSerializerOptions GetOptions()
		{
			return new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
		}

		/// <summary>
		/// Attempts to parse a MessageToServer whose Data is a JsonElement into a strongly-typed MessageToServer<T> when the Action matches the expected action.
		/// </summary>
		public static bool TryDeserializeMessage<T>(MessageToServer<JsonElement> message, string expectedAction, out MessageToServer<T> typedMessage, JsonSerializerOptions options) where T : notnull
		{
			typedMessage = default;

			if (!string.Equals(message.Action, expectedAction, StringComparison.OrdinalIgnoreCase)) return false;

			try
			{
				var data = message.Data.Deserialize<T>(options);

				if (data is null) return false;

				typedMessage = new MessageToServer<T>(message.Action, message.ResponseMessageID, data);

				return true;
			}
			catch (JsonException)
			{
				return false;
			}
		}
	}
}
