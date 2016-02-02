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
using System.Drawing;
using System.Windows.Forms;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Catalog;
using ESRI.ArcGIS.CatalogUI;
using ESRI.ArcGIS.esriSystem;

namespace CatalogTools
{
    public partial class CreateThumbnailForm : Form
    {
        private bool cancelled;
        private IGxApplication GxApplication = null;
        private enum CatalogViewTab { Contents, Preview, Metadata };
        private ITrackCancel trackCancel = null;
        private Utilities _utils = null;
        private int thumbCount = 0;

        public CreateThumbnailForm()
        {
            InitializeComponent();

            _utils = new Utilities();

            // an attempt to use a cancel tracker but it doesn't really work
            cancelled = false;
            trackCancel = new TrackCancel();

            this.Focus();

            try
            {
                GetSelectedDatasets();
            }
            catch (Exception ex)
            {
                AddText(string.Format("Error: {0}. {1}", ex.Message, ex.StackTrace));
            }

            if (thumbCount > 0)
                AddText(string.Format("{0} thumbnails created", thumbCount));

            if (!cancelled && trackCancel.Continue())
                AddText("Done");

            // change cancel button into a close button
            cancelled = true;
            btnCancel.Text = "Close";
        }

        // change tab if it is not changed already
        private void SwitchTab(CatalogViewTab view)
        {
            UID uid = new UIDClass();
            switch (view)
            {
                case CatalogViewTab.Contents:
                    if (!(GxApplication.View is IGxContentsView))
                    {
                        uid.Value = "{B1DE27AE-D892-11D1-AA81-064342000000}";
                        GxApplication.ViewClassID = uid;
                    }
                    break;
                case CatalogViewTab.Preview:
                    if (!(GxApplication.View is IGxPreview))
                    {
                        uid.Value = "{B1DE27AF-D892-11D1-AA81-064342000000}";
                        GxApplication.ViewClassID = uid;
                    }
                    break;
                case CatalogViewTab.Metadata:
                    if (!(GxApplication.View is IGxDocumentationView))
                    {
                        uid.Value = "{B1DE27B1-D892-11D1-AA81-064342000000}";
                        GxApplication.ViewClassID = uid;
                    }
                    break;
            }
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
                if (cancelled || !trackCancel.Continue())
                    return;

                HandleAndReleaseSelectedObject(gxObj);
            }
        }

        private void HandleAndReleaseSelectedObject(IGxObject gxObj)
        {
            // make sure object is really selected (i.e. multiple selections in contents view)
            if (!gxObj.FullName.Equals(GxApplication.Selection.Location.FullName))
                GxApplication.Selection.SetLocation(gxObj, null);

            // determine selected object type
            if (gxObj.Category == "Folder" ||
                gxObj.Category == "File Geodatabase" ||
                gxObj.Category == "File Geodatabase Feature Dataset")
            {
                AddText(string.Format("Finding datasets in {0}", gxObj.BaseName));
                IGxObjectContainer gxObjContainer = (IGxObjectContainer)gxObj;
                IEnumGxObject enumGxObj = gxObjContainer.Children;
                enumGxObj.Reset();
                IGxObject gxChild = enumGxObj.Next();
                while (gxChild != null)
                {
                    HandleAndReleaseSelectedObject(gxChild);
                    gxChild = enumGxObj.Next();
                }
            }
            else if (gxObj.Category == "Raster Dataset" ||
                gxObj.Category == "Shapefile" ||
                gxObj.Category == "File Geodatabase Feature Class" ||
                gxObj.Category == "File Geodatabase Raster Dataset")
            {
                // object is a dataset - update the thumbnail
                using (Bitmap newThumb = CreateThumbnailForSelectedObject())
                {
                    if (newThumb != null)
                    {
                        // convert to stdole.Picture and update current thumbnail
                        IGxDataset gxDs = (IGxDataset)gxObj;
                        IGxThumbnail gxThumb = (IGxThumbnail)gxDs;
                        gxThumb.Thumbnail = ImageConverter.ConvertBitmapToIPicture(newThumb);
                        thumbCount++;
                    }
                }
            }
            //else if (gxObj.Category == "Map Document")
            //{
            //    // *** NOT IMPLEMENTED *** - thumbnail creates ok but gxThumb won't update
            //    // object is a map - update the thumbnail
            //    Bitmap newThumb = CreateThumbnailForSelectedObject();

            //    if (newThumb == null)
            //        return;

            //    // convert to stdole.Picture and update current thumbnail
            //    IGxMap gxMap = (IGxMap)gxObj;
            //    IGxThumbnail gxThumb = (IGxThumbnail)gxMap;
            //    gxThumb.Thumbnail = ImageConverter.ConvertBitmapToIPicture(newThumb);
            //}
            else if (gxObj.Category == "Layer")
            {
                // object is a layer - update the thumbnail
                using (Bitmap newThumb = CreateThumbnailForSelectedObject())
                {
                    if (newThumb != null)
                    {
                        // convert to stdole.Picture and update current thumbnail
                        IGxLayer gxLayer = (IGxLayer)gxObj;
                        IGxThumbnail gxThumb = (IGxThumbnail)gxLayer;
                        gxThumb.Thumbnail = ImageConverter.ConvertBitmapToIPicture(newThumb);
                        thumbCount++;
                    }
                }
            }
            else if (gxObj.Category == "File Geodatabase Table" ||
                gxObj.Category == "File Geodatabase Relationship Class" ||
                gxObj.Category == "Map Document" ||
                gxObj.Category == "XML Document" ||
                gxObj.Category == "Map Package" ||
                gxObj.Category == "Toolbox")
                AddText(string.Format("Cannot create thumbnail for {0} {1}", gxObj.Category, gxObj.BaseName));
            else
                AddText(string.Format("Object {0} is unknown category {1}", gxObj.BaseName, gxObj.Category));

            // release resources for current object
            System.Diagnostics.Debug.WriteLine(string.Format("Memory before release {0}", System.Diagnostics.Process.GetCurrentProcess().WorkingSet64));
            int toRelease = 0;
            do
            {
                toRelease = System.Runtime.InteropServices.Marshal.ReleaseComObject(gxObj);
            }
            while (toRelease > 0);
            gxObj = null;
            System.Diagnostics.Debug.WriteLine(string.Format("After release {0}", System.Diagnostics.Process.GetCurrentProcess().WorkingSet64));
            GC.Collect();
            System.Diagnostics.Debug.WriteLine(string.Format("After GC {0}", System.Diagnostics.Process.GetCurrentProcess().WorkingSet64));
        }

        private Bitmap CreateThumbnailForSelectedObject()
        {
            AddText(string.Format("Creating thumbnail for {0}", GxApplication.Selection.Location.BaseName));
            SwitchTab(CatalogViewTab.Preview);

            // make sure preview tab is Geographic view, not Table view
            IGxPreview gxPreview = (IGxPreview)GxApplication.View;
            UID uid = new UIDClass();
            uid.Value = "esriCatalogUI.GxGeoGraphicView";  // aka "{B1DE27B0-D892-11D1-AA81-064342000000}";
            gxPreview.ViewClassID = uid;
            IGxGeographicView2 gxGeogView = (IGxGeographicView2)gxPreview.View;

            return ExportMapImage(gxGeogView.ActiveView);
        }

        private Bitmap ExportMapImage(IActiveView activeView)
        {
            if (activeView == null)
                return null;

            // make sure image has been drawn
            activeView.Refresh();

            // get export bounds to determine export image size
            tagRECT exportFrame = activeView.ExportFrame;
            int imageWidth = Math.Abs(exportFrame.right - exportFrame.left);
            int imageHeight = Math.Abs(exportFrame.top - exportFrame.bottom);

            // check there is something to export
            if (imageHeight == 0 && imageWidth == 0)
                return null;

            // create bitmap and output view to it
            Bitmap outBMP = new Bitmap(imageWidth, imageHeight);
            using (Graphics g = Graphics.FromImage(outBMP))
            {
                int gDC = g.GetHdc().ToInt32();
                // have to do it twice because sometimes it outputs before drawing is complete
                // maybe second output uses cached image?
                activeView.Output(gDC, 0, ref exportFrame, null, trackCancel);
                activeView.Output(gDC, 0, ref exportFrame, null, trackCancel);

                g.ReleaseHdc();

                return outBMP;
            }
        }

        private void AddText(string msg)
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
                trackCancel.Cancel();
            }
        }

        private void ProgressForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                trackCancel.Cancel();
        }
    }
}
