using Microsoft.Graph.Communications.Common.Telemetry;
using Sentry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
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

        public static string RemoveNonAlphaNumeric(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            StringBuilder sb = new StringBuilder();
            foreach (var c in text)
            {
                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))
                    sb.Append(c);
            }

            return sb.ToString().Trim().ToLower();
        }
    }
}
