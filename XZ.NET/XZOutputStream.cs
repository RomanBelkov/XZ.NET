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
    public sealed class XZOutputStream : Stream
    {
        private LzmaStream _lzmaStream;
        private readonly Stream _mInnerStream;
        private readonly IntPtr _inbuf;
        private readonly IntPtr _outbuf;

        // This is a default compression preset
        private const int Preset = 6;

        // You can tweak BufSize value to get optimal results
        // of speed and chunk size
        private const int BufSize = 1 * 1024 * 1024;

        public XZOutputStream(Stream s) : this(s, 1) // single thread by default
        {
        }

        public XZOutputStream(Stream s, int threads) : this(s, threads, Preset) { }

        public XZOutputStream(Stream s, int threads, byte compression)
        {
            if (threads <= 0) throw new ArgumentOutOfRangeException("threads");
            if (threads > Environment.ProcessorCount)
            {
                Trace.TraceWarning("{0} threads required, but only {1} processors available", threads, Environment.ProcessorCount);
                threads = Environment.ProcessorCount;
            }
            if(compression > 9) throw new ArgumentOutOfRangeException(nameof(compression));

            _mInnerStream = s;

            var mt = new LzmaMT()
            {
                flags = 0,
                block_size = 0,
                timeout = 0,
                preset = compression,
                filters = IntPtr.Zero,
                check = LzmaCheck.LzmaCheckCrc64,
                threads = (uint)threads
            };

            var ret = Native.lzma_stream_encoder_mt(ref _lzmaStream, ref mt);
            //var ret = Native.lzma_easy_encoder(ref _lzmaStream, compression, LzmaCheck.LzmaCheckCrc64);

            if(ret == LzmaReturn.LzmaOK)
            {
                _inbuf = Marshal.AllocHGlobal(BufSize);
                _outbuf = Marshal.AllocHGlobal(BufSize);

                _lzmaStream.avail_in = 0;
                _lzmaStream.next_out = _outbuf;
                _lzmaStream.avail_out = BufSize;
                return;
            }

            switch (ret)
            {
                case LzmaReturn.LzmaMemError:
                    throw new InsufficientMemoryException("Memory allocation failed");

                case LzmaReturn.LzmaOptionsError:
                    throw new Exception("Specified preset is not supported");

                case LzmaReturn.LzmaUnsupportedCheck:
                    throw new Exception("Specified integrity check is not supported");

                default:
                    throw new Exception("Unknown error, possibly a bug");
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

            var action = LzmaAction.LzmaRun;

            if (_lzmaStream.avail_in == 0)
            {
                _lzmaStream.avail_in = (uint)count;
                Marshal.Copy(buffer, offset, _inbuf, (int)_lzmaStream.avail_in);
                _lzmaStream.next_in = _inbuf;

                if (count < BufSize)
                    action = LzmaAction.LzmaFinish;
            }

            var ret = LzmaReturn.LzmaOK;

            while (_lzmaStream.avail_in > 0)
            {
                ret = Native.lzma_code(ref _lzmaStream, action);

                if (action == LzmaAction.LzmaFinish)
                {
                    while (ret != LzmaReturn.LzmaStreamEnd)
                    {
                        ret = Native.lzma_code(ref _lzmaStream, action);

                        if (_lzmaStream.avail_out == 0 || ret == LzmaReturn.LzmaStreamEnd)
                        {
                            var writeSize = BufSize - (int)_lzmaStream.avail_out;
                            Marshal.Copy(_outbuf, outManagedBuf, 0, writeSize);

                            _mInnerStream.Write(outManagedBuf, 0, writeSize);

                            _lzmaStream.next_out = _outbuf;
                            _lzmaStream.avail_out = BufSize;
                        }
                    }
                }

                if (_lzmaStream.avail_out == 0 || ret == LzmaReturn.LzmaStreamEnd)
                {
                    var writeSize = BufSize - (int)_lzmaStream.avail_out;
                    Marshal.Copy(_outbuf, outManagedBuf, 0, writeSize);

                    _mInnerStream.Write(outManagedBuf, 0, writeSize);

                    _lzmaStream.next_out = _outbuf;
                    _lzmaStream.avail_out = BufSize;
                }
            }

            if (ret != LzmaReturn.LzmaOK)
            {
                if (ret == LzmaReturn.LzmaStreamEnd)
                    return;

                Native.lzma_end(ref _lzmaStream);

                switch (ret)
                {
                    case LzmaReturn.LzmaMemError:
                        throw new InsufficientMemoryException("Memory allocation failed");

                    case LzmaReturn.LzmaDataError:
                        throw new Exception("File size limits exceeded");

                    default:
                        throw new Exception("Unknown error, possibly a bug");
                }
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
            Dispose(true);
        }

        protected override void Dispose(bool disposing)
        {
            _lzmaStream.avail_in = 0; //check if needed

            var ret = Native.lzma_code(ref _lzmaStream, LzmaAction.LzmaFinish);
            var outManagedBuf = new byte[BufSize];

            if (_lzmaStream.avail_out == 0 || ret == LzmaReturn.LzmaStreamEnd)
            {
                var writeSize = BufSize - (int)_lzmaStream.avail_out;
                Marshal.Copy(_outbuf, outManagedBuf, 0, writeSize);

                _mInnerStream.Write(outManagedBuf, 0, writeSize);
            }

            Native.lzma_end(ref _lzmaStream);

            Marshal.FreeHGlobal(_inbuf);
            Marshal.FreeHGlobal(_outbuf);

            base.Dispose(disposing);
        }
    }
}
