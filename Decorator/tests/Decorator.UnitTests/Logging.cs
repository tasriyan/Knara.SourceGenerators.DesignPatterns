using CodeGenerator.Patterns.Decorator;

namespace TestNamespace
{
    [GenerateDecoratorFactory]
    public interface IService
    {
        void DoWork();
    }

    [Decorator(Type = "Logging")]
    public class LoggingDecorator : IService
    {
        private readonly IService _service;

        public LoggingDecorator(IService service)
        {
            _service = service;
        }

        public void DoWork() => _service.DoWork();
    }
}