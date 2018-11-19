using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using FluentDynamoDb.Mappers;
using System.Threading.Tasks;

namespace FluentDynamoDb
{
    public class DynamoDbStore<TEntity, TKey> : DynamoDbStoreBase, IDynamoDbStore<TEntity, TKey>
        where TEntity : class, new()
    {
        private readonly IAmazonDynamoDB _amazonDynamoDbClient;
        private readonly Table _entityTable;
        private readonly DynamoDbMapper<TEntity> _mapper;

        public DynamoDbStore()
        {
            var rootConfiguration = LoadConfiguration<TEntity>();

            _amazonDynamoDbClient = new AmazonDynamoDBClient();
            _entityTable = Table.LoadTable(_amazonDynamoDbClient, rootConfiguration.TableName);
            _mapper = new DynamoDbMapper<TEntity>(rootConfiguration.DynamoDbEntityConfiguration);
        }

        public async Task<TEntity> GetItem(TKey id)
        {
            dynamic idValue = id;
            var document = await _entityTable.GetItemAsync(idValue);
            return _mapper.ToEntity(document);
        }

        public async Task<TEntity> DeleteItem(TKey id)
        {
            dynamic idValue = id;
            var deletedDocument = await _entityTable.DeleteItemAsync(idValue, new DeleteItemOperationConfig
            {
                ReturnValues = ReturnValues.AllOldAttributes
            });

            return _mapper.ToEntity(deletedDocument);
        }

        public async Task<TEntity> UpdateItem(TEntity entity)
        {
            var document = _mapper.ToDocument(entity);

            var updatedDocument = await _entityTable.UpdateItemAsync(document, new UpdateItemOperationConfig
            {
                ReturnValues = ReturnValues.AllNewAttributes
            });

            return _mapper.ToEntity(updatedDocument);
        }

        public async Task PutItem(TEntity entity)
        {
            await _entityTable.PutItemAsync(_mapper.ToDocument(entity));
        }

        public void Dispose()
        {
            _amazonDynamoDbClient.Dispose();
        }
    }
}