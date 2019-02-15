﻿using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Clockwise;
using MLS.Agent.Tools;

namespace WorkspaceServer
{
    public class Dotnet
    {
        private readonly DirectoryInfo _workingDirectory;

        public Dotnet(DirectoryInfo workingDirectory = null)
        {
            _workingDirectory = workingDirectory ??
                                new DirectoryInfo(Directory.GetCurrentDirectory());
        }

        public Task<CommandLineResult> New(string templateName, string args = null, Budget budget = null)
        {
            if (string.IsNullOrWhiteSpace(templateName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(templateName));
            }

            return Execute($"new {templateName} {args}", budget);
        }

        public Task<CommandLineResult> AddPackage(string packageId, string version = null, Budget budget = null)
        {
            var versionArg = string.IsNullOrWhiteSpace(version)
                ? ""
                : $"--version {version}";

            return Execute($"add package {versionArg} {packageId}", budget);
        }

        public Task<CommandLineResult> Build(string args = null, Budget budget = null) =>
            Execute("build".AppendArgs(args), budget);

        public Task<CommandLineResult> Execute(string args, Budget budget = null) =>
            CommandLine.Execute(
                Path,
                args,
                _workingDirectory,
                budget);

        public Task<CommandLineResult> Publish(string args, Budget budget = null) =>
            Execute("publish".AppendArgs(args), budget);

        public Task<CommandLineResult> VSTest(string args, Budget budget = null) =>
            Execute("vstest".AppendArgs(args), budget);

        public Task<CommandLineResult> ToolInstall(string args = null, Budget budget = null) =>
            Execute("tool install".AppendArgs(args), budget);

        public Task<CommandLineResult> ToolInstall(
            string packageName, 
            string toolPath,
            DirectoryInfo addSource = null, Budget budget = null)
        {
            var args = $"{packageName} --tool-path {toolPath} --version 1.0.0";
            if (addSource != null)
            {
                args += $" --add-source \"{addSource}\"";
            }

            return Execute("tool install".AppendArgs(args), budget);
        }

        public Task<CommandLineResult> Pack(string args = null, Budget budget = null) =>
            Execute("pack".AppendArgs(args), budget);

        private static readonly Lazy<FileInfo> _getPath = new Lazy<FileInfo>(() =>
                                                                                 FindDotnetFromAppContext() ??
                                                                                 FindDotnetFromPath());

        public static FileInfo Path => _getPath.Value;

        private static FileInfo FindDotnetFromPath()
        {
            FileInfo fileInfo = null;

            using (var process = Process.Start("dotnet"))
            {
                if (process != null)
                {
                    fileInfo = new FileInfo(process.MainModule.FileName);
                }
            }

            return fileInfo;
        }

        private static FileInfo FindDotnetFromAppContext()
        {
            var muxerFileName = "dotnet".ExecutableName();

            var fxDepsFile = GetDataFromAppDomain("FX_DEPS_FILE");

            if (!string.IsNullOrEmpty(fxDepsFile))
            {
                var muxerDir = new FileInfo(fxDepsFile).Directory?.Parent?.Parent?.Parent;

                if (muxerDir != null)
                {
                    var muxerCandidate = new FileInfo(System.IO.Path.Combine(muxerDir.FullName, muxerFileName));

                    if (muxerCandidate.Exists)
                    {
                        return muxerCandidate;
                    }
                }
            }

            return null;
        }

        public static string GetDataFromAppDomain(string propertyName)
        {
            var appDomainType = typeof(object).GetTypeInfo().Assembly?.GetType("System.AppDomain");
            var currentDomain = appDomainType?.GetProperty("CurrentDomain")?.GetValue(null);
            var deps = appDomainType?.GetMethod("GetData")?.Invoke(currentDomain, new[] { propertyName });
            return deps as string;
        }
    }
}