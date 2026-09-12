namespace ASP2
{
    public interface ICounter
    {
        int Value { get; }
        void Increment();
    }

    public class Counter : ICounter
    {
        private int _value;

        public int Value => _value;

        public void Increment() => _value++;

    }
}