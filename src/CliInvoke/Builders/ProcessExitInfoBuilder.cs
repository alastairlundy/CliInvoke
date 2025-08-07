using AlastairLundy.CliInvoke.Core.Builders;
using AlastairLundy.CliInvoke.Core.Primitives;

namespace AlastairLundy.CliInvoke.Builders;

/// <summary>
/// 
/// </summary>
public class ProcessExitInfoBuilder : IProcessExitInfoBuilder
{
    private readonly ProcessExitInfo _processExitInfo;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="processExitInfo"></param>
    public ProcessExitInfoBuilder(ProcessExitInfo processExitInfo)
    {
        _processExitInfo = processExitInfo;
    }
        
    /// <summary>
    /// Sets the Result Validation whether to throw an exception or not if the Process does not execute successfully.
    /// </summary>
    /// <param name="validation">The result validation behaviour to be used.</param>
    /// <returns>The new ProcessExitInfoBuilder object with the configured Result Validation behaviour.</returns>
    public IProcessExitInfoBuilder WithValidation(ProcessResultValidation validation)
    {
        return new ProcessExitInfoBuilder(
            new ProcessExitInfo(_processExitInfo.TimeoutPolicy, validation));
    }

    /// <summary>
    /// Sets the Process Timeout Policy to be used for this Process.
    /// </summary>
    /// <param name="processTimeoutPolicy">The process timeout policy to use.</param>
    /// <returns>The new ProcessExitInfoBuilder with the specified Process Timeout Policy.</returns>
    public IProcessExitInfoBuilder WithProcessTimeoutPolicy(ProcessTimeoutPolicy processTimeoutPolicy)
    {
        return new ProcessExitInfoBuilder(
            new ProcessExitInfo(processTimeoutPolicy, _processExitInfo.ResultValidation));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public ProcessExitInfo Build() =>  
        _processExitInfo;
}