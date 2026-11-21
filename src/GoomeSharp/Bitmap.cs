using System;
using System.Runtime.InteropServices;

namespace GoomeSharp
{
    /// <summary>An image for drawing, loaded from a file or memory. Dispose it when you are done</summary>
    public sealed unsafe class Bitmap : IDisposable
    {
        RawBitmap* b;
        GCHandle pin;

        Bitmap() => b = (RawBitmap*)Mem.Alloc(sizeof(RawBitmap));

        /// <summary>Frees the pixels if you forgot to Dispose</summary>
        ~Bitmap() => Free();

        /// <summary>Why the last load failed</summary>
        public static string LastError => Native.Str(Native.BitmapError()) ?? "";

        static Bitmap Check(Bitmap bm, int rc)
        {
            if (rc == 0) return bm;
            string why = LastError;
            bm.Dispose();
            throw new GoomeException((GoomeError)rc, why);
        }

        /// <summary>Loads png, jpeg, bmp, ppm or qoi. It picks by content, not by name</summary>
        public static Bitmap Load(string path)
        {
            var bm = new Bitmap();
            int rc;
            fixed (byte* p = U8.A(path)) rc = Native.BitmapLoad(p, bm.b);
            return Check(bm, rc);
        }

        /// <summary>Loads png, jpeg, bmp, ppm or qoi. It picks by content, not by name</summary>
        public static Bitmap Load(ReadOnlySpan<byte> data)
        {
            var bm = new Bitmap();
            int rc;
            fixed (byte* p = data) rc = Native.BitmapLoadMem(p, (UIntPtr)data.Length, bm.b);
            return Check(bm, rc);
        }

        /// <summary>Copies plain argb pixels into a new bitmap</summary>
        public static Bitmap FromArgb(ReadOnlySpan<uint> argb, int width, int height)
        {
            if (width <= 0 || height <= 0 || argb.Length < (long)width * height) throw new ArgumentException("pixels too short for that size");
            var bm = new Bitmap();
            int rc;
            fixed (uint* p = argb) rc = Native.BitmapFromArgb(p, width, height, bm.b);
            return Check(bm, rc);
        }

        /// <summary>Wraps premultiplied pixels you already have, without copying</summary>
        /// <param name="premultiplied">The pixels, keep the array alive</param>
        /// <param name="stride">Row length in pixels, 0 = width</param>
        public static Bitmap Wrap(uint[] premultiplied, int width, int height, int stride = 0)
        {
            if (stride <= 0) stride = width;
            if (width <= 0 || height <= 0 || stride < width || premultiplied.Length < (long)stride * (height - 1) + width) throw new ArgumentException("pixels too short for that size");
            var bm = new Bitmap();
            bm.pin = GCHandle.Alloc(premultiplied, GCHandleType.Pinned);
            bm.b->Px = (uint*)bm.pin.AddrOfPinnedObject();
            bm.b->W = width;
            bm.b->H = height;
            bm.b->Stride = stride;
            return bm;
        }

        internal RawBitmap* Ptr => b != null ? b : throw new ObjectDisposedException(nameof(Bitmap));

        /// <summary>The raw c struct</summary>
        public ref RawBitmap Raw => ref *Ptr;
        /// <summary>Width in px</summary>
        public int Width => Ptr->W;
        /// <summary>Height in px</summary>
        public int Height => Ptr->H;
        /// <summary>Row length in pixels</summary>
        public int Stride => Ptr->Stride;
        /// <summary>True if goome owns the pixels and frees them</summary>
        public bool Owned => Ptr->Owned != 0;
        /// <summary>True once Dispose has run</summary>
        public bool IsDisposed => b == null;
        /// <summary>The pixels, premultiplied argb</summary>
        public Span<uint> Pixels => Ptr->Px == null ? default : new(b->Px, b->H <= 0 ? 0 : (b->H - 1) * b->Stride + b->W);

        /// <summary>Frees the bitmap</summary>
        public void Dispose()
        {
            Free();
            GC.SuppressFinalize(this);
        }

        void Free()
        {
            if (b == null) return;
            if (pin.IsAllocated) pin.Free();
            else Native.BitmapFree(b);
            Mem.Free(b);
            b = null;
        }
    }
}
