using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace System.Web.Mvc {
    public static class ViewDataExtensions {
        public static IEnumerable<T> ViewData<T>(this HtmlHelper helper, string name) {
            if (helper.ViewData[name] != null) {
                return (IEnumerable<T>)helper.ViewData[name];
            }
            return new List<T>();
        }

        public static T ViewDataSingle<T>(this HtmlHelper helper, string name) {
            if (helper.ViewData[name] != null) {
                return (T)helper.ViewData[name];
            }
            return default(T);
        }

    }
}
