namespace AndreyTalanin0x00.AspNetCore.SpaServices.Options;

/// <summary>
/// Represents Single Page Application (SPA) development server options.
/// </summary>
public class SpaDevelopmentServerOptions
{
    /// <summary>
    /// Gets the configuration section name.
    /// </summary>
    public static string SectionName { get; } = "SpaDevelopmentServer";

    /// <summary>
    /// Gets or sets the path, relative to the application working directory, of the directory that contains the Single Page Application (SPA) source files during development.
    /// The directory may not exist in published applications.
    /// </summary>
    public string SourcePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the package manager executable (e.g npm, yarn) to run the Single Page Application (SPA).
    /// The default value is 'npm'.
    /// </summary>
    public string PackageManagerCommand { get; set; } = "npm";

    /// <summary>
    /// Gets or sets the name of the package manager project script (e.g start, dev) to run the Single Page Application (SPA).
    /// The default value is 'start'.
    /// </summary>
    public string Script { get; set; } = "start";

    /// <summary>
    /// Gets or sets the array of the package manager project script parameters to supply to the Single Page Application (SPA).
    /// The default value is <see langword="null" />.
    /// </summary>
    public string[] ScriptParameters { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether the Single Page Application (SPA) development server proxy should be used.
    /// </summary>
    public bool UseExternalDevelopmentServer { get; set; }

    /// <summary>
    /// Gets or sets the base URI of the Single Page Application (SPA) development server to which requests should be proxied.
    /// </summary>
    public string ExternalDevelopmentServerBaseUri { get; set; } = string.Empty;
}
