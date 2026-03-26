using System;

namespace AndreyTalanin0x00.AspNetCore.SpaServices.Attributes;

/// <summary>
/// Represents the assembly attribute providing access to the <c>SpaProxyLaunchCommand</c> MSBuild property.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
public sealed class SpaProxyLaunchCommandPropertyAttribute : Attribute
{
    /// <summary>
    /// Gets the <c>SpaProxyLaunchCommand</c> MSBuild property value.
    /// </summary>
    public string SpaProxyLaunchCommand { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpaProxyLaunchCommandPropertyAttribute" /> type using the <c>SpaProxyLaunchCommand</c> MSBuild property value.
    /// </summary>
    /// <param name="spaProxyLaunchCommand">The <c>SpaProxyLaunchCommand</c> MSBuild property value.</param>
    public SpaProxyLaunchCommandPropertyAttribute(string spaProxyLaunchCommand)
    {
        SpaProxyLaunchCommand = spaProxyLaunchCommand;
    }
}
