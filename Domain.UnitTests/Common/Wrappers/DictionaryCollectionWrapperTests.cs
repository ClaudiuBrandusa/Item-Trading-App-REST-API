using Domain.Common.Wrappers;

namespace Domain.UnitTests.Common.Wrappers;

public class DictionaryCollectionWrapperTests
{
    public class MockEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
    }

    [Fact(DisplayName = "Adds an entity to the dictionary and it should contain the entity")]
    public void Add_AddsAnEntityToTheDictionary_DictionaryShouldContainTheEntity()
    {
        // Arrange

        var wrapper = CreateDictionaryUnderTest();

        var mockEntity = new MockEntity();

        string key = mockEntity.Id;

        // Act

        wrapper.Add(mockEntity);

        // Assert

        Assert.True(wrapper.ContainsKey(key));
        Assert.Contains(mockEntity, wrapper);
    }

    [Fact(DisplayName = "Adds an entity to the dictionary then removes it, the entity should no longer be in the dictionary")]
    public void Remove_AddsAnEntityToTheDictionaryAndThenRemovesIt_DictionaryShouldRemoveTheEntitySuccessfully()
    {
        // Arrange

        var wrapper = CreateDictionaryUnderTest();

        var mockEntity = new MockEntity();

        string key = mockEntity.Id;

        // Act

        wrapper.Add(mockEntity);
        bool added = wrapper.Contains(mockEntity);
        bool removed = wrapper.Remove(mockEntity);

        // Assert

        Assert.True(added);
        Assert.True(removed);
        Assert.DoesNotContain(mockEntity, wrapper);
    }

    [Fact(DisplayName = "Adds an entity to the dictionary then removes it (by key), the entity should no longer be in the dictionary")]
    public void Remove_AddsAnEntityToTheDictionaryAndThenRemovesItByKey_DictionaryShouldRemoveTheEntitySuccessfully()
    {
        // Arrange

        var wrapper = CreateDictionaryUnderTest();

        var mockEntity = new MockEntity();

        string key = mockEntity.Id;

        // Act

        wrapper.Add(mockEntity);
        bool added = wrapper.ContainsKey(key);
        bool removed = wrapper.Remove(key);

        // Assert

        Assert.True(added);
        Assert.True(removed);
        Assert.DoesNotContain(mockEntity, wrapper);
    }

    [Fact(DisplayName = "Adds an entity then clears the dictionary, should have an empty dictionary")]
    public void Clear_AddsAnEntityThenClearsTheDictionary_ShouldHaveAnEmptyDictionary()
    {
        // Arrange

        var wrapper = CreateDictionaryUnderTest();

        var mockEntity = new MockEntity();

        // Act

        wrapper.Add(mockEntity);
        wrapper.Clear();

        // Assert

        Assert.True(wrapper.Count == 0);
    }

    [Fact(DisplayName = "Copy the values of the dictionary to a given array, then that array should have the same elements as the dictionary")]
    public void CopyTo_CopyTheStoredValuesToAGivenArray_TheGivenArrayHasTheSameContentAsTheDictionary()
    {
        // Arrange

        const int expectedAmountOfEntities = 5;

        var wrapper = CreateDictionaryUnderTest();

        for (int i = 0; i < expectedAmountOfEntities; i++)
            wrapper.Add(new MockEntity());

        var resultArray = new MockEntity[expectedAmountOfEntities];

        // Act

        wrapper.CopyTo(resultArray, 0);

        // Assert

        Assert.Equal(expectedAmountOfEntities, wrapper.Count);
        Assert.Distinct(resultArray);
        Assert.All(wrapper, x => resultArray.Contains(x));
    }

    [Fact(DisplayName = "Add several entities to the dictionary then enumerate through all of those entities, should enumerate all of the elements")]
    public void GetEnumerator_AddSeveralEntitiesThenGetTheEnumerator_ShouldEnumerateThroughAllOfTheEntities()
    {
        // Arrange

        const int expectedAmountOfEntities = 5;

        var wrapper = CreateDictionaryUnderTest();

        for (int i = 0; i < expectedAmountOfEntities; i++)
            wrapper.Add(new MockEntity());

        // Act

        var resultList = new List<MockEntity>();

        using (var enumerator = wrapper.GetEnumerator())
        {
            while (enumerator.MoveNext())
            {
                resultList.Add(enumerator.Current);
            }
        }

        // Assert

        Assert.Equal(expectedAmountOfEntities, resultList.Count);
        Assert.Distinct(resultList);
        Assert.All(wrapper, x => resultList.Contains(x));
    }

    [Fact(DisplayName = "Add several entities then get those entities by their id, should return the correct entity by their id")]
    public void Indexing_AddSeveralEntitiesThenGetThemByIndex_ReturnsTheCorrectEntityByIndex()
    {
        // Arrange

        var wrapper = CreateDictionaryUnderTest();

        var mockEntity0 = new MockEntity();
        var mockEntity1 = new MockEntity();

        // Act

        wrapper.Add(mockEntity0);
        wrapper.Add(mockEntity1);

        var firstEntity = wrapper[mockEntity0.Id];
        var secondEntity = wrapper[mockEntity1.Id];

        // Assert

        Assert.Equal(mockEntity0, firstEntity);
        Assert.Equal(mockEntity1, secondEntity);
    }

    [Fact(DisplayName = "Add entity to the dictionary then try to get value (by id), should return the correct entity")]
    public void TryGetValue_AddEntityThenTryGetValue_ShouldReturnTheCorrectEntityForTheGivenKey()
    {
        // Arrange

        var wrapper = CreateDictionaryUnderTest();

        var expectedEntity = new MockEntity();

        wrapper.Add(expectedEntity);

        // Act

        bool operationResult = wrapper.TryGetValue(expectedEntity.Id, out var result);

        // Assert

        Assert.True(operationResult);
        Assert.Equal(expectedEntity, result);
    }

    private DictionaryCollectionWrapper<string, MockEntity> CreateDictionaryUnderTest() => new DictionaryCollectionWrapper<string, MockEntity>(x => x.Id);
}
