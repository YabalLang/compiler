using Yabal.Bot.Responders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Remora.Discord.API.Abstractions.Gateway.Commands;
using Remora.Discord.Gateway;
using Remora.Discord.Gateway.Extensions;
using Remora.Discord.Hosting.Options;
using Remora.Discord.Hosting.Services;
using Remora.Extensions.Options.Immutable;

var builder = Host.CreateApplicationBuilder(args);

// Logging
builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
builder.Logging.AddFilter("System.Net.Http.HttpClient.*.LogicalHandler", LogLevel.Warning);
builder.Logging.AddFilter("System.Net.Http.HttpClient.*.ClientHandler", LogLevel.Warning);

// Options
builder.Services.AddOptions();
builder.Services.Configure(() => new DiscordServiceOptions(TerminateApplicationOnCriticalGatewayErrors: true));

// Infrastructure
builder.Services.AddDiscordGateway(_ => builder.Configuration.GetConnectionString("Discord")!);
builder.Services.Configure<DiscordGatewayClientOptions>(g => g.Intents |= GatewayIntents.MessageContents);

// Commands
builder.Services.AddResponder<MessageCreateResponder>();

// Hosted services
builder.Services.AddHostedService<DiscordService>();

var host = builder.Build();

await host.RunAsync();
