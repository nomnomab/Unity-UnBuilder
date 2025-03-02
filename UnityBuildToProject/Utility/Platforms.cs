namespace Nomnom;

/// <summary>
/// A cross-platform string value. Use <see cref="GetValue"/> to fetch the current string.
/// </summary>
public record CrossPlatformString(
    string Windows,
    string Unix,
    string? MacOs
) {
    public string GetValue() {
        return Environment.OSVersion.Platform switch {
            PlatformID.Win32NT  or
            PlatformID.Win32Windows or
            PlatformID.Win32NT  or
            PlatformID.WinCE  => Windows,
            PlatformID.Unix   => Unix,
            PlatformID.MacOSX => string.IsNullOrEmpty(MacOs) ? Unix : MacOs,
            _ => throw new NotImplementedException(),
        };
    }
};
