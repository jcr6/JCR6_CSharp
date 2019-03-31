// Lic:
// Drivers/FileTypes/RealDir.cs
// (c)  Real Directory Driver for JCR6 C#.
// 
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL was not
// distributed with this file, You can obtain one at
// http://mozilla.org/MPL/2.0/.
// Version: 19.03.27
// EndLic


using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using TrickyUnits;



namespace UseJCR6
{



    class JCR6_RealDir : TJCRBASEDRIVER
    {

        string FLError { set { JCR6.JERROR = value; } }
        public JCR6_RealDir() { name = "Real Dir"; JCR6.FileDrivers["Real Directory"] = this; }
        public bool allowhidden = false;
        public bool automerge = true;
        public override bool Recognize(string file)
        {
            try {
                FileAttributes attr = File.GetAttributes(file);
                return (attr & FileAttributes.Directory) == FileAttributes.Directory;
            } catch (Exception e) {
                JCR6.JERROR = $"I could not analyse directory {file} for realdir scanning.\n{e.Message}";
                return false;
            }
        }

        public override TJCRDIR Dir(string file) => RDir(file, true);


        private TJCRDIR RDir(string file, bool ap)
        {
            /*
            // init
            var ret = new TJCRDIR();
            var path = file;
            var w = new List<string>();
            var di = new DirectoryInfo(path);
            ret.Comments["Real Dir"] = "Actually \"" + path + "\" is not a JCR6 resource, but a directory \"faked\" into a JCR6 resource.";
            // Check
            if (!di.Exists) {
                FLError = "UseJCR6.JCR6_RealDir.Dir(\"" + path + "\"): Directory does not exist!";
                return null;
            }
            // Listout
            foreach (DirectoryInfo fi in di.GetDirectories()) {
                if (allowhidden || fi.Name.Substring(0, 1) != ".") {
                    JCR6.dCHAT("Recursing: " + fi.Name);
                    var a = JCR6.Dir(path + "/" + fi.Name);
                    foreach (string k in a.Entries.Keys) {
                        var ke = a.Entries[k];
                        ret.Entries[(fi.Name + "/" + k).ToUpper()] = ke;
                        ke.Entry = fi.Name + "/" + ke.Entry;
                        ke.MainFile = qstr.ExtractDir(path) + "/" + fi.Name + "/" + ke.Entry;
                    }
                }
            }
            foreach (FileInfo fi in di.GetFiles()) {
                if (automerge && JCR6.Recognize(path + "/" + fi.Name) != "NONE") {
                    var a = JCR6.Dir(path + "/" + fi.Name);
                    if (a == null) {
                        Console.WriteLine($"WARNING! Scanning {fi.Name} failed >> {JCR6.JERROR}");
                    } else {
                        foreach (string k in a.Entries.Keys) {
                            var ke = a.Entries[k];
                            ret.Entries[(fi.Name + "/" + k).ToUpper()] = ke;
                            ke.Entry = fi.Name + "/" + ke.Entry;
                            ke.MainFile = path + "/" + ke.Entry;
                        }
                        foreach (string k in a.Comments.Keys) { ret.Comments[k] = a.Comments[k]; }
                    }
                } else {
                    var e = new TJCREntry();
                    e.Entry = fi.Name;
                    e.MainFile = path + "/" + fi.Name;
                    e.Storage = "Store";
                    e.CompressedSize = (int)fi.Length;
                    e.Size = (int)fi.Length;
                    ret.Entries[fi.Name.ToUpper()] = e;
                }
            }
            */
            var ret = new TJCRDIR();
            var dir = FileList.GetTree(file, true, allowhidden);
            ret.Comments["Real Dir"] = "Actually \"" + file + "\" is not a JCR6 resource, but a directory \"faked\" into a JCR6 resource.";
            foreach (string chkfile in dir) {
                var mf = $"{file.Replace('\\', '/')}/{chkfile}";
                if (automerge && JCR6.Recognize(mf) != "NONE") {
                    var t = JCR6.Dir(mf);
                    if (t == null) {
                        Debug.WriteLine($"Error in auto-merge JCR: {JCR6.JERROR}");
                    } else {
                        foreach (string k in t.Entries.Keys) {
                            var ke = t.Entries[k];
                            ret.Entries[$"{mf.ToUpper()}/{k}"] = ke;
                            ke.Entry = chkfile + "/" + ke.Entry;
                            ke.MainFile = mf; //+ "/" + ke.Entry;
                        }
                    }
                } else {
                    var e = new TJCREntry();
                    var fi = new FileInfo(mf);
                    e.Entry = chkfile; //fi.Name;
                    e.MainFile = mf;
                    e.Storage = "Store";
                    e.CompressedSize = (int)fi.Length;
                    e.Size = (int)fi.Length;
                    ret.Entries[mf.ToUpper()] = e;
                }
            }
            // return the crap
            return ret;
        }
    }
}
