// Lic:
// Drivers/FileTypes/WestwoodPAK.cs
// JCR6 - Westwood PAK driver
// version: 21.03.09
// Copyright (C) 2021 Jeroen P. Broks
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
using System.Collections.Generic;
using System.IO;
using System.Text;
using TrickyUnits;

namespace UseJCR6 {


     internal class WWEnt {
        internal uint
            offset,
         size;
        internal StringBuilder FileName;
        internal WWEnt() { offset = 0; FileName = new StringBuilder(); size = 0; }
    }


    /// <summary>
    /// Now Westwood PAK format is very extremely terrible and even beyond amateur!!
    /// Because of this, I cannot recommend to make your JCR6 based applications to support it
    /// unless it is really required or even some of the first purposes of the application
    /// you are setting up. If you want to use a kind of packing system in your game
    /// and are using JCR6 to support it Westwood PAK can better be ignored, as it's
    /// a file type that is very hard to autodetect, and false positives can easily pop up.
    /// The system supports no compression whatsoever in any way and also has no official
    /// way of being recognized.... As a matter of fact, technically JCR6 is just condemed to
    /// try to analyze the file and see if the content makes sense.... Sheeesh....
    /// </summary>
    class JCR_WestwoodPAK :TJCRBASEDRIVER {

        /// <summary>
        /// When set to 'false', Westwood PAK files will not be recognized. This can speed things up a little or prevent conflicts due to the chance of false positives.
        /// </summary>
        public bool Enable = true;

        string LastScanned = "***";
        TJCRDIR LastScannedDir = null;
        public string LastError { get; private set; } = "";


        void Error(string E) {
            LastScanned = "***";
            LastScannedDir = null;
            LastError = E;
        }

        void Scan(string file) {
            // Does the file even exist?
            if (!File.Exists(file)) { Error($"File not found: {file}"); return; }
            // Prepare
            var Entries = new List<WWEnt>();
            // Open
            var BT = QuickStream.ReadFile(file);
            WWEnt First = null;
            uint LastOffset = 0;
            // Read the actual data
            do {
                var Ent = new WWEnt();
                if (First == null) First = Ent;
                // 4-byte file start position.
                Ent.offset = BT.ReadUnSignedInt();

                var Position = BT.Position;
                // Trap for version 2 and 3 PAK files.
                if (Ent.offset > BT.Size) {
                    Error("Entry offset beyond EOF.");
                    return;
                }
                if (Ent.offset == 0) {
                    break;
                } else {
                    // Trap for version 1 PAK files.
                    if ((Position - 1) == First.offset) {
                        //Entries.Add(Ent); //FileCount = FileCount + 1
                        break;
                    } else {
                        if (Ent.offset < LastOffset) {
                            Error("Offset conflict. This cannot be a WestWood PAK");
                            return;
                        }
                        LastOffset = Ent.offset;
                        // Read the file name until we hit a null.             
                        byte Char = 0;
                        do {
                            Char = BT.ReadByte();
                            if (Char != 0) {
                                Ent.FileName.Append((char)Char);
                                if (Char < 30 || Char > 126) { Error($"Character #{Char} is not likely used in a file name! "); return; }
                            }
                        } while (Char > 0);
                        Entries.Add(Ent); //FileCount = FileCount + 1
                    }
                }
            } while (true);
            // Reading itself is done now!
            var ResSize = (uint)BT.Size;
            BT.Close();

            // Working with an array is easier from this point
            var EntArray = Entries.ToArray();

            // Calculating file sizes (it's really beyond me why Westwood saw fit NOT to include that essential data)
            for (uint FileNo = 0; FileNo < EntArray.Length; ++FileNo) {
                uint FileSize = 0;
                var cEnt = EntArray[FileNo];
                // Get the file size.
                if (FileNo == EntArray.Length-1) {
                    FileSize = ResSize - cEnt.offset;
                } else {
                    FileSize = EntArray[FileNo + 1].offset - cEnt.offset;
                }
                cEnt.size = FileSize;
            }

            // No to convert all collected data to data JCR6 can understand
            var Dir = new TJCRDIR();
            foreach(var WE in EntArray) {
                var E = new TJCREntry();
                E.Entry = WE.FileName.ToString();
                E.Size = (int)WE.size; if (E.Size<0) { Error("Invalid size data. This Westwood file may have gone beyond the limitations of JCR6"); return; } // The error is given the fact that this is a DOS format not likely to happen, but technically possible, so we must be prepared.
                E.Offset = (int)WE.offset; if (E.Size < 0) { Error("Invalid offset data. This Westwood file may have gone beyond the limitations of JCR6"); return; } // The error is given the fact that this is a DOS format not likely to happen, but technically possible, so we must be prepared.
                E.Author = "(?) Westwood Studios Inc. (?)";
                E.Notes = "Please be aware that this file came from a Westwood PAK file. Aside from copyright the file format is so primitive that I cannot guarantee things went right";
                E.MainFile = file;
                E.Storage = "Store"; // The only format PAK supports anyway, so that's easy.
                E.CompressedSize = E.Size;
                Dir.Entries[E.Entry.ToUpper()] = E;
            }
            LastScanned = file;
            LastScannedDir = Dir;
        }

        public override bool Recognize(string file) {
            Scan(file);
            return LastScannedDir != null;
        }

        public override TJCRDIR Dir(string file) {
            if (file != LastScanned || LastScannedDir == null)
                Scan(file);
            return LastScannedDir;
        }

        

        public JCR_WestwoodPAK(bool support = true) {
            name = "Westwood PAK";
            Enable = support;
            JCR6.FileDrivers[name] = this;
        }
    }

}