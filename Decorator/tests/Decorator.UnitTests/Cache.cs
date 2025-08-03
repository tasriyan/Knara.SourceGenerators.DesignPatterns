using CodeGenerator.Patterns.Decorator;

namespace TestNamespace
{
    [GenerateDecoratorFactory(BaseName = "CustomService")]
    public interface IMyService
    {
        string GetData();
    }

    [Decorator(Type = "Cache")]
    public class CacheDecorator : IMyService
    {
        private readonly IMyService _service;

        public CacheDecorator(IMyService service)
        {
            _service = service;
        }

        public string GetData() => _service.GetData();
    }
}