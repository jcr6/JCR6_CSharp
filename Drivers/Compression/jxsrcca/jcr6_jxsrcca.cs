using TrickyUnits;

namespace UseJCR6 {


    class JCR6_jxsrcca : TJCRBASECOMPDRIVER     {
        public JCR6_jxsrcca(bool nor = false) { if (!nor) Init(); }

        /// <summary>
        /// Call this function to install the jxsrcca driver into JCR6
        /// That's all you need to do, as JCR6 will take care of the rest ALL BY ITSELF ;)
        /// </summary>
        public static void Init() {
            JCR6.CompDrivers["jxsrcca"] = new JCR6_jxsrcca(true);
            MKL.Lic("JCR6 - jxsrcca.cs", "ZLib License");
            MKL.Version("JCR6 - jxsrcca.cs", "19.03.06");
        }

        override public byte[] Compress(byte[] inputbuffer) {
            byte[] ret = new byte[inputbuffer.Length*2]; // double is the max posibility something can become with jxsrcca. Always safer way to go. JCR6 detects afterwards if something became bigger resorting to "Store" in stead!
            return ret;
        }

        public override byte[] Expand(byte[] inputbuffer, int realsize) {
            byte[] ret = new byte[realsize];
            int pos = 0;
            QuickStream bi = QuickStream.StreamFromBytes(inputbuffer);
            bi.ReadByte(); // First byte had to be ignored... A little issue that came up in the first draft... silly me!
            while (!bi.EOF) {
                byte xbyte = bi.ReadByte();
                byte xkeer = bi.ReadByte();
                for (byte i = 0; i < xkeer; ++i) {
                    if (pos>=realsize) {
                        JCR6.JERROR = $"JXSRCCA: Pos {i} expanded over the realsize length {pos}/{realsize}. Is this entry corrupted?";
                        System.Console.WriteLine("ERROR! "+JCR6.JERROR);
                        bi.Close();
                        return null;
                    }
                    //System.Console.Write(qstr.Chr(xbyte));
                    ret[pos] = xbyte;
                    pos++;
                }
            }
            bi.Close();
            return ret;
        }
    }
}