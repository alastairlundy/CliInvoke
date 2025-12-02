using CliInvoke.Core;
using CliInvoke.Core.Extensibility;
using CliInvoke.Core.Extensibility.Factories;
using CliInvoke.Specializations.Configurations;

namespace CliInvoke.Specializations;

/// <summary>
/// 
/// </summary>
public class CmdProcessInvoker : RunnerProcessInvokerBase
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="processInvoker"></param>
    /// <param name="runnerProcessFactory"></param>
    /// <param name="windowCreation"></param>
    /// <param name="redirectOutputs"></param>
    public CmdProcessInvoker(IProcessInvoker processInvoker, IRunnerProcessFactory runnerProcessFactory, bool windowCreation = true, bool redirectOutputs = true) :
        base(processInvoker, runnerProcessFactory, new CmdProcessConfiguration("", false,
            redirectOutputs, redirectOutputs, windowCreation: windowCreation))
    {
        
    }
}