# JCR6_Sharp 

These are just a bunch of class files you can use to read JCR6 files in C#


JCR = Jeroen's Collected Resource, and it's a modular resource "archiving" system, specifically set up for use in Games rather than real archiving (one of the reasons why I am not planning to do 'solid archiving' in it). It has proved to be pretty fast, and solid, and patchable, and for some uses very flexible.
The system has been setup to be modular, meaning that (as long as the drivers for that are provided) JCR6 should be able to handle any compression algorithm, and for those who really want to mess around, yeah if you want the contents for your JCR file to be very very secret, you can even try to code your own in order to make unpacking in the open source unable to unpack it. All too easy. ;)


JCR began as JBCode and would later be NCode, in which encryption codeshad more emphasis. The first 4 versions were based on that, oh, and it's by JCR4 that the name JCR was actually used for the first time, and it remained :)
JCR5 was the first to be completely modular for more flexible use, but I got stuck on a few things that were put in too fast making some things impossible to implement without turning things into chaos, and so JCR6 began to cover all that up, and hopefully I do not have to replace it with JCR7 in the future :P
JCR6 was originally set up in both BlitzMax and Python and is later set up in Go. Much of the C# classes are actually translated Go codes, although I did remove a few Go lines in order to take advantage of a few features in which C# appears to me more advanced that Go.

Prior to the translation to C#, JCR6 has been used in the production of Star Story and The Fairy Tale REVAMPED, which do not only use it for reading their script codes and assets, but for creating their savegames as well. Sixty-Three Fires of Lung has also been set up to use JCR6, although the LOVE engine is not friendly on it especially not in Windows, so the game has (especially in Windows) been set up to "cheat" around things a little to cheat around the advantages of JCR6 that zip does not have... far from ideal, but it has to do (for now)).
I also wrote some utilities that take advantage of JCR6, and depending on what the future brings me when it comes to coding, I may do a lot more with it in the future. JCR has always been my basis to work on, on the very first version of it (back then written in Turbo Pascal), up to today. I always feel hanicapped without it.

# Needed:

In the jcr6.cs file I've noted which files from my TrickyUnits for C# you need to get code using JCR6 compiled. Now to get JCR6 to run, you will need all the files in the root of the project, otherwise, well you may suffer dearly. :P
The drivers dir is optional, although without the classes in there, all JCR6 can do is read non-compressed JCR6 files. Not much, eh? :-P

# License:

JCR6 has been released under the terms of the Mozilla Public License 2.0
Technically you can add it to your programs for free, even in closed sourced commericial projects as long as the JCR6 source files remain unmodified and if you modify then then you must release the modified versions.
If you want to add your own compression methods or file formats in a close sourced manner, you can create them in a separate project calling the JCR6 classes and then you are fine :P

Of course, JCR6 comes "as is" and under no circumstances can I be held liable for any sort of damage or other kind of bad effects coming forth from the usage of JCR6... Ah yeah, the standard stuff I guess.



I am planning to release JCR6 as a .dll file which could be linked to at least any .NET compatible program. Documentation about its classes and how to use it, is a longer term thing. I first want it to work at all ;)


# Notice:

This repository is going to move to https://github.com/JCR6/JCR6_CSharp -- If you are already there, cool! If not, then please configure git to take it from there on. Until June 2019 I'll update both repositories, although the repository in the main is the main repository, somewhere in or after January 2020, the old repository will be removed.
