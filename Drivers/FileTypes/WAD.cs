// Lic:
//   WAD.cs
//   WAD Driver for JCR6
//   version: 18.08.26
//   Copyright (C) 2018 Jeroen P. Broks
//   This software is provided 'as-is', without any express or implied
//   warranty.  In no event will the authors be held liable for any damages
//   arising from the use of this software.
//   Permission is granted to anyone to use this software for any purpose,
//   including commercial applications, and to alter it and redistribute it
//   freely, subject to the following restrictions:
//   1. The origin of this software must not be misrepresented; you must not
//      claim that you wrote the original software. If you use this software
//      in a product, an acknowledgment in the product documentation would be
//      appreciated but is not required.
//   2. Altered source versions must be plainly marked as such, and must not be
//      misrepresented as being the original software.
//   3. This notice may not be removed or altered from any source distribution.
// EndLic


using TrickyUnits;
using System.Collections.Generic;
using System.IO;

namespace UseJCR6{

    /* This driver should allow JCR6 to load WAD files 
     * as used in DOOM, DOOM II, Ultimate DOOM, Heretic, HeXen, 
     * Rise of the Triad, Hexx and maybe even a few more games.
     * 
     * It should also auto detect the strange way maps were put in WAD files.
     * Now I just copied my original BlitzMax code into C# and translated it 
     * in order to make C# understand it. 
     * 
     * Not the most elegant way to do it, but hey, what works that works, right?
     * 
     * The BlitzMax code I used was version 15.09.23
     */

    class JCR6_WAD:TJCRBASEDRIVER{

        public JCR6_WAD()
        {
            MKL.Version("JCR6 - WAD.cs","18.08.26");
            MKL.Lic    ("JCR6 - WAD.cs","ZLib License");
            name = "Where's All the Data?";

        }

        public bool SupportLevel = true;

        public override TJCRDIR Dir(string file)
        {
            //Private
            //Function JCR_FetchWAD:TJCRDir(WAD$, SupportLevel= 1)
            var Returner = new TJCRDIR();
            //var Ret:TMap = New TMap; returner.entries = ret
            TJCREntry E;
            var BT = QOpen.ReadFile(file, QOpen.LittleEndian);
            var Level = "";
            string[] LevelFiles = { "THINGS", "LINEDEFS", "SIDEDEFS", "VERTEXES", "SEGS", "SSECTORS", "NODES", "SECTORS", "REJECT", "BLOCKMAP", "BEHAVIOR" }; //' All files used in a DOOM/HERETIC/HEXEN level, in which I must note that "BEHAVIOR" is only used in HEXEN.
            if (BT == null)
            {
                JCR6.JERROR = "WAD file could not be read!\n" + WAD;
                return null;
            }
            //BT = LittleEndianStream(BT) ' WADs were all written for the MS-DOS platform, which used LittleEndian, so we must make sure that (even if the routine is used on PowerPC Macs) that LittleEndian is used
            //WAD files start with a header that can either be 'IWAD' for main wad files or 'PWAD' for patch WAD files. For JCR this makes no difference it all (it didn't even to WAD for that matter), but this is our only way to check if the WAD loaded is actually a WAD file.
            var Header = BT.ReadString(4);
            switch (Header)
            {
                case "IWAD":
                    Returner.Comments["Important notice"] = "The WAD file you are viewing is an IWAD,~nmeaning it belongs to a copyrighted project.~n~nAll content within it is very likely protected by copyright~neither by iD software or Apogee's Developers of Incredible Power or Raven Software.~n~nNothing can stop you from analysing this file and viewing its contents,~nbut don't extract and distribute any contents of this file~nwithout proper permission from the original copyright holder";
                    break;
                case "PWAD":
                    Returner.Comments["Notice"] = "This WAD file is a PWAD or Patch-WAD. It's not part of any official file of the games using the WAD system. Please respect the original copyright holders copyrights though!";
                    break;
                default:
                    JCR6.JERROR = "JCR_FetchWAD('" + WAD + "'): Requested file is not a WAD file";
                    return null;
            }
            Returner.CFGbool["__CaseSensitive"] = false;
            //'Next in the WAD files are 2 32bit int values telling how many files the WAD file contains and where in the WAD file the File Table is stored
            var FileCount = BT.ReadInt();
            var DirOffset = BT.ReadInt();
            //DebugLog "This WAD contains "+FileCount+" entries starting at "+DirOffset
            BT.Position = DirOffset;
            // And let's now read all the crap
            for (int Ak = 1; Ak <= FileCount; Ak++)
            {
                //    'DebugLog "Reading entry #"+Ak
                E = new TJCREntry();
                // E.PVars = New StringMap ' Just has to be present to prevent crashes in viewer based software. // Not an issue in the C# version.
                E.MainFile = file;
                E.Offset = BT.ReadInt();
                E.Size = BT.ReadInt();
                E.Entry = BT.ReadString(8).Trim().Replace("\0", ""); //Replace(Trim(ReadString(BT, 8)), Chr(0), "")
                E.CompressedSize = E.Size;
                E.Storage = "Store"     // WAD does not support compression, so always deal this as "Stored"
                //    'E.Encryption = 0  ' WAD does not support encryption, so always value 0
                    if (SupportLevel) // ' If set the system will turn DOOM levels into a folder for better usage. When unset the system will just dump everything together with not the best results, but hey, who cares :)
                {
                    //'Print "File = "+E.FileName+" >> Level = ~q"+Level+"~q >> Len="+Len(E.FileName)+" >> 1 = "+Left(E.FileName,1)+" >> 3 = "+Mid(E.FileName,3,1)
                    //'If Level="" 
                    if (qstr.Left(E.FileName, 3) == "MAP")
                    {
                        Level = "MAP_" + E.Entry + "/";
                    }
                    else if (E.Entry.Length == 4 && qstr.Left(E.Entry, 1) == "E" && qstr.Mid(E.Entry, 3, 1) == "M")
                    {
                        Level = "MAP_" + E.FileName + "/";
                    }
                    else if (Level != "")
                    {
                        var Ok = false;
                        foreach (string S in LevelFiles)
                        {
                            Ok = Ok || E.Entry == S;
                            //'Print "Comparing "+E.FileName+" with "+S+"   >>>> "+Ok
                        }
                        if (Ok) E.Entry = Level + E.Entry; else Level = "";
                    }
                }
                JCR6.dCHAT("Adding: " + E.Entry);
                Returner.Entries[E.Entry.ToUpper()] = E;
            }
            BT.Close();
            //Return Ret
            return Returner;
        }
        //Public

        override public bool Recognize(string file)
        {
            if (!File.Exists(file)) return false;
            var bt = QOpen.ReadFile(fil, QOpen.LittleEndian);
            if (bt == null) return false;
            if (bt.Size < 4) { bt.Close; return false; }
            var head = bt.ReadString(4);
            bt.Close();
            return head == "IWAD" || head == "PWAD";
        }


    }

}
