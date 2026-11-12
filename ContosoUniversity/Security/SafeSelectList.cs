using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace ContosoUniversity.Security
{
    /// <summary>
    /// Builds drop-down lists whose display text has been passed through the HtmlSanitizer.
    /// </summary>
    public static class SafeSelectList
    {
        private class Option
        {
            public object Value { get; set; }
            public string Text { get; set; }
        }

        public static SelectList Create<T>(IEnumerable<T> source, Func<T, object> value, Func<T, string> text, object selectedValue = null)
        {
            var options = source.Select(s => new Option
            {
                Value = value(s),
                Text = HtmlSanitizerProvider.SanitizeText(text(s))
            }).ToList();
            return new SelectList(options, "Value", "Text", selectedValue);
        }
    }
}
