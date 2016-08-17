# msblutil
MSBuild Log Utility

Utility for parsing corefx msbuild logs to ease with debugging very verbose (diagnostic) logs.

This tool was created to ease with the pain of debugging msbuild diagnostic logs generated in a CoreFx build.  The MSBuild logs are difficult to dig through because they are multi-threaded logs and some of them can be so large as to not be loadable in common editors (such as notepad).

**Microft Build Log Utility Usage:**

msblutil split [msbuild log]
  Split an msbuild log in half (by lines), generating logname_top.extension and logname_bottom.extension files.

msblutil grab [msbuild log] [proc #]
  Gather all of the msbuild log entries for a specific process # and generate a new log file.

msblutil grab [msbuild log] [project name]
  Find all projects matching the specified project name and generate new log files on a per process basis.

msblutil list [msbuild log]
  List all of the projects and processes in an msbuild log file.

msblutil failures [msbuild log]
  List all of the projects in an msbuild log file which had test failures.
