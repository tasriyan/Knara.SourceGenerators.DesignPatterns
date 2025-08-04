using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace CodeGenerator.Patterns.Mediator;

// Base request interfaces
public interface IQuery<out TResponse> { }
public interface ICommand { }
public interface ICommand<out TResponse> { }
public interface INotification { }
public interface IStreamQuery<out TResponse> { }

// Handler interfaces
public interface IQueryHandler<in TQuery, TResponse> where TQuery : IQuery<TResponse>
{
	ValueTask<TResponse> Handle(TQuery query, CancellationToken cancellationToken = default);
}

public interface ICommandHandler<in TCommand> where TCommand : ICommand
{
	Task Handle(TCommand command, CancellationToken cancellationToken = default);
}

public interface ICommandHandler<in TCommand, TResponse> where TCommand : ICommand<TResponse>
{
	Task<TResponse> Handle(TCommand command, CancellationToken cancellationToken = default);
}

public interface INotificationHandler<in TNotification> where TNotification : INotification
{
	Task Handle(TNotification notification, CancellationToken cancellationToken = default);
}

public interface IStreamQueryHandler<in TQuery, out TResponse> where TQuery : IStreamQuery<TResponse>
{
	IAsyncEnumerable<TResponse> Handle(TQuery query, [EnumeratorCancellation] CancellationToken cancellationToken = default);
}

// Mediator interface
public interface IMediator
{
	ValueTask<TResponse> Send<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default);
	Task Send(ICommand command, CancellationToken cancellationToken = default);
	Task<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default);
	Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification;
	IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamQuery<TResponse> query, CancellationToken cancellationToken = default);
}
