using pORM.Core.Caching;
using pORM.Core.Models;
using pORM.Tests.Models;

namespace pORM.Tests;

public class TableCacheTests
{
    [Fact]
    public void GetItems_ReturnsMapping_WithPrimaryKeyMarked()
    {
        // Arrange
        TableCache cache = new TableCache();

        // Act: Get mapping for TestEntity.
        var mappings = cache.GetItems<TestEntity>();

        // Assert: Verify that there's at least one mapping and that the key property is flagged.
        Assert.NotEmpty(mappings);
        TableCacheItem? keyMapping = mappings.FirstOrDefault(item => item.Metadata.Name == "Id");
        Assert.NotNull(keyMapping);
        Assert.True(keyMapping.IsKey);
    }

    [Fact]
    public void GetKeyItem_Throws_IfNoKeyDefined()
    {
        // Arrange: Create a dummy entity with no property marked as key.
        Assert.Throws<InvalidOperationException>(() =>
        {
            // Define an inline type with no key attribute.
            TableCacheItem keyItem = new TableCache().GetKeyItem<NoKeyEntity>();
        });
    }

    // Dummy entity with no key property for testing.
    private class NoKeyEntity
    {
        public int Value { get; set; }
    }
}