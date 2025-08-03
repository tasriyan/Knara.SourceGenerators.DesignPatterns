using CodeGenerator.Patterns.Decorator;

namespace TestNamespace
{
    [GenerateDecoratorFactory]
    public interface IRepository
    {
        void Save(string data);
    }

    [Decorator(Type = "Retry")]
    public class RetryDecorator : IRepository
    {
        private readonly IRepository _repository;
        private readonly int _maxAttempts;
        private readonly string _logPrefix;

        public RetryDecorator(IRepository repository, int maxAttempts, string logPrefix)
        {
            _repository = repository;
            _maxAttempts = maxAttempts;
            _logPrefix = logPrefix;
        }

        public void Save(string data) => _repository.Save(data);
    }
}