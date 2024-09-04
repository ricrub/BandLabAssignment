using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Services.Interfaces.Aws;
using Services.Wrappers;

namespace Services.Concrete.Aws;

public class DynamoDbRepository : IDynamoDbRepository
{
    private readonly IDynamoDBContext _context;

    public DynamoDbRepository(IDynamoDBContext context)
    {
        _context = context;
    }

    public async Task SaveAsync<T>(T entity) where T : class
    {
        await _context.SaveAsync(entity);
    }

    public async Task<T?> LoadAsync<T>(object hashKey, object rangeKey) where T : class
    {
        return await _context.LoadAsync<T>(hashKey, rangeKey);
    }

    public AsyncSearch<T> QueryAsync<T>(QueryOperationConfig config) where T : class
    {
        return _context.FromQueryAsync<T>(config);
    }
    
    public ITransactWriteWrapper<T> CreateTransactWrite<T>() where T : class
    {
        return new TransactWriteWrapper<T>(_context.CreateTransactWrite<T>());
    }

    public async Task ExecuteTransactWriteAsync<T1, T2>(ITransactWriteWrapper<T1> write1, ITransactWriteWrapper<T2> write2) 
        where T1 : class 
        where T2 : class
    {
        var transactWrite1 = (write1 as TransactWriteWrapper<T1>).GetTransactWrite();
        var transactWrite2 = (write2 as TransactWriteWrapper<T2>).GetTransactWrite();

        if (transactWrite1 != null && transactWrite2 != null)
        {
            var combined = transactWrite1.Combine(transactWrite2);
            await combined.ExecuteAsync();
        }    }
}