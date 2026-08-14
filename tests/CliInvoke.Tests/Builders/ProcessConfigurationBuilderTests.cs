using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;
using System.Text;

namespace CliInvoke.Tests.Builders;

[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
public class ProcessConfigurationBuilderTests
{
    [Test]
    public async Task TestDefaultConfiguration()
    {
        // Arrange
        IProcessConfigurationBuilder processConfigBuilder = new
            ProcessConfigurationBuilder("foo");

        // Act
        ProcessConfiguration builtCommand = processConfigBuilder.Build();

        // Assert 
        await Assert.That(builtCommand.TargetFilePath).IsEqualTo("foo");
        await Assert.That(builtCommand.Arguments).IsEqualTo(string.Empty);
        await Assert.That(builtCommand.WorkingDirectoryPath).IsEqualTo(Directory.GetCurrentDirectory());
        await Assert.That(builtCommand.EnvironmentVariables).IsEmpty();
        await Assert.That(builtCommand.StandardInputEncoding.Equals(Encoding.Default) &&
                          builtCommand.StandardOutputEncoding.Equals(Encoding.Default) &&
                          builtCommand.StandardErrorEncoding.Equals(Encoding.Default)).IsTrue();
        await Assert.That(builtCommand.Credential.GetHashCode()).IsEqualTo(UserCredential.Null.GetHashCode());
        await Assert.That(builtCommand.StandardInput).IsEqualTo(StreamWriter.Null);
        await Assert.That(builtCommand.Credential).IsEqualTo(UserCredential.Null);
        await Assert.That(builtCommand.ResourcePolicy).IsEqualTo(ProcessResourcePolicy.Default);
        await Assert.That(builtCommand.WindowCreation).IsFalse();
        await Assert.That(builtCommand.UseShellExecution).IsFalse();
        await Assert.That(builtCommand.RequiresAdministrator).IsFalse();
    }

    [Test]
    public async Task WithResourcePolicy_ShouldSetResourcePolicy()
    {
        // Arrange
        IProcessConfigurationBuilder builder = new ProcessConfigurationBuilder("foo");
        ProcessResourcePolicy policy = new ProcessResourcePolicy(null, null, null, ProcessPriorityClass.High);

        // Act
        builder = builder.SetProcessResourcePolicy(policy);
        ProcessConfiguration config = builder.Build();

        // Assert
        await Assert.That(config.ResourcePolicy).IsEqualTo(policy);
    }

    [Test]
    public async Task Build_ShouldReturnConfiguration()
    {
        // Arrange
        IProcessConfigurationBuilder builder = new ProcessConfigurationBuilder("test.exe")
            .SetArguments("arg1 arg2");

        // Act
        ProcessConfiguration config = builder.Build();

        // Assert
        await Assert.That(config.TargetFilePath).IsEqualTo("test.exe");
        await Assert.That(config.Arguments).IsEqualTo("arg1 arg2");
    }

    [Test]
    public async Task TestIncompatiblePipingOptionsThrowsException()
    {
        IProcessConfigurationBuilder processConfigBuilder =
            new ProcessConfigurationBuilder("foo");

        //Assert
        await Assert.That(() =>
        {
            processConfigBuilder.UseShellExecution(true)
                .SetStandardInputPipe(new StreamWriter(Console.OpenStandardInput()));
        }).Throws<ArgumentException>();
    }

    [Test]
    public async Task TestTargetFileReconfigured()
    {
        //Arrange
        IProcessConfigurationBuilder processConfigBuilder = new ProcessConfigurationBuilder("foo");

        //Act
        processConfigBuilder = processConfigBuilder.SetTargetFilePath("bar");

        //Assert
        ProcessConfiguration command = processConfigBuilder.Build();
        await Assert.That(command.TargetFilePath).IsEqualTo("bar");
    }

    [Test]
    public async Task TestArgumentsNotReplaced()
    {
        //Arrange
        IProcessConfigurationBuilder processConfigBuilder = new ProcessConfigurationBuilder("foo")
            .SetArguments("--arg-value=value");

        //Act
        ProcessConfiguration newArguments = processConfigBuilder
            .Build();

        //Assert
        await Assert.That(newArguments.Arguments).IsEqualTo("--arg-value=value");
    }

    [SupportedOSPlatform("windows")]
    [Test]
    public async Task TestReconfiguredUserCredential()
    {
        //Arrange
        SecureString password = new SecureString();
        password.AppendChar('1');
        password.AppendChar('2');
        password.AppendChar('3');
        password.AppendChar('4');

        IProcessConfigurationBuilder processConfigBuilder = new ProcessConfigurationBuilder("foo")
            .SetUserCredential(new UserCredential(null,
                "admin",
                password,
                false));

        //Act
        SecureString password2 = new SecureString();
        password2.AppendChar('9');
        password2.AppendChar('8');
        password2.AppendChar('7');
        password2.AppendChar('6');

        UserCredential userCredential = new UserCredential(null,
            "root",
            password2,
            false);

        processConfigBuilder = processConfigBuilder.SetUserCredential(userCredential);

        //Assert
        ProcessConfiguration command = processConfigBuilder.Build();
        await Assert.That(command.Credential).IsEqualTo(userCredential);
    }


    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("freebsd")]
    [Test]
    public async Task TestReconfiguredResourcePolicy()
    {
        //Arrange
        IProcessConfigurationBuilder processConfigBuilder = new ProcessConfigurationBuilder("foo")
            .SetProcessResourcePolicy(ProcessResourcePolicy.Default);

        //Arrange
        ProcessResourcePolicy resourcePolicy = new ProcessResourcePolicy(null,
            null,
            null,
            ProcessPriorityClass.AboveNormal);

        processConfigBuilder = processConfigBuilder.SetProcessResourcePolicy(resourcePolicy);

        //Assert
        ProcessConfiguration command = processConfigBuilder.Build();
        await Assert.That(command.ResourcePolicy).IsEqualTo(resourcePolicy);
    }

    [Test]
    public async Task TestReconfiguredAdminPrivileges()
    {
        //Act
        IProcessConfigurationBuilder processConfigBuilder = new ProcessConfigurationBuilder("foo");

        //Arrange
        processConfigBuilder = processConfigBuilder.RequireAdministratorPrivileges();

        //Assert
        ProcessConfiguration command = processConfigBuilder.Build();
        await Assert.That(command.RequiresAdministrator).IsTrue();
    }

    [Test]
    public async Task TestReconfiguredWorkingDirectory()
    {
        //Act
        IProcessConfigurationBuilder processConfigBuilder = new ProcessConfigurationBuilder("foo")
            .SetWorkingDirectory(Path.GetTempPath());

        string directory = Path.GetTempPath();
        
        //Arrange
        processConfigBuilder = processConfigBuilder.SetWorkingDirectory(directory);

        //Assert
        ProcessConfiguration command = processConfigBuilder.Build();
        await Assert.That(command.WorkingDirectoryPath).IsEqualTo(directory);
    }

    // ─────────────────────────────────────────────────────────────────────
    // ConfigureArguments tests (migrated from ArgumentsBuilderTests)
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task ConfigureArguments_Add_AppendsValue_WithSingleSpaceBetween()
    {
        // Arrange
        IProcessConfigurationBuilder builder = new ProcessConfigurationBuilder("test.exe");

        // Act
        builder.ConfigureArguments(spec =>
        {
            spec.Add("first", escape: false);
            spec.Add("second", escape: false);
        });

        // Assert
        ProcessConfiguration config = builder.Build();
        await Assert.That(config.Arguments).IsEqualTo("first second");
    }

    [Test]
    public async Task ConfigureArguments_Add_EscapesSpecialCharacters()
    {
        // Arrange
        IProcessConfigurationBuilder builder = new ProcessConfigurationBuilder("test.exe");
        string input = "\\\n\t\r\"";
        string expected = "\"\\\\\\n\\t\\r\\\"";

        // Act
        builder.ConfigureArguments(spec => spec.Add(input, escape: true));

        // Assert
        ProcessConfiguration config = builder.Build();
        await Assert.That(config.Arguments).IsEqualTo(expected);
    }

    [Test]
    public async Task ConfigureArguments_Clear_ResetsBuffer()
    {
        // Arrange
        IProcessConfigurationBuilder builder = new ProcessConfigurationBuilder("test.exe");

        // Act
        builder.ConfigureArguments(spec =>
        {
            spec.Add("hello", escape: false);
            spec.Clear();
            spec.Add("world", escape: false);
        });

        // Assert
        ProcessConfiguration config = builder.Build();
        await Assert.That(config.Arguments).IsEqualTo("world");
    }

    [Test]
    public async Task ConfigureArguments_WithValidationLogic_InvalidThrows()
    {
        // Arrange
        IProcessConfigurationBuilder builder = new ProcessConfigurationBuilder("test.exe",
            argumentValidationLogic: s => s != "bad");

        // Act & Assert
        await Assert.That(() =>
        {
            builder.ConfigureArguments(spec => spec.Add("bad", escape: false));
        }).Throws<ArgumentException>();
    }

    [Test]
    public async Task ConfigureArguments_WithValidationLogic_ValidAppends()
    {
        // Arrange
        IProcessConfigurationBuilder builder = new ProcessConfigurationBuilder("test.exe",
            argumentValidationLogic: _ => true);

        // Act
        builder.ConfigureArguments(spec => spec.Add("ok", escape: false));

        // Assert
        ProcessConfiguration config = builder.Build();
        await Assert.That(config.Arguments).IsEqualTo("ok");
    }

    [Test]
    public async Task ConfigureArguments_AddEnumerable_EscapesAndJoinsValues()
    {
        // Arrange
        IProcessConfigurationBuilder builder = new ProcessConfigurationBuilder("test.exe");
        string[] values = ["a\nb", "c\"d"];
        const string expected = "\"a\\nb c\\\"d\"";

        // Act
        builder.ConfigureArguments(spec => spec.AddEnumerable(values, escape: true));

        // Assert
        ProcessConfiguration config = builder.Build();
        await Assert.That(config.Arguments).IsEqualTo(expected);
    }

    [Test]
    public async Task ConfigureArguments_AddEnumerable_EmptyThrows()
    {
        // Arrange
        IProcessConfigurationBuilder builder = new ProcessConfigurationBuilder("test.exe");

        // Act & Assert
        await Assert.That(() =>
        {
            builder.ConfigureArguments(spec => spec.AddEnumerable(Enumerable.Empty<string>(), escape: false));
        }).Throws<ArgumentException>();
    }

    [Test]
    public async Task ConfigureArguments_Add_IFormattable_JoinsValues()
    {
        // Arrange
        IProcessConfigurationBuilder builder = new ProcessConfigurationBuilder("test.exe");
        IFormattable[] values = [1, 2];

        // Act
        builder.ConfigureArguments(spec => spec.AddEnumerable(values, escape: false));

        // Assert
        ProcessConfiguration config = builder.Build();
        await Assert.That(config.Arguments).IsEqualTo("\"1 2\"");
    }

    [Test]
    public async Task ConfigureArguments_Add_StringWithoutEscape_AppendsRawValue()
    {
        // Arrange
        IProcessConfigurationBuilder builder = new ProcessConfigurationBuilder("test.exe");

        // Act
        builder.ConfigureArguments(spec => spec.Add("x y", escape: false));

        // Assert
        ProcessConfiguration config = builder.Build();
        await Assert.That(config.Arguments).IsEqualTo("x y");
    }

    // ─────────────────────────────────────────────────────────────────────
    // ConfigureEnvironmentVariables tests (migrated from EnvironmentVariablesBuilderTests)
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task ConfigureEnvironmentVariables_SetPair_SingleVariable()
    {
        // Arrange
        IProcessConfigurationBuilder builder = new ProcessConfigurationBuilder("test.exe");
        string key = "MY_VAR";
        string value = "my_value";

        // Act
        builder.ConfigureEnvironmentVariables(spec => spec.SetPair(key, value));

        // Assert
        ProcessConfiguration config = builder.Build();
        await Assert.That(config.EnvironmentVariables[key]).IsEqualTo(value);
    }

    [Test]
    public async Task ConfigureEnvironmentVariables_SetEnumerable_MultipleVariables()
    {
        // Arrange
        IProcessConfigurationBuilder builder = new ProcessConfigurationBuilder("test.exe");
        List<KeyValuePair<string, string>> list =
        [
            new("KEY1", "VALUE1"),
            new("KEY2", "VALUE2"),
            new("KEY3", "VALUE3")
        ];

        // Act
        builder.ConfigureEnvironmentVariables(spec => spec.SetEnumerable(list));

        // Assert
        ProcessConfiguration config = builder.Build();
        foreach (KeyValuePair<string, string> pair in list)
            await Assert.That(config.EnvironmentVariables[pair.Key]).IsEqualTo(pair.Value);
    }

    [Test]
    public async Task ConfigureEnvironmentVariables_SetDictionary_Variables()
    {
        // Arrange
        IProcessConfigurationBuilder builder = new ProcessConfigurationBuilder("test.exe");
        Dictionary<string, string> dictionary = new()
        {
            { "D_KEY1", "D_VALUE1" },
            { "D_KEY2", "D_VALUE2" }
        };

        // Act
        builder.ConfigureEnvironmentVariables(spec => spec.SetDictionary(dictionary));

        // Assert
        ProcessConfiguration config = builder.Build();
        foreach (KeyValuePair<string, string> pair in dictionary)
            await Assert.That(config.EnvironmentVariables[pair.Key]).IsEqualTo(pair.Value);
    }

    [Test]
    public async Task ConfigureEnvironmentVariables_SetReadOnlyDictionary_Variables()
    {
        // Arrange
        IProcessConfigurationBuilder builder = new ProcessConfigurationBuilder("test.exe");
        Dictionary<string, string> source = new()
        {
            { "RO_KEY1", "RO_VALUE1" },
            { "RO_KEY2", "RO_VALUE2" }
        };
        ReadOnlyDictionary<string, string> readOnlyDictionary = new(source);

        // Act
        builder.ConfigureEnvironmentVariables(spec => spec.SetReadOnlyDictionary(readOnlyDictionary));

        // Assert
        ProcessConfiguration config = builder.Build();
        foreach (KeyValuePair<string, string> pair in readOnlyDictionary)
            await Assert.That(config.EnvironmentVariables[pair.Key]).IsEqualTo(pair.Value);
    }

    [Test]
    public async Task ConfigureEnvironmentVariables_Combined_AllMethods()
    {
        // Arrange
        IProcessConfigurationBuilder builder = new ProcessConfigurationBuilder("test.exe");
        Dictionary<string, string> dictionary = new() { { "D_KEY", "D_VALUE" } };
        List<KeyValuePair<string, string>> list = [new("L_KEY", "L_VALUE")];

        // Act
        builder.ConfigureEnvironmentVariables(spec =>
        {
            spec.SetPair("P_KEY", "P_VALUE")
                .SetEnumerable(list)
                .SetDictionary(dictionary);
        });

        // Assert
        ProcessConfiguration config = builder.Build();
        await Assert.That(config.EnvironmentVariables["P_KEY"]).IsEqualTo("P_VALUE");
        await Assert.That(config.EnvironmentVariables["L_KEY"]).IsEqualTo("L_VALUE");
        await Assert.That(config.EnvironmentVariables["D_KEY"]).IsEqualTo("D_VALUE");
    }

    [Test]
    public async Task ConfigureEnvironmentVariables_Clear_RemovesAll()
    {
        // Arrange
        IProcessConfigurationBuilder builder = new ProcessConfigurationBuilder("test.exe");

        // Act
        builder.ConfigureEnvironmentVariables(spec =>
        {
            spec.SetPair("KEY1", "VALUE1");
            spec.Clear();
            spec.SetPair("KEY2", "VALUE2");
        });

        // Assert
        ProcessConfiguration config = builder.Build();
        await Assert.That(config.EnvironmentVariables).HasCount().EqualTo(1);
        await Assert.That(config.EnvironmentVariables["KEY2"]).IsEqualTo("VALUE2");
    }

    // ─────────────────────────────────────────────────────────────────────
    // ConfigureProcessResourcePolicy tests (migrated from ProcessResourcePolicyBuilderTests)
    // ─────────────────────────────────────────────────────────────────────

    [Test]
    public async Task ConfigureProcessResourcePolicy_SetPriorityClass()
    {
        // Arrange
        IProcessConfigurationBuilder builder = new ProcessConfigurationBuilder("test.exe");

        // Act
        builder.ConfigureProcessResourcePolicy(spec =>
            spec.SetPriorityClass(ProcessPriorityClass.High));

        // Assert
        ProcessConfiguration config = builder.Build();
        await Assert.That(config.ResourcePolicy.PriorityClass).IsEqualTo(ProcessPriorityClass.High);
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task ConfigureProcessResourcePolicy_SetPriorityBoost(bool enablePriorityBoost)
    {
        // Arrange
        IProcessConfigurationBuilder builder = new ProcessConfigurationBuilder("test.exe");

        // Act
        builder.ConfigureProcessResourcePolicy(spec =>
            spec.ConfigurePriorityBoost(enablePriorityBoost));

        // Assert
        ProcessConfiguration config = builder.Build();
        await Assert.That(config.ResourcePolicy.EnablePriorityBoost).IsEqualTo(enablePriorityBoost);
    }

    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [Test]
    public async Task ConfigureProcessResourcePolicy_SetProcessorAffinity()
    {
        // Arrange
        IProcessConfigurationBuilder builder = new ProcessConfigurationBuilder("test.exe");
        nint processorAffinity = 2;

        // Act
        builder.ConfigureProcessResourcePolicy(spec =>
            spec.SetProcessorAffinity(processorAffinity));

        // Assert
        ProcessConfiguration config = builder.Build();
        await Assert.That(config.ResourcePolicy.ProcessorAffinity).IsNotNull();
        await Assert.That(config.ResourcePolicy.ProcessorAffinity).IsEqualTo(processorAffinity);
    }

    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [Test]
    public async Task ConfigureProcessResourcePolicy_SetProcessorAffinity_InvalidThrows()
    {
        // Arrange
        IProcessConfigurationBuilder builder = new ProcessConfigurationBuilder("test.exe");

        // Act & Assert — zero mask is invalid (less than 1)
        await Assert.That(() =>
        {
            builder.ConfigureProcessResourcePolicy(spec =>
                spec.SetProcessorAffinity(0));
        }).Throws<ArgumentOutOfRangeException>();

        // A value beyond the valid bitmask for the available processors is invalid
        int processorCount = Math.Max(1, Environment.ProcessorCount);
        nint maxAffinityMask = ((nint)1 << processorCount) - 1;
        nint invalidBeyondMax = maxAffinityMask + 1;
        await Assert.That(() =>
        {
            builder.ConfigureProcessResourcePolicy(spec =>
                spec.SetProcessorAffinity(invalidBeyondMax));
        }).Throws<ArgumentOutOfRangeException>();
    }

    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("freebsd")]
    [Test]
    [Arguments(1024_000)]
    [Arguments(8192)]
    [Arguments(1024)]
    public async Task ConfigureProcessResourcePolicy_SetWorkingSet_Valid(nint minWorkingSet)
    {
        // Arrange
        IProcessConfigurationBuilder builder = new ProcessConfigurationBuilder("test.exe");

        // Act
        builder.ConfigureProcessResourcePolicy(spec =>
        {
            spec.SetMinWorkingSet(minWorkingSet);
            spec.SetMaxWorkingSet(minWorkingSet + 1);
        });

        // Assert
        ProcessConfiguration config = builder.Build();
        await Assert.That(config.ResourcePolicy.MinWorkingSet).IsNotNull();
        await Assert.That(config.ResourcePolicy.MinWorkingSet).IsEqualTo(minWorkingSet);
    }

    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("freebsd")]
    [Test]
    [Arguments(-1000)]
    [Arguments(-1)]
    public async Task ConfigureProcessResourcePolicy_SetWorkingSet_InvalidThrows(nint minWorkingSet)
    {
        // Arrange
        IProcessConfigurationBuilder builder = new ProcessConfigurationBuilder("test.exe");

        // Act & Assert
        await Assert.That(() =>
        {
            builder.ConfigureProcessResourcePolicy(spec =>
            {
                spec.SetMinWorkingSet(minWorkingSet);
                spec.SetMaxWorkingSet(minWorkingSet + 1);
            });
        }).Throws<ArgumentOutOfRangeException>();
    }

    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("freebsd")]
    [Test]
    [Arguments(2, true, ProcessPriorityClass.AboveNormal)]
    [Arguments(1, false, ProcessPriorityClass.Normal)]
    [Arguments(1, true, ProcessPriorityClass.BelowNormal)]
    public async Task ConfigureProcessResourcePolicy_Build_CombinedSettings(
        nint processorAffinity, bool priorityBoostEnabled, ProcessPriorityClass priorityClass)
    {
        // Arrange
        IProcessConfigurationBuilder builder = new ProcessConfigurationBuilder("test.exe");

        // Act
        builder.ConfigureProcessResourcePolicy(spec =>
        {
            spec.ConfigurePriorityBoost(priorityBoostEnabled)
                .SetPriorityClass(priorityClass);

            if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux())
                spec.SetProcessorAffinity(processorAffinity);
        });

        // Assert
        ProcessConfiguration config = builder.Build();
        await Assert.That(config.ResourcePolicy.EnablePriorityBoost).IsEqualTo(priorityBoostEnabled);
        await Assert.That(config.ResourcePolicy.PriorityClass).IsEqualTo(priorityClass);

        if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux())
            await Assert.That(config.ResourcePolicy.ProcessorAffinity).IsEqualTo(processorAffinity);
    }

    // ─────────────────────────────────────────────────────────────────────
    // ConfigureUserCredential tests (migrated from UserCredentialBuilderTests)
    // ─────────────────────────────────────────────────────────────────────

    [SupportedOSPlatform("windows")]
    [Test]
    public async Task ConfigureUserCredential_SetUsername()
    {
        // Arrange
        IProcessConfigurationBuilder builder = new ProcessConfigurationBuilder("test.exe");
        string userName = "testuser";

        // Act
        builder.ConfigureUserCredential(spec => spec.SetUsername(userName));

        // Assert
        ProcessConfiguration config = builder.Build();
        await Assert.That(config.Credential.UserName).IsEqualTo(userName);
    }

    [SupportedOSPlatform("windows")]
    [Test]
    public async Task ConfigureUserCredential_SetPassword()
    {
        // Arrange
        IProcessConfigurationBuilder builder = new ProcessConfigurationBuilder("test.exe");
        SecureString password = new SecureString();
        password.AppendChar('f');
        password.AppendChar('a');
        password.AppendChar('k');
        password.AppendChar('e');

        // Act
        builder.ConfigureUserCredential(spec => spec.SetPassword(password));

        // Assert
        ProcessConfiguration config = builder.Build();
        await Assert.That(ReadSecureString(config.Credential.Password)).IsEqualTo("fake");
    }

    [SupportedOSPlatform("windows")]
    [Test]
    public async Task ConfigureUserCredential_SetDomain()
    {
        // Arrange
        IProcessConfigurationBuilder builder = new ProcessConfigurationBuilder("test.exe");
        string domain = "TESTDOMAIN";

        // Act
        builder.ConfigureUserCredential(spec => spec.SetDomain(domain));

        // Assert
        ProcessConfiguration config = builder.Build();
        await Assert.That(config.Credential.Domain).IsEqualTo(domain);
    }

    [SupportedOSPlatform("windows")]
    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task ConfigureUserCredential_SetUserProfileLoading(bool loadUserProfile)
    {
        // Arrange
        IProcessConfigurationBuilder builder = new ProcessConfigurationBuilder("test.exe");

        // Act
        builder.ConfigureUserCredential(spec => spec.SetUserProfileLoading(loadUserProfile));

        // Assert
        ProcessConfiguration config = builder.Build();
        await Assert.That(config.Credential.LoadUserProfile).IsEqualTo(loadUserProfile);
    }

    [SupportedOSPlatform("windows")]
    [Test]
    public async Task ConfigureUserCredential_Build_AllSettings()
    {
        // Arrange
        IProcessConfigurationBuilder builder = new ProcessConfigurationBuilder("test.exe");
        SecureString password = new SecureString();
        password.AppendChar('f');
        password.AppendChar('a');
        password.AppendChar('k');
        password.AppendChar('e');
        string domain = "TESTDOMAIN";
        string userName = "testuser";

        // Act
        builder.ConfigureUserCredential(spec =>
        {
            spec.SetDomain(domain)
                .SetUsername(userName)
                .SetPassword(password)
                .SetUserProfileLoading(true);
        });

        // Assert
        ProcessConfiguration config = builder.Build();
        await Assert.That(config.Credential).IsNotNull();
        await Assert.That(config.Credential.Domain).IsEqualTo(domain);
        await Assert.That(config.Credential.UserName).IsEqualTo(userName);
        await Assert.That(ReadSecureString(config.Credential.Password)).IsEqualTo("fake");
        await Assert.That(config.Credential.LoadUserProfile).IsTrue();
    }

    private static string ReadSecureString(SecureString secureString)
    {
        IntPtr ptr = Marshal.SecureStringToBSTR(secureString);
        try
        {
            return Marshal.PtrToStringBSTR(ptr)!;
        }
        finally
        {
            Marshal.ZeroFreeBSTR(ptr);
        }
    }
}