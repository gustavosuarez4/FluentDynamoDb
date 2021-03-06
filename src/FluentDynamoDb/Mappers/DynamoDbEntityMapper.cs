﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Amazon.DynamoDBv2.DocumentModel;
using FluentDynamoDb.Exceptions;
using FluentDynamoDb.Extensions;

namespace FluentDynamoDb.Mappers
{
    public class DynamoDbMapper<TEntity>
        where TEntity : class, new()
    {
        private readonly DynamoDbEntityConfiguration _configuration;

        protected Dictionary<Type, Func<DynamoDBEntry, dynamic>> MappingFromType = new Dictionary
            <Type, Func<DynamoDBEntry, dynamic>>
        {
            {typeof (string), value => value.AsString()},
            {typeof (Guid), value => value.AsGuid()},
            {typeof (decimal), value => value.AsDecimal()},
            {typeof (bool), value => value.AsBoolean()},
            {typeof (int), value => value.AsInt()},
            {typeof (DateTime), value => value.AsDateTime()},
            {typeof (IEnumerable<string>), value => value.AsListOfString()}
        };

        public DynamoDbMapper(DynamoDbEntityConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Document ToDocument(TEntity entity)
        {
            return ToDocument(entity, _configuration.Fields);
        }

        private Document ToDocument(object entity, IEnumerable<FieldConfiguration> fields)
        {
            var document = new Document();

            foreach (var field in fields)
            {
                if (field.IsComplexType)
                {
                    var value = GetPropertyValue(entity, field);
                    if (value == null) continue;

                    if (IsEnumerable(field))
                    {
                        document[field.PropertyName] = CreateDocumentList(value, field.FieldConfigurations);
                    }
                    else
                    {
                        document[field.PropertyName] = ToDocument(value, field.FieldConfigurations);
                    }
                }
                else
                {
                    dynamic value = GetPropertyValue(entity, field);

                    if (field.PropertyConverter != null)
                    {
                        value = field.PropertyConverter.ToEntry(value);
                    }

                    document[field.PropertyName] = value;
                }
            }

            return document;
        }

        private static object GetPropertyValue(object entity, FieldConfiguration field)
        {
            return entity.GetType().GetProperty(field.PropertyName).GetValue(entity, null);
        }

        private List<Document> CreateDocumentList(object value, IEnumerable<FieldConfiguration> configuration)
        {
            return (from object item in (IEnumerable)value select ToDocument(item, configuration)).ToList();
        }

        private static bool IsEnumerable(FieldConfiguration field)
        {
            return field.Type.IsGenericType && field.Type.GetGenericTypeDefinition() == typeof(IEnumerable<>);
        }

        public TEntity ToEntity(Document document)
        {
            return (TEntity)ToEntity(document, _configuration.Fields, typeof(TEntity));
        }

        private object ToEntity(Document document, IEnumerable<FieldConfiguration> fields, Type type)
        {
            if (document == null) return null;

            var entity = Activator.CreateInstance(type);

            foreach (var field in fields)
            {
                if (field.IsComplexType)
                {
                    if (!document.ContainsKey(field.PropertyName)) continue;
                    var dbEntry = document[field.PropertyName];
                    if (dbEntry == null) continue;

                    if (IsEnumerable(field))
                    {
                        var itemType = field.Type.GetGenericArguments()[0];
                        var list = CreateListOf(itemType);

                        foreach (var dbEntryItem in dbEntry.AsListOfDocument())
                        {
                            var itemDocument = dbEntryItem.AsDocument();
                            var itemValue = ToEntity(itemDocument, field.FieldConfigurations, itemType);
                            list.Add(itemValue);
                        }

                        if (field.AccessStrategy == AccessStrategy.CamelCaseUnderscoreName)
                        {
                            var backingFieldName = field.PropertyName.ConvertToCamelCaseUnderscore();
                            var backingField = entity.GetType().GetField(backingFieldName, BindingFlags.NonPublic | BindingFlags.Instance);
                            if (backingField == null)
                            {
                                throw new FluentDynamoDbMappingException(string.Format("Could not find backing field named {0} of type {1}", backingFieldName, entity.GetType()));
                            }

                            backingField.SetValue(entity, list);
                        }
                        else
                        {
                            SetPropertyValue(entity, field.PropertyName, list);
                        }
                    }
                    else
                    {
                        var value = ToEntity(dbEntry.AsDocument(), field.FieldConfigurations, field.Type);
                        SetPropertyValue(entity, field.PropertyName, value);
                    }
                }
                else
                {
                    if (field.PropertyConverter != null)
                    {
                        var value = field.PropertyConverter.FromEntry(document[field.PropertyName]);
                        SetPropertyValue(entity, field.PropertyName, value);
                    }
                    else
                    {
                        SetPropertyValue(entity, field.PropertyName,
                            MappingFromType[field.Type](document[field.PropertyName]));
                    }
                }
            }

            return entity;
        }

        private static void SetPropertyValue(object instance, string propertyName, object value)
        {
            instance.GetType().GetProperty(propertyName).SetValue(instance, value);
        }

        private static IList CreateListOf(Type itemType)
        {
            var listType = typeof(List<>);
            var constructedListType = listType.MakeGenericType(itemType);
            return (IList)Activator.CreateInstance(constructedListType);
        }
    }
}