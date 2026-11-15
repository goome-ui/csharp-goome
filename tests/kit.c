#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#include "goome.h"

static int fails;
static const char *gm_dump_dir;
#define CHECK(x)                                                                    \
    do {                                                                            \
        if (!(x)) printf("FAIL %s:%d  %s\n", __FILE__, __LINE__, #x), fails++;     \
    } while (0)

static uint32_t fnv(const uint32_t *px, int w, int h, int stride)
{
    uint32_t v = 2166136261u;
    for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++) {
            uint32_t p = px[(size_t)y * stride + x];
            for (int k = 0; k < 4; k++) v = (v ^ ((p >> (k * 8)) & 255)) * 16777619u;
        }
    return v;
}

typedef struct {
    goome *g;
    goome_ui *u;
    int opens, deep, chk, mopen, ok, a, b, cb, ctx, closes, focus_a, focus_in, focus_b;
    char name[32];
    const char *toast;
    int toast_ms;
} kit_t;

static void kit_ev(kit_t *k, int type, double x, double y, int button, int key, int mods, const char *text)
{
    goome_event e;
    memset(&e, 0, sizeof(e));
    e.type = type, e.x = x, e.y = y, e.button = button, e.key = key, e.mods = mods;
    if (text) snprintf(e.text, sizeof(e.text), "%s", text);
    goome_push(k->g, &e);
}
static void kit_click(kit_t *k, double x, double y, int button)
{
    kit_ev(k, GOOME_MOTION, x, y, 0, 0, 0, NULL);
    kit_ev(k, GOOME_DOWN, x, y, button, 0, 0, NULL);
    kit_ev(k, GOOME_UP, x, y, button, 0, 0, NULL);
}
static void kit_key(kit_t *k, int key, int mods)
{
    char t[2] = {0, 0};
    if (key >= 32 && key < 127 && !(mods & (GOOME_CTRL | GOOME_ALT | GOOME_SUPER))) t[0] = (char)key;
    kit_ev(k, GOOME_KEY, 0, 0, 0, key, mods, t);
}
static void kit_type(kit_t *k, const char *s)
{
    for (; *s; s++) kit_key(k, (unsigned char)*s, 0);
}

static uint32_t kit_frame(kit_t *k)
{
    goome_event ev;
    while (goome_poll(k->g, &ev, 0) > 0) {
        if (ev.type == GOOME_CLOSE) k->closes++;
        goome_ui_feed(k->u, &ev);
    }
    goome_ui *u = k->u;
    goome_canvas *c = goome_ui_begin(u, 0);
    if (!c) return 0;
    if (k->toast) goome_ui_toast(u, k->toast, k->toast_ms), k->toast = NULL;
    goome_ui_app(u, "Kit", GOOME_APP_TITLEBAR | GOOME_APP_MENUBAR | GOOME_APP_STATUSBAR);
    if (goome_ui_menubar_begin(u)) {
        if (goome_ui_menu_begin(u, "File")) {
            if (goome_ui_menu_item(u, "Open", "Ctrl+O", NULL)) k->opens++;
            goome_ui_menu_item(u, "Check", NULL, &k->chk);
            goome_ui_menu_separator(u);
            if (goome_ui_menu_begin(u, "More")) {
                if (goome_ui_menu_item(u, "Deep", NULL, NULL)) k->deep++;
                goome_ui_menu_end(u);
            }
            goome_ui_menu_end(u);
        }
        if (goome_ui_menu_begin(u, "Edit")) {
            goome_ui_menu_item(u, "Copy", "Ctrl+C", NULL);
            goome_ui_menu_end(u);
        }
        goome_ui_menubar_end(u);
    }
    if (goome_ui_button(u, "A")) k->a++;
    k->focus_a = goome_ui_focused(u);
    goome_ui_input(u, "Name", k->name, sizeof(k->name));
    k->focus_in = goome_ui_focused(u);
    if (goome_ui_button(u, "B")) k->b++;
    k->focus_b = goome_ui_focused(u);
    if (goome_ui_context_begin(u, "bctx")) {
        if (goome_ui_menu_item(u, "Ctx", NULL, NULL)) k->ctx++;
        goome_ui_context_end(u);
    }
    goome_ui_checkbox(u, "C", &k->cb);
    if (goome_ui_modal_begin(u, "Dialog", &k->mopen)) {
        goome_ui_label(u, "sure?");
        if (goome_ui_button(u, "OK")) k->ok++, goome_ui_popup_close(u);
        goome_ui_modal_end(u);
    }
    goome_ui_statusbar(u, "ready");
    goome_ui_app_end(u);
    goome_ui_end(u);
    return fnv(c->px, c->w, c->h, c->stride);
}

static void kit_frames(kit_t *k, int n)
{
    for (int i = 0; i < n; i++) kit_frame(k);
}

static void kit_dump(kit_t *k, const char *name)
{
    goome_image f;
    if (!gm_dump_dir || goome_frame(k->g, &f) != GOOME_OK) return;
    char path[512];
    snprintf(path, sizeof(path), "%s/%s.ppm", gm_dump_dir, name);
    goome_save_ppm(path, f.px, f.w, f.h, f.stride);
}

static double kit_ink(kit_t *k, int x0, int y0, int x1, int y1)
{
    goome_image f;
    if (goome_frame(k->g, &f) != GOOME_OK) return -1;
    double s = 0;
    for (int y = y0; y < y1 && y < f.h; y++)
        for (int x = x0; x < x1 && x < f.w; x++) s += (f.px[(size_t)y * f.stride + x] >> 8) & 255;
    return s;
}

static void app_kit(void)
{
    int err;
    goome *g = goome_open(&(goome_opts){.backend = GOOME_TEST, .mode = GOOME_MODE_WINDOW, .title = "kit", .width = 420, .height = 300,
                                        .window_flags = GOOME_WINDOW_BORDERLESS | GOOME_WINDOW_RESIZABLE, .test_script = "end 100000"},
                          &err);
    CHECK(g != NULL);
    if (!g) return;
    kit_t k;
    memset(&k, 0, sizeof(k));
    k.g = g, k.u = goome_ui_new(g);
    CHECK(k.u != NULL);
    if (!k.u) {
        goome_close(g);
        return;
    }
    goome_ui_theme(k.u)->font = NULL, goome_ui_theme(k.u)->font_size = 14;
    uint32_t hs[6];
    kit_frames(&k, 3);
    hs[0] = kit_frame(&k);
    kit_dump(&k, "kit0");

    kit_click(&k, 30, 53, 1);
    kit_frames(&k, 11);
    hs[1] = kit_frame(&k);
    kit_dump(&k, "kit1");
    CHECK(goome_hit(g, 100, 89));
    kit_click(&k, 60, 89, 1);
    kit_frames(&k, 2);
    CHECK(k.opens == 1);
    kit_click(&k, 30, 53, 1);
    kit_frames(&k, 3);
    kit_key(&k, GOOME_K_ESC, 0);
    kit_frames(&k, 2);
    kit_click(&k, 60, 89, 1);
    kit_frames(&k, 2);
    CHECK(k.opens == 1);

    kit_click(&k, 30, 53, 1);
    kit_frames(&k, 3);
    kit_key(&k, GOOME_K_DOWN, 0), kit_key(&k, GOOME_K_DOWN, 0), kit_key(&k, GOOME_K_ENTER, 0);
    kit_frames(&k, 2);
    CHECK(k.chk == 1 && k.opens == 1);

    kit_click(&k, 30, 53, 1);
    kit_frames(&k, 3);
    kit_ev(&k, GOOME_MOTION, 60, 152, 0, 0, 0, NULL);
    kit_frames(&k, 12);
    hs[2] = kit_frame(&k);
    kit_dump(&k, "kit2");
    kit_click(&k, 240, 152, 1);
    kit_frames(&k, 2);
    CHECK(k.deep == 1);
    kit_click(&k, 30, 53, 1);
    kit_frames(&k, 3);
    kit_key(&k, GOOME_K_UP, 0), kit_key(&k, GOOME_K_RIGHT, 0);
    kit_frames(&k, 3);
    kit_key(&k, GOOME_K_ESC, 0);
    kit_frames(&k, 2);
    kit_key(&k, GOOME_K_RIGHT, 0);
    kit_frames(&k, 3);
    kit_key(&k, GOOME_K_ENTER, 0);
    kit_frames(&k, 2);
    CHECK(k.deep == 2 && k.opens == 1 && k.chk == 1);
    kit_click(&k, 30, 53, 1);
    kit_frames(&k, 2);
    kit_key(&k, GOOME_K_RIGHT, 0);
    kit_frames(&k, 3);
    kit_key(&k, GOOME_K_LEFT, 0);
    kit_frames(&k, 3);
    kit_key(&k, GOOME_K_ENTER, 0);
    kit_frames(&k, 2);
    CHECK(k.opens == 1);
    kit_key(&k, GOOME_K_DOWN, 0), kit_key(&k, GOOME_K_ENTER, 0);
    kit_frames(&k, 2);
    CHECK(k.opens == 2);

    k.mopen = 1;
    kit_ev(&k, GOOME_MOTION, 300, 250, 0, 0, 0, NULL);
    kit_frames(&k, 13);
    hs[3] = kit_frame(&k);
    kit_dump(&k, "kit3");
    kit_click(&k, 32, 98, 1);
    kit_frames(&k, 2);
    CHECK(k.a == 0 && k.mopen == 1);
    kit_key(&k, GOOME_K_ESC, 0);
    kit_frames(&k, 2);
    CHECK(k.mopen == 0);
    k.mopen = 1;
    kit_frames(&k, 12);
    kit_click(&k, 64, 167, 1);
    kit_frames(&k, 2);
    CHECK(k.ok == 1 && k.mopen == 0);

    kit_key(&k, GOOME_K_TAB, 0);
    kit_frames(&k, 2);
    CHECK(k.focus_a && !k.focus_in);
    kit_key(&k, GOOME_K_TAB, 0);
    kit_frames(&k, 2);
    CHECK(k.focus_in && !k.focus_a);
    kit_key(&k, GOOME_K_TAB, 0);
    kit_frames(&k, 2);
    CHECK(k.focus_b);
    kit_key(&k, GOOME_K_TAB, GOOME_SHIFT);
    kit_frames(&k, 2);
    CHECK(k.focus_in && !k.focus_b);
    kit_key(&k, GOOME_K_TAB, 0);
    kit_frames(&k, 2);
    kit_key(&k, GOOME_K_SPACE, 0);
    kit_frames(&k, 1);
    hs[4] = kit_frame(&k);
    kit_dump(&k, "kit4");
    CHECK(k.b == 1 && k.focus_b);
    kit_key(&k, GOOME_K_TAB, 0), kit_key(&k, GOOME_K_ENTER, 0);
    kit_frames(&k, 2);
    CHECK(k.cb == 0 && k.b == 2);
    kit_key(&k, GOOME_K_SPACE, 0);
    kit_frames(&k, 2);
    CHECK(k.cb == 1);
    kit_key(&k, GOOME_K_TAB, 0);
    kit_frames(&k, 2);
    CHECK(k.focus_a);
    kit_key(&k, GOOME_K_ESC, 0);
    kit_frames(&k, 2);
    CHECK(!k.focus_a);

    kit_click(&k, 210, 159, 1);
    kit_frames(&k, 1);
    CHECK(goome_cursor_shape(g) == GOOME_CURSOR_IBEAM);
    kit_type(&k, "hello world");
    kit_frames(&k, 1);
    kit_key(&k, 'a', GOOME_CTRL), kit_key(&k, 'c', GOOME_CTRL);
    kit_frames(&k, 1);
    char clip[64];
    CHECK(goome_clipboard_get(g, clip, sizeof(clip)) == 11 && !strcmp(clip, "hello world"));
    kit_key(&k, GOOME_K_END, 0);
    for (int i = 0; i < 5; i++) kit_key(&k, GOOME_K_LEFT, GOOME_SHIFT);
    kit_frames(&k, 1);
    hs[5] = kit_frame(&k);
    kit_dump(&k, "kit5");
    kit_key(&k, 'x', GOOME_CTRL);
    kit_frames(&k, 1);
    CHECK(!strcmp(k.name, "hello ") && goome_clipboard_get(g, clip, sizeof(clip)) == 5 && !strcmp(clip, "world"));
    kit_key(&k, 'v', GOOME_CTRL), kit_key(&k, 'v', GOOME_CTRL);
    kit_frames(&k, 1);
    CHECK(!strcmp(k.name, "hello worldworld"));
    kit_key(&k, 'z', GOOME_CTRL);
    kit_frames(&k, 1);
    CHECK(!strcmp(k.name, "hello world"));
    goome_clipboard_set(g, "a\nb\tc");
    kit_key(&k, GOOME_K_HOME, 0), kit_key(&k, 'v', GOOME_CTRL);
    kit_frames(&k, 1);
    CHECK(!strcmp(k.name, "a b chello world"));
    kit_key(&k, GOOME_K_BACKSPACE, GOOME_CTRL), kit_key(&k, GOOME_K_DELETE, 0);
    kit_frames(&k, 1);
    CHECK(!strcmp(k.name, "a b ello world"));
    kit_ev(&k, GOOME_DOWN, 70, 159, 1, 0, 0, NULL), kit_ev(&k, GOOME_UP, 70, 159, 1, 0, 0, NULL);
    kit_ev(&k, GOOME_DOWN, 70, 159, 1, 0, 0, NULL), kit_ev(&k, GOOME_UP, 70, 159, 1, 0, 0, NULL);
    kit_frames(&k, 1);
    kit_type(&k, "hi");
    kit_frames(&k, 1);
    CHECK(!strcmp(k.name, "a b hi world"));
    kit_key(&k, GOOME_K_ENTER, 0);
    kit_frames(&k, 1);

    kit_ev(&k, GOOME_MOTION, 418, 150, 0, 0, 0, NULL);
    kit_frames(&k, 1);
    CHECK(goome_cursor_shape(g) == GOOME_CURSOR_RESIZE_EW);
    kit_ev(&k, GOOME_MOTION, 418, 298, 0, 0, 0, NULL);
    kit_frames(&k, 1);
    CHECK(goome_cursor_shape(g) == GOOME_CURSOR_RESIZE_NWSE);
    kit_ev(&k, GOOME_MOTION, 100, 250, 0, 0, 0, NULL);
    kit_frames(&k, 1);
    CHECK(goome_cursor_shape(g) == GOOME_CURSOR_ARROW);

    kit_click(&k, 32, 196, 3);
    kit_frames(&k, 12);
    kit_dump(&k, "kit6");
    kit_click(&k, 60, 216, 1);
    kit_frames(&k, 2);
    CHECK(k.ctx == 1 && k.b == 2);

    double base = kit_ink(&k, 220, 200, 420, 272), ink[4];
    k.toast = "saved", k.toast_ms = 400;
    int at[4] = {2, 10, 22, 30}, f = 0;
    for (int i = 0; i <= 30; i++) {
        kit_frame(&k);
        if (i == at[f]) ink[f++] = kit_ink(&k, 220, 200, 420, 272) - base;
    }
    printf("toast ink %.0f %.0f %.0f %.0f\n", ink[0], ink[1], ink[2], ink[3]);
    CHECK(ink[0] > 0 && ink[1] > ink[0] && ink[2] < ink[1] && ink[2] > 0 && ink[3] == 0);

    kit_ev(&k, GOOME_DOWN, 150, 18, 1, 0, 0, NULL), kit_ev(&k, GOOME_UP, 150, 18, 1, 0, 0, NULL);
    kit_ev(&k, GOOME_DOWN, 150, 18, 1, 0, 0, NULL), kit_ev(&k, GOOME_UP, 150, 18, 1, 0, 0, NULL);
    kit_frames(&k, 2);
    CHECK(goome_window_get_state(g) == GOOME_STATE_MAXIMIZED);
    kit_click(&k, 397, 18, 1);
    kit_frames(&k, 2);
    CHECK(k.closes == 1);

    static const uint32_t want[6] = {0xaef05fe5u, 0x26bd1b58u, 0xbcba3bddu, 0xbbbab1e5u, 0x863c70c5u, 0x3f3f63e1u};
    printf("kit hashes %08x %08x %08x %08x %08x %08x\n", hs[0], hs[1], hs[2], hs[3], hs[4], hs[5]);
    for (int i = 0; i < 6; i++) CHECK(hs[i] == want[i]);
    goome_ui_free(k.u);
    goome_close(g);
}


typedef struct {
    goome *g;
    goome_ui *u;
    int page, flags, ro_flags, tree_open, sort_col, sort_desc, sort_changes, lc_first, lc_last, links, ival, dropped;
    char ed[256], ro[64], small[12], got[32];
    double fv, nv, frac;
    uint32_t hash;
} w2_t;

static void w2_ev(w2_t *k, int type, double x, double y, int key, int mods, const char *text)
{
    goome_event e;
    memset(&e, 0, sizeof(e));
    e.type = type, e.x = x, e.y = y, e.button = 1, e.key = key, e.mods = mods;
    if (text) snprintf(e.text, sizeof(e.text), "%s", text);
    goome_push(k->g, &e);
}
static void w2_click(w2_t *k, double x, double y)
{
    w2_ev(k, GOOME_MOTION, x, y, 0, 0, NULL);
    w2_ev(k, GOOME_DOWN, x, y, 0, 0, NULL);
    w2_ev(k, GOOME_UP, x, y, 0, 0, NULL);
}
static void w2_key(w2_t *k, int key, int mods)
{
    char t[2] = {0, 0};
    if (key >= 32 && key < 127 && !(mods & (GOOME_CTRL | GOOME_ALT | GOOME_SUPER))) t[0] = (char)key;
    w2_ev(k, GOOME_KEY, 0, 0, key, mods, t);
}
static void w2_scroll(w2_t *k, double x, double y, int dy)
{
    goome_event e;
    memset(&e, 0, sizeof(e));
    e.type = GOOME_SCROLL, e.x = x, e.y = y, e.dy = dy;
    goome_push(k->g, &e);
}
static void w2_type(w2_t *k, const char *s)
{
    for (; *s; s++) w2_key(k, (unsigned char)*s, 0);
}

static void w2_frame(w2_t *k)
{
    static const char *const names[] = {"pear", "apple", "fig", "kiwi", "plum"};
    static const int sizes[] = {3, 9, 1, 7, 5};
    static const float line[] = {2, 5, 3, 8, 6, 9, 4, 7, 5, 10, 6, 3};
    goome_event ev;
    while (goome_poll(k->g, &ev, 0) > 0) goome_ui_feed(k->u, &ev);
    goome_ui *u = k->u;
    goome_canvas *c = goome_ui_begin(u, 0);
    if (!c) return;
    goome_ui_app(u, "W", 0);
    if (k->page == 0) {
        goome_ui_text_edit(u, "ed", k->ed, sizeof(k->ed), 200, k->flags);
        goome_ui_text_edit(u, "ro", k->ro, sizeof(k->ro), 60, GOOME_EDIT_READONLY);
        goome_ui_text_edit(u, "small", k->small, sizeof(k->small), 50, 0);
    } else if (k->page == 1) {
        int order[5] = {0, 1, 2, 3, 4};
        for (int i = 0; i < 5; i++)
            for (int j = i + 1; j < 5; j++) {
                int a = order[i], b = order[j], sw = k->sort_col == 1 ? sizes[a] > sizes[b] : strcmp(names[a], names[b]) > 0;
                if (k->sort_desc) sw = !sw;
                if (sw) order[i] = b, order[j] = a;
            }
        if (goome_ui_table_begin(u, "tab", 2, 0, GOOME_TABLE_SORTABLE | GOOME_TABLE_STRIPES | GOOME_TABLE_BORDERS | GOOME_TABLE_RESIZABLE)) {
            goome_ui_table_column(u, "Name", 0);
            goome_ui_table_column(u, "Size", 120);
            if (goome_ui_table_sort(u, &k->sort_col, &k->sort_desc)) k->sort_changes++;
            for (int i = 0; i < 5; i++) {
                goome_ui_table_row(u);
                goome_ui_label(u, "%s", names[order[i]]);
                goome_ui_table_next(u);
                goome_ui_label(u, "%d", sizes[order[i]]);
            }
        }
        goome_ui_table_end(u);
        if (goome_ui_tree(u, "root", &k->tree_open)) {
            goome_ui_label(u, "child a");
            goome_ui_label(u, "child b");
            goome_ui_tree_end(u);
        }
        goome_ui_scroll_begin(u, "lc", 100);
        goome_ui_list_clip(u, 1000, 0, &k->lc_first, &k->lc_last);
        for (int i = k->lc_first; i < k->lc_last; i++) {
            char t[16];
            snprintf(t, sizeof(t), "row %d", i);
            goome_ui_selectable(u, t, 0);
        }
        goome_ui_scroll_end(u);
    } else if (k->page == 2) {
        goome_ui_row(u, 2);
        goome_ui_drag_float(u, "Float", &k->fv, 0.1, 0, 100, "%.1f");
        goome_ui_drag_int(u, "Int", &k->ival, 0.5, -10, 10);
        goome_ui_number(u, "Num", &k->nv, 2, "%.1f");
        if (goome_ui_link(u, "a link")) k->links++;
        goome_ui_same_line(u);
        goome_ui_badge(u, "new", 0);
        goome_ui_same_line(u);
        goome_ui_badge(u, "light", 0xFFF2C94C);
        goome_ui_row(u, 2);
        goome_ui_plot_lines(u, "Lines", line, 12, 0, 0, 60);
        goome_ui_plot_bars(u, "Bars", line, 12, 0, 12, 60);
    } else {
        goome_ui_split_begin(u, "sp", 0, &k->frac);
        goome_ui_selectable(u, "drag me", 0);
        goome_ui_drag_source(u, "thing", "payload", 8);
        goome_ui_split_next(u);
        goome_ui_button(u, "drop here");
        int n = goome_ui_drop_target(u, "thing", k->got, sizeof(k->got));
        if (n) k->dropped = n;
        goome_ui_button(u, "wrong type");
        goome_ui_drop_target(u, "other", NULL, 0);
        goome_ui_split_end(u);
    }
    goome_ui_app_end(u);
    goome_ui_end(u);
    k->hash = fnv(c->px, c->w, c->h, c->stride);
}

static void w2_frames(w2_t *k, int n)
{
    for (int i = 0; i < n; i++) w2_frame(k);
}

static void w2_dump(w2_t *k, const char *name)
{
    goome_image f;
    if (!gm_dump_dir || goome_frame(k->g, &f) != GOOME_OK) return;
    char path[512];
    snprintf(path, sizeof(path), "%s/%s.ppm", gm_dump_dir, name);
    goome_save_ppm(path, f.px, f.w, f.h, f.stride);
}

static void w2_fuzz(w2_t *k, int steps, unsigned seed)
{
    static const int keys[] = {GOOME_K_LEFT, GOOME_K_RIGHT, GOOME_K_UP, GOOME_K_DOWN, GOOME_K_HOME, GOOME_K_END, GOOME_K_PGUP, GOOME_K_PGDN,
                               GOOME_K_BACKSPACE, GOOME_K_DELETE, GOOME_K_ENTER, GOOME_K_TAB, 'z', 'y', 'x', 'c', 'v', 'a'};
    static const char *const texts[] = {"a", " ", "\xc3\xa9", "\xe2\x82\xac", "\xf0\x9f\x98\x80", "Z", "\n", "wq"};
    k->page = 0;
    for (int i = 0; i < steps; i++) {
        seed = seed * 1103515245u + 12345u;
        unsigned r = seed >> 8;
        int what = (int)(r % 10);
        double x = 14 + (r >> 4) % 460, y = 14 + (r >> 12) % 210;
        if (what < 2) w2_ev(k, GOOME_DOWN, x, y, 0, (r & 64) ? GOOME_SHIFT : 0, NULL), w2_ev(k, GOOME_MOTION, x + 30, y + 10, 0, 0, NULL), w2_ev(k, GOOME_UP, x + 30, y + 10, 0, 0, NULL);
        else if (what < 3) w2_scroll(k, x, y, (r & 1) ? 1 : -2);
        else if (what < 7) {
            int key = keys[(r >> 5) % (sizeof(keys) / sizeof(keys[0]))], mods = 0;
            if (key < 128 && key >= 'a') mods = GOOME_CTRL | ((r & 1024) ? GOOME_SHIFT : 0);
            else mods = (r & 2048 ? GOOME_SHIFT : 0) | (r & 4096 ? GOOME_CTRL : 0);
            w2_ev(k, GOOME_KEY, 0, 0, key, mods, NULL);
        } else {
            const char *t = texts[(r >> 6) % 8];
            if (t[0] == '\n') w2_ev(k, GOOME_KEY, 0, 0, GOOME_K_ENTER, 0, NULL);
            else w2_ev(k, GOOME_KEY, 0, 0, (unsigned char)t[0], 0, t);
        }
        if ((r >> 3) % 3 == 0) k->flags ^= GOOME_EDIT_WRAP;
        if ((r >> 9) % 41 == 0) goome_clipboard_set(k->g, (r & 1) ? "two\nlines\r\nand\ttab" : "\xe2\x82\xac\xe2\x82\xac\xe2\x82\xac\xe2\x82\xac");
        w2_frame(k);
        CHECK(memchr(k->ed, 0, sizeof(k->ed)) != NULL && memchr(k->small, 0, sizeof(k->small)) != NULL);
        int n = (int)strlen(k->ed);
        for (int j = 0; j < n; j++) {
            unsigned char ch = (unsigned char)k->ed[j];
            int need = ch >= 0xF0 ? 3 : ch >= 0xE0 ? 2 : ch >= 0xC0 ? 1 : 0;
            if ((ch & 0xC0) == 0x80) {
                CHECK(!"stray utf8 continuation");
                break;
            }
            for (int q = 1; q <= need; q++)
                if (j + q >= n || (k->ed[j + q] & 0xC0) != 0x80) {
                    CHECK(!"cut utf8 char");
                    q = need;
                }
            j += need;
        }
        if (fails) break;
    }
}

static void widgets2(void)
{
    int err;
    goome *g = goome_open(&(goome_opts){.mode = GOOME_MODE_OFFSCREEN, .title = "w2", .width = 480, .height = 360}, &err);
    CHECK(g != NULL);
    if (!g) return;
    static w2_t k;
    memset(&k, 0, sizeof(k));
    k.g = g, k.u = goome_ui_new(g), k.flags = GOOME_EDIT_LINE_NUMBERS, k.tree_open = 1, k.frac = 0.4;
    CHECK(k.u != NULL);
    if (!k.u) {
        goome_close(g);
        return;
    }
    goome_ui_theme(k.u)->font = NULL, goome_ui_theme(k.u)->font_size = 14;
    goome_ui_theme(k.u)->anim_ms = 0;
    snprintf(k.ro, sizeof(k.ro), "read only text");
    uint32_t hs[8];
    char clip[64];

    w2_frames(&k, 2);
    w2_click(&k, 100, 25);
    w2_frames(&k, 1);
    w2_type(&k, "hello world");
    w2_key(&k, GOOME_K_ENTER, 0);
    w2_type(&k, "  foo");
    w2_key(&k, GOOME_K_ENTER, 0);
    w2_type(&k, "bar");
    w2_frames(&k, 2);
    CHECK(!strcmp(k.ed, "hello world\n  foo\n  bar"));
    w2_key(&k, 'z', GOOME_CTRL);
    w2_frames(&k, 1);
    CHECK(!strcmp(k.ed, "hello world\n  foo\n  "));
    w2_key(&k, 'z', GOOME_CTRL);
    w2_frames(&k, 1);
    CHECK(!strcmp(k.ed, "hello world\n  foo"));
    w2_key(&k, 'y', GOOME_CTRL);
    w2_frames(&k, 1);
    CHECK(!strcmp(k.ed, "hello world\n  foo\n  "));
    w2_key(&k, 'z', GOOME_CTRL | GOOME_SHIFT);
    w2_frames(&k, 1);
    CHECK(!strcmp(k.ed, "hello world\n  foo\n  bar"));
    w2_key(&k, GOOME_K_HOME, GOOME_CTRL), w2_key(&k, GOOME_K_END, GOOME_SHIFT), w2_key(&k, 'c', GOOME_CTRL);
    w2_frames(&k, 1);
    CHECK(goome_clipboard_get(g, clip, sizeof(clip)) == 11 && !strcmp(clip, "hello world"));
    w2_key(&k, GOOME_K_END, GOOME_CTRL), w2_key(&k, GOOME_K_ENTER, 0), w2_key(&k, 'v', GOOME_CTRL);
    w2_frames(&k, 1);
    CHECK(!strcmp(k.ed, "hello world\n  foo\n  bar\n  hello world"));
    w2_key(&k, GOOME_K_UP, GOOME_SHIFT), w2_key(&k, GOOME_K_LEFT, GOOME_SHIFT | GOOME_CTRL);
    w2_frames(&k, 3);
    hs[0] = k.hash;
    w2_dump(&k, "w2_edit");
    w2_key(&k, 'x', GOOME_CTRL);
    w2_frames(&k, 1);
    CHECK(!strcmp(k.ed, "hello world\n  foo\n  ") && goome_clipboard_get(g, clip, sizeof(clip)) > 0 && !strcmp(clip, "bar\n  hello world"));
    w2_key(&k, 'z', GOOME_CTRL);
    w2_frames(&k, 1);
    CHECK(!strcmp(k.ed, "hello world\n  foo\n  bar\n  hello world"));

    w2_ev(&k, GOOME_DOWN, 75, 46, 0, 0, NULL), w2_ev(&k, GOOME_UP, 75, 46, 0, 0, NULL);
    w2_ev(&k, GOOME_DOWN, 75, 46, 0, 0, NULL), w2_ev(&k, GOOME_UP, 75, 46, 0, 0, NULL);
    w2_frames(&k, 1);
    w2_type(&k, "baz");
    w2_frames(&k, 1);
    CHECK(!strcmp(k.ed, "hello world\n  baz\n  bar\n  hello world"));
    for (int i = 0; i < 3; i++) w2_ev(&k, GOOME_DOWN, 90, 25, 0, 0, NULL), w2_ev(&k, GOOME_UP, 90, 25, 0, 0, NULL), w2_frames(&k, 1);
    w2_key(&k, GOOME_K_DELETE, 0);
    w2_frames(&k, 1);
    CHECK(!strcmp(k.ed, "  baz\n  bar\n  hello world"));
    w2_key(&k, 'a', GOOME_CTRL), w2_key(&k, GOOME_K_TAB, 0);
    w2_frames(&k, 1);
    CHECK(!strcmp(k.ed, "      baz\n      bar\n      hello world"));
    w2_key(&k, GOOME_K_TAB, GOOME_SHIFT), w2_key(&k, GOOME_K_TAB, GOOME_SHIFT);
    w2_frames(&k, 1);
    CHECK(!strcmp(k.ed, "baz\nbar\nhello world"));
    w2_key(&k, GOOME_K_HOME, GOOME_CTRL), w2_key(&k, GOOME_K_TAB, 0), w2_key(&k, GOOME_K_END, 0);
    w2_key(&k, GOOME_K_HOME, 0), w2_key(&k, GOOME_K_DELETE, 0), w2_key(&k, GOOME_K_HOME, 0), w2_key(&k, 'q', 0);
    w2_frames(&k, 1);
    CHECK(!strcmp(k.ed, "q    az\nbar\nhello world"));
    k.ed[0] = 0;
    w2_frames(&k, 1);
    for (int i = 0; i < 40; i++) {
        char t[8];
        snprintf(t, sizeof(t), "%d", i);
        w2_type(&k, t);
        if (i < 39) w2_key(&k, GOOME_K_ENTER, 0);
    }
    w2_frames(&k, 2);
    w2_key(&k, GOOME_K_HOME, GOOME_CTRL), w2_key(&k, GOOME_K_PGDN, 0), w2_key(&k, GOOME_K_PGDN, 0), w2_key(&k, '#', 0);
    w2_frames(&k, 12);
    hs[1] = k.hash;
    w2_dump(&k, "w2_page");
    CHECK(strstr(k.ed, "\n#14\n") != NULL);
    w2_key(&k, GOOME_K_PGUP, 0), w2_key(&k, '@', 0);
    w2_frames(&k, 1);
    CHECK(strstr(k.ed, "\n7@\n") != NULL);
    snprintf(k.ed, sizeof(k.ed), "a long line of words that has to wrap around inside the box because it is wider than it\nshort");
    k.flags = GOOME_EDIT_WRAP;
    w2_frames(&k, 2);
    w2_key(&k, GOOME_K_HOME, GOOME_CTRL), w2_key(&k, GOOME_K_DOWN, 0), w2_key(&k, '!', 0);
    w2_frames(&k, 12);
    hs[2] = k.hash;
    w2_dump(&k, "w2_wrap");
    char *bang = strchr(k.ed, '!');
    CHECK(bang && bang < strchr(k.ed, '\n'));
    w2_click(&k, 100, 245);
    w2_frames(&k, 1);
    w2_type(&k, "zzz");
    w2_key(&k, 'a', GOOME_CTRL), w2_key(&k, 'c', GOOME_CTRL), w2_key(&k, GOOME_K_BACKSPACE, 0);
    w2_frames(&k, 1);
    CHECK(!strcmp(k.ro, "read only text") && goome_clipboard_get(g, clip, sizeof(clip)) == 14);
    w2_click(&k, 100, 300);
    w2_frames(&k, 1);
    w2_type(&k, "0123456789abcdef");
    w2_frames(&k, 1);
    CHECK(!strcmp(k.small, "0123456789a"));
    goome_clipboard_set(g, "\xe2\x82\xac\xe2\x82\xac");
    w2_key(&k, GOOME_K_HOME, 0);
    for (int i = 0; i < 4; i++) w2_key(&k, GOOME_K_DELETE, 0);
    w2_key(&k, 'v', GOOME_CTRL);
    w2_frames(&k, 1);
    CHECK(!strcmp(k.small, "\xe2\x82\xac" "456789a"));
    w2_key(&k, GOOME_K_ESC, 0);
    w2_frames(&k, 1);

    k.page = 1;
    w2_frames(&k, 3);
    CHECK(k.lc_first == 0 && k.lc_last > 1 && k.lc_last < 10);
    hs[3] = k.hash;
    w2_dump(&k, "w2_table");
    w2_click(&k, 400, 28);
    w2_frames(&k, 2);
    CHECK(k.sort_col == 1 && k.sort_desc == 0 && k.sort_changes == 1);
    w2_click(&k, 400, 28);
    w2_frames(&k, 2);
    CHECK(k.sort_col == 1 && k.sort_desc == 1 && k.sort_changes == 2);
    w2_frames(&k, 10);
    hs[4] = k.hash;
    w2_dump(&k, "w2_sorted");
    w2_scroll(&k, 100, 320, 2);
    w2_frames(&k, 30);
    CHECK(k.lc_first > 0 && k.lc_last > k.lc_first && k.lc_last - k.lc_first < 10);
    w2_click(&k, 60, 203);
    w2_frames(&k, 2);
    CHECK(k.tree_open == 0);

    k.page = 2, k.fv = 5, k.nv = 1;
    w2_frames(&k, 3);
    w2_ev(&k, GOOME_MOTION, 100, 45, 0, 0, NULL), w2_ev(&k, GOOME_DOWN, 100, 45, 0, 0, NULL);
    w2_frames(&k, 1);
    w2_ev(&k, GOOME_MOTION, 150, 45, 0, 0, NULL);
    w2_frames(&k, 1);
    w2_ev(&k, GOOME_UP, 150, 45, 0, 0, NULL);
    w2_frames(&k, 1);
    CHECK(k.fv > 9.9 && k.fv < 10.1);
    w2_ev(&k, GOOME_MOTION, 300, 45, 0, 0, NULL), w2_ev(&k, GOOME_DOWN, 300, 45, 0, 0, NULL);
    w2_frames(&k, 1);
    w2_ev(&k, GOOME_MOTION, 280, 45, 0, 0, NULL);
    w2_frames(&k, 1);
    w2_ev(&k, GOOME_UP, 280, 45, 0, 0, NULL);
    w2_frames(&k, 1);
    CHECK(k.ival == -10);
    w2_click(&k, 460, 106);
    w2_frames(&k, 1);
    w2_click(&k, 460, 106);
    w2_frames(&k, 1);
    CHECK(k.nv == 5);
    w2_click(&k, 28, 106);
    w2_frames(&k, 1);
    CHECK(k.nv == 3);
    w2_click(&k, 200, 106);
    w2_frames(&k, 1);
    w2_key(&k, 'a', GOOME_CTRL), w2_type(&k, "42.5"), w2_key(&k, GOOME_K_ENTER, 0);
    w2_frames(&k, 2);
    CHECK(k.nv == 42.5);
    w2_click(&k, 200, 106);
    w2_frames(&k, 1);
    w2_key(&k, GOOME_K_UP, 0);
    w2_frames(&k, 1);
    w2_key(&k, GOOME_K_ESC, 0);
    w2_frames(&k, 1);
    CHECK(k.nv == 44.5);
    w2_ev(&k, GOOME_DOWN, 100, 45, 0, 0, NULL), w2_ev(&k, GOOME_UP, 100, 45, 0, 0, NULL);
    w2_ev(&k, GOOME_DOWN, 100, 45, 0, 0, NULL), w2_ev(&k, GOOME_UP, 100, 45, 0, 0, NULL);
    w2_frames(&k, 1);
    w2_type(&k, "7.5"), w2_key(&k, GOOME_K_ENTER, 0);
    w2_frames(&k, 2);
    CHECK(k.fv == 7.5);
    w2_click(&k, 40, 140);
    w2_frames(&k, 1);
    CHECK(k.links == 1);
    w2_ev(&k, GOOME_MOTION, 300, 200, 0, 0, NULL);
    w2_frames(&k, 4);
    hs[5] = k.hash;
    w2_dump(&k, "w2_misc");

    k.page = 3;
    w2_frames(&k, 3);
    hs[6] = k.hash;
    w2_dump(&k, "w2_split");
    w2_ev(&k, GOOME_MOTION, 192, 200, 0, 0, NULL), w2_ev(&k, GOOME_DOWN, 192, 200, 0, 0, NULL);
    w2_frames(&k, 1);
    w2_ev(&k, GOOME_MOTION, 240, 200, 0, 0, NULL);
    w2_frames(&k, 1);
    w2_ev(&k, GOOME_UP, 240, 200, 0, 0, NULL);
    w2_frames(&k, 1);
    CHECK(k.frac > 0.49 && k.frac < 0.51);
    w2_ev(&k, GOOME_MOTION, 60, 24, 0, 0, NULL), w2_ev(&k, GOOME_DOWN, 60, 24, 0, 0, NULL);
    w2_frames(&k, 1);
    w2_ev(&k, GOOME_MOTION, 80, 30, 0, 0, NULL);
    w2_frames(&k, 1);
    w2_ev(&k, GOOME_MOTION, 300, 70, 0, 0, NULL);
    w2_frames(&k, 1);
    w2_ev(&k, GOOME_MOTION, 300, 26, 0, 0, NULL);
    w2_frames(&k, 3);
    hs[7] = k.hash;
    w2_dump(&k, "w2_drag");
    CHECK(goome_cursor_shape(g) == GOOME_CURSOR_MOVE);
    w2_ev(&k, GOOME_UP, 300, 26, 0, 0, NULL);
    w2_frames(&k, 2);
    CHECK(k.dropped == 8 && !strcmp(k.got, "payload"));

    printf("widget hashes %08x %08x %08x %08x %08x %08x %08x %08x\n", hs[0], hs[1], hs[2], hs[3], hs[4], hs[5], hs[6], hs[7]);

    k.page = 0, k.flags = GOOME_EDIT_LINE_NUMBERS, k.ed[0] = 0, k.small[0] = 0;
    w2_frames(&k, 1);
    w2_click(&k, 100, 25);
    w2_fuzz(&k, getenv("GOOME_FUZZ") ? 200000 : 3000, 12345);

    static uint32_t px[120 * 90], bp[12 * 12];
    for (int y = 0; y < 12; y++)
        for (int x = 0; x < 12; x++) bp[y * 12 + x] = x < 4 || y < 4 || x >= 8 || y >= 8 ? 0xFF2050C0u : 0x80404040u;
    goome_bitmap b = {bp, 12, 12, 12, 0};
    uint32_t nh[2];
    for (int s = 0; s < 2; s++) {
        goome_canvas c;
        memset(&c, 0, sizeof(c));
        memset(px, 0, sizeof(px));
        c.px = px, c.w = 120, c.h = 90, c.stride = 120, c.scale = s ? 1.5 : 1;
        goome_draw_bitmap_nine(&c, &b, 3.3, 2.7, 70, 50, 4, 4, 4, 4);
        goome_draw_bitmap_nine(&c, &b, 2, 56, 5, 5, 4, 4, 4, 4);
        nh[s] = fnv(px, 120, 90, 120);
        if (!s) CHECK(px[2 * 120 + 3] == 0 && px[3 * 120 + 3] == 0xFF2050C0u && px[30 * 120 + 40] == 0x80404040u && px[30 * 120 + 72] == 0xFF2050C0u);
    }
    printf("nine hashes %08x %08x\n", nh[0], nh[1]);
    CHECK(nh[0] == 0x638fa7a8u && nh[1] == 0xdd482365u);
    goome_ui_free(k.u);
    goome_close(g);
}

static void skins(void)
{
    static uint32_t bp[12 * 12];
    for (int y = 0; y < 12; y++)
        for (int x = 0; x < 12; x++) bp[y * 12 + x] = x < 4 || y < 4 || x >= 8 || y >= 8 ? 0xFF2050C0u : 0x80404040u;
    goome_bitmap b = {bp, 12, 12, 12, 0};
    int err;
    goome *g = goome_open(&(goome_opts){.mode = GOOME_MODE_OFFSCREEN, .title = "skin", .width = 260, .height = 150}, &err);
    if (!g) return;
    goome_ui *u = goome_ui_new(g);
    goome_theme *t = goome_ui_theme(u);
    t->font = NULL, t->font_size = 14, t->anim_ms = 0;
    t->skin_button = (goome_skin){&b, 4, 4, 4, 4};
    t->skin_panel = (goome_skin){&b, 4, 4, 4, 4};
    t->skin_input = (goome_skin){&b, 3, 3, 3, 3};
    double x = 10, y = 10, w = 240, h = 0;
    char name[16] = "skinned";
    uint32_t hash = 0;
    for (int f = 0; f < 3; f++) {
        goome_canvas *c = goome_ui_begin(u, 0);
        if (!c) continue;
        if (goome_ui_window(u, "Skin", &x, &y, &w, &h, GOOME_WIN_MOVE, NULL)) {
            goome_ui_button(u, "Button");
            goome_ui_input(u, "Name", name, sizeof(name));
        }
        goome_ui_window_end(u);
        goome_ui_end(u);
        hash = fnv(c->px, c->w, c->h, c->stride);
    }
    printf("skin hash %08x\n", hash);
    goome_ui_free(u);
    goome_close(g);
}

static uint32_t style_frame(goome_ui *u, int styled, int leave)
{
    static uint32_t bp[12 * 12];
    for (int i = 0; i < 12 * 12; i++) bp[i] = i % 12 < 3 || i % 12 >= 9 ? 0xFFC05020u : 0xFF303030u;
    goome_bitmap b = {bp, 12, 12, 12, 0};
    goome_skin sk = {&b, 3, 3, 3, 3};
    double x = 8, y = 8, w = 280, h = 0, v = 0.4;
    int on = 1;
    goome_canvas *c = goome_ui_begin(u, 0);
    if (!c) return 0;
    if (goome_ui_window(u, "Style", &x, &y, &w, &h, 0, NULL)) {
        if (styled) goome_ui_push_num(u, GOOME_S_RADIUS, 0);
        goome_ui_button(u, "Square");
        if (styled) goome_ui_pop(u, 1), goome_ui_push_radii(u, (goome_radii){12, 2, 12, 2});
        goome_ui_same_line(u);
        goome_ui_button(u, "Leaf");
        if (styled) goome_ui_pop(u, 1), goome_ui_push_color(u, GOOME_S_ACCENT, 0xFFFF6FA8), goome_ui_push_num(u, GOOME_S_PAD, 14);
        goome_ui_toggle(u, "Pink", &on);
        goome_ui_slider(u, "Amount", &v, 0, 1, "%.2f");
        if (styled) goome_ui_pop(u, 2), goome_ui_push_skin(u, GOOME_SKIN_BUTTON, &sk), goome_ui_push_num(u, GOOME_S_FONT_SIZE, 19);
        goome_ui_button(u, "Skinned");
        if (styled) {
            goome_ui_pop(u, 2);
            goome_theme t = *goome_ui_theme(u);
            goome_ui_theme_light(&t);
            t.radius = 4;
            goome_ui_push_theme(u, &t);
        }
        goome_ui_button(u, "Light");
        if (styled && !leave) goome_ui_pop(u, 1);
        goome_ui_button(u, "After");
    }
    goome_ui_window_end(u);
    goome_ui_end(u);
    return fnv(c->px, c->w, c->h, c->stride);
}

static void style(void)
{
    int err;
    goome *g = goome_open(&(goome_opts){.mode = GOOME_MODE_OFFSCREEN, .title = "style", .width = 300, .height = 240}, &err);
    if (!g) return;
    goome_ui *u = goome_ui_new(g);
    goome_theme *t = goome_ui_theme(u);
    t->font = NULL, t->font_size = 14, t->anim_ms = 0;
    uint32_t h[4];
    style_frame(u, 0, 0);
    h[0] = style_frame(u, 0, 0);
    style_frame(u, 1, 0);
    h[1] = style_frame(u, 1, 0);
    h[2] = style_frame(u, 1, 1);
    style_frame(u, 0, 0);
    h[3] = style_frame(u, 0, 0);
    CHECK(h[0] != h[1] && h[1] != h[2] && h[0] == h[3]);
    printf("style hashes %08x %08x %08x %08x\n", h[0], h[1], h[2], h[3]);
    goome_ui_free(u);
    goome_close(g);
}

static void icon_bitmaps(goome_bitmap *ib, goome_bitmap *hb, goome_bitmap *sb)
{
    static uint32_t a[8 * 8], b[6 * 6], c[12 * 12];
    for (int y = 0; y < 8; y++)
        for (int x = 0; x < 8; x++) a[y * 8 + x] = (x + y) & 1 ? 0xFFFFFFFFu : 0;
    for (int y = 0; y < 6; y++)
        for (int x = 0; x < 6; x++) b[y * 6 + x] = x >= y ? 0xFFE0567Au : 0;
    for (int y = 0; y < 12; y++)
        for (int x = 0; x < 12; x++) c[y * 12 + x] = x < 4 || y < 4 || x >= 8 || y >= 8 ? 0xFF40A060u : 0xFF202830u;
    goome_bitmap_from_argb(a, 8, 8, ib);
    goome_bitmap_from_argb(b, 6, 6, hb);
    goome_bitmap_from_argb(c, 12, 12, sb);
}

static uint32_t icon_frame(goome_ui *u, int push, const goome_bitmap *hb, const goome_skin *sk)
{
    static const char *const tabs[] = {"One", "Two"};
    static const char *const items[] = {"Red", "Green"};
    double x = 8, y = 8, w = 300, h = 250, v = 0.6;
    int a = 1, b = 1, r = 1, sel = 0, tab = 1, open = 1, wopen = 1;
    goome_canvas *c = goome_ui_begin(u, 0);
    if (!c) return 0;
    if (goome_ui_window(u, "Icons", &x, &y, &w, &h, GOOME_WIN_RESIZE | GOOME_WIN_COLLAPSE | GOOME_WIN_CLOSE, &wopen)) {
        goome_ui_tabs(u, "t", tabs, 2, &tab);
        if (push) goome_ui_push_icon(u, GOOME_ICON_CHECK, hb), goome_ui_push_skin(u, GOOME_SKIN_TOGGLE, sk);
        goome_ui_checkbox(u, "Check", &a);
        goome_ui_toggle(u, "Toggle", &b);
        if (push) goome_ui_pop(u, 2);
        goome_ui_checkbox(u, "After", &a);
        goome_ui_radio(u, "Radio", &r, 1);
        goome_ui_slider(u, "Slide", &v, 0, 1, "%.1f");
        goome_ui_combo(u, "Pick", &sel, items, 2);
        goome_ui_collapsing(u, "Fold", &open);
    }
    goome_ui_window_end(u);
    goome_ui_end(u);
    return fnv(c->px, c->w, c->h, c->stride);
}

static void icons(void)
{
    int err;
    goome *g = goome_open(&(goome_opts){.mode = GOOME_MODE_OFFSCREEN, .title = "icons", .width = 320, .height = 270}, &err);
    if (!g) return;
    goome_ui *u = goome_ui_new(g);
    goome_theme *t = goome_ui_theme(u);
    t->font = NULL, t->font_size = 14, t->anim_ms = 0;
    goome_bitmap ib, hb, sb;
    icon_bitmaps(&ib, &hb, &sb);
    goome_skin sk = {&sb, 4, 4, 4, 4};
    uint32_t h[6];
    icon_frame(u, 0, &hb, &sk);
    h[0] = icon_frame(u, 0, &hb, &sk);
    h[1] = icon_frame(u, 1, &hb, &sk);
    h[2] = icon_frame(u, 0, &hb, &sk);
    for (int i = 0; i < GOOME_ICON_COUNT; i++) t->icon[i] = &ib;
    t->skin_checkbox = t->skin_radio = t->skin_toggle = t->skin_slider = t->skin_slider_fill = t->skin_tab = t->skin_titlebar = t->skin_scrollbar = sk;
    h[3] = icon_frame(u, 0, &hb, &sk);
    t->icon_tint = 1;
    h[4] = icon_frame(u, 0, &hb, &sk);
    for (int i = 0; i < GOOME_ICON_COUNT; i++) t->icon[i] = NULL;
    t->icon_tint = 0;
    goome_skin none = {0};
    t->skin_checkbox = t->skin_radio = t->skin_toggle = t->skin_slider = t->skin_slider_fill = t->skin_tab = t->skin_titlebar = t->skin_scrollbar = none;
    h[5] = icon_frame(u, 0, &hb, &sk);
    CHECK(h[0] != h[1] && h[0] == h[2] && h[3] != h[0] && h[4] != h[3] && h[5] == h[0]);
    printf("icon hashes %08x %08x %08x %08x %08x %08x\n", h[0], h[1], h[2], h[3], h[4], h[5]);
    goome_ui_free(u);
    goome_bitmap_free(&ib);
    goome_bitmap_free(&hb);
    goome_bitmap_free(&sb);
    goome_close(g);
}

static uint32_t look_frame(goome_ui *u, const goome_theme *th)
{
    static const char *const tabs[] = {"Play", "Stats"};
    static const char *const modes[] = {"Story", "Arena"};
    double x = 8, y = 8, w = 300, h = 0, v = 0.4;
    int on = 1, lvl = 1, tab = 0, mode = 1;
    char name[32] = "sev";
    goome_canvas *c = goome_ui_begin(u, 0);
    if (!c) return 0;
    if (th) goome_ui_push_theme(u, th);
    if (goome_ui_window(u, "Look", &x, &y, &w, &h, 0, NULL)) {
        goome_ui_tabs(u, "t", tabs, 2, &tab);
        goome_ui_heading(u, "Player");
        goome_ui_input(u, "Name", name, sizeof(name));
        goome_ui_slider(u, "Volume", &v, 0, 1, "%.2f");
        goome_ui_row(u, 2);
        goome_ui_checkbox(u, "Sound", &on);
        goome_ui_toggle(u, "Online", &on);
        goome_ui_radio(u, "Easy", &lvl, 0);
        goome_ui_same_line(u);
        goome_ui_radio(u, "Hard", &lvl, 1);
        goome_ui_combo(u, "Mode", &mode, modes, 2);
        goome_ui_progress(u, 0.6, "loading");
        goome_ui_row(u, 3);
        goome_ui_badge(u, "new", 0);
        goome_ui_link(u, "goome");
        goome_ui_button(u, "Start");
    }
    goome_ui_window_end(u);
    if (th) goome_ui_pop(u, 1);
    goome_ui_end(u);
    return fnv(c->px, c->w, c->h, c->stride);
}

static uint32_t theme_bytes(const goome_theme *t)
{
    const unsigned char *p = (const unsigned char *)t;
    uint32_t v = 2166136261u;
    for (size_t i = 0; i < sizeof(*t); i++) v = (v ^ p[i]) * 16777619u;
    return v;
}

static void looks(void)
{
    static const struct {
        uint32_t accent;
        int dark;
        double r;
    } makes[] = {{0xFF8B5CF6, 1, 12}, {0xFF2F7BF6, 0, 20}, {0xFFFFD60A, 1, 0}, {0x00E5484D, 0, 6}};
    int err;
    goome *g = goome_open(&(goome_opts){.mode = GOOME_MODE_OFFSCREEN, .title = "looks", .width = 320, .height = 400}, &err);
    if (!g) return;
    goome_ui *u = goome_ui_new(g);
    goome_theme *t = goome_ui_theme(u);
    t->font = NULL, t->font_size = 14, t->anim_ms = 0;
    goome_ui_cfg(u)->blink_ms = 0;
    uint32_t h[12], b[11];
    look_frame(u, NULL);
    h[0] = look_frame(u, NULL);
    for (int i = 0; i < 11; i++) {
        goome_theme p;
        memset(&p, 0, sizeof(p));
        if (i < GOOME_PRESET_COUNT) CHECK(goome_ui_theme_preset(&p, i) == GOOME_OK);
        else goome_ui_theme_make(&p, makes[i - GOOME_PRESET_COUNT].accent, makes[i - GOOME_PRESET_COUNT].dark, makes[i - GOOME_PRESET_COUNT].r);
        b[i] = theme_bytes(&p);
        p.font_size = 14, p.anim_ms = 0;
        look_frame(u, &p);
        h[1 + i] = look_frame(u, &p);
        CHECK(h[1 + i] != h[0] || i == GOOME_PRESET_DARK);
    }
    printf("look hashes");
    for (int i = 0; i < 12; i++) printf(" %08x", h[i]);
    printf("\ntheme bytes");
    for (int i = 0; i < 11; i++) printf(" %08x", b[i]);
    printf("\n");
    goome_ui_scale(u, 1.25);
    look_frame(u, NULL);
    uint32_t big = look_frame(u, NULL);
    printf("scaled theme %.2f %.2f %.2f %.2f\n", t->font_size, t->pad, t->check_size, t->scroll_w);
    goome_ui_scale(u, 0.8);
    look_frame(u, NULL);
    uint32_t back = look_frame(u, NULL);
    CHECK(big != h[0] && back == h[0]);
    printf("scale hashes %08x %08x\n", big, back);
    goome_ui_free(u);
    goome_close(g);
}

static void ui_key(goome_ui *u, int key, int mods)
{
    goome_event e;
    memset(&e, 0, sizeof(e));
    e.type = GOOME_KEY, e.key = key, e.mods = mods;
    if (key >= 32 && key < 127 && !(mods & (GOOME_CTRL | GOOME_ALT | GOOME_SUPER))) e.text[0] = (char)key;
    goome_ui_feed(u, &e);
}

static uint32_t input_frame(goome_ui *u, char *buf, int cap, int flags, const char *hint, int focus, int *ret)
{
    double x = 8, y = 8, w = 280, h = 0;
    goome_canvas *c = goome_ui_begin(u, 0);
    if (!c) return 0;
    if (goome_ui_window(u, "Input", &x, &y, &w, &h, 0, NULL)) {
        goome_ui_button(u, "Hi");
        if (focus) goome_ui_set_focus(u);
        int r = goome_ui_input_ex(u, "in", buf, cap, flags, hint);
        if (ret && r) *ret = r;
    }
    goome_ui_window_end(u);
    goome_ui_end(u);
    return fnv(c->px, c->w, c->h, c->stride);
}

static uint32_t input_still(goome_ui *u, const char *text, int flags, const char *hint)
{
    char buf[24];
    snprintf(buf, sizeof(buf), "%s", text);
    input_frame(u, buf, sizeof(buf), flags, hint, 0, NULL);
    return input_frame(u, buf, sizeof(buf), flags, hint, 0, NULL);
}

static void input_typed(goome_ui *u, char *buf, int cap, int flags, const char *type, int *ret)
{
    input_frame(u, buf, cap, flags, NULL, 1, ret);
    for (const char *p = type; *p; p++) {
        if (*p == '\n') ui_key(u, GOOME_K_ENTER, 0);
        else ui_key(u, (unsigned char)*p, 0);
        input_frame(u, buf, cap, flags, NULL, 0, ret);
    }
}

static void inputs(void)
{
    int err;
    goome *g = goome_open(&(goome_opts){.mode = GOOME_MODE_OFFSCREEN, .title = "inputs", .width = 300, .height = 160}, &err);
    if (!g) return;
    goome_ui *u = goome_ui_new(g);
    goome_theme *t = goome_ui_theme(u);
    t->font = NULL, t->font_size = 14, t->anim_ms = 0;
    goome_ui_cfg(u)->blink_ms = 0;
    uint32_t h[6];
    h[0] = input_still(u, "abc", 0, NULL);
    h[1] = input_still(u, "abc", GOOME_INPUT_PASSWORD, NULL);
    h[2] = input_still(u, "xyz", GOOME_INPUT_PASSWORD, NULL);
    h[3] = input_still(u, "", 0, NULL);
    h[4] = input_still(u, "", 0, "type here");
    goome_ui_cfg(u)->mask = "#";
    h[5] = input_still(u, "abc", GOOME_INPUT_PASSWORD, NULL);
    goome_ui_cfg(u)->mask = NULL;
    CHECK(h[0] != h[1] && h[1] == h[2] && h[3] != h[4] && h[5] != h[1]);
    printf("input hashes %08x %08x %08x %08x %08x %08x\n", h[0], h[1], h[2], h[3], h[4], h[5]);
    goome_ui_free(u);
    u = goome_ui_new(g);
    t = goome_ui_theme(u);
    t->font = NULL, t->font_size = 14, t->anim_ms = 0;
    char num[16] = "", sel[16] = "ab", keep[16] = "ab";
    int ret = 0, plain = 0;
    input_typed(u, num, sizeof(num), GOOME_INPUT_NUMBER, "a1-.x2", NULL);
    input_typed(u, sel, sizeof(sel), 0, "z", NULL);
    input_typed(u, keep, sizeof(keep), GOOME_INPUT_NO_SELECT_ON_FOCUS, "z", NULL);
    char e1[16] = "ab", e2[16] = "ab";
    input_typed(u, e1, sizeof(e1), GOOME_INPUT_ENTER_RETURNS, "\n", &ret);
    input_typed(u, e2, sizeof(e2), 0, "\n", &plain);
    printf("input texts %s|%s|%s|%d|%d\n", num, sel, keep, ret, plain);
    goome_ui_free(u);
    goome_close(g);
}

int main(void)
{
    app_kit();
    widgets2();
    skins();
    style();
    icons();
    looks();
    inputs();
    return fails != 0;
}
