using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MapWinGIS;
namespace MapWin.classes
{
    public class LabelProperty
    {
        public tkLabelAlignment Alignment { get; set; }

        public string FontName { get; set; }
        public int FontSize { get; set; }
        public uint FontColor { get; set; }
        public uint FontColor2 { get; set; }
        public tkLinearGradientMode FontGradientMode { get; set; }
        public bool FontBold { get; set; }
        public bool FontItalic { get; set; }
        public bool FontStrikeout { get; set; }
        public bool FontUnderline { get; set; }
        public int FontTransparency { get; set; }

        public uint FontOutlineColor { get; set; }
        public int FontOutlineWidth { get; set; }
        public bool FontOutlineVisible { get; set; }

        public uint FrameBackColor { get; set; }
        public uint FrameBackColor2 { get; set; }
        public tkLinearGradientMode FrameGradientMode { get; set; }
        public uint FrameOutlineColor { get; set; }
        public tkDashStyle FrameOutlineStyle { get; set; }
        public int FrameOutlineWidth { get; set; }
        public int FramePaddingX { get; set; }
        public int FramePaddingY { get; set; }
        public int FrameTransparency { get; set; }
        public tkLabelFrameType FrameType { get; set; }
        public bool FrameVisible { get; set; }
        public uint HaloColor { get; set; }
        public int HaloSize { get; set; }
        public bool HaloVisible { get; set; }

        public tkLabelAlignment InboxAlignment { get; set; }
        public tkLineLabelOrientation LineOrientation { get; set; }
        public double OffsetX { get; set; }
        public double OffsetY { get; set; }
        public uint ShadowColor { get; set; }
        public int ShadowOffsetX { get; set; }
        public int ShadowOffsetY { get; set; }
        public bool ShadowVisible { get; set; }
        public string Expression { get; set; }
    }
}
