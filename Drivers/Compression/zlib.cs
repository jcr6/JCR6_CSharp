/*
 * This code has been copied from https://gist.github.com/markandey/764648 and was either written or publishe by Mark Andy.
 * All I (Jeroen Broks) did was alter the code to make it work as a JCR6 compression driver in order to enable zlib support;
 */


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

    class InitQompress {
        static InitQompress() { JCR6.CompDrivers["zlib"] = new Qompress(); }
    }
}