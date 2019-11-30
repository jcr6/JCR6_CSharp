// Lic:
// Drivers/FileTypes/a.cs
// a support for JCR6
// version: 19.11.30
// Copyright (C)  Jeroen P. Broks
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
#undef a_dbg

using System;
using TrickyUnits;



namespace UseJCR6 {
    class JCR_a : TJCRBASEDRIVER {
        const string header = "!<arch>\n";
        readonly string ending = $"{(char)0x60}{(char)0x0A}";

        void Chat(string a) {
#if a_dbg
            QCol.Magenta("DEBUG:> "); QCol.Yellow($"{a}\n");
#endif
        }

        public override bool Recognize(string file) {
            try {
                var ret = false;
                var bt = QuickStream.ReadFile(file);
                ret = bt.ReadString(header.Length) == header;
                bt.Close();
                return ret;
            } catch {
                return false;
            }
        }

        public override TJCRDIR Dir(string file) {
            var ret = new TJCRDIR();
            QuickStream bt = null;
            try {
                bt = QuickStream.ReadFile(file);
                if (bt.ReadString(header.Length) != header) throw new Exception("AR archive with incorrect header");
                while (!bt.EOF) {
                    var e = new TJCREntry();
                    e.Entry = bt.ReadNullTerminatedString(16).Trim(); if (qstr.Suffixed(e.Entry, "/") )e.Entry = qstr.Left(e.Entry, e.Entry.Length - 1); Chat($"File: \"{e.Entry}\"");
                    e.dataint["__TimeStamp"] = qstr.ToInt(bt.ReadNullTerminatedString(12)); Chat($"TimeStamp: {e.dataint["__TimeStamp"]}!");
                    Chat($"OwnerID:  {bt.ReadNullTerminatedString(6)}"); // OwnerID -- Not relevant or supported by JCR6 (yet)
                    Chat($"GroupID:  {bt.ReadNullTerminatedString(6)}"); // GroupID -- Not relevant or supported by JCR6 (yet)
                    Chat($"FileMode: {bt.ReadNullTerminatedString(8)}"); // Permisisons -- Not yet relevant, but might be added later!
                    e.Size = qstr.ToInt(bt.ReadNullTerminatedString(10)); Chat($"Size: {e.Size}");
                    e.CompressedSize = e.Size;
                    e.Storage = "Store";
                    e.MainFile = file;
                    //if (bt.ReadString(2) != ending) throw new Exception("End of file record is not 600A");
                    var e1 = bt.ReadByte();
                    var e2 = bt.ReadByte();
                    if (e1 != 0x60) throw new Exception($"0x60 expected, but got {e1.ToString("X2")}");
                    if (e2 != 0x0a) throw new Exception($"0x0a expected, but got {e1.ToString("X2")}");
                    ret.Entries[e.Entry.ToUpper()] = e;
                    e.Offset = (int)bt.Position;
                    bt.Position += e.Size;
                    byte b;
                    do b = bt.ReadByte(); while (b == 10);
                    bt.Position--;
                }
                return ret;
            } catch (Exception NETERROR) {
#if DEBUG
                JCR6.ERROR = $"{NETERROR.Message}\n{NETERROR.StackTrace}";
#else
                JCR6.JERROR = NETERROR.Message;
#endif
                return null;
            } finally {
                if (bt != null) bt.Close();
            }
        }


        public JCR_a() {
            MKL.Version("JCR6 - a.cs","19.11.30");
            MKL.Lic    ("JCR6 - a.cs","ZLib License");
            JCR6.FileDrivers["AR"] = this;
            JCR6.FileDrivers["AR"].name = "The Archiver";
        }
    }
}

