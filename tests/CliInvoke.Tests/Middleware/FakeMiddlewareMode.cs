namespace CliInvoke.Tests.Middleware;

public enum FakeMiddlewareMode
{
    AlwaysInvokeNext,
    NeverInvokeNext,
    ThrowOnInvoke,
}