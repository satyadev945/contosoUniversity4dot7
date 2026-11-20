using System;
using System.Collections;
using System.Collections.Generic;
using System.Web.Mvc;
using System.Web.UI;

namespace ContosoUniversity.Security
{
    /// <summary>
    /// Builds SelectLists whose display text has been passed through the HtmlSanitizer.
    /// </summary>
    public static class SafeSelectList
    {
        public static SelectList Create(IEnumerable items, string dataValueField, string dataTextField, object selectedValue = null)
        {
            var list = new List<SelectListItem>();
            foreach (var item in items)
            {
                var value = DataBinder.Eval(item, dataValueField);
                var text = DataBinder.Eval(item, dataTextField);
                list.Add(new SelectListItem
                {
                    Value = Convert.ToString(value),
                    Text = HtmlSanitizerProvider.SanitizeText(Convert.ToString(text))
                });
            }
            return new SelectList(list, "Value", "Text", selectedValue == null ? null : Convert.ToString(selectedValue));
        }
    }
}
