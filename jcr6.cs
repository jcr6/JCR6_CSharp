// Lic:
//         jcr6.cs
// 	(c) 2018 Jeroen Petrus Broks.
// 	
// 	This Source Code Form is subject to the terms of the 
// 	Mozilla Public License, v. 2.0. If a copy of the MPL was not 
// 	distributed with this file, You can obtain one at 
// 	http://mozilla.org/MPL/2.0/.
//         Version: 18.09.30
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
    abstract class TJCRBASECOMPDRIVER
    {
        public abstract byte[] Compress(byte[] inputbuffer);
        public abstract byte[] Expand(byte[] inputbuffer, int realsize);
    }
    abstract class TJCRBASEDRIVER
    {
        public string name = "???";
        public abstract bool Recognize(string file);
        public abstract TJCRDIR Dir(string file);
    }

    class TJCRCStore : TJCRBASECOMPDRIVER
    {
        public override byte[] Compress(byte[] inputbuffer) { return inputbuffer; }
        public override byte[] Expand(byte[] inputbuffer, int realsize) { return inputbuffer; }
    }

    class TJCR6DRIVER : TJCRBASEDRIVER
    {
        public TJCR6DRIVER() { name = "JCR6"; }
        readonly string checkheader = "JCR6" + ((char)26);
        public override bool Recognize(string file)
        {
            bool ret = false;
            if (!File.Exists(file))
            {
                JCR6.dCHAT(file + " not found!");
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
            string Tag = "";
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
                JCR6.JERROR = "The File Table of file '" + file + "' was packed with the '" + ret.FATstorage + "' algorithm, but unfortunately I don't have drivers loaded for that one.";
                return null;
            }
            var fatbytes = JCR6.CompDrivers[ret.FATstorage].Expand(fatcbytes, ret.FATsize);
            bt = QOpen.StreamFromBytes(fatbytes, QOpen.LittleEndian); // Little Endian is the default, but I need to make sure as JCR6 REQUIRES Little Endian for its directory structures.
            while ((!bt.EOF) && (!theend))
            {
                var mtag = bt.ReadByte();
                var ppp = bt.Position;
                switch (mtag)
                {
                    case 0xff:
                        theend = true;
                        break;
                    case 0x01:
                        var tag = bt.ReadString().ToUpper(); //strings.ToUpper(qff.ReadString(btf)); 
                        switch (tag)
                        {
                            case "FILE":
                                var newentry = new TJCREntry();
                                newentry.MainFile = file;
                                /* Not needed in C#
                                 * newentry.Datastring = map[string]string{}
                                 * newentry.Dataint = map[string]int{}
                                 * newentry.Databool = map[string]bool{}
                                 */
                                var ftag = bt.ReadByte();
                                while (ftag != 255)
                                {
                                    //chats("FILE TAG %d", ftag)
                                    switch (ftag)
                                    {
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
                            case "IMPORT":
                            case "REQUIRE":
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
                                while (deptag != 255)
                                {
                                    depk = bt.ReadString();
                                    depv = bt.ReadString();
                                    depm[depk] = depv;
                                    deptag = bt.ReadByte();
                                }
                                var depfile = depm["File"];
                                //depsig   := depm["Signature"]
                                var deppatha = depm.ContainsKey("AllowPath") && depm["AllowPath"] == "TRUE";
                                var depcall = "";
                                // var depgetpaths[2][] string
                                List<string>[] depgetpaths = new List<string>[2];
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
                                if (deppatha)
                                {
                                    deppath = 1;
                                }
                                if (owndir != "") { owndir += "/"; }
                                depgetpaths[0].Add(owndir);
                                depgetpaths[1].Add(owndir);
                                // TODO: JCR6: depgetpaths[1] = append(depgetpaths[1], dirry.Dirry("$AppData$/JCR6/Dependencies/") )
                                if (qstr.Left(depfile, 1) != "/" && qstr.Left(depfile, 2) != ":")
                                {
                                    foreach (string depdir in depgetpaths[deppath]) //for _,depdir:=range depgetpaths[deppath]
                                    {
                                        if ((depcall == "") && File.Exists(depdir + depfile))
                                        {
                                            depcall = depdir + depfile;
                                        } /*else if (depcall=="" && impdebug ){
                                            if !qff.Exists(depdir+depfile) {
                                                fmt.Printf("It seems %s doesn't exist!!\n",depdir+depfile)
                                            }*/
                                    }
                                }
                                else
                                {
                                    if (File.Exists(depfile))
                                    {
                                        depcall = depfile;
                                    }
                                }
                                if (depcall != "")
                                {
                                    ret.PatchFile(depcall);
                                    if (JCR6.JERROR != "" && tag == "REQUIRE")
                                    {//((!ret.PatchFile(depcall)) && tag=="REQUIRE"){
                                        JCR6.JERROR = "Required JCR6 addon file (" + depcall + ") could not imported! Importer reported: " + JCR6.JERROR; //,fil,"N/A","JCR 6 Driver: Dir()")
                                        bt.Close();
                                        return null;
                                    }
                                    else if (tag == "REQUIRE")
                                    {
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
                        JCR6.JERROR = $"Unknown main tag {mtag}, at file table position ";
                        JCR6.JERROR += bt.Position;
                        bt.Close();
                        return null;
                }
            }
            bt.Close();
            return ret; // Actual reader comes later.
        }
    }




    /// <summary>
    /// This class is used to store all information about an entry living inside a JCR6 file.
    /// </summary>
    class TJCREntry
    {
        /// <summary>
        /// Will contain the filename of the resource file this entry lives in (this is most of all important for multi-file resources).
        /// </summary>
        public string MainFile = "";
        /// <summary>
        /// Contains string data about the entry. The nice part is that you can add fields to it to your liking if you want, but please keep in mind that all fields prefixed with two underscores are considered to be part of JCR6 itself and should not be used if you don't want conflicts with future versions.
        /// </summary>
        public Dictionary<string, string> datastring = new Dictionary<string, string>();
        /// <summary>
        /// Contains integer data about the entry. The nice part is that you can add fields to it to your liking if you want, but please keep in mind that all fields prefixed with two underscores are considered to be part of JCR6 itself and should not be used if you don't want conflicts with future versions.
        /// </summary>
        public Dictionary<string, int> dataint = new Dictionary<string, int>();
        /// <summary>
        /// Contains boolean data about the entry. The nice part is that you can add fields to it to your liking if you want, but please keep in mind that all fields prefixed with two underscores are considered to be part of JCR6 itself and should not be used if you don't want conflicts with future versions.
        /// </summary>
        public Dictionary<string, bool> databool = new Dictionary<string, bool>();
        /// <summary>
        /// Contains the true name of the entry. Also with its regular upper and lower case settings.
        /// </summary>
        public string Entry
        {
            get { return datastring["__Entry"]; }
            set { datastring["__Entry"] = value; }
        }
        /// <summary>
        /// Gets or sets the size of an entry (setting should only be done by JCR6 write classes itself) without compression.
        /// </summary>
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
        /// <summary>
        /// Gets or sets the compressed size of an entry (setting should only be done by JCR6 write classes itself)
        /// </summary>
        public int CompressedSize
        {
            get { return dataint["__CSize"]; }
            set { dataint["__CSize"] = value; }
        }
        /// <summary>
        /// Contains a string presentation of the ratio in %.
        /// Please note, JCR6 doesn't tell you how much smaller it is, but how to how much % the file has been reduced.
        /// </summary>
        public string Ratio{ get {
                if (Size <= 0) return "N/A";
                return $"{Math.Floor((double)(((double)CompressedSize / Size) * (double)100))}%";
            }}

        /// <summary>
        /// Gets or sets the offset of an entry inside its mainfile. (setting should only be done by JCR6 write classes itself)
        /// </summary>
        public int Offset
        {
            get { return dataint["__Offset"]; }
            set { dataint["__Offset"] = value; }
        }
        /// <summary>
        /// Which compression algorithm has been used to compress this entry?
        /// </summary>
        public string Storage
        {
            get { return datastring["__Storage"]; }
            set { datastring["__Storage"] = value; }
        }
        /// <summary>
        /// Gets or sets the author of this entry. This can be handy when you make use of 3rd party assets in your projects.
        /// </summary>
        public string Author
        {
            get { if (!datastring.ContainsKey("__Author")) return "";
				return datastring["__Author"]; }
            set { datastring["__Author"] = value; }

        }
        /// <summary>
        /// Gets or sets the notes of an entry. I myself often use these to include copyright and license notices.
        /// </summary>
        public string Notes
        {
            get {
                if (!datastring.ContainsKey("__Notes")) return "";
                return datastring["__Notes"]; }
            set { datastring["__Notes"] = value; }

        }






    }


    /// <summary>
    /// This class is used to store all information of the directory inside the JCR6 resource.
    /// It also contains many handy methods to help you work with JCR6 resources.
    /// Although strictly speaking writing is possible, it's best to consider everything within this class as "read-only".
    /// </summary>
    class TJCRDIR
    {
        public int FAToffset;
        public int FATsize;
        public int FATcsize;
        public string FATstorage;
        public Dictionary<string, string> CFGstr = new Dictionary<string, string>();
        public Dictionary<string, bool> CFGbool = new Dictionary<string, bool>();
        public Dictionary<string, int> CFGint = new Dictionary<string, int>();
        public SortedDictionary<string, TJCREntry> Entries = new SortedDictionary<string, TJCREntry>();
        public SortedDictionary<string, string> Comments = new SortedDictionary<string, string>();

        /// <summary>
        /// Count the entries.
        /// </summary>
        /// <value>the number of entries inside this JCR6 resource.</value>
        public int CountEntries {
            get{
                var ret = 0;
                foreach (string str in Entries.Keys) ret++;
                return ret;
            }
        }


        /// <summary>
        /// Checks if an entry exists
        /// </summary>
        /// <remarks>Please remember that JCR6 is case INSENSITIVE!!!</remarks>
        /// <returns>True if the entry does exist and false otherwise</returns>
        /// <param name="entry">Entry.</param>
        public bool Exists(string entry) => Entries.ContainsKey(entry.ToUpper());

        /// <summary>
        /// Patches a file into a JCR6 resource, if JCR6 can recognise it as a resource.
        /// </summary>
        /// <param name="file">File.</param>
        public void PatchFile(string file)
        {
            JCR6.dCHAT($"Patching: {file}");
            var p = JCR6.Dir(file);
            if (p == null)
            {
                JCR6.JERROR = ("PATCH ERROR:" + JCR6.JERROR);
                return;
            }
            Patch(p);
        }
        /// <summary>
        /// Patches another resource into this resource.
        /// </summary>
        /// <param name="pdata">Pdata.</param>
        public void Patch(TJCRDIR pdata)
        {
            foreach (string k in pdata.CFGstr.Keys) { this.CFGstr[k] = pdata.CFGstr[k]; }
            foreach (string k in pdata.CFGint.Keys) { this.CFGint[k] = pdata.CFGint[k]; }
            foreach (string k in pdata.CFGbool.Keys) { this.CFGbool[k] = pdata.CFGbool[k]; }
            foreach (string k in pdata.Entries.Keys) { this.Entries[k] = pdata.Entries[k]; }
            foreach (string k in pdata.Comments.Keys) { this.Comments[k] = pdata.Comments[k]; }

        }

        /// <summary>
        /// Reads the content of a JCR6 resource entry.
        /// </summary>
        /// <returns>The contents of the entry in a byte array</returns>
        /// <param name="entry">The entry name (case insensitive)</param>
        public byte[] JCR_B(string entry)
        {
            JCR6.JERROR = "";
            var ce = entry.ToUpper();
            if (!Entries.ContainsKey(ce)) { JCR6.JERROR = "Resource does not appear to contain an entry called: " + entry; return null; }
            var e = Entries[ce];
            byte[] cbuf;
            byte[] ubuf;
            var bt = QOpen.ReadFile(e.MainFile);
            bt.Position = e.Offset;
            cbuf = bt.ReadBytes(e.CompressedSize);
            bt.Close();
            if (!JCR6.CompDrivers.ContainsKey(e.Storage)) { JCR6.JERROR = "Entry \"" + entry + "\" has been packed with the unsupported \"" + e.Storage + "\" algorithm"; return null; }
            ubuf = JCR6.CompDrivers[e.Storage].Expand(cbuf, e.Size);
            return ubuf;
        }


        /// <summary>
        /// Loads a stringmap (or Dictionary&lt;string,string&gt;) from a JCR file,in simple form (I rarely used this, but hey,it's there :P )
        /// </summary>
        /// <returns>The string map.</returns>
        /// <param name="filename">Name of the entry in this JCR6 resource.</param>
        public Dictionary<string, string> LoadStringMapSimple(string filename)
        {
            var bt = ReadFile(filename,QOpen.LittleEndian);
            bt.Position = 0;
            var ret = new Dictionary<string, string>();
            //Console.WriteLine($"LSM START! {bt.Position}/{bt.Size}");
            while (!bt.EOF)
            {
                //Console.WriteLine($"LSM: {bt.Position}/{bt.Size}");
                var lkey = bt.ReadInt(); //Console.WriteLine(lkey);
                var key = bt.ReadString(lkey); //Console.WriteLine(key);
                var lvalue = bt.ReadInt(); //Console.WriteLine(lvalue);
                var value = bt.ReadString(lvalue); //Console.WriteLine(value);
                //Console.WriteLine($"LSM: {key} = {value}.");
                ret[key] = value;
            }
            //Console.WriteLine("Loaded LSM");
            bt.Close();
            return ret;
        }

        /// <summary>
        /// Loads the stringmap. In most of my works this variant has been used.
        /// </summary>
        /// <returns>The string map.</returns>
        /// <param name="entry">Entry in JCR6 resource.</param>
        public Dictionary<string, string> LoadStringMap(string entry){
            var bt = ReadFile(entry, QOpen.LittleEndian);
            if (bt == null) return null;
            var ret = new Dictionary<string, string>();
            string k;
            string v;
            while (true){
                var tag = bt.ReadByte();
                switch(tag){
                    case 1:
                        k = bt.ReadString();
                        v = bt.ReadString();
                        ret[k] = v;
                        break;
                    case 255:
                        bt.Close();
                        return ret;
                    default:
                        bt.Close();
                        JCR6.JERROR = $"Invalid tag in stringmap {tag}";
                        return null;
                }
            }
        }


        /// <summary>
        /// Reads the content of a JCR6 resource entry.
        /// </summary>
        /// <returns>The contents of the entry as a string</returns>
        /// <param name="entry">The entry name (case insensitive)</param>
        public string LoadString(string entry)
        {
            var buf = JCR_B(entry);
            if (buf == null) return "";
            return System.Text.Encoding.Default.GetString(buf);
        }

        /// <summary>
        /// Opens an entry inside a JCR6 file as a standard default memory stream for the regular C# routines to read.
        /// </summary>
        /// <returns>The memory stream.</returns>
        /// <param name="entry">The entry name (case insensitive)</param>
        public MemoryStream AsMemoryStream(string entry)
        {
            var buf = JCR_B(entry);
            if (buf == null) return null;
            return new MemoryStream(buf);
        }

        /// <summary>
        /// Opens the file as a QuickStream. See the qstream.cs class file for more information about that.
        /// </summary>
        /// <returns>The QuickStream</returns>
        /// <param name="entry">>The entry name (case insensitive)</param>
        /// <param name="endian">QOpen.LittleEndian or QOpen.BigEndian for automatic endian conversion, if set to 0 it will just read endians by the way the CPU does it.</param>
        public QuickStream ReadFile(string entry, byte endian = QOpen.LittleEndian)
        {
            var buf = JCR_B(entry);
            if (buf == null) return null;
            return QOpen.StreamFromBytes(buf, endian);
        }

        /// <summary>
        /// Reads the content of a JCR6 resource entry
        /// </summary>
        /// <returns>All lines of the JCR6 entry (assuming it's a text file). A (limited) support is there for recognition of DOS-text files (as used by Windows) and Unix text files (as used by Mac and Linux).</returns>
        /// <param name="entry">The entry name (case insensitive)</param>
        public string[] ReadLines(string entry)
        {
            var s = LoadString(entry);
            string[] eol = new string[3]; eol[0] = "\r\n"; eol[1] = "\n\r"; eol[2] = "\n";
            foreach (string eoln in eol)
            {
                if (s.Contains(eoln))
                {
                    var sp = new System.Text.RegularExpressions.Regex(eoln);
                    return sp.Split(s);
                }
            }
            return new[] { s }; // if all one line, just dump it as one line!
        }

    }

    class TJCRCreateStream
    {
        readonly QuickStream stream;
        readonly string storage;
        readonly string author;
        readonly string notes;
        readonly string entry;
        readonly MemoryStream memstream;
        readonly TJCRCreate parent;
        //public byte[] buffer;


        public TJCRCreateStream(TJCRCreate theparent, string theentry, string thestorage, string theauthor = "", string thenotes = "", byte Endian = QOpen.LittleEndian)
        {
            entry = theentry;
            storage = thestorage;
            author = theauthor;
            notes = thenotes;
            memstream = new MemoryStream();
            stream = new QuickStream(memstream,Endian);
            parent = theparent;
            parent.OpenEntries[this] = theentry;
        }

        public void WriteByte(byte b) => stream.WriteByte(b);
        public void WriteInt(int i) => stream.WriteInt(i);
        public void WriteString(string s, bool raw = false) => stream.WriteString(s, raw);
        public void WriteLong(long i) => stream.WriteLong(i);
        public void WriteBytes(byte[] b, bool ce = false) => stream.WriteBytes(b, ce);
        public void Close()
        {
            var rawbuff = memstream.ToArray();
            var cmpbuff = JCR6.CompDrivers[storage].Compress(rawbuff);
            var astorage = storage;
            // TODO: "BRUTE" support entry closure
            if (storage != "Store" && rawbuff.Length <= cmpbuff.Length) { cmpbuff = rawbuff; astorage = "Store"; }
            var NEntry = new TJCREntry();
            NEntry.Entry = entry;
            NEntry.Size = rawbuff.Length;
            NEntry.CompressedSize = cmpbuff.Length;
            NEntry.Offset = (int)parent.mystream.Position;
            NEntry.Author = author;
            NEntry.Notes = notes;
            NEntry.Storage = astorage;
            NEntry.datastring["__JCR6FOR"] = "C#";
            parent.Entries[entry.ToUpper()] = NEntry;
            parent.mystream.WriteBytes(cmpbuff);
            parent.OpenEntries.Remove(this);
            stream.Close();
        }
    }



    class TJCRCreate
    {

        public QuickStream mystream;
        public Dictionary<TJCRCreateStream, string> OpenEntries = new Dictionary<TJCRCreateStream, string>();
        public Dictionary<string, TJCREntry> Entries = new Dictionary<string, TJCREntry>();
        Dictionary<string, string> Comments = new Dictionary<string, string>();

        readonly string FileTableStorage;
        readonly int ftoffint;
        readonly string MainFile;

        bool closed = false;

        /// <summary>
        /// Creates a stream for an entry you want to add to the JCR6 resource. 
        /// </summary>
        /// <remarks>JCR6 uses memory streams for this, so whatever you add to this, keep the limitations of your RAM in mind.</remarks>
        /// <returns>The entry's stream.</returns>
        /// <param name="Entry">Entry name.</param>
        /// <param name="Storage">Storage/compression algorithm.</param>
        /// <param name="Author">Author name.</param>
        /// <param name="Notes">Notes.</param>
        /// <param name="Endian">Endian setting.</param>
        public TJCRCreateStream NewEntry(string Entry, string Storage="", string Author = "", string Notes = "", byte Endian = QOpen.LittleEndian)
        {

            if (!JCR6.CompDrivers.ContainsKey(Storage)) { JCR6.JERROR = $"I cannot compress with unknown storage method \"{Storage}\""; return null; }
            return new TJCRCreateStream(this, Entry, Storage, Author, Notes,Endian);
        }

        /// <summary>
        /// Saves a stringmap (Dictionary&lt;string,string&gt;&lt;/string&gt;) into a JCR6 file as an entry
        /// </summary>
        /// <param name="data">The stringmap in question</param>
        /// <param name="Entry">Entry name.</param>
        /// <param name="Storage">Storage algorith.</param>
        /// <param name="Author">Author name.</param>
        /// <param name="Notes">Notes.</param>
        public void NewStringMap(Dictionary<string,string> data,string Entry,string Storage="", string Author="",string Notes=""){
            var bt = NewEntry(Entry, Storage, Author, Notes, QOpen.LittleEndian);
            foreach(string k in data.Keys){
                bt.WriteByte(1);
                bt.WriteString(k);
                bt.WriteString(data[k]);
            }
            bt.WriteByte(255);
            bt.Close();
        }


        /// <summary>
        /// Creates an "alias" of a JCR6 entry. In JCR6 an "Alias" is just a second entry poiting to the same data as another entry. With advanced JCR6 usage, this can sometimes make your life a lot easier.
        /// </summary>
        /// <remarks>If the target already exists, JCR6 will just override the reference, but NOT the data, so that can lead to unaccesible data in your JCR6 file. Second, JCR6 is NOT able to tell which entry is the "orginal" and which is the "target". For JCR6 they are just two separate entries and it really doesn't care that all their pointer data is the same.
        /// </remarks>
        /// <param name="original">Original entry.</param>
        /// <param name="target">Target entry.</param>
        public void Alias(string original, string target)
        {
            if (!Entries.ContainsKey(original.ToUpper())) { JCR6.JERROR = $"Cannot alias {original}. Entry not found!"; return; }
            var OEntry = Entries[original.ToUpper()];
            var TEntry = new TJCREntry();
            TEntry.Entry = target;
            TEntry.MainFile = MainFile;
            foreach (string k in OEntry.datastring.Keys) { TEntry.datastring[k] = OEntry.datastring[k]; }
            foreach (string k in OEntry.dataint.Keys) { TEntry.dataint[k] = OEntry.dataint[k]; }
            foreach (string k in OEntry.databool.Keys) { TEntry.databool[k] = OEntry.databool[k]; }
        }



        public void CloseAllEntries()
        {
            List<TJCRCreateStream> tl = new List<TJCRCreateStream>();
            foreach (TJCRCreateStream s in OpenEntries.Keys) { tl.Add(s); }
            foreach (TJCRCreateStream s in tl) { s.Close(); }
        }

        /// <summary>
        /// Closes and finalizes JCR6 file so it's ready for usage.
        /// All Streams attacked to this JCR6 creation instance will automatically be closed and added according to their settings respectively.
        /// </summary>
        public void Close()
        {
            if (closed) return;
            CloseAllEntries();
            var whereami = mystream.Position;
            mystream.Position = ftoffint;
            mystream.WriteInt((int)whereami);
            mystream.Position = whereami;
            // TODO: finalizing JCR6 file
            var ms = new MemoryStream();
            var bt = new QuickStream(ms);
            foreach (string k in Comments.Keys)
            {
                bt.WriteByte(1);
                bt.WriteString("COMMENT");
                bt.WriteString(k);
                bt.WriteString(Comments[k]);
            }
            foreach(string k in Entries.Keys){
                bt.WriteByte(1);
                bt.WriteString("FILE");
                var E = Entries[k];
                foreach (string k2 in E.datastring.Keys) { bt.WriteByte(1); bt.WriteString(k2); bt.WriteString(E.datastring[k2]); }
                foreach (string k2 in E.databool.Keys) { bt.WriteByte(2); bt.WriteString(k2); bt.WriteBool(E.databool[k2]); }
                foreach (string k2 in E.dataint.Keys) { bt.WriteByte(3); bt.WriteString(k2); bt.WriteInt(E.dataint[k2]); }
                bt.WriteByte(255);
            }
            bt.WriteByte(255);
            // TODO: "BRUTE" support file table storage
            //Console.WriteLine($"Write on {whereami}/{mystream.Position}");
            var unpacked = ms.ToArray();
            var fts = FileTableStorage;
            var packed = JCR6.CompDrivers[FileTableStorage].Compress(unpacked);
            if (fts != "Store" || packed.Length >= unpacked.Length) { packed = unpacked; fts = "Store"; }
            bt.Close();
            mystream.WriteInt(unpacked.Length);
            mystream.WriteInt(packed.Length);
            mystream.WriteString(fts);
            mystream.WriteBytes(packed);
            mystream.Close();
            closed = true;
        }

        public void AddString(string mystring, string Entry, string Storage="Store", string Author = "", string Notes = "")
        {
            var s = NewEntry(Entry, Storage, Author, Notes);
            s.WriteString(mystring, true);
            s.Close();
        }

        public void AddBytes(byte[] mybuffer, string Entry, string Storage = "Store", string Author = "", string Notes = "")
        {
            var s = NewEntry(Entry, Storage, Author, Notes);
            s.WriteBytes(mybuffer);
            s.Close();
        }


        public void AddFile(string OriginalFile, string Entry, string Storage="Store", string Author = "", string Notes = "")
        {
            var rs = QOpen.ReadFile(OriginalFile);
            var buf = rs.ReadBytes((int)rs.Size);
            rs.Close();
            var ws = NewEntry(Entry, Storage, Author, Notes);
            ws.WriteBytes(buf);
            ws.Close();
        }

        public void AddComment(string name, string comment)
        {
            Comments[name] = comment;
        }


        public TJCRCreate(string OutputFile, string FTStorage = "Store", string Signature = "")
        {
            JCR6.JERROR = "";
            // TODO: Make "Brute" always pass if asked for it in FT storage.
            if (!JCR6.CompDrivers.ContainsKey(FTStorage)) { JCR6.JERROR = $"Storage method {FTStorage} not present!"; return; }
            mystream = QOpen.WriteFile(OutputFile, QOpen.LittleEndian);
            FileTableStorage = FTStorage;
            mystream.WriteString("JCR6"+(char)26,true);
            ftoffint = (int)mystream.Position;
            MainFile = OutputFile;
            mystream.WriteInt(0);
            mystream.WriteByte(1);
            mystream.WriteString("__Signature");
            mystream.WriteString(Signature);
            mystream.WriteByte(2);
            mystream.WriteString("__CaseSensitive");
            mystream.WriteByte(0);
            mystream.WriteByte(255);
        }

        ~TJCRCreate() { Close(); }



    }

    /// <summary>
    /// The basic JCR6 class.
    /// </summary>
    class JCR6
    {
        public const bool dbg = false;

        // Better leave these alone all the time!
        // They are basically only used for drivier initiation, and since other classes must be able to do that, they are (for now) public.
        public static Dictionary<string, TJCRBASECOMPDRIVER> CompDrivers = new Dictionary<string, TJCRBASECOMPDRIVER>();
        public static Dictionary<string, TJCRBASEDRIVER> FileDrivers = new Dictionary<string, TJCRBASEDRIVER>();

        public static void dCHAT(string s)
        {
            if (dbg) { Console.WriteLine(s); }
        }

        /// <summary>Contains error message if last JCR6 error went wrong</summary> 
        static public string JERROR = "";

        static JCR6()
        {
            MKL.Version("JCR6 - jcr6.cs","18.09.30");
            MKL.Lic    ("JCR6 - jcr6.cs","Mozilla Public License 2.0");
            CompDrivers["Store"] = new TJCRCStore();
            FileDrivers["JCR6"] = new TJCR6DRIVER();
        }

        /// <summary>
        /// Recognize the specified file for use for JCR6. You'll rarely need this yourself, JCR6.Dir calls it to know which driver it needs.
        /// </summary>
        /// <returns>The name of the driver needed to load this file with JCR6, or NONE if the file has not been recognized.</returns>
        /// <param name="file">JCR resource.</param>
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

        /// <summary>
        /// Get the directory of a JCR resource.
        /// All known drivers will be tried automatically.
        /// </summary>
        /// <returns>The directory class.</returns>
        /// <param name="file">The file holding the JCR6 resource (or the directory in case of a real-dir, *if* the dirver is loaded that is).</param>
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
