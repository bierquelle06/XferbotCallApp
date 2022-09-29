using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CallingBotSample.Bots;
using CallingBotSample.Constants;
using CallingMeetingBot.Extenstions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Graph.Communications.Client;
using Microsoft.Graph.Communications.Common.Telemetry;
using Sentry;

namespace CallingMeetingBot.Controllers
{
    [Route(HttpRouteConstants.OnIncomingRequestRoute)]
    public class CallbackController: Controller
    {
        private readonly CallingBot _callingBot;

        private readonly IHub _sentryHub;

        public CallbackController(CallingBot bot, IHub sentryHub)
        {
            this._callingBot = bot;
            this._sentryHub = sentryHub;
        }

        [HttpPost, HttpGet]
        public async Task HandleCallbackRequestAsync()
        {
            var log = $"STEP 1 : Received HTTP {this.Request.Method}, {this.Request.Path} OnIncomingRequestAsync";
            this._sentryHub.CaptureMessage(message: log);

            await this._callingBot.ProcessNotificationAsync(this.Request, this.Response).ConfigureAwait(false);
        }

        //[HttpPost, HttpGet]
        //[Route(HttpRouteConstants.OnIncomingRequestRoute)]
        //public async Task OnIncomingRequestAsync()
        //{
        //    var log = $"Received HTTP {this.Request.Method}, {this.Request.Path} OnIncomingRequestAsync";
        //    this._sentryHub.CaptureMessage(message: log);

        //    var httpRequest = this.Request.CreateRequestMessage();

        //    await this._callingBot._client.ProcessNotificationAsync(httpRequest).ConfigureAwait(false);
        //}

        //[HttpPost, HttpGet]
        //[Route(HttpRouteConstants.OnNotificationRequestRoute)]
        //public async Task OnNotificationRequestAsync()
        //{
        //    var log = $"Received HTTP {this.Request.Method}, {this.Request.Path} OnNotificationRequestAsync";
        //    this._sentryHub.CaptureMessage(message: log);

        //    var httpRequest = this.Request.CreateRequestMessage();

        //    await this._callingBot._client.ProcessNotificationAsync(httpRequest).ConfigureAwait(false);
        //}
    }
}
