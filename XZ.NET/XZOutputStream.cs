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
using System.Runtime.InteropServices;

namespace XZ.NET
{
    public class XZOutputStream : Stream
    {
        private LzmaStream _lzmaStream;
        private readonly Stream _mInnerStream;
        private readonly IntPtr _inbuf;
        private readonly IntPtr _outbuf;

        private const int MaxThreads = 8;

        // This is a default compression preset & since
        // the output does not benefit a lot from changing 
        // this value it is hard coded
        private const int Preset = 6;

        // You can tweak BufSize value to get optimal results
        // of speed and chunk size
        private const int BufSize = 1 * 1024 * 1024;

        public XZOutputStream(Stream s)
        {
            _mInnerStream = s;

            var mt = new LzmaMT()
            {
                flags = 0,
                block_size = 0,
                timeout = 0,
                preset = Preset,
                filters = IntPtr.Zero,
                check = LzmaCheck.LzmaCheckCrc64,
                threads = (uint)Environment.ProcessorCount
            };

            if (mt.threads > MaxThreads)
                mt.threads = MaxThreads;

            var ret = Native.lzma_stream_encoder_mt(ref _lzmaStream, ref mt);
            //var ret = Native.lzma_easy_encoder(ref _lzmaStream, Preset, LzmaCheck.LzmaCheckCrc64);

            _inbuf = Marshal.AllocHGlobal(BufSize);
            _outbuf = Marshal.AllocHGlobal(BufSize);

            _lzmaStream.avail_in = 0;
            _lzmaStream.next_out = _outbuf;
            _lzmaStream.avail_out = BufSize;

            if (ret == LzmaReturn.LzmaOK)
                return;

            switch (ret)
            {
                case LzmaReturn.LzmaMemError:
                    throw new Exception("Memory allocation failed");

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

            if (_lzmaStream.avail_in == 0)
            {
                _lzmaStream.avail_in = (uint)count;
                Marshal.Copy(buffer, 0, _inbuf, (int)_lzmaStream.avail_in);
                _lzmaStream.next_in = _inbuf;
            }

            var ret = LzmaReturn.LzmaOK;

            while (_lzmaStream.avail_in > 0)
            {
                ret = Native.lzma_code(ref _lzmaStream, LzmaAction.LzmaRun);

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
                        throw new Exception("Memory allocation failed");

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
