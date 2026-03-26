using System;

namespace AndreyTalanin0x00.AspNetCore.SpaServices.Attributes;

/// <summary>
/// Represents the assembly attribute providing access to the <c>SpaProxyServerUrl</c> MSBuild property.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
public sealed class SpaProxyServerUrlPropertyAttribute : Attribute
{
    /// <summary>
    /// Gets the <c>SpaProxyServerUrl</c> MSBuild property value.
    /// </summary>
    public string SpaProxyServerUrl { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpaProxyServerUrlPropertyAttribute" /> type using the <c>SpaProxyServerUrl</c> MSBuild property value.
    /// </summary>
    /// <param name="spaProxyServerUrl">The <c>SpaProxyServerUrl</c> MSBuild property value.</param>
    public SpaProxyServerUrlPropertyAttribute(string spaProxyServerUrl)
    {
        SpaProxyServerUrl = spaProxyServerUrl;
    }
}
