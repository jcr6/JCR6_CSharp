// Lic:
//         jcr6.cs
// 	(c) 2018 Jeroen Petrus Broks.
// 	
// 	This Source Code Form is subject to the terms of the 
// 	Mozilla Public License, v. 2.0. If a copy of the MPL was not 
// 	distributed with this file, You can obtain one at 
// 	http://mozilla.org/MPL/2.0/.
//         Version: 18.08.24
// EndLic

using TrickyUnits;
using System.Collections.Generic;
using System;
using System.IO;



// required TrickyUnits:
//   = mkl.cs
//   = qstream.cs

namespace UseJCR6
{
    // Basically you should not meddle with this class unless you know what you are doing.
    // The two abstract methods speak for itself. Compress is to make compression possible and Expand for Expansion or Decompression. 
    // The 'realsize' parameter has only been added as in my experience in earlier getups, so algorithms can require to know this prior to decompression.
    abstract class TJCRBASECOMPDRIVER{
        public abstract byte[] Compress(byte[] inputbuffer);
        public abstract byte[] Expand(byte[] inputbuffer, int realsize);
    }
    abstract class TJCRBASEDRIVER{
        public string name = "???";
        public abstract bool Recognize(string file);
        public abstract TJCRDIR Dir(string file);
    }

    class TJCRCStore : TJCRBASECOMPDRIVER{
        public override byte[] Compress(byte[] inputbuffer) { return inputbuffer; }
        public override byte[] Expand(byte[] inputbuffer, int realsize) { return inputbuffer;  }
    }

    class TJCR6DRIVER : TJCRBASEDRIVER
    {
        public TJCR6DRIVER() { name="JCR6";}
        readonly string checkheader = "JCR6"+((char)26);
        public override bool Recognize(string file)
        {
            bool ret = false;
            if (!File.Exists(file)) {
                Console.WriteLine(file + " not found!");
                return false; 
            }
            var bt = QOpen.ReadFile(file);
            ret = bt.Size > 10; // I don't believe a JCR6 file can even get close to that!
            ret = ret && bt.ReadString(checkheader.Length) == checkheader;
            bt.Close();
            //Console.WriteLine(ret);
            return ret;
        }

        public override TJCRDIR Dir(string file)
        {
            var ret = new TJCRDIR();
            bool isJ = false;
            if (!File.Exists(file))
            {
                Console.WriteLine(file + " not found!");
                return null;
            }
            var bt = QOpen.ReadFile(file);
            isJ = bt.Size > 10; // I don't believe a JCR6 file can even get close to that!
            isJ = isJ && bt.ReadString(checkheader.Length) == checkheader;
            if (!isJ) { JCR6.JERROR = file + " is not a JCR6 file!"; bt.Close(); return null; } // This error should NEVER be possible, unless you are using JCR6 NOT the way it was intended to be used.
            ret.FAToffset = bt.ReadInt();
            if (ret.FAToffset <= 0)
            {
                JCR6.JERROR = "Invalid FAT offset. Maybe you are trying to read a JCR6 file that has never been properly finalized";
                bt.Close();
                return null;
            }
            byte TTag = 0;
            string Tag="";
            do
            {
                TTag = bt.ReadByte();
                if (TTag != 255) { Tag = bt.ReadString(); }
                switch (TTag)
                {
                    case 1:
                        ret.CFGstr[Tag] = bt.ReadString();
                        break;
                    case 2:
                        ret.CFGbool[Tag] = bt.ReadByte() == 1;
                        break;
                    case 3:
                        ret.CFGint[Tag] = bt.ReadInt();
                        break;
                    case 255:
                        break;
                    default:
                        JCR6.JERROR = $"Invalid config tag ({TTag}) {file}";
                        bt.Close();
                        return null;

                }


            } while (TTag != 255);
            if (ret.CFGbool.ContainsKey("_CaseSensitive") && ret.CFGbool["_CaseSensitive"])
            {
                JCR6.JERROR = "Case Sensitive dir support was already deprecated and removed from JCR6 before it went to the Go language. It's only obvious that support for this was never implemented in C# in the first place.";
                bt.Close();
                return null;
            }
            bt.Position = ret.FAToffset;
            bool theend = false;
            ret.FATsize = bt.ReadInt();
            ret.FATcsize = bt.ReadInt();
            ret.FATstorage = bt.ReadString();
            //  ret.Entries = map[string]TJCR6Entry{ } // Was needed in Go, but not in C#, as unlike Go, C# DOES support field assign+define
            var fatcbytes = bt.ReadBytes(ret.FATcsize);
            bt.Close();
            //Console.WriteLine(ret);
            if (!JCR6.CompDrivers.ContainsKey(ret.FATstorage))
            {
                JCR6.JERROR = "The File Table of file '"+file+"' was packed with the '" + ret.FATstorage + "' algorithm, but unfortunately I don't have drivers loaded for that one.";
                return null;
            }
            var fatbytes = JCR6.CompDrivers[ret.FATstorage].Expand(fatcbytes, ret.FATsize);
            bt = QOpen.StreamFromBytes(fatbytes, QOpen.LittleEndian); // Little Endian is the default, but I need to make sure as JCR6 REQUIRES Little Endian for its directory structures.
            while ((!bt.EOF) && (!theend)) {
                var mtag = bt.ReadByte();
                var ppp = bt.Position;
                switch (mtag) {
                    case 0xff:
                        theend = true;
                        break;
                    case 0x01:
                        var tag= bt.ReadString().ToUpper(); //strings.ToUpper(qff.ReadString(btf)); 
                        switch (tag) {
                            case "FILE":
                                var newentry = new TJCREntry();
                                newentry.MainFile = file;
                            /* Not needed in C#
                             * newentry.Datastring = map[string]string{}
                             * newentry.Dataint = map[string]int{}
                             * newentry.Databool = map[string]bool{}
                             */
                                var ftag= bt.ReadByte();
                                while( ftag != 255 ){
                                    //chats("FILE TAG %d", ftag)
                                    switch (ftag) {
                                        case 1:
                                            var k = bt.ReadString();
                                            var v = bt.ReadString();
                                            newentry.datastring[k] = v;
                                            break;
                                        case 2:
                                            var kb = bt.ReadString();
                                            var vb = bt.ReadBoolean();
                                            newentry.databool[kb] = vb;
                                            break;
                                        case 3:
                                            var ki = bt.ReadString();
                                            var vi = bt.ReadInt();
                                            newentry.dataint[ki] = vi;
                                            break;
                                        case 255:
                                            break;
                                        default:
                                            // p,_:= btf.Seek(0, 1)
                                            JCR6.JERROR = $"Illegal tag in FILE part {ftag} on fatpos {bt.Position}";
                                            bt.Close();
                                            return null;
                                    }
                                    ftag = bt.ReadByte();
                                }
                                var centry = newentry.Entry.ToUpper();
                                ret.Entries[centry] = newentry;
                                break;
                            case "COMMENT":
                                var commentname = bt.ReadString();
                                ret.Comments[commentname] = bt.ReadString();
                                break;
                            case "IMPORT": case "REQUIRE":
                                //if impdebug {
                                //    fmt.Printf("%s request from %s\n", tag, file)
                                //                    }
                                // Now we're playing with power. Tha ability of 
                                // JCR6 to automatically patch other files into 
                                // one resource
                                var deptag = bt.ReadByte();
                                string depk;
                                string depv;
                                var depm = new Dictionary<string, string>();
                                while( deptag != 255 ){
                                    depk = bt.ReadString();
                                    depv = bt.ReadString();
                                    depm[depk] = depv;
                                    deptag = bt.ReadByte();
                                }
                                var depfile = depm["File"];
                                //depsig   := depm["Signature"]
                                var deppatha = depm["AllowPath"] == "TRUE";
                                var depcall = "";
                                // var depgetpaths[2][] string
                                List<string>[] depgetpaths= new List<string>[2];
                                depgetpaths[0] = new List<string>();
                                depgetpaths[1] = new List<string>();
                                var owndir = Path.GetDirectoryName(file);
                                int deppath = 0;
                                /*if impdebug{
                                    fmt.Printf("= Wanted file: %s\n",depfile)
                                       fmt.Printf("= Allow Path:  %d\n",deppatha)
                                       fmt.Printf("= ValConv:     %d\n",deppath)
                                       fmt.Printf("= Prio entnum  %d\n",len(ret.Entries))
                                }*/
                                if (deppatha) {
                                    deppath = 1;
                                }
                                if (owndir != "") {owndir += "/";}
                                depgetpaths[0].Add(owndir);
                                depgetpaths[1].Add(owndir);
                                // TODO: JCR6: depgetpaths[1] = append(depgetpaths[1], dirry.Dirry("$AppData$/JCR6/Dependencies/") )
                                if (qstr.Left(depfile,1)!="/" && qstr.Left(depfile,2)!=":") {
                                    foreach(string depdir in depgetpaths[deppath]) //for _,depdir:=range depgetpaths[deppath]
                                    {
                                        if ((depcall=="") && File.Exists(depdir+depfile) ) {
                                            depcall = depdir + depfile;
                                        } /*else if (depcall=="" && impdebug ){
                                            if !qff.Exists(depdir+depfile) {
                                                fmt.Printf("It seems %s doesn't exist!!\n",depdir+depfile)
                                            }*/
                                    }   
                                } else {
                                    if (File.Exists(depfile)) {
                                        depcall = depfile;
                                    }
                                }
                                if (depcall!="") {
                                    ret.PatchFile(depcall);
                                    if (JCR6.JERROR!="" && tag=="REQUIRE") {//((!ret.PatchFile(depcall)) && tag=="REQUIRE"){
                                        JCR6.JERROR = "Required JCR6 addon file (" + depcall + ") could not imported! Importer reported: "+JCR6.JERROR; //,fil,"N/A","JCR 6 Driver: Dir()")
                                        bt.Close();
                                        return null;
                                    } else if (tag=="REQUIRE") {
                                        JCR6.JERROR = "Required JCR6 addon file (" + depcall + ") could not found!"; //,fil,"N/A","JCR 6 Driver: Dir()")
                                        bt.Close();
                                        return null;
                                    }
                                } /*else if impdebug {
                                    fmt.Printf("Importing %s failed!", depfile);
                                    fmt.Printf("Request:    %s", tag);
                                }*/
                                break;
                        }
                        break;
                    default:
                        JCR6.JERROR = $"Unknown main tag {mtag}";
                        bt.Close();
                        return null;
                }
            }
            bt.Close();
            return ret; // Actual reader comes later.
        }
    }





    class TJCREntry
    {
        public string MainFile = "";
        public Dictionary<string, string> datastring = new Dictionary<string, string>();
        public Dictionary<string, int> dataint = new Dictionary<string, int>();
        public Dictionary<string, bool> databool = new Dictionary<string, bool>();
        public string Entry
        {
            get { return datastring["__Entry"]; }
            set { datastring["__Entry"] = value; }
        }
        public int Size
        {
            get
            {
                return dataint["__Size"];
            }
            set
            {
                dataint["__Size"] = value;
            }
        }
        public int CompressedSize
        {
            get { return dataint["__CSize"]; }
            set { dataint["__CSize"] = value; }
        }
        public int Offset
        {
            get { return dataint["__Offset"]; }
            set { dataint["__Offset"] = value; }
        }
        public string Storage
        {
            get { return datastring["__Storage"]; }
            set { datastring["__Storage"] = value; }
        }
        public string Author
        {
            get { return datastring["__Author"]; }
            set { datastring["__Author"] = value; }

        }
        public string Notes
        {
            get { return datastring["__Notes"]; }
            set { datastring["__Notes"] = value; }

        }






    }


    class TJCRDIR
    {
        public int FAToffset;
        public int FATsize;
        public int FATcsize;
        public string FATstorage;
        public Dictionary<string, string> CFGstr = new Dictionary<string, string>();
        public Dictionary<string, bool> CFGbool = new Dictionary<string, bool>();
        public Dictionary<string, int> CFGint = new Dictionary<string, int>();
        public Dictionary<string, TJCREntry> Entries = new Dictionary<string, TJCREntry>();
        public Dictionary<string, string> Comments = new Dictionary<string, string>();

        public void PatchFile(string file)
        {
            JCR6.dCHAT($"Patching: {file}");
            var p=JCR6.Dir(file);
            if (p==null) {
                JCR6.JERROR = ("PATCH ERROR:" + JCR6.JERROR);
                return;
            }
            Patch(p);
        }

        public void Patch(TJCRDIR pdata)
        {
            foreach (string k in pdata.CFGstr.Keys) { this.CFGstr[k] = pdata.CFGstr[k]; }
            foreach (string k in pdata.CFGint.Keys) { this.CFGint[k] = pdata.CFGint[k]; }
            foreach (string k in pdata.CFGbool.Keys) { this.CFGbool[k] = pdata.CFGbool[k]; }
            foreach (string k in pdata.Entries.Keys) { this.Entries[k] = pdata.Entries[k]; }
            foreach (string k in pdata.Comments.Keys) { this.Comments[k] = pdata.Comments[k]; }

        }
    }

    class JCR6
    {
        public const bool dbg = true;

        // Better leave these alone all the time!
        // They are basically only used for drivier initiation, and since other classes must be able to do that, they are (for now) public.
        public static Dictionary<string, TJCRBASECOMPDRIVER> CompDrivers = new Dictionary<string, TJCRBASECOMPDRIVER>();
        public static Dictionary<string, TJCRBASEDRIVER> FileDrivers = new Dictionary<string, TJCRBASEDRIVER>();

        public static void dCHAT(string s)
        {
            if (dbg) { Console.WriteLine(s); }
        }

        // Contains error message if last JCR6 error went wrong
        static public string JERROR = "";

        static JCR6()
        {
            MKL.Version("JCR6 - jcr6.cs", "18.08.24");
            MKL.Lic("JCR6 - jcr6.cs", "Mozilla Public License 2.0");
            CompDrivers["Store"] = new TJCRCStore();
            FileDrivers["JCR6"] = new TJCR6DRIVER();
        }

        static public string Recognize(string file)
        {
            var ret = "NONE";
            JERROR = "";
            foreach (string k in FileDrivers.Keys)
            { // k, v := range JCR6Drivers        
              // chat("Is " + file + " of type " + k + "?")            
              //fmt.Printf("key[%s] value[%s]\n", k, v)
                dCHAT("Testing format: " + k);
                var v = FileDrivers[k];
                if (v.Recognize(file))
                {
                    ret = k;
                }
            }
            return ret;
        }

        static public TJCRDIR Dir(string file)
        {
            var t = Recognize(file);
            if (t == "NONE")
            {
                JERROR = "\"" + file + "\" has not been recognized as any kind of file JCR6 supports";
                return null;
            }
            return FileDrivers[t].Dir(file);
        }

    }
}
