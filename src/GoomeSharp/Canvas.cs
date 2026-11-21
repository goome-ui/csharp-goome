using System;

namespace GoomeSharp
{
    /// <summary>A pixel buffer you draw on with logical coords. Colors are plain 0xAARRGGBB</summary>
    public sealed unsafe class Canvas : IDisposable
    {
        RawCanvas* c;
        readonly bool owned;

        internal Canvas(RawCanvas* p) => c = p;

        Canvas(int w, int h, int stride, double scale)
        {
            if (w <= 0 || h <= 0 || stride < w) throw new ArgumentException("bad canvas size");
            c = (RawCanvas*)Mem.Alloc(sizeof(RawCanvas));
            c->Px = (uint*)Mem.Alloc((long)stride * h * sizeof(uint));
            c->W = w;
            c->H = h;
            c->Stride = stride;
            c->Scale = scale;
            owned = true;
        }

        /// <summary>Makes an offscreen canvas you own and have to dispose</summary>
        /// <param name="stride">Row length in pixels, 0 = width</param>
        /// <param name="scale">Real px per logical px</param>
        public static Canvas Create(int width, int height, int stride = 0, double scale = 1) => new(width, height, stride > 0 ? stride : width, scale);

        /// <summary>Frees the canvas if you forgot to Dispose</summary>
        ~Canvas()
        {
            if (owned) Free();
        }

        /// <summary>Frees a canvas made with Create. Canvases from Begin belong to goome</summary>
        public void Dispose()
        {
            if (!owned) return;
            Free();
            GC.SuppressFinalize(this);
        }

        void Free()
        {
            if (c == null) return;
            Mem.Free(c->Px);
            Mem.Free(c);
            c = null;
        }

        internal void Detach() => c = null;

        internal RawCanvas* Ptr => c != null ? c : throw new ObjectDisposedException(nameof(Canvas));

        /// <summary>The raw c struct</summary>
        public ref RawCanvas Raw => ref *Ptr;
        /// <summary>True if you made it with Create</summary>
        public bool IsOwned => owned;
        /// <summary>Width in real px</summary>
        public int Width => Ptr->W;
        /// <summary>Height in real px</summary>
        public int Height => Ptr->H;
        /// <summary>Row length in pixels</summary>
        public int Stride => Ptr->Stride;
        /// <summary>Logical x of the first pixel</summary>
        public double OriginX => Ptr->Ox;
        /// <summary>Logical y of the first pixel</summary>
        public double OriginY => Ptr->Oy;
        /// <summary>Real px per logical px</summary>
        public double Scale => Ptr->Scale;
        /// <summary>Monitor id this canvas is for</summary>
        public int Monitor => Ptr->Mon;
        /// <summary>How many clips are pushed right now</summary>
        public int ClipDepth => Ptr->Nclip;
        /// <summary>The pixels, premultiplied argb. If you write them yourself call Damage</summary>
        public Span<uint> Pixels => new(Ptr->Px, Ptr->Stride * Ptr->H);

        /// <summary>Real px box drawn since the clear</summary>
        public (int X0, int Y0, int X1, int Y1) Drawn => (Ptr->Dx0, Ptr->Dy0, Ptr->Dx1, Ptr->Dy1);
        /// <summary>Real px box the clear wiped</summary>
        public (int X0, int Y0, int X1, int Y1) Cleared => (Ptr->Cx0, Ptr->Cy0, Ptr->Cx1, Ptr->Cy1);
        /// <summary>Real px clip box, drawing never leaves it</summary>
        public (int X0, int Y0, int X1, int Y1) Clip => (Ptr->Kx0, Ptr->Ky0, Ptr->Kx1, Ptr->Ky1);

        /// <summary>Tells goome you changed pixels yourself</summary>
        /// <param name="r">Logical rect, or none for the whole canvas</param>
        public void Damage(Rect r) => Native.Damage(Ptr, &r);
        /// <summary>Tells goome you changed pixels yourself</summary>
        public void Damage() => Native.Damage(Ptr, null);

        /// <summary>Wipes the canvas to see-through</summary>
        public void Clear() => Native.Clear(Ptr);
        /// <summary>Fills a rectangle</summary>
        public void DrawRect(double x, double y, double w, double h, uint argb) => Native.DrawRect(Ptr, x, y, w, h, argb);
        /// <summary>Fills a rectangle with rounded corners</summary>
        public void DrawRoundRect(double x, double y, double w, double h, double radius, uint argb) => Native.DrawRoundRect(Ptr, x, y, w, h, radius, argb);
        /// <summary>Draws a rectangle outline</summary>
        /// <param name="width">Line width</param>
        public void DrawFrame(double x, double y, double w, double h, double width, uint argb) => Native.DrawFrame(Ptr, x, y, w, h, width, argb);
        /// <summary>Draws a line</summary>
        /// <param name="width">Line width</param>
        public void DrawLine(double x0, double y0, double x1, double y1, double width, uint argb) => Native.DrawLine(Ptr, x0, y0, x1, y1, width, argb);
        /// <summary>Fills a triangle</summary>
        public void DrawTriangle(double x0, double y0, double x1, double y1, double x2, double y2, uint argb) => Native.DrawTriangle(Ptr, x0, y0, x1, y1, x2, y2, argb);
        /// <summary>Makes a rectangle see-through again</summary>
        public void Erase(double x, double y, double w, double h) => Native.Erase(Ptr, x, y, w, h);

        /// <summary>Draws plain argb pixels</summary>
        /// <param name="argb">Pixels, tightly packed</param>
        /// <param name="iw">Image width in px</param>
        /// <param name="ih">Image height in px</param>
        public void DrawImage(double x, double y, double w, double h, ReadOnlySpan<uint> argb, int iw, int ih)
        {
            if (iw < 0 || ih < 0 || argb.Length < (long)iw * ih) throw new ArgumentException("image pixels too short for that size");
            fixed (uint* p = argb) Native.DrawImage(Ptr, x, y, w, h, p, iw, ih);
        }

        /// <summary>Draws text with the built in font, y is the top</summary>
        /// <param name="big">Bigger built in size</param>
        /// <returns>Width of the text. The overload with a font takes a Font and a size</returns>
        public double DrawText(double x, double y, string text, bool big, uint argb)
        {
            fixed (byte* s = U8.A(text)) return Native.DrawText(Ptr, x, y, s, big ? 1 : 0, argb);
        }

        /// <summary>Width of text in the built in font</summary>
        public double TextWidth(string text, bool big = false)
        {
            fixed (byte* s = U8.A(text)) return Native.TextWidth(Ptr, s, big ? 1 : 0);
        }

        /// <summary>Draws text with the built in font, y is the top</summary>
        /// <returns>Width of the text. The overload with a font takes a Font and a size</returns>
        public double DrawText(Font? font, double size, double x, double y, string text, uint argb)
        {
            var f = font?.Handle ?? IntPtr.Zero;
            fixed (byte* s = U8.A(text)) return Native.Text(Ptr, f, size, x, y, s, argb);
        }

        /// <summary>How many built in fonts there are</summary>
        public static int BuiltInFontCount => Native.FontCount();
        /// <summary>Picks which built in font DrawText uses</summary>
        public static void SetBuiltInFont(int font) => Native.SetFont(font);

        /// <summary>Draws another canvas on this one</summary>
        /// <param name="alpha">0 to 1</param>
        /// <param name="dx">Logical x to put it at</param>
        /// <param name="dy">Logical y to put it at</param>
        public void Blit(Canvas src, double alpha, double dx, double dy) => Native.Blit(Ptr, src.Ptr, alpha, dx, dy);

        /// <summary>Limits drawing to a rect. Snaps to whole px, 16 deep, Begin resets it</summary>
        public void ClipPush(double x, double y, double w, double h) => Native.ClipPush(Ptr, x, y, w, h);
        /// <summary>Goes back to the clip before the last ClipPush</summary>
        public void ClipPop() => Native.ClipPop(Ptr);

        /// <summary>Fills a rectangle with a radius per corner</summary>
        public void DrawRRect(double x, double y, double w, double h, Radii r, uint argb) => Native.DrawRRect(Ptr, x, y, w, h, r, argb);
        /// <summary>Outlines a rectangle with a radius per corner</summary>
        /// <param name="width">Line width</param>
        public void DrawRRectLine(double x, double y, double w, double h, Radii r, double width, uint argb) => Native.DrawRRectLine(Ptr, x, y, w, h, r, width, argb);
        /// <summary>Fills a rounded rectangle with a gradient</summary>
        /// <param name="from">Start color</param>
        /// <param name="to">End color</param>
        /// <param name="sideways">True goes left to right, false goes top to bottom</param>
        public void DrawRRectGrad(double x, double y, double w, double h, Radii r, uint from, uint to, bool sideways = false) => Native.DrawRRectGrad(Ptr, x, y, w, h, r, from, to, sideways ? 1 : 0);
        /// <summary>Draws a soft shadow, blur works like css box-shadow</summary>
        /// <param name="radius">Corner radius of the shadowed box</param>
        /// <param name="blur">Softness</param>
        public void DrawShadow(double x, double y, double w, double h, double radius, double blur, uint argb) => Native.DrawShadow(Ptr, x, y, w, h, radius, blur, argb);
        /// <summary>Fills a circle</summary>
        public void DrawCircle(double cx, double cy, double r, uint argb) => Native.DrawCircle(Ptr, cx, cy, r, argb);
        /// <summary>Draws a circle outline, r is the middle of the stroke</summary>
        /// <param name="width">Line width</param>
        public void DrawRing(double cx, double cy, double r, double width, uint argb) => Native.DrawRing(Ptr, cx, cy, r, width, argb);
        /// <summary>Draws an arc in radians, 0 is 3 oclock, clockwise, with round caps</summary>
        /// <param name="width">Line width</param>
        /// <param name="a0">Start angle</param>
        /// <param name="a1">End angle</param>
        public void DrawArc(double cx, double cy, double r, double width, double a0, double a1, uint argb) => Native.DrawArc(Ptr, cx, cy, r, width, a0, a1, argb);

        /// <summary>Fills a polygon with the nonzero rule</summary>
        /// <param name="xy">Pairs of x,y</param>
        public void DrawPoly(ReadOnlySpan<double> xy, uint argb)
        {
            fixed (double* p = xy) Native.DrawPoly(Ptr, p, xy.Length / 2, argb);
        }

        /// <summary>Draws a bitmap scaled into the rect</summary>
        /// <param name="r">Corner radii</param>
        /// <param name="tint">Multiplied in, white leaves it alone</param>
        public void DrawBitmap(Bitmap b, double x, double y, double w, double h, Radii r, uint tint = 0xFFFFFFFF) => Native.DrawBitmap(Ptr, b.Ptr, x, y, w, h, r, tint);
        /// <summary>Draws a bitmap scaled into the rect</summary>
        public void DrawBitmap(Bitmap b, double x, double y, double w, double h) => DrawBitmap(b, x, y, w, h, 0);
        /// <summary>Draws a bitmap as a nine slice, so the borders stay the same size</summary>
        /// <param name="left">Left border in bitmap px</param>
        /// <param name="top">Top border in bitmap px</param>
        /// <param name="right">Right border in bitmap px</param>
        /// <param name="bottom">Bottom border in bitmap px</param>
        public void DrawBitmapNine(Bitmap b, double x, double y, double w, double h, double left, double top, double right, double bottom) => Native.DrawBitmapNine(Ptr, b.Ptr, x, y, w, h, left, top, right, bottom);
        /// <summary>Draws part of a bitmap</summary>
        /// <param name="sx">Source x in bitmap px</param>
        /// <param name="sy">Source y in bitmap px</param>
        /// <param name="sw">Source width</param>
        /// <param name="sh">Source height</param>
        public void DrawBitmapPart(Bitmap b, int sx, int sy, int sw, int sh, double x, double y, double w, double h, uint tint = 0xFFFFFFFF) => Native.DrawBitmapPart(Ptr, b.Ptr, sx, sy, sw, sh, x, y, w, h, tint);
    }
}
