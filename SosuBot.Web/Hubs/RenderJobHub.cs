using Microsoft.AspNetCore.SignalR;
using SosuBot.Web.Services;

namespace SosuBot.Web.Hubs;

public sealed class RenderJobHub(
    ILogger<RenderJobHub> logger,
    RabbitMqService rabbitMqService) : Hub
{
    public override async Task OnConnectedAsync()
    {
        rabbitMqService.AddConnectionId(Context.ConnectionId);
        await this.Clients.All.SendAsync("ReceiveMessage", $"Client renderer {this.Context.ConnectionId} has joined.");
        await this.Clients.All.SendAsync("ReceiveMessage", $"There are {rabbitMqService.GetConnectionIdsCount()} renderers now");
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        rabbitMqService.RemoveConnectionId(Context.ConnectionId);
        await this.Clients.All.SendAsync("ReceiveMessage", $"Client renderer {this.Context.ConnectionId} has disconnected.");
        await this.Clients.All.SendAsync("ReceiveMessage", $"There are {rabbitMqService.GetConnectionIdsCount()} renderers now");
    }

    public async Task RenderError(string message)
    {
        logger.LogWarning("Message nacked");
        await rabbitMqService.NackMessageAsync(message);
    }
}