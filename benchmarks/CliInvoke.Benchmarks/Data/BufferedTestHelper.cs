using AlastairLundy.WhatExecLib;
using AlastairLundy.WhatExecLib.Detectors;
using AlastairLundy.WhatExecLib.Locators;

namespace CliInvoke.Benchmarking.Data;

public class BufferedTestHelper
{
    public BufferedTestHelper()
    {
        ExecutableFileDetector executableFileDetector = new();

        WhatExecutableResolver filePathResolver = new(new PathExecutableResolver(executableFileDetector),
            new ExecutableFileInstancesLocator(executableFileDetector));

        TargetFilePath = filePathResolver.ResolveExecutableFilePath("CliInvokeMockDataSimTool").FullName;
        
    }

    public string TargetFilePath { get; }

    public string Arguments => "gen-fake-text";
}
