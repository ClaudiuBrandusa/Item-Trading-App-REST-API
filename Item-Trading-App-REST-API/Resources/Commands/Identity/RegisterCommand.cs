namespace Item_Trading_App_REST_API.Resources.Commands.Identity;

public record RegisterCommand : LoginCommand
{
    public string Email { get; set; }
}
