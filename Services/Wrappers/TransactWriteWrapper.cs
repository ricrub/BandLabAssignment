using Amazon.DynamoDBv2.DataModel;

namespace Services.Wrappers;

public class TransactWriteWrapper<T> : ITransactWriteWrapper<T> where T : class
{
    private readonly TransactWrite<T> _transactWrite;

    public TransactWriteWrapper(TransactWrite<T> transactWrite)
    {
        _transactWrite = transactWrite;
    }

    public void AddSaveItem(T item)
    {
        _transactWrite.AddSaveItem(item);
    }

    public void AddDeleteItem(T item)
    {
        _transactWrite.AddDeleteItem(item);
    }

    public TransactWrite<T> GetTransactWrite()
    {
        return _transactWrite;
    }
}