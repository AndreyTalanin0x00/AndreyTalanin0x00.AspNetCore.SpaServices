using System;

namespace AndreyTalanin0x00.AspNetCore.SpaServices.Attributes;

/// <summary>
/// Represents the assembly attribute providing access to the <c>SpaRoot</c> MSBuild property.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
public sealed class SpaRootPropertyAttribute : Attribute
{
    /// <summary>
    /// Gets the <c>SpaRoot</c> MSBuild property value.
    /// </summary>
    public string SpaRoot { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpaRootPropertyAttribute" /> type using the <c>SpaRoot</c> MSBuild property value.
    /// </summary>
    /// <param name="spaRoot">The <c>SpaRoot</c> MSBuild property value.</param>
    public SpaRootPropertyAttribute(string spaRoot)
    {
        SpaRoot = spaRoot;
    }
}
