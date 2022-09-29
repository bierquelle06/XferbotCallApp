// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio EmptyBot v4.11.1

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace CallingBotSample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseSentry(o =>
                    {
                        o.Dsn = "https://8ec15f7fdaa848029eb07665cba6b4ef@o504565.ingest.sentry.io/6780092";
                        // When configuring for the first time, to see what the SDK is doing:
                        o.Debug = true;
                        // Set TracesSampleRate to 1.0 to capture 100% of transactions for performance monitoring.
                        // We recommend adjusting this value in production.
                        o.TracesSampleRate = 1.0;
                    });

                    webBuilder.UseStartup<Startup>();
                });
    }
}
