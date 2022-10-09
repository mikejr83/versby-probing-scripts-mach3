# Probing Scripts for Mach3

The following changes are for the vers.by probing scripts used in the Physics Anonymous screen set for Mach3. The scripts have been modified so that they will work with the cheaper BL-USBMach boards that are found on sites like AliExpress.

**IMPORTANT:** Before you use these scripts I suggest reading the section on using them below. I am not responsible for broken probes and probing tips. The YouTube video describing the original screenset for Mach3 that these were pulled from did not go into detail on how to use them. If you're not accoustomed to the way these probe you could damage your probe or probe stylus/tip.

## Differences From The vers.by Scripts

The main difference for the majority of the scripts is that the `GetVar` calls have been substitued with calls to `GetOEMDRO` for the axis that was probed. I have found that these boards do not reliably support returning sane values for `GetVar(2000)` and `GetVar(2001)` when probing X and Y, respectively.

Also, I found with my particular board that `G31` would only work with a single axis. The original scripts made use of `G31 X<SomeValue> Y<SomeValue>` for "safe moves. The command would only interpret the X movement and then when calculating if Y had moved would fail. This was fixed by breaking up the command into two separate commands, one for each direction. After each movement the script checks to see if a premature trigger of the probe happend thus fulfilling the "safe" movement design.

### Additional Alerts

All the scripts I edited have a dialog that is presented to the user before any movement occurs. It will ask if you have checked that your probe is plugged in. I've included this as a safety because I've already destroyed one probe tip. Hitting the "Cancel" button will abort any probing. 

In some of the center finding script I also included an alert that the search distance is smaller than the clearance and offset. If the user clicks "Ok" the search distance will be modified to the sum of the clearance and offset **for this probing routine only**. Clicking cancel will continue the script operation as normal but will likely fail due to the search distance being too small.

## Installation

You can download the contents of this GitHub repository by clicking the "Download" button and selecting to download as a zip file. Once downloaded extract the zip into a temporary folder.

These scripts do not include edits to the Z probing routine. It is my thought that most users will have a puck like probe that they will be overriding the z height probing. I do not think most users of these cheap BL-USBMach boards will also be using tool height setting. If you are or would like the z height scripts modified please let me know via an issue request and I'll rework those as well. However, since I do not have tooling to verify they will be done on assumptions.

On the probing screen you'll see four sections:

* Outside
* Inside
* Center
* Z (Not included)

![Probe Button Locations](Images/ProbingScreen.png)

### Outside and Inside Scripts

For the "Outside" section see the "Outside Routines" folder. The scripts are numbered 1-8. The 1 script goes in the top left corner. The rest are located in a clockwise fashion from this initial button. Thus, 2 is the middle top, 3 is the top right, 4 the middle right, and so on. The "Inside" section is exactly the same and the scripts are located in the "Inside Routines" folder.

### Center Scripts

I've broken the center finding scripts into 3 areas, "Outside Center", "Inside Center", and "Hole and Post". These correspond to the sets of buttons from top to bottom in the third grouping.

The first group of buttons are for finding outside centers. Their scripts are located in the "Outside Center Routines" folder. The scripts are labeled to indicate if its find the center for Y or X.

The second group of buttons are for finding inside centers. Their scripts are located in the "Inside Center Routines" folder. The scripts are labeled to indicate if its find the center for Y or X.

The last group of buttons are for finding the center of a post or hole. Their scripts are located in the "Hole and Post Routines" folder. The script labeled "PostCenter" is for probing the center from the outside of a post or cylinder. This is the left button. The script labeled "HoleCenter" is for finding the center of a hole and corresponds to the right button.
