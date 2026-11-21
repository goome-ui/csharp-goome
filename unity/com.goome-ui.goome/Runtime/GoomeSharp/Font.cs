#nullable enable
using System;

namespace GoomeSharp
{
    /// <summary>A font for Canvas.DrawText and the ui. Dispose it when you are done</summary>
    public sealed unsafe class Font : IDisposable
    {
        IntPtr f;
        readonly bool owned;
        System.Collections.Generic.List<Font>? fallbacks;
        static TextDirection direction;

        Font(IntPtr h, bool owned) => (f, this.owned) = (h, owned);

        /// <summary>The built in font</summary>
        public static Font BuiltIn { get; } = new(IntPtr.Zero, false);

        /// <summary>Frees the font if you forgot to Dispose</summary>
        ~Font()
        {
            if (owned && f != IntPtr.Zero) Native.FontFree(f);
        }

        static Font Check(IntPtr h, string what) => h != IntPtr.Zero ? new Font(h, true) : throw new GoomeException(GoomeError.NotSupported, what);

        /// <summary>Loads a .ttf, .otf or .ttc. The index overload picks a face out of a .ttc</summary>
        public static Font Load(string path)
        {
            IntPtr h;
            fixed (byte* p = U8.A(path)) h = Native.FontLoad(p);
            return Check(h, $"cant load the font {path}");
        }
        /// <summary>Loads a .ttf, .otf or .ttc. The index overload picks a face out of a .ttc</summary>
        /// <param name="index">Face number</param>
        public static Font Load(string path, int index)
        {
            IntPtr h;
            fixed (byte* p = U8.A(path)) h = Native.FontLoadIndex(p, index);
            return Check(h, $"cant load face {index} of {path}");
        }

        /// <summary>Loads a .ttf, .otf or .ttc. The index overload picks a face out of a .ttc</summary>
        public static Font Load(ReadOnlySpan<byte> data)
        {
            IntPtr h;
            fixed (byte* p = data) h = Native.FontLoadMem(p, (UIntPtr)data.Length, 1);
            return Check(h, "thats not a font goome can read");
        }

        /// <summary>The font the desktop uses for its ui, or a system font by name</summary>
        /// <param name="name">null = the desktops usual ui font</param>
        public static Font? System(string? name = null)
        {
            IntPtr h;
            fixed (byte* p = U8.A(name)) h = Native.FontSystem(p);
            return h == IntPtr.Zero ? null : new Font(h, true);
        }

        internal static Font Borrow(IntPtr h) => h == IntPtr.Zero ? BuiltIn : new Font(h, false);

        internal IntPtr Handle => owned && f == IntPtr.Zero ? throw new ObjectDisposedException(nameof(Font)) : f;

        /// <summary>True for the built in font</summary>
        public bool IsBuiltIn => f == IntPtr.Zero && !owned;

        /// <summary>Width of text at a size</summary>
        /// <param name="size">Text size in logical px</param>
        public double Measure(double size, string text)
        {
            var fh = Handle;
            fixed (byte* s = U8.A(text)) return Native.TextMeasure(fh, size, s, null);
        }

        /// <summary>Width of text at a size</summary>
        /// <param name="size">Text size in logical px</param>
        /// <param name="height">Height of the text</param>
        public double Measure(double size, string text, out double height)
        {
            double h;
            double w;
            var fh = Handle;
            fixed (byte* s = U8.A(text)) w = Native.TextMeasure(fh, size, s, &h);
            height = h;
            return w;
        }

        /// <summary>Height of one line at a size</summary>
        public double LineHeight(double size) => Native.TextLineHeight(Handle, size);

        /// <summary>Fonts to try, in order, for characters this one doesnt have. This font doesnt free it</summary>
        public void Fallback(Font fallback)
        {
            if (fallback == null) throw new ArgumentNullException(nameof(fallback));
            var fh = Handle;
            if (fh == IntPtr.Zero) throw new InvalidOperationException("the built in font cant have fallbacks");
            Native.FontFallback(fh, fallback.Handle);
            if (fallback == this) return;
            fallbacks ??= new System.Collections.Generic.List<Font>();
            if (!fallbacks.Contains(fallback)) fallbacks.Add(fallback);
        }

        /// <summary>Paragraph direction for all text. Auto goes by the first strong letter of each line</summary>
        public static TextDirection Direction
        {
            get => direction;
            set
            {
                Native.TextDir((int)value);
                direction = value;
            }
        }

        /// <summary>Frees the font</summary>
        public void Dispose()
        {
            if (!owned || f == IntPtr.Zero) return;
            Native.FontFree(f);
            f = IntPtr.Zero;
            GC.SuppressFinalize(this);
        }
    }
}
