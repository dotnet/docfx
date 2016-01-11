Doc-as-code: DocFx.exe Incremental Build Specification
==========================================

This documentation describes the implementation of incrementally extracting metadata from source. Currently we are using *Roslyn* to compile and analyse source code on the fly. When input sources are large, it may take minutes to load and process the files. To speed up the extraction, previous extracted details are saved to cache for further reference. 

There are two level caches in current implementation. First one is called *Application* Level cache, and the other one is *Project* level cache.

*Application* level cache is saved in file `%LocalAppData%/xdoc/cache`.

For *Project* level cache,

a. If input sources are supported project files, e.g. `.csproj` or `.vbproj` files, *Project* level cache is located in file `obj/xdoc/.cache` under the same folder of the project file. 

b. If input sources are supported source code files, e.g. `.cs` or `.vb` files, *Project* level cache is located in file `obj/xdoc/.cache` under the same folder of the alphabetically first source code file.

The cache file contains key-value pairs saved in *JSON* format. The key is the normalized input source code files, and the data structure for the value is as below:

Property             | Description   
---------------------|--------------------------
TriggeredUTCTime     | The UTC time when the action is triggered
CompletedUTCTime     | The UTC time when the action is completed
OutputFolder         | The output folder for the extracted result
RelativeOutputFiles  | The paths of the extracted results related to the *OutputFolder*
CheckSum             | The MD5 checksum calculated for all the extracted results

Detailed Steps are described below: 
1. For each input solution/project/source files, get most latest `LastModifiedTime`.
    a. For solution, get `LastModifiedTime` for the solution file, and containing projects
    b. For project, get `LastModifiedTime` for the project file, project references, assembly references and containing documents.
    c. For source files, get `LastModifiedTime` for the files
2. Normalize project list, check if *Application* level cache for these project list exists. Compare `TriggeredUTCTime` with the `LastModifiedTime` fetched in #1, and check if checksum remains unchanged for output files. If is, copy result files to output folder. Otherwise, continue to #3.

3. For each supported solution/project/source code files,

	*Step 1*. Check if *Project* level cache exists. If not, go to *Step 4*.
	
	*Step 2*. Compare `TriggeredUTCTime` with the `LastModifiedTime` fetched in #1, and check if checksum remains unchanged for output files. If not, go to *Step 3*.
	
	*Step 3*. Generate YAML metadata for current project and save to *Project* level cache.
 
4. Read YAML metadata for each project, and merge with others following rules below:

	*Rule 1*. For `namespace`, if `uid` equals, **append**.
	
	*Rule 2*. For other type, if `uid` equals, **override**.

5. Save result, and update **Application* level cache.
