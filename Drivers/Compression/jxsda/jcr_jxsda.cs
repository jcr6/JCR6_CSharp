// Lic:
// Drivers/Compression/jxsda/jcr_jxsda.cs
// jxsda driver for JCR6
// version: 21.03.09
// Copyright (C) 2020, 2021 Jeroen P. Broks
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

using TrickyUnits;

namespace UseJCR6 {

	class JCR6_JXSDA : TJCRBASECOMPDRIVER {

		public JCR6_JXSDA(bool nor = false) { if (!nor) Init(); }

		static public void Init() {
			//System.Console.WriteLine("DEBUG: ZLIB INIT!!!");
			JCR6.CompDrivers["jxsda"] = new JCR6_JXSDA(true);
			MKL.Lic    ("JCR6 - jcr_jxsda.cs","ZLib License");
			MKL.Version("JCR6 - jcr_jxsda.cs","21.03.09");
			//JXSDA.Verbose = true;
		}

		public override byte[] Compress(byte[] inputbuffer) => JXSDA.Pack(inputbuffer);

		public override byte[] Expand(byte[] inputbuffer, int realsize) {
			var ret = JXSDA.Unpack(inputbuffer);
			if (ret==null) { new JCR6Exception("Error in JXSDA unpacking"); }
			return ret;
		}

	}
}