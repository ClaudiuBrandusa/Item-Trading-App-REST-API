namespace Item_Trading_App_REST_API.Models.Identity;

public record Register : Login
{
    public string Email { get; set; }
}
