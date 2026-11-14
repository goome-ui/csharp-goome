using System;

namespace GoomeSharp
{
    public sealed unsafe class Font : IDisposable
    {
        IntPtr f;
        readonly bool owned;

        Font(IntPtr h, bool owned) => (f, this.owned) = (h, owned);

        public static Font BuiltIn { get; } = new(IntPtr.Zero, false);

        ~Font()
        {
            if (owned && f != IntPtr.Zero) Native.FontFree(f);
        }

        static Font Check(IntPtr h, string what) => h != IntPtr.Zero ? new Font(h, true) : throw new GoomeException(GoomeError.NotSupported, what);

        public static Font Load(string path)
        {
            IntPtr h;
            fixed (byte* p = U8.A(path)) h = Native.FontLoad(p);
            return Check(h, $"cant load the font {path}");
        }
        public static Font Load(string path, int index)
        {
            IntPtr h;
            fixed (byte* p = U8.A(path)) h = Native.FontLoadIndex(p, index);
            return Check(h, $"cant load face {index} of {path}");
        }

        public static Font Load(ReadOnlySpan<byte> data)
        {
            IntPtr h;
            fixed (byte* p = data) h = Native.FontLoadMem(p, (UIntPtr)data.Length, 1);
            return Check(h, "thats not a font goome can read");
        }

        public static Font? System(string? name = null)
        {
            IntPtr h;
            fixed (byte* p = U8.A(name)) h = Native.FontSystem(p);
            return h == IntPtr.Zero ? null : new Font(h, true);
        }

        internal static Font Borrow(IntPtr h) => h == IntPtr.Zero ? BuiltIn : new Font(h, false);

        internal IntPtr Handle => owned && f == IntPtr.Zero ? throw new ObjectDisposedException(nameof(Font)) : f;

        public bool IsBuiltIn => f == IntPtr.Zero && !owned;

        public double Measure(double size, string text)
        {
            var fh = Handle;
            fixed (byte* s = U8.A(text)) return Native.TextMeasure(fh, size, s, null);
        }

        public double Measure(double size, string text, out double height)
        {
            double h;
            double w;
            var fh = Handle;
            fixed (byte* s = U8.A(text)) w = Native.TextMeasure(fh, size, s, &h);
            height = h;
            return w;
        }

        public double LineHeight(double size) => Native.TextLineHeight(Handle, size);

        public void Dispose()
        {
            if (!owned || f == IntPtr.Zero) return;
            Native.FontFree(f);
            f = IntPtr.Zero;
            GC.SuppressFinalize(this);
        }
    }
}
