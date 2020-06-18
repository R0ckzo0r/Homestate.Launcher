using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace TestUI
{
    public class UIWindow : Form
    {
        public UIWindow()
        {
            this._mAeroEnabled = false;
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.ResizeRedraw, true);
        }
        
        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn
        (
            int nLeftRect,
            int nTopRect, 
            int nRightRect, 
            int nBottomRect, 
            int nWidthEllipse, 
            int nHeightEllipse
         );        
    
        [DllImport("dwmapi.dll")]
        public static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref Margins pMarInset);
    
        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
    
        [DllImport("dwmapi.dll")]
        public static extern int DwmIsCompositionEnabled(ref int pfEnabled);
    
        private bool _mAeroEnabled;                    
        private const int CsDropshadow = 0x00020000;
        private const int WM_NCPAINT = 0x0085;
        private const int WM_ACTIVATEAPP = 0x001C;
    
        public struct Margins                           
        {
            public int LeftWidth;
            public int RightWidth;
            public int TopHeight;
            public int BottomHeight;
        }
    
     
        private const int HTClient = 0x1;
        private const int HTCaption = 0x2;
    
        protected override CreateParams CreateParams
        {
            get
            {
                _mAeroEnabled = CheckAeroEnabled();
    
                var cp = base.CreateParams;
                if (!_mAeroEnabled)
                    cp.ClassStyle |= CsDropshadow;
    
                return cp;
            }
        }
    
        private static bool CheckAeroEnabled()
        {
            if (Environment.OSVersion.Version.Major < 6) return false;
            var enabled = 0;
            DwmIsCompositionEnabled(ref enabled);
            return enabled == 1;
        }
    
        protected override void WndProc(ref Message m)
        {
            if (/*m.Msg == WM_NCPAINT*/true)
                if (_mAeroEnabled)
                {
                    var v = 2;
                    DwmSetWindowAttribute(Handle, 2, ref v, 4);
                    Margins margins = new Margins()
                    {
                        BottomHeight = 1,
                        LeftWidth = 1,
                        RightWidth = 1,
                        TopHeight = 1
                    };
                    DwmExtendFrameIntoClientArea(Handle, ref margins);
                }
    
            base.WndProc(ref m);
        }

    }
}