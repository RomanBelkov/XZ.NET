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

namespace XZ.NET
{
    public unsafe class XZOutputStream : Stream
    {
        private LzmaStream _lzmaStream;
        private readonly Stream _mInnerStream;
        private readonly bool leaveOpen;
        private readonly byte[] _outbuf;

        /// <summary>
        /// Default compression preset.
        /// </summary>
        public const uint DefaultPreset = 6;
        public const uint PresetExtremeFlag = (uint)1 << 31;

        // You can tweak BufSize value to get optimal results
        // of speed and chunk size
        private const int BufSize = 4096;

        public XZOutputStream(Stream s) : this(s, 1) { }
        public XZOutputStream(Stream s, int threads) : this(s, threads, DefaultPreset) { }
        public XZOutputStream(Stream s, int threads, uint preset) : this(s, threads, preset, false) { }
        public XZOutputStream(Stream s, int threads, uint preset, bool leaveOpen)
        {
            _mInnerStream = s;
            this.leaveOpen = leaveOpen;

            LzmaReturn ret;
            if(threads == 1) ret = Native.lzma_easy_encoder(ref _lzmaStream, preset, LzmaCheck.LzmaCheckCrc64);
            else
            {
                if(threads <= 0) throw new ArgumentOutOfRangeException(nameof(threads));
                if(threads > Environment.ProcessorCount)
                {
                    Trace.TraceWarning("{0} threads required, but only {1} processors available", threads, Environment.ProcessorCount);
                    threads = Environment.ProcessorCount;
                }
                var mt = new LzmaMT()
                {
                    preset = preset,
                    check = LzmaCheck.LzmaCheckCrc64,
                    threads = (uint)threads
                };
                ret = Native.lzma_stream_encoder_mt(ref _lzmaStream, ref mt);
            }

            if(ret == LzmaReturn.LzmaOK)
            {
                _outbuf = new byte[BufSize];
                _lzmaStream.avail_out = (UIntPtr)BufSize;
                return;
            }

            GC.SuppressFinalize(this);
            switch (ret)
            {
                case LzmaReturn.LzmaMemError:
                    throw new InsufficientMemoryException("Memory allocation failed");

                case LzmaReturn.LzmaOptionsError:
                    throw new ArgumentException("Specified preset is not supported");

                case LzmaReturn.LzmaUnsupportedCheck:
                    throw new Exception("Specified integrity check is not supported");

                default:
                    throw new Exception("Unknown error, possibly a bug: " + ret);
            }
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if(count == 0) return;
            var guard = buffer[checked((uint)offset + (uint)count) - 1];

            if(_lzmaStream.avail_in != UIntPtr.Zero) throw new InvalidOperationException();
            _lzmaStream.avail_in = (UIntPtr)count;
            do
            {
                LzmaReturn ret;
                fixed (byte* inbuf = &buffer[offset])
                {
                    _lzmaStream.next_in = inbuf;
                    fixed (byte* outbuf = &_outbuf[BufSize - (int)(ulong)_lzmaStream.avail_out])
                    {
                        _lzmaStream.next_out = outbuf;
                        ret = Native.lzma_code(ref _lzmaStream, LzmaAction.LzmaRun);
                    }
                    offset += (int)(_lzmaStream.next_in - inbuf);
                }
                if(ret != LzmaReturn.LzmaOK) throw ThrowError(ret);

                if (_lzmaStream.avail_out == UIntPtr.Zero)
                {
                    _mInnerStream.Write(_outbuf, 0, BufSize);
                    _lzmaStream.avail_out = (UIntPtr)BufSize;
                }
            } while(_lzmaStream.avail_in != UIntPtr.Zero);
        }

        Exception ThrowError(LzmaReturn ret)
        {
            Native.lzma_end(ref _lzmaStream);
            switch(ret)
            {
                case LzmaReturn.LzmaMemError: return new InsufficientMemoryException("Memory allocation failed");
                case LzmaReturn.LzmaDataError: return new InvalidDataException("File size limits exceeded");
                default: return new Exception("Unknown error, possibly a bug: " + ret);
            }
        }

        public override bool CanRead
        {
            get { return false; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override long Length
        {
            get { throw new NotSupportedException(); }
        }

        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        public override void Close()
        {
            // finish encoding only if all input has been successfully processed
            if(_lzmaStream.internalState != IntPtr.Zero && _lzmaStream.avail_in == UIntPtr.Zero)
            {
                LzmaReturn ret;
                do
                {
                    fixed (byte* outbuf = &_outbuf[BufSize - (int)(ulong)_lzmaStream.avail_out])
                    {
                        _lzmaStream.next_out = outbuf;
                        ret = Native.lzma_code(ref _lzmaStream, LzmaAction.LzmaFinish);
                    }
                    if(ret > LzmaReturn.LzmaStreamEnd) throw ThrowError(ret);

                    var writeSize = BufSize - (int)(ulong)_lzmaStream.avail_out;
                    if(writeSize != 0)
                    {
                        _mInnerStream.Write(_outbuf, 0, writeSize);
                        _lzmaStream.avail_out = (UIntPtr)BufSize;
                    }
                } while(ret != LzmaReturn.LzmaStreamEnd);
            }

            base.Close();
        }

        ~XZOutputStream() => Dispose(false);

        protected override void Dispose(bool disposing)
        {
            Native.lzma_end(ref _lzmaStream);

            if(disposing && !leaveOpen) _mInnerStream?.Close();

            base.Dispose(disposing);
        }
    }
}
