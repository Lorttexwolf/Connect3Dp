
using Lib3Dp.Connectors;
using Lib3Dp.Connectors.BambuLab;
using System.Net;
using System.Text.Json;

namespace Connect3Dp
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var P2S = BBLMachineConnector.LAN("Mr. Worldwide", "22E8AJ591501848", "744d0749", IPAddress.Parse("192.168.1.183"));

			_ = P2S.Connect();

			var builder = WebApplication.CreateBuilder(args);

			// Add services to the container.
			//builder.Services.AddAuthorization();

			// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
			builder.Services.AddEndpointsApiExplorer();
			builder.Services.AddSwaggerGen();

			//builder.Services.Configure<Connect3DpSettings>(config =>
			//{
			//	config.ConfigFilePath = "config.json";
			//});

			// Check Enviroment
			// 1. Load with File-Based
			// 2. Load with MySQL Lite.

			//builder.Services.AddHostedService<MachineService>();
			//builder.Services.AddSingleton<MachineWebSocketService>();

			var app = builder.Build();

			// Configure the HTTP request pipeline.
			if (app.Environment.IsDevelopment())
			{
				app.UseSwagger();
				app.UseSwaggerUI();
			}

			app.UseHttpsRedirection();
			//app.UseAuthorization();

			//app.Services.GetService<MachineWebSocketService>();

			app.MapGet("/machine", () => P2S.State);

			app.Use(async (context, next) =>
			{
				if (context.Request.Path == "/ws")
				{
					if (context.WebSockets.IsWebSocketRequest)
					{
						using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
						//await Echo(webSocket);


					}
					else
					{
						context.Response.StatusCode = StatusCodes.Status400BadRequest;
					}
				}
				else
				{
					await next(context);
				}

			});

			//app.MapGet("/weatherforecast", (HttpContext httpContext) =>
			//{
			//	var forecast = Enumerable.Range(1, 5).Select(index =>
			//		new WeatherForecast
			//		{
			//			Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
			//			TemperatureC = Random.Shared.Next(-20, 55),
			//			Summary = summaries[Random.Shared.Next(summaries.Length)]
			//		})
			//		.ToArray();
			//	return forecast;
			//})
			//.WithName("GetWeatherForecast")
			//.WithOpenApi();

			//app.MapPut("/machines", (HttpContext httpContext) =>
			//{



			//});

			app.Run();
		}
	}

	internal record Connect3DpConfiguration
	{
		// Environments
		// Machines
		// Auth
		// Storage? Copy File ID's and store them locally so we don't loose any data.
	}
}
