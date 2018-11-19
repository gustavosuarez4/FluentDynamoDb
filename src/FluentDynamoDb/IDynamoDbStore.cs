using System;
using System.Threading.Tasks;

namespace FluentDynamoDb
{
    public interface IDynamoDbStore<TEntity, in TKey> : IDisposable
        where TEntity : class, new()
    {
        Task<TEntity> GetItem(TKey id);
        Task PutItem(TEntity entity);
        Task<TEntity> UpdateItem(TEntity entity);
        Task<TEntity> DeleteItem(TKey id);
    }
}