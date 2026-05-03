namespace Web.API.IntegrationTests.Common;

public static class Constants
{
    public const string DefaultTestUserName = "root";
    public const string DefaultTestUserRole = "admin";
    
    public const string UserIdRequestHeaderName = "x-user-id";
    public const string UserNameRequestHeaderName = "x-test-user";
    public const string UserRoleRequestHeaderName = "x-test-role";

    public const string NotifyEndpoint = "notify";

    public const string DefaultHubNotificationUrl = "http://localhost/hubs/notification";
}
