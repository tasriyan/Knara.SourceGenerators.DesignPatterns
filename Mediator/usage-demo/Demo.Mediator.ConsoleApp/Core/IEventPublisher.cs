using CodeGenerator.Patterns.Mediator;

namespace Demo.Mediator.ConsoleApp.Core;

public interface IEventPublisher
{
    Task Publish<T>(T notification) where T : INotification;
}