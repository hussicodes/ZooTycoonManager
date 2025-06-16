namespace ZooTycoonManager
{
    public interface IObserver<in T>
    {
        void Update(T value);
    }
} 