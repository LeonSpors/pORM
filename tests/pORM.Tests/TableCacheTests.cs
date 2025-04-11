using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using NUnit.Framework;
using pORM.Core.Caching;
using pORM.Core.Interfaces;
using pORM.Core.Models;
using pORM.Tests.Models;

namespace pORM.Tests
{
    [TestFixture]
    public class TableCacheTests
    {
        private TableCache _tableCache;

        [SetUp]
        public void Setup()
        {
            _tableCache = new TableCache();
        }
        
        // ----------------------
        // GetItems<T> Tests
        // ----------------------
        [Test]
        public void GetItems_ReturnsPropertiesExcludingNotMapped()
        {
            IReadOnlyList<TableCacheItem> items = _tableCache.GetItems<TestEntity>();

            Assert.That(items.Count, Is.EqualTo(2));
            Assert.That(items.Any(item => item.Metadata.Name == "Ignored"), Is.False);
        }

        [Test]
        public void GetItems_CachesResults()
        {
            IReadOnlyList<TableCacheItem> firstCall = _tableCache.GetItems<TestEntity>();
            IReadOnlyList<TableCacheItem> secondCall = _tableCache.GetItems<TestEntity>();

            Assert.That(object.ReferenceEquals(firstCall, secondCall), Is.True);
        }

        // ----------------------
        // GetItem(PropertyInfo) Tests
        // ----------------------
        [Test]
        public void GetItem_WithValidProperty_ReturnsMapping()
        {
            PropertyInfo idProp = typeof(TestEntity).GetProperty("Id")!;

            TableCacheItem mapping = _tableCache.GetItem(idProp);

            Assert.That(mapping, Is.Not.Null);
            Assert.That(mapping.Metadata, Is.EqualTo(idProp));
        }

        [Test]
        public void GetItem_WithNotMappedProperty_ThrowsInvalidOperationException()
        {
            PropertyInfo ignoredProp = typeof(TestEntity).GetProperty("Ignored")!;

            InvalidOperationException? ex = Assert.Throws<InvalidOperationException>(
                () => _tableCache.GetItem(ignoredProp)
            );
            
            Assert.That(ex?.Message, Is.EqualTo(
                $"No mapping found for property '{ignoredProp.Name}' on type '{ignoredProp.DeclaringType?.Name}'."
            ));
        }

        [Test]
        public void GetItem_NullProperty_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _tableCache.GetItem(null));
        }

        // ----------------------
        // GetKeyItem<T> Tests
        // ----------------------
        [Test]
        public void GetKeyItem_WithEntityHavingKey_ReturnsKeyMapping()
        {
            TableCacheItem keyMapping = _tableCache.GetKeyItem<TestEntity>();

            Assert.That(keyMapping, Is.Not.Null);
            Assert.That(keyMapping.Metadata.Name, Is.EqualTo("Id"));
            Assert.That(keyMapping.IsKey, Is.True);
        }

        [Test]
        public void GetKeyItem_WithEntityWithoutKey_ThrowsInvalidOperationException()
        {
            InvalidOperationException? ex = Assert.Throws<InvalidOperationException>(
                () => _tableCache.GetKeyItem<TestEntityNoKey>()
            );
            
            Assert.That(ex?.Message, Is.EqualTo($"No primary key defined on type {nameof(TestEntityNoKey)}"));
        }
    }
}
