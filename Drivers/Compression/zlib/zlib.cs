// Lic:
// Drivers/Compression/zlib/zlib.cs
// zlib driver for C# JCR6
// version: 21.03.09
// Copyright (C) 2018, 2021 Jeroen P. Broks
// This software is provided 'as-is', without any express or implied
// warranty.  In no event will the authors be held liable for any damages
// arising from the use of this software.
// Permission is granted to anyone to use this software for any purpose,
// including commercial applications, and to alter it and redistribute it
// freely, subject to the following restrictions:
// 1. The origin of this software must not be misrepresented; you must not
// claim that you wrote the original software. If you use this software
// in a product, an acknowledgment in the product documentation would be
// appreciated but is not required.
// 2. Altered source versions must be plainly marked as such, and must not be
// misrepresented as being the original software.
// 3. This notice may not be removed or altered from any source distribution.
// EndLic


// Let's go!!!



using ComponentAce.Compression.Libs.zlib;
using System.IO;
using System;



// Only used for MKL. If you hate that, just "mute" this line and all lines prefixed with "MKL." and everything should work.

using TrickyUnits;



//using zlib;



namespace UseJCR6 {

    class JCR6_zlib : TJCRBASECOMPDRIVER {



        public JCR6_zlib(bool nor = false) { if (!nor) Init(); }

        static public void Init() {
            //System.Console.WriteLine("DEBUG: ZLIB INIT!!!");
            JCR6.CompDrivers["zlib"] = new JCR6_zlib(true);
            MKL.Lic    ("JCR6 - zlib.cs","ZLib License");
            MKL.Version("JCR6 - zlib.cs","21.03.09");
        }



        public static int CopyStream(System.IO.Stream input, System.IO.Stream output) {
            int total = 0;
            byte[] buffer = new byte[2000];
            int len;
            while ((len = input.Read(buffer, 0, 2000)) > 0) {
                output.Write(buffer, 0, len);
                total += len;
            }
            output.Flush();
            return total;
        }

        private static void CompressData(byte[] inData, out byte[] outData) {
            using (MemoryStream outMemoryStream = new MemoryStream())
            using (ZOutputStream outZStream = new ZOutputStream(outMemoryStream, zlibConst.Z_DEFAULT_COMPRESSION))
            using (Stream inMemoryStream = new MemoryStream(inData)) {
                CopyStream(inMemoryStream, outZStream);
                outZStream.finish();
                outData = outMemoryStream.ToArray();
            }
        }



        override public byte[] Compress(byte[] inputbuffer) {
            /*
            byte[] ret = new byte[(int)Math.Ceiling(inputbuffer.Length * 1.75)]; // in zlib compression you must take into account that "reduced to" 175% can be the outcome, now JCR6 will if that happens, automatically resort to "Store", but if not taken care of here, JCR6 can and will crash before it can resort to Store....
            var outFileStream = new MemoryStream(ret);
            var compsize = 0;
            ZOutputStream outZStream = new ZOutputStream(outFileStream, zlibConst.Z_BEST_COMPRESSION);
            var inFileStream = new MemoryStream(inputbuffer);
            try {
                //Console.WriteLine($"\nPre: OutFileStream.Length {outFileStream.Length}");
                compsize = CopyStream(inFileStream, outZStream);
                //Console.WriteLine($"Pst: OutFileStream.Length {outFileStream.Length}");
                
                int test = ret.Length-1; while (test > -1 && ret[test] > 0) test--; Console.WriteLine($"\nTest<{test}>");
            } finally {
                outZStream.Close();
                outFileStream.Close();
                inFileStream.Close();
            }
            byte[] rettruncated = outFileStream.ToArray(); //new byte[compsize];
            //Array.Copy(ret, rettruncated, compsize);
#if DEBUG
            Console.WriteLine($"\nzlib.... input<{inputbuffer.Length} bytes>; Reserve<{(int)Math.Ceiling(inputbuffer.Length * 1.75)} bytes>; compsize<{compsize} bytes>; rettrunc<{rettruncated.Length} bytes>; ret<{ret.Length} bytes>"); 
#endif
            return rettruncated;
            */
            byte[] ret;
            CompressData(inputbuffer, out ret);
            return ret;
        }

        private static void DecompressData(byte[] inData, out byte[] outData) {
            using (MemoryStream outMemoryStream = new MemoryStream())
            using (ZOutputStream outZStream = new ZOutputStream(outMemoryStream))
            using (Stream inMemoryStream = new MemoryStream(inData)) {
                CopyStream(inMemoryStream, outZStream);
                outZStream.finish();
                outData = outMemoryStream.ToArray();
            }
        }


        public override byte[] Expand(byte[] inputbuffer, int realsize) {
            /*
            byte[] ret = new byte[realsize];
            var instr = new MemoryStream(inputbuffer);
            var oustr = new MemoryStream(ret);
            var zlstr = new ZOutputStream(oustr);
            try {
                CopyStream(instr, zlstr);
            } finally {
                zlstr.Close();
                oustr.Close();
                instr.Close();
            }
            */
            byte[] ret;
            DecompressData(inputbuffer, out ret);
            return ret;
        }

    }

}