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
        public XZInputStream(Stream s, bool leaveOpen)
        {
            _mInnerStream = s;
            this.leaveOpen = leaveOpen;

            var ret = Native.lzma_stream_decoder(ref _lzmaStream, UInt64.MaxValue, LzmaConcatenatedFlag);

            if(ret == LzmaReturn.LzmaOK)
            {
                _inbuf = new byte[BufSize];
                return;
            }

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
                    var read = _mInnerStream.Read(_inbuf, 0, BufSize);
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
                    _inbufOffset = (int)(_lzmaStream.next_in - inbuf);
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
                    var streamFooter = new byte[streamFooterSize];

                    _mInnerStream.Seek(-streamFooterSize, SeekOrigin.End);
                    if(_mInnerStream.Read(streamFooter, 0, streamFooterSize) != streamFooterSize) throw new InvalidDataException();

                    LzmaStreamFlags lzmaStreamFlags;
                    var ret = Native.lzma_stream_footer_decode(&lzmaStreamFlags, streamFooter);
                    if(ret != LzmaReturn.LzmaOK) throw new InvalidDataException("Index decoding failed: " + ret);
                    var indexPointer = new byte[lzmaStreamFlags.backwardSize];

                    _mInnerStream.Seek(-streamFooterSize - indexPointer.Length, SeekOrigin.End);
                    if(_mInnerStream.Read(indexPointer, 0, indexPointer.Length) != indexPointer.Length) throw new InvalidDataException();
                    _mInnerStream.Seek(0, SeekOrigin.Begin);

                    void* index;
                    var memLimit = UInt64.MaxValue;
                    UIntPtr inPos;

                    ret = Native.lzma_index_buffer_decode(&index, &memLimit, null, indexPointer, &inPos, (UIntPtr)indexPointer.Length);
                    if(ret != LzmaReturn.LzmaOK) throw new InvalidDataException("Index decoding failed: " + ret);

                    var uSize = Native.lzma_index_uncompressed_size(index);

                    Native.lzma_index_end(index, null);
                    _length = (Int64)uSize;
                    return _length;
                }
                else
                {
                    return _length;
                }
            }
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
