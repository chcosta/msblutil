using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace msblutil
{
    class Program
    {
        private static string log;
        private static string arg1;
        private static Regex lineRegex = new Regex(@"(?<date>\d+:\d+:\d+\.\d+)\s+(?<proc>\d+(:\d+)?)\>(?<line>.*)");

        static void Main(string[] args)
        {
            string action = ParseArgs(args);

            if(action.Equals("split", StringComparison.OrdinalIgnoreCase))
            {
                SplitLog(log);
            }
            else if(action.Equals("grab", StringComparison.OrdinalIgnoreCase))
            {
                Regex typeRegex = new Regex(@"\d+(:\d+)?");
                Match m = typeRegex.Match(arg1);
                if (m.Success)
                {
                    GrabProc(log, arg1);
                }
                else
                {
                    var procs = GrabProj(log, arg1);
                    if (procs != null)
                    {
                        foreach (var proc in procs)
                        {
                            GrabProc(log, proc);
                        }
                    }
                
                }
            }
            else if(action.Equals("list", StringComparison.OrdinalIgnoreCase))
            {
                ListProcs(log);
            }
            else if(action.Equals("failures", StringComparison.OrdinalIgnoreCase))
            {
                ListFailures(log);
            }
            else
            {
                Usage();
            }
        }

        private static void ListFailures(string log)
        {
            var procProjects = GetProcs(log);
            var testFailures = GetFailures(log, procProjects);
            if (testFailures.Count > 0)
            {
                Console.WriteLine(Environment.NewLine + "Test Failures" + Environment.NewLine);
                Console.WriteLine("proc #\t: project");
                foreach (var procProject in testFailures.OrderBy(p => p.Value))
                {
                    Console.WriteLine("{0}\t: {1}", procProject.Key, procProject.Value);
                }
                Console.WriteLine(Environment.NewLine + "Found {0} test projects with failures", testFailures.Count);
            }
        }

        private static IEnumerable<string> GrabProj(string log, string proj)
        {
            var procProjects = GetProcs(log);
            var matchingProjects = procProjects.Where(p => p.Value.ToLower().Contains(proj.ToLower()));

            if(matchingProjects.Count() == 0)
            {
                Console.WriteLine("No matching projects found in log.");
            }
            else
            {
                return matchingProjects.Select(p => p.Key);
            }
            return null;
        }

        private static void ListProcs(string log)
        {
            var procProjects = GetProcs(log);
            Console.WriteLine("Projects" + Environment.NewLine);
            Console.WriteLine("proc #\t: project");
            foreach (var procProject in procProjects.OrderBy(p => p.Value))
            {
                Console.WriteLine("{0}\t: {1}", procProject.Key, procProject.Value);
            }

            Console.WriteLine(Environment.NewLine + "Found {0} projects", procProjects.Count);

        }

        private static Dictionary<string, string> GetProcs(string log)
        {
            Regex projRegex = new Regex("Done Building Project (?<project>\"[^\"]+proj\")");

            Dictionary<string, string> procProjects = new Dictionary<string, string>();
            using (StreamReader reader = File.OpenText(log))
            {
                string line;
                string proc = string.Empty;
                string proj = string.Empty;
                while (reader.Peek() != -1)
                {
                    line = reader.ReadLine();
                    Match m = lineRegex.Match(line);
                    if (m.Success)
                    {
                        proc = m.Groups["proc"].Value;
                        Match m2 = projRegex.Match(m.Groups["line"].Value);
                        if (m2.Success)
                        {
                            proj = m2.Groups["project"].Value;
                            procProjects.Add(proc, proj);
                        }
                    }
                }
            }
            return procProjects;
        }

        private static Dictionary<string, string> GetFailures(string log, Dictionary<string, string> procProjects)
        {
            Dictionary<string, string> testFailures = new Dictionary<string, string>();

            using (StreamReader reader = File.OpenText(log))
            {
                string line;
                string proc = string.Empty;
                string proj = string.Empty;
                while (reader.Peek() != -1)
                {
                    line = reader.ReadLine();
                    Match m = lineRegex.Match(line);
                    if (m.Success)
                    {
                        proc = m.Groups["proc"].Value;
                    }
                    if (line.Contains("One or more tests failed"))
                    {
                        if (!testFailures.ContainsKey(proc))
                        {
                            testFailures.Add(proc, procProjects[proc]);
                        }
                    }
                }
            }
            return testFailures;
        }

        private static string ParseArgs(string[] args)
        {
            if (args.Length == 0 ||
                args[0].Equals("/?") ||
                args[0].Equals("-?") ||
                args[0].Equals("-help"))
            {
                return string.Empty;
            }
            
            string action = args[0];

            if(action.Equals("split", StringComparison.OrdinalIgnoreCase))
            {
                log = args[1];
                return action;
            }
            if(action.Equals("grab", StringComparison.OrdinalIgnoreCase))
            {
                log = args[1];
                arg1 = args[2];
                return action;
            }
            if (action.Equals("list", StringComparison.OrdinalIgnoreCase))
            {
                log = args[1];
                return action;
            }
            if(action.Equals("failures", StringComparison.OrdinalIgnoreCase))
            {
                log = args[1];
                return action;
            }
            return string.Empty;
        }

        private static void Usage()
        {
            Console.WriteLine("Microft Build Log Utility Usage:" + Environment.NewLine);
            Console.WriteLine("msblutil split [msbuild log]");
            Console.WriteLine("  Split an msbuild log in half (by lines), generating logname_top.extension and logname_bottom.extension files." + Environment.NewLine);
            Console.WriteLine("msblutil grab [msbuild log] [proc #]");
            Console.WriteLine("  Gather all of the msbuild log entries for a specific process # and generate a new log file." + Environment.NewLine);
            Console.WriteLine("msblutil grab [msbuild log] [project name]");
            Console.WriteLine("  Find all projects matching the specified project name and generate new log files on a per process basis." + Environment.NewLine);
            Console.WriteLine("msblutil list [msbuild log]");
            Console.WriteLine("  List all of the projects and processes in an msbuild log file." + Environment.NewLine);
            Console.WriteLine("msblutil failures [msbuild log]");
            Console.WriteLine("  List all of the projects in an msbuild log file which had test failures." + Environment.NewLine);

        }

        private static void GrabProc(string log, string proc)
        {
            string sanitizeProc = proc.Replace(':', '-');
            string writeLog = Path.Combine(Path.GetDirectoryName(log), Path.GetFileNameWithoutExtension(log) + sanitizeProc + Path.GetExtension(log));
            using (StreamReader reader = File.OpenText(log))
            using (StreamWriter writer = File.CreateText(writeLog))
            {
                string line;
                bool inProcMatch = false;
                while(reader.Peek() != -1)
                {
                    line = reader.ReadLine();
                    Match m = lineRegex.Match(line);
                    if(m.Success)
                    {
                        string p = m.Groups["proc"].Value;
                        if(p.Equals(proc))
                        {
                            inProcMatch = true;
                        }
                        else
                        {
                            inProcMatch = false;
                        }
                    }
                    if(inProcMatch)
                    {
                        writer.WriteLine(line);
                    }
                }
                Console.WriteLine("Generated {0}", writeLog);
            }
        }

        private static void SplitLog(string log)
        {
            long lineCount = 0;

            // get a total line count
            using (StreamReader reader = File.OpenText(log))
            {
                while(reader.Peek() != -1)
                {
                    reader.ReadLine();
                    lineCount++;
                }
            }
            Console.WriteLine("Filename: {0}", log);
            Console.WriteLine("Line count: {0}", lineCount);

            int splitIndex = (int)(lineCount / 2);
            string topFile = Path.Combine(Path.GetDirectoryName(log), Path.GetFileNameWithoutExtension(log) + "_top" + Path.GetExtension(log));
            string bottomFile = Path.Combine(Path.GetDirectoryName(log), Path.GetFileNameWithoutExtension(log) + "_bottom" + Path.GetExtension(log));

            using (StreamReader reader = File.OpenText(log))
            {

                using (StreamWriter filestreamWriter = File.CreateText(topFile))
                {
                    for (int i = 0; i < splitIndex; i++)
                    {
                        string line = reader.ReadLine();
                        filestreamWriter.WriteLine(line);
                    }
                    Console.WriteLine("Generated file 1: {0}", topFile);
                }
                using (StreamWriter fileStreamWriter = File.CreateText(bottomFile))
                {
                    while (reader.Peek() != -1)
                    {
                        string line = reader.ReadLine();
                        fileStreamWriter.WriteLine(line);
                    }
                    Console.WriteLine("Generated file 2: {0}", bottomFile);
                }
            }
        }
    }
}
