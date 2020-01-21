namespace OpenAPIService
{
    public static class GraphConstants
    {
        public static readonly string GraphAuthorizationUrl = "https://login.microsoftonline.com/common/oauth2/v2.0/authorize";
        public static readonly string GraphTokenUrl = "https://login.microsoftonline.com/common/oauth2/v2.0/token";
        public static readonly string GraphUrl = "https://graph.microsoft.com/{0}/";
    }
}
