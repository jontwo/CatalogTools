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

namespace CatalogTools
{
    public class ThumbnailButton : ESRI.ArcGIS.Desktop.AddIns.Button
    {
        private CreateThumbnailForm m_pThumbnailForm;

        public ThumbnailButton()
        {
        }

        protected override void OnClick()
        {
            ArcCatalog.Application.CurrentTool = null;

            try
            {
                // if it's the first click, create a new form
                // otherwise just bring it to the front
                if (m_pThumbnailForm == null || m_pThumbnailForm.IsDisposed)
                {
                    CatalogToolsExtension.WriteDebug("Opening CreateThumbnailForm");
                    m_pThumbnailForm = new CreateThumbnailForm();
                }

                if (!m_pThumbnailForm.Visible)
                {
                    m_pThumbnailForm.Show();
                }

                m_pThumbnailForm.BringToFront();
                m_pThumbnailForm.Focus();
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(string.Format("{0}\n{1}", ex.Message, ex.StackTrace));
            }
        }

        protected override void OnUpdate()
        {
            Enabled = ArcCatalog.Application != null;
        }
    }
}
