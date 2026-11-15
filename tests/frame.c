#include <stdio.h>
#include <string.h>

#include "goome.h"

static void push(goome *g, int type, double x, double y)
{
    goome_event e;
    memset(&e, 0, sizeof(e));
    e.type = type, e.x = x, e.y = y, e.button = 1;
    goome_push(g, &e);
}

static void type_text(goome *g, const char *s)
{
    for (; *s; s++) {
        goome_event e;
        memset(&e, 0, sizeof(e));
        e.type = GOOME_KEY, e.key = (unsigned char)*s, e.text[0] = *s;
        goome_push(g, &e);
    }
}

static unsigned fnv(const uint32_t *px, int w, int h, int stride)
{
    unsigned v = 2166136261u;
    for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
            for (int k = 0; k < 4; k++) v = (v ^ ((px[(size_t)y * stride + x] >> (k * 8)) & 255)) * 16777619u;
    return v;
}

int main(void)
{
    int err;
    goome *g = goome_open(&(goome_opts){.mode = GOOME_MODE_OFFSCREEN, .title = "engine", .width = 360, .height = 330, .scale = 2}, &err);
    if (!g) return 1;
    goome_ui *u = goome_ui_new(g);
    goome_theme *t = goome_ui_theme(u);
    t->font = NULL, t->font_size = 14, t->anim_ms = 0;
    double wx = 20, wy = 20, ww = 320, wh = 0, volume = 0.25;
    int check = 0;
    char name[64] = "goome";
    for (int frame = 0; frame < 10; frame++) {
        if (frame == 2) push(g, GOOME_DOWN, 40, 109), push(g, GOOME_UP, 40, 109);
        if (frame == 3) push(g, GOOME_DOWN, 60, 163);
        if (frame == 4) push(g, GOOME_MOTION, 250, 163);
        if (frame == 5) push(g, GOOME_UP, 250, 163);
        if (frame == 6) push(g, GOOME_DOWN, 150, 223), push(g, GOOME_UP, 150, 223);
        if (frame == 7) type_text(g, " from an engine");
        if (frame == 8) push(g, GOOME_MOTION, 80, 279);
        goome_event ev;
        while (goome_poll(g, &ev, 0) > 0) goome_ui_feed(u, &ev);
        if (!goome_ui_begin(u, 0)) continue;
        if (goome_ui_window(u, "offscreen", &wx, &wy, &ww, &wh, GOOME_WIN_MOVE, NULL)) {
            goome_ui_label(u, "%s", "no window, the engine owns the screen");
            goome_ui_checkbox(u, "Pushed a click", &check);
            goome_ui_slider(u, "Volume", &volume, 0, 1, "%.2f");
            goome_ui_input(u, "Name", name, sizeof(name));
            goome_ui_progress(u, volume, NULL);
            goome_ui_button(u, "Hover me");
        }
        goome_ui_window_end(u);
        goome_ui_end(u);
    }
    goome_image f;
    if (goome_frame(g, &f) != GOOME_OK) return 1;
    printf("%08x %dx%d check %d volume %.2f name %s hit %d%d%d cursor %d\n", fnv(f.px, f.w, f.h, f.stride), f.w, f.h, check, volume, name,
           goome_hit(g, 40, 109), goome_hit(g, 30, 30), goome_hit(g, 5, 320), goome_cursor_shape(g));
    goome_ui_free(u);
    goome_close(g);
    return 0;
}
