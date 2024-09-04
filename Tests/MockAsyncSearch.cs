using Amazon.DynamoDBv2.DataModel;

namespace Tests;

public class MockAsyncSearch<T> : AsyncSearch<T>
{
    private readonly List<T> _items;
    private int _currentIndex;

    public MockAsyncSearch(List<T> items)
    {
        _items = items;
    }

    public override Task<List<T>> GetNextSetAsync(CancellationToken cancellationToken = default)
    {
        var result = _items.GetRange(_currentIndex, _items.Count - _currentIndex);
        _currentIndex = _items.Count;
        return Task.FromResult(result);
    }

    public override Task<List<T>> GetRemainingAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_items);
    }
}