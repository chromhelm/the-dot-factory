﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Drawing.Drawing2D;

namespace TheDotFactory
{
    static class MyExtensions
    {
        #region Bitmap
        public static int[] ToArgbArray(Bitmap bmp)
        {
            int[] Pixels;
            BitmapData bd;
            Bitmap copy;

            if (bmp == null) throw new ArgumentNullException("bmp");
            if (bmp.PixelFormat != PixelFormat.Format32bppArgb)
            {
                copy = ChangePixelFormat(bmp, PixelFormat.Format32bppArgb);
            }
            else copy = bmp;

            bd = copy.LockBits(new Rectangle(0, 0, copy.Width, copy.Height), ImageLockMode.ReadOnly, copy.PixelFormat);

            Pixels = new int[copy.Width * copy.Height];

            // Copy data from pointer to array
            Marshal.Copy(bd.Scan0, Pixels, 0, Pixels.Length);

            copy.UnlockBits(bd);

            return Pixels;
        }

        public static Bitmap ToBitmap(int[] ArgbPixels, Size size)
        {
            BitmapData bd;
            Bitmap bmp;

            if (ArgbPixels == null) throw new ArgumentNullException("bmp");

            bmp = new Bitmap(size.Width, size.Height);

            bd = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, bmp.PixelFormat);

            // Copy data from pointer to array
            Marshal.Copy(ArgbPixels, 0, bd.Scan0, ArgbPixels.Length);

            bmp.UnlockBits(bd);

            return bmp;
        }

        /// <summary>
        /// Changes the pixelformat of a given bitmap into any of the GDI+ supported formats.
        /// </summary>
        /// <param name="bmp">Die Bitmap die verändert werden soll.</param>
        /// <param name="format">Das neu anzuwendende Pixelformat.</param>
        /// <returns>Die Bitmap mit dem neuen PixelFormat</returns>
        public static Bitmap ChangePixelFormat(Bitmap bmp, PixelFormat format)
        {
            return bmp.Clone(new Rectangle(0, 0, bmp.Width, bmp.Height), format);
        }

        public static Color[] GetColorList(Bitmap bmp)
        {
            return ToArgbArray(bmp).Distinct().Select<int, Color>(x => Color.FromArgb(x)).ToArray();
        }

        public static Border GetBorders(Bitmap bmp, Color borderColor)
        {
            return GetBorders(bmp, new Color[] { borderColor });
        }

        public static Border GetBorders(Bitmap bmp, Color[] borderColorList)
        {
            int[] pixel = ToArgbArray(bmp);
            Border b = new Border();
            int width = bmp.Width, height = bmp.Height;
            int[] borderColorListInt = borderColorList.Select<Color, int>(p => p.ToArgb()).ToArray();

            Func<int, int, int> getPixel = delegate (int x, int y)
            {
                return pixel[y * width + x];
            };

            // returns whether a bitmap column is empty (empty means all is border color)
            Func<int, bool> columnIsEmpty = delegate (int column)
            {
                // for each row in the column
                for (int row = 0; row < height; ++row)
                {
                    // is the pixel black?
                    if (!borderColorListInt.Contains(getPixel(column, row)))
                    {
                        // found. column is not empty
                        return false;
                    }
                }

                // column is empty
                return true;
            };

            // returns whether a bitmap row is empty (empty means all is border color)
            Func<int, bool> rowIsEmpty = delegate (int row)
            {
                // for each column in the row
                for (int column = 0; column < width; ++column)
                {
                    // is the pixel black?
                    if (!borderColorListInt.Contains(getPixel(column, row)))
                    {
                        // found. row is not empty
                        return false;
                    }
                }

                // row is empty
                return true;
            };
            
            for (b.Left = 0; b.Left < width; ++b.Left)
            {
                if (!columnIsEmpty(b.Left)) break;
            }
            for (b.Right = width - 1; b.Right >= 0; --b.Right)
            {
                if (!columnIsEmpty(b.Right)) break;
            }
            for (b.Top = 0; b.Top < height; ++b.Top)
            {
                if (!rowIsEmpty(b.Top)) break;
            }
            for (b.Bottom = height - 1; b.Bottom >= 0; --b.Bottom)
            {
                if (!rowIsEmpty(b.Bottom)) break;
            }

            return b;
        }

        /// <summary>
        /// Resize the image to the specified width and height with low quality(pixel by pixel).
        /// </summary>
        /// <param name="image">The image to resize.</param>
        /// <param name="faktor">The scaling faktor to scale with</param>
        /// <returns>The resized image.</returns>
        public static Bitmap ResizeImage(Bitmap image, int faktor)
        {
            if (faktor <= 0) throw new ArgumentException();

            Size newSize = new Size(image.Size.Width * faktor, image.Height * faktor);

            Rectangle destRect = new Rectangle(new Point(), newSize);

            Bitmap destImage = new Bitmap(newSize.Width, newSize.Height);
            Graphics g = Graphics.FromImage(destImage);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            for(int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    g.FillRectangle(new SolidBrush(image.GetPixel(x, y)), x * faktor, y * faktor, faktor, faktor);
                }
            }

            return destImage;
        }

        /// <summary>
        /// Creates a copy of the section of this Bitmap defined with a specified PixelFormat and default color when the rectangel reaches out og the image.
        /// </summary>
        /// <param name="rectangek">Defines the portion of this Bitmap to copy. Coordinates are relative to this Bitmap.</param>
        /// <param name="format">The pixel format for the new Bitmap. This must specify a value that begins with Format.</param>
        /// <param name="defaultColor">The color whitch is used to file the areas that are outside the image</param>
        /// <returns>The resized image.</returns>
        public static Bitmap Clone(Bitmap image, Rectangle rectangel, PixelFormat format, Color defaultColor)
        {
            Bitmap destImage = new Bitmap(rectangel.Width, rectangel.Height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            for (int y = 0; y < destImage.Height; y++)
            {
                for (int x = 0; x < destImage.Width; x++)
                {
                    Color c;
                    if (rectangel.X + x < image.Width && rectangel.Y + y < image.Height) c = image.GetPixel(rectangel.X + x, rectangel.Y + y);
                    else c = defaultColor;
                    destImage.SetPixel(x, y, c);
                }
            }

            return destImage;
        }
        #endregion

        #region enum types
        // convert bits to bytes according to desc format
        public static int ConvertValueByDescriptorFormat(OutputConfiguration.DescriptorFormat descFormat, int valueInBits)
        {
            // according to format
            switch(descFormat)
            {
                case OutputConfiguration.DescriptorFormat.DisplayInBytes:
                    // get value in bytes
                    int valueInBytes = valueInBits / 8;
                    if (valueInBits % 8 != 0) valueInBytes++;
                    // set into string
                    return valueInBytes;
                case OutputConfiguration.DescriptorFormat.DisplayInBits:
                case OutputConfiguration.DescriptorFormat.DontDisplay:
                    // no conversion required
                    return valueInBits;
                default:
                    throw new NotImplementedException();
            }
        }
        #endregion

        #region string
        // make 'name' suitable as a variable name, starting with '_'
        // or a letter and containing only letters, digits, and '_'
        public static string ScrubVariableName(string name)
        {
            // scrub invalid characters from the font name
            StringBuilder outName = new StringBuilder();
            foreach (char ch in name)
            {
                if (Char.IsLetterOrDigit(ch) || ch == '_')
                    outName.Append(ch);
            }

            // prepend '_' if the first character is a number
            if (Char.IsDigit(outName[0]))
                outName.Insert(0, '_');

            // convert the first character to lower case
            outName[0] = Char.ToLower(outName[0]);

            // return name
            return outName.ToString();
        }
        
        // get only the variable name from an expression in a specific format
        // e.g. input: const far unsigned int my_font[] = ; 
        //      output: my_font[]
        public static string GetVariableNameFromExpression(string expression)
        {
            // iterator
            int charIndex = 0;

            // invalid format string
            const string invalidFormatString = "##Invalid format##";

            //
            // Strip array parenthesis
            //

            // search for '[number, zero or more] '
            const string arrayRegexString = @"\[[0-9]*\]";

            // modify the expression
            expression = Regex.Replace(expression, arrayRegexString, "");

            //
            // Find the string between '=' and a space, trimming spaces from end
            //

            // start at the end and look for the letter or number
            for (charIndex = expression.Length - 1; charIndex != 1; --charIndex)
            {
                // find the last character of the variable name
                if (expression[charIndex] != '=' && expression[charIndex] != ' ') break;
            }

            // check that its valid
            if (charIndex == 0) return invalidFormatString;

            // save this index
            int lastVariableNameCharIndex = charIndex;

            // continue looking for a space
            for (charIndex = lastVariableNameCharIndex; charIndex != 0; --charIndex)
            {
                // find the last character of the variable name
                if (expression[charIndex] == ' ') break;
            }

            // check that its valid
            if (charIndex == 0) return invalidFormatString;

            // save this index as well
            int firstVariableNameCharIndex = charIndex + 1;

            // return the substring
            return expression.Substring(firstVariableNameCharIndex, lastVariableNameCharIndex - firstVariableNameCharIndex + 1);
        }
        #endregion

        public static TKey[] GetEnabledKeys<TKey>(Dictionary<TKey, bool> dic)
        {
            return dic.Aggregate<KeyValuePair<TKey, bool>, List<TKey>>(
                    new List<TKey>(),
                    (list, kvp) =>
                    {
                        if (kvp.Value) list.Add(kvp.Key);
                        return list;
                    }).ToArray();
        }

        public static TKey[] GetDiabeldKeys<TKey>(Dictionary<TKey, bool> dic)
        {
            return dic.Aggregate<KeyValuePair<TKey, bool>, List<TKey>>(
                    new List<TKey>(),
                    (list, kvp) =>
                    {
                        if (!kvp.Value) list.Add(kvp.Key);
                        return list;
                    }).ToArray();
        }
    }
}
