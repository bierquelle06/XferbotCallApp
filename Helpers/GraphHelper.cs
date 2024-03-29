﻿// <copyright file="GraphHelper.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace CallingBotSample.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Runtime.Serialization.Json;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using CallingBotSample.Configuration;
    using CallingBotSample.Interfaces;
    using CallingBotSample.Utility;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.Graph;
    using Microsoft.Graph.Auth;
    using Microsoft.Graph.Communications.Calls;
    using Microsoft.Identity.Client;

    /// <summary>
    /// Helper class for Graph.
    /// </summary>
    public class GraphHelper : IGraph
    {
        private readonly ILogger<GraphHelper> logger;
        private readonly IConfiguration configuration;
        private readonly IEnumerable<Configuration.User> users;
        private readonly IGraphServiceClient graphServiceClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphHelper"/> class.
        /// </summary>
        /// <param name="httpClientFactory">IHttpClientFactory instance.</param>
        /// <param name="logger">ILogger instance.</param>
        /// <param name="configuration">IConfiguration instance.</param>
        public GraphHelper(ILogger<GraphHelper> logger, IConfiguration configuration, IOptions<Configuration.Users> users, IGraphServiceClient graphServiceClient)
        {
            this.logger = logger;
            this.configuration = configuration;
            this.graphServiceClient = graphServiceClient;
            this.users = LoadUserGraphAsync().GetAwaiter().GetResult();//configuration.GetSection("Users").Get<Configuration.User[]>().AsEnumerable();
        }

        public async Task<List<Configuration.User>> LoadUserGraphAsync()
        {
            List<Configuration.User> result = new List<Configuration.User>();

            try
            {
                var graphResult = await graphServiceClient.Users.Request().GetAsync();

                for (int i = 0; i < graphResult.Count(); i++)
                {
                    var graphItem = graphResult[i];

                    result.Add(new Configuration.User()
                    {
                        Id = graphItem.Id,
                        DisplayName = CommonUtils.RemoveNonAlphaNumeric(graphItem.DisplayName ?? ""),
                        GivenName = CommonUtils.RemoveNonAlphaNumeric(graphItem.GivenName ?? ""),
                        Language = graphItem.PreferredLanguage ?? "en-US",
                        MobilePhone = graphItem.MobilePhone ?? "",
                        OfficeLocation = graphItem.OfficeLocation ?? "",
                        Surname = CommonUtils.RemoveNonAlphaNumeric(graphItem.Surname ?? "")
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, ex.Message);
                return null;
            }
        }

        /// <inheritdoc/>
        public async Task<OnlineMeeting> CreateOnlineMeetingAsync()
        {
            try
            {
                var onlineMeeting = new OnlineMeeting
                {
                    StartDateTime = DateTime.UtcNow,
                    EndDateTime = DateTime.UtcNow.AddMinutes(30),
                    Subject = "Calling bot meeting",
                };

                var onlineMeetingResponse = await graphServiceClient.Users[this.configuration[Common.Constants.UserIdConfigurationSettingsKey]].OnlineMeetings
                           .Request()
                           .AddAsync(onlineMeeting);
                return onlineMeetingResponse;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, ex.Message);
                return null;
            }
        }

        public async Task<Call> CreateCallAsync(string id)
        {
            var user = this.users.Where(x => x.Id == id).FirstOrDefault();

            var call = new Call
            {
                CallbackUri = $"{this.configuration[Common.Constants.BotBaseUrlConfigurationSettingsKey]}/callback",
                TenantId = this.configuration[Common.Constants.TenantIdConfigurationSettingsKey],
                Targets = new List<InvitationParticipantInfo>()
                    {
                        new InvitationParticipantInfo
                        {
                            Identity = new IdentitySet
                            {
                                User = new Identity
                                {
                                    DisplayName = user.DisplayName,
                                    Id = user.Id
                                }
                            }
                        }
                    },
                RequestedModalities = new List<Modality>()
                    {
                        Modality.Audio
                    },
                MediaConfig = new ServiceHostedMediaConfig
                {
                }
            };

            return await graphServiceClient.Communications.Calls
                .Request()
                .AddAsync(call);
        }

        public async Task TransferCallAsync(string replaceCallId)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(15000);
                var transferTarget = new InvitationParticipantInfo
                {
                    Identity = new IdentitySet
                    {
                        User = new Identity
                        {
                            DisplayName = this.users.ElementAt(1).DisplayName,
                            Id = this.users.ElementAt(1).Id
                        }
                    },
                    AdditionalData = new Dictionary<string, object>()
                         {
                            {"endpointType", "default"}
                         },
                    //ReplacesCallId = targetCallResponse.Id
                };

                try
                {
                    await graphServiceClient.Communications.Calls[replaceCallId]
                        .Transfer(transferTarget)
                        .Request()
                        .PostAsync();
                }
                catch (System.Exception ex)
                {

                    throw ex;
                }
            });
        }

        public async Task<Call> JoinScheduledMeeting(string meetingUrl)
        {
            try
            {
                MeetingInfo meetingInfo;
                ChatInfo chatInfo;

                (chatInfo, meetingInfo) = JoinInfo.ParseJoinURL(meetingUrl);

                var call = new Call
                {
                    CallbackUri = $"{this.configuration[Common.Constants.BotBaseUrlConfigurationSettingsKey]}/callback",
                    RequestedModalities = new List<Modality>()
                    {
                        Modality.Audio
                    },
                    MediaConfig = new ServiceHostedMediaConfig
                    {
                    },
                    ChatInfo = chatInfo,
                    MeetingInfo = meetingInfo,
                    TenantId = (meetingInfo as OrganizerMeetingInfo)?.Organizer.GetPrimaryIdentity()?.GetTenantId()
                };

                var statefulCall = await graphServiceClient.Communications.Calls
                        .Request()
                        .AddAsync(call);

                return statefulCall;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public void InviteParticipant(string meetingId)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(10000);

                try
                {
                    var participants = new List<InvitationParticipantInfo>()
                {
                    new InvitationParticipantInfo
                    {
                        Identity = new IdentitySet
                        {
                            User = new Identity
                            {
                                DisplayName = this.users.ElementAt(2).DisplayName,
                                Id = this.users.ElementAt(2).Id
                            }
                        }
                    }
                };

                    var statefulCall = await graphServiceClient.Communications.Calls[meetingId].Participants
                       .Invite(participants)
                       .Request()
                       .PostAsync();
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            });
        }

    }
}
