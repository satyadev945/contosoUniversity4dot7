using System;
using System.Collections;
using System.Linq;
using System.Web.Mvc;

namespace ContosoUniversity.Security
{
    /// <summary>
    /// Helpers used by controllers and views before values are placed in ViewBag,
    /// so that no unsanitized, potentially user-controlled markup reaches Razor views.
    /// </summary>
    public static class ViewBagSanitizer
    {
        /// <summary>Sanitizes a string for rendering through Razor encoding.</summary>
        public static string Text(string value)
        {
            return HtmlSanitization.SanitizeText(value);
        }

        /// <summary>
        /// Sanitizes strings; value types (numbers, booleans, dates) cannot carry markup
        /// and are returned unchanged so their original type is preserved.
        /// </summary>
        public static object Value(object value)
        {
            if (value == null)
            {
                return null;
            }

            var s = value as string;
            if (s != null)
            {
                return HtmlSanitization.SanitizeText(s);
            }

            if (value.GetType().IsValueType)
            {
                return value;
            }

            return HtmlSanitization.SanitizeText(value.ToString());
        }

        /// <summary>
        /// Builds a SelectList whose display text has been sanitized.
        /// </summary>
        public static SelectList SanitizedSelectList(IEnumerable items, string dataValueField, string dataTextField, object selectedValue = null)
        {
            var original = new SelectList(items, dataValueField, dataTextField, selectedValue);
            var sanitized = original
                .Select(i => new SelectListItem
                {
                    Value = i.Value,
                    Text = HtmlSanitization.SanitizeText(i.Text),
                    Selected = i.Selected
                })
                .ToList();

            return new SelectList(sanitized, "Value", "Text", selectedValue);
        }
    }
}
