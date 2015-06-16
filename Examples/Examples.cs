/* 
 * The MIT License (MIT)

 * Copyright (c) 2015 Roman Belkov, Kirill Melentyev

 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:

 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.

 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
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
