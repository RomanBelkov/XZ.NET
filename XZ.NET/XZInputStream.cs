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
using System.IO;

namespace XZ.NET
{
    public unsafe class XZInputStream : Stream
    {
        private LzmaStream _lzmaStream;
        private readonly Stream _mInnerStream;
        private readonly bool leaveOpen;
        private readonly byte[] _inbuf;
        private int _inbufOffset;
        private long _length;

        // You can tweak BufSize value to get optimal results
        // of speed and chunk size
        private const int BufSize = 4096;
        private const int LzmaConcatenatedFlag = 0x08;

        public XZInputStream(Stream s) : this(s, false) { }
        public XZInputStream(Stream s, bool leaveOpen) : this()
        {
            if(s == null) throw new ArgumentNullException();
            _mInnerStream = s;
            this.leaveOpen = leaveOpen;
            _inbuf = new byte[BufSize];
        }

        public XZInputStream(byte[] buffer) : this()
        {
            _inbuf = buffer;
            _lzmaStream.avail_in = (UIntPtr)buffer.Length;
        }

        XZInputStream()
        {
            var ret = Native.lzma_stream_decoder(ref _lzmaStream, UInt64.MaxValue, LzmaConcatenatedFlag);

            if(ret == LzmaReturn.LzmaOK)
                return;

            GC.SuppressFinalize(this);
            switch (ret)
            {
                case LzmaReturn.LzmaMemError:
                    throw new InsufficientMemoryException("Memory allocation failed");

                case LzmaReturn.LzmaOptionsError:
                    throw new Exception("Unsupported decompressor flags");

                default:
                    throw new Exception("Unknown error, possibly a bug");
            }
        }

        #region Overrides
        public override void Flush()
        {
            throw new NotSupportedException("XZ Stream does not support flush");
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException("XZ Stream does not support seek");
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException("XZ Stream does not support setting length");
        }

        /// <summary>
        /// Reads bytes from stream
        /// </summary>
        /// <returns>Number of bytes read or 0 on end of stream</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if(count == 0) return 0;
            var guard = buffer[checked((uint)offset + (uint)count) - 1];

            var action = LzmaAction.LzmaRun;
            var readCount = 0;
            do
            {
                if (_lzmaStream.avail_in == UIntPtr.Zero)
                {
                    var read = _mInnerStream?.Read(_inbuf, 0, BufSize) ?? 0;
                    if((uint)read > BufSize) throw new InvalidDataException();
                    _lzmaStream.avail_in = (UIntPtr)read;
                    _inbufOffset = 0;
                    if(read == 0)
                        action = LzmaAction.LzmaFinish;
                }

                LzmaReturn ret;
                _lzmaStream.avail_out = (UIntPtr)count;
                fixed (byte* inbuf = &_inbuf[_inbufOffset])
                {
                    _lzmaStream.next_in = inbuf;
                    fixed (byte* outbuf = &buffer[offset])
                    {
                        _lzmaStream.next_out = outbuf;
                        ret = Native.lzma_code(ref _lzmaStream, action);
                    }
                    _inbufOffset += (int)(_lzmaStream.next_in - inbuf);
                }
                if(ret > LzmaReturn.LzmaStreamEnd) throw ThrowError(ret);

                var c = count - (int)(ulong)_lzmaStream.avail_out;
                readCount += c;
                if(ret == LzmaReturn.LzmaStreamEnd) break;
                offset += c;
                count -= c;
            } while(count != 0);
            return readCount;
        }

        Exception ThrowError(LzmaReturn ret)
        {
            Native.lzma_end(ref _lzmaStream);
            return GetDecodingError(ret);
        }

        static Exception GetDecodingError(LzmaReturn ret)
        {
            switch(ret)
            {
                case LzmaReturn.LzmaMemError: return new InsufficientMemoryException("Memory allocation failed");
                case LzmaReturn.LzmaFormatError: return new InvalidDataException("The input is not in the .xz format");
                case LzmaReturn.LzmaOptionsError: return new Exception("Unsupported compression options");
                case LzmaReturn.LzmaDataError: return new InvalidDataException("Compressed file is corrupt");
                case LzmaReturn.LzmaBufError: return new InvalidDataException("Compressed file is truncated or otherwise corrupt");
                default: return new Exception("Unknown error, possibly a bug: " + ret);
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("XZ Input stream does not support writing");
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        /// <summary>
        /// Gets the size of uncompressed data in bytes
        /// </summary>
        public override long Length
        {
            get
            {
                const int streamFooterSize = 12;

                if (_length == 0)
                {
                    var str = _mInnerStream;
                    if(str != null)
                    {
                        var buf = new byte[streamFooterSize];

                        str.Seek(-streamFooterSize, SeekOrigin.End);
                        if(str.Read(buf, 0, streamFooterSize) != streamFooterSize) throw new InvalidDataException();

                        var len = GetIndexSize(ref buf[0]);
                        if(len != streamFooterSize) buf = new byte[len];

                        str.Seek(-streamFooterSize - buf.Length, SeekOrigin.End);
                        if(str.Read(buf, 0, buf.Length) != buf.Length) throw new InvalidDataException();
                        str.Seek(0, SeekOrigin.Begin);
                        _length = GetUncompressedSize(buf, UIntPtr.Zero);
                    }
                    else _length = GetUncompressedSize(_inbuf);
                }
                return _length;
            }
        }

        static ulong GetIndexSize(ref byte footer)
        {
            LzmaStreamFlags lzmaStreamFlags;
            LzmaReturn ret;
            fixed (byte* inp = &footer) ret = Native.lzma_stream_footer_decode(&lzmaStreamFlags, inp);
            if(ret != LzmaReturn.LzmaOK) throw IndexDecodingError(ret);
            return lzmaStreamFlags.backwardSize;
        }

        public static long GetUncompressedSize(byte[] buf)
        {
            const int streamFooterSize = 12;
            return GetUncompressedSize(buf, (UIntPtr)((uint)buf.Length - streamFooterSize - GetIndexSize(ref buf[buf.Length - streamFooterSize])));
        }

        static long GetUncompressedSize(byte[] buf, UIntPtr inPos)
        {
            void* index;
            var memLimit = UInt64.MaxValue;

            var ret = Native.lzma_index_buffer_decode(&index, &memLimit, null, buf, &inPos, (UIntPtr)buf.Length);
            if(ret != LzmaReturn.LzmaOK) throw IndexDecodingError(ret);

            var uSize = Native.lzma_index_uncompressed_size(index);
            Native.lzma_index_end(index, null);
            return (long)uSize;
        }

        static Exception IndexDecodingError(LzmaReturn ret) => new InvalidDataException("Index decoding failed: " + ret);

        /// <summary>
        /// Single-call buffer decoding
        /// </summary>
        public static byte[] Decode(byte[] buffer)
        {
            var res = new byte[GetUncompressedSize(buffer)];

            var memLimit = UInt64.MaxValue;
            UIntPtr inPos, outPos;
            var ret = Native.lzma_stream_buffer_decode(&memLimit, LzmaConcatenatedFlag, null, buffer, &inPos, (UIntPtr)buffer.Length, res, &outPos, (UIntPtr)res.Length);
            if(ret != LzmaReturn.LzmaOK) throw GetDecodingError(ret);
            return res;
        }

        public override long Position
        {
            get { throw new NotSupportedException("XZ Stream does not support getting position"); }
            set { throw new NotSupportedException("XZ Stream does not support setting position"); }
        }

        ~XZInputStream() => Dispose(false);

        protected override void Dispose(bool disposing)
        {
            Native.lzma_end(ref _lzmaStream);

            if(disposing && !leaveOpen) _mInnerStream?.Close();

            base.Dispose(disposing);
        }

        #endregion
    }
}
