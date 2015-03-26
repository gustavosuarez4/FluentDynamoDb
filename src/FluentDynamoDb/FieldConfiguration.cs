using System;
using System.Collections.Generic;
using Amazon.DynamoDBv2.DataModel;

namespace FluentDynamoDb
{
    public class FieldConfiguration : IFieldConfiguration
    {
        public FieldConfiguration()
        {
            FieldConfigurations = new List<IFieldConfiguration>();
        }

        public Type Type { get; set; }
        public string PropertyName { get; set; }
        public bool IsComplexType { get; set; }
        public ICollection<IFieldConfiguration> FieldConfigurations { get; set; }
        public IPropertyConverter PropertyConverter { get; set; }
    }
}