using System.IO;
using System.Collections.Generic;

namespace UseJCR6{

    class JCR6_RealDir : TJCRBASEDRIVER
    {
        string FLError { set { JCR6.JERROR = value; } }    
        public JCR6_RealDir() { Name = "Real Dir"; }
        public bool allowhidden = false;
        public bool automerge = true;
        public override bool Recognize(string file) {
            FileAttributes attr = File.GetAttributes(file);
            return (attr & FileAttributes.Directory) == FileAttributes.Directory;
        }
        public override TJCRDIR Dir(string file){
            // init
            var ret = new TJCRDIR();
            var path = file;
            var w = new List<string>();
            var di = new DirectoryInfo(path);
            ret.Comments["Real Dir"] = "Actually \"" + path + "\" is not a JCR6 resource, but a directory \"faked\" into a JCR6 resource.";
            // Check
            if (!di.Exists)
            {
                FLError = "UseJCR6.JCR6_RealDir.Dir(\"" + path + "\"): Directory does not exist!";
                return null;
            }
            // Listout
            foreach (DirectoryInfo fi in di.GetDirectories())
            {
                if (allowhidden || fi.Name.Substring(0, 1) != ".")
                {
                    var a = JCR6.Dir(path + "/" + fi.Name);
                    foreach (string k in a.Entries.Keys)
                    {
                        var ke = a.Entries[k];
                        ret.Entries[(fi.Name + "/" + k).ToUpper()] = ke;
                        ke.Entry = fi.Name + "/" + ke.Entry;
                        ke.MainFile = path + "/" + ke.Entry;
                    }
                }
            }
            foreach (FileInfo fi in di.GetFiles()) {
                if (automerge && JCR6.Recognize(path + "/" + fi) != "NONE")
                {
                    var a = JCR6.Dir(path + "/" + fi.Name);
                    foreach (string k in a.Entries.Keys)
                    {
                        var ke = a.Entries[k];
                        ret.Entries[(fi.Name + "/" + k).ToUpper()] = ke;
                        ke.Entry = fi.Name + "/" + ke.Entry;
                        ke.MainFile = path + "/" + ke.Entry;
                    }
                    foreach (string k in a.Comments.Keys) { ret.Comments[k] = a.Comments[k]; }
                }
                else
                {
                    var e = new TJCREntry();
                    e.Entry = fi.Name;
                    e.MainFile = path + "/" + fi.Name;
                    e.Storage = "Store";
                    e.CompressedSize = (int)fi.Length;
                    e.Size = (int)fi.Length;
                    ret.Entries[fi.Name.ToUpper()] = e;
                }
            }



            // return the crap
            return ret;
        }
    }

}
