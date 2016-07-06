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
using System.Text;
using System.Windows.Forms;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Catalog;
using ESRI.ArcGIS.CatalogUI;

namespace CatalogTools
{
    public partial class CreateThumbnailForm : Form
    {
        private bool selecting;
        private IGxApplication GxApplication;
        private GxSelection GxSel;
        private ArcCatalogUtils catUtils;
        private int thumbCount;
        private IGxObject lastLocation;

        public CreateThumbnailForm()
        {
            InitializeComponent();
            GxApplication = (IGxApplication)ArcCatalog.Application;

            SetUpEvents();

            catUtils = new ArcCatalogUtils(this);
            selecting = true;
            UpdateSelection(GxApplication.Selection);
        }

        private void OnSelectionChanged(IGxSelection selection, ref object initiator)
        {
            // TODO make this fire when changing focus from dataset in contents view to parent gdb in tree view
            // as the location is the same, the selection has "not changed"
            // workaround is to click a child dataset in tree view then click parent again
            if (selecting)
            {
                UpdateSelection(selection);
                if (lastLocation == null || !lastLocation.FullName.Equals(selection.Location.FullName))
                {
                    lastLocation = selection.Location;
                }
            }
        }

        //private void OnViewChanged()
        //{
        //    // do something - call UpdateSelection probably
        //}

        private void SetUpEvents()
        {
            // This doesn't work as events.OnViewChanged is a method group so cannot be assigned to.
            // I think the API is incomplete, so IGxApplicationEvents can't be used.
            // It would probably fix the situation above if we could get it working.
            //IGxApplicationEvents events = (IGxApplicationEvents)GxApplication;
            //events.OnViewChanged += OnViewChanged;

            GxSel = GxApplication.Selection as GxSelection;
            GxSel.OnSelectionChanged += new IGxSelectionEvents_OnSelectionChangedEventHandler(OnSelectionChanged);
            
        }

        private void CreateThumbnails()
        {
            // don't update tree view when selection changes
            selecting = false;
            this.Focus();
            thumbCount = 0;

            // reset cancel tracker
            Utilities.TrackCancel = new TrackCancel();

            try
            {
                // iterate through selected objects
                foreach (TreeNode node in tvSelectedItems.Nodes)
                {
                    if (node.Checked)
                    {
                        // form is often unresponsive while processing but try and cancel anyway
                        this.BringToFront();
                        if (!Utilities.Continue())
                            return;

                        SaveCurrentSelection();
                        catUtils.CreateThumbnailForNamedObject(node.Text);
                        thumbCount++;
                        node.Checked = false;
                    }
                }
            }
            catch (Exception ex)
            {
                AddText(string.Format("Error: {0}. {1}", ex.Message, ex.StackTrace));
            }

            if (Utilities.Continue())
                AddText("Done");
            else
                AddText("Cancelled");

            AddText(string.Format("{0} thumbnails created", thumbCount));

            // start updating tree view on selection change again
            selecting = true;
        }

        private void UpdateSelection(IGxSelection gxSel)
        {
            if (gxSel == null)
                return;

            // gather all the selected objects in to a list so selection can be changed while processing
            var selectedObjs = new List<IGxObject>();
            tvSelectedItems.BeginUpdate();
            tvSelectedItems.Nodes.Clear();

            // reset cancel tracker
            Utilities.TrackCancel = new TrackCancel();

            if (gxSel.Count > 0)
            {
                IEnumGxObject enumGxObj = gxSel.SelectedObjects;
                enumGxObj.Reset();
                IGxObject gxObj = enumGxObj.Next();
                while (gxObj != null)
                {
                    GetSelectedDatasets(gxObj, ref selectedObjs);
                    gxObj = enumGxObj.Next();
                }
            }
            else
            {
                if (lastLocation == null || !lastLocation.FullName.Equals(gxSel.Location.FullName))
                {
                    System.Diagnostics.Debug.WriteLine(string.Format("Item selected in tree view {0}", gxSel.Location.FullName));
                    GetSelectedDatasets(GxApplication.SelectedObject, ref selectedObjs);
                }
            }

            // populate treeview
            foreach (var selObj in selectedObjs)
            {
                AddToTreeView(selObj);
            }
            tvSelectedItems.EndUpdate();
            AddText(string.Format("{0} items added to list", selectedObjs.Count));
        }

        private void GetChildDatasets(IGxObject gxObj, ref List<IGxObject> selectedObjs)
        {
            AddText(string.Format("Finding datasets in {0}", gxObj.BaseName));
            IGxObjectContainer gxObjContainer = (IGxObjectContainer)gxObj;
            IEnumGxObject enumGxObj = gxObjContainer.Children;
            if (enumGxObj == null)
                return;

            enumGxObj.Reset();
            IGxObject gxChild = enumGxObj.Next();
            while (gxChild != null)
            {
                GetSelectedDatasets(gxChild, ref selectedObjs);
                if (!Utilities.Continue())
                    break;
                gxChild = enumGxObj.Next();
            }
        }

        private void GetSelectedDatasets(IGxObject gxObj, ref List<IGxObject> selectedObjs)
        {
            // determine selected object type
            if (catUtils.IsContainer(gxObj.Category))
            {
                if (gxObj.Category == "Folder Connection")
                {
                    string msg = @"Folder connection selected. Do you want to search the whole folder for datasets?";
                    string caption = @"Select Folder";
                    DialogResult result = MessageBox.Show(msg, caption, MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                    if (result == DialogResult.No)
                        return;
                } 
                GetChildDatasets(gxObj, ref selectedObjs);
            }
            else if (catUtils.IsDataset(gxObj.Category))
            {
                // object is a dataset or layer - update the thumbnail
                CatalogToolsExtension.WriteDebug(string.Format("Adding {0} {1} to list", gxObj.Category, gxObj.Name));
                selectedObjs.Add(gxObj);
            }
            else if (catUtils.IsOther(gxObj.Category))
            {
                AddText(string.Format("Cannot create thumbnail for {0} {1}", gxObj.Category, gxObj.BaseName));
            }
            else
                AddText(string.Format("Object {0} is unknown category {1}", gxObj.BaseName, gxObj.Category));
        }

        private void LoadPreviousSelection()
        {
            tvSelectedItems.Nodes.Clear();
            var items = Utilities.ReadFromTempFile();

            if (!string.IsNullOrEmpty(items))
            {
                foreach (var line in items.Split('\n'))
                {
                    var trimmedLine = line.Trim();
                    if (!string.IsNullOrEmpty(trimmedLine))
                        tvSelectedItems.Nodes.Add(trimmedLine);
                }
                AddText(string.Format("Loaded {0} items", tvSelectedItems.Nodes.Count));
            }
        }

        private void SaveCurrentSelection()
        {
            var sb = new StringBuilder();
            foreach (TreeNode node in tvSelectedItems.Nodes)
            {
                if (node.Checked)
                    sb.AppendLine(node.Text);
            }

            Utilities.WriteToTempFile(sb.ToString());
        }

        internal void AddToTreeView(IGxObject gxObj)
        {
            var newNode = new TreeNode();
            newNode.Text = gxObj.FullName;
            tvSelectedItems.Nodes.Add(newNode);
        }

        private void CheckAllNodes()
        {
            foreach (TreeNode node in tvSelectedItems.Nodes)
                node.Checked = true;
        }

        private void UncheckAllNodes()
        {
            foreach (TreeNode node in tvSelectedItems.Nodes)
                node.Checked = false;
        }

        internal void AddText(string msg)
        {
            txtInfo.AppendText(msg);
            txtInfo.AppendText("\n");
            CatalogToolsExtension.WriteDebug(msg);
        }

        private void ProgressForm_KeyDown(object sender, KeyEventArgs e)
        {
            if ((e.KeyCode & Keys.Escape) == Keys.Escape)
                Utilities.Cancel();
        }

        private void btnCreate_Click(object sender, EventArgs e)
        {
            CreateThumbnails();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Utilities.Cancel();
        }

        private void btnOpen_Click(object sender, EventArgs e)
        {
            // TODO read from previous selection
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            // TODO save current selection
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnSelectAll_Click(object sender, EventArgs e)
        {
            CheckAllNodes();
        }

        private void btnSelectNone_Click(object sender, EventArgs e)
        {
            UncheckAllNodes();
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            LoadPreviousSelection();
        }

        private void CreateThumbnailForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            CatalogToolsExtension.WriteDebug("Closing CreateThumbnailForm");
        }
    }
}
