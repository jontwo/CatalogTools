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

using System.Drawing;
using stdole;

namespace CatalogTools
{
    public class ImageConverter : System.Windows.Forms.AxHost
    {
        private ImageConverter()
            : base(string.Empty)
        {
        }

        public static IPicture ConvertBitmapToIPicture(Image image)
        {
            if (image == null)
                return null;

            // create new bitmap
            int w = image.Width;
            int h = image.Height;
            using (Bitmap b = new Bitmap(w, h))
            {
                using (Graphics g = Graphics.FromImage(b))
                {
                    // draw a white background
                    g.FillRectangle(Brushes.White, 0, 0, w, h);

                    // draw the input image over the top
                    g.DrawImage(image, 0, 0, w, h);

                    // convert to stdole.IPicture
                    return (IPicture)GetIPictureFromPicture(b);
                }
            }
        }
    }
}
