using System;
using System.Runtime.InteropServices;

namespace Insomniac
{
    public class IdleTimeFinder
    {
        private struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        [DllImport("User32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        [DllImport("Kernel32.dll")]
        private static extern uint GetLastError();

        public static long GetIdleTime()
        {
            LASTINPUTINFO lastInputInfo = new LASTINPUTINFO();

            lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);

            GetLastInputInfo(ref lastInputInfo);

            return (((Environment.TickCount & int.MaxValue) - (lastInputInfo.dwTime & int.MaxValue)) & int.MaxValue) / 1000;
        }

        public static long GetLastInputTime()
        {
            LASTINPUTINFO lastInputInfo = new LASTINPUTINFO();

            lastInputInfo.cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(lastInputInfo);

            if (!GetLastInputInfo(ref lastInputInfo))
            {
                throw new Exception(GetLastError().ToString());
            }

            return lastInputInfo.dwTime;
        }
    }
}