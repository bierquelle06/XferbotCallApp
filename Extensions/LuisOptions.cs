namespace Microsoft.AspNetCore.Authentication
{
    /// <summary>
    /// The Luis options class.
    /// </summary>
    public class LuisOptions
    {
        /// <summary>
        /// Gets or sets the application id as auth client id.
        /// </summary>
        public string AppId { get; set; }

        /// <summary>
        /// Gets or sets the application secret as auth client secret.
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// Gets or sets the instance.
        /// </summary>
        public string ApiEndpointUrl { get; set; }
    }
}
