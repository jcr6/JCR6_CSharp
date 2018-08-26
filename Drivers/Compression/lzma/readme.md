# JCR6 LZMA drivers

In this folder, you can find the drivers JCR6 needs to support LZMA compression.
ALL files in this "lzma" folder AND all files in its subfolders MUST be present or LZMA won't work (if one is missing your project might even NOT compile :P)

Now the ONLY file I wrote myself is lzma2jcr6.cs which only contains the class JCR6 needs to get everything to work. This class calls directly to a "helper" written by Peter Bromberg (Is that an intentionally funny name in Dutch, or just coincidence?) More information about that helper can be found [here](http://www.nullskull.com/a/768/7zip-lzma-inmemory-compression-with-c.aspx)

The LZMA packer and unpacker themselves are the original LZMA routines written by Igor Pavlov himself for the C# language, and they can be found on [his own site](https://www.7-zip.org/sdk.html).

# License & copyrights:

I only take credit for the class that ties everything into JCR6, which means that I only take credit for the lzma2jcr6.cs file, all other files are still copyrighted and licensed by their respective creators (in which I have to note that the source codes of the LZMA routines by Igor were even put into the Public Domain. Now some countries do not support the concept of stuff being in the public domain for other reasons that the expirement of copyrights due to the copyright holder being dead for xxx years, wellin that case you can consider it licensed under the CC0 license). My own tie-up code is part of the JCR6 project and as such licensed under the MPL 2.

# Usage of this driver in JCR6

Unfortunately, it appears that C# only compiles and includes classes that are actually used, although when you use JCR6 you don't need to call for the drivers, as JCR6 does that itself as soon as it needs them, and as far as I could find out C# didn't fully like that concept. 
Still if JCR6 is present, you'll just need to add the line:
~~~C#
new JCR6_lzma();
~~~
to your code, and the lzma driver will automatically register itself into the JCR6 main class, and JCR6 will recognise it, for as long as your program is running. :)
Basically all JCR6 drivers I'll provide will require this setup... ;)

