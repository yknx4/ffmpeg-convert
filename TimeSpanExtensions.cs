using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ffmpeg_convert
{
    public static class TimeSpanExtensions
    {
        public static TimeSpan ZeroMilliseconds(this TimeSpan dt)
        {
            return new TimeSpan(((dt.Ticks / 10000000) * 10000000));
        }
    }
}
