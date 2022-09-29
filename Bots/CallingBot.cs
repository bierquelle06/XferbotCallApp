// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CallingBotSample.Interfaces;
using CallingBotSample.Utility;
using CallingMeetingBot.Extenstions;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Communications.Calls;
using Microsoft.Graph.Communications.Calls.Media;
using Microsoft.Graph.Communications.Client;
using Microsoft.Graph.Communications.Client.Authentication;
using Microsoft.Graph.Communications.Common;
using Microsoft.Graph.Communications.Common.Telemetry;
using Microsoft.Graph.Communications.Core.Notifications;
using Microsoft.Graph.Communications.Core.Serialization;
using Microsoft.Skype.Bots.Media;
using Sentry;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CallingBotSample.Bots
{
    public class CallingBot : ActivityHandler
    {
        private readonly IConfiguration _configuration;
        
        private readonly IHub _sentryHub;
        private IRequestAuthenticationProvider _authenticationProvider { get; }

        private INotificationProcessor _notificationProcessor { get; }
        private CommsSerializer _serializer { get; }

        private readonly BotOptions _botOptions;

        private readonly ICard _card;
        private readonly IGraph _graph;
        private readonly IGraphServiceClient _graphServiceClient;

        public ConcurrentDictionary<string, CallHandler> CallHandlers { get; } = new ConcurrentDictionary<string, CallHandler>();

        /// <summary>
        /// Gets the entry point for stateful bot.
        /// </summary>
        /// <value>The client.</value>
        public ICommunicationsClient _client { get; private set; }

        protected readonly BotState _conversationState;
        protected readonly BotState _userState;

        public void Dispose()
        {
            this._client?.Dispose();
            this._client = null;
        }

        public CallingBot(BotOptions options, 
            IConfiguration configuration, 
            ICard card, 
            IGraph graph, 
            IGraphServiceClient graphServiceClient, 
            ConversationState conversationState, 
            UserState userState,
            IHub sentryHub)
        {
            this._conversationState = conversationState;
            this._userState = userState;
            this._sentryHub = sentryHub;

            this._botOptions = options;
            this._configuration = configuration;

            this._card = card;
            this._graph = graph;

            this._graphServiceClient = graphServiceClient;
            
            var name = this.GetType().Assembly.GetName().Name;

            var builder = new CommunicationsClientBuilder(name, options.AppId);

            this._authenticationProvider = new AuthenticationProvider(name, options.AppId, options.AppSecret, sentryHub);

            this._serializer = new CommsSerializer();

            this._notificationProcessor = new NotificationProcessor(this._serializer);
            this._notificationProcessor.OnNotificationReceived += this.NotificationProcessor_OnNotificationReceived;

            builder.SetAuthenticationProvider(this._authenticationProvider);
            builder.SetNotificationUrl(options.PlaceCallEndpointUrl);
            //builder.SetMediaPlatformSettings(_azureSettings.MediaPlatformSettings);
            builder.SetServiceBaseUrl(options.BotBaseUrl);

            //this._client = builder.Build();
            //this._client.Calls().OnIncoming += CallingBot_OnIncoming;
            //this._client.Calls().OnUpdated += CallingBot_OnUpdated;

            //_sentryHub.CaptureMessage("Initializing Bot Service");
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            await base.OnTurnAsync(turnContext, cancellationToken);

            // Save any state changes that might have occurred during the turn.
            await this._conversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await this._userState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        private static async Task SendIntroCardAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var card = new HeroCard
            {
                Title = "Welcome to XFERBOT!",
                Text = @"XFERBOT",
                Images = new List<CardImage>() { new CardImage("https://aka.ms/bf-welcome-card-image") },
                Buttons = new List<CardAction>()
                {
                    new CardAction(ActionTypes.OpenUrl, "Get an overview", null, "Get an overview", "Get an overview", "https://docs.microsoft.com/en-us/azure/bot-service/?view=azure-bot-service-4.0"),
                    new CardAction(ActionTypes.OpenUrl, "Ask a question", null, "Ask a question", "Ask a question", "https://stackoverflow.com/questions/tagged/botframework"),
                    new CardAction(ActionTypes.OpenUrl, "Learn how to deploy", null, "Learn how to deploy", "Learn how to deploy", "https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-howto-deploy-azure?view=azure-bot-service-4.0"),
                }
            };

            var response = MessageFactory.Attachment(card.ToAttachment());
            await turnContext.SendActivityAsync(response, cancellationToken);
        }

        private void CallingBot_OnUpdated(ICallCollection sender, Microsoft.Graph.Communications.Resources.CollectionEventArgs<ICall> args)
        {
            _sentryHub.CaptureMessage("CallingBot_OnUpdated");

            foreach (var call in args.AddedResources)
            {
                var callHandler = new CallHandler(this, call, this._serializer);
                this.CallHandlers[call.Id] = callHandler;
            }

            foreach (var call in args.RemovedResources)
            {
                if (this.CallHandlers.TryRemove(call.Id, out CallHandler handler))
                {
                    handler.Dispose();
                }
            }
        }

        private void CallingBot_OnIncoming(ICallCollection sender, Microsoft.Graph.Communications.Resources.CollectionEventArgs<ICall> args)
        {
            _sentryHub.CaptureMessage("CallingBot_OnIncoming");

            args.AddedResources.ForEach(call =>
            {
                // Get the policy recording parameters.
                _sentryHub.CaptureMessage("CallingBot_OnIncoming :: 1");

                // The context associated with the incoming call.
                IncomingContext incomingContext =
                    call.Resource.IncomingContext;

                _sentryHub.CaptureMessage("CallingBot_OnIncoming :: 2");

                // The RP participant.
                string observedParticipantId =
                    incomingContext.ObservedParticipantId;

                _sentryHub.CaptureMessage("CallingBot_OnIncoming :: 3");

                // If the observed participant is a delegate.
                IdentitySet onBehalfOfIdentity = incomingContext.OnBehalfOf;

                _sentryHub.CaptureMessage("CallingBot_OnIncoming :: 4");

                // If a transfer occured, the transferor.
                IdentitySet transferorIdentity = incomingContext.Transferor;

                _sentryHub.CaptureMessage("CallingBot_OnIncoming :: 5");

                string countryCode = null;
                EndpointType? endpointType = null;

                // Note: this should always be true for CR calls.
                if (incomingContext.ObservedParticipantId == incomingContext.SourceParticipantId)
                {
                    // The dynamic location of the RP.
                    countryCode = call.Resource.Source.CountryCode;

                    _sentryHub.CaptureMessage("CallingBot_OnIncoming :: Country Code : " + countryCode);

                    // The type of endpoint being used.
                    endpointType = call.Resource.Source.EndpointType;

                    _sentryHub.CaptureMessage("CallingBot_OnIncoming :: Endpoint Type : " + endpointType);
                }

                _sentryHub.CaptureMessage("CallingBot_OnIncoming :: CALL ID : " + call.Id);

                IMediaSession mediaSession = Guid.TryParse(call.Id, out Guid callId)
                    ? this.CreateLocalMediaSession(callId)
                    : this.CreateLocalMediaSession();

                // Answer call
                string answerCallText = $"Answering call {call.Id} with scenario {call.ScenarioId}.";

                _sentryHub.CaptureMessage("CallingBot_OnIncoming :: answerCallText : " + answerCallText);

                call?.AnswerAsync(mediaSession).ForgetAndLogExceptionAsync(this._sentryHub, answerCallText);
            });
        }

        private ILocalMediaSession CreateLocalMediaSession(Guid mediaSessionId = default)
        {
            try
            {
                _sentryHub.CaptureMessage("CreateLocalMediaSession :: mediaSessionId : " + mediaSessionId);

                // create media session object, this is needed to establish call connections
                return this._client.CreateMediaSession(
                    new AudioSocketSettings
                    {
                        StreamDirections = StreamDirection.Sendrecv,
                        // Note! Currently, the only audio format supported when receiving unmixed audio is Pcm16K
                        SupportedAudioFormat = AudioFormat.Pcm16K,
                        ReceiveUnmixedMeetingAudio = false //get the extra buffers for the speakers
                    },
                    new VideoSocketSettings
                    {
                        StreamDirections = StreamDirection.Inactive
                    },
                    mediaSessionId: mediaSessionId);
            }
            catch (Exception ex)
            {
                this._sentryHub.CaptureException(ex);
                this._sentryHub.CaptureMessage(ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Process "/callback" notifications asyncronously. 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        public async Task ProcessNotificationAsync(
            HttpRequest request,
            HttpResponse response)
        {
            try
            {
                _sentryHub.CaptureMessage("STEP 2");

                var httpRequest = request.CreateRequestMessage();
                var results = await this._authenticationProvider.ValidateInboundRequestAsync(httpRequest).ConfigureAwait(false);
                if (results.IsValid)
                {
                    var httpResponse = await this._notificationProcessor.ProcessNotificationAsync(httpRequest).ConfigureAwait(false);
                    await httpResponse.CreateHttpResponseAsync(response).ConfigureAwait(false);
                }
                else
                {
                    var httpResponse = httpRequest.CreateResponse(HttpStatusCode.Forbidden);
                    await httpResponse.CreateHttpResponseAsync(response).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _sentryHub.CaptureMessage("ProcessNotificationAsync Error : " + ex.ToString());
                _sentryHub.CaptureException(ex);

                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                await response.WriteAsync(ex.ToString()).ConfigureAwait(false);
            }
        }


        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            var credentials = new MicrosoftAppCredentials(
                this._configuration[Common.Constants.MicrosoftAppIdConfigurationSettingsKey], 
                this._configuration[Common.Constants.MicrosoftAppPasswordConfigurationSettingsKey]);

            ConversationReference conversationReference = null;

            foreach (var member in membersAdded)
            {

                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    var proactiveMessage = MessageFactory.Attachment(this._card.GetWelcomeCardAttachment());
                    proactiveMessage.TeamsNotifyUser();
                    var conversationParameters = new ConversationParameters
                    {
                        IsGroup = false,
                        Bot = turnContext.Activity.Recipient,
                        Members = new ChannelAccount[] { member },
                        TenantId = turnContext.Activity.Conversation.TenantId
                    };
                    await ((BotFrameworkAdapter)turnContext.Adapter).CreateConversationAsync(
                        turnContext.Activity.TeamsGetChannelId(),
                        turnContext.Activity.ServiceUrl,
                        credentials,
                        conversationParameters,
                        async (t1, c1) =>
                        {
                            conversationReference = t1.Activity.GetConversationReference();
                            await ((BotFrameworkAdapter)turnContext.Adapter).ContinueConversationAsync(
                                this._configuration[Common.Constants.MicrosoftAppIdConfigurationSettingsKey],
                                conversationReference,
                                async (t2, c2) =>
                                {
                                    await t2.SendActivityAsync(proactiveMessage, c2);
                                },
                                cancellationToken);
                        },
                        cancellationToken);
                }
            }
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(turnContext.Activity.Text))
            {
                dynamic value = turnContext.Activity.Value;
                if (value != null)
                {
                    string type = value["type"];
                    type = string.IsNullOrEmpty(type) ? "." : type.ToLower();
                    await SendReponse(turnContext, type, cancellationToken);
                }
            }
            else
            {
                await SendReponse(turnContext, turnContext.Activity.Text.Trim().ToLower(), cancellationToken);
            }
        }

        private async Task SendReponse(ITurnContext<IMessageActivity> turnContext, string input, CancellationToken cancellationToken)
        {
            var userList = await _graph.LoadUserGraphAsync();

            var user = userList.Where(x => x.DisplayName.Trim().ToLower().Contains(input) 
            || x.GivenName.Trim().ToLower().Contains(input) 
            || x.Surname.Trim().ToLower().Contains(input)).FirstOrDefault();

            if(user == null)
            {
                //Speech - Konuþacak... kullanýcý bulunamadý. diyecek.

                //burayý konuþturamýyoruz....
            }

            ///CALISMAYACAK
            switch (input)
            {
                case "generatefile":

                    var localFile = await GenerateTTSFile("Welcome");

                    //var sb = new StringBuilder();

                    //var ttsMedia = new MediaInfo
                    //{
                    //    Uri = new Uri(this._botOptions.BotBaseUrl, localFile).ToString(),
                    //    ResourceId = Guid.NewGuid().ToString(),
                    //};
                    //var ttsMediaPrompt = new MediaPrompt() { MediaInfo = ttsMedia, Loop = 1 };

                    //await this.Call.PlayPromptAsync(new List<MediaPrompt> { ttsMediaPrompt }).ConfigureAwait(false);

                    await turnContext.SendActivityAsync("File : " + localFile);

                    break;

                case "talk":

                    var msg = MessageFactory.Text($"Echo: TALK",
                        "<speak version=\"1.0\" xmlns=\"https://www.w3.org/2001/10/synthesis\" xmlns:mstts=\"https://www.w3.org/2001/mstts\" xml:lang=\"en-US\">" +
                        "<voice name=\"en-US-JennyNeural\">Hello World</voice>" +
                        "</speak>");
                    await turnContext.SendActivityAsync(msg);

                    break;

                case "downloadjson":

                    var message = MessageFactory.Text("Test", InputHints.IgnoringInput);
                    message.Attachments.Add(new Microsoft.Bot.Schema.Attachment
                    {
                        Name = "Test.json",
                        ContentType = "application/json",
                        ContentUrl = new Uri(this._botOptions.BotBaseUrl, "audio/test.json").ToString()
                    });

                    await turnContext.SendActivityAsync(message);

                    break;

                case "createcall":

                    var call = await this._graph.CreateCallAsync(user.Id);
                    if (call != null)
                    {
                        await turnContext.SendActivityAsync("Placed a call Successfully.");
                    }
                    break;

                case "transfercall":
                case "agent one":
                case "one":

                    var userAgent = userList.Where(x => x.DisplayName.Contains("agent one") || x.GivenName.Contains("agent") || x.Surname.Contains("one")).FirstOrDefault();

                    var sourceCallResponse = await this._graph.CreateCallAsync(userAgent.Id);
                    if (sourceCallResponse != null)
                    {
                        await turnContext.SendActivityAsync("Transferring the call!");
                        await this._graph.TransferCallAsync(sourceCallResponse.Id);
                    }
                    break;

                case "joinscheduledmeeting":
                    var onlineMeeting = await this._graph.CreateOnlineMeetingAsync();
                    if (onlineMeeting != null)
                    {
                        var statefullCall = await this._graph.JoinScheduledMeeting(onlineMeeting.JoinWebUrl);
                        if (statefullCall != null)
                        {
                            await turnContext.SendActivityAsync($"[Click here to Join the meeting]({onlineMeeting.JoinWebUrl})");
                        }
                    }
                    break;

                case "inviteparticipant":
                    var meeting = await this._graph.CreateOnlineMeetingAsync();
                    if (meeting != null)
                    {
                        var statefullCall = await this._graph.JoinScheduledMeeting(meeting.JoinWebUrl);
                        if (statefullCall != null)
                        {
                            this._graph.InviteParticipant(statefullCall.Id);
                            await turnContext.SendActivityAsync("Invited participant successfuly");
                        }
                    }
                    break;
                default:
                    await turnContext.SendActivityAsync("Welcome to bot");
                    break;
            }
        }


        private void NotificationProcessor_OnNotificationReceived(NotificationEventArgs args)
        {
            _ = NotificationProcessor_OnNotificationReceivedAsync(args).ForgetAndLogExceptionAsync(this._sentryHub,
              $"Error processing notification {args.Notification.ResourceUrl} with scenario {args.ScenarioId}");
        }

        private async Task NotificationProcessor_OnNotificationReceivedAsync(NotificationEventArgs args)
        {
            this._sentryHub.CaptureMessage($"CorrelationId :: {args.ScenarioId}");

            if (args.ResourceData is Call call)
            {
                if (args.ChangeType == ChangeType.Created && call.State == CallState.Incoming)
                {
                    await this.BotAnswerIncomingCallAsync(call.Id, args.TenantId, args.ScenarioId).ConfigureAwait(false);
                }
            }

        }

        public async Task<string> GenerateTTSFile(string message)
        {
            var accessToken = await GetAccessToken("7334556c6e21477ba4c885d298995123");
            var fileStream = await GetTTS(accessToken, message);

            var filename = Guid.NewGuid();
            using (var stream = System.IO.File.Create("wwwroot/audio/" + filename + ".wav"))
            {
                fileStream.Seek(0, SeekOrigin.Begin);
                fileStream.CopyTo(stream);
            }
            return "audio/" + filename + ".wav";
        }

        private async Task<string> GetAccessToken(string subscriptionKey)
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
            var response = await client.PostAsync("https://westus.api.cognitive.microsoft.com/sts/v1.0/issuetoken", null);
            return await response.Content.ReadAsStringAsync();
        }

        private async Task<Stream> GetTTS(string accessToken, string message)
        {
            var xmlMessage = string.Format("<speak version='1.0' xmlns='https://www.w3.org/2001/10/synthesis' xmlns:mstts='https://www.w3.org/2001/mstts' xml:lang='en-US'><voice xml:lang='en-US' name='en-US-JennyNeural'>{0}</voice></speak>", message);
            HttpClient client = new HttpClient();
            HttpContent content = new StringContent(xmlMessage);
      
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/ssml+xml");
            client.DefaultRequestHeaders.Add("X-Microsoft-OutputFormat", "riff-16khz-16bit-mono-pcm");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            client.DefaultRequestHeaders.Add("User-Agent", "Your App Name");

            var response = await client.PostAsync("https://speech.platform.bing.com/synthesize", content);
            return await response.Content.ReadAsStreamAsync();
        }

        private async Task BotAnswerIncomingCallAsync(string callId, string tenantId, Guid scenarioId)
        {
            //var call = this._client.Calls().Where(x => x.Id == callId).FirstOrDefault();

            var localFile = await GenerateTTSFile("Welcome");

            //call.PlayPromptAsync();

            Task answerTask = Task.Run(async () =>
                                await this._graphServiceClient.Communications.Calls[callId].Answer(
                                    callbackUri: new Uri(this._botOptions.BotBaseUrl, "callback").ToString(),
                                    mediaConfig: new ServiceHostedMediaConfig
                                    {
                                        PreFetchMedia = new List<MediaInfo>()
                                        {
                                              new MediaInfo()
                                              {
                                                  Uri = new Uri(this._botOptions.BotBaseUrl, localFile).ToString(),
                                                  ResourceId = Guid.NewGuid().ToString(),
                                              }
                                        }
                                    },
                                    acceptedModalities: new List<Modality> { Modality.Audio }).Request().PostAsync()
                                 );

            await answerTask.ContinueWith(async (antecedent) =>
            {
                if (antecedent.Status == System.Threading.Tasks.TaskStatus.RanToCompletion)
                {
                    await Task.Delay(5000);
                    await this._graphServiceClient.Communications.Calls[callId].PlayPrompt(
                       prompts: new List<Microsoft.Graph.Prompt>()
                       {
                             new MediaPrompt
                             {
                                 MediaInfo = new MediaInfo
                                 {
                                     Uri = new Uri(this._botOptions.BotBaseUrl, localFile).ToString(),
                                     ResourceId = Guid.NewGuid().ToString(),
                                 }
                             }
                       }).Request().PostAsync();
                }
            }
          );

        }
    }
}

