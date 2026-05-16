namespace DotnetToolUpdaterExample.Models;

public record DotnetToolItem(
    string PackageId,
    string Version,
    List<string> Commands);