using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using PRISM;

namespace MASICBrowser
{
    // -------------------------------------------------------------------------------
    // Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA)
    // Program started October 17, 2003
    // Copyright 2005, Battelle Memorial Institute.  All Rights Reserved.

    // E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov
    // Website: https://github.com/PNNL-Comp-Mass-Spec/ or https://panomics.pnnl.gov/ or https://www.pnnl.gov/integrative-omics
    // -------------------------------------------------------------------------------
    //
    // Licensed under the 2-Clause BSD License; you may Not use this file except
    // in compliance with the License.  You may obtain a copy of the License at
    // https://opensource.org/licenses/BSD-2-Clause

    internal static class Program
    {
        public const string PROGRAM_DATE = "November 8, 2021";

        private static string mInputFilePath;

        [STAThread]
        public static int Main()
        {
            // Returns 0 if no error, error code if an error

            var commandLineParser = new clsParseCommandLine();

            mInputFilePath = string.Empty;

            try
            {
                var proceed = false;
                if (commandLineParser.ParseCommandLine())
                {
                    if (SetOptionsUsingCommandLineParameters(commandLineParser))
                        proceed = true;
                }
                else if (!commandLineParser.NeedToShowHelp)
                {
                    proceed = true;
                }

                if (commandLineParser.NeedToShowHelp || !proceed)
                {
                    ShowProgramHelp();
                    return -1;
                }

                ShowGUI();

                return 0;
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error occurred in Program->Main: " + Environment.NewLine + ex.Message);
                return -1;
            }
        }

        /// <summary>
        /// Returns the full path to the executing .Exe or .Dll
        /// </summary>
        /// <returns>File path</returns>
        private static string GetAppPath()
        {
            return Assembly.GetExecutingAssembly().Location;
        }

        /// <summary>
        /// Returns the .NET assembly version followed by the program date
        /// </summary>
        /// <param name="programDate"></param>
        private static string GetAppVersion(string programDate)
        {
            return Assembly.GetExecutingAssembly().GetName().Version + " (" + programDate + ")";
        }

        private static bool SetOptionsUsingCommandLineParameters(clsParseCommandLine commandLineParser)
        {
            // Returns True if no problems; otherwise, returns false

            var validParameters = new List<string> { "I" };

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

                return true;
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error parsing the command line parameters: " + Environment.NewLine + ex.Message);
            }

            return false;
        }

        private static void ShowErrorMessage(string message)
        {
            ConsoleMsgUtils.ShowError(message);
        }

        private static void ShowErrorMessage(string title, IEnumerable<string> errorMessages)
        {
            ConsoleMsgUtils.ShowErrors(title, errorMessages);
        }

        public static void ShowGUI()
        {
            Application.EnableVisualStyles();
            Application.DoEvents();

            var masicBrowser = new frmBrowser();

            if (!string.IsNullOrWhiteSpace(mInputFilePath))
            {
                masicBrowser.FileToAutoLoad = mInputFilePath;
            }

            masicBrowser.ShowDialog();
        }

        private static void ShowProgramHelp()
        {
            try
            {
                Console.WriteLine("This program is used to visualize the results from MASIC");
                Console.WriteLine();

                Console.WriteLine("Program syntax:" + Environment.NewLine + Path.GetFileName(GetAppPath()));
                Console.WriteLine(" MasicResults_SICs.xml");
                Console.WriteLine();

                Console.WriteLine("The input file path is the MASIC results file to load");
                Console.WriteLine();

                Console.WriteLine("Program written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA)");
                Console.WriteLine("Version: " + GetAppVersion(PROGRAM_DATE));

                Console.WriteLine();

                Console.WriteLine("E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov");
                Console.WriteLine("Website: https://github.com/PNNL-Comp-Mass-Spec/ or https://panomics.pnnl.gov/ or https://www.pnnl.gov/integrative-omics");
                Console.WriteLine();

                // Delay for 750 msec in case the user double clicked this file from within Windows Explorer (or started the program via a shortcut)
                Thread.Sleep(750);
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error displaying the program syntax: " + ex.Message);
            }
        }
    }
}
