using System;

namespace AndreyTalanin0x00.AspNetCore.SpaServices.Attributes;

/// <summary>
/// Represents the assembly attribute providing access to the <c>SpaPublishRoot</c> MSBuild property.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
public sealed class SpaPublishRootPropertyAttribute : Attribute
{
    /// <summary>
    /// Gets the <c>SpaPublishRoot</c> MSBuild property value.
    /// </summary>
    public string SpaPublishRoot { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpaPublishRootPropertyAttribute" /> type using the <c>SpaPublishRoot</c> MSBuild property value.
    /// </summary>
    /// <param name="spaPublishRoot">The <c>SpaPublishRoot</c> MSBuild property value.</param>
    public SpaPublishRootPropertyAttribute(string spaPublishRoot)
    {
        SpaPublishRoot = spaPublishRoot;
    }
}
