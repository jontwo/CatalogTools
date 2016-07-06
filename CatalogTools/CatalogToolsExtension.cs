/*
 * CatalogTools - toolkit for ArcCatalog
 * Copyright (C) 2015 Jon Morris
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 *  
 */

using System;
using System.Diagnostics;
using System.IO;
using Microsoft.VisualBasic.Logging;

namespace CatalogTools
{
    public class CatalogToolsExtension : ESRI.ArcGIS.Desktop.AddIns.Extension
    {
        public static bool verboseLogging = true;
        static FileLogTraceListener logFile = null;

        public CatalogToolsExtension()
        {
        }

        protected override void OnStartup()
        {
        }

        protected override void OnShutdown()
        {
            WriteDebug("Shutdown CatalogTools");
            if (logFile != null)
                logFile.Close();
        }

        /// <summary>
        /// Create a new log file under the Roaming folder and delete any logs that are more than 6 months old
        /// </summary>
        private static void SetupLogFile()
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
        public static void WriteDebug(string msg)
        {
            if (verboseLogging)
                WriteLog(msg, TraceLevel.Verbose);
        }

        /// <summary>
        /// Write to the log at Info level
        /// </summary>
        /// <param name="msg">text to write to log</param>
        public static void WriteLog(string msg)
        {
            WriteLog(msg, TraceLevel.Info);
        }

        /// <summary>
        /// Write to the log at Warning level
        /// </summary>
        /// <param name="msg">text to write to log</param>
        public static void WriteWarning(string msg)
        {
            WriteLog(msg, TraceLevel.Warning);
        }

        /// <summary>
        /// Write to the log at Error level
        /// </summary>
        /// <param name="msg">text to write to log</param>
        public static void WriteError(string msg)
        {
            WriteLog(msg, TraceLevel.Error);
        }

        /// <summary>
        /// Write to the log at the given level.
        /// Create a new log if it has not been created already.
        /// </summary>
        /// <param name="msg">text to write to log</param>
        /// <param name="level">log level to write at</param>
        public static void WriteLog(string msg, TraceLevel level)
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
