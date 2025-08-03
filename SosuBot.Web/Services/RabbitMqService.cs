using System.Text;
using Microsoft.AspNetCore.SignalR;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SosuBot.Web.Hubs;
using SosuBot.Web.Services.Models;
#pragma warning disable CS8602 // Dereference of a possibly null reference.

namespace SosuBot.Web.Services;

public class RabbitMqService(
    ILogger<RabbitMqService> logger,
    IHubContext<RenderJobHub> hubContext)
{
    private static int _nextClient;
    private readonly List<string> _webSocketConnectionIds = new();
    private readonly List<RenderJob> _pendingRenderJobs = new();

    private IChannel? _channel;

    private static readonly object Lock1 = new();
    private static readonly object Lock2 = new();

    public async Task AckMessageAsync(string message)
    {
        if (_channel == null)
        {
            await Task.Delay(10000);
            if (_channel == null)
            {
                throw new Exception("Channel is null");
            }
        }

        RenderJob[] pendingJobs = GetPendingRenderJobs();
        RenderJob? renderJob = pendingJobs.FirstOrDefault(m => m.ReplayFileName == message);
        if (renderJob == null)
        {
            logger.LogError("Render job not found");
            return;
        }

        Console.WriteLine("[x] Done");
        await _channel.BasicAckAsync(deliveryTag: renderJob.RabbitMQDeliveryTag, multiple: false);
        RemovePendingRenderJob(renderJob);
    }

    public async Task NackMessageAsync(string message)
    {
        if (_channel == null)
        {
            await Task.Delay(10000);
            if (_channel == null)
            {
                throw new Exception("Channel is null");
            }
        }

        var pendingJobs = GetPendingRenderJobs();
        RenderJob? renderJob = pendingJobs.FirstOrDefault(m => m.ReplayFileName == message);
        if (renderJob == null)
        {
            logger.LogError("Render job not found");
            return;
        }

        await _channel.BasicNackAsync(renderJob.RabbitMQDeliveryTag, multiple: false, requeue: true);
    }

    public async Task SendRenderRequestAsync(object model, BasicDeliverEventArgs ea)
    {
        if (GetConnectionIdsCount() == 0)
        {
            await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
            return;
        }

        try
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            Console.WriteLine($" [x] Received {message}");

            AddPendingRenderJob(new RenderJob(ea.DeliveryTag, message));


            int index = Interlocked.Increment(ref _nextClient) % GetConnectionIdsCount();
            string connectionId = GetConnectionId(index);

            await hubContext.Clients.Client(connectionId).SendAsync("RenderJob", message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Handling failed. Requeueing...");
            await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
        }
    }

    public bool JobMessageExists(string message)
    {
        return GetPendingRenderJobs().Any(m => m.ReplayFileName == message);
    }

    public void SetChannel(IChannel channel)
    {
        _channel = channel;
    }

    public void AddConnectionId(string connectionId)
    {
        lock (Lock1)
        {
            _webSocketConnectionIds.Add(connectionId);
        }
    }

    public void RemoveConnectionId(string connectionId)
    {
        lock (Lock1)
        {
            _webSocketConnectionIds.Remove(connectionId);
        }
    }

    public int GetConnectionIdsCount()
    {
        int count;
        lock (Lock1)
        {
            count = _webSocketConnectionIds.Count;
        }
        
        return count;
    }

    public string GetConnectionId(int index)
    {
        string connectionId;
        lock (Lock1)
        {
            connectionId = _webSocketConnectionIds[index];
        }
        
        return connectionId;
    }

    public void AddPendingRenderJob(RenderJob renderJob)
    {
        lock (Lock2)
        {
            _pendingRenderJobs.Add(renderJob);
        }
    }

    public void RemovePendingRenderJob(RenderJob renderJob)
    {
        lock (Lock2)
        {
            _pendingRenderJobs.Remove(renderJob);
        }
    }

    public RenderJob[] GetPendingRenderJobs()
    {
        RenderJob[] renderJobs;
        lock (Lock2)
        {
            renderJobs = _pendingRenderJobs.ToArray();
        }
        
        return renderJobs;
    }
}