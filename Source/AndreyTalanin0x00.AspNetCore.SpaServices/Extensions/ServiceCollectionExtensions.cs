using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using AndreyTalanin0x00.AspNetCore.SpaServices.Attributes;
using AndreyTalanin0x00.AspNetCore.SpaServices.Options;
using AndreyTalanin0x00.Extensions.Hosting.Services.Abstractions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AndreyTalanin0x00.AspNetCore.SpaServices.Extensions;

/// <summary>
/// Provides a set of extension methods for the <see cref="IServiceCollection" /> <see langword="interface" />.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers services required for serving Vite Single Page Application (SPA) static files in a deployed environment in the specified <see cref="IServiceCollection" /> collection.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> collection for adding service descriptors.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="startupAssemblyProvider">The startup assembly provider. This parameter is optional.</param>
    /// <param name="configure">An <see cref="Action{T}"/> method to configure the provided <see cref="SpaStaticFilesOptions"/> options. This parameter is optional.</param>
    /// <returns>The same service collection so that multiple calls can be chained.</returns>
    public static IServiceCollection AddViteSpaStaticFiles(this IServiceCollection services, IConfiguration configuration, IStartupAssemblyProvider? startupAssemblyProvider = null, Action<SpaStaticFilesOptions>? configure = null)
    {
        SpaStaticFilesOptions spaStaticFilesOptions = new();

        if (TryGetStartupAssembly(startupAssemblyProvider, out Assembly? startupAssembly))
            spaStaticFilesOptions = PopulateSpaStaticFilesOptions(startupAssembly, spaStaticFilesOptions);

        IConfigurationSection spaStaticFilesConfigurationSection = configuration.GetSection(SpaStaticFilesOptions.SectionName);
        if (spaStaticFilesConfigurationSection.Exists())
            spaStaticFilesConfigurationSection.Bind(spaStaticFilesOptions);

        if (configure is not null)
            configure(spaStaticFilesOptions);

        OptionsWrapper<SpaStaticFilesOptions> spaStaticFilesOptionsWrapper = new(spaStaticFilesOptions);

        services.AddSingleton<IOptions<SpaStaticFilesOptions>>(spaStaticFilesOptionsWrapper);

        services.AddSpaStaticFiles(spaStaticFilesOptionsMicrosoftAspNetCore =>
        {
            // Must match the <SpaPublishRoot /> MSBuild property of the main .csproj file.
            spaStaticFilesOptionsMicrosoftAspNetCore.RootPath = spaStaticFilesOptions.RootPath;
        });

        return services;
    }

    /// <summary>
    /// Registers services required for using the React development server (provided by Vite) for the Single Page Application (SPA) in the specified <see cref="IServiceCollection" /> collection.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> collection for adding service descriptors.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="startupAssemblyProvider">The startup assembly provider. This parameter is optional.</param>
    /// <param name="configure">An <see cref="Action{T}"/> method to configure the provided <see cref="SpaDevelopmentServerOptions"/> options. This parameter is optional.</param>
    /// <returns>The same service collection so that multiple calls can be chained.</returns>
    public static IServiceCollection AddViteSpaDevelopmentServer(this IServiceCollection services, IConfiguration configuration, IStartupAssemblyProvider? startupAssemblyProvider = null, Action<SpaDevelopmentServerOptions>? configure = null)
    {
        SpaDevelopmentServerOptions spaDevelopmentServerOptions = new();

        if (TryGetStartupAssembly(startupAssemblyProvider, out Assembly? startupAssembly))
            spaDevelopmentServerOptions = PopulateSpaDevelopmentServerOptions(startupAssembly, spaDevelopmentServerOptions);

        IConfigurationSection spaDevelopmentServerConfigurationSection = configuration.GetSection(SpaDevelopmentServerOptions.SectionName);
        if (spaDevelopmentServerConfigurationSection.Exists())
            spaDevelopmentServerConfigurationSection.Bind(spaDevelopmentServerOptions);

        if (configure is not null)
            configure(spaDevelopmentServerOptions);

        OptionsWrapper<SpaDevelopmentServerOptions> spaDevelopmentServerOptionsWrapper = new(spaDevelopmentServerOptions);

        services.AddSingleton<IOptions<SpaDevelopmentServerOptions>>(spaDevelopmentServerOptionsWrapper);

        return services;
    }

    private static SpaStaticFilesOptions PopulateSpaStaticFilesOptions(Assembly startupAssembly, SpaStaticFilesOptions spaStaticFilesOptions)
    {
        if (TryGetSpaPublishRootPropertyAttributeValue(startupAssembly, out string? spaPublishRoot))
            spaStaticFilesOptions.RootPath = spaPublishRoot;

        return spaStaticFilesOptions;
    }

    private static SpaDevelopmentServerOptions PopulateSpaDevelopmentServerOptions(Assembly startupAssembly, SpaDevelopmentServerOptions spaDevelopmentServerOptions)
    {
        if (TryGetSpaRootPropertyAttributeValue(startupAssembly, out string? spaRoot))
            spaDevelopmentServerOptions.SourcePath = spaRoot;
        if (TryGetSpaProxyLaunchCommandPropertyAttributeValue(startupAssembly, out string? spaProxyLaunchCommand))
        {
            NodeLaunchCommandParser.ParseSpaProxyLaunchCommand(spaProxyLaunchCommand, out string packageManagerCommand, out string script, out string[]? scriptParameters);
            spaDevelopmentServerOptions.PackageManagerCommand = packageManagerCommand;
            spaDevelopmentServerOptions.Script = script;
            spaDevelopmentServerOptions.ScriptParameters = scriptParameters;
        }
        if (TryGetSpaProxyServerUrlPropertyAttributeValue(startupAssembly, out string? spaProxyServerUrl))
        {
            spaDevelopmentServerOptions.UseExternalDevelopmentServer = true;
            spaDevelopmentServerOptions.ExternalDevelopmentServerBaseUri = spaProxyServerUrl;
        }

        return spaDevelopmentServerOptions;
    }

    private static bool TryGetSpaRootPropertyAttributeValue(Assembly startupAssembly, [NotNullWhen(true)] out string? spaRoot)
    {
        spaRoot = null;
        SpaRootPropertyAttribute? spaRootPropertyAttribute = startupAssembly.GetCustomAttribute<SpaRootPropertyAttribute>();
        if (spaRootPropertyAttribute is not null)
        {
            spaRoot = spaRootPropertyAttribute.SpaRoot;
            return true;
        }

        return false;
    }

    private static bool TryGetSpaProxyLaunchCommandPropertyAttributeValue(Assembly startupAssembly, [NotNullWhen(true)] out string? spaProxyLaunchCommand)
    {
        spaProxyLaunchCommand = null;
        SpaProxyLaunchCommandPropertyAttribute? spaProxyLaunchCommandPropertyAttribute = startupAssembly.GetCustomAttribute<SpaProxyLaunchCommandPropertyAttribute>();
        if (spaProxyLaunchCommandPropertyAttribute is not null)
        {
            spaProxyLaunchCommand = spaProxyLaunchCommandPropertyAttribute.SpaProxyLaunchCommand;
            return true;
        }

        return false;
    }

    private static bool TryGetSpaProxyServerUrlPropertyAttributeValue(Assembly startupAssembly, [NotNullWhen(true)] out string? spaProxyServerUrl)
    {
        spaProxyServerUrl = null;
        SpaProxyServerUrlPropertyAttribute? spaProxyServerUrlPropertyAttribute = startupAssembly.GetCustomAttribute<SpaProxyServerUrlPropertyAttribute>();
        if (spaProxyServerUrlPropertyAttribute is not null)
        {
            spaProxyServerUrl = spaProxyServerUrlPropertyAttribute.SpaProxyServerUrl;
            return true;
        }

        return false;
    }

    private static bool TryGetSpaPublishRootPropertyAttributeValue(Assembly startupAssembly, [NotNullWhen(true)] out string? spaPublishRoot)
    {
        spaPublishRoot = null;
        SpaPublishRootPropertyAttribute? spaPublishRootPropertyAttribute = startupAssembly.GetCustomAttribute<SpaPublishRootPropertyAttribute>();
        if (spaPublishRootPropertyAttribute is not null)
        {
            spaPublishRoot = spaPublishRootPropertyAttribute.SpaPublishRoot;
            return true;
        }

        return false;
    }

    private static bool TryGetStartupAssembly(IStartupAssemblyProvider? startupAssemblyProvider, [NotNullWhen(true)] out Assembly? startupAssembly)
    {
        startupAssembly = null;
        if (startupAssemblyProvider is not null)
        {
            startupAssembly = startupAssemblyProvider.GetStartupAssembly();
            return true;
        }

        return false;
    }
}
