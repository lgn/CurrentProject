using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;
using System.Web.Routing;
using NUnit.Framework;

namespace Tests.Helpers
{
    public static class UnitTestHelpers
    {
        public static void ShouldEqual<T>(this T actualValue, T expectedValue) {
            Assert.AreEqual(expectedValue, actualValue);
        }

        public static void ShouldNotEqual<T>(this T actualValue, T expectedValue) {
            Assert.AreNotEqual(expectedValue, actualValue);
        }

        public static void ShouldContain<T>(this List<T> actualValue, T expectedValue) {
            Assert.Contains(expectedValue, actualValue);
        }

        public static void ShouldNotContain<T>(this List<T> actualValue, T expectedValue) {
            Assert.IsFalse(actualValue.Contains(expectedValue));
        }

        public static void ShouldBeRedirectedTo(this ActionResult actionResult, object expectedRouteValues) {
            var actualValues = ((RedirectToRouteResult)actionResult).RouteValues;
            var expectedValues = new RouteValueDictionary(expectedRouteValues);

            foreach (string key in expectedValues.Keys) {
                actualValues[key].ShouldEqual(expectedValues[key]);
            }
        }

        public static void ShouldBeDefaultView(this ActionResult actionResult) {
            actionResult.ShouldBeView(string.Empty);
        }

        public static void ShouldBeView(this ActionResult actionResult, string viewName) {
            Assert.IsInstanceOf<ViewResult>(actionResult);
            ((ViewResult)actionResult).ViewName.ShouldEqual(viewName);
        }

        public static void ShouldBeAuthorized(this Controller controller) {
            Assert.IsTrue(controller.GetType().GetCustomAttributes(false).Any(a =>
                a.GetType() == typeof(AuthorizeAttribute)));
        }

        public static void ShouldBeAuthorized(this Controller controller, string action, params Type[] parameters) {
            Assert.IsTrue((controller.GetType().GetMethod(action, parameters)).GetCustomAttributes(false).Any(a =>
                a.GetType() == typeof(AuthorizeAttribute)));
        }
    }
}
