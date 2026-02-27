using Connect3Dp;
using Connect3Dp.Extensions;
using Connect3Dp.Extensions.JeWebSocket;
using Connect3Dp.Logging;
using JeIdentity.AspNetCore.Extensions;
using Lib3Dp;
using Lib3Dp.Configuration;
using Lib3Dp.Connectors;
using Lib3Dp.Files;
using Lib3Dp.Utilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using static Connect3Dp.Extensions.JeWebSocket.JeWebSocketExtensions;

namespace Connect3Dp.Host
{
	public static class Program
	{
		public static async Task Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			builder.Configuration
				.AddJsonFile("connect3dp.config.json", optional: true, reloadOnChange: true);

			// TODO: Logs should be recorded and readable by HTTP / WS.
			builder.Logging.AddConsole();

			var bufferedLoggerProvider = new BufferedLoggerProvider(500);
			builder.Services.AddSingleton(bufferedLoggerProvider);
			builder.Logging.AddProvider(bufferedLoggerProvider);

			builder.Services.AddOpenApi();
			//builder.Services.AddSwaggerGen();
			builder.Services
				.AddUserDefinedMachineFileStore(builder.Configuration)
				.AddUserDefinedMachineConfigurationStore(builder.Configuration)
				.AddMachineConnectionCollection()
				.AddJeWebSocketServiceWithConnect3DpClient();

			builder.Services.AddControllers();
			builder.Services.AddJeIdentity();

			var app = builder.Build();

			var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Connect3Dp.Host.Program");

			if (app.Environment.IsDevelopment())
			{
				logger.LogInformation("Development mode: enabling OpenAPI and Swagger");
				app.MapOpenApi();
				//app.UseSwagger();
				//app.UseSwaggerUI();
			}
			else
			{
				app.UseHttpsRedirection();
			}

			app.UseWebSockets();

			logger.LogInformation("Mapping Controllers");
			app.MapControllers();

			logger.LogInformation("Mapping JeIdentity endpoint at /jeIdentity");
			app.MapJeIdentity("/jeIdentity");

			app.Services.MapAllConnect3DpWebSocketActions();
			app.MapConnect3DpJeWebSocketServerAtUserDefinedEndpoint(builder.Configuration, logger);

			var mc = app.Services.GetRequiredService<MachineConnectionCollection>();
			var mcStore = app.Services.GetRequiredService<IMachineConfigurationStore>();

			logger.LogInformation("Loading machine configurations from store");

			var cfgs = await mcStore.LoadConfigurations();
			logger.LogInformation("Loaded {Count} machine configuration(s):", cfgs.Length);

			foreach (var cfg in cfgs)
				logger.LogInformation("\t - {MachineId}: {Discrimination}", cfg.MachineID, cfg.ConfigurationWithDiscrimination.Discrimination);

			logger.LogInformation("Instantiating machine connections");

			await mc.LoadFromConfigurations(cfgs);

			logger.LogInformation("Instantiated {Count} machine connection(s):", mc.Connections.Count);

			foreach (var (id, connection) in mc.Connections)
				logger.LogInformation("\t - {MachineId} ({Type})", id, connection.GetType().Name);

			logger.LogInformation("Connecting to disconnected machines");

			_ = mc.ConnectIfDisconnected(default);

			logger.LogInformation("Startup complete — {Count} machine(s) configured", mc.Connections.Count);

			app.Run();
		}

		public static IEndpointConventionBuilder MapConnect3DpJeWebSocketServerAtUserDefinedEndpoint(this IEndpointRouteBuilder routeBuilder, IConfiguration configuration, ILogger logger)
		{
			var wsEndpoint = configuration["Connect3Dp:WebSocket:Endpoint"] ?? "/ws";

			logger.LogInformation("Mapping WebSocket Server at {}", wsEndpoint);

			return routeBuilder.MapJeWebSocketServer<JeWebSocketClientForConnect3Dp>(wsEndpoint);
		}

		public static IServiceCollection AddUserDefinedMachineFileStore(this IServiceCollection services, IConfiguration configuration)
		{
			var type = configuration["Connect3Dp:MachineFileStore:Type"] ?? "FileSystem";

			return type switch
			{
				"FileSystem" => services.AddFileSystemMachineFileStore(new FileSystemMachineFileStoreOptions(
					configuration["Connect3Dp:FileStore:FileSystem:PathToDirectory"] ?? "./",
					configuration.GetValue<bool>("Connect3Dp:FileStore:FileSystem:VerifyHashes"))),

				_ => throw new InvalidOperationException($"Unknown FileStore type {type}")
			};
		}

		public static IServiceCollection AddUserDefinedMachineConfigurationStore(this IServiceCollection services, IConfiguration configuration)
		{
			var type = configuration["Connect3Dp:MachineConfigurationStore:Type"] ?? "Json";

			return type switch
			{
				"Json" => services.AddJsonFileBasedMachineConfigurationStore(configuration["Connect3Dp:MachineConfigurationStore:Json:Path"] ?? throw new InvalidOperationException("Path must be provided when using Json as the MachineConfigurationStore.")),

				_ => throw new InvalidOperationException($"Unknown MachineConfigurationStore type {type}")
			};
		}

		public static IServiceProvider MapAllConnect3DpWebSocketActions(this IServiceProvider services)
		{
			var ws = services.GetRequiredConnect3DpJeWebSocketServer();
			var mc = services.GetRequiredService<MachineConnectionCollection>();
			var mfs = services.GetRequiredService<IMachineFileStore>();
			var bufferedLogger = services.GetRequiredService<BufferedLoggerProvider>();

			ws
				.WithMachineFileStoreMachineUsage(mc, mfs)
				.WithMachineFileStoreTotalUsage(mfs)
				.WithSubscribeToLogs(bufferedLogger)
				.WithUnsubscribeToLogs(bufferedLogger)
				.WithLogHistoryRetrieval(bufferedLogger)
				.WithSubscribeAndUnsubscribeAction(mc)
				.WithStateBroadcasts(mc)
				.WithMarkAsIdleAction(mc)
				.WithConfigurationsAction(mc)
				.WithResumeMachine(mc)
				.WithStopMachine(mc)
				.WithFindMatchingSpoolsMachineAction(mc)
				.WithPauseMachine(mc);

			return services;
		}
	}
}
