using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CallingBotSample.Model
{
    public enum CallType
    {


        /// <summary>
        /// The call is an incoming call to the bot.
        /// </summary>
        BotIncoming,

        /// <summary>
        /// The call is an incoming call to a bot endpoint.
        /// </summary>
        BotEndpointIncoming,

    }
}
