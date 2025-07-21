using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace EnhancedGameHub.Helpers
{
    public static class IconHelper
    {
        #region Win32 API
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern uint PrivateExtractIcons(string lpszFile, int nIconIndex, int cxIcon, int cyIcon, IntPtr[] phicon, uint[] piconid, uint nIcons, uint flags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);
        
        [StructLayout(LayoutKind.Sequential)]
        private struct ICONINFO
        {
            public bool fIcon;
            public uint xHotspot;
            public uint yHotspot;
            public IntPtr hbmMask;
            public IntPtr hbmColor;
        }

        [DllImport("user32.dll")]
        private static extern bool GetIconInfo(IntPtr hIcon, out ICONINFO piconinfo);
        #endregion

        public static BitmapSource GetHighestResolutionIcon(string exePath)
        {
            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath)) return null;

            IntPtr[] iconHandles = null;
            try
            {
                uint iconCount = PrivateExtractIcons(exePath, 0, 0, 0, null, null, 0, 0);
                if (iconCount == 0 || iconCount == 0xFFFFFFFF) return null;

                iconHandles = new IntPtr[iconCount];
                uint[] iconIds = new uint[iconCount];

                uint extractedCount = PrivateExtractIcons(exePath, 0, 256, 256, iconHandles, iconIds, iconCount, 0);
                if (extractedCount == 0) return null;

                int bestIconIndex = -1;
                int maxIconSize = 0;

                for (int i = 0; i < extractedCount; i++)
                {
                    if (GetIconInfo(iconHandles[i], out ICONINFO info))
                    {
                        using (Bitmap bmp = Bitmap.FromHbitmap(info.hbmColor))
                        {
                            if (bmp.Width > maxIconSize)
                            {
                                maxIconSize = bmp.Width;
                                bestIconIndex = i;
                            }
                        }
                    }
                }
                if (bestIconIndex != -1)
                {
                    return Imaging.CreateBitmapSourceFromHIcon( iconHandles[bestIconIndex], Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                }
                return null;
            }
            catch (Exception)
            {
                return null;
            }
            finally
            {
                if (iconHandles != null)
                {
                    foreach (var handle in iconHandles)
                    {
                        if (handle != IntPtr.Zero)
                        {
                            DestroyIcon(handle);
                        }
                    }
                }
            }
        }
    }
}
