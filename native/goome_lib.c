#define GOOME_IMPLEMENTATION
#include "goome.h"

#ifndef GOOME_NO_UI
void goomesharp_ui_label(goome_ui *u, const char *text)
{
    goome_ui_label(u, "%s", text);
}
#endif
