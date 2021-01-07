// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;

using Microsoft.Build.Definition;
using System.Diagnostics;

using BenchmarkDotNet.Running;
using BenchmarkDotNet.Attributes;

namespace Microsoft.Build.CommandLine
{
    [MemoryDiagnoser]
    public class Evaluation_Benchmark
    {
        Project _project1;
        Project _project2;

        [GlobalSetup]
        public void GlobalSetup()
        {
            ProjectOptions options = new ProjectOptions();
            _project1 = Project.FromFile(@"C:\src\msbuild2\src\Framework\Microsoft.Build.Framework.csproj", options);
            _project2 = Project.FromFile(@"C:\src\_test2\AnotherConsole\AnotherConsole.csproj", options);
        }

        [Benchmark]
        public void Project1()
        {
            _project1.MarkDirty();
            _ = _project1.CreateProjectInstance();
        }

        [Benchmark]
        public void Project2()
        {
            _project2.MarkDirty();
            _ = _project2.CreateProjectInstance();
        }
    }

    /// <summary>
    /// This class implements the MSBuild.exe command-line application. It processes
    /// command-line arguments and invokes the build engine.
    /// </summary>
    static public class MSBuildApp
    {
        //private static void MeasureProject(string path)
        //{
        //    ProjectOptions options = new ProjectOptions();
        //    //Directory.SetCurrentDirectory(@"C:\src\_test2\AnotherConsole");
        //    Project project = Project.FromFile(path, options);

        //    const int ITERATIONS = 1000;

        //    project.MarkDirty();
        //    _ = project.CreateProjectInstance();

        //    Stopwatch sw = new Stopwatch();
        //    sw.Start();

        //    for (int i = 0; i < ITERATIONS; i++)
        //    {
        //        project.MarkDirty();
        //        _ = project.CreateProjectInstance();
        //    }

        //    sw.Stop();
        //    Console.WriteLine("{0}: {1}", path, sw.ElapsedMilliseconds);
        //}

        /// <summary>
        /// This is the entry point for the application.
        /// </summary>
        /// <remarks>
        /// MSBuild no longer runs any arbitrary code (tasks or loggers) on the main thread, so it never needs the
        /// main thread to be in an STA. Accordingly, to avoid ambiguity, we explicitly use the [MTAThread] attribute.
        /// This doesn't actually do any work unless COM interop occurs for some reason.
        /// </remarks>
        /// <returns>0 on success, 1 on failure</returns>
        public static int Main()
        {
            BenchmarkRunner.Run<Evaluation_Benchmark>();

            return 0;
        }
    }
}
