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
using System.Runtime.InteropServices;

namespace XZ.NET
{
    internal enum LzmaReturn : uint
    {
        LzmaOK = 0,
        LzmaStreamEnd = 1,
        LzmaNoCheck = 2,
        LzmaUnsupportedCheck = 3,
        LzmaGetCheck = 4,
        LzmaMemError = 5,
        LzmaMemlimitError = 6,
        LzmaFormatError = 7,
        LzmaOptionsError = 8,
        LzmaDataError = 9,
        LzmaBufError = 10,
        LzmaProgError = 11
    }

    internal enum LzmaAction
    {
        LzmaRun = 0,
        LzmaSyncFlush = 1,
        LzmaFullFlush = 2,
        LzmaFinish = 3,
        LzmaFullBarrier = 4
    }

    internal enum LzmaCheck
    {
        LzmaCheckNone = 0,
        LzmaCheckCrc32 = 1,
        LzmaCheckCrc64 = 4,
        LzmaCheckSha256 = 10
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct LzmaStreamFlags
    {
        private readonly UInt32 version;

        public UInt64 backwardSize;

        public LzmaCheck check;

        readonly int reserved_enum1;
        readonly int reserved_enum2;
        readonly int reserved_enum3;
        readonly int reserved_enum4;
        readonly char reserved_bool1;
        readonly char reserved_bool2;
        readonly char reserved_bool3;
        readonly char reserved_bool4;
        readonly char reserved_bool5;
        readonly char reserved_bool6;
        readonly char reserved_bool7;
        readonly char reserved_bool8;
        readonly UInt32 reserved_int1;
        readonly UInt32 reserved_int2;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct LzmaStream
    {
        public IntPtr next_in;
        public UInt32 avail_in;
        public UInt64 total_in;

        public IntPtr next_out;
        public UInt32 avail_out;
        public UInt64 total_out;

        public IntPtr allocator;

        private readonly IntPtr internalState;

        private readonly IntPtr reserved_ptr1;
        private readonly IntPtr reserved_ptr2;
        private readonly IntPtr reserved_ptr3;
        private readonly IntPtr reserved_ptr4;
        private readonly UInt64 reserved_int1;
        private readonly UInt64 reserved_int2;
        private readonly UInt32 reserved_int3;
        private readonly UInt32 reserved_int4;
        private readonly UInt32 reserved_enum1;
        private readonly UInt32 reserved_enum2;
    }

    public static class Native
    {
        [DllImport("liblzma.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern LzmaReturn lzma_stream_decoder(ref LzmaStream stream, UInt64 memLimit, UInt32 flags);

        [DllImport("liblzma.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern LzmaReturn lzma_code(ref LzmaStream stream, LzmaAction action);

        [DllImport("liblzma.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern LzmaReturn lzma_stream_footer_decode(ref LzmaStreamFlags options, byte[] inp);

        [DllImport("liblzma.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern UInt64 lzma_index_uncompressed_size(IntPtr i);

        [DllImport("liblzma.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern UInt32 lzma_index_buffer_decode(ref IntPtr i, ref UInt64 memLimit, IntPtr allocator, byte[] indexBuffer,
            ref UInt32 inPosition, UInt64 inSize);

        [DllImport("liblzma.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void lzma_index_end(IntPtr i, IntPtr allocator);

        [DllImport("liblzma.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void lzma_end(ref LzmaStream stream);

        [DllImport("liblzma.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern LzmaReturn lzma_easy_encoder(ref LzmaStream stream, int preset, LzmaCheck check);
    }
}