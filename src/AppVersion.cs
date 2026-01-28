using System.Reflection;

#pragma warning disable IDE0130
namespace System;
#pragma warning restore IDE0130

internal static class AppVersion
{
    private static readonly Lazy<string> lazyValue = new( () =>
    {
        var assembly = Assembly.GetExecutingAssembly();
        var value = assembly.GetName().Version?.ToString( 3 ) ?? "0.0.0";

        var infoVerAttr = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();

        if ( infoVerAttr != null )
        {
            return infoVerAttr.InformationalVersion;
        }

        return value;
    } );

    public static string Value => lazyValue.Value;
}
