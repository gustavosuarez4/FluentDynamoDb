﻿using FluentDynamoDb.Mappers;
using Moq;
using NUnit.Framework;

namespace FluentDynamoDb.Tests
{
    [TestFixture]
    public class ClassMapStringTests : ClassMapBase
    {
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            var fooMap = new FooMap(DynamoDbRootEntityConfiguration, DynamoDbMappingConfigurationFake.Object);
        }

        [Test]
        public void Map_WhenMappingFooName_AddFieldConfigurationShouldBeCalled()
        {
            DynamoDbMappingConfigurationFake.Verify(c => c.AddFieldConfiguration(It.IsAny<FieldConfiguration>()),
                Times.Once);
        }

        [Test]
        public void Map_WhenMappingFooNameWithIsAString_ShouldCreateAFieldConfigurationAsExpected()
        {
            Assert.AreEqual("FooString", CurrentFieldConfiguration.PropertyName);
            Assert.AreEqual(typeof (string), CurrentFieldConfiguration.Type);
        }

        public class Foo
        {
            public string FooString { get; set; }
        }

        public class FooMap : ClassMap<Foo>
        {
            public FooMap(DynamoDbRootEntityConfiguration dynamoDbRootEntityConfiguration,
                DynamoDbEntityConfiguration dynamoDbEntityConfiguration)
                : base(dynamoDbRootEntityConfiguration, dynamoDbEntityConfiguration)
            {
                Map(c => c.FooString);
            }
        }
    }
}