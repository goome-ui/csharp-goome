using System;
using System.Runtime.InteropServices;

namespace GoomeSharp;

public sealed unsafe class Bitmap : IDisposable
{
    RawBitmap* b;
    GCHandle pin;

    Bitmap() => b = (RawBitmap*)NativeMemory.AllocZeroed((nuint)sizeof(RawBitmap));

    ~Bitmap() => Free();

    public static string LastError => Native.Str(Native.BitmapError()) ?? "";

    static Bitmap Check(Bitmap bm, int rc)
    {
        if (rc == 0) return bm;
        string why = LastError;
        bm.Dispose();
        throw new GoomeException((GoomeError)rc, why);
    }

    public static Bitmap Load(string path)
    {
        var bm = new Bitmap();
        return Check(bm, Native.BitmapLoad(path, bm.b));
    }

    public static Bitmap Load(ReadOnlySpan<byte> data)
    {
        var bm = new Bitmap();
        int rc;
        fixed (byte* p = data) rc = Native.BitmapLoadMem(p, (nuint)data.Length, bm.b);
        return Check(bm, rc);
    }

    public static Bitmap FromArgb(ReadOnlySpan<uint> argb, int width, int height)
    {
        if (width <= 0 || height <= 0 || argb.Length < (long)width * height) throw new ArgumentException("pixels too short for that size");
        var bm = new Bitmap();
        int rc;
        fixed (uint* p = argb) rc = Native.BitmapFromArgb(p, width, height, bm.b);
        return Check(bm, rc);
    }

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

    public ref RawBitmap Raw => ref *Ptr;
    public int Width => Ptr->W;
    public int Height => Ptr->H;
    public int Stride => Ptr->Stride;
    public bool Owned => Ptr->Owned != 0;
    public bool IsDisposed => b == null;
    public Span<uint> Pixels => Ptr->Px == null ? default : new(b->Px, b->H <= 0 ? 0 : (b->H - 1) * b->Stride + b->W);

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
        NativeMemory.Free(b);
        b = null;
    }
}
