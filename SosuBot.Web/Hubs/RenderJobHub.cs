using Microsoft.AspNetCore.SignalR;
using SosuBot.Web.Services;

namespace SosuBot.Web.Hubs;

public sealed class RenderJobHub(
    ILogger<RenderJobHub> logger,
    RabbitMqService rabbitMqService) : Hub
{
    public override async Task OnConnectedAsync()
    {
        logger.LogInformation($"ConnectionId: {Context.ConnectionId}\n");
        await this.Clients.All.SendAsync("ReceiveMessage", $"Client renderer {this.Context.ConnectionId} has joined.");
        rabbitMqService.AddConnectionId(Context.ConnectionId);
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        rabbitMqService.RemoveConnectionId(Context.ConnectionId);
        return Task.CompletedTask;
    }

    public async Task RenderError(string message)
    {
        logger.LogWarning("Message nacked");
        await rabbitMqService.NackMessageAsync(message);
    }
}