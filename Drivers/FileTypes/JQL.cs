// Lic:
// Drivers/FileTypes/JQL.cs
// JQL (JCR quick link)
// version: 23.01.10
// Copyright (C) 2020, 2023 Jeroen P. Broks
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TrickyUnits;


namespace UseJCR6 {

	
	class JCR_QuickLink:TJCRBASEDRIVER {
		
		struct QP {
			public string commando;
			public string parameter;
			public QP(string p) {
				var i = p.IndexOf(':');
				if (i<0) {
					commando = p.ToUpper();
					parameter = "";
				} else {
					commando = p.Substring(0, i).ToUpper();
					parameter = p.Substring(i + 1);
				}
			}
		}

		string RL(QuickStream BT,bool trim=true) {
			var r = new StringBuilder();
			byte b = 0;
			while (true) {
				if (BT.EOF) break;
				b = BT.ReadByte();
				if (b == 10) break;
				if (b != 13) r.Append((char)b);
			}
			if (trim) return r.ToString().Trim();
			return r.ToString();
		}

		QP RQP(QuickStream BT) => new QP(RL(BT));

		public override bool Recognize(string file) {
			QuickStream BT=null;
			try {
				//Console.WriteLine($"JQL Recognize {File.Exists(file)}");
				if (!File.Exists(file)) return false;
				BT = QuickStream.ReadFile(file);
				string s;
				do {
					s = RL(BT);
					//Console.WriteLine($"_{s}_");
					if (s != "" && (!qstr.Prefixed(s, "#"))) return s == "JQL";
				} while (!BT.EOF);
				return false;
			} finally {
				if (BT!=null) BT.Close();
			}
		}

		public override TJCRDIR Dir(string file) {
			QuickStream BT = null;
			var MapFrom = new Dictionary<string, TJCRDIR>();
			TJCRDIR From = null;
			try {
				BT = QuickStream.ReadFile(file);
				var ret = new TJCRDIR();
				string s;
				do {
					if (BT.EOF) throw new Exception("JQL heading not found!");
					s = RL(BT);                    
				} while (s=="" || qstr.Prefixed(s, "#"));
				if (s != "JQL") throw new Exception("JQL not properly headed!");
						var optional = true;
						var author = "";
						var notes = "";
				while(!BT.EOF) {
					s = RL(BT);
						var c = new QP(s);
					if (s!="" && (!qstr.Prefixed(s, "#"))){
						switch (c.commando) {
							case "REQUIRED":
							case "REQ":
								optional = false;
								break;
							case "OPTIONAL":
							case "OPT":
								optional = true;
								break;
							case "PATCH": {
									var to = c.parameter.IndexOf('>');
									if (to < 0) {
										var p = JCR6.Dir(c.parameter);
										if (p == null) {
											if (optional) break;
											throw new Exception($"Patch error {JCR6.JERROR}");
										}
										ret.Patch(p);
									} else {
										var rw = c.parameter.Substring(0, to).Trim().Replace("\\", "/");
										var tg = c.parameter.Substring(to + 1).Trim().Replace("\\", "/");
										var p = JCR6.Dir(rw);
										if (p == null) {
											if (optional) break;
											throw new Exception($"Patch error {JCR6.JERROR}");
										}
										ret.Patch(p, tg);
									}
									break;
								}
							case "AUTHOR":
							case "AUT":
								author = c.parameter;
								break;
							case "NOTES":
							case "NTS":
								notes = c.parameter;
								break;
							case "RAW": {
									var p = c.parameter.IndexOf('>');
									var rw = c.parameter.Replace("\\","/");
									var tg = rw;
									if (p >= 0) {
										rw = c.parameter.Substring(0, p).Trim().Replace("\\","/");
										tg = c.parameter.Substring(p + 1).Trim().Replace("\\", "/");
									}
									if (tg.Length>1 && tg[1] == ':') tg = tg.Substring(2);
									while (tg[1] == '/') tg = tg.Substring(1);
									if (rw == "") throw new Exception("RAW no original");
									if (tg == "") throw new Exception("RAW no target");
									if (!File.Exists(rw)) {
										if (optional) break;
										throw new Exception($"Required raw file \"{rw}\" doesn't exist!");
									}
									var e = new TJCREntry();
									e.Entry = tg;
									e.MainFile = rw;
									e.Storage = "Store";
									e.Offset = 0;
									e.Size = (int)new FileInfo(rw).Length;
									e.CompressedSize = e.Size;
									e.Notes = notes;
									e.Author = author;
									ret.Entries[tg.ToUpper()] = e;
									break;
								}
							case "TEXT":
							case "TXT": {
									var tg = c.parameter.Trim().Replace("\\", "/");
									if (tg.Length > 1 && tg[1] == ':') tg = tg.Substring(2);
									while (tg[1] == '/') tg = tg.Substring(1);
									if (tg == "") throw new Exception("TEXT no target");
									var e = new TJCREntry();
									var buf = new byte[5];
									e.Entry = tg;
									e.MainFile = file;
									e.Storage = "Store";
									e.Offset = (int)BT.Position;
									e.Notes = notes;
									e.Author = author;
									do {
										if (BT.EOF) throw new Exception("Unexpected end of file (TXT Block not ended)");
										for (int i = 0; i < 4; i++) buf[i] = buf[i + 1];
										buf[4] = BT.ReadByte();
										//Console.WriteLine(Encoding.UTF8.GetString(buf, 0, buf.Length));
									} while (Encoding.UTF8.GetString(buf,0,buf.Length) != "@END@");
									RL(BT);
									e.Size = (int)(BT.Position - 7) - e.Offset;
									e.CompressedSize = e.Size;
									ret.Entries[tg.ToUpper()] = e;
									break;
								}
							case "COMMENT":
							case "CMT": {
									if (c.parameter == "") throw new Exception("Comment without a name");
									var cmt = new StringBuilder("");
									var l = "";
									do {
										if (BT.EOF) throw new Exception("Unexpected end of file (COMMENT block not ended)");
										l = RL(BT, false);
										if (l.Trim() != "@END@")
											cmt.Append($"{l}\n");
									} while (l.Trim() != "@END@");
									ret.Comments[c.parameter] = cmt.ToString();
									break;
								}
							case "IMPORT":
								ret.PatchFile(c.parameter);
								break;
							case "FROM": {
									var P = c.parameter.ToUpper();
									if (MapFrom.ContainsKey(P)) {
										From = MapFrom[P];
									} else {
										var F = JCR6.Dir(P);
										if (F == null) throw new Exception(JCR6.JERROR);
										From = F;
										MapFrom[P] = F;
									}
								}
								break;
							case "STEAL": {
									if (From == null) throw new Exception("STEAL cannot be used without FROM");
									var p = c.parameter.IndexOf('>');
									var rw = c.parameter.Replace("\\", "/");
									var tg = rw;
									if (p >= 0) {
										rw = c.parameter.Substring(0, p).Trim().Replace("\\", "/");
										tg = c.parameter.Substring(p + 1).Trim().Replace("\\", "/");
									}
									if (tg.Length > 1 && tg[1] == ':') tg = tg.Substring(2);
									while (tg[1] == '/') tg = tg.Substring(1);
									if (rw == "") throw new Exception("STEAL no original");
									if (tg == "") throw new Exception("STEAL no target");
									if (!From.Exists(rw)) throw new Exception($"Cannot steal a non-existent entry ({rw})");
									var ei = From.Entries[rw.ToUpper()];
									var eo = new TJCREntry();
									foreach (var dat in ei.databool) eo.databool[dat.Key] = dat.Value;
									foreach (var dat in ei.dataint) eo.dataint[dat.Key] = dat.Value;
									foreach (var dat in ei.datastring) eo.datastring[dat.Key] = dat.Value;
									eo.MainFile = ei.MainFile;
									eo.Entry = tg;
									ret.Entries[tg.ToUpper()] = eo;
								}
								break;
							case "END":
								return ret;
							default: throw new Exception($"Unknown instruction! {c.commando}");
						}
					}
				}
				return ret;
			} catch (Exception e) {
				JCR6.JERROR = $"JQL error: {e.Message}";
#if DEBUG
				Console.WriteLine(e.StackTrace);
#endif
				return null;
			} finally {
				if (BT != null) BT.Close();
			}
		}

		public JCR_QuickLink() {
			name = "JCR Quick Link";
			JCR6.FileDrivers["JCR6 Quick Link"] = this;
		}
	}
	
}