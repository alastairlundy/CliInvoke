using AlastairLundy.CliInvoke.Builders;
using AlastairLundy.CliInvoke.Core.Abstractions.Builders;
using AlastairLundy.CliInvoke.Core.Primitives;

namespace AlastairLundy.CliInvoke.Internal.Extensions;

internal static class ToProcessConfigurationExtensions
{
    internal static ProcessConfiguration ToProcessConfiguration(this CliCommandConfiguration configuration)
    {
        IProcessConfigurationBuilder processConfigurationBuilder = new ProcessConfigurationBuilder(configuration.TargetFilePath);
        processConfigurationBuilder.WithArguments(configuration.Arguments);
        processConfigurationBuilder.WithWorkingDirectory(configuration.WorkingDirectoryPath);
        processConfigurationBuilder.WithAdministratorPrivileges(configuration.RequiresAdministrator);
        processConfigurationBuilder.WithEnvironmentVariables(configuration.EnvironmentVariables);
        processConfigurationBuilder.WithEncoding(configuration.StandardInputEncoding,
            configuration.StandardOutputEncoding, configuration.StandardErrorEncoding);
        processConfigurationBuilder.WithProcessResourcePolicy(configuration.ResourcePolicy);
        processConfigurationBuilder.WithAdministratorPrivileges(configuration.RequiresAdministrator);
        processConfigurationBuilder.WithShellExecution(configuration.UseShellExecution);
        processConfigurationBuilder.WithValidation(configuration.ResultValidation);
        processConfigurationBuilder.WithWindowCreation(configuration.WindowCreation);
        processConfigurationBuilder.WithStandardInputPipe(configuration.StandardInput);
        processConfigurationBuilder.WithStandardOutputPipe(configuration.StandardOutput);
        processConfigurationBuilder.WithStandardErrorPipe(configuration.StandardError);

        processConfigurationBuilder.WithUserCredential(configuration.Credential);
        
        return processConfigurationBuilder.Build();
    }
}