namespace Item_Trading_App_REST_API.Models.Identity;

public record ListUsers
{
    public string SearchString { get; set; }

    public string UserId { get; set; }
}
