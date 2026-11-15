using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using GoomeSharp;

namespace GoomeUI.ModKit
{
    public static class NativeLoader
    {
        public static string? Loaded { get; private set; }

        static IntPtr handle;

        public static string FileName => Windows ? "goome.dll" : Mac ? "libgoome.dylib" : "libgoome.so";

        static bool Windows => Path.DirectorySeparatorChar == '\\';

        static bool Mac => !Windows && Directory.Exists("/System/Library/Frameworks");

        public static bool Load(params string?[] dirs)
        {
            if (Loaded != null) return true;
            foreach (var dir in dirs)
            {
                if (string.IsNullOrEmpty(dir)) continue;
                string path = Path.Combine(dir, FileName);
                if (!File.Exists(path)) continue;
                if (TryLoad(path, dir!)) return true;
            }
            return false;
        }

        public static string? DirOf(Assembly a)
        {
            try
            {
                return string.IsNullOrEmpty(a.Location) ? null : Path.GetDirectoryName(a.Location);
            }
            catch (NotSupportedException)
            {
                return null;
            }
        }

        static bool TryLoad(string path, string dir)
        {
            var nl = Type.GetType("System.Runtime.InteropServices.NativeLibrary");
            if (nl != null)
            {
                var load = nl.GetMethod("Load", new[] { typeof(string) });
                try
                {
                    handle = (IntPtr)load!.Invoke(null, new object[] { path })!;
                }
                catch (TargetInvocationException)
                {
                    return false;
                }
                HookResolver(nl);
                Loaded = path;
                return true;
            }
            try
            {
                if (Windows)
                {
                    SetDllDirectoryW(dir);
                    handle = LoadLibraryW(path);
                }
                else if (Mac)
                {
                    handle = DlopenMac(path, 2 | 8);
                }
                else
                {
                    try
                    {
                        handle = DlopenLinux(path, 2 | 0x100);
                    }
                    catch (DllNotFoundException)
                    {
                        handle = DlopenLibc(path, 2 | 0x100);
                    }
                }
            }
            catch (Exception e) when (e is DllNotFoundException || e is EntryPointNotFoundException)
            {
                return false;
            }
            if (handle == IntPtr.Zero) return false;
            Loaded = path;
            return true;
        }

        static void HookResolver(Type nl)
        {
            var resolverType = Type.GetType("System.Runtime.InteropServices.DllImportResolver");
            var set = nl.GetMethod("SetDllImportResolver");
            if (resolverType == null || set == null) return;
            var fn = Delegate.CreateDelegate(resolverType, typeof(NativeLoader).GetMethod(nameof(Resolve), BindingFlags.NonPublic | BindingFlags.Static)!);
            try
            {
                set.Invoke(null, new object[] { typeof(Goome).Assembly, fn });
            }
            catch (TargetInvocationException e) when (e.InnerException is InvalidOperationException)
            {
            }
        }

        static IntPtr Resolve(string name, Assembly asm, DllImportSearchPath? path) => name == "goome" ? handle : IntPtr.Zero;

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern IntPtr LoadLibraryW(string path);

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool SetDllDirectoryW(string path);

        [DllImport("libdl.so.2", EntryPoint = "dlopen")]
        static extern IntPtr DlopenLinux(string path, int flags);

        [DllImport("libc.so.6", EntryPoint = "dlopen")]
        static extern IntPtr DlopenLibc(string path, int flags);

        [DllImport("/usr/lib/libSystem.B.dylib", EntryPoint = "dlopen")]
        static extern IntPtr DlopenMac(string path, int flags);
    }
}
