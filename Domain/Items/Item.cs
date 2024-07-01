using System.ComponentModel.DataAnnotations;

namespace Domain.Items;

public class Item
{
    [Key]
    public string ItemId { get; set; }

    [Required]
    public string Name { get; set; }

    public string Description { get; set; }
}
