using System.ComponentModel.DataAnnotations;

namespace Item_Trading_App_REST_API.Entities;

public class Item
{
    [Key]
    public string ItemId { get; set; }

    [Required]
    public string Name { get; set; }

    public string Description { get; set; }
}
