using System;

namespace Web.Helpers
{
    public static class DateTimeExtensions
    {
        public static DateTime FromUnixTime(this Int64 self) {
            var ret = new DateTime(1970, 1, 1);
            return ret.AddSeconds(self);
        }

        public static Int64 ToUnixTime(this DateTime self) {
            var epoc = new DateTime(1970, 1, 1);
            var delta = self - epoc;

            if (delta.TotalSeconds < 0) throw new ArgumentOutOfRangeException("Unix epoc starts January 1st, 1970");

            return (long)delta.TotalSeconds;
        }

        //public static string ToRelativeString(this DateTime dt) {
        //    var now = DateTime.Now;
        //    if (now.Date != dt.Date)
        //        // dt happend more than a day ago... show the date & time
        //        return dt.ToString();
        //    else {
        //        // dt and rightNow happened on the same day...
        //        int secondsApart = Convert.ToInt32(now.Subtract(dt).TotalSeconds);

        //        // See if the date dt is within the last hour...
        //        if (secondsApart < 10)
        //            return "Seconds ago...";
        //        else if (secondsApart < 60)
        //            return "Less than a minute ago...";
        //        else if (secondsApart < 3600)
        //            return string.Format("{0:N0} minutes ago...", secondsApart / 60 + 1);
        //        else if (secondsApart < 7200)
        //            return string.Format("about an hour ago");
        //        else if (secondsApart < 43200)
        //            return string.Format("{0} hours ago...", secondsApart / 3600);

        //        // Ok, the date is more than an hour old... show the time)
        //        return dt.ToShortDateString();
        //    }
        //}
    }
}