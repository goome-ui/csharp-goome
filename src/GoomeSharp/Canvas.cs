using System;
using System.Runtime.InteropServices;

namespace GoomeSharp;

public sealed unsafe class Canvas : IDisposable
{
    RawCanvas* c;
    readonly bool owned;

    internal Canvas(RawCanvas* p) => c = p;

    Canvas(int w, int h, int stride, double scale)
    {
        if (w <= 0 || h <= 0 || stride < w) throw new ArgumentException("bad canvas size");
        c = (RawCanvas*)NativeMemory.AllocZeroed((nuint)sizeof(RawCanvas));
        c->Px = (uint*)NativeMemory.AllocZeroed((nuint)stride * (nuint)h, sizeof(uint));
        c->W = w;
        c->H = h;
        c->Stride = stride;
        c->Scale = scale;
        owned = true;
    }

    public static Canvas Create(int width, int height, int stride = 0, double scale = 1) => new(width, height, stride > 0 ? stride : width, scale);

    ~Canvas()
    {
        if (owned) Free();
    }

    public void Dispose()
    {
        if (!owned) return;
        Free();
        GC.SuppressFinalize(this);
    }

    void Free()
    {
        if (c == null) return;
        NativeMemory.Free(c->Px);
        NativeMemory.Free(c);
        c = null;
    }

    internal void Detach() => c = null;

    internal RawCanvas* Ptr => c != null ? c : throw new ObjectDisposedException(nameof(Canvas));

    public ref RawCanvas Raw => ref *Ptr;
    public bool IsOwned => owned;
    public int Width => Ptr->W;
    public int Height => Ptr->H;
    public int Stride => Ptr->Stride;
    public double OriginX => Ptr->Ox;
    public double OriginY => Ptr->Oy;
    public double Scale => Ptr->Scale;
    public int Monitor => Ptr->Mon;
    public int ClipDepth => Ptr->Nclip;
    public Span<uint> Pixels => new(Ptr->Px, Ptr->Stride * Ptr->H);

    public (int X0, int Y0, int X1, int Y1) Drawn => (Ptr->Dx0, Ptr->Dy0, Ptr->Dx1, Ptr->Dy1);
    public (int X0, int Y0, int X1, int Y1) Cleared => (Ptr->Cx0, Ptr->Cy0, Ptr->Cx1, Ptr->Cy1);
    public (int X0, int Y0, int X1, int Y1) Clip => (Ptr->Kx0, Ptr->Ky0, Ptr->Kx1, Ptr->Ky1);

    public void Damage(Rect r) => Native.Damage(Ptr, &r);
    public void Damage() => Native.Damage(Ptr, null);

    public void Clear() => Native.Clear(Ptr);
    public void DrawRect(double x, double y, double w, double h, uint argb) => Native.DrawRect(Ptr, x, y, w, h, argb);
    public void DrawRoundRect(double x, double y, double w, double h, double radius, uint argb) => Native.DrawRoundRect(Ptr, x, y, w, h, radius, argb);
    public void DrawFrame(double x, double y, double w, double h, double width, uint argb) => Native.DrawFrame(Ptr, x, y, w, h, width, argb);
    public void DrawLine(double x0, double y0, double x1, double y1, double width, uint argb) => Native.DrawLine(Ptr, x0, y0, x1, y1, width, argb);
    public void DrawTriangle(double x0, double y0, double x1, double y1, double x2, double y2, uint argb) => Native.DrawTriangle(Ptr, x0, y0, x1, y1, x2, y2, argb);
    public void Erase(double x, double y, double w, double h) => Native.Erase(Ptr, x, y, w, h);

    public void DrawImage(double x, double y, double w, double h, ReadOnlySpan<uint> argb, int iw, int ih)
    {
        if (iw < 0 || ih < 0 || argb.Length < (long)iw * ih) throw new ArgumentException("image pixels too short for that size");
        fixed (uint* p = argb) Native.DrawImage(Ptr, x, y, w, h, p, iw, ih);
    }

    public double DrawText(double x, double y, string text, bool big, uint argb) => Native.DrawText(Ptr, x, y, text, big ? 1 : 0, argb);
    public double TextWidth(string text, bool big = false) => Native.TextWidth(Ptr, text, big ? 1 : 0);

    public double DrawText(Font? font, double size, double x, double y, string text, uint argb) => Native.Text(Ptr, font?.Handle ?? IntPtr.Zero, size, x, y, text, argb);

    public static int BuiltInFontCount => Native.FontCount();
    public static void SetBuiltInFont(int font) => Native.SetFont(font);

    public void Blit(Canvas src, double alpha, double dx, double dy) => Native.Blit(Ptr, src.Ptr, alpha, dx, dy);

    public void ClipPush(double x, double y, double w, double h) => Native.ClipPush(Ptr, x, y, w, h);
    public void ClipPop() => Native.ClipPop(Ptr);

    public void DrawRRect(double x, double y, double w, double h, Radii r, uint argb) => Native.DrawRRect(Ptr, x, y, w, h, r, argb);
    public void DrawRRectLine(double x, double y, double w, double h, Radii r, double width, uint argb) => Native.DrawRRectLine(Ptr, x, y, w, h, r, width, argb);
    public void DrawRRectGrad(double x, double y, double w, double h, Radii r, uint from, uint to, bool sideways = false) => Native.DrawRRectGrad(Ptr, x, y, w, h, r, from, to, sideways ? 1 : 0);
    public void DrawShadow(double x, double y, double w, double h, double radius, double blur, uint argb) => Native.DrawShadow(Ptr, x, y, w, h, radius, blur, argb);
    public void DrawCircle(double cx, double cy, double r, uint argb) => Native.DrawCircle(Ptr, cx, cy, r, argb);
    public void DrawRing(double cx, double cy, double r, double width, uint argb) => Native.DrawRing(Ptr, cx, cy, r, width, argb);
    public void DrawArc(double cx, double cy, double r, double width, double a0, double a1, uint argb) => Native.DrawArc(Ptr, cx, cy, r, width, a0, a1, argb);

    public void DrawPoly(ReadOnlySpan<double> xy, uint argb)
    {
        fixed (double* p = xy) Native.DrawPoly(Ptr, p, xy.Length / 2, argb);
    }

    public void DrawBitmap(Bitmap b, double x, double y, double w, double h, Radii r, uint tint = 0xFFFFFFFF) => Native.DrawBitmap(Ptr, b.Ptr, x, y, w, h, r, tint);
    public void DrawBitmap(Bitmap b, double x, double y, double w, double h) => DrawBitmap(b, x, y, w, h, 0);
    public void DrawBitmapPart(Bitmap b, int sx, int sy, int sw, int sh, double x, double y, double w, double h, uint tint = 0xFFFFFFFF) => Native.DrawBitmapPart(Ptr, b.Ptr, sx, sy, sw, sh, x, y, w, h, tint);
}
