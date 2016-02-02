using System;
using System.Diagnostics;
using System.IO;
using Microsoft.VisualBasic.Logging;


namespace CatalogTools
{
    class Utilities
    {
        public bool verboseLogging = true;
        FileLogTraceListener logFile = null;

        internal Utilities()
        {
        }

        ~Utilities()
        {
            WriteDebug("shutdown utilities");
            if (logFile != null)
                logFile.Close();
        }

        private bool GetRegValue(string keyName, string valueName, out string returnValue)
        {
            object regValue = null;

            regValue = Microsoft.Win32.Registry.GetValue(keyName, valueName, null);
            if (regValue != null)
            {
                returnValue = regValue.ToString();
                return true;
            }

            // try again in 32bit reg
            regValue = Microsoft.Win32.Registry.GetValue(keyName.Replace(@"HKEY_LOCAL_MACHINE\Software", @"HKEY_LOCAL_MACHINE\Software\Wow6432Node"), valueName, null);
            if (regValue != null)
            {
                returnValue = regValue.ToString();
                return true;
            }

            returnValue = "";
            return false;
        }

        private void SetupLogFile()
        {
            logFile = new FileLogTraceListener();
            // set name and location
            logFile.BaseFileName = string.Format("CatalogToolsDebug_{0}", Process.GetCurrentProcess().Id);
            logFile.CustomLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Path.Combine("Getech", "CatalogTools"));
            logFile.Location = LogFileLocation.Custom;
            // flush after each write
            logFile.AutoFlush = true;
            // create new file every day with datestamp
            logFile.LogFileCreationSchedule = LogFileCreationScheduleOption.Daily;
            // start logging
            WriteLog("Started CatalogTools " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);

            // try and delete old logs, but don't worry if it fails
            WriteDebug("Deleting old logs");
            try
            {
                string logFolder = logFile.CustomLocation;
                DateTime maxAge = DateTime.Now.AddMonths(-6);

                foreach (string log in Directory.GetFiles(logFolder, "*.log", SearchOption.TopDirectoryOnly))
                {
                    FileInfo logInfo = new FileInfo(log);
                    if (logInfo.CreationTime < maxAge)
                        logInfo.Delete();
                }

            }
            catch (Exception ex)
            {
                WriteWarning(string.Format("Problem deleting old logs: {0}", ex.Message));
            }
        }

        /// <summary>
        /// Write detailed information to the log file.
        /// Text will only be written if debug flag is set.
        /// </summary>
        /// <param name="msg">text to write to log</param>
        public void WriteDebug(string msg)
        {
            if (verboseLogging)
                WriteLog(msg, TraceLevel.Verbose);
        }

        public void WriteLog(string msg)
        {
            WriteLog(msg, TraceLevel.Info);
        }

        public void WriteWarning(string msg)
        {
            WriteLog(msg, TraceLevel.Warning);
        }

        public void WriteError(string msg)
        {
            WriteLog(msg, TraceLevel.Error);
        }

        public void WriteLog(string msg, TraceLevel level)
        {
            try
            {
                switch (level)
                {
                    // send important messages to the console
                    case TraceLevel.Warning:
                    case TraceLevel.Error:
                    case TraceLevel.Verbose:
                        Debug.WriteLine(msg);
                        break;
                    default:
                        break;
                }
                // then log to file with timestamp and level
                if (logFile == null)
                    SetupLogFile();
                logFile.WriteLine(string.Format("{0}[{1}] {2}", DateTime.Now.ToString("[yyyy/MM/dd][HH:mm:ss:fff]"), level, msg));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(string.Format("ERROR:{0}\n{1}", ex.Message, ex.StackTrace));
            }
        }

    }
}
