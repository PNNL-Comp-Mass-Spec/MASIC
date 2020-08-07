using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using MASIC.Options;
using PRISM;
using PRISM.FileProcessor;
using PRISM.Logging;

#if GUI
using System.Windows.Forms;
using ProgressFormNET;
#endif

namespace MASIC
{
    // See clsMASIC for a program description
    //
    // -------------------------------------------------------------------------------
    // Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA)
    // Program started October 11, 2003
    // Copyright 2005, Battelle Memorial Institute.  All Rights Reserved.

    // E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov
    // Website: https://omics.pnl.gov/ or https://panomics.pnnl.gov/
    // -------------------------------------------------------------------------------
    //
    // Licensed under the 2-Clause BSD License; you may not use this file except
    // in compliance with the License.  You may obtain a copy of the License at
    // https://opensource.org/licenses/BSD-2-Clause

    public static class Program
    {
        public const string PROGRAM_DATE = "July 28, 2020";

        private static string mInputFilePath;
        private static string mOutputDirectoryPath;                         // Optional
        private static string mParameterFilePath;                           // Optional
        private static string mOutputDirectoryAlternatePath;                // Optional
        private static bool mRecreateDirectoryHierarchyInAlternatePath;     // Optional

        private static string mDatasetLookupFilePath;                       // Optional
        private static int mDatasetID;

        private static bool mRecurseDirectories;
        private static int mMaxLevelsToRecurse;

        private static bool mLogMessagesToFile;
        private static string mLogFilePath = string.Empty;
        private static string mLogDirectoryPath = string.Empty;

        private static string mMASICStatusFilename = string.Empty;

        private static bool mCreateParamFile;
        private static string mExampleParamFilePath;

        private static bool mQuietMode;

        private static clsMASIC mMASIC;
#if GUI
        private static frmProgress mProgressForm;

        // Uncomment to test DPI scaling
        //[System.Runtime.InteropServices.DllImport("user32.dll")]
        //private static extern bool SetProcessDPIAware();
#endif

        private static DateTime mLastSubtaskProgressTime;
        private static DateTime mLastProgressReportTime;
        private static int mLastProgressReportValue;

        /// <summary>
        /// Entry method
        /// </summary>
        /// <returns>0 if no error; error code if an error</returns>
        [STAThread]
        public static int Main()
        {
#if GUI
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Uncomment to test DPI scaling
            //if (Environment.OSVersion.Version.Major >= 6)
            //    SetProcessDPIAware();
#endif
            var commandLineParser = new clsParseCommandLine();

            mInputFilePath = string.Empty;
            mOutputDirectoryPath = string.Empty;
            mParameterFilePath = string.Empty;

            mRecurseDirectories = false;
            mMaxLevelsToRecurse = 0;

            mQuietMode = false;
            mLogMessagesToFile = false;
            mLogFilePath = string.Empty;

            mLastSubtaskProgressTime = DateTime.UtcNow;

            try
            {
                var proceed = false;
                if (commandLineParser.ParseCommandLine())
                {
                    if (SetOptionsUsingCommandLineParameters(commandLineParser))
                        proceed = true;
                }

                if (commandLineParser.ParameterCount + commandLineParser.NonSwitchParameterCount == 0 && !commandLineParser.NeedToShowHelp)
                {
#if GUI
                    ShowGUI();
#else
                    ShowProgramHelp();
#endif
                    return 0;
                }

                if (mCreateParamFile)
                {
                    CreateExampleParameterFile();
                    return 0;
                }

                if (!proceed || commandLineParser.NeedToShowHelp || mInputFilePath.Length == 0)
                {
                    ShowProgramHelp();
                    return -1;
                }

                var returnCode = ProcessFiles();

                if (returnCode != 0)
                {
                    ProgRunner.SleepMilliseconds(1500);
                }

                return returnCode;
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error occurred in Program->Main: " + Environment.NewLine + ex.Message);
                ProgRunner.SleepMilliseconds(1500);
                return -1;
            }
#if GUI
            finally
            {
                if (mProgressForm != null)
                {
                    mProgressForm.HideForm();
                    mProgressForm = null;
                }
            }
#endif
        }

        private static void CreateExampleParameterFile()
        {
            var masicProcessor = new clsMASIC();
            RegisterMasicEvents(masicProcessor);

            masicProcessor.CreateExampleParameterFile(mExampleParamFilePath);
        }

        private static void DisplayProgressPercent(int percentComplete, bool addCarriageReturn)
        {
            if (addCarriageReturn)
            {
                Console.WriteLine();
            }

            if (percentComplete > 100)
                percentComplete = 100;

            Console.Write("Processing: " + percentComplete.ToString() + "% ");
            if (addCarriageReturn)
            {
                Console.WriteLine();
            }
        }

        private static string GetAppVersion()
        {
            return ProcessFilesOrDirectoriesBase.GetAppVersion(PROGRAM_DATE);
        }

        private static int ProcessFiles()
        {
            mMASIC = new clsMASIC();
            RegisterMasicEvents(mMASIC);

            mMASIC.Options.DatasetLookupFilePath = mDatasetLookupFilePath;
            mMASIC.Options.SICOptions.DatasetID = mDatasetID;

            if (!string.IsNullOrEmpty(mMASICStatusFilename))
            {
                mMASIC.Options.MASICStatusFilename = mMASICStatusFilename;
            }

            mMASIC.LogMessagesToFile = mLogMessagesToFile;
            mMASIC.LogFilePath = mLogFilePath;
            mMASIC.LogDirectoryPath = mLogDirectoryPath;

            if (!mQuietMode)
            {
#if GUI
                    mProgressForm = new frmProgress();

                    mProgressForm.InitializeProgressForm("Parsing " + Path.GetFileName(mInputFilePath), 0, 100, false, true);
                    mProgressForm.InitializeSubtask(string.Empty, 0, 100, false);
                    mProgressForm.ResetKeyPressAbortProcess();
                    mProgressForm.Show();
                    Application.DoEvents();
#else
                Console.WriteLine("Parsing " + Path.GetFileName(mInputFilePath));
#endif
            }

            if (mRecurseDirectories)
            {
                // This call will lead to calls to method ProcessFile in clsMASIC
                if (mMASIC.ProcessFilesAndRecurseDirectories(mInputFilePath, mOutputDirectoryPath,
                                                             mOutputDirectoryAlternatePath, mRecreateDirectoryHierarchyInAlternatePath,
                                                             mParameterFilePath, mMaxLevelsToRecurse))
                {
                    return 0;
                }

                return (int)mMASIC.ErrorCode;
            }

            if (mMASIC.ProcessFilesWildcard(mInputFilePath, mOutputDirectoryPath, mParameterFilePath))
            {
                return 0;
            }

            var returnCode = (int)mMASIC.ErrorCode;
            if (returnCode != 0)
            {
                Console.WriteLine("Error while processing: " + mMASIC.GetErrorMessage());
            }

            return returnCode;
        }

        private static void RegisterEvents(IEventNotifier sourceClass)
        {
            sourceClass.StatusEvent += StatusEventHandler;
            sourceClass.DebugEvent += DebugEventHandler;
            sourceClass.ErrorEvent += ErrorEventHandler;
            sourceClass.WarningEvent += WarningEventHandler;
        }

        private static void RegisterMasicEvents(clsMASIC sourceClass)
        {
            RegisterEvents(sourceClass);

            sourceClass.ProgressUpdate += ProgressUpdateHandler;
            sourceClass.ProgressResetKeypressAbort += ProgressResetKeypressAbortHandler;
            sourceClass.ProgressSubtaskChanged += ProgressSubtaskChangedHandler;
        }

        /// <summary>
        /// Set options using command line arguments
        /// </summary>
        /// <param name="commandLineParser"></param>
        /// <returns>True if no problems; otherwise, false</returns>
        private static bool SetOptionsUsingCommandLineParameters(clsParseCommandLine commandLineParser)
        {
            var validParameters = new List<string> {
                "I", "O", "P", "D", "S", "A", "R", "L", "Log", "SF", "LogDir", "LogFolder", "CreateParamFile", "Q"
            };

            try
            {
                // Make sure no invalid parameters are present
                if (commandLineParser.InvalidParametersPresent(validParameters))
                {
                    ShowErrorMessage("Invalid command line parameters",
                        (from item in commandLineParser.InvalidParameters(validParameters) select ("/" + item)).ToList());
                    return false;
                }

                // Query commandLineParser to see if various parameters are present
                if (commandLineParser.RetrieveValueForParameter("I", out var inputFilePath))
                {
                    mInputFilePath = inputFilePath;
                }
                else if (commandLineParser.NonSwitchParameterCount > 0)
                {
                    // Treat the first non-switch parameter as the input file
                    mInputFilePath = commandLineParser.RetrieveNonSwitchParameter(0);
                }

                if (commandLineParser.RetrieveValueForParameter("O", out var outputDirectoryPath))
                    mOutputDirectoryPath = outputDirectoryPath;

                if (commandLineParser.RetrieveValueForParameter("P", out var parameterFilePath))
                    mParameterFilePath = parameterFilePath;

                if (commandLineParser.RetrieveValueForParameter("D", out var datasetIdOrLookupFile))
                {
                    if (int.TryParse(datasetIdOrLookupFile, out var datasetId))
                    {
                        mDatasetID = datasetId;
                    }
                    else if (!string.IsNullOrWhiteSpace(datasetIdOrLookupFile))
                    {
                        // Assume the user specified a dataset number lookup file comma, space, or tab delimited delimited file specifying the dataset number for each input file)
                        mDatasetLookupFilePath = datasetIdOrLookupFile;
                        mDatasetID = 0;
                    }
                }

                if (commandLineParser.RetrieveValueForParameter("S", out var recursionDepth))
                {
                    mRecurseDirectories = true;
                    if (int.TryParse(recursionDepth, out var levelsToRecurse))
                    {
                        mMaxLevelsToRecurse = levelsToRecurse;
                    }
                }

                if (commandLineParser.RetrieveValueForParameter("A", out var alternateOutputDirectory))
                    mOutputDirectoryAlternatePath = alternateOutputDirectory;

                if (commandLineParser.IsParameterPresent("R"))
                    mRecreateDirectoryHierarchyInAlternatePath = true;

                var logFileName = string.Empty;
                var logToFile = false;

                if (commandLineParser.IsParameterPresent("L"))
                {
                    logToFile = commandLineParser.RetrieveValueForParameter("L", out logFileName);
                }
                else if (commandLineParser.IsParameterPresent("Log"))
                {
                    logToFile = commandLineParser.RetrieveValueForParameter("Log", out logFileName);
                }

                if (logToFile)
                {
                    mLogMessagesToFile = true;
                    if (!string.IsNullOrEmpty(logFileName))
                    {
                        mLogFilePath = logFileName.Trim('"');
                    }
                }

                if (commandLineParser.RetrieveValueForParameter("SF", out var masicStatusFile))
                {
                    mMASICStatusFilename = masicStatusFile;
                }

                if (commandLineParser.RetrieveValueForParameter("LogDir", out var logDirectoryPath))
                {
                    mLogMessagesToFile = true;
                    if (!string.IsNullOrEmpty(logDirectoryPath))
                    {
                        mLogDirectoryPath = logDirectoryPath;
                    }
                }

                if (commandLineParser.RetrieveValueForParameter("LogFolder", out var logFolderPath))
                {
                    mLogMessagesToFile = true;
                    if (!string.IsNullOrEmpty(logFolderPath))
                    {
                        mLogDirectoryPath = logFolderPath;
                    }
                }

                if (commandLineParser.RetrieveValueForParameter("CreateParamFile", out var exampleParamFilePath))
                {
                    mCreateParamFile = true;
                    if (!string.IsNullOrEmpty(exampleParamFilePath))
                    {
                        mExampleParamFilePath = exampleParamFilePath;
                    }
                }

                if (commandLineParser.IsParameterPresent("Q"))
                    mQuietMode = true;

                return true;
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error parsing the command line parameters: " + Environment.NewLine + ex.Message);
            }

            return false;
        }

        private static void ShowErrorMessage(string message, Exception ex = null)
        {
            ConsoleMsgUtils.ShowError(message, ex);
        }

        private static void ShowErrorMessage(string title, IEnumerable<string> errorMessages)
        {
            ConsoleMsgUtils.ShowErrors(title, errorMessages);
        }

#if GUI
        public static void ShowGUI()
        {
            Application.EnableVisualStyles();
            Application.DoEvents();

            var mainWindow = new frmMain();

            // The following call is needed because the .ShowDialog() call is inexplicably increasing the size of the form
            mainWindow.SetHeightAdjustForce(mainWindow.Height);

            mainWindow.ShowDialog();
        }
#endif

        private static void ShowProgramHelp()
        {
            try
            {
                Console.WriteLine(ConsoleMsgUtils.WrapParagraph(
                    "This program will read a Thermo .RAW file, .mzML file, .mzXML file, or Agilent LC/MSD .CDF/.MGF file combo " +
                    "and create a selected ion chromatogram (SIC) for each parent ion. " +
                    "It also supports extracting reporter ion intensities (e.g. iTRAQ or TMT), " +
                    "and additional metadata from mass spectrometer data files."));

                Console.WriteLine();

                Console.WriteLine("Program syntax:" + Environment.NewLine + Path.GetFileName(ProcessFilesOrDirectoriesBase.GetAppPath()));
                Console.WriteLine(" /I:InputFilePath [/O:OutputDirectoryPath]");
                Console.WriteLine(" [/P:ParamFilePath] [/D:DatasetID or DatasetLookupFilePath] ");
                Console.WriteLine(" [/S:[MaxLevel]] [/A:AlternateOutputDirectoryPath] [/R]");
                Console.WriteLine(" [/L:[LogFilePath]] [/LogDir:LogDirPath] [/SF:StatusFileName]");
                Console.WriteLine(" [/CreateParamFile:FileName.xml] [/Q]");
                Console.WriteLine();

                Console.WriteLine("The input file path can contain the wildcard character *");
                Console.WriteLine("It will typically point to an instrument data file (e.g. Thermo .raw file)");
                Console.WriteLine();
                Console.WriteLine(ConsoleMsgUtils.WrapParagraph(
                    // ReSharper disable once StringLiteralTypo
                    "If the input file name ends with _SICstats.txt, MASIC will create plots using existing results. " +
                    "If a _ReporterIons.txt file is also present, the Reporter Ion Observation Rate plots will also be made"));
                Console.WriteLine();

                Console.WriteLine(ConsoleMsgUtils.WrapParagraph(
                    "The output directory name is optional. " +
                    "If omitted, the output files will be created in the same directory as the input file. " +
                    "If included, a subdirectory will be created with the name OutputDirectoryName."));
                Console.WriteLine();

                Console.WriteLine(ConsoleMsgUtils.WrapParagraph(
                    "The parameter file switch /P is optional. " +
                    "If supplied, it should point to a valid MASIC XML parameter file.  If omitted, defaults are used." +
                    "Create a parameter file for MASIC using the MASIC GUI. " +
                    "Alternatively, download a file from GitHub: https://github.com/PNNL-Comp-Mass-Spec/MASIC/tree/master/Docs/Parameter_Files"));

                Console.WriteLine();

                Console.WriteLine(ConsoleMsgUtils.WrapParagraph(
                    "The /D switch can be used to specify the Dataset ID of the input file; if omitted, 0 will be used"));
                Console.WriteLine();

                Console.WriteLine(ConsoleMsgUtils.WrapParagraph(
                    "Alternatively, a lookup file can be specified with the /D switch (useful if processing multiple files using * or /S). " +
                    "The lookup file is a comma, space, or tab delimited file with two columns:" + Environment.NewLine + "Dataset Name and Dataset ID"));
                Console.WriteLine();

                Console.WriteLine(ConsoleMsgUtils.WrapParagraph(
                    "Use /S to process all valid files in the input directory and subdirectories. " +
                    "Include a number after /S (like /S:2) to limit the level of subdirectories to examine."));

                Console.WriteLine("When using /S, you can redirect the output of the results using /A.");
                Console.WriteLine(ConsoleMsgUtils.WrapParagraph(
                    "When using /S, you can use /R to re-create the input directory hierarchy in the alternate output directory (if defined)."));
                Console.WriteLine();

                Console.WriteLine(ConsoleMsgUtils.WrapParagraph(
                    "Use /L or /Log to specify that a log file should be created. " +
                    "Use /L:LogFilePath to specify the name (or full path) for the log file."));
                Console.WriteLine();

                Console.WriteLine(ConsoleMsgUtils.WrapParagraph(
                    "Use /SF to specify the name to use for the Masic Status file (default is " + MASICOptions.DEFAULT_MASIC_STATUS_FILE_NAME + ")."));
                Console.WriteLine();

                Console.WriteLine("Use /CreateParamFile to create an example parameter file named MASIC_ExampleSettings.xml");
                Console.WriteLine("Include a colon and a filename to customize the filename");
                Console.WriteLine();

                Console.WriteLine("The optional /Q switch will prevent the progress window from being shown");
                Console.WriteLine();

                Console.WriteLine("Program written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA) in 2003");
                Console.WriteLine("Version: " + GetAppVersion());

                Console.WriteLine();

                Console.WriteLine("E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov");
                Console.WriteLine("Website: https://omics.pnl.gov/ or https://panomics.pnnl.gov/");
                Console.WriteLine();

                // Delay for 750 msec in case the user double clicked this file from within Windows Explorer (or started the program via a shortcut)
                Thread.Sleep(750);
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error displaying the program syntax: " + ex.Message);
            }
        }

        private static void ProgressUpdateHandler(string taskDescription, float percentComplete)
        {
            const int PROGRESS_DOT_INTERVAL_MSEC = 250;

#if GUI
            const int PERCENT_REPORT_INTERVAL = 25;

            if (mProgressForm != null)
            {
                mProgressForm.UpdateCurrentTask(mMASIC.ProgressStepDescription);
                mProgressForm.UpdateProgressBar(percentComplete);
                if (mProgressForm.KeyPressAbortProcess)
                {
                    mMASIC.AbortProcessingNow();
                }

                Application.DoEvents();
                return;
            }
#else
            const int PERCENT_REPORT_INTERVAL = 5;
#endif

            if (percentComplete >= mLastProgressReportValue)
            {
                Console.WriteLine();
                DisplayProgressPercent(mLastProgressReportValue, false);
                Console.WriteLine();
                mLastProgressReportValue += PERCENT_REPORT_INTERVAL;
                mLastProgressReportTime = DateTime.UtcNow;
            }
            else if (DateTime.UtcNow.Subtract(mLastProgressReportTime).TotalMilliseconds > PROGRESS_DOT_INTERVAL_MSEC)
            {
                mLastProgressReportTime = DateTime.UtcNow;
                Console.Write(".");
            }
        }

        private static void ProgressResetKeypressAbortHandler()
        {
#if GUI
            mProgressForm?.ResetKeyPressAbortProcess();
#endif
        }

        private static void ProgressSubtaskChangedHandler()
        {
#if GUI
            if (mProgressForm != null)
            {
                mProgressForm.UpdateCurrentSubTask(mMASIC.SubtaskDescription);
                mProgressForm.UpdateSubtaskProgressBar(mMASIC.SubtaskProgressPercentComplete);
                if (mProgressForm.KeyPressAbortProcess)
                {
                    mMASIC.AbortProcessingNow();
                }

                Application.DoEvents();
                return;
            }
#endif

            if (DateTime.UtcNow.Subtract(mLastSubtaskProgressTime).TotalSeconds < 10)
                return;
            mLastSubtaskProgressTime = DateTime.UtcNow;

            ConsoleMsgUtils.ShowDebug("{0}: {1}%", mMASIC.SubtaskDescription, mMASIC.SubtaskProgressPercentComplete);
        }

        private static void StatusEventHandler(string message)
        {
            Console.WriteLine(message);
        }

        private static void DebugEventHandler(string message)
        {
            ConsoleMsgUtils.ShowDebug(message);
        }

        private static void ErrorEventHandler(string message, Exception ex)
        {
            ShowErrorMessage(message, ex);
        }

        private static void WarningEventHandler(string message)
        {
            ConsoleMsgUtils.ShowWarning(message);
        }
    }
}
