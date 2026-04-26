namespace DotnetToolUpdaterExample.Models;

public record DotnetToolListResponse(
    int Version, List<DotnetToolItem> Data);