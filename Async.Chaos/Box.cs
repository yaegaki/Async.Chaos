namespace Async.Chaos
{
    public static class ChaosBox
    {
        public static ChaosBox<T> Create<T>(T value)
        {
            return new ChaosBox<T>()
            {
                Value = value,
            };
        }
    }

    public class ChaosBox<T>
    {
        public T Value { get; set; }
    }
}
