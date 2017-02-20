/* 
 * The MIT License (MIT)

 * Copyright (c) 2015 Roman Belkov, Kirill Melentyev, Carter Li

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
using System.Runtime.CompilerServices;
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

    internal struct LzmaStreamFlags
    {
        private readonly UInt32 version;

        public UInt64 backwardSize;

        public LzmaCheck check;

        private readonly int reserved_enum1;
        private readonly int reserved_enum2;
        private readonly int reserved_enum3;
        private readonly int reserved_enum4;
        private readonly byte reserved_bool1, reserved_bool2, reserved_bool3, reserved_bool4, reserved_bool5, reserved_bool6, reserved_bool7, reserved_bool8;
        private readonly UInt32 reserved_int1;
        private readonly UInt32 reserved_int2;
    }

    internal struct LzmaMT
    {
        public UInt32 flags;
        public UInt32 threads;
        public UInt64 block_size;
        public UInt32 timeout;
        public UInt32 preset;
        public IntPtr filters;
        //public LzmaFilter filters;
        public LzmaCheck check;

        private readonly int reserved_enum1;
        private readonly int reserved_enum2;
        private readonly int reserved_enum3;
        private readonly int reserved_int1;
        private readonly int reserved_int2;
        private readonly int reserved_int3;
        private readonly int reserved_int4;
        private readonly UInt64 reserved_int5;
        private readonly UInt64 reserved_int6;
        private readonly UInt64 reserved_int7;
        private readonly UInt64 reserved_int8;
        private readonly IntPtr reserved_ptr1;
        private readonly IntPtr reserved_ptr2;
        private readonly IntPtr reserved_ptr3;
        private readonly IntPtr reserved_ptr4;
    }

    internal unsafe struct LzmaStream
    {
        public byte* next_in;
        public UIntPtr avail_in;
        public UInt64 total_in;

        public byte* next_out;
        public UIntPtr avail_out;
        public UInt64 total_out;

        public IntPtr allocator;

        public IntPtr internalState;

        private readonly IntPtr reserved_ptr1;
        private readonly IntPtr reserved_ptr2;
        private readonly IntPtr reserved_ptr3;
        private readonly IntPtr reserved_ptr4;
        private readonly UInt64 reserved_int1;
        private readonly UInt64 reserved_int2;
        private readonly UIntPtr reserved_int3;
        private readonly UIntPtr reserved_int4;
        private readonly UInt32 reserved_enum1;
        private readonly UInt32 reserved_enum2;
    }

    static unsafe class Native
    {
        const MethodImplOptions inline = (MethodImplOptions)0x100; // AggressiveInlining is not defined before .NET 4.5

        [MethodImpl(inline)]
        internal static LzmaReturn lzma_stream_decoder(ref LzmaStream stream, UInt64 memLimit, UInt32 flags)
            => IntPtr.Size > 4 ? X64.lzma_stream_decoder(ref stream, memLimit, flags) : X86.lzma_stream_decoder(ref stream, memLimit, flags);

        [MethodImpl(inline)]
        internal static LzmaReturn lzma_stream_buffer_decode(UInt64* memlimit, UInt32 flags, void* allocator, byte[] @in, UIntPtr* in_pos, UIntPtr in_size, byte[] @out, UIntPtr* out_pos, UIntPtr out_size)
            => IntPtr.Size > 4 ? X64.lzma_stream_buffer_decode(memlimit, flags, allocator, @in, in_pos, in_size, @out, out_pos, out_size)
            : X86.lzma_stream_buffer_decode(memlimit, flags, allocator, @in, in_pos, in_size, @out, out_pos, out_size);

        [MethodImpl(inline)]
        internal static LzmaReturn lzma_code(ref LzmaStream stream, LzmaAction action) => IntPtr.Size > 4 ? X64.lzma_code(ref stream, action) : X86.lzma_code(ref stream, action);

        [MethodImpl(inline)]
        internal static LzmaReturn lzma_stream_footer_decode(LzmaStreamFlags* options, byte* inp)
            => IntPtr.Size > 4 ? X64.lzma_stream_footer_decode(options, inp) : X86.lzma_stream_footer_decode(options, inp);

        [MethodImpl(inline)]
        internal static UInt64 lzma_index_uncompressed_size(void* i) => IntPtr.Size > 4 ? X64.lzma_index_uncompressed_size(i) : X86.lzma_index_uncompressed_size(i);

        [MethodImpl(inline)]
        internal static LzmaReturn lzma_index_buffer_decode(void** i, UInt64* memLimit, void* allocator, byte[] indexBuffer, UIntPtr* inPosition, UIntPtr inSize)
            => IntPtr.Size > 4 ? X64.lzma_index_buffer_decode(i, memLimit, allocator, indexBuffer, inPosition, inSize)
            : X86.lzma_index_buffer_decode(i, memLimit, allocator, indexBuffer, inPosition, inSize);

        [MethodImpl(inline)]
        internal static void lzma_index_end(void* i, void* allocator)
        {
            if(IntPtr.Size > 4) X64.lzma_index_end(i, allocator);
            else X86.lzma_index_end(i, allocator);
        }

        [MethodImpl(inline)]
        internal static void lzma_end(ref LzmaStream stream)
        {
            if(IntPtr.Size > 4) X64.lzma_end(ref stream);
            else X86.lzma_end(ref stream);
        }

        [MethodImpl(inline)]
        internal static LzmaReturn lzma_stream_encoder_mt(ref LzmaStream stream, ref LzmaMT mt)
            => IntPtr.Size > 4 ? X64.lzma_stream_encoder_mt(ref stream, ref mt) : X86.lzma_stream_encoder_mt(ref stream, ref mt);

        [MethodImpl(inline)]
        internal static LzmaReturn lzma_easy_encoder(ref LzmaStream stream, UInt32 preset, LzmaCheck check)
            => IntPtr.Size > 4 ? X64.lzma_easy_encoder(ref stream, preset, check) : X86.lzma_easy_encoder(ref stream, preset, check);

        [MethodImpl(inline)]
        internal static UIntPtr lzma_stream_buffer_bound(UIntPtr uncompressed_size)
            => IntPtr.Size > 4 ? X64.lzma_stream_buffer_bound(uncompressed_size) : X86.lzma_stream_buffer_bound(uncompressed_size);

        [MethodImpl(inline)]
        internal static LzmaReturn lzma_easy_buffer_encode(UInt32 preset, LzmaCheck check, void* allocator, byte[] @in, UIntPtr in_size, byte[] @out, UIntPtr* out_pos, UIntPtr out_size)
            => IntPtr.Size > 4 ? X64.lzma_easy_buffer_encode(preset, check, allocator, @in, in_size, @out, out_pos, out_size)
            : X86.lzma_easy_buffer_encode(preset, check, allocator, @in, in_size, @out, out_pos, out_size);

        [System.Security.SuppressUnmanagedCodeSecurity]
        static class X64
        {
            [DllImport("liblzma64.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
            internal static extern LzmaReturn lzma_stream_decoder(ref LzmaStream stream, UInt64 memLimit, UInt32 flags);

            [DllImport("liblzma64.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
            internal static extern LzmaReturn lzma_stream_buffer_decode(UInt64* memlimit, UInt32 flags, void* allocator, byte[] @in, UIntPtr* in_pos, UIntPtr in_size, byte[] @out, UIntPtr* out_pos, UIntPtr out_size);

            [DllImport("liblzma64.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
            internal static extern LzmaReturn lzma_code(ref LzmaStream stream, LzmaAction action);

            [DllImport("liblzma64.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
            internal static extern LzmaReturn lzma_stream_footer_decode(LzmaStreamFlags* options, byte* inp);

            [DllImport("liblzma64.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
            internal static extern UInt64 lzma_index_uncompressed_size(void* i);

            [DllImport("liblzma64.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
            internal static extern LzmaReturn lzma_index_buffer_decode(void** i, UInt64* memLimit, void* allocator, byte[] indexBuffer, UIntPtr* inPosition, UIntPtr inSize);

            [DllImport("liblzma64.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void lzma_index_end(void* i, void* allocator);

            [DllImport("liblzma64.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void lzma_end(ref LzmaStream stream);

            [DllImport("liblzma64.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
            internal static extern LzmaReturn lzma_stream_encoder_mt(ref LzmaStream stream, ref LzmaMT mt);

            [DllImport("liblzma64.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
            internal static extern LzmaReturn lzma_easy_encoder(ref LzmaStream stream, UInt32 preset, LzmaCheck check);

            [DllImport("liblzma64.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
            internal static extern UIntPtr lzma_stream_buffer_bound(UIntPtr uncompressed_size);

            [DllImport("liblzma64.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
            internal static extern LzmaReturn lzma_easy_buffer_encode(UInt32 preset, LzmaCheck check, void* allocator, byte[] @in, UIntPtr in_size, byte[] @out, UIntPtr* out_pos, UIntPtr out_size);
        }

        [System.Security.SuppressUnmanagedCodeSecurity]
        static class X86
        {
            [DllImport("liblzma.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
            internal static extern LzmaReturn lzma_stream_decoder(ref LzmaStream stream, UInt64 memLimit, UInt32 flags);

            [DllImport("liblzma.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
            internal static extern LzmaReturn lzma_stream_buffer_decode(UInt64* memlimit, UInt32 flags, void* allocator, byte[] @in, UIntPtr* in_pos, UIntPtr in_size, byte[] @out, UIntPtr* out_pos, UIntPtr out_size);

            [DllImport("liblzma.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
            internal static extern LzmaReturn lzma_code(ref LzmaStream stream, LzmaAction action);

            [DllImport("liblzma.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
            internal static extern LzmaReturn lzma_stream_footer_decode(LzmaStreamFlags* options, byte* inp);

            [DllImport("liblzma.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
            internal static extern UInt64 lzma_index_uncompressed_size(void* i);

            [DllImport("liblzma.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
            internal static extern LzmaReturn lzma_index_buffer_decode(void** i, UInt64* memLimit, void* allocator, byte[] indexBuffer, UIntPtr* inPosition, UIntPtr inSize);

            [DllImport("liblzma.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void lzma_index_end(void* i, void* allocator);

            [DllImport("liblzma.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void lzma_end(ref LzmaStream stream);

            [DllImport("liblzma.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
            internal static extern LzmaReturn lzma_stream_encoder_mt(ref LzmaStream stream, ref LzmaMT mt);

            [DllImport("liblzma.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
            internal static extern LzmaReturn lzma_easy_encoder(ref LzmaStream stream, UInt32 preset, LzmaCheck check);

            [DllImport("liblzma.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
            internal static extern UIntPtr lzma_stream_buffer_bound(UIntPtr uncompressed_size);

            [DllImport("liblzma.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
            internal static extern LzmaReturn lzma_easy_buffer_encode(UInt32 preset, LzmaCheck check, void* allocator, byte[] @in, UIntPtr in_size, byte[] @out, UIntPtr* out_pos, UIntPtr out_size);
        }
    }
}