using System;
using System.Collections.Generic;
using System.Text;

namespace ICE.ConfigFiles;

public partial class Config
{
    public bool ShowInfoButton { get; set; } = true;
    public float MiddleColumnWidth { get; set; } = 1000f;
    public uint SelectedJob { get; set; } = 8;
    public bool XPRelicIgnoreManual { get; set; } = false;
    public bool XPRelicOnlyEnabled { get; set; } = false;
}
