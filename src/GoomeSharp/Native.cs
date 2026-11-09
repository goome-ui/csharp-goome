using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GoomeSharp;

internal static unsafe partial class Native
{
    const string Lib = "goome";

    [LibraryImport(Lib, EntryPoint = "goome_open")] internal static partial IntPtr Open(RawOpts* o, int* err);
    [LibraryImport(Lib, EntryPoint = "goome_close")] internal static partial void Close(IntPtr g);
    [LibraryImport(Lib, EntryPoint = "goome_error")] internal static partial byte* Error(IntPtr g);
    [LibraryImport(Lib, EntryPoint = "goome_strerror")] internal static partial byte* StrError(int err);
    [LibraryImport(Lib, EntryPoint = "goome_backend_name")] internal static partial byte* BackendName(IntPtr g);
    [LibraryImport(Lib, EntryPoint = "goome_caps")] internal static partial int Caps(IntPtr g);
    [LibraryImport(Lib, EntryPoint = "goome_monitors")] internal static partial int Monitors(IntPtr g, RawMonitor* o, int max);
    [LibraryImport(Lib, EntryPoint = "goome_capture")] internal static partial int Capture(IntPtr g, int mon, Rect* r, RawImage* img);
    [LibraryImport(Lib, EntryPoint = "goome_alpha_at")] internal static partial int AlphaAt(IntPtr g, double x, double y);
    [LibraryImport(Lib, EntryPoint = "goome_begin")] internal static partial RawCanvas* Begin(IntPtr g, int mon);
    [LibraryImport(Lib, EntryPoint = "goome_end")] internal static partial int End(IntPtr g, RawCanvas* c, Rect* hits, int n);
    [LibraryImport(Lib, EntryPoint = "goome_damage")] internal static partial void Damage(RawCanvas* c, Rect* r);
    [LibraryImport(Lib, EntryPoint = "goome_poll")] internal static partial int Poll(IntPtr g, RawEvent* ev, int timeout);
    [LibraryImport(Lib, EntryPoint = "goome_pointer")] internal static partial int Pointer(IntPtr g, double* x, double* y, int* b);
    [LibraryImport(Lib, EntryPoint = "goome_keyboard")] internal static partial int Keyboard(IntPtr g, int on);
    [LibraryImport(Lib, EntryPoint = "goome_hotkey", StringMarshalling = StringMarshalling.Utf8)] internal static partial int Hotkey(IntPtr g, string combo, int id);
    [LibraryImport(Lib, EntryPoint = "goome_parse_combo", StringMarshalling = StringMarshalling.Utf8)] internal static partial int ParseCombo(string combo, int* key, int* mods);
    [LibraryImport(Lib, EntryPoint = "goome_control_path")] internal static partial byte* ControlPath(IntPtr g);
    [LibraryImport(Lib, EntryPoint = "goome_send", StringMarshalling = StringMarshalling.Utf8)] internal static partial int Send(string dir, string msg);
    [LibraryImport(Lib, EntryPoint = "goome_pace")] internal static partial int Pace(Pacer* p, int fps);
    [LibraryImport(Lib, EntryPoint = "goome_now_ms")] internal static partial CLong NowMs();
    [LibraryImport(Lib, EntryPoint = "goome_save_ppm", StringMarshalling = StringMarshalling.Utf8)] internal static partial int SavePpm(string path, uint* px, int w, int h, int stride);

    [LibraryImport(Lib, EntryPoint = "goome_clear")] internal static partial void Clear(RawCanvas* c);
    [LibraryImport(Lib, EntryPoint = "goome_draw_rect")] internal static partial void DrawRect(RawCanvas* c, double x, double y, double w, double h, uint argb);
    [LibraryImport(Lib, EntryPoint = "goome_draw_round_rect")] internal static partial void DrawRoundRect(RawCanvas* c, double x, double y, double w, double h, double r, uint argb);
    [LibraryImport(Lib, EntryPoint = "goome_draw_frame")] internal static partial void DrawFrame(RawCanvas* c, double x, double y, double w, double h, double width, uint argb);
    [LibraryImport(Lib, EntryPoint = "goome_draw_line")] internal static partial void DrawLine(RawCanvas* c, double x0, double y0, double x1, double y1, double width, uint argb);
    [LibraryImport(Lib, EntryPoint = "goome_draw_triangle")] internal static partial void DrawTriangle(RawCanvas* c, double x0, double y0, double x1, double y1, double x2, double y2, uint argb);
    [LibraryImport(Lib, EntryPoint = "goome_draw_image")] internal static partial void DrawImage(RawCanvas* c, double x, double y, double w, double h, uint* img, int iw, int ih);
    [LibraryImport(Lib, EntryPoint = "goome_erase")] internal static partial void Erase(RawCanvas* c, double x, double y, double w, double h);
    [LibraryImport(Lib, EntryPoint = "goome_draw_text", StringMarshalling = StringMarshalling.Utf8)] internal static partial double DrawText(RawCanvas* c, double x, double y, string s, int big, uint argb);
    [LibraryImport(Lib, EntryPoint = "goome_text_width", StringMarshalling = StringMarshalling.Utf8)] internal static partial double TextWidth(RawCanvas* c, string s, int big);
    [LibraryImport(Lib, EntryPoint = "goome_set_font")] internal static partial void SetFont(int font);
    [LibraryImport(Lib, EntryPoint = "goome_font_count")] internal static partial int FontCount();
    [LibraryImport(Lib, EntryPoint = "goome_canvas_blit")] internal static partial void Blit(RawCanvas* dst, RawCanvas* src, double alpha, double dx, double dy);
    [LibraryImport(Lib, EntryPoint = "goome_radius")] internal static partial Radii Radius(double r);
    [LibraryImport(Lib, EntryPoint = "goome_clip_push")] internal static partial void ClipPush(RawCanvas* c, double x, double y, double w, double h);
    [LibraryImport(Lib, EntryPoint = "goome_clip_pop")] internal static partial void ClipPop(RawCanvas* c);
    [LibraryImport(Lib, EntryPoint = "goome_draw_rrect")] internal static partial void DrawRRect(RawCanvas* c, double x, double y, double w, double h, Radii r, uint argb);
    [LibraryImport(Lib, EntryPoint = "goome_draw_rrect_line")] internal static partial void DrawRRectLine(RawCanvas* c, double x, double y, double w, double h, Radii r, double width, uint argb);
    [LibraryImport(Lib, EntryPoint = "goome_draw_rrect_grad")] internal static partial void DrawRRectGrad(RawCanvas* c, double x, double y, double w, double h, Radii r, uint from, uint to, int sideways);
    [LibraryImport(Lib, EntryPoint = "goome_draw_shadow")] internal static partial void DrawShadow(RawCanvas* c, double x, double y, double w, double h, double radius, double blur, uint argb);
    [LibraryImport(Lib, EntryPoint = "goome_draw_circle")] internal static partial void DrawCircle(RawCanvas* c, double cx, double cy, double r, uint argb);
    [LibraryImport(Lib, EntryPoint = "goome_draw_ring")] internal static partial void DrawRing(RawCanvas* c, double cx, double cy, double r, double width, uint argb);
    [LibraryImport(Lib, EntryPoint = "goome_draw_arc")] internal static partial void DrawArc(RawCanvas* c, double cx, double cy, double r, double width, double a0, double a1, uint argb);
    [LibraryImport(Lib, EntryPoint = "goome_draw_poly")] internal static partial void DrawPoly(RawCanvas* c, double* xy, int n, uint argb);

    [LibraryImport(Lib, EntryPoint = "goome_bitmap_load", StringMarshalling = StringMarshalling.Utf8)] internal static partial int BitmapLoad(string path, RawBitmap* o);
    [LibraryImport(Lib, EntryPoint = "goome_bitmap_load_mem")] internal static partial int BitmapLoadMem(void* data, nuint n, RawBitmap* o);
    [LibraryImport(Lib, EntryPoint = "goome_bitmap_error")] internal static partial byte* BitmapError();
    [LibraryImport(Lib, EntryPoint = "goome_bitmap_from_argb")] internal static partial int BitmapFromArgb(uint* argb, int w, int h, RawBitmap* o);
    [LibraryImport(Lib, EntryPoint = "goome_bitmap_free")] internal static partial void BitmapFree(RawBitmap* b);
    [LibraryImport(Lib, EntryPoint = "goome_draw_bitmap")] internal static partial void DrawBitmap(RawCanvas* c, RawBitmap* b, double x, double y, double w, double h, Radii r, uint tint);
    [LibraryImport(Lib, EntryPoint = "goome_draw_bitmap_part")] internal static partial void DrawBitmapPart(RawCanvas* c, RawBitmap* b, int sx, int sy, int sw, int sh, double x, double y, double w, double h, uint tint);

    [LibraryImport(Lib, EntryPoint = "goome_font_load", StringMarshalling = StringMarshalling.Utf8)] internal static partial IntPtr FontLoad(string path);
    [LibraryImport(Lib, EntryPoint = "goome_font_load_index", StringMarshalling = StringMarshalling.Utf8)] internal static partial IntPtr FontLoadIndex(string path, int index);
    [LibraryImport(Lib, EntryPoint = "goome_font_load_mem")] internal static partial IntPtr FontLoadMem(void* data, nuint n, int copy);
    [LibraryImport(Lib, EntryPoint = "goome_font_system", StringMarshalling = StringMarshalling.Utf8)] internal static partial IntPtr FontSystem(string? name);
    [LibraryImport(Lib, EntryPoint = "goome_font_free")] internal static partial void FontFree(IntPtr f);
    [LibraryImport(Lib, EntryPoint = "goome_text", StringMarshalling = StringMarshalling.Utf8)] internal static partial double Text(RawCanvas* c, IntPtr f, double size, double x, double y, string s, uint argb);
    [LibraryImport(Lib, EntryPoint = "goome_text_measure", StringMarshalling = StringMarshalling.Utf8)] internal static partial double TextMeasure(IntPtr f, double size, string s, double* h);
    [LibraryImport(Lib, EntryPoint = "goome_text_line_height")] internal static partial double TextLineHeight(IntPtr f, double size);

    [LibraryImport(Lib, EntryPoint = "goome_ui_new")] internal static partial IntPtr UiNew(IntPtr g);
    [LibraryImport(Lib, EntryPoint = "goome_ui_free")] internal static partial void UiFree(IntPtr u);
    [LibraryImport(Lib, EntryPoint = "goome_ui_theme")] internal static partial RawTheme* UiTheme(IntPtr u);
    [LibraryImport(Lib, EntryPoint = "goome_ui_theme_dark")] internal static partial void UiThemeDark(RawTheme* t);
    [LibraryImport(Lib, EntryPoint = "goome_ui_theme_light")] internal static partial void UiThemeLight(RawTheme* t);
    [LibraryImport(Lib, EntryPoint = "goome_ui_feed")] internal static partial int UiFeed(IntPtr u, RawEvent* ev);
    [LibraryImport(Lib, EntryPoint = "goome_ui_begin")] internal static partial RawCanvas* UiBegin(IntPtr u, int mon);
    [LibraryImport(Lib, EntryPoint = "goome_ui_end")] internal static partial void UiEnd(IntPtr u);
    [LibraryImport(Lib, EntryPoint = "goome_ui_hit")] internal static partial void UiHit(IntPtr u, double x, double y, double w, double h);
    [LibraryImport(Lib, EntryPoint = "goome_ui_animating")] internal static partial int UiAnimating(IntPtr u);
    [LibraryImport(Lib, EntryPoint = "goome_ui_window", StringMarshalling = StringMarshalling.Utf8)] internal static partial int UiWindow(IntPtr u, string title, double* x, double* y, double* w, double* h, int flags, int* open);
    [LibraryImport(Lib, EntryPoint = "goome_ui_window_end")] internal static partial void UiWindowEnd(IntPtr u);
    [LibraryImport(Lib, EntryPoint = "goome_ui_row")] internal static partial void UiRow(IntPtr u, int cols);
    [LibraryImport(Lib, EntryPoint = "goome_ui_same_line")] internal static partial void UiSameLine(IntPtr u);
    [LibraryImport(Lib, EntryPoint = "goome_ui_space")] internal static partial void UiSpace(IntPtr u, double px);
    [LibraryImport(Lib, EntryPoint = "goome_ui_separator")] internal static partial void UiSeparator(IntPtr u);
    [LibraryImport(Lib, EntryPoint = "goome_ui_width")] internal static partial void UiWidth(IntPtr u, double w);
    [LibraryImport(Lib, EntryPoint = "goomesharp_ui_label", StringMarshalling = StringMarshalling.Utf8)] internal static partial void UiLabel(IntPtr u, string text);
    [LibraryImport(Lib, EntryPoint = "goome_ui_heading", StringMarshalling = StringMarshalling.Utf8)] internal static partial void UiHeading(IntPtr u, string text);
    [LibraryImport(Lib, EntryPoint = "goome_ui_wrapped", StringMarshalling = StringMarshalling.Utf8)] internal static partial void UiWrapped(IntPtr u, string text);
    [LibraryImport(Lib, EntryPoint = "goome_ui_button", StringMarshalling = StringMarshalling.Utf8)] internal static partial int UiButton(IntPtr u, string label);
    [LibraryImport(Lib, EntryPoint = "goome_ui_image_button", StringMarshalling = StringMarshalling.Utf8)] internal static partial int UiImageButton(IntPtr u, string id, RawBitmap* b, double w, double h);
    [LibraryImport(Lib, EntryPoint = "goome_ui_image")] internal static partial void UiImage(IntPtr u, RawBitmap* b, double w, double h, double radius);
    [LibraryImport(Lib, EntryPoint = "goome_ui_checkbox", StringMarshalling = StringMarshalling.Utf8)] internal static partial int UiCheckbox(IntPtr u, string label, int* v);
    [LibraryImport(Lib, EntryPoint = "goome_ui_toggle", StringMarshalling = StringMarshalling.Utf8)] internal static partial int UiToggle(IntPtr u, string label, int* v);
    [LibraryImport(Lib, EntryPoint = "goome_ui_radio", StringMarshalling = StringMarshalling.Utf8)] internal static partial int UiRadio(IntPtr u, string label, int* v, int value);
    [LibraryImport(Lib, EntryPoint = "goome_ui_slider", StringMarshalling = StringMarshalling.Utf8)] internal static partial int UiSlider(IntPtr u, string label, double* v, double min, double max, string? fmt);
    [LibraryImport(Lib, EntryPoint = "goome_ui_slider_int", StringMarshalling = StringMarshalling.Utf8)] internal static partial int UiSliderInt(IntPtr u, string label, int* v, int min, int max);
    [LibraryImport(Lib, EntryPoint = "goome_ui_input", StringMarshalling = StringMarshalling.Utf8)] internal static partial int UiInput(IntPtr u, string label, byte* buf, int cap);
    [LibraryImport(Lib, EntryPoint = "goome_ui_combo", StringMarshalling = StringMarshalling.Utf8)] internal static partial int UiCombo(IntPtr u, string label, int* sel, byte** items, int n);
    [LibraryImport(Lib, EntryPoint = "goome_ui_selectable", StringMarshalling = StringMarshalling.Utf8)] internal static partial int UiSelectable(IntPtr u, string label, int selected);
    [LibraryImport(Lib, EntryPoint = "goome_ui_tabs", StringMarshalling = StringMarshalling.Utf8)] internal static partial int UiTabs(IntPtr u, string id, byte** names, int n, int* sel);
    [LibraryImport(Lib, EntryPoint = "goome_ui_collapsing", StringMarshalling = StringMarshalling.Utf8)] internal static partial int UiCollapsing(IntPtr u, string label, int* open);
    [LibraryImport(Lib, EntryPoint = "goome_ui_progress", StringMarshalling = StringMarshalling.Utf8)] internal static partial void UiProgress(IntPtr u, double frac, string? text);
    [LibraryImport(Lib, EntryPoint = "goome_ui_color", StringMarshalling = StringMarshalling.Utf8)] internal static partial int UiColor(IntPtr u, string label, uint* argb);
    [LibraryImport(Lib, EntryPoint = "goome_ui_tooltip", StringMarshalling = StringMarshalling.Utf8)] internal static partial void UiTooltip(IntPtr u, string text);
    [LibraryImport(Lib, EntryPoint = "goome_ui_hovered")] internal static partial int UiHovered(IntPtr u);
    [LibraryImport(Lib, EntryPoint = "goome_ui_scroll_begin", StringMarshalling = StringMarshalling.Utf8)] internal static partial int UiScrollBegin(IntPtr u, string id, double h);
    [LibraryImport(Lib, EntryPoint = "goome_ui_scroll_end")] internal static partial void UiScrollEnd(IntPtr u);
    [LibraryImport(Lib, EntryPoint = "goome_ui_push_id", StringMarshalling = StringMarshalling.Utf8)] internal static partial void UiPushId(IntPtr u, string id);
    [LibraryImport(Lib, EntryPoint = "goome_ui_pop_id")] internal static partial void UiPopId(IntPtr u);

    internal static string? Str(byte* p) => p == null ? null : Marshal.PtrToStringUTF8((IntPtr)p);

    [ModuleInitializer]
    internal static void Init()
    {
        try
        {
            NativeLibrary.SetDllImportResolver(typeof(Native).Assembly, Resolve);
        }
        catch (InvalidOperationException)
        {
        }
    }

    static IntPtr Resolve(string name, System.Reflection.Assembly asm, DllImportSearchPath? path)
    {
        if (name != Lib) return IntPtr.Zero;
        string file = OperatingSystem.IsWindows() ? "goome.dll" : OperatingSystem.IsMacOS() ? "libgoome.dylib" : "libgoome.so";
        string? env = Environment.GetEnvironmentVariable("GOOME_NATIVE");
        if (!string.IsNullOrEmpty(env))
        {
            string p = System.IO.Directory.Exists(env) ? System.IO.Path.Combine(env, file) : env;
            if (NativeLibrary.TryLoad(p, out IntPtr h)) return h;
        }
        string os = OperatingSystem.IsWindows() ? "win" : OperatingSystem.IsMacOS() ? "osx" : "linux";
        string arch = RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();
        string dir = AppContext.BaseDirectory;
        foreach (string p in new[] { System.IO.Path.Combine(dir, "runtimes", $"{os}-{arch}", "native", file), System.IO.Path.Combine(dir, file) })
            if (NativeLibrary.TryLoad(p, out IntPtr h)) return h;
        return IntPtr.Zero;
    }
}
