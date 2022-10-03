// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio EmptyBot v4.11.1

using CallingBotSample.Bots;
using CallingBotSample.Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Sentry;
using System.Threading.Tasks;

namespace CallingBotSample.Controllers
{
    // This ASP Controller is created to handle a request. Dependency Injection will provide the Adapter and IBot
    // implementation at runtime. Multiple different IBot implementations running at different endpoints can be
    // achieved by specifying a more specific type for the bot constructor argument.
   
    [ApiController]
    public class BotController : ControllerBase
    {
        private readonly IBotFrameworkHttpAdapter _adapter;

        private readonly CallingBot _callingBot;

        private readonly IHub _sentryHub;

        public BotController(IBotFrameworkHttpAdapter adapter, IHub sentryHub, CallingBot bot)
        {
            _adapter = adapter;
            _sentryHub = sentryHub;
            _callingBot = bot;
        }

        [HttpPost, HttpGet]
        [Route(HttpRouteConstants.MessagesRequestRoute)]
        public async Task PostAsync()
        {
            var log = $"STEP 1 : PostAsync :: Received HTTP {this.Request.Method}, {this.Request.Path}";

            this._sentryHub.CaptureMessage("BotController :: PostAsync");

            // Delegate the processing of the HTTP POST to the adapter.
            // The adapter will invoke the bot.
            await _adapter.ProcessAsync(this.Request, this.Response, this._callingBot);
        }
    }
}
