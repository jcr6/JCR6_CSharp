// Lic:
// Drivers/FileTypes/JCR5.cs
// JCR5 for JCR6 C#
// version: 19.11.16
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


using TrickyUnits;

namespace UseJCR6
{
    /// <summary>This driver allows reading and extracting from JCR5 files. Please note this is only read-only as JCR5 has been discontinued ages ago, but some older games of mine which are still eligable for bugfixes and stuff still use JCR5, so viewing these files is still an important feature to me. Using JCR5 in stead of JCR6 is STRONGLY discouraged.</summary>
    class JCR_JCR5 : TJCRBASEDRIVER
    {

        public JCR_JCR5() => Init(false);
        string[] StorageName = new string[]{ "Store", "zlib" };

        public static void Init(bool doit=true)
        {
            if (doit)
            {
                MKL.Version("JCR6 - JCR5.cs","19.11.16");
                MKL.Lic    ("JCR6 - JCR5.cs","ZLib License");
                JCR6.FileDrivers["JCR5"] = new JCR_JCR5();
                JCR6.FileDrivers["JCR5"].name = "JCR5";
            }
        }

        override public bool Recognize(string file)
        {
            try {
                var BT = QuickStream.ReadFile(file);
                if (BT == null) return false;
                var Header = BT.ReadString(5);
                BT.Close();
                return Header == "JCR5" + qstr.Chr(26);
            } catch {
                return false;
            }
        }

        void JCR_JAMERR(string e, string mf, string en, string f) { JCR6.JERROR = ($"JCR5 ERROR!\n{e}\n\nMainfile: {mf}; Entry {en};\nFunction: {f}").Replace("~q",@"\"); }

        override public TJCRDIR Dir(string file)
        {
            var Ret = new TJCRDIR();
            var BT = QuickStream.ReadFile(file, QuickStream.LittleEndian);
            var Header = "";
            var FE = new TJCREntry();
            int Tag;
            int Length;
            byte[] PackedBank;
            byte[] UnpackedBank;
            long PackedSize;
            long UnPackedSize;
            //Local JCRD_DumpError$= ""
            var JCRFILE = file;
            if (BT == null)
            {
                JCR_JAMERR("JCR_Dir(~q" + JCRFILE + "~q): JCR file has not been found!", file, "N/A", "Dir");
                return null;
            }
            Header = BT.ReadString(5);
            if (Header != "JCR5" + qstr.Chr(26))
            {
                JCR_JAMERR("JCR_Dir(~q" + JCRFILE + "~q): File given appears not to be a JCR 5 file!", file, "N/A", "Dir");
                BT.Close();
                return null;
            }
            var FatOffSet = BT.ReadLong();
            if (FatOffSet > BT.Size)
            {
                JCR_JAMERR("JCR_Dir(~q" + JCRFILE + "~q): FAT offset beyond EOF (" + FatOffSet + ">" + BT.Size, file, "N/A", "Dir");
                BT.Close();
                return null;
            }
            BT.Position = FatOffSet;
            do
            {//Repeat
                Tag = BT.ReadByte();
                //'DebugLog "JCR-TAG:"+Tag
                switch (Tag)
                {
                    case 0xff: break;
                    case 0: BT.ReadLong(); break; //'JCRD_Print "JCR File ~q"+JCRFILE+"~q contains "+ReadLong(BT)+" entries~n~n"
                    case 1: FE = new TJCREntry(); FE.MainFile = JCRFILE; break;
                    case 2:
                        Length = BT.ReadInt();
                        //'DebugLog "FLen = "+Length; 
                        FE.Entry = BT.ReadString(Length);
                        Ret.Entries[FE.Entry.ToUpper()] = FE; //MapInsert Ret.Entries, Upper(FE.FileName), FE;
                                                              //'DebugLog "Found: "+FE.FIleName
                        break;
                    case 3: FE.Size = (int)BT.ReadLong(); break;
                    case 4: FE.Offset = (int)BT.ReadLong(); break;
                    case 5: BT.ReadInt(); break; //' FE.Time = ReadInt(BT) ' Not supported in JCR6 yet(strictly speaking it's easy to support it, but it's not needed in JCR6's purpose and therefore not supported by this driver)
                    case 6: BT.ReadInt(); break; //'FE.Permissions = ReadInt(BT) ' Permissions not supported in JCR6 yet, and I doubt they ever will be.
                    case 7: FE.Storage = StorageName[BT.ReadInt()]; break;
                    case 8: BT.ReadInt(); break; //' FE.Encryption = ReadInt(BT) ' This was never worked out in JCR5, no need to consider any support for it in JCR6
                    case 9: FE.CompressedSize = (int)BT.ReadLong(); break;
                    case 10:
                        Length = BT.ReadInt();
                        //'DebugLog "ALen = "+Length; 
                        FE.Author = BT.ReadString(Length);
                        break;
                    case 11:
                        Length = BT.ReadInt();
                        FE.Notes = BT.ReadString(Length);
                        break;
                    case 12:
                        BT.ReadByte();
                        //'FE.Comment = ReadByte(BT)
                        //'If FE.Comment And (Not LoadComments) MapRemove Ret,Upper(FE.FileName)
                        break;
                    case 200: XTag(BT, Ret, false, JCRFILE); break;
                    case 254:
                        //'JCRD_Print "This JCR file contains a compressed FAT"
                        UnPackedSize = BT.ReadLong();
                        PackedSize = BT.ReadLong();
                        //Unpackedbank = new byte[UnPackedSize]; //CreateBank(UnpackedSize)
                        //PackedBank = new byte[PackedSize]; //= CreateBank(PackedSize)
                        PackedBank = BT.ReadBytes((int)PackedSize);
                        //ReadBank PackedBank, BT, 0, PackedSize
                        //uncompress BankBuf(UnPackedBank), UnpackedSize, BankBuf(PackedBank), PackedSize
                        BT.Close();
                        if (!JCR6.CompDrivers.ContainsKey("zlib"))
                        {
                            JCR6.JERROR = "The file table of this JCR6 file has been packed with the zlib algorithm!";
                            return null;
                        }
                        UnpackedBank = JCR6.CompDrivers["zlib"].Expand(PackedBank, (int)UnPackedSize);
                        BT = new QuickStream(new System.IO.MemoryStream(UnpackedBank)); //CreateBankStream(UnPackedBank); BT = LittleEndianStream(BT)
                        break;
                    default:
                        JCR_JAMERR("JCR_Dir(~q" + JCRFILE + "~q): Unknown FAT Tag (" + Tag + ")", file, "N/A", "Dir");
                        BT.Close();
                        return null;
                }
            } while (Tag != 0xff);
            //Forever
            BT.Close();
            return Ret;
        }

        /*
            The XTag function is need to add extra functionality to JCR Dir and is called by JCR_Dir if these extra functions are actually used.

            (Well, the need of this XTag function lead to JCR5 becoming really really ugly, and that eventually lead to me realizing that if JCR5 would ever be able to do more a more flexible format was needed, hence the birth of JCR6. ;)

        */
        void XTag(QuickStream BT, TJCRDIR JCR, bool LoadComments, string JCRFile)
        {
            string XTC;  // Stands for 'XTag Command'. I'm not promoting any sorts of stuff here(in fact I recommend not to use that) :P
            var Length = BT.ReadInt();
            XTC = BT.ReadString(Length).ToUpper();
            string F;
            TJCRDIR J;
            var D = System.IO.Path.GetDirectoryName(JCRFile).Replace(@"\", "/"); //Replace(ExtractDir(JCRFile),"\","/"); If D And Right(D,1)<>"/" D:+"/"
            if (D != "" && qstr.Right(D, 1) != "/") D += "/";
            //'JCRD_Print "XTag: "+XTC
            switch (XTC)
            {
                case "IMP":
                case "IMPORT":
                    Length = BT.ReadInt();
                    F = BT.ReadString(Length).Replace("\\", "/");
                    if (qstr.Left(F, 1) != "/" && qstr.Mid(F, 2, 1) != ":" && System.IO.File.Exists(D + F)) F = D + F; //' If the required file to import is found in the same directory than hit that file.
                    J = JCR6.Dir(F); //' ,LoadComments)
                    if (J == null)
                    {
                        //'JCRD_Print "WARNING! Could not import "+F+"~n= Report: "+JCRD_DumpError
                    }
                    else
                    {
                        //'JCRD_Print "Importing: "+F
                        //'For Local K$=EachIn MapKeys(J)
                        //'   MapInsert JCR,K,MapValueForKey(J,K)
                        //'   Next
                        JCR.Patch( J);
                    }
                    break;
                default:
                    JCR_JAMERR("WARNING! Unknown XTag in JCR file!", "?", "?", "XTAG");
                    break;
            }
        }

    }
}

/* Original BlitzMax code for reference purposes
 * Ignore the notice about the MPL license.... That license was in place for the BlitzMax code, but as the BlitzMax code is based on a discontinued system I see no longer use to keep that weak-copyleft license up.
 * The driver is from now on zlib licensed;
 */ 

/*
Rem
        JCR6_JCR5Driver.bmx
	(c) 2015 Jeroen Petrus Broks.
	
	This Source Code Form is subject to the terms of the 
	Mozilla Public License, v. 2.0. If a copy of the MPL was not 
	distributed with this file, You can obtain one at 
	http://mozilla.org/MPL/2.0/.
        Version: 15.09.02
End Rem
Rem

	(c) 2015 Jeroen Petrus Broks.
	
	This Source Code Form is subject to the terms of the 
	Mozilla Public License, v. 2.0. If a copy of the MPL was not 
	distributed with this file, You can obtain one at 
	http://mozilla.org/MPL/2.0/.


Version: 15.05.20

End Rem
Strict
Import jcr6.jcr6main
Import jcr6.zlibdriver 

Global StorageName$[] = ["Store","zlib"]


Type DRV_JCR5_FOR_JCR6 Extends DRV_JCRDIR
	Method recognize(fil$)
	Local BT:TStream = ReadFile(fil)
	If Not bt Return
	Local Header$ = ReadString(BT,5); 
	CloseFile bt
	Return Header="JCR5"+Chr(26) 
	End Method
	
	Method name$() Return "JCR5" End Method
	
	Method Dir:TJCRDir(fil$)
	Local Ret:TJCRDir = New TJCRDir
	Local BT:TStream = ReadStream(fil); 
	Local Header$
	Local FE:TJCREntry = New TJCREntry
	Local Tag,Length
	Local PackedBank:TBank,UnpackedBank:TBank
	Local PackedSize:Int,UnPackedSize:Int
	Local JCRD_DumpError$=""
	Local JCRFILE$ = fil
	If Not BT 
		JCR_JAMERR "JCR_Dir(~q"+JCRFIle+"~q): JCR file has not been found!",fil,"N/A","Dir"
		Return Null
		EndIf
	BT = LittleEndianStream(BT)
	If Not BT 
		JCR_JAMERR "JCR_Dir(~q"+JCRFIle+"~q): Forcing into LittleEndian failed!",fil,"N/A","Dir"
		Return Null
		EndIf
	Header = ReadString(BT,5); 
	If Header<>"JCR5"+Chr(26) 
		JCR_JAMERR "JCR_Dir(~q"+JCRFIle+"~q): File given appears not to be a JCR 5 file!",fil,"N/A","Dir"
	CloseStream BT
	Return Null
	EndIf
	Local FatOffSet = ReadLong(BT)
	If FatOffset>StreamSize(BT)
		JCR_JAMERR "JCR_Dir(~q"+JCRFIle+"~q): FAT offset beyond EOF ("+Fatoffset+">"+StreamSize(BT),fil,"N/A","Dir"
		CloseStream BT
		Return Null
		EndIf
	SeekStream BT,FatOffset; 
	Repeat
	tag = ReadByte(BT); 
	'DebugLog "JCR-TAG:"+Tag
	Select Tag
		Case $ff Exit
		Case   0 ReadLong(BT) 'JCRD_Print "JCR File ~q"+JCRFILE+"~q contains "+ReadLong(BT)+" entries~n~n"
		Case   1 FE = New TJCREntry; FE.MainFile=JCRFile;
		Case   2 	Length = ReadInt(BT); 
						'DebugLog "FLen = "+Length; 
						FE.FileName = ReadString(BT,Length); 
						MapInsert Ret.Entries,Upper(FE.FileName),FE; 
						'DebugLog "Found: "+FE.FIleName
		Case   3 FE.Size = ReadLong(BT)
		Case   4 FE.Offset = ReadLong(BT)
		Case   5 ReadInt(BT) ' FE.Time = ReadInt(BT) ' Not supported in JCR6 yet (strictly speaking it's easy to support it, but it's not needed in JCR6's purpose and therefore not supported by this driver)
		Case   6 ReadInt(BT) 'FE.Permissions = ReadInt(BT) ' Permissions not supported in JCR6 yet, and I doubt they ever will be.
		Case   7 FE.Storage = StorageName[ReadInt(BT)]
		Case   8 ReadInt(BT) ' FE.Encryption = ReadInt(BT) ' This was never worked out in JCR5, no need to consider any support for it in JCR6
		Case   9 FE.CompressedSize = ReadLong(BT)
		Case  10 Length = ReadInt(BT)
					 'DebugLog "ALen = "+Length; 
	    	     FE.Author = ReadString(BT,Length)
		Case  11 Length = ReadInt(BT)
		         FE.Notes = ReadString(BT,Length)	    	     
		Case  12 ReadByte(BT) 
		 		 'FE.Comment = ReadByte(BT)
	         	 'If FE.Comment And (Not LoadComments) MapRemove Ret,Upper(FE.FileName)
		Case 200 XTag BT,Ret,False,JCRFile$
		Case 254
			'JCRD_Print "This JCR file contains a compressed FAT"
			UnPackedSize = ReadLong(BT)
			PackedSize = ReadLong(BT)
			Unpackedbank = CreateBank(UnpackedSize)
			PackedBank = CreateBank(PackedSize)
			ReadBank PackedBank,BT,0,PackedSize
			uncompress BankBuf(UnPackedBank),UnpackedSize,BankBuf(PackedBank),PackedSize
			CloseFile BT		
			BT = CreateBankStream(UnPackedBank); BT=LittleEndianStream(BT)
		Default 
			JCR_JAMERR "JCR_Dir(~q"+JCRFIle+"~q): Unknown FAT Tag ("+Tag+")",fil,"N/A","Dir"
			CloseFile BT
			Return Null
		End Select
	Forever
	CloseFile BT
	Return Ret
	End Method

	End Type
	
Private
Rem 
The XTag function is need to add extra functionality to JCR Dir and is called by JCR_Dir if these extra functions are actually used.
End Rem
Function XTag(BT:TStream,JCR:TJCRDir,LoadComments,JCRFile$)
Local XTC$ ' Stands for 'XTag Command'. I'm not promoting any sorts of stuff here (in fact I recommend not to use that) :P
Local Length = ReadInt(BT); 
XTC = ReadString(BT,Length).toUpper()
Local F$
Local J:TJCRDir
Local D$ = Replace(ExtractDir(JCRFile),"\","/"); If D And Right(D,1)<>"/" D:+"/"
'JCRD_Print "XTag: "+XTC
Select XTC 
  Case "IMP","IMPORT"
 		Length = ReadInt(BT)
    F = Replace(ReadString(BT,Length),"\","/")
    If Left(F,1)<>"/" And Mid(F,2,1)<>":" And FileType(D+F)=1 Then F=D+F ' If the required file to import is found in the same directory than hit that file.
		J = JCR_Dir(F) ' ,LoadComments)
		If Not J
			'JCRD_Print "WARNING! Could not import "+F+"~n= Report: "+JCRD_DumpError
			Else
			'JCRD_Print "Importing: "+F
			'For Local K$=EachIn MapKeys(J)
			'	MapInsert JCR,K,MapValueForKey(J,K)
			'	Next
			JCR_AddPatch JCR,J
			EndIf
	Default
		JCR_JAMERR "WARNING! Unknown XTag in JCR file!","?","?","XTAG"
	End Select
End Function
Public
	


New drv_jcr5_for_jcr6
Rem
        JCR6_JCR5Driver.bmx
	(c) 2015 Jeroen Petrus Broks.
	
	This Source Code Form is subject to the terms of the 
	Mozilla Public License, v. 2.0. If a copy of the MPL was not 
	distributed with this file, You can obtain one at 
	http://mozilla.org/MPL/2.0/.
        Version: 15.09.02
End Rem
Rem

	(c) 2015 Jeroen Petrus Broks.
	
	This Source Code Form is subject to the terms of the 
	Mozilla Public License, v. 2.0. If a copy of the MPL was not 
	distributed with this file, You can obtain one at 
	http://mozilla.org/MPL/2.0/.


Version: 15.05.20

End Rem
Strict
Import jcr6.jcr6main
Import jcr6.zlibdriver 

Global StorageName$[] = ["Store","zlib"]


Type DRV_JCR5_FOR_JCR6 Extends DRV_JCRDIR
	Method recognize(fil$)
	Local BT:TStream = ReadFile(fil)
	If Not bt Return
	Local Header$ = ReadString(BT,5); 
	CloseFile bt
	Return Header="JCR5"+Chr(26) 
	End Method
	
	Method name$() Return "JCR5" End Method
	
	Method Dir:TJCRDir(fil$)
	Local Ret:TJCRDir = New TJCRDir
	Local BT:TStream = ReadStream(fil); 
	Local Header$
	Local FE:TJCREntry = New TJCREntry
	Local Tag,Length
	Local PackedBank:TBank,UnpackedBank:TBank
	Local PackedSize:Int,UnPackedSize:Int
	Local JCRD_DumpError$=""
	Local JCRFILE$ = fil
	If Not BT 
		JCR_JAMERR "JCR_Dir(~q"+JCRFIle+"~q): JCR file has not been found!",fil,"N/A","Dir"
		Return Null
		EndIf
	BT = LittleEndianStream(BT)
	If Not BT 
		JCR_JAMERR "JCR_Dir(~q"+JCRFIle+"~q): Forcing into LittleEndian failed!",fil,"N/A","Dir"
		Return Null
		EndIf
	Header = ReadString(BT,5); 
	If Header<>"JCR5"+Chr(26) 
		JCR_JAMERR "JCR_Dir(~q"+JCRFIle+"~q): File given appears not to be a JCR 5 file!",fil,"N/A","Dir"
	CloseStream BT
	Return Null
	EndIf
	Local FatOffSet = ReadLong(BT)
	If FatOffset>StreamSize(BT)
		JCR_JAMERR "JCR_Dir(~q"+JCRFIle+"~q): FAT offset beyond EOF ("+Fatoffset+">"+StreamSize(BT),fil,"N/A","Dir"
		CloseStream BT
		Return Null
		EndIf
	SeekStream BT,FatOffset; 
	Repeat
	tag = ReadByte(BT); 
	'DebugLog "JCR-TAG:"+Tag
	Select Tag
		Case $ff Exit
		Case   0 ReadLong(BT) 'JCRD_Print "JCR File ~q"+JCRFILE+"~q contains "+ReadLong(BT)+" entries~n~n"
		Case   1 FE = New TJCREntry; FE.MainFile=JCRFile;
		Case   2 	Length = ReadInt(BT); 
						'DebugLog "FLen = "+Length; 
						FE.FileName = ReadString(BT,Length); 
						MapInsert Ret.Entries,Upper(FE.FileName),FE; 
						'DebugLog "Found: "+FE.FIleName
		Case   3 FE.Size = ReadLong(BT)
		Case   4 FE.Offset = ReadLong(BT)
		Case   5 ReadInt(BT) ' FE.Time = ReadInt(BT) ' Not supported in JCR6 yet (strictly speaking it's easy to support it, but it's not needed in JCR6's purpose and therefore not supported by this driver)
		Case   6 ReadInt(BT) 'FE.Permissions = ReadInt(BT) ' Permissions not supported in JCR6 yet, and I doubt they ever will be.
		Case   7 FE.Storage = StorageName[ReadInt(BT)]
		Case   8 ReadInt(BT) ' FE.Encryption = ReadInt(BT) ' This was never worked out in JCR5, no need to consider any support for it in JCR6
		Case   9 FE.CompressedSize = ReadLong(BT)
		Case  10 Length = ReadInt(BT)
					 'DebugLog "ALen = "+Length; 
	    	     FE.Author = ReadString(BT,Length)
		Case  11 Length = ReadInt(BT)
		         FE.Notes = ReadString(BT,Length)	    	     
		Case  12 ReadByte(BT) 
		 		 'FE.Comment = ReadByte(BT)
	         	 'If FE.Comment And (Not LoadComments) MapRemove Ret,Upper(FE.FileName)
		Case 200 XTag BT,Ret,False,JCRFile$
		Case 254
			'JCRD_Print "This JCR file contains a compressed FAT"
			UnPackedSize = ReadLong(BT)
			PackedSize = ReadLong(BT)
			Unpackedbank = CreateBank(UnpackedSize)
			PackedBank = CreateBank(PackedSize)
			ReadBank PackedBank,BT,0,PackedSize
			uncompress BankBuf(UnPackedBank),UnpackedSize,BankBuf(PackedBank),PackedSize
			CloseFile BT		
			BT = CreateBankStream(UnPackedBank); BT=LittleEndianStream(BT)
		Default 
			JCR_JAMERR "JCR_Dir(~q"+JCRFIle+"~q): Unknown FAT Tag ("+Tag+")",fil,"N/A","Dir"
			CloseFile BT
			Return Null
		End Select
	Forever
	CloseFile BT
	Return Ret
	End Method

	End Type
	
Private
Rem 
The XTag function is need to add extra functionality to JCR Dir and is called by JCR_Dir if these extra functions are actually used.
End Rem
Function XTag(BT:TStream,JCR:TJCRDir,LoadComments,JCRFile$)
Local XTC$ ' Stands for 'XTag Command'. I'm not promoting any sorts of stuff here (in fact I recommend not to use that) :P
Local Length = ReadInt(BT); 
XTC = ReadString(BT,Length).toUpper()
Local F$
Local J:TJCRDir
Local D$ = Replace(ExtractDir(JCRFile),"\","/"); If D And Right(D,1)<>"/" D:+"/"
'JCRD_Print "XTag: "+XTC
Select XTC 
  Case "IMP","IMPORT"
 		Length = ReadInt(BT)
    F = Replace(ReadString(BT,Length),"\","/")
    If Left(F,1)<>"/" And Mid(F,2,1)<>":" And FileType(D+F)=1 Then F=D+F ' If the required file to import is found in the same directory than hit that file.
		J = JCR_Dir(F) ' ,LoadComments)
		If Not J
			'JCRD_Print "WARNING! Could not import "+F+"~n= Report: "+JCRD_DumpError
			Else
			'JCRD_Print "Importing: "+F
			'For Local K$=EachIn MapKeys(J)
			'	MapInsert JCR,K,MapValueForKey(J,K)
			'	Next
			JCR_AddPatch JCR,J
			EndIf
	Default
		JCR_JAMERR "WARNING! Unknown XTag in JCR file!","?","?","XTAG"
	End Select
End Function
Public
	


New drv_jcr5_for_jcr6
*/

