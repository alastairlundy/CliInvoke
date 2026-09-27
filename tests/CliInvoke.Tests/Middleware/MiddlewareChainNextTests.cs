/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using CliInvoke.Core.Middleware;
using TUnit.Assertions.Enums;

namespace CliInvoke.Tests.Middleware;

/// <summary>
///     Regression tests for <see cref="MiddlewareContext.Next"/> and per-run context
///     sharing: Next must invoke only
///     the middleware further down the chain (never the current middleware or anything upstream),
///     and one <see cref="MiddlewareContext"/> instance plus one token is shared per chain run.
/// </summary>
public class MiddlewareChainNextTests
{
    private static InvocationContext CreateContext()
    {
        return new InvocationContext(
            new ProcessConfigurationBuilder("test.exe").Build(),
            ProcessExitConfiguration.Default,
            InvocationMode.Raw);
    }

    [Test]
    public async Task Next_InvokesOnlyForwardMiddleware_NotTheWholeChain()
    {
        List<string> callLog = [];

        // Deliberately ignores the InvokeAsync next parameter and goes through
        // context.Middleware.Next: with the old fully-composed-head delegate this re-entered this
        // middleware and recursed forever instead of running the rest of the chain once.
        IProcessMiddleware selfAware = new DelegateMiddleware("A", callLog, useContextNext: true);
        FakeMiddleware downstream = new FakeMiddleware("B", callLog);

        MiddlewareChain chain = new MiddlewareChain(
            new List<IProcessMiddleware> { selfAware, downstream },
            ctx =>
            {
                callLog.Add("terminal");
                return Task.CompletedTask;
            });

        InvocationContext ctx = CreateContext();
        await chain.RunAsync(ctx, CancellationToken.None);

        await Assert.That(callLog).IsEquivalentTo(
            new List<string> { "A", "B", "terminal" }, CollectionOrdering.Matching);
    }

    [Test]
    public async Task Next_AfterAwaitingNext_RemainsForwardOnly()
    {
        List<string> callLog = [];

        // Awaits the InvokeAsync next delegate once, then invokes context.Middleware.Next again:
        // the re-invocation must replay only B and the terminal, never A itself.
        IProcessMiddleware rewinds = new DelegateMiddleware("A", callLog, useContextNext: false, invokeNextTwice: true);
        FakeMiddleware downstream = new FakeMiddleware("B", callLog);

        MiddlewareChain chain = new MiddlewareChain(
            new List<IProcessMiddleware> { rewinds, downstream },
            ctx =>
            {
                callLog.Add("terminal");
                return Task.CompletedTask;
            });

        InvocationContext ctx = CreateContext();
        await chain.RunAsync(ctx, CancellationToken.None);

        await Assert.That(callLog).IsEquivalentTo(
            new List<string> { "A", "B", "terminal", "B", "terminal" }, CollectionOrdering.Matching);
    }

    [Test]
    public async Task Context_IsOneSharedInstanceWithOneTokenPerRun()
    {
        List<string> callLog = [];
        ObserveContextMiddleware first = new ObserveContextMiddleware("A", callLog);
        ObserveContextMiddleware second = new ObserveContextMiddleware("B", callLog);

        MiddlewareChain chain = new MiddlewareChain(
            new List<IProcessMiddleware> { first, second },
            ctx =>
            {
                callLog.Add("terminal");
                return Task.CompletedTask;
            });

        using CancellationTokenSource cts = new CancellationTokenSource();
        InvocationContext ctx = CreateContext();
        await chain.RunAsync(ctx, cts.Token);

        await Assert.That(callLog).IsEquivalentTo(
            new List<string> { "A", "B", "terminal" }, CollectionOrdering.Matching);

        // One instance per run, shared by every middleware (before and after awaiting next).
        await Assert.That(first.ObservedBefore).IsEqualTo(second.ObservedBefore);
        await Assert.That(first.ObservedBefore).IsEqualTo(first.ObservedAfter);
        await Assert.That(second.ObservedBefore).IsEqualTo(second.ObservedAfter);

        // One run-scoped token on the shared context.
        await Assert.That(first.ObservedBefore!.CancellationToken).IsEqualTo(cts.Token);
        await Assert.That(second.ObservedBefore!.CancellationToken).IsEqualTo(cts.Token);
    }

    /// <summary>
    ///     Logs its name, then either invokes <c>context.Middleware.Next</c> or the
    ///     <c>InvokeAsync</c> delegate (optionally twice), asserting forward-only semantics.
    /// </summary>
    private sealed class DelegateMiddleware : IProcessMiddleware
    {
        private readonly string _name;
        private readonly List<string> _callLog;
        private readonly bool _useContextNext;
        private readonly bool _invokeNextTwice;

        public DelegateMiddleware(string name, List<string> callLog, bool useContextNext, bool invokeNextTwice = false)
        {
            _name = name;
            _callLog = callLog;
            _useContextNext = useContextNext;
            _invokeNextTwice = invokeNextTwice;
        }

        public async Task InvokeAsync(InvocationContext context, Func<InvocationContext, Task> next)
        {
            _callLog.Add(_name);

            if (_useContextNext)
            {
                await context.Middleware!.Next(context);
                return;
            }

            await next(context);

            if (_invokeNextTwice)
                await context.Middleware!.Next(context);
        }
    }

    /// <summary>
    ///     Records the <c>context.Middleware</c> instance observed before and after awaiting next.
    /// </summary>
    private sealed class ObserveContextMiddleware : IProcessMiddleware
    {
        private readonly string _name;
        private readonly List<string> _callLog;

        public ObserveContextMiddleware(string name, List<string> callLog)
        {
            _name = name;
            _callLog = callLog;
        }

        public MiddlewareContext? ObservedBefore { get; private set; }

        public MiddlewareContext? ObservedAfter { get; private set; }

        public async Task InvokeAsync(InvocationContext context, Func<InvocationContext, Task> next)
        {
            _callLog.Add(_name);
            ObservedBefore = context.Middleware;
            await next(context);
            ObservedAfter = context.Middleware;
        }
    }
}
