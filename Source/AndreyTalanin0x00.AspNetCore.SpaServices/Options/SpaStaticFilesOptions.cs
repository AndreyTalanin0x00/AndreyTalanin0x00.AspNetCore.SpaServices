namespace AndreyTalanin0x00.AspNetCore.SpaServices.Options;

/// <summary>
/// Represents Single Page Application (SPA) static files options.
/// </summary>
public class SpaStaticFilesOptions
{
    /// <summary>
    /// Gets the configuration section name.
    /// </summary>
    public static string SectionName { get; } = "SpaStaticFiles";

    /// <summary>
    /// Gets or sets the root path where Single Page Application (SPA) static files are deployed.
    /// </summary>
    public string RootPath { get; set; } = string.Empty;
}
