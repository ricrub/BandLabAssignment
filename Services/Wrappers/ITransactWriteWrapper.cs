namespace Services.Wrappers;

public interface ITransactWriteWrapper<T> where T : class
{
    void AddSaveItem(T item);
    void AddDeleteItem(T item);
}