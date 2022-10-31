using System;
using Arcus.EventGrid.Publishing;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Workers.MessageHandlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults(builder =>
    {
        builder.Services.AddTransient(serviceProvider =>
        {
            var eventGridTopic = Environment.GetEnvironmentVariable("EVENTGRID_TOPIC_URI");
            var eventGridKey = Environment.GetEnvironmentVariable("EVENTGRID_AUTH_KEY");

            return EventGridPublisherBuilder
                   .ForTopic(eventGridTopic)
                   .UsingAuthenticationKey(eventGridKey)
                   .Build();
        });

        builder.Services.AddEventHubsMessageRouting()
                        .WithEventHubsMessageHandler<OrderEventHubsMessageHandler, Order>();
    })
    .Build();

host.Run();