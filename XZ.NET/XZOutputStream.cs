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
using System.Runtime.InteropServices;

namespace XZ.NET
{
    public class XZOutputStream : Stream
    {
        private LzmaStream _lzmaStream;
        private readonly Stream _mInnerStream;
        private readonly bool leaveOpen;
        private readonly IntPtr _inbuf;
        private readonly IntPtr _outbuf;

        /// <summary>
        /// Default compression preset.
        /// </summary>
        public const uint DefaultPreset = 6;
        public const uint PresetExtremeFlag = (uint)1 << 31;

        // You can tweak BufSize value to get optimal results
        // of speed and chunk size
        private const int BufSize = 1 * 1024 * 1024;

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
                if (threads > Environment.ProcessorCount)
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

            if (ret == LzmaReturn.LzmaOK)
            {
                _inbuf = Marshal.AllocHGlobal(BufSize);
                _outbuf = Marshal.AllocHGlobal(BufSize);

                _lzmaStream.next_out = _outbuf;
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
            var outManagedBuf = new byte[BufSize];
            while(count != 0)
            {
                if(_lzmaStream.avail_in == UIntPtr.Zero)
                {
                    _lzmaStream.avail_in = (UIntPtr)Math.Min(checked((uint)count), BufSize);
                    Marshal.Copy(buffer, offset, _inbuf, (int)_lzmaStream.avail_in);
                    _lzmaStream.next_in = _inbuf;
                    offset += (int)_lzmaStream.avail_in;
                    count -= (int)_lzmaStream.avail_in;
                }

                do
                {
                    var ret = Native.lzma_code(ref _lzmaStream, LzmaAction.LzmaRun);
                    if(ret != LzmaReturn.LzmaOK) ThrowError(ret);

                    if (_lzmaStream.avail_out == UIntPtr.Zero)
                    {
                        Marshal.Copy(_outbuf, outManagedBuf, 0, BufSize);
                        _mInnerStream.Write(outManagedBuf, 0, BufSize);

                        _lzmaStream.next_out = _outbuf;
                        _lzmaStream.avail_out = (UIntPtr)BufSize;
                    }
                } while(_lzmaStream.avail_in != UIntPtr.Zero);
            }
        }

        void ThrowError(LzmaReturn ret)
        {
            Native.lzma_end(ref _lzmaStream);
            switch(ret)
            {
                case LzmaReturn.LzmaMemError: throw new InsufficientMemoryException("Memory allocation failed");
                case LzmaReturn.LzmaDataError: throw new Exception("File size limits exceeded");
                default: throw new Exception("Unknown error, possibly a bug: " + ret);
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
            LzmaReturn ret;
            do
            {
                ret = Native.lzma_code(ref _lzmaStream, LzmaAction.LzmaFinish);
                if(ret > LzmaReturn.LzmaStreamEnd) ThrowError(ret);

                if(_lzmaStream.avail_out == UIntPtr.Zero || ret == LzmaReturn.LzmaStreamEnd && (int)_lzmaStream.avail_out < BufSize)
                {
                    var outManagedBuf = new byte[BufSize - (int)_lzmaStream.avail_out];
                    Marshal.Copy(_outbuf, outManagedBuf, 0, outManagedBuf.Length);
                    _mInnerStream.Write(outManagedBuf, 0, outManagedBuf.Length);

                    _lzmaStream.next_out = _outbuf;
                    _lzmaStream.avail_out = (UIntPtr)BufSize;
                }
            } while(ret != LzmaReturn.LzmaStreamEnd);

            base.Close();
        }

        ~XZOutputStream() { Dispose(false); }

        protected override void Dispose(bool disposing)
        {
            Native.lzma_end(ref _lzmaStream);

            Marshal.FreeHGlobal(_inbuf);
            Marshal.FreeHGlobal(_outbuf);

            if(disposing && !leaveOpen) _mInnerStream?.Close();

            base.Dispose(disposing);
        }
    }
}
