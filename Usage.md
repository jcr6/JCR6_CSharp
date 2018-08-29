# Use JCR6

It's up to you to either copy the files into your project or to use your project files to link to them only, as long as everything is in accordance with the license that is.
All files in the main directory MUST be present or JCR6 can, and very likely WILL malfunction, if your project compiles at all that is, which I highly doubt, as the files in the main directory basically contain all the core features of JCR6 and also the default JCR6 file reader.

The Drivers folder is completely optional, but without it all the library can do is read and write JCR6 files without any form of compression. The "FileTypes" folder is for enabling JCR6 to read from file formats it doesn't know by default. Most of them are not really needed for anything and more fun to have, although the "RealDir.cs" driver might be handy to have for debugging purposes. ;)

The "Compression" folder contains all compression methods that have been set up to work in JCR6 in C#. Please take note of which compression methods are supported and which you add into your own project, as the JCR6 tool doesn't care about such things, and it may be possible that its default settings have not yet been properly ported to C#, and especially when you use "BRUTE" you may get past compression algorithms for which nobody ever took the trouble to port the to C#, and if you use the JCR6 tools compiled by anybody who is not me, who knows what kind of algorithms they could have concucted themselves, so sort this out well, before you pack stuff and try it in C#. (The JCR6 cli tools have been written in Go, and I have little to no interest to convert those to C# on short term).

If you are interested in writing nice drivers for JCR6 in C#, don't be a stranger, and try it. If you need some extra explanations or documentations about such things, ask me. :)



If you take a look in the Wiki, you can see a quick overview of all the classes and their respective methods for optimal use of JCR6.
