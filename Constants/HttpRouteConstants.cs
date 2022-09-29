using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CallingBotSample.Constants
{
    public class HttpRouteConstants
    {
        /// <summary>
        /// Route prefix for all incoming requests.
        /// </summary>
        public const string CallSignalingRoutePrefix = "api/calling";

        /// <summary>
        /// Route for incoming call requests.
        /// </summary>
        public const string OnIncomingRequestRoute = "api/callback";

        /// <summary>
        /// Route for incoming notification requests.
        /// </summary>
        public const string OnNotificationRequestRoute = "notification";

        /// <summary>
        /// 
        /// </summary>
        public const string MessagesRequestRoute = "api/messages";

        /// <summary>
        /// The logs route for GET.
        /// </summary>
        public const string Logs = "logs";

        /// <summary>
        /// The calls route for both GET and POST.
        /// </summary>
        public const string Calls = "calls";

        /// <summary>
        /// The route for join call.
        /// </summary>
        public const string JoinCall = "joinCall";

        /// <summary>
        /// The route for getting the call.
        /// </summary>
        public const string CallRoute = Calls + "/{callLegId}";

        /// <summary>
        /// Route for incoming call requests.
        /// </summary>
        public const string HealthRoute = "health";
    }
}
