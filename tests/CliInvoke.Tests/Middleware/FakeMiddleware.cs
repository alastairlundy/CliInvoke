using System.Collections.Generic;
using CliInvoke.Core.Middleware;

namespace CliInvoke.Tests.Middleware;

public enum FakeMiddlewareMode
{
    AlwaysInvokeNext,
    NeverInvokeNext,
    ThrowOnInvoke
}

public class FakeMiddleware : IProcessMiddleware
{
    private readonly string _name;
    private readonly FakeMiddlewareMode _mode;
    private readonly IList<string> _callLog;
    private readonly Exception? _exceptionToThrow;

    public FakeMiddleware(string name, IList<string> callLog, FakeMiddlewareMode mode = FakeMiddlewareMode.AlwaysInvokeNext, Exception? exceptionToThrow = null)
    {
        _name = name;
        _callLog = callLog;
        _mode = mode;
        _exceptionToThrow = exceptionToThrow;
    }

    public Task InvokeAsync(InvocationContext context, Func<InvocationContext, CancellationToken, Task> next)
    {
        _callLog.Add(_name);

        return _mode switch
        {
            FakeMiddlewareMode.AlwaysInvokeNext => next(context, context.CancellationToken),
            FakeMiddlewareMode.NeverInvokeNext => Task.CompletedTask,
            FakeMiddlewareMode.ThrowOnInvoke => throw _exceptionToThrow ?? new InvalidOperationException($" {_name} threw"),
            _ => Task.CompletedTask
        };
    }
}
