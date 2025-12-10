using AlastairLundy.WhatExecLib;
using AlastairLundy.WhatExecLib.Detectors;
using AlastairLundy.WhatExecLib.Locators;

namespace CliInvoke.Benchmarking.Data;

public class BufferedTestHelper
{
    public BufferedTestHelper()
    {
        TargetFilePath = "";
        
    }

    public string TargetFilePath
    {
        get;
        private set => field = value;
    }

    public string Arguments => "gen-fake-text";
}
