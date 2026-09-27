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
///     Regression tests for context restoration around a conditional sub-chain: the
///     conditional's sub-chain must not leak its own run-scoped
///     <see cref="MiddlewareContext"/> to outer middleware. Upstream middleware
///     observe their own context again after <c>await next</c>, and downstream middleware
///     (which runs as the sub-chain's terminal) observe the outer context.
/// </summary>
public class ConditionalMiddlewareContextRestoreTests
{
    private static InvocationContext CreateContext()
    {
        return new InvocationContext(
            new ProcessConfigurationBuilder("test.exe").Build(),
            ProcessExitConfiguration.Default,
            InvocationMode.Raw);
    }

    private static MiddlewareChain CreateChain(
        IReadOnlyList<IProcessMiddleware> middleware,
        List<string> callLog)
    {
        return new MiddlewareChain(middleware, ctx =>
        {
            callLog.Add("terminal");
            return Task.CompletedTask;
        });
    }

    [Test]
    public async Task UpstreamMiddleware_SeesItsOwnContext_AfterSubChainCompletes()
    {
        List<string> callLog = [];
        ObserveContextMiddleware upstream = new ObserveContextMiddleware("Upstream", callLog);
        ConditionalMiddleware conditional = new ConditionalMiddleware(
            _ => Task.FromResult(true),
            new List<IProcessMiddleware> { new FakeMiddleware("SubA", callLog) });

        MiddlewareChain chain = CreateChain(
            new List<IProcessMiddleware> { upstream, conditional }, callLog);

        InvocationContext ctx = CreateContext();
        await chain.RunAsync(ctx, CancellationToken.None);

        await Assert.That(callLog).IsEquivalentTo(
            new List<string> { "Upstream", "SubA", "terminal" }, CollectionOrdering.Matching);

        // The sub-chain overwrote context.Middleware during execution; the outer context must be
        // restored so upstream middleware reading it after `await next` still see their own.
        await Assert.That(upstream.ObservedAfter).IsEqualTo(upstream.ObservedBefore);
        await Assert.That(upstream.ObservedBefore).IsNotNull();
    }

    [Test]
    public async Task UpstreamMiddleware_SeesItsOwnContext_WhenSubPipelineBlocks()
    {
        List<string> callLog = [];
        ObserveContextMiddleware upstream = new ObserveContextMiddleware("Upstream", callLog);
        ConditionalMiddleware conditional = new ConditionalMiddleware(
            _ => Task.FromResult(true),
            new List<IProcessMiddleware>
            {
                new FakeMiddleware("BlockingSub", callLog, FakeMiddlewareMode.NeverInvokeNext)
            });

        MiddlewareChain chain = CreateChain(
            new List<IProcessMiddleware> { upstream, conditional }, callLog);

        InvocationContext ctx = CreateContext();
        await chain.RunAsync(ctx, CancellationToken.None);

        await Assert.That(callLog).IsEquivalentTo(
            new List<string> { "Upstream", "BlockingSub" }, CollectionOrdering.Matching);

        // The sub-chain never reaches its terminal, so only the try/finally restore applies.
        await Assert.That(upstream.ObservedAfter).IsEqualTo(upstream.ObservedBefore);
    }

    [Test]
    public async Task DownstreamMiddleware_SeesOuterContext_NotSubChainContext()
    {
        List<string> callLog = [];
        ObserveContextMiddleware upstream = new ObserveContextMiddleware("Upstream", callLog);
        ConditionalMiddleware conditional = new ConditionalMiddleware(
            _ => Task.FromResult(true),
            new List<IProcessMiddleware> { new FakeMiddleware("SubA", callLog) });
        ObserveContextMiddleware downstream = new ObserveContextMiddleware("Downstream", callLog);

        MiddlewareChain chain = CreateChain(
            new List<IProcessMiddleware> { upstream, conditional, downstream }, callLog);

        InvocationContext ctx = CreateContext();
        await chain.RunAsync(ctx, CancellationToken.None);

        await Assert.That(callLog).IsEquivalentTo(
            new List<string> { "Upstream", "SubA", "Downstream", "terminal" },
            CollectionOrdering.Matching);

        // Downstream outer middleware runs as the sub-chain's terminal, inside
        // subChain.RunAsync. It must observe the outer context, not the sub-chain's.
        await Assert.That(downstream.ObservedBefore).IsEqualTo(upstream.ObservedBefore);
        await Assert.That(downstream.ObservedAfter).IsEqualTo(upstream.ObservedBefore);
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
