namespace SosuBot.Web.Services.Models;

// ReSharper disable once InconsistentNaming
public record RenderJob(ulong RabbitMQDeliveryTag, string ReplayFileName);