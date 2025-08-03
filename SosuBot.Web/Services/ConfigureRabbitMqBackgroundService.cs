using System.Collections;
using System.Collections.Concurrent;
using System.Text;
using Microsoft.AspNetCore.SignalR;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SosuBot.Web.Hubs;
using SosuBot.Web.Services.Models;

namespace SosuBot.Web.Services;

public sealed class ConfigureRabbitMqBackgroundService(
    RabbitMqService rabbitMQService,
    ILogger<ConfigureRabbitMqBackgroundService> logger) : BackgroundService
{
    private CancellationToken _token;

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _token = cancellationToken;
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await SetupRabbitMqConnection(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Task was cancelled.");
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "RabbitMQ connection failed. Retrying...");
            }

            try
            {
                await Task.Delay(1000, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Task was cancelled.");
            }
        }
    }

    private async Task SetupRabbitMqConnection(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory() { HostName = "localhost" };
        await using var connection = await factory.CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
        rabbitMQService.SetChannel(channel);

        string queueName = "render-job-queue";
        await channel.QueueDeclareAsync(queue: queueName, durable: true, exclusive: false,
            autoDelete: false, arguments: null, cancellationToken: stoppingToken);
        
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 0, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            await rabbitMQService.SendRenderRequestAsync(model, ea);
        };
        
        await channel.BasicConsumeAsync(queueName, autoAck: true, consumer: consumer, cancellationToken: stoppingToken);
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}