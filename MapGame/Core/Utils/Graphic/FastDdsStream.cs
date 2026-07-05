using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MapGame.Core.Utils.Graphic
{
    public class FastDdsStream : Stream
    {
        private readonly byte[] _header;
        private readonly byte[] _pixelData;
        private long _position;

        public FastDdsStream(byte[] pixelData, int width, int height)
        {
            _pixelData = pixelData;
            _position = 0;

            _header = new byte[128];
            using var ms = new MemoryStream(_header);
            using var bw = new BinaryWriter(ms);
            bw.Write(0x20534444);   // Sygnatura "DDS "
            bw.Write(124);          // Rozmiar nagłówka
            bw.Write(0x0000100Fu);  // Flagi
            bw.Write(height);
            bw.Write(width);
            bw.Write(width * 4);    // Pitch (Stride)
            bw.Write(0);
            bw.Write(0);
            for (int i = 0; i < 11; i++) bw.Write(0); // Zarezerwowane

            bw.Write(32u);          // Rozmiar struktury pikseli
            bw.Write(0x41u);        // Flagi: RGB + Alpha
            bw.Write(0u);
            bw.Write(32u);          // Bity na piksel
            bw.Write(0x00FF0000u);  // Maska R
            bw.Write(0x0000FF00u);  // Maska G
            bw.Write(0x000000FFu);  // Maska B
            bw.Write(0xFF000000u);  // Maska Alpha

            bw.Write(0x1000u);      // DDSCAPS_TEXTURE
            bw.Write(0u);
            bw.Write(0u);
            bw.Write(0u);
            bw.Write(0u);
        }

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;

        public override long Length => 128 + _pixelData.Length;

        public override long Position
        {
            get => _position;
            set => _position = value;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesRead = 0;

            if (_position < 128)
            {
                int toRead = (int)Math.Min(count, 128 - _position);
                Array.Copy(_header, _position, buffer, offset, toRead);
                _position += toRead;
                offset += toRead;
                count -= toRead;
                bytesRead += toRead;
            }

            if (count > 0 && _position >= 128)
            {
                long pixelOffset = _position - 128;
                int toRead = (int)Math.Min(count, _pixelData.Length - pixelOffset);
                if (toRead > 0)
                {
                    Array.Copy(_pixelData, pixelOffset, buffer, offset, toRead);
                    _position += toRead;
                    bytesRead += toRead;
                }
            }

            return bytesRead;
        }
        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin: _position = offset; break;
                case SeekOrigin.Current: _position += offset; break;
                case SeekOrigin.End: _position = Length + offset; break;
            }
            return _position;
        }

        public override void Flush() { }
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
