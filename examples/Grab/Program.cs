using System;
using System.Collections.Generic;
using GoomeSharp;

if (args.Length < 1)
{
    Console.Error.WriteLine("usage: Grab [monitor] [x y w h] out.ppm");
    return 2;
}
var nums = new List<int>();
for (int i = 0; i < args.Length - 1 && nums.Count < 5; i++) nums.Add(int.TryParse(args[i], out int v) ? v : 0);
int nn = nums.Count;

Goome g;
try
{
    g = new Goome(new GoomeOptions { Title = "goome grab" });
}
catch (GoomeException e)
{
    Console.Error.WriteLine($"cant open: {Goome.StrError(e.Code)}");
    return 1;
}
using (g)
{
    int mon = nn == 1 || nn == 5 ? nums[0] : 0;
    Rect? r = nn >= 4 ? new Rect(nums[nn - 4], nums[nn - 3], nums[nn - 2], nums[nn - 1]) : null;
    Image img = default;
    var e = GoomeError.Again;
    for (int tries = 0; tries < 50 && e == GoomeError.Again; tries++)
    {
        e = g.TryCapture(mon, r, out img);
        if (e == GoomeError.Again) g.Poll(out _, 100);
    }
    if (e != GoomeError.Ok)
    {
        Console.Error.WriteLine($"grab failed: {Goome.StrError(e)} ({g.LastError})");
        return 1;
    }
    e = Goome.SavePpm(args[^1], img);
    Console.WriteLine($"{img.Width}x{img.Height} px of {img.Area.X},{img.Area.Y} {img.Area.W}x{img.Area.H} on monitor {mon}" +
                      (img.HasSelf ? ", has our overlay" : "") + (img.HasCursor ? ", has the cursor" : ""));
    return e == GoomeError.Ok ? 0 : 1;
}
