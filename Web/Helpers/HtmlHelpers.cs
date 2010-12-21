using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text;

namespace System.Web.Mvc
{
    public static class HtmlHelpers
    {
        const string pubDir = "/Public";
        const string cssDir = "Css";
        const string imageDir = "Images";
        const string scriptDir = "Scripts";

        public static string DatePickerEnable(this HtmlHelper helper) {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<script type='text/javascript'>$(document).ready(function() {$('.date-selector').datepicker();});</script>\n");
            return sb.ToString();
        }

        public static string Friendly(this HtmlHelper helper) {
            return helper.ViewContext.HttpContext.Request.Cookies["friendly"] != null ?
                helper.h(helper.ViewContext.HttpContext.Request.Cookies["friendly"].Value) : "";
        }

        public static HtmlString Script(this HtmlHelper helper, string fileName) {
            if (!fileName.EndsWith(".js"))
                fileName += ".js";
            var jsPath = new HtmlString(string.Format("<script src='{0}/{1}/{2}' ></script>\n",
                pubDir, scriptDir, helper.AttributeEncode(fileName)));
            return jsPath;
        }

        public static HtmlString CSS(this HtmlHelper helper, string fileName) {
            return CSS(helper, fileName, "screen");
        }

        public static HtmlString CSS(this HtmlHelper helper, string fileName, string media) {
            if (!fileName.EndsWith(".css"))
                fileName += ".css";
            var jsPath = new HtmlString(string.Format("<link rel='stylesheet' type='text/css' href='{0}/{1}/{2}'  media='" + media + "'/>\n",
                pubDir, cssDir, helper.AttributeEncode(fileName)));
            return jsPath;
        }

        public static string Image(this HtmlHelper helper, string fileName) {
            return Image(helper, fileName, "");
        }

        public static string Image(this HtmlHelper helper, string fileName, string attributes) {
            fileName = string.Format("{0}/{1}/{2}", pubDir, imageDir, fileName);
            return string.Format("<img src='{0}' '{1}' />", helper.AttributeEncode(fileName), helper.AttributeEncode(attributes));
        }

        public static string Truncate(this HtmlHelper helper, string input, int length) {
            if (input.Length <= length) {
                return input;
            } else {
                return input.Substring(0, length) + "...";
            }
        }
    }
}
