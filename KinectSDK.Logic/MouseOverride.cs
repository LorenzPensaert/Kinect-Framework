using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace KinectSDK.Logic {

    /// <summary>
    /// https://social.msdn.microsoft.com/Forums/en-US/1ea09f18-94f6-4f4f-bcba-d02da27beaa4/control-mouse-position-and-generate-click-from-program-c-winforms-aim-control-pc-from-serial?forum=csharpgeneral
    /// Makes us able to set a custom position of the mouse using the kinect.
    /// </summary>
    internal class MouseOverride {

        [DllImport("user32.dll")]
        public static extern bool SetCursorPos (int X, int Y);

        [DllImport("user32.dll")]
        private static extern void mouse_event (MouseEventFlag flags, int dx, int dy, uint data, UIntPtr extraInfo);

        [Flags]
        private enum MouseEventFlag : uint {
            Move = 0x0001,
            LeftDown = 0x0002,
            LeftUp = 0x0004,
            RightDown = 0x0008,
            RightUp = 0x0010,
            MiddleDown = 0x0020,
            MiddleUp = 0x0040,
            XDown = 0x0080,
            XUp = 0x0100,
            Wheel = 0x0800,
            VirtualDesk = 0x4000,
            Absolute = 0x8000
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT {
            public int X;
            public int Y;

            public static implicit operator Point (POINT point) {
                return new Point(point.X, point.Y);
            }
        }

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos (out POINT lpPoint);

        public static Point GetCursorPosition () {
            POINT lpPoint;
            GetCursorPos(out lpPoint);

            return lpPoint;
        }

        public static void MouseLeftDown () {
            mouse_event(MouseEventFlag.LeftDown, 0, 0, 0, UIntPtr.Zero);
        }

        public static void MouseLeftUp () {
            mouse_event(MouseEventFlag.LeftUp, 0, 0, 0, UIntPtr.Zero);
        }
    }
}