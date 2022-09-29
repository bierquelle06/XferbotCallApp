// < copyright file = "GraphConfiguration.cs" company = "Microsoft Corporation" >
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace CallingBotSample.Extensions
{
    using CallingBotSample.Configuration;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using Microsoft.Graph;
    using Microsoft.Identity.Client;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.AI.Luis;
    using System;
    using CallingBotSample.Bots;

    /// <summary>
    /// Luis Configuration.
    /// </summary>
    public static class LuisConfiguration
    {
        /// <summary>
        /// Configure Luis Component.
        /// </summary>
        /// <param name="services">IServiceCollection .</param>
        /// <param name="configuration">IConfiguration .</param>
        /// <returns>..</returns>
        public static IServiceCollection ConfigureLuisComponent(this IServiceCollection services, Action<LuisOptions> luisOptionsAction)
        {
            var options = new LuisOptions();
            luisOptionsAction(options);

            if (string.IsNullOrEmpty(options.AppId))
                return services;

            var luisApplication = new LuisApplication(options.AppId, 
                options.ApiKey, 
                options.ApiEndpointUrl);

            var recognizerOptions = new LuisRecognizerOptionsV3(luisApplication)
            {
                PredictionOptions = new Microsoft.Bot.Builder.AI.LuisV3.LuisPredictionOptions
                {
                    IncludeInstanceData = true,
                }
            };

            services.AddSingleton<LuisRecognizer>(sp => 
            {
                return new LuisRecognizer(recognizerOptions);
            });

            return services.AddTransient<CallingBot>();
            //return services;
        }
    }
}