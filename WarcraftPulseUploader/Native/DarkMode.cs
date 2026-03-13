// Native/DarkMode.cs
using System.Runtime.InteropServices;

namespace WarcraftPulseUploader.Native;

/// <summary>
/// Applies Windows dark-mode chrome to a form window via DWM APIs.
/// Silently ignored on builds that don't support it.
/// </summary>
internal static class DarkMode
{
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(nint hwnd, uint attr, ref int value, int size);

    // DWMWA_USE_IMMERSIVE_DARK_MODE  = 20 (Windows 10 18985+ / Windows 11)
    // DWMWA_BORDER_COLOR             = 34 (Windows 11 22000+)
    // DWMWA_CAPTION_COLOR            = 35 (Windows 11 22000+)
    // COLORREF format: 0x00BBGGRR

    internal static void Apply(nint hwnd)
    {
        int dark    = 1;
        int caption = 0x00160F0F; // #0f0f16 as COLORREF
        int border  = 0x00352525; // #252535 as COLORREF

        DwmSetWindowAttribute(hwnd, 20, ref dark,    4);
        DwmSetWindowAttribute(hwnd, 35, ref caption, 4);
        DwmSetWindowAttribute(hwnd, 34, ref border,  4);
    }
}
