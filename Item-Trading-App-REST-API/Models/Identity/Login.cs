namespace Item_Trading_App_REST_API.Models.Identity;

public record Login
{
    public string Username { get; set; }

    public string Password { get; set; }
}
