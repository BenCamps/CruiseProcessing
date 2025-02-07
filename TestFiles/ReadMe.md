# Cruise Processing Test Files

## Testing Plan
There are many different variables when processing cruise data and generating reports. 
To ensure our test cover the most amount of cases with minimal duplication we will catigorize three types of test files:
 - Testing for different Cruise Methods. Cruise Method has a big effect on Stats, Expansion Factors and Proration; because of that we want to focus on validating data in population based reports and summary reports i.e. ST, UT, VSM, WT, TIM, and TEA reports . Coverint both plot and tree based cruise methods is important but there are some specific cruise methods that need special focus. 
     - Single Stage Plot
     - Double Stage Plot
     - Str
     - 3p - calculations that use KPI, and behavior when we have STM trees
     - FixCNT - whole different volume and biomass calculation

 - Testing for different regions. There are a lot of things that can change when processing from region to region, regional variations when using certian cruise methods, regional specific reports, and different commonly used reports based on region. Regional specific reports should be tested, as well, there may be some regional differences to volume and weight calculations. We should look at the indiviual tree and log calculated values in the `A` reports and well as compare the calculated values in stored in the cruise file. 
 - Testiong for specific cases. Less is more sometimes. It can be hard to test for everything at once so for some specific cases we can create more specific test files. This could be for testing a specific report, or a end-to-end testing for a special case such as processing a merged cruise. 


## Summary of Contents
This folder contains all the test files for Cruise Processing.
These test files have been developed over multiple generations of Cruise Processing with the help of many different hands. 

### OgTest files
File in the OgTest directory were originaly created for V1 of Cruise Processing and are in the .crz file format.
Those V1 files were updated to V2 and should have corisponding .cruise file with the same file name.
And some of those V2 test files were also updated to V3 and have a .crz3 file.

### Version3Testing files
These files were created in the Fall of 2021 using a pre-production versions of NatCruise and FScruiser. 
The intention was to re-create a cruise based off of a V2 cruise by hand, so that we could test out various cruise methods

### SteveTest
These Files were created in Sept 2022 by an intern working for us, Steven Adler. He created a set of V3 files, recreaded by re-entering original data in from V2 files. These were created using a production version of NatCruise and data entered using a production version of FScruiser. Some of the test files also test the cruise merging process. 

### Issues
These are files that were causing an issue in Cruise Processing. 
They may be from users or created specificly to test for an issue. 
Some of these files my no longer be needed after a while, if it is determined that tests should be cleaned up. 
See [notes.txt](./Issues/notes.txt) for details on each file

### Temp
Ignore any files in this folder if it exists

## Index of Test Files
```
+---Issues
|       02376_Cowboy Salvage_TS_202405071110_GalaxyTabActive3-18CJU.process.cruise
|       20383_Jiffy Stewardship_TS.crz3
|       notes.txt
|
+---OgTest
|   |   readme.md
|   |
|   +---BLM
|   |       Hammer Away.cruise
|   |       Hammer Away.crz
|   |       Long Nine.cruise
|   |       Long Nine.crz
|   |
|   +---Region1
|   |       French_Gulch.crz
|   |       R1_FrenchGulch.cruise
|   |
|   +---Region10
|   |       R10.cruise
|   |       R10.crz3
|   |       r10testcruise.crz
|   |
|   +---Region2
|   |       R2Test.crz
|   |       R2_Test.cruise
|   |       R2_Test_V3.crz3
|   |
|   +---Region3
|   |       R3_FCM_100.cruise
|   |       R3_FCM_100.crz3
|   |       R3_PCM_FIXCNT.cruise
|   |       R3_PCM_FIXCNT.crz3
|   |       R3_PNT_FIXCNT.cruise
|   |       R3_PNT_FIXCNT.crz3
|   |
|   +---Region4
|   |       R4_McDougal.cruise
|   |       R4_McDougal.crz3
|   |       Test4TestFile.crz
|   |
|   +---Region5
|   |       R5.cruise
|   |       R5.crz3
|   |       Region5TestFile.crz
|   |
|   +---Region6
|   |       06001.crz
|   |       R6.cruise
|   |       R6.crz3
|   |
|   +---Region8
|   |       R8.cruise
|   |       R8.crz3
|   |       r8test.crz
|   |
|   \---Region9
|           Eight Bird.crz
|           R9.cruise
|           R9.crz3
|
\---Version3Testing
    |
    +---3P
    |   |   87654 test 3P TS.cruise
    |   |   87654_Test 3P_Timber_Sale.crz3
    |   |   87654_Test 3P_Timber_Sale_WithSTMTrees.crz3
    |   |   ReadMe.md
    |   |
    |   \---3P with STM trees
    |           87654 test 3P TS.cruise
    |           87654_Test 3P_Timber_Sale_10112021_GalaxyTabActive3-AQWF.crz3
    |
    +---FIX
    |       20301 Cold Springs Recon.cruise
    |       20301_Cold Springs_Timber_Sale_29092021.crz3
    |
    +---FIX and PNT
    |       99996FIX_PNT_Timber_Sale_08072021.crz3
    |       99996FIX_PNT_Timber_Sale_08242021.cruise
    |       99996_TestMeth_Timber_Sale_08072021.crz3
    |       99996_TestMeth_TS_202310040107_KC'sTabActive3-R9Q8.crz3
    |       ReadMe.md
    |
    +---PCM
    |       27504_Spruce East_TS.cruise
    |
    +---PNT
    |       Exercise3_Dead_LP_Recon.cruise
    |
    \---STR
            98765 test STR TS.cruise
            98765_test STR_Timber_Sale_30092021.crz3
            ReadMe.md
+---SteveTest
|   |   ReadMe.md
|   |
|   +---Class Exercises
|   |   +---Converted V3 Files
|   |   |   +---MWACP
|   |   |   |       Dead LP_Recon.crz3
|   |   |   |       Exercise 1.crz3
|   |   |   |       JavaBean Recon.crz3
|   |   |   |       Mixed Conifer Recon.crz3
|   |   |   |       Recon Aspen.crz3
|   |   |   |
|   |   |   +---R6
|   |   |   |       Class Recon.crz3
|   |   |   |
|   |   |   \---R89
|   |   |           42001_Redhorse_Recon.crz3
|   |   |           C72 DxP Recon.crz3
|   |   |           Multi Strata HOGAN RECON FINAL.crz3
|   |   |           PCM HOGAN RECON.crz3
|   |   |
|   |   +---MWACP
|   |   |       Exercise 1 Recon.cruise
|   |   |       JavaBean_Recon.cruise
|   |   |       Recon Aspen.cruise
|   |   |       Recon Dead LP.cruise
|   |   |       Recon Mixed Conifer.cruise
|   |   |
|   |   +---R6
|   |   |       Class_Recon.cruise
|   |   |
|   |   \---R89
|   |           42001 Redhorse Recon Data.cruise
|   |           C72 DxP Recon.cruise
|   |           Multi Strata HOGAN RECON FINAL.cruise
|   |           PCM HOGAN RECON.cruise
|   |
|   +---Region1
|   |       Dutch_Oven_TS_FSC2.cruise
|   |       V3_Dutch Oven_TS_CRZ.crz3
|   |
|   +---Region10
|   |   |   01522 Wrangell Island # 1 TS.cruise
|   |   |   99999_Snakey Unit 70A_TS.cruise
|   |   |   Merged_01522_Wrangell Island #1_TS_202209300327_GalaxyTabS3-15CRZ.crz3
|   |   |   V3_Snakey Unit 70A_TS_CRZ.crz3
|   |   |
|   |   \---Wrangell TS Component files
|   |           EvenPlots_01522_Wrangell Island #1_TS_202209300327_GalaxyTabS3-15CRZ.crz3
|   |           OddPlots_01522_Wrangell Island #1_TS_202209300406_GalaxyTabS3-15CRZ.crz3
|   |
|   +---Region2
|   |       04025_Ripley_TS.M.cruise
|   |       04035_Atkinson Timber Sale_TS.1.cruise
|   |       04035_Atkinson Timber Sale_TS.2.cruise
|   |       04035_Atkinson Timber Sale_TS.M.cruise
|   |       04036_BostonPeak_TS.M.cruise
|   |       V3_04036_BostonPeak_TS_CRZ.crz3
|   |
|   +---Region3
|   |       08084 Walker Hill Demo TS.cruise
|   |       V3_08084_Walker Hill Demo_Strata1-3_TS_CRZ.crz3
|   |       V3_08088_Walker Hill Demo Strata4_TS_CRZ.crz3
|   |
|   +---Region4
|   |       20301_Rusty Goose_TS.M.cruise
|   |       20301_Rusty Goose_TS_15CRZ.crz3
|   |
|   +---Region5
|   |   \---lightstwd
|   |           Light_stwd.1.cruise
|   |           Light_stwd.2.cruise
|   |           Light_stwd.3.cruise
|   |           Light_stwd.4.cruise
|   |           Light_stwd.5.cruise
|   |           Light_stwd.6.cruise
|   |           Light_stwd.7.cruise
|   |           Light_stwd.cruise
|   |           Light_stwd.M.cruise
|   |           Light_stwd.M.out
|   |           Light_stwd.M.PDF
|   |
|   +---Region6
|   |       21200.cruise
|   |
|   +---Region8
|   |       02363 Bonnerdale DXP TS.M.cruise
|   |       BreadCreek.M.cruise
|   |       V3_02363_Bonnerdale DXP_TS_CRZ.crz3
|   |
|   +---Region9
|   |       Good_Neighbor_Pine_Production_TSFinal.cruise
|   |       V3_Good Neighbor Pine Production_TS_CRZ.crz3
|   |
|   \---Testmeth
|           99996_TestMeth_TS_202310040107_KC'sTabActive3-R9Q8.crz3
|           99996_TestMeth_TS_202310040107_KC'sTabActive3-R9Q8.out
|           99996_TestMeth_TS_202310040107_KC'sTabActive3-R9Q8.process
```








