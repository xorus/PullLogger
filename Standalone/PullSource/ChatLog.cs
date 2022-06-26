using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;

namespace Standalone.PullSource;

public unsafe class ChatLog
{
    [StructLayout(LayoutKind.Explicit)]
    struct LogBufferHdr
    {
        [FieldOffset(0)] public UInt32 contentSize;
        [FieldOffset(4)] public UInt32 fileSize;
        [FieldOffset(8)] public UInt32* offsetEntries;
    }

    // [StructLayout(LayoutKind.Explicit)]
    // struct LogEntry
    // {
    //     public UInt32 timestamp;
    //     public UInt16 eventType;
    //     public UInt16 unknown;
    //     [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 66)]
    //     public string message;
    // }

    public void read(byte[] data)
    {
        using var fs = new FileStream(@"D:\Xorus\Documents\My Games\FINAL FANTASY XIV - A Realm Reborn\FFXIV_CHR0040002E92E02EA5\log\00000000.log", FileMode.Open, FileAccess.Read);
        using var br = new BinaryReader(fs);

        var contentSize = br.ReadBytes(Marshal.SizeOf<UInt32>());
        var fileSize = br.ReadBytes(Marshal.SizeOf<UInt32>());

        // var size = (LogBufferHdr) br.ReadBytes(Marshal.SizeOf<LogBufferHdr>());


        // var hdr = br.ReadBytes(Marshal.SizeOf<LogBufferHdr>());

        // var hdr = (LogBufferHdr)data;
        //
        // var entries = new List<LogEntry>();
        // var offsetEntriesCount = hdr.fileSize - hdr.contentSize;
        //
        // for (var i = 0; i < offsetEntriesCount; i++)
        // {
        //     var offset = hdr.offsetEntries[i];
        //     var entry = (LogEntry)data.Slice(offset, LogEntry.Size);
        //     entries.Add(entry);
        // }
    }
    
    
    // [DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
    // public static extern int BitBlt(IntPtr hDC, int x, int y, int nWidth, int nHeight, IntPtr hSrcDC, int xSrc, int ySrc, int dwRop);
    //
    //
    // // Bitmap screenPixel = new Bitmap(1, 1, PixelFormat.Format32bppArgb);
    // public void GetColorAt(Point location)
    // {
    //     using (Graphics gdest = Graphics.FromImage(screenPixel))
    //     {
    //         using (Graphics gsrc = Graphics.FromHwnd(IntPtr.Zero))
    //         {
    //             IntPtr hSrcDC = gsrc.GetHdc();
    //             IntPtr hDC = gdest.GetHdc();
    //             int retval = BitBlt(hDC, 0, 0, 1, 1, hSrcDC, location.X, location.Y, (int)CopyPixelOperation.SourceCopy);
    //             gdest.ReleaseHdc();
    //             gsrc.ReleaseHdc();
    //         }
    //     }
    //
    //     return screenPixel.GetPixel(0, 0);
    // }

    public void Main()
    {
        Console.WriteLine("test");
    }
}