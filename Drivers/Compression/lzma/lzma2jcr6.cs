// Lic:
//         lzma2jcr6.cs
// 	(c) 2018 Jeroen Petrus Broks.
// 	
// 	This Source Code Form is subject to the terms of the 
// 	Mozilla Public License, v. 2.0. If a copy of the MPL was not 
// 	distributed with this file, You can obtain one at 
// 	http://mozilla.org/MPL/2.0/.
//         Version: 18.08.26
// EndLic
using SevenZip.Compression.LZMA;
namespace UseJCR6{

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

        public JCR6_lzma() { JCR6.CompDrivers["lzma"] = this; }

    }

}
