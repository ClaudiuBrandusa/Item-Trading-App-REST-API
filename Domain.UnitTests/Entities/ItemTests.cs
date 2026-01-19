using Domain.Entities.Items;

namespace Domain.UnitTests.Entities
{
    public class ItemTests
    {
        [Fact(DisplayName = "Create a new item with valid data")]
        public void Constructor_CreateNewWithWithValidData_ItemCreated()
        {
            // Arrange

            const string expectedName = "Amethyst";
            const string expectedDescription = "Violet variation of quartz";

            // Act

            var item = new Item(expectedName, expectedDescription);

            // Assert

            Assert.NotNull(item);
            Assert.True(!string.IsNullOrEmpty(item.ItemId));
            Assert.Equal(expectedName, item.Name);
            Assert.Equal(expectedDescription, item.Description);
        }

        [Fact(DisplayName = "Create a new item without description")]
        public void Constructor_CreateNewWithWithoutDescription_ItemCreated()
        {
            // Arrange

            const string expectedName = "Amethyst";

            // Act

            var item = new Item(expectedName);

            // Assert

            Assert.NotNull(item);
            Assert.True(!string.IsNullOrEmpty(item.ItemId));
            Assert.Equal(expectedName, item.Name);
            Assert.Empty(item.Description);
        }

        [Fact(DisplayName = "Attempt to create a new item without name (name is null)")]
        public void Constructor_CreateNewWithoutName_ThrowsException()
        {
            // Arrange

            string? expectedName = null;
            Item? item = null;

            // Act

            var action = () => item = new Item(expectedName);

            // Assert

            Assert.Throws<ArgumentNullException>(action);
            Assert.Null(item);
        }

        [Fact(DisplayName = "Attempt to create a new item without name (name is empty)")]
        public void Constructor_CreateNewWithEmptyName_ThrowsException()
        {
            // Arrange

            string expectedName = string.Empty;
            Item? item = null;

            // Act

            var action = () => item = new Item(expectedName);

            // Assert

            Assert.Throws<ArgumentException>(action);
            Assert.Null(item);
        }

        [Fact(DisplayName = "Attempt to create a new item with a name that is too short")]
        public void Constructor_CreateNewWithTooShortName_ThrowsException()
        {
            // Arrange

            string expectedName = "A";
            Item? item = null;

            // Act

            var action = () => item = new Item(expectedName);

            // Assert

            Assert.Throws<ArgumentException>(action);
            Assert.Null(item);
        }

        [Fact(DisplayName = "Create a new item with valid data")]
        public void UpdateItemName_UpdateItemNameWithValidData_ItemUpdated()
        {
            // Arrange

            const string expectedName = "Amethyst";
            const string expectedDescription = "Violet variation of quartz";
            const string updatedName = $"{expectedName}_Updated";

            var item = new Item(expectedName, expectedDescription);

            // Act

            item.UpdateItemName(updatedName);

            // Assert

            Assert.Equal(updatedName, item.Name);
            Assert.Equal(expectedDescription, item.Description);
        }

        [Fact(DisplayName = "Attempt to update the item name with an invalid name (name is null)")]
        public void UpdateItemName_UpdateItemNameWithNullName_ThrowsException()
        {
            // Arrange

            const string expectedName = "Lead";
            Item item = new Item(expectedName);
            const string? updatedName = null;

            // Act

            var action = () => item.UpdateItemName(updatedName);

            // Assert

            Assert.Throws<ArgumentNullException>(action);
            Assert.Equal(expectedName, item.Name);
        }

        [Fact(DisplayName = "Attempt to update the item name with an invalid name (name is empty)")]
        public void UpdateItemName_UpdateItemNameWithEmptyName_ThrowsException()
        {
            // Arrange

            const string expectedName = "Lead";
            Item item = new Item(expectedName);
            string updatedName = string.Empty;

            // Act

            var action = () => item.UpdateItemName(updatedName);

            // Assert

            Assert.Throws<ArgumentException>(action);
            Assert.Equal(expectedName, item.Name);
        }

        [Fact(DisplayName = "Attempt to update the item name with a name that is too short")]
        public void UpdateItemName_UpdateItemNameWithTooShortName_ThrowsException()
        {
            // Arrange

            const string expectedName = "Lead";
            Item item = new Item(expectedName);
            const string updatedName = "A";

            // Act

            var action = () => item.UpdateItemName(updatedName);

            // Assert

            Assert.Throws<ArgumentException>(action);
            Assert.Equal(expectedName, item.Name);
        }
    }
}