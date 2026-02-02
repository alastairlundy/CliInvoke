namespace CliInvoke.Core.Consoles;

public interface IPseudoConsole
{
    void SetSize(int width, int height);

    void CloseConsole();
}