using System.Text.RegularExpressions;

namespace System.Web.Mvc
{
    public static class StringExtensions
    {
        public static string FormattedDayMonth(this int dayMonth) {
            var result = dayMonth.ToString();
            if (result.Length == 1)
                result = "0" + result;
            return result;
        }

        public static string CreateSlug(this string source) {
            var regex = new Regex(@"([^a-z0-9\-]?)");
            var slug = "";

            if (!string.IsNullOrEmpty(source)) {
                slug = source.Trim().ToLower();
                slug = slug.Replace(' ', '-');
                slug = slug.Replace("---", "-");
                slug = slug.Replace("--", "-");
                if (regex != null)
                    slug = regex.Replace(slug, "");

                if (slug.Length * 2 < source.Length)
                    return "";

                if (slug.Length > 100)
                    slug = slug.Substring(0, 100);
            }

            return slug;
        }

        public static string StripHTML(this string htmlString) {
            const string pattern = @"<(.|\n)*?>";
            return Regex.Replace(htmlString, pattern, string.Empty);
        }

        public static string Chop(this string content, string[] separator) {
            var strSplitArr = content.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            return strSplitArr[0];
        }
    }
}