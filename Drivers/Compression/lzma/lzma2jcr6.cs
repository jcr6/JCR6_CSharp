// Lic:
// Drivers/Compression/lzma/lzma2jcr6.cs
// (c) 2018, 2020 Jeroen P. Broks (driver only. Algorithm still belongs to Igor Pavlov) :).
// 
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL was not
// distributed with this file, You can obtain one at
// http://mozilla.org/MPL/2.0/.
// Version: 20.08.16
// EndLic


using SevenZip.Compression.LZMA;
using TrickyUnits;

namespace UseJCR6 {
    class JCR6_lzma : TJCRBASECOMPDRIVER {
        override public byte[] Compress(byte[] inputbuffer) { return SevenZipHelper.Compress(inputbuffer); }
        override public byte[] Expand(byte[] inputbuffer, int realsize) {
            var ret = SevenZipHelper.Decompress(inputbuffer);
            if (ret.Length!=realsize) { 
                System.Console.WriteLine("WARNING! I expected this lzma block to be "+realsize+" byte long, but it is in fact "+ret.Length+" bytes long");
                if (ret.Length==inputbuffer.Length) { System.Console.WriteLine("I have the feeling decompression did NOT take place somehow(?)");}
            }            
            return ret;
        }



        public JCR6_lzma() { 
            JCR6.CompDrivers["lzma"] = this;
            MKL.Lic    ("JCR6 - lzma2jcr6.cs","Mozilla Public License 2.0");
            MKL.Version("JCR6 - lzma2jcr6.cs","20.08.16");
        }



        /// <summary>
        /// Initizes the JCR6 lzma drivers into JCR6
        /// </summary>
        static public void Init() { 
            new JCR6_lzma(); 
        }

    }



}