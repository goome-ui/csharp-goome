#nullable enable
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace GoomeSharp
{
    internal static unsafe class Native
    {
        const string Lib = "goome";

        [DllImport(Lib, EntryPoint = "goome_open", ExactSpelling = true)] internal static extern IntPtr Open(RawOpts* o, int* err);
        [DllImport(Lib, EntryPoint = "goome_open_app", ExactSpelling = true)] internal static extern IntPtr OpenApp(byte* title, int w, int h, int* err);
        [DllImport(Lib, EntryPoint = "goome_close", ExactSpelling = true)] internal static extern void Close(IntPtr g);
        [DllImport(Lib, EntryPoint = "goome_error", ExactSpelling = true)] internal static extern byte* Error(IntPtr g);
        [DllImport(Lib, EntryPoint = "goome_strerror", ExactSpelling = true)] internal static extern byte* StrError(int err);
        [DllImport(Lib, EntryPoint = "goome_backend_name", ExactSpelling = true)] internal static extern byte* BackendName(IntPtr g);
        [DllImport(Lib, EntryPoint = "goome_caps", ExactSpelling = true)] internal static extern int Caps(IntPtr g);
        [DllImport(Lib, EntryPoint = "goome_mode", ExactSpelling = true)] internal static extern int Mode(IntPtr g);
        [DllImport(Lib, EntryPoint = "goome_monitors", ExactSpelling = true)] internal static extern int Monitors(IntPtr g, RawMonitor* o, int max);
        [DllImport(Lib, EntryPoint = "goome_capture", ExactSpelling = true)] internal static extern int Capture(IntPtr g, int mon, Rect* r, RawImage* img);
        [DllImport(Lib, EntryPoint = "goome_alpha_at", ExactSpelling = true)] internal static extern int AlphaAt(IntPtr g, double x, double y);
        [DllImport(Lib, EntryPoint = "goome_begin", ExactSpelling = true)] internal static extern RawCanvas* Begin(IntPtr g, int mon);
        [DllImport(Lib, EntryPoint = "goome_end", ExactSpelling = true)] internal static extern int End(IntPtr g, RawCanvas* c, Rect* hits, int n);
        [DllImport(Lib, EntryPoint = "goome_damage", ExactSpelling = true)] internal static extern void Damage(RawCanvas* c, Rect* r);
        [DllImport(Lib, EntryPoint = "goome_poll", ExactSpelling = true)] internal static extern int Poll(IntPtr g, RawEvent* ev, int timeout);
        [DllImport(Lib, EntryPoint = "goome_pointer", ExactSpelling = true)] internal static extern int Pointer(IntPtr g, double* x, double* y, int* b);
        [DllImport(Lib, EntryPoint = "goome_keyboard", ExactSpelling = true)] internal static extern int Keyboard(IntPtr g, int on);
        [DllImport(Lib, EntryPoint = "goome_grab", ExactSpelling = true)] internal static extern int Grab(IntPtr g, int on);
        [DllImport(Lib, EntryPoint = "goome_grabbed", ExactSpelling = true)] internal static extern int Grabbed(IntPtr g);
        [DllImport(Lib, EntryPoint = "goome_hotkey", ExactSpelling = true)] internal static extern int Hotkey(IntPtr g, byte* combo, int id);
        [DllImport(Lib, EntryPoint = "goome_parse_combo", ExactSpelling = true)] internal static extern int ParseCombo(byte* combo, int* key, int* mods);
        [DllImport(Lib, EntryPoint = "goome_control_path", ExactSpelling = true)] internal static extern byte* ControlPath(IntPtr g);
        [DllImport(Lib, EntryPoint = "goome_send", ExactSpelling = true)] internal static extern int Send(byte* dir, byte* msg);
        [DllImport(Lib, EntryPoint = "goome_pace", ExactSpelling = true)] internal static extern int Pace(Pacer* p, int fps);
        [DllImport(Lib, EntryPoint = "goome_now_ms", ExactSpelling = true)] static extern int NowMs32();
        [DllImport(Lib, EntryPoint = "goome_now_ms", ExactSpelling = true)] static extern IntPtr NowMs64();
        [DllImport(Lib, EntryPoint = "goome_save_ppm", ExactSpelling = true)] internal static extern int SavePpm(byte* path, uint* px, int w, int h, int stride);

        [DllImport(Lib, EntryPoint = "goome_window_title", ExactSpelling = true)] internal static extern int WindowTitle(IntPtr g, byte* title);
        [DllImport(Lib, EntryPoint = "goome_window_size", ExactSpelling = true)] internal static extern int WindowSize(IntPtr g, int w, int h);
        [DllImport(Lib, EntryPoint = "goome_window_pos", ExactSpelling = true)] internal static extern int WindowPos(IntPtr g, int* x, int* y, int set);
        [DllImport(Lib, EntryPoint = "goome_window_state", ExactSpelling = true)] internal static extern int WindowState(IntPtr g, int state);
        [DllImport(Lib, EntryPoint = "goome_window_get_state", ExactSpelling = true)] internal static extern int WindowGetState(IntPtr g);
        [DllImport(Lib, EntryPoint = "goome_window_drag", ExactSpelling = true)] internal static extern int WindowDrag(IntPtr g, int edges);
        [DllImport(Lib, EntryPoint = "goome_window_show", ExactSpelling = true)] internal static extern int WindowShow(IntPtr g, int on);
        [DllImport(Lib, EntryPoint = "goome_cursor", ExactSpelling = true)] internal static extern int Cursor(IntPtr g, int shape);
        [DllImport(Lib, EntryPoint = "goome_cursor_shape", ExactSpelling = true)] internal static extern int CursorShape(IntPtr g);
        [DllImport(Lib, EntryPoint = "goome_clipboard_set", ExactSpelling = true)] internal static extern int ClipboardSet(IntPtr g, byte* text);
        [DllImport(Lib, EntryPoint = "goome_clipboard_get", ExactSpelling = true)] internal static extern int ClipboardGet(IntPtr g, byte* buf, int cap);
        [DllImport(Lib, EntryPoint = "goome_drop_path", ExactSpelling = true)] internal static extern byte* DropPath(IntPtr g);
        [DllImport(Lib, EntryPoint = "goome_push", ExactSpelling = true)] internal static extern int Push(IntPtr g, RawEvent* ev);
        [DllImport(Lib, EntryPoint = "goome_resize", ExactSpelling = true)] internal static extern int Resize(IntPtr g, int w, int h, double scale);
        [DllImport(Lib, EntryPoint = "goome_frame", ExactSpelling = true)] internal static extern int Frame(IntPtr g, RawImage* img);
        [DllImport(Lib, EntryPoint = "goome_hit", ExactSpelling = true)] internal static extern int Hit(IntPtr g, double x, double y);
        [DllImport(Lib, EntryPoint = "goome_frame_dirty", ExactSpelling = true)] internal static extern int FrameDirty(IntPtr g, Rect* r);
        [DllImport(Lib, EntryPoint = "goome_frame_copy", ExactSpelling = true)] internal static extern int FrameCopy(IntPtr g, void* dst, int stride, int format, Rect* r);

        [DllImport(Lib, EntryPoint = "goome_clear", ExactSpelling = true)] internal static extern void Clear(RawCanvas* c);
        [DllImport(Lib, EntryPoint = "goome_draw_rect", ExactSpelling = true)] internal static extern void DrawRect(RawCanvas* c, double x, double y, double w, double h, uint argb);
        [DllImport(Lib, EntryPoint = "goome_draw_round_rect", ExactSpelling = true)] internal static extern void DrawRoundRect(RawCanvas* c, double x, double y, double w, double h, double r, uint argb);
        [DllImport(Lib, EntryPoint = "goome_draw_frame", ExactSpelling = true)] internal static extern void DrawFrame(RawCanvas* c, double x, double y, double w, double h, double width, uint argb);
        [DllImport(Lib, EntryPoint = "goome_draw_line", ExactSpelling = true)] internal static extern void DrawLine(RawCanvas* c, double x0, double y0, double x1, double y1, double width, uint argb);
        [DllImport(Lib, EntryPoint = "goome_draw_triangle", ExactSpelling = true)] internal static extern void DrawTriangle(RawCanvas* c, double x0, double y0, double x1, double y1, double x2, double y2, uint argb);
        [DllImport(Lib, EntryPoint = "goome_draw_image", ExactSpelling = true)] internal static extern void DrawImage(RawCanvas* c, double x, double y, double w, double h, uint* img, int iw, int ih);
        [DllImport(Lib, EntryPoint = "goome_erase", ExactSpelling = true)] internal static extern void Erase(RawCanvas* c, double x, double y, double w, double h);
        [DllImport(Lib, EntryPoint = "goome_draw_text", ExactSpelling = true)] internal static extern double DrawText(RawCanvas* c, double x, double y, byte* s, int big, uint argb);
        [DllImport(Lib, EntryPoint = "goome_text_width", ExactSpelling = true)] internal static extern double TextWidth(RawCanvas* c, byte* s, int big);
        [DllImport(Lib, EntryPoint = "goome_set_font", ExactSpelling = true)] internal static extern void SetFont(int font);
        [DllImport(Lib, EntryPoint = "goome_font_count", ExactSpelling = true)] internal static extern int FontCount();
        [DllImport(Lib, EntryPoint = "goome_canvas_blit", ExactSpelling = true)] internal static extern void Blit(RawCanvas* dst, RawCanvas* src, double alpha, double dx, double dy);
        [DllImport(Lib, EntryPoint = "goome_radius", ExactSpelling = true)] internal static extern Radii Radius(double r);
        [DllImport(Lib, EntryPoint = "goome_clip_push", ExactSpelling = true)] internal static extern void ClipPush(RawCanvas* c, double x, double y, double w, double h);
        [DllImport(Lib, EntryPoint = "goome_clip_pop", ExactSpelling = true)] internal static extern void ClipPop(RawCanvas* c);
        [DllImport(Lib, EntryPoint = "goome_draw_rrect", ExactSpelling = true)] internal static extern void DrawRRect(RawCanvas* c, double x, double y, double w, double h, Radii r, uint argb);
        [DllImport(Lib, EntryPoint = "goome_draw_rrect_line", ExactSpelling = true)] internal static extern void DrawRRectLine(RawCanvas* c, double x, double y, double w, double h, Radii r, double width, uint argb);
        [DllImport(Lib, EntryPoint = "goome_draw_rrect_grad", ExactSpelling = true)] internal static extern void DrawRRectGrad(RawCanvas* c, double x, double y, double w, double h, Radii r, uint from, uint to, int sideways);
        [DllImport(Lib, EntryPoint = "goome_draw_shadow", ExactSpelling = true)] internal static extern void DrawShadow(RawCanvas* c, double x, double y, double w, double h, double radius, double blur, uint argb);
        [DllImport(Lib, EntryPoint = "goome_draw_circle", ExactSpelling = true)] internal static extern void DrawCircle(RawCanvas* c, double cx, double cy, double r, uint argb);
        [DllImport(Lib, EntryPoint = "goome_draw_ring", ExactSpelling = true)] internal static extern void DrawRing(RawCanvas* c, double cx, double cy, double r, double width, uint argb);
        [DllImport(Lib, EntryPoint = "goome_draw_arc", ExactSpelling = true)] internal static extern void DrawArc(RawCanvas* c, double cx, double cy, double r, double width, double a0, double a1, uint argb);
        [DllImport(Lib, EntryPoint = "goome_draw_poly", ExactSpelling = true)] internal static extern void DrawPoly(RawCanvas* c, double* xy, int n, uint argb);

        [DllImport(Lib, EntryPoint = "goome_bitmap_load", ExactSpelling = true)] internal static extern int BitmapLoad(byte* path, RawBitmap* o);
        [DllImport(Lib, EntryPoint = "goome_bitmap_load_mem", ExactSpelling = true)] internal static extern int BitmapLoadMem(void* data, UIntPtr n, RawBitmap* o);
        [DllImport(Lib, EntryPoint = "goome_bitmap_error", ExactSpelling = true)] internal static extern byte* BitmapError();
        [DllImport(Lib, EntryPoint = "goome_bitmap_from_argb", ExactSpelling = true)] internal static extern int BitmapFromArgb(uint* argb, int w, int h, RawBitmap* o);
        [DllImport(Lib, EntryPoint = "goome_bitmap_free", ExactSpelling = true)] internal static extern void BitmapFree(RawBitmap* b);
        [DllImport(Lib, EntryPoint = "goome_draw_bitmap", ExactSpelling = true)] internal static extern void DrawBitmap(RawCanvas* c, RawBitmap* b, double x, double y, double w, double h, Radii r, uint tint);
        [DllImport(Lib, EntryPoint = "goome_draw_bitmap_part", ExactSpelling = true)] internal static extern void DrawBitmapPart(RawCanvas* c, RawBitmap* b, int sx, int sy, int sw, int sh, double x, double y, double w, double h, uint tint);
        [DllImport(Lib, EntryPoint = "goome_draw_bitmap_nine", ExactSpelling = true)] internal static extern void DrawBitmapNine(RawCanvas* c, RawBitmap* b, double x, double y, double w, double h, double l, double t, double r, double bm);

        [DllImport(Lib, EntryPoint = "goome_font_load", ExactSpelling = true)] internal static extern IntPtr FontLoad(byte* path);
        [DllImport(Lib, EntryPoint = "goome_font_load_index", ExactSpelling = true)] internal static extern IntPtr FontLoadIndex(byte* path, int index);
        [DllImport(Lib, EntryPoint = "goome_font_load_mem", ExactSpelling = true)] internal static extern IntPtr FontLoadMem(void* data, UIntPtr n, int copy);
        [DllImport(Lib, EntryPoint = "goome_font_system", ExactSpelling = true)] internal static extern IntPtr FontSystem(byte* name);
        [DllImport(Lib, EntryPoint = "goome_font_free", ExactSpelling = true)] internal static extern void FontFree(IntPtr f);
        [DllImport(Lib, EntryPoint = "goome_text", ExactSpelling = true)] internal static extern double Text(RawCanvas* c, IntPtr f, double size, double x, double y, byte* s, uint argb);
        [DllImport(Lib, EntryPoint = "goome_text_measure", ExactSpelling = true)] internal static extern double TextMeasure(IntPtr f, double size, byte* s, double* h);
        [DllImport(Lib, EntryPoint = "goome_text_line_height", ExactSpelling = true)] internal static extern double TextLineHeight(IntPtr f, double size);

        [DllImport(Lib, EntryPoint = "goome_ui_new", ExactSpelling = true)] internal static extern IntPtr UiNew(IntPtr g);
        [DllImport(Lib, EntryPoint = "goome_ui_free", ExactSpelling = true)] internal static extern void UiFree(IntPtr u);
        [DllImport(Lib, EntryPoint = "goome_ui_theme", ExactSpelling = true)] internal static extern RawTheme* UiTheme(IntPtr u);
        [DllImport(Lib, EntryPoint = "goome_ui_theme_dark", ExactSpelling = true)] internal static extern void UiThemeDark(RawTheme* t);
        [DllImport(Lib, EntryPoint = "goome_ui_theme_light", ExactSpelling = true)] internal static extern void UiThemeLight(RawTheme* t);
        [DllImport(Lib, EntryPoint = "goome_ui_feed", ExactSpelling = true)] internal static extern int UiFeed(IntPtr u, RawEvent* ev);
        [DllImport(Lib, EntryPoint = "goome_ui_begin", ExactSpelling = true)] internal static extern RawCanvas* UiBegin(IntPtr u, int mon);
        [DllImport(Lib, EntryPoint = "goome_ui_end", ExactSpelling = true)] internal static extern void UiEnd(IntPtr u);
        [DllImport(Lib, EntryPoint = "goome_ui_hit", ExactSpelling = true)] internal static extern void UiHit(IntPtr u, double x, double y, double w, double h);
        [DllImport(Lib, EntryPoint = "goome_ui_animating", ExactSpelling = true)] internal static extern int UiAnimating(IntPtr u);
        [DllImport(Lib, EntryPoint = "goome_ui_wants", ExactSpelling = true)] internal static extern int UiWants(IntPtr u);
        [DllImport(Lib, EntryPoint = "goome_ui_auto_grab", ExactSpelling = true)] internal static extern void UiAutoGrab(IntPtr u, int wants);
        [DllImport(Lib, EntryPoint = "goome_ui_window", ExactSpelling = true)] internal static extern int UiWindow(IntPtr u, byte* title, double* x, double* y, double* w, double* h, int flags, int* open);
        [DllImport(Lib, EntryPoint = "goome_ui_window_end", ExactSpelling = true)] internal static extern void UiWindowEnd(IntPtr u);
        [DllImport(Lib, EntryPoint = "goome_ui_row", ExactSpelling = true)] internal static extern void UiRow(IntPtr u, int cols);
        [DllImport(Lib, EntryPoint = "goome_ui_same_line", ExactSpelling = true)] internal static extern void UiSameLine(IntPtr u);
        [DllImport(Lib, EntryPoint = "goome_ui_space", ExactSpelling = true)] internal static extern void UiSpace(IntPtr u, double px);
        [DllImport(Lib, EntryPoint = "goome_ui_separator", ExactSpelling = true)] internal static extern void UiSeparator(IntPtr u);
        [DllImport(Lib, EntryPoint = "goome_ui_width", ExactSpelling = true)] internal static extern void UiWidth(IntPtr u, double w);
        [DllImport(Lib, EntryPoint = "gmcs_label", ExactSpelling = true)] internal static extern void UiLabel(IntPtr u, byte* text);
        [DllImport(Lib, EntryPoint = "goome_ui_heading", ExactSpelling = true)] internal static extern void UiHeading(IntPtr u, byte* text);
        [DllImport(Lib, EntryPoint = "goome_ui_wrapped", ExactSpelling = true)] internal static extern void UiWrapped(IntPtr u, byte* text);
        [DllImport(Lib, EntryPoint = "goome_ui_button", ExactSpelling = true)] internal static extern int UiButton(IntPtr u, byte* label);
        [DllImport(Lib, EntryPoint = "goome_ui_image_button", ExactSpelling = true)] internal static extern int UiImageButton(IntPtr u, byte* id, RawBitmap* b, double w, double h);
        [DllImport(Lib, EntryPoint = "goome_ui_image", ExactSpelling = true)] internal static extern void UiImage(IntPtr u, RawBitmap* b, double w, double h, double radius);
        [DllImport(Lib, EntryPoint = "goome_ui_checkbox", ExactSpelling = true)] internal static extern int UiCheckbox(IntPtr u, byte* label, int* v);
        [DllImport(Lib, EntryPoint = "goome_ui_toggle", ExactSpelling = true)] internal static extern int UiToggle(IntPtr u, byte* label, int* v);
        [DllImport(Lib, EntryPoint = "goome_ui_radio", ExactSpelling = true)] internal static extern int UiRadio(IntPtr u, byte* label, int* v, int value);
        [DllImport(Lib, EntryPoint = "goome_ui_slider", ExactSpelling = true)] internal static extern int UiSlider(IntPtr u, byte* label, double* v, double min, double max, byte* fmt);
        [DllImport(Lib, EntryPoint = "goome_ui_slider_int", ExactSpelling = true)] internal static extern int UiSliderInt(IntPtr u, byte* label, int* v, int min, int max);
        [DllImport(Lib, EntryPoint = "goome_ui_input", ExactSpelling = true)] internal static extern int UiInput(IntPtr u, byte* label, byte* buf, int cap);
        [DllImport(Lib, EntryPoint = "goome_ui_combo", ExactSpelling = true)] internal static extern int UiCombo(IntPtr u, byte* label, int* sel, byte** items, int n);
        [DllImport(Lib, EntryPoint = "goome_ui_selectable", ExactSpelling = true)] internal static extern int UiSelectable(IntPtr u, byte* label, int selected);
        [DllImport(Lib, EntryPoint = "goome_ui_tabs", ExactSpelling = true)] internal static extern int UiTabs(IntPtr u, byte* id, byte** names, int n, int* sel);
        [DllImport(Lib, EntryPoint = "goome_ui_collapsing", ExactSpelling = true)] internal static extern int UiCollapsing(IntPtr u, byte* label, int* open);
        [DllImport(Lib, EntryPoint = "goome_ui_progress", ExactSpelling = true)] internal static extern void UiProgress(IntPtr u, double frac, byte* text);
        [DllImport(Lib, EntryPoint = "goome_ui_color", ExactSpelling = true)] internal static extern int UiColor(IntPtr u, byte* label, uint* argb);
        [DllImport(Lib, EntryPoint = "goome_ui_tooltip", ExactSpelling = true)] internal static extern void UiTooltip(IntPtr u, byte* text);
        [DllImport(Lib, EntryPoint = "goome_ui_hovered", ExactSpelling = true)] internal static extern int UiHovered(IntPtr u);
        [DllImport(Lib, EntryPoint = "goome_ui_scroll_begin", ExactSpelling = true)] internal static extern int UiScrollBegin(IntPtr u, byte* id, double h);
        [DllImport(Lib, EntryPoint = "goome_ui_scroll_end", ExactSpelling = true)] internal static extern void UiScrollEnd(IntPtr u);
        [DllImport(Lib, EntryPoint = "goome_ui_push_id", ExactSpelling = true)] internal static extern void UiPushId(IntPtr u, byte* id);
        [DllImport(Lib, EntryPoint = "goome_ui_pop_id", ExactSpelling = true)] internal static extern void UiPopId(IntPtr u);

        [DllImport(Lib, EntryPoint = "goome_ui_app", ExactSpelling = true)] internal static extern int UiApp(IntPtr u, byte* title, int flags);
        [DllImport(Lib, EntryPoint = "goome_ui_app_end", ExactSpelling = true)] internal static extern void UiAppEnd(IntPtr u);
        [DllImport(Lib, EntryPoint = "goome_ui_menubar_begin", ExactSpelling = true)] internal static extern int UiMenubarBegin(IntPtr u);
        [DllImport(Lib, EntryPoint = "goome_ui_menubar_end", ExactSpelling = true)] internal static extern void UiMenubarEnd(IntPtr u);
        [DllImport(Lib, EntryPoint = "goome_ui_menu_begin", ExactSpelling = true)] internal static extern int UiMenuBegin(IntPtr u, byte* label);
        [DllImport(Lib, EntryPoint = "goome_ui_menu_end", ExactSpelling = true)] internal static extern void UiMenuEnd(IntPtr u);
        [DllImport(Lib, EntryPoint = "goome_ui_menu_item", ExactSpelling = true)] internal static extern int UiMenuItem(IntPtr u, byte* label, byte* shortcut, int* check);
        [DllImport(Lib, EntryPoint = "goome_ui_menu_separator", ExactSpelling = true)] internal static extern void UiMenuSeparator(IntPtr u);
        [DllImport(Lib, EntryPoint = "goome_ui_context_begin", ExactSpelling = true)] internal static extern int UiContextBegin(IntPtr u, byte* id);
        [DllImport(Lib, EntryPoint = "goome_ui_context_end", ExactSpelling = true)] internal static extern void UiContextEnd(IntPtr u);
        [DllImport(Lib, EntryPoint = "goome_ui_popup_open", ExactSpelling = true)] internal static extern void UiPopupOpen(IntPtr u, byte* id);
        [DllImport(Lib, EntryPoint = "goome_ui_popup_begin", ExactSpelling = true)] internal static extern int UiPopupBegin(IntPtr u, byte* id);
        [DllImport(Lib, EntryPoint = "goome_ui_popup_end", ExactSpelling = true)] internal static extern void UiPopupEnd(IntPtr u);
        [DllImport(Lib, EntryPoint = "goome_ui_popup_close", ExactSpelling = true)] internal static extern void UiPopupClose(IntPtr u);
        [DllImport(Lib, EntryPoint = "goome_ui_modal_begin", ExactSpelling = true)] internal static extern int UiModalBegin(IntPtr u, byte* title, int* open);
        [DllImport(Lib, EntryPoint = "goome_ui_modal_end", ExactSpelling = true)] internal static extern void UiModalEnd(IntPtr u);
        [DllImport(Lib, EntryPoint = "goome_ui_toast", ExactSpelling = true)] internal static extern void UiToast(IntPtr u, byte* text, int ms);
        [DllImport(Lib, EntryPoint = "goome_ui_statusbar", ExactSpelling = true)] internal static extern void UiStatusbar(IntPtr u, byte* text);
        [DllImport(Lib, EntryPoint = "goome_ui_focused", ExactSpelling = true)] internal static extern int UiFocused(IntPtr u);
        [DllImport(Lib, EntryPoint = "goome_ui_set_focus", ExactSpelling = true)] internal static extern void UiSetFocus(IntPtr u);

        [DllImport(Lib, EntryPoint = "goome_ui_text_edit", ExactSpelling = true)] internal static extern int UiTextEdit(IntPtr u, byte* id, byte* buf, int cap, double h, int flags);
        [DllImport(Lib, EntryPoint = "goome_ui_table_begin", ExactSpelling = true)] internal static extern int UiTableBegin(IntPtr u, byte* id, int cols, double h, int flags);
        [DllImport(Lib, EntryPoint = "goome_ui_table_column", ExactSpelling = true)] internal static extern void UiTableColumn(IntPtr u, byte* name, double width);
        [DllImport(Lib, EntryPoint = "goome_ui_table_sort", ExactSpelling = true)] internal static extern int UiTableSort(IntPtr u, int* col, int* desc);
        [DllImport(Lib, EntryPoint = "goome_ui_table_row", ExactSpelling = true)] internal static extern void UiTableRow(IntPtr u);
        [DllImport(Lib, EntryPoint = "goome_ui_table_next", ExactSpelling = true)] internal static extern void UiTableNext(IntPtr u);
        [DllImport(Lib, EntryPoint = "goome_ui_table_end", ExactSpelling = true)] internal static extern void UiTableEnd(IntPtr u);
        [DllImport(Lib, EntryPoint = "goome_ui_tree", ExactSpelling = true)] internal static extern int UiTree(IntPtr u, byte* label, int* open);
        [DllImport(Lib, EntryPoint = "goome_ui_tree_end", ExactSpelling = true)] internal static extern void UiTreeEnd(IntPtr u);
        [DllImport(Lib, EntryPoint = "goome_ui_list_clip", ExactSpelling = true)] internal static extern int UiListClip(IntPtr u, int count, double rowH, int* first, int* last);
        [DllImport(Lib, EntryPoint = "goome_ui_drag_float", ExactSpelling = true)] internal static extern int UiDragFloat(IntPtr u, byte* label, double* v, double speed, double min, double max, byte* fmt);
        [DllImport(Lib, EntryPoint = "goome_ui_drag_int", ExactSpelling = true)] internal static extern int UiDragInt(IntPtr u, byte* label, int* v, double speed, int min, int max);
        [DllImport(Lib, EntryPoint = "goome_ui_number", ExactSpelling = true)] internal static extern int UiNumber(IntPtr u, byte* label, double* v, double step, byte* fmt);
        [DllImport(Lib, EntryPoint = "goome_ui_plot_lines", ExactSpelling = true)] internal static extern void UiPlotLines(IntPtr u, byte* label, float* v, int n, double min, double max, double h);
        [DllImport(Lib, EntryPoint = "goome_ui_plot_bars", ExactSpelling = true)] internal static extern void UiPlotBars(IntPtr u, byte* label, float* v, int n, double min, double max, double h);
        [DllImport(Lib, EntryPoint = "goome_ui_split_begin", ExactSpelling = true)] internal static extern int UiSplitBegin(IntPtr u, byte* id, int vertical, double* frac);
        [DllImport(Lib, EntryPoint = "goome_ui_split_next", ExactSpelling = true)] internal static extern void UiSplitNext(IntPtr u);
        [DllImport(Lib, EntryPoint = "goome_ui_split_end", ExactSpelling = true)] internal static extern void UiSplitEnd(IntPtr u);
        [DllImport(Lib, EntryPoint = "goome_ui_drag_source", ExactSpelling = true)] internal static extern int UiDragSource(IntPtr u, byte* type, void* data, int len);
        [DllImport(Lib, EntryPoint = "goome_ui_drop_target", ExactSpelling = true)] internal static extern int UiDropTarget(IntPtr u, byte* type, void* buf, int cap);
        [DllImport(Lib, EntryPoint = "goome_ui_spinner", ExactSpelling = true)] internal static extern void UiSpinner(IntPtr u, double size);
        [DllImport(Lib, EntryPoint = "goome_ui_link", ExactSpelling = true)] internal static extern int UiLink(IntPtr u, byte* text);
        [DllImport(Lib, EntryPoint = "goome_ui_badge", ExactSpelling = true)] internal static extern void UiBadge(IntPtr u, byte* text, uint argb);
        [DllImport(Lib, EntryPoint = "goome_ui_pos", ExactSpelling = true)] internal static extern void UiPos(IntPtr u, double x, double y);
        [DllImport(Lib, EntryPoint = "goome_ui_size", ExactSpelling = true)] internal static extern void UiSize(IntPtr u, double w, double h);
        [DllImport(Lib, EntryPoint = "goome_ui_push_color", ExactSpelling = true)] internal static extern void UiPushColor(IntPtr u, int what, uint argb);
        [DllImport(Lib, EntryPoint = "goome_ui_push_num", ExactSpelling = true)] internal static extern void UiPushNum(IntPtr u, int what, double v);
        [DllImport(Lib, EntryPoint = "goome_ui_push_radii", ExactSpelling = true)] internal static extern void UiPushRadii(IntPtr u, Radii r);
        [DllImport(Lib, EntryPoint = "goome_ui_push_font", ExactSpelling = true)] internal static extern void UiPushFont(IntPtr u, IntPtr f);
        [DllImport(Lib, EntryPoint = "goome_ui_push_skin", ExactSpelling = true)] internal static extern void UiPushSkin(IntPtr u, int kind, RawSkin* s);
        [DllImport(Lib, EntryPoint = "goome_ui_push_theme", ExactSpelling = true)] internal static extern void UiPushTheme(IntPtr u, RawTheme* t);
        [DllImport(Lib, EntryPoint = "goome_ui_push_icon", ExactSpelling = true)] internal static extern void UiPushIcon(IntPtr u, int icon, RawBitmap* b);
        [DllImport(Lib, EntryPoint = "goome_ui_pop", ExactSpelling = true)] internal static extern void UiPop(IntPtr u, int n);
        [DllImport(Lib, EntryPoint = "goome_ui_custom", ExactSpelling = true)] internal static extern int UiCustom(IntPtr u, byte* id, double w, double h, int flags, Item* it);
        [DllImport(Lib, EntryPoint = "goome_ui_canvas", ExactSpelling = true)] internal static extern RawCanvas* UiCanvas(IntPtr u);
        [DllImport(Lib, EntryPoint = "goome_ui_store", ExactSpelling = true)] internal static extern double* UiStore(IntPtr u, byte* id, int n);
        [DllImport(Lib, EntryPoint = "goome_ui_theme_make", ExactSpelling = true)] internal static extern void UiThemeMake(RawTheme* t, uint accent, int dark, double radius);
        [DllImport(Lib, EntryPoint = "goome_ui_theme_preset", ExactSpelling = true)] internal static extern int UiThemePreset(RawTheme* t, int preset);
        [DllImport(Lib, EntryPoint = "goome_ui_scale", ExactSpelling = true)] internal static extern void UiScale(IntPtr u, double s);
        [DllImport(Lib, EntryPoint = "goome_ui_cfg", ExactSpelling = true)] internal static extern RawUiConfig* UiCfg(IntPtr u);
        [DllImport(Lib, EntryPoint = "goome_ui_config_default", ExactSpelling = true)] internal static extern void UiConfigDefault(RawUiConfig* c);
        [DllImport(Lib, EntryPoint = "goome_ui_bind", ExactSpelling = true)] internal static extern int UiBind(IntPtr u, int act, byte* combos);
        [DllImport(Lib, EntryPoint = "goome_ui_input_ex", ExactSpelling = true)] internal static extern int UiInputEx(IntPtr u, byte* label, byte* buf, int cap, int flags, byte* hint);

        internal static long NowMs() => Os.Windows ? NowMs32() : (long)NowMs64();

        #if NETSTANDARD2_0
        internal static string? Str(byte* p)
        {
            if (p == null) return null;
            int n = 0;
            while (p[n] != 0) n++;
            return Encoding.UTF8.GetString(p, n);
        }
#else
        internal static string? Str(byte* p) => p == null ? null : Marshal.PtrToStringUTF8((IntPtr)p);
#endif

#if NET5_0_OR_GREATER
        [System.Runtime.CompilerServices.ModuleInitializer]
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
            string file = Os.Windows ? "goome.dll" : Os.Mac ? "libgoome.dylib" : "libgoome.so";
            string? env = Environment.GetEnvironmentVariable("GOOME_NATIVE");
            if (!string.IsNullOrEmpty(env))
            {
                string p = System.IO.Directory.Exists(env) ? System.IO.Path.Combine(env, file) : env;
                if (NativeLibrary.TryLoad(p, out IntPtr h)) return h;
            }
            string os = Os.Windows ? "win" : Os.Mac ? "osx" : "linux";
            string arch = RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();
            string dir = AppContext.BaseDirectory;
            foreach (string p in new[] { System.IO.Path.Combine(dir, "runtimes", $"{os}-{arch}", "native", file), System.IO.Path.Combine(dir, file) })
                if (NativeLibrary.TryLoad(p, out IntPtr h)) return h;
            return IntPtr.Zero;
        }
#endif
    }

    internal static class Os
    {
        internal static readonly bool Windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        internal static readonly bool Mac = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    }

    internal static unsafe class Mem
    {
        internal static void* Alloc(long bytes)
        {
            var p = (byte*)Marshal.AllocHGlobal((IntPtr)Math.Max(1, bytes));
            new Span<byte>(p, (int)Math.Max(1, bytes)).Clear();
            return p;
        }

        internal static void Free(void* p)
        {
            if (p != null) Marshal.FreeHGlobal((IntPtr)p);
        }
    }

    internal static class U8
    {
        [ThreadStatic] static byte[]? a, b, c;

        internal static byte[]? A(string? s) => Fill(ref a, s);
        internal static byte[]? B(string? s) => Fill(ref b, s);
        internal static byte[]? C(string? s) => Fill(ref c, s);

#if NETSTANDARD2_0
        internal static IntPtr CoTask(string s)
        {
            var bytes = Encoding.UTF8.GetBytes(s);
            var p = Marshal.AllocCoTaskMem(bytes.Length + 1);
            Marshal.Copy(bytes, 0, p, bytes.Length);
            Marshal.WriteByte(p, bytes.Length, 0);
            return p;
        }
#else
        internal static IntPtr CoTask(string s) => Marshal.StringToCoTaskMemUTF8(s);
#endif

        static byte[]? Fill(ref byte[]? buf, string? s)
        {
            if (s == null) return null;
            int max = s.Length * 3 + 1;
            if (buf == null || buf.Length < max) buf = new byte[Math.Max(64, max)];
            int n = Encoding.UTF8.GetBytes(s, 0, s.Length, buf, 0);
            buf[n] = 0;
            return buf;
        }
    }
}
