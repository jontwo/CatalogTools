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
using ESRI.ArcGIS.ADF;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Catalog;
using ESRI.ArcGIS.CatalogUI;
using ESRI.ArcGIS.esriSystem;

namespace CatalogTools
{
    class ArcCatalogUtils
    {
        private IGxApplication GxApplication;
        internal enum CatalogViewTab { Contents, Preview, Metadata };
        private CreateThumbnailForm parentForm;
        private ComReleaser releaser;
        public Dictionary<string, string> knownCategories = new Dictionary<string, string>()
        {
            {"Folder", "Container"},
            {"Folder Connection", "Container"},
            {"File Geodatabase", "Container"},
            {"File Geodatabase Feature Dataset", "Container"},
            {"Personal Geodatabase", "Container"},
            {"Personal Geodatabase Feature Dataset", "Container"},
            {"SDE Feature Dataset", "Container"},
            {"Spatial Database Connection", "Container"},
            {"Raster Dataset", "Dataset"},
            {"Shapefile", "Dataset"},
            {"File Geodatabase Feature Class", "Dataset"},
            {"File Geodatabase Raster Dataset", "Dataset"},
            {"Personal Geodatabase Feature Class", "Dataset"},
            {"Personal Geodatabase Raster Dataset", "Dataset"},
            {"SDE Feature Class", "Dataset"},
            {"SDE Raster Dataset", "Dataset"},
            {"Layer", "Dataset"},
            {"Coordinate System", "Other"},
            {"ESRI AddIn", "Other"},
            {"File Geodatabase Table", "Other"},
            {"Personal Geodatabase Table", "Other"},
            {"SDE Table", "Other"},
            {"File Geodatabase Relationship Class", "Other"},
            {"Personal Geodatabase Relationship Class", "Other"},
            {"SDE Relationship Class", "Other"},
            {"Globe Document", "Other"},
            {"Map Document", "Other"},
            {"Map Template", "Other"},
            {"XML Document", "Other"},
            {"Map Package", "Other"},
            {"Text File", "Other"},
            {"Toolbox", "Other"}
        };

        public ArcCatalogUtils()
        {
            InitializeUtils();
        }

        ~ArcCatalogUtils()
        {
            if (releaser != null)
                releaser.Dispose();
        }

        public ArcCatalogUtils(CreateThumbnailForm form)
        {
            parentForm = form;
            InitializeUtils();
        }

        private void InitializeUtils()
        {
            GxApplication = (IGxApplication)ArcCatalog.Application;
            releaser = new ComReleaser();
        }

        public bool IsContainer(string item)
        {
            if (knownCategories.ContainsKey(item))
                if (knownCategories[item].Equals("Container"))
                    return true;

            return false;
        }

        public bool IsDataset(string item)
        {
            if (knownCategories.ContainsKey(item))
                if (knownCategories[item].Equals("Dataset"))
                    return true;

            return false;
        }

        public bool IsOther(string item)
        {
            if (knownCategories.ContainsKey(item))
                if (knownCategories[item].Equals("Other"))
                    return true;

            return false;
        }

        // change tab if it is not changed already
        internal void SwitchTab(CatalogViewTab view)
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

        internal void CreateThumbnailForNamedObject(string name)
        {
            IGxObject gxObj = SelectObjectByName(name);
            if (gxObj == null)
                return;

            // determine selected object type
            if (gxObj.Category == "Layer")
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
                    }
                }
            }
            else if (IsDataset(gxObj.Category))
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
            else
                AddText(string.Format("Cannot create thumbnail for {0} {1}", gxObj.Category, gxObj.BaseName));
        }

        internal IGxObject SelectObjectByName(string fullName)
        {
            int objCount;
            try
            {
                object foundObj = GxApplication.Catalog.GetObjectFromFullName(fullName, out objCount);
                if (objCount > 1)
                {
                    GxApplication.Selection.Clear(null);
                    IEnumGxObject enumGxObj = (IEnumGxObject)foundObj;
                    enumGxObj.Reset();
                    IGxObject gxObj = enumGxObj.Next();
                    while (gxObj != null)
                    {
                        GxApplication.Selection.Select(gxObj, true, null);
                        gxObj = enumGxObj.Next();
                    }
                }
                else if (foundObj != null)
                {
                    GxApplication.Selection.SetLocation((IGxObject)foundObj, null);
                }
                return GxApplication.SelectedObject;
            }
            catch (Exception ex)
            {
                AddText(string.Format("{0}: {1}", ex.Message, ex.StackTrace));
            }
            return null;
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
                activeView.Output(gDC, 0, ref exportFrame, null, Utilities.TrackCancel);
                activeView.Output(gDC, 0, ref exportFrame, null, Utilities.TrackCancel);

                g.ReleaseHdc();

                return outBMP;
            }
        }

        private void AddText(string msg)
        {
            if (parentForm != null)
                parentForm.AddText(msg);
        }
    }
}
