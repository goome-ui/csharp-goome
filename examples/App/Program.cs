using System;
using System.Collections.Generic;
using System.Linq;
using GoomeSharp;

string[] colors = { "None", "Blue", "Green", "Yellow", "Red" };
string[] sorts = { "Recently edited", "Title", "Created" };
var notes = new List<Note>();
int sel = 0, sort = 0, made = 0;
bool settings = false, about = false, confirm = false, quitting = false, wrap = true, light = false, autosave = true;
double fontSize = 15, side = 0.27;
string search = "";
long typed = 0;

void AddNote(string title, string tags, string body, bool pinned = false, int color = 0)
{
    notes.Add(new Note(title, tags, body, pinned, color) { Made = ++made, Edited = made });
    sel = notes.Count - 1;
}

void Touch(Note n)
{
    n.Edited = ++made;
    n.Changed = true;
    typed = Goome.NowMs;
}

AddNote("Groceries", "home", "milk\neggs\ncoffee\nrice\ncat food", true, 2);
AddNote("Release checklist", "work", "bump the version\nrun the tests\nwrite the changelog\ntag it", color: 1);
AddNote("Books", "reading", "finish the one on the nightstand first", color: 3);
AddNote("Workout", "health", "mon legs, wed push, fri pull");
AddNote("Trip", "travel", "train at 8:10, book the hotel", color: 4);
sel = 0;

GoomeApp app;
try
{
    app = new GoomeApp("Notes", 960, 640) { Flags = AppFlags.MenuBar | AppFlags.StatusBar | AppFlags.NoPad };
}
catch (GoomeException e)
{
    Console.Error.WriteLine(e.Message);
    return 1;
}

using (app)
{
    app.Ui.Theme.FontSize = fontSize;
    app.RedrawEveryMs = 1000;
    app.OnClose = () =>
    {
        if (!notes.Any(n => n.Changed)) return true;
        quitting = true;
        app.Invalidate();
        return false;
    };
    app.OnEvent += ev =>
    {
        if (ev.Type == EventType.Key && ev.Mods == Mods.Ctrl)
        {
            if (ev.Key == 's' && sel >= 0 && sel < notes.Count) Save(notes[sel]);
            if (ev.Key == 'n') AddNote("Untitled", "", "");
            if (ev.Key == 'q') app.Quit();
        }
        if (ev.Type == EventType.Drop) app.Ui.Toast("dropped " + (app.Goome.DropPath ?? ev.Message), 2500);
    };

    void Save(Note n, bool quiet = false)
    {
        n.Changed = false;
        if (!quiet) app.Ui.Toast($"Saved \"{n.Title}\"", 2500);
    }

    int Order(Note x, Note y)
    {
        if (x.Pinned != y.Pinned) return x.Pinned ? -1 : 1;
        if (sort == 1)
        {
            int c = string.CompareOrdinal(x.Title, y.Title);
            if (c != 0) return c;
        }
        if (sort == 0) return y.Edited - x.Edited;
        return x.Made - y.Made;
    }

    int Words(string s) => s.Split(new[] { ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;

    app.Run(ui =>
    {
        var n = sel >= 0 && sel < notes.Count ? notes[sel] : null;
        if (ui.MenuBarBegin())
        {
            if (ui.MenuBegin("File"))
            {
                if (ui.MenuItem("New note", "Ctrl+N")) AddNote("Untitled", "", "");
                if (ui.MenuItem("Save", "Ctrl+S") && n != null) Save(n);
                ui.MenuSeparator();
                if (ui.MenuItem("Settings", "Ctrl+,")) settings = true;
                ui.MenuSeparator();
                if (ui.MenuItem("Quit", "Ctrl+Q")) app.Quit();
                ui.MenuEnd();
            }
            if (ui.MenuBegin("Edit"))
            {
                if (ui.MenuItem("Copy note") && n != null && app.Goome.SetClipboard(n.Body)) ui.Toast("Copied", 1500);
                if (ui.MenuItem("Paste at the end") && n != null)
                {
                    var text = app.Goome.GetClipboard();
                    if (!string.IsNullOrEmpty(text))
                    {
                        n.Body += text;
                        Touch(n);
                    }
                }
                ui.MenuSeparator();
                if (ui.MenuItem("Delete note", "Del") && n != null) confirm = true;
                ui.MenuEnd();
            }
            if (ui.MenuBegin("View"))
            {
                ui.MenuItem("Wrap text", null, ref wrap);
                if (ui.MenuItem("Light theme", null, ref light))
                {
                    if (light) ui.Theme.Light();
                    else ui.Theme.Dark();
                    ui.Theme.FontSize = fontSize;
                }
                ui.MenuSeparator();
                if (ui.MenuBegin("Sort by"))
                {
                    for (int i = 0; i < sorts.Length; i++)
                    {
                        bool on = sort == i;
                        if (ui.MenuItem(sorts[i], null, ref on)) sort = i;
                    }
                    ui.MenuEnd();
                }
                ui.MenuEnd();
            }
            if (ui.MenuBegin("Help"))
            {
                if (ui.MenuItem("About Notes")) about = true;
                ui.MenuEnd();
            }
            ui.MenuBarEnd();
        }

        ui.SplitBegin("panes", false, ref side);
        ui.Input("##search", ref search, 64);
        ui.Tooltip("filter notes by title");
        ui.Space(2);
        var order = Enumerable.Range(0, notes.Count).ToList();
        order.Sort((a, b) => Order(notes[a], notes[b]));
        foreach (int i in order)
        {
            if (search.Length > 0 && !notes[i].Title.Contains(search)) continue;
            if (ui.Selectable($"{(notes[i].Pinned ? "* " : "")}{notes[i].Title}##{i}", i == sel)) sel = i;
            if (ui.ContextBegin($"note{i}"))
            {
                if (ui.MenuItem("Open")) sel = i;
                ui.MenuItem("Pin to top", null, ref notes[i].Pinned);
                if (ui.MenuItem("Duplicate")) AddNote(notes[i].Title, notes[i].Tags, notes[i].Body, false, notes[i].Color);
                ui.MenuSeparator();
                if (ui.MenuItem("Delete"))
                {
                    sel = i;
                    confirm = true;
                }
                ui.ContextEnd();
            }
        }
        ui.Space(4);
        if (ui.Button("New note")) AddNote("Untitled", "", "");
        ui.SplitNext();
        n = sel >= 0 && sel < notes.Count ? notes[sel] : null;
        if (n != null)
        {
            ui.Heading(n.Title.Length > 0 ? n.Title : "Untitled");
            bool ch = false;
            ui.Row(2);
            ch |= ui.Input("Title", ref n.Title, 64);
            ch |= ui.Input("Tags", ref n.Tags, 64);
            ui.Row(2);
            ch |= ui.Combo("Label", ref n.Color, colors);
            ui.Combo("Sort notes by", ref sort, sorts);
            ch |= ui.Toggle("Pinned", ref n.Pinned);
            if (ui.Button("Save")) Save(n);
            ui.SameLine();
            if (ui.Button("Delete")) confirm = true;
            ui.SameLine();
            if (ui.Button("More...")) ui.PopupOpen("more");
            if (ui.PopupBegin("more"))
            {
                ui.Label($"{Words(n.Body)} words");
                ui.Toggle("Autosave", ref autosave);
                if (ui.Button("Duplicate")) AddNote(n.Title, n.Tags, n.Body, false, n.Color);
                if (ui.Button("Close")) ui.PopupClose();
                ui.PopupEnd();
            }
            ui.PushId($"note{sel}");
            ch |= ui.TextEdit("body", ref n.Body, 0, wrap ? EditFlags.Wrap : EditFlags.None, 4096);
            ui.PopId();
            if (ch) Touch(n);
        }
        else
            ui.Label("no note picked");
        ui.SplitEnd();

        if (ui.ModalBegin("Settings", ref settings))
        {
            if (ui.Slider("Text size", ref fontSize, 12, 20, "%.0f px")) ui.Theme.FontSize = fontSize;
            ui.Separator();
            ui.Toggle("Save while typing", ref autosave);
            ui.Space(6);
            if (ui.Button("Done")) ui.PopupClose();
            ui.ModalEnd();
        }
        if (ui.ModalBegin("Delete this note?", ref confirm))
        {
            ui.Wrapped(n != null ? "it goes away for good, theres no trash folder yet." : "nothing picked");
            ui.Space(6);
            if (ui.Button("Delete") && n != null)
            {
                notes.RemoveAt(sel);
                if (sel >= notes.Count) sel = notes.Count - 1;
                ui.Toast("Note deleted", 2000);
                ui.PopupClose();
            }
            ui.SameLine();
            if (ui.Button("Keep it")) ui.PopupClose();
            ui.ModalEnd();
        }
        if (ui.ModalBegin("About Notes", ref about))
        {
            ui.Label($"Notes, a goome {Goome.Version} demo in C#");
            if (ui.Link("goome-ui.github.io/docs")) ui.Toast("the docs are at goome-ui.github.io/docs", 2500);
            if (ui.Button("Nice")) ui.PopupClose();
            ui.ModalEnd();
        }
        if (ui.ModalBegin("Quit without saving?", ref quitting))
        {
            ui.Label("you have changes that arent saved");
            if (ui.Button("Save and quit"))
            {
                foreach (var x in notes) Save(x, true);
                app.Quit();
            }
            ui.SameLine();
            if (ui.Button("Quit")) app.Quit();
            ui.SameLine();
            if (ui.Button("Cancel")) ui.PopupClose();
            ui.ModalEnd();
        }

        if (autosave && typed != 0 && Goome.NowMs - typed > 1500)
        {
            foreach (var x in notes) Save(x, true);
            typed = 0;
        }
        app.RedrawEveryMs = typed != 0 ? 250 : 1000;
        bool left = notes.Any(x => x.Changed);
        ui.StatusBar($"{notes.Count} notes   {(n != null ? Words(n.Body) : 0)} words   {(left ? "unsaved changes" : "all saved")}");
    });
}
return 0;

class Note
{
    public string Title, Tags, Body;
    public bool Pinned, Changed;
    public int Color, Made, Edited;

    public Note(string title, string tags, string body, bool pinned = false, int color = 0) => (Title, Tags, Body, Pinned, Color) = (title, tags, body, pinned, color);
}
