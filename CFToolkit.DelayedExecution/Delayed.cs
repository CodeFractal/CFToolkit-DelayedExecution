using System;
using System.Threading.Tasks;

namespace CFToolkit.DelayedExecution
{
    public class Delayed<T>
    {
        private readonly Func<T> _valueFactory;
        private T _value;
        private Task<T> _evalTask;
        public bool Evaluated { get; private set; }

        public Delayed(Func<T> valueFactory, bool evaluateAsync = false)
        {
            _valueFactory = valueFactory;
            if (evaluateAsync) EvaluateAsync();
        }

        public Delayed(bool evaluateAsync, Func<T> valueFactory) : this(valueFactory, evaluateAsync) { }

        public T Current(bool waitIfRunning = true, bool mustHaveValue = true)
        {
            bool willEvaluate =
                (mustHaveValue && !Evaluated) ||
                (waitIfRunning && _evalTask?.Status == TaskStatus.Running);

            if (willEvaluate)
            {
                _evalTask.Wait();
            }

            return _value;
        }

        public T Evaluate()
        {
            EvaluateAsync().Wait();
            return _value;
        }

        public async Task<T> EvaluateAsync()
        {
            if (_valueFactory != null)
            {
                if (_evalTask == null || _evalTask.Status != TaskStatus.Running)
                {
                    _evalTask = Task.Run(_valueFactory)
                        .ContinueWith(t => {
                            _value = t.Result;
                            Evaluated = true;
                            return _value;
                        });
                }
                return await _evalTask;
            }
            return _value;
        }

        public static implicit operator T(Delayed<T> instance)
        {
            return instance.Current(false);
        }

        public static implicit operator Delayed<T>(T value)
        {
            var result = new Delayed<T>(null);
            result.Evaluated = true;
            result._value = value;
            return result;
        }

        public static implicit operator Delayed<T>(Func<T> valueFactory)
        {
            var result = new Delayed<T>(valueFactory);
            return result;
        }
    }

    public static class Delayed
    {
        public static Delayed<T> From<T>(Func<T> valueFactory, bool evaluateAsync = false) =>
            new Delayed<T>(valueFactory, evaluateAsync);

        public static Delayed<T> From<T>(bool evaluateAsync, Func<T> valueFactory) =>
            new Delayed<T>(evaluateAsync, valueFactory);
    }
}