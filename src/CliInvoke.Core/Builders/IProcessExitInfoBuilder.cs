using AlastairLundy.CliInvoke.Core.Primitives;

namespace AlastairLundy.CliInvoke.Core.Builders;

/// <summary>
/// 
/// </summary>
public interface IProcessExitInfoBuilder
{
        
    /// <summary>
    /// Sets the Result Validation whether to throw an exception or not if the Process does not execute successfully.
    /// </summary>
    /// <param name="validation">The result validation behaviour to be used.</param>
    /// <returns>The new ProcessConfigurationBuilder object with the configured Result Validation behaviour.</returns>
    IProcessExitInfoBuilder WithValidation(ProcessResultValidation validation);

    
    /// <summary>
    /// Sets the Process Timeout Policy to be used for this Process.
    /// </summary>
    /// <param name="processTimeoutPolicy">The process timeout policy to use.</param>
    /// <returns>The new ProcessConfigurationBuilder with the specified Process Timeout Policy.</returns>
    IProcessExitInfoBuilder WithProcessTimeoutPolicy(ProcessTimeoutPolicy processTimeoutPolicy);

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    ProcessExitInfo Build();
}