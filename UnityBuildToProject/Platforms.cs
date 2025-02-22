namespace Nomnom;

public record CrossPlatformString(
    string windows,
    string unix,
    string? macOs
) {
    public string GetValue() {
        return Environment.OSVersion.Platform switch {
            PlatformID.Win32NT  or
            PlatformID.Win32Windows or
            PlatformID.Win32NT  or
            PlatformID.WinCE  => windows,
            PlatformID.Unix   => unix,
            PlatformID.MacOSX => string.IsNullOrEmpty(macOs) ? unix : macOs,
            _ => throw new NotImplementedException(),
        };
    }
};
