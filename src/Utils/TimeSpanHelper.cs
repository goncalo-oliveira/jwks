using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Jwks.Utils;

public static class TimeSpanHelper
{
    private static readonly List<(string abbrev, TimeSpan ts)> timeSpanDefault =
    [
        ( "d", TimeSpan.FromDays( 1 ) ),
        ( "h", TimeSpan.FromHours( 1 ) ),
        ( "m", TimeSpan.FromMinutes( 1 ) ),
        ( "s", TimeSpan.FromSeconds( 1 ) ),
    ];

    public static bool TryParseDuration( string? value, [MaybeNullWhen( false )] out TimeSpan duration )
    {
        if ( ParseDuration( value ) is TimeSpan ts )
        {
            duration = ts;
            return true;
        }

        duration = default;

        return false;
    }

    public static TimeSpan? ParseDuration( string? value )
    {
        if ( value == null )
        {
            return null;
        }

        // if value contains only digits, it is a number of seconds
        if ( int.TryParse( value, out var seconds ) )
        {
            return TimeSpan.FromSeconds( seconds );
        }

        try
        {
            return timeSpanDefault
                .Where( ts => value.Contains( ts.abbrev ) )
                .Select( ts => ts.ts * int.Parse( new Regex( @$"(\d+){ts.abbrev}" ).Match( value ).Groups[1].Value ) )
                .Aggregate( ( acc, ts ) => acc + ts );
        }
        catch ( Exception )
        {
            return null;
        }
    }
}
