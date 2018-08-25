/*
 * This code has been copied from https://gist.github.com/markandey/764648 and was either written or publishe by Mark Andy.
 * All I (Jeroen Broks) did was alter the code to make it work as a JCR6 compression driver in order to enable zlib support;
 *
 * Din't work though, so that's why I deactivated it


using System.IO;
using System.IO.Compression;
using System.Collections.Generic;


namespace UseJCR6
{
    class Qompress : TJCRBASECOMPDRIVER
    {

        public static byte[] CompressBuffer(byte[] byteArray)
        {
            MemoryStream strm = new MemoryStream();
            GZipStream GZipStrem = new GZipStream(strm, CompressionMode.Compress, true);
            GZipStrem.Write(byteArray, 0, byteArray.Length);
            GZipStrem.Flush();
            strm.Flush();
            byte[] ByteArrayToreturn = strm.GetBuffer();
            GZipStrem.Close();
            strm.Close();
            return ByteArrayToreturn;
        }

        public static byte[] DeCompressBuffer(byte[] byteArray)
        {
            MemoryStream strm = new MemoryStream(byteArray);
            GZipStream GZipStrem = new GZipStream(strm, CompressionMode.Decompress, true);
            List<byte> ByteListUncompressedData = new List<byte>();

            int bytesRead = GZipStrem.ReadByte();
            while (bytesRead != -1)
            {
                ByteListUncompressedData.Add((byte)bytesRead);
                bytesRead = GZipStrem.ReadByte();
            }
            GZipStrem.Flush();
            strm.Flush();
            GZipStrem.Close();
            strm.Close();
            return ByteListUncompressedData.ToArray();
        }

        public override byte[] Compress(byte[] inputbuffer) { return CompressBuffer(inputbuffer); }
        public override byte[] Expand(byte[] inputbuffer, int realsize) { return DeCompressBuffer(inputbuffer); }
    }

    class InitZLIB {
        static InitZLIB() { JCR6.CompDrivers["zlib"] = new Qompress(); JCR6.dCHAT("zlib driver present!"); }
    }

}
*/

/*
namespace UseJCR6
{
    class JCR6_zlib : TJCRBASECOMPDRIVER
    {

        public JCR6_zlib() { JCR6.CompDrivers["zlib"] = this; }

        public static byte[] Inflate(byte[] data)
        {
            int outputSize = 1024;
            byte[] output = new Byte[outputSize];
            bool expectRfc1950Header = true;
            using (MemoryStream ms = new MemoryStream())
            {
                ZlibCodec compressor = new ZlibCodec();
                compressor.InitializeInflate(expectRfc1950Header);

                compressor.InputBuffer = data;
                compressor.AvailableBytesIn = data.Length;
                compressor.NextIn = 0;
                compressor.OutputBuffer = output;

                foreach (var f in new FlushType[] { FlushType.None, FlushType.Finish })
                {
                    int bytesToWrite = 0;
                    do
                    {
                        compressor.AvailableBytesOut = outputSize;
                        compressor.NextOut = 0;
                        compressor.Inflate(f);

                        bytesToWrite = outputSize - compressor.AvailableBytesOut;
                        if (bytesToWrite > 0)
                            ms.Write(output, 0, bytesToWrite);
                    }
                    while ((f == FlushType.None && (compressor.AvailableBytesIn != 0 || compressor.AvailableBytesOut == 0)) ||
                        (f == FlushType.Finish && bytesToWrite != 0));
                }

                compressor.EndInflate();

                return ms.ToArray();
            }
        }

        public static byte[] Deflate(byte[] data)
        {
            int outputSize = 1024;
            byte[] output = new Byte[outputSize];
            int lengthToCompress = data.Length;

            // If you want a ZLIB stream, set this to true.  If you want
            // a bare DEFLATE stream, set this to false.
            bool wantRfc1950Header = true;

            using (MemoryStream ms = new MemoryStream())
            {
                ZlibCodec compressor = new ZlibCodec();
                compressor.InitializeDeflate(CompressionLevel.BestCompression, wantRfc1950Header);

                compressor.InputBuffer = data;
                compressor.AvailableBytesIn = lengthToCompress;
                compressor.NextIn = 0;
                compressor.OutputBuffer = output;

                foreach (var f in new FlushType[] { FlushType.None, FlushType.Finish })
                {
                    int bytesToWrite = 0;
                    do
                    {
                        compressor.AvailableBytesOut = outputSize;
                        compressor.NextOut = 0;
                        compressor.Deflate(f);

                        bytesToWrite = outputSize - compressor.AvailableBytesOut;
                        if (bytesToWrite > 0)
                            ms.Write(output, 0, bytesToWrite);
                    }
                    while ((f == FlushType.None && (compressor.AvailableBytesIn != 0 || compressor.AvailableBytesOut == 0)) ||
                        (f == FlushType.Finish && bytesToWrite != 0));
                }

                compressor.EndDeflate();

                ms.Flush();
                return ms.ToArray();
            }
        }

        public override byte[] Compress(byte[] inputbuffer) { return Deflate(inputbuffer); }
        public override byte[] Expand(byte[] inputbuffer, int realsize) { return Inflate(inputbuffer); }

    }

}
*/