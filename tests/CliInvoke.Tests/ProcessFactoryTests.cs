using Xunit;
using CliInvoke;

namespace CliInvoke.Tests
{
    public class ProcessFactoryTests
    {
        [Fact]
        public void Constructor_ShouldInstantiate()
        {
            // TODO: Use mocks for dependencies
            var factory = new ProcessFactory(null, null);
            Assert.NotNull(factory);
        }

        [Fact]
        public void From_ProcessStartInfo_ShouldThrowIfFileNameEmpty()
        {
            // TODO: Mock IFilePathResolver, IProcessPipeHandler
            // TODO: Test ArgumentException when FileName is empty
        }

        [Fact]
        public void From_ProcessStartInfo_ShouldReturnProcess()
        {
            // TODO: Test valid ProcessStartInfo returns Process
        }

        [Fact]
        public void From_ProcessStartInfo_UserCredential_ShouldReturnProcess()
        {
            // TODO: Test overload with UserCredential
        }

        [Fact]
        public void From_ProcessConfiguration_ShouldReturnProcess()
        {
            // TODO: Test overload with ProcessConfiguration
        }

        [Fact]
        public void StartNew_ProcessStartInfo_ShouldStartProcess()
        {
            // TODO: Test StartNew with ProcessStartInfo
        }

        [Fact]
        public void StartNew_ProcessStartInfo_UserCredential_ShouldStartProcess()
        {
            // TODO: Test StartNew with ProcessStartInfo and UserCredential
        }

        [Fact]
        public void StartNew_ProcessStartInfo_ResourcePolicy_ShouldStartProcess()
        {
            // TODO: Test StartNew with ProcessStartInfo and ProcessResourcePolicy
        }

        [Fact]
        public void StartNew_ProcessStartInfo_ResourcePolicy_UserCredential_ShouldStartProcess()
        {
            // TODO: Test StartNew with ProcessStartInfo, ProcessResourcePolicy, UserCredential
        }

        [Fact]
        public void StartNew_ProcessConfiguration_ShouldStartProcess()
        {
            // TODO: Test StartNew with ProcessConfiguration
        }

        [Fact]
        public async Task ContinueWhenExitAsync_ShouldReturnProcessResult()
        {
            // TODO: Test ContinueWhenExitAsync
        }

        [Fact]
        public async Task ContinueWhenExitAsync_WithProcessConfiguration_ShouldReturnProcessResult()
        {
            // TODO: Test ContinueWhenExitAsync with ProcessConfiguration
        }

        [Fact]
        public async Task ContinueWhenExitBufferedAsync_ShouldReturnBufferedProcessResult()
        {
            // TODO: Test ContinueWhenExitBufferedAsync
        }

        [Fact]
        public async Task ContinueWhenExitBufferedAsync_WithProcessConfiguration_ShouldReturnBufferedProcessResult()
        {
            // TODO: Test ContinueWhenExitBufferedAsync with ProcessConfiguration
        }

        [Fact]
        public async Task ContinueWhenExitPipedAsync_ShouldReturnPipedProcessResult()
        {
            // TODO: Test ContinueWhenExitPipedAsync
        }

        [Fact]
        public async Task ContinueWhenExitPipedAsync_WithProcessConfiguration_ShouldReturnPipedProcessResult()
        {
            // TODO: Test ContinueWhenExitPipedAsync with ProcessConfiguration
        }
    }
}
