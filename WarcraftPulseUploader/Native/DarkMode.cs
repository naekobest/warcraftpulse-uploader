// Native/DarkMode.cs
using System.Runtime.InteropServices;

namespace WarcraftPulseUploader.Native;

/// <summary>
/// Applies Windows dark-mode chrome to forms and controls via DWM / UxTheme APIs.
/// Silently ignored on builds that don't support a given attribute.
/// </summary>
internal static class DarkMode
{
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(nint hwnd, uint attr, ref int value, int size);

    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
    private static extern int SetWindowTheme(nint hwnd, string pszSubAppName, string? pszSubIdList);

    // DWMWA_USE_IMMERSIVE_DARK_MODE  = 20 (Windows 10 18985+ / Windows 11)
    // DWMWA_BORDER_COLOR             = 34 (Windows 11 22000+)
    // DWMWA_CAPTION_COLOR            = 35 (Windows 11 22000+)
    // COLORREF format: 0x00BBGGRR

    /// <summary>Dark title bar, caption colour, and border colour for a form window.</summary>
    internal static void ApplyToWindow(nint hwnd)
    {
        int dark    = 1;
        int caption = 0x00160F0F; // #0f0f16 as COLORREF
        int border  = 0x00352525; // #252535 as COLORREF

        DwmSetWindowAttribute(hwnd, 20, ref dark,    4);
        DwmSetWindowAttribute(hwnd, 35, ref caption, 4);
        DwmSetWindowAttribute(hwnd, 34, ref border,  4);
    }

    /// <summary>
    /// Applies the Explorer dark-mode visual style to a control (ListView scrollbar,
    /// CheckBox glyph, etc.).
    /// </summary>
    internal static void ApplyToControl(nint hwnd) =>
        SetWindowTheme(hwnd, "DarkMode_Explorer", null);
}
