using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GPXManager.views
{
    public partial class MapWindowForm
    {
        [StructLayout(LayoutKind.Sequential)]
        struct ICONINFO
        {
            public bool fIcon;         // Specifies whether this structure defines an icon or a cursor. A value of TRUE specifies
            // an icon; FALSE specifies a cursor.
            public Int32 xHotspot;     // Specifies the x-coordinate of a cursor's hot spot. If this structure defines an icon, the hot
            // spot is always in the center of the icon, and this member is ignored.
            public Int32 yHotspot;     // Specifies the y-coordinate of the cursor's hot spot. If this structure defines an icon, the hot
            // spot is always in the center of the icon, and this member is ignored.
            public IntPtr hbmMask;     // (HBITMAP) Specifies the icon bitmask bitmap. If this structure defines a black and white icon,
            // this bitmask is formatted so that the upper half is the icon AND bitmask and the lower half is
            // the icon XOR bitmask. Under this condition, the height should be an even multiple of two. If
            // this structure defines a color icon, this mask only defines the AND bitmask of the icon.
            public IntPtr hbmColor;    // (HBITMAP) Handle to the icon color bitmap. This member can be optional if this
            // structure defines a black and white icon. The AND bitmask of hbmMask is applied with the SRCAND
            // flag to the destination; subsequently, the color bitmap is applied (using XOR) to the
            // destination by using the SRCINVERT flag.
        }

        [DllImport("user32.dll")]
        static extern bool GetIconInfo(IntPtr hIcon, out ICONINFO piconinfo);

        [DllImport("user32.dll")]
        static extern IntPtr CreateIconIndirect([In] ref ICONINFO piconinfo);

        private void MakeCursor(string cursorName)
        {
            Bitmap bm=null;
            switch (cursorName)
            {
                case "zoom_plus":
                    {
                        bm = Properties.Resources.zoom_plus;
                    }
                    break;
                case "zoom_minus":
                    break;
            }
            
            bm.MakeTransparent(bm.GetPixel(0, 0));
            
            MapControl.MapCursor = MapWinGIS.tkCursor.crsrUserDefined;
            MapControl.UDCursorHandle = BitmapToCursor(bm, 7, 7); 
            //MapControl.MapCursor = System.Windows.Interop.CursorInteropHelper.Create(sf);
        }
        private int BitmapToCursor(Bitmap bmp, int hot_x, int hot_y)
        {
            // Initialize the cursor information.
            ICONINFO icon_info = new ICONINFO();
            IntPtr h_icon = bmp.GetHicon();
            GetIconInfo(h_icon, out icon_info);
            icon_info.xHotspot = hot_x;
            icon_info.yHotspot = hot_y;
            icon_info.fIcon = false;    // Cursor, not icon.

            // Create the cursor.
            return CreateIconIndirect(ref icon_info).ToInt32();

        }
    }
}
