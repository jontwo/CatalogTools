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
using System.Collections.Generic;
using System.Windows.Forms;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Catalog;
using ESRI.ArcGIS.CatalogUI;

namespace CatalogTools
{
    public partial class CreateThumbnailForm : Form
    {
        private bool cancelled;
        private IGxApplication GxApplication = null;
        private enum CatalogViewTab { Contents, Preview, Metadata };
        private Utilities _utils = null;
        private int thumbCount = 0;

        public CreateThumbnailForm()
        {
            InitializeComponent();

            _utils = new Utilities();

            // reset cancel tracker
            cancelled = false;
            Utilities.TrackCancel = new TrackCancel();

            CreateThumbnails();
        }

        private void CreateThumbnails()
        {
            this.Focus();

            try
            {
                GetSelectedDatasets();
            }
            catch (Exception ex)
            {
                AddText(string.Format("Error: {0}. {1}", ex.Message, ex.StackTrace));
            }

            if (!cancelled && Utilities.Continue())
                AddText("Done");
            else
                AddText("Cancelled");

            if (thumbCount > 0)
                AddText(string.Format("{0} thumbnails created", thumbCount));

            // change cancel button into a close button
            cancelled = true;
            btnCancel.Text = "Close";
        }

        private void GetSelectedDatasets()
        {
            GxApplication = (IGxApplication)ArcCatalog.Application;
            IGxSelection gxSel = GxApplication.Selection;

            // gather all the selected objects in to a list so selection can be changed while processing
            List<IGxObject> selectedObjs = new List<IGxObject>();
            if (gxSel.Count > 0)
            {
                AddText(string.Format("{0} items selected", gxSel.Count));

                IEnumGxObject enumGxObj = GxApplication.Selection.SelectedObjects;
                enumGxObj.Reset();
                IGxObject gxObj = enumGxObj.Next();
                while (gxObj != null)
                {
                    selectedObjs.Add(gxObj);
                    gxObj = enumGxObj.Next();
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine(string.Format("Item selected in tree view {0}", GxApplication.SelectedObject.FullName));
                selectedObjs.Add(GxApplication.SelectedObject);
            }

            // iterate through selected objects
            foreach (IGxObject gxObj in selectedObjs)
            {
                // form is often unresponsive while processing but try and cancel anyway
                this.BringToFront();
                if (cancelled || !Utilities.Continue())
                    return;

                ArcCatalogUtils catUtils = new ArcCatalogUtils(this);
                catUtils.HandleAndReleaseSelectedObject(gxObj);
                thumbCount += catUtils.ThumbCount;
            }
        }

        internal void AddText(string msg)
        {
            txtInfo.AppendText(msg);
            txtInfo.AppendText("\n");
            _utils.WriteDebug(msg);
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            if (cancelled)
                this.Close();
            else
            {
                cancelled = true;
                Utilities.Cancel();
            }
        }

        private void ProgressForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                Utilities.Cancel();
        }
    }
}
