using System.Net;
using Dapr.Client;
using IntakeService.Models;

namespace IntakeService.Services;

public sealed class DaprNotificationLookup
{
    private readonly DaprClient daprClient;
    private readonly ILogger<DaprNotificationLookup> logger;

    public DaprNotificationLookup(DaprClient daprClient, ILogger<DaprNotificationLookup> logger)
    {
        this.daprClient = daprClient;
        this.logger = logger;
    }

    public async Task<NotificationLookupResponse?> GetAsync(string trackingId)
    {
        try
        {
            var response = await daprClient.InvokeMethodAsync<NotificationLookupResponse>(
                HttpMethod.Get,
                DaprServiceIds.NotificationService,
                $"notifications/{trackingId}");

            return response;
        }
        catch (InvocationException ex) when (ex.Response?.StatusCode == HttpStatusCode.NotFound)
        {
            logger.LogDebug(
                "No notification record found yet for submission {TrackingId}.",
                trackingId);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Failed to read notification for submission {TrackingId}. Continuing without payment notification details.",
                trackingId);
            return null;
        }
    }
}
