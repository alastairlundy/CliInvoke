using System.IO;

namespace CliInvoke.Tests.Internal.Constants;

public static class TargetFilePaths
{
    public static readonly string CmdFilePath = Environment.SystemDirectory + Path.DirectorySeparatorChar + "cmd.exe";

    public static readonly string LinuxEchoFilePath = "/usr/bin/echo";
}