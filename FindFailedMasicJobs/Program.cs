using PRISM;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace FindFailedMasicJobs
{
    /// <summary>
    /// This program searches for MASIC related error messages in Analysis Manager log files
    /// </summary>
    internal static class Program
    {
        private static readonly Regex mErrorMessageMatcher = new(@"\t[a-z]+\t(?<ErrorMessage>.+), WARN", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex mJobStartMatcher = new(@"Started analysis job (?<Job>\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex mStartMasicMatcher = new(@"Calling MASIC to create the SIC files, job (?<Job>\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex mJobEndMatcher = new(@"Completed job (?<Job>\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static void Main()
        {
            try
            {
                var workingDirectory = new DirectoryInfo(".");
                if (workingDirectory.Parent == null)
                {
                    ConsoleMsgUtils.ShowError("Cannot determine the parent directory of the working directory: " + workingDirectory.FullName);
                    return;
                }

                var inputFileCandidates = new List<FileInfo>
                {
                    new(Path.Combine(workingDirectory.FullName, "DirectoriesToSearch.txt")),
                    new(Path.Combine(workingDirectory.Parent.FullName, "DirectoriesToSearch.txt")),
                    new(Path.Combine(workingDirectory.Parent.FullName, "Data", "DirectoriesToSearch.txt"))
                };

                var directoriesToSearch = ReadDirectoryFile(inputFileCandidates);

                SearchForErrors(directoriesToSearch);

                Console.WriteLine("Search complete");
            }
            catch (Exception ex)
            {
                ConsoleMsgUtils.ShowError("Error searching log files", ex);
            }
        }

        private static SortedSet<string> ReadDirectoryFile(List<FileInfo> inputFileCandidates)
        {
            var directoriesToSearch = new SortedSet<string>();

            try
            {
                foreach (var item in inputFileCandidates)
                {
                    if (!item.Exists)
                        continue;

                    using var reader = new StreamReader(new FileStream(item.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));

                    while (!reader.EndOfStream)
                    {
                        var dataLine = reader.ReadLine();

                        if (string.IsNullOrWhiteSpace(dataLine) || directoriesToSearch.Contains(dataLine))
                            continue;

                        directoriesToSearch.Add(dataLine);
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleMsgUtils.ShowError("Error in ReadDirectoryFile", ex);
            }
            return directoriesToSearch;
        }

        private static void SearchForErrors(SortedSet<string> directoriesToSearch)
        {
            try
            {
                var outputFilePath = new FileInfo("MASICJobErrors.txt");

                using var resultsWriter = new StreamWriter(new FileStream(outputFilePath.FullName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite));

                foreach (var item in directoriesToSearch)
                {
                    var inputDirectory = new DirectoryInfo(item);
                    if (!inputDirectory.Exists)
                    {
                        ConsoleMsgUtils.ShowWarning("Directory not found: " + item);
                        continue;
                    }

                    foreach (var inputFile in inputDirectory.GetFiles("*.txt", SearchOption.AllDirectories))
                    {
                        SearchForErrors(inputFile, resultsWriter);
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleMsgUtils.ShowError("Error in SearchForErrors", ex);
            }
        }

        private static void SearchForErrors(FileSystemInfo inputFile, TextWriter resultsWriter)
        {
            try
            {
                var currentJob = string.Empty;

                using var reader = new StreamReader(new FileStream(inputFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));

                while (!reader.EndOfStream)
                {
                    var dataLine = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(dataLine))
                        continue;

                    var jobStart = mJobStartMatcher.Match(dataLine);
                    if (jobStart.Success)
                    {
                        currentJob = jobStart.Groups["Job"].Value;
                        continue;
                    }
                    var masicStart = mStartMasicMatcher.Match(dataLine);
                    if (masicStart.Success)
                    {
                        currentJob = masicStart.Groups["Job"].Value;
                        continue;
                    }

                    var jobEnd = mJobEndMatcher.Match(dataLine);
                    if (jobEnd.Success)
                    {
                        currentJob = jobEnd.Groups["Job"].Value;
                        continue;
                    }

                    if (dataLine.IndexOf("Errors found in the MASIC Log File", StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    // Read the next line to look for an error message

                    var msgLine = reader.EndOfStream ? "Unknown error" : reader.ReadLine();

                    var messageMatch = mErrorMessageMatcher.Match(msgLine ?? string.Empty);

                    var messageDetail = messageMatch.Success ? messageMatch.Groups["ErrorMessage"].Value : msgLine;

                    var errorMessage = string.Format("Error in job {0}: {1}", currentJob, messageDetail);
                    ConsoleMsgUtils.ShowWarning(errorMessage);

                    resultsWriter.WriteLine(errorMessage);
                }
            }
            catch (Exception ex)
            {
                ConsoleMsgUtils.ShowError("Error in SearchForErrors", ex);
            }
        }
    }
}