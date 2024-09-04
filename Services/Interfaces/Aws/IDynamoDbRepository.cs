using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Services.Wrappers;

namespace Services.Interfaces.Aws;

public interface IDynamoDbRepository
{
    Task SaveAsync<T>(T entity) where T : class;
    Task<T?> LoadAsync<T>(object hashKey, object rangeKey = null) where T : class;
    AsyncSearch<T> QueryAsync<T>(QueryOperationConfig config) where T : class;
    ITransactWriteWrapper<T> CreateTransactWrite<T>() where T : class;

    Task ExecuteTransactWriteAsync<T1, T2>(ITransactWriteWrapper<T1> write1, ITransactWriteWrapper<T2> write2)
        where T1 : class
        where T2 : class;
}