// Lic:
//   QuakePAK.cs
//   
//   version: 18.08.27
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
using System.IO;

namespace UseJCR6
{

    // This driver allows JCR6 to read the PAK files Quake I uses. I do not 
    // know if it supports later instalments of Quake.
    // The QuakePAK format is rather just an improved version of the WAD 
    // system and therefore it was easy to redo the code needed for WAD to
    // Make it support this system, although some very significant differences
    // are still there.
    // Here too, I just as I did with WAD, I copied my original BlitzMax code
    // into C# and I traslated it. ;)

    // The original BlitzMax version I used was: 15.09.23


    class JCR_QuakePack : TJCRBASEDRIVER
    {

        public JCR_QuakePack()
        {
            MKL.Version("JCR6 - QuakePAK.cs","18.08.27");
            MKL.Lic    ("JCR6 - QuakePAK.cs","ZLib License");
            JCR6.FileDrivers["Quake PAK"] = this;
            name = "Quake PAK";
        }

        override public TJCRDIR Dir(string file)
        {
            var QuakePAK = file;
            //var SupportLevel = false;
            var Returner = new TJCRDIR();
            TJCREntry E;
            var BT = QOpen.ReadFile(QuakePAK, QOpen.LittleEndian);
            //var Level = "";
            //Local LevelFiles$[] = ["THINGS","LINEDEFS","SIDEDEFS","VERTEXES","SEGS","SSECTORS","NODES","SECTORS","REJECT","BLOCKMAP","BEHAVIOR"] ' All files used in a DOOM/HERETIC/HEXEN level, in which I must note that "BEHAVIOR" is only used in HEXEN.
            if (BT == null)
            {
                //'JCRD_DumpError = "JCR_FetchQuakePAK(~q"+QuakePAK+"~q): QuakePAK file could not be read"
                JCR6.JERROR = "QuakePAK file could not be read!\n\n" + QuakePAK;
                BT.Close();
                return null;
            }
            var Header = BT.ReadString(4);
            switch (Header)
            {
                case "PACK":
                    break; //Print "Quake Pack?"
                default:
                    JCR6.JERROR = "JCR_Fetch(\"" + QuakePAK + "\"): Requested file is not a QuakePAK file";
                    BT.Close();
                    return null;
            }
            Returner.CFGbool["__CaseSensitive"] = false;
            //'Next in the QuakePAK files are 2 32bit int values telling how many files the QuakePAK file contains and where in the QuakePAK file the File Table is stored
            var DirOffset = BT.ReadInt();
            var FileCount = BT.ReadInt();
            string FN;
            string[] FNS;
            //DebugLog "This QuakePAK contains "+(FileCount/64)+" entries starting at "+DirOffset
            BT.Position = DirOffset;
            //And let's now read all the crap
            for (int Ak = 0; Ak < FileCount; Ak += 64)
            {
                //'DebugLog "Reading entry #"+Ak
                if (BT.EOF) break;
                E = new TJCREntry();
                //E.PVars = New StringMap ' Just has to be present to prevent crashes in viewer based software.
                E.MainFile = QuakePAK;
                FN = BT.ReadString(56);
                E.Offset = BT.ReadInt();
                E.Size = BT.ReadInt();
                FNS = FN.Split((char)0);
                E.Entry = FNS[0]; //'Replace(Trim(ReadString(BT,8)),Chr(0),"")
                E.CompressedSize = E.Size;
                E.Storage = "Store"; // QuakePAK does not support compression, so always deal this as "Stored"
                //'E.Encryption = 0  ' QuakePAK does not support encryption, so always value 0
                //'If SupportLevel ' If set the system will turn DOOM levels into a folder for better usage. When unset the system will just dump everything together with not the best results, but hey, who cares :)
                //'Print "File = "+E.FileName+" >> Level = ~q"+Level+"~q >> Len="+Len(E.FileName)+" >> 1 = "+Left(E.FileName,1)+" >> 3 = "+Mid(E.FileName,3,1)
                //'If Level="" 
                //Rem
                //If(Left(E.FileName, 3) = "MAP")
                //Level = "MAP_" + E.FileName + "/"
                //ElseIf((Len(E.FileName) = 4 And Left(E.FileName, 1) = "E" And Mid(E.FileName, 3, 1) = "M")) 
                //Level = "MAP_" + E.FileName + "/"
                //ElseIf Level<>""
                //End Rem
                //var Ok = false;
                //            For Local S$= EachIn LevelFiles
                // If E.FileName = S Ok = True
                //
                //'Print "Comparing "+E.FileName+" with "+S+"   >>>> "+Ok
                //
                //Next
                //'If Ok E.FileName = Level+E.FileName Else level=""
                //'EndIf
                //'EndIf
                //Print "Adding: " + E.FileName
                Returner.Entries[E.Entry.ToUpper()] = E;
            }
            BT.Close();
            //'Return Ret
            return Returner;
        }





        override public bool Recognize(string file)
        {
            if (!File.Exists(file)) return false;
            var bt = QOpen.ReadFile(file);
            if (bt == null) return false;
            if (bt.Size < 12 ){ bt.Close(); return false; }
            var head = bt.ReadString(4);
            bt.Close();
            return head == "PACK";
        }
    }

}
