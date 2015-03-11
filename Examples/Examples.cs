﻿/**
 *  XZ.NET - a .NET wrapper for liblzma.dll
 *
 *  Copyright 2015 by Roman Belkov <romanbelkov@gmail.com>
 *  Copyright 2015 by Melentyev Kirill <melentyev.k@gmail.com>
 *
 *  Licensed under GNU General Public License 3.0 or later. 
 *  Some rights reserved. See LICENSE, AUTHORS, LICENSE-Notices.
 *
 * @license GPL-3.0+ <http://www.gnu.org/licenses/gpl-3.0.en.html>
 */

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using XZ.NET;

namespace Examples
{
    class Program
    {
        static void Main()
        {

            #region Displaying uncompressed data in console

            //using (var fileStream = new FileStream(@"test.txt.xz", FileMode.Open))
            //{
            //    using (var xzStream = new XZInputStream(fileStream))
            //    {
            //        var buf = new byte[2048];

            //        while (true)
            //        {
            //            var count = xzStream.Read(buf, 0, buf.Length);
            //            Console.Write(Encoding.ASCII.GetString(buf, 0, count));
            //            if (count == 0)
            //                break;
            //        }
            //    }
            //}

            #endregion

            #region Getting amount of data that we need to store uncompressed file

            //using (var fileStream = new FileStream(@"test.xz", FileMode.Open))
            //{
            //    using (var xzStream = new XZInputStream(fileStream))
            //    {
            //        var mem = xzStream.Length;

            //        Console.Write(mem);
            //    }
            //}

            #endregion

            #region Simple decompression

            //var inFileStream = new FileStream(@"test.xz", FileMode.Open);
            //var binaryWriter =
            //    new BinaryWriter(new FileStream(@"test.img", FileMode.OpenOrCreate, FileAccess.Write, FileShare.None));

            //using (var xzStream = new XZInputStream(inFileStream))
            //{
            //    var buf = new byte[2048];

            //    while (true)
            //    {
            //        var count = xzStream.Read(buf, 0, buf.Length);
            //        binaryWriter.Write(buf, 0, count);
            //        if (count == 0)
            //            break;
            //    }
            //}

            //inFileStream.Close();
            //binaryWriter.Close();

            #endregion

            #region Simple compression (with timing)

            //var timer = new Stopwatch();
            //timer.Start();

            //var writer =
            //    new BinaryReader(new FileStream(@"test.img", FileMode.Open, FileAccess.Read, FileShare.Read));
            //var output = new FileStream(@"test.img.xz", FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);

            //var f = new FileInfo(@"test.img");
            //var size = f.Length;

            //using (var strm = new XZOutputStream(output))
            //{
            //    var buf = new byte[1 * 1024 * 1024];
            //    Int64 bytesRead = 0;

            //    while (bytesRead < size)
            //    {
            //        var count = writer.Read(buf, 0, buf.Length);
            //        strm.Write(buf, 0, count);
            //        bytesRead += count;
            //    }
            //}

            //writer.Close();
            //output.Close();

            //timer.Stop();

            //var end = timer.ElapsedMilliseconds;
            //var spd = size / end;

            //Console.WriteLine("Time: " + end);
            //Console.WriteLine("Speed: " + spd);
            //Console.ReadKey();

            #endregion
        }
    }
}
