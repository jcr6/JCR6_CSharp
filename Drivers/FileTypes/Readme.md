# File type drivers

This is where classes will be stored to allow JCR6 to read non-JCR6 resources.


### realdir.cs

Allows JCR6 to think a directory is actually a JCR6 resource. Please note that support is limited, and stuff like symlinks can act this class to go haywire. I've often used this for testing purposes, as it's not always a good idea to keep rebuilding a JCR6 resource. (In large project that takes a lot of time).

# WAD.cs

Will load a Doom WAD file as it were a JCR6 file. WAD is a very simplistic format and it doesn't support even half of what JCR6 can support, but everything WAD has to offer is supported by JCR6. Due to the 8 character limit, iD software had to improvise on how the levels were added, and their method used is something JCR6 can normally not handle due to duplicate filenames, but doncha worry, I've set the driver to turn this into folders in stead.

# QuakePack

A more sophisticated version of WAD, but still inferior to JCR6 and therefore JCR6 can load it just fine thanks to this driver ;)
Yeah, these files were used in the game Quake. It was only tested on Quake's first instalment, so I do not know if it can handle the files used in later instalments.
