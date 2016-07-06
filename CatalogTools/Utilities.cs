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
using System.IO;
using ESRI.ArcGIS.esriSystem;
using Filestream = System.IO.FileStream;


namespace CatalogTools
{
    class Utilities
    {
        private static ITrackCancel trackCancel = null;
        internal static string tempFileName = "GetechCatalogTools.txt";
        internal static string tempFilePath = Environment.GetEnvironmentVariable("TEMP", EnvironmentVariableTarget.User);

        public static ITrackCancel TrackCancel
        {
            get { return Utilities.trackCancel; }
            set { Utilities.trackCancel = value; }
        }

        internal Utilities()
        {
        }

        /// <summary>
        /// Cancel current job if tracker has been set
        /// </summary>
        public static void Cancel()
        {
            if (trackCancel != null)
                trackCancel.Cancel();
        }

        /// <summary>
        /// Get current job status if tracker has been set
        /// </summary>
        /// <returns>True if job is running, false if not running or tracker not set</returns>
        public static bool Continue()
        {
            if (trackCancel != null)
                return trackCancel.Continue();

            return false;
        }

        public static string ReadFromTempFile()
        {
            string text = "";
            string filePath = Path.Combine(tempFilePath, tempFileName);

            if (File.Exists(filePath))
            {
                using (Filestream stream = new Filestream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (StreamReader reader = new StreamReader(stream))
                {
                    text = reader.ReadToEnd();
                    CatalogToolsExtension.WriteDebug(string.Format("Read {0} chars from {1}", text.Length, filePath));
                }
            }
            else
                CatalogToolsExtension.WriteWarning(string.Format("Could not read {0}, file not found.", filePath));

            return text;
        }

        public static void WriteToTempFile(string text)
        {
            string filePath = Path.Combine(tempFilePath, tempFileName); ;
            try
            {
                File.WriteAllText(filePath, text);
            }
            catch (Exception ex)
            {
                CatalogToolsExtension.WriteWarning(string.Format("Problem writing {0}: {1}", filePath, ex.Message));
            }
        }
    }
}
