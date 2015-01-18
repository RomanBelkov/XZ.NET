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
            //    using (var xzStream = new XZStream(fileStream))
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
            //    using (var xzStream = new XZStream(fileStream))
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
            
            //    using (var xzStream = new XZStream(inFileStream))
            //    {
            //        var buf    = new byte[2048];

            //        while (true)
            //        {
            //            var count = xzStream.Read(buf, 0, buf.Length);
            //            binaryWriter.Write(buf, 0, count);
            //            if (count == 0)
            //                break;
            //        }
            //    }

            //inFileStream.Close();
            //binaryWriter.Close();

            #endregion
        }
    }
}
