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
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace XZ.NET
{
    public class XZInputStream : Stream
    {
        private readonly List<byte> _mInternalBuffer = new List<byte>();
        private LzmaStream _lzmaStream;
        private readonly Stream _mInnerStream;
        private readonly IntPtr _inbuf;
        private readonly IntPtr _outbuf;
        private long _length;

        // You can tweak BufSize value to get optimal results
        // of speed and chunk size
        private const int BufSize = 512;
        private const int LzmaConcatenatedFlag = 0x08;

        public XZInputStream(Stream s)
        {
            _mInnerStream = s;

            var ret = Native.lzma_stream_decoder(ref _lzmaStream, UInt64.MaxValue, LzmaConcatenatedFlag);

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
        /// <returns>byte read or -1 on end of stream</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            var action = LzmaAction.LzmaRun;

            var readBuf = new byte[BufSize];
            var outManagedBuf = new byte[BufSize];

            while (_mInternalBuffer.Count < count)
            {
                if (_lzmaStream.avail_in == 0)
                {
                    _lzmaStream.avail_in = (uint)_mInnerStream.Read(readBuf, 0, readBuf.Length);
                    Marshal.Copy(readBuf, 0, _inbuf, (int)_lzmaStream.avail_in);
                    _lzmaStream.next_in = _inbuf;

                    if (_lzmaStream.avail_in == 0)
                        action = LzmaAction.LzmaFinish;
                }

                var ret = Native.lzma_code(ref _lzmaStream, action);

                if (_lzmaStream.avail_out == 0 || ret == LzmaReturn.LzmaStreamEnd)
                {
                    var writeSize = BufSize - (int)_lzmaStream.avail_out;
                    Marshal.Copy(_outbuf, outManagedBuf, 0, writeSize);

                    _mInternalBuffer.AddRange(outManagedBuf);
                    var tail = outManagedBuf.Length - writeSize;
                    _mInternalBuffer.RemoveRange(_mInternalBuffer.Count - tail, tail);

                    _lzmaStream.next_out = _outbuf;
                    _lzmaStream.avail_out = BufSize;
                }

                if (ret != LzmaReturn.LzmaOK)
                {
                    if (ret == LzmaReturn.LzmaStreamEnd)
                        break;

                    Native.lzma_end(ref _lzmaStream);

                    switch (ret)
                    {
                        case LzmaReturn.LzmaMemError:
                            throw new Exception("Memory allocation failed");

                        case LzmaReturn.LzmaFormatError:
                            throw new Exception("The input is not in the .xz format");

                        case LzmaReturn.LzmaOptionsError:
                            throw new Exception("Unsupported compression options");

                        case LzmaReturn.LzmaDataError:
                            throw new Exception("Compressed file is corrupt");

                        case LzmaReturn.LzmaBufError:
                            throw new Exception("Compressed file is truncated or otherwise corrupt");

                        default:
                            throw new Exception("Uknown error.Possibly a bug");
                    }
                }
            }

            if (_mInternalBuffer.Count >= count)
            {
                _mInternalBuffer.CopyTo(0, buffer, offset, count);
                _mInternalBuffer.RemoveRange(0, count);
                return count;
            }
            else
            {
                var intBufLength = _mInternalBuffer.Count;
                _mInternalBuffer.CopyTo(0, buffer, offset, intBufLength);
                _mInternalBuffer.Clear();
                return intBufLength;
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
        /// Gives a size of uncompressed data in bytes
        /// </summary>
        /// <returns>Size of uncompressed data or 0 if error occured</returns>
        public override long Length
        {
            get
            {
                const int streamFooterSize = 12;

                if (_length == 0)
                {
                    var lzmaStreamFlags = new LzmaStreamFlags();
                    var streamFooter = new byte[streamFooterSize];

                    _mInnerStream.Seek(-streamFooterSize, SeekOrigin.End);
                    _mInnerStream.Read(streamFooter, 0, streamFooterSize);

                    Native.lzma_stream_footer_decode(ref lzmaStreamFlags, streamFooter);
                    var indexPointer = new byte[lzmaStreamFlags.backwardSize];

                    _mInnerStream.Seek(-(Int64)streamFooterSize - (Int64)lzmaStreamFlags.backwardSize, SeekOrigin.End);
                    _mInnerStream.Read(indexPointer, 0, (int)lzmaStreamFlags.backwardSize);
                    _mInnerStream.Seek(0, SeekOrigin.Begin);

                    var index = IntPtr.Zero;
                    var memLimit = UInt64.MaxValue;
                    UInt32 inPos = 0;

                    Native.lzma_index_buffer_decode(ref index, ref memLimit, IntPtr.Zero, indexPointer, ref inPos,
                        lzmaStreamFlags.backwardSize);

                    if (inPos != lzmaStreamFlags.backwardSize)
                    {
                        Native.lzma_index_end(index, IntPtr.Zero);
                        throw new Exception("Index decoding failed!");
                    }

                    var uSize = Native.lzma_index_uncompressed_size(index);

                    Native.lzma_index_end(index, IntPtr.Zero);
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

        public override void Close()
        {
            Dispose(true);
        }

        protected override void Dispose(bool disposing)
        {
            Native.lzma_end(ref _lzmaStream);

            Marshal.FreeHGlobal(_inbuf);
            Marshal.FreeHGlobal(_outbuf);

            base.Dispose(disposing);
        }

        #endregion
    }
}
