using Microsoft.Graph.Communications.Common.Telemetry;
using Sentry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CallingBotSample.Utility
{
    public static class CommonUtils
    {
        public static async Task ForgetAndLogExceptionAsync(
            this Task task,
            IHub sentryHub,
            string description = null,
            [CallerMemberName] string memberName = null,
            [CallerFilePath] string filePath = null,
            [CallerLineNumber] int lineNumber = 0)
        {
            try
            {
                await task.ConfigureAwait(false);

                sentryHub.CaptureMessage($"Completed running task successfully: {description ?? string.Empty}" +
                    $"memberName: {memberName}" +
                    $"filePath: {filePath}" +
                    $"lineNumber: {lineNumber}");
            }
            catch (Exception ex)
            {
                sentryHub.CaptureException(ex);

                sentryHub.CaptureMessage($"Completed running task successfully: {description ?? string.Empty}" +
                   $"memberName: {memberName}" +
                   $"filePath: {filePath}" +
                   $"lineNumber: {lineNumber}");
            }
        }
    }
}
