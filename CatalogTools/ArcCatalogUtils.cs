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
using System.Drawing;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Catalog;
using ESRI.ArcGIS.CatalogUI;
using ESRI.ArcGIS.esriSystem;

namespace CatalogTools
{
    class ArcCatalogUtils
    {
        private IGxApplication GxApplication = null;
        internal enum CatalogViewTab { Contents, Preview, Metadata };
        private CreateThumbnailForm parentForm = null;
        private int thumbCount = 0;

        public int ThumbCount
        {
            get { return thumbCount; }
            set { thumbCount = value; }
        }

        public ArcCatalogUtils()
        {
            InitializeUtils();
        }

        public ArcCatalogUtils(CreateThumbnailForm form)
        {
            parentForm = form;
            InitializeUtils();
        }

        private void InitializeUtils()
        {
            GxApplication = (IGxApplication)ArcCatalog.Application;
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

        internal void HandleAndReleaseSelectedObject(IGxObject gxObj)
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
                    ArcCatalogUtils catUtils = new ArcCatalogUtils(parentForm);
                    catUtils.HandleAndReleaseSelectedObject(gxChild);
                    thumbCount += catUtils.ThumbCount;
                    if (!Utilities.Continue())
                        break;
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
