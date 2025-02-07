
 - Fix file issues: bad converted files or files with processing errors. See [FileIssues.md](./FileIssues.md)
     - [Ben] Recreate bad .cruise files from .crz files using COconvert
     - [Ben] Recreate bad .crz3 fiels using CruiseCLI
 - Reprocess all .cruise and .crz3 files: delete any old .html, .out files in the process
     - Create new .out files from the .cruise files and rename with .out.v2 extention
     - Create new .out file from the .crz3 with the .out.v3 extention
     - Keep any .process files created when processing .crz3 files
     - Create .sum files so we can test them too 
 - [Maybe] If any V1 .crz3 files exist process them and create .out.v1 file... this might not be imediatly useful but could be good for reference. 
 - Check volume Equations on V3 files that were manualy created. We didn't have the ability to save Volume Equations changes to V3 back then so they may need to be setup.
 - Add additional reports improve coverage on different reports, per the "Testing Plan"

## Version3Testing
 - Remove duplicates and old copies of files. 
    - Decide which files to keep in `Version3Testing/FIX and PNT/`
 - Up convert .cruise files to V3 where no V3 test file exists for cruise method


Currently we have our older "OgTest" files that cover our regional testing. But we have some regions not well covered (R6). 
The files in "Version3Testing" cover the some of the different cruise methods, 
but are missing a few methods, don't have V3 files for some and might not cover all the special cases (nested plots, ...)
The Version3Testing files were partialy created to help test the V3 to V2 conversion. That process has been tested pretty well, so I don't think we need to create any more sets of files where we have a V2 and V3 file, but we do need to fill out test for 
more the rest of the cruise methods we need to cover (FixCNT, ...)

Through our collecting of Cruise Files we have a lot of files where there is both a V2 version and V3 version. Some of the V3 
Were created by automaticly converting a V2 file to a V3 file. Others were created by manualy re-entering V2 cruise data into a V3 file. The two methods offer different binifits and risks:
The automatic conversion eliminates errors due to miss-entering data and if there is an issue in the conversion we can fix it and reconvert the file. 
The manualy converted files, allowed for testing with a file created as a cruiser would have done and prevents undiscovered issues from the conversion from effecting the results. 

The files in `OgTest` were generaly created using the conversion process. Files in `Version3Testing` were mainualy recreated. Files in `SteveTest` were likely all manualy re-created. 

 - [Ben] Re-upconvert all V2 files in the OgTest folder to V3
 

## SteveTest
 - Identify the cruises that test merging multiple device files and look at what Cruise Methods types of data were used. We want to have coverage for both Plot and Tree Based methods. Idealy FixCNT, 3p, and logs too. 
 - Need to process, quality check, and generate out files for all Steves files
 
