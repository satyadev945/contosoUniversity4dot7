using System.Collections;
using System.Collections.Generic;
using System.Web.Mvc;

namespace ContosoUniversity.Security
{
    /// <summary>
    /// Builds SelectLists whose display text has been sanitized with the HtmlSanitizer service,
    /// so untrusted database values can never carry markup into the view.
    /// </summary>
    public static class SafeSelectList
    {
        public static SelectList Create(IEnumerable items, string valueField, string textField, object selectedValue = null)
        {
            var list = new List<SelectListItem>();
            foreach (var item in items)
            {
                var type = item.GetType();
                var value = type.GetProperty(valueField).GetValue(item, null);
                var text = type.GetProperty(textField).GetValue(item, null);
                list.Add(new SelectListItem
                {
                    Value = value == null ? null : value.ToString(),
                    Text = HtmlSanitizationService.Default.SanitizeToText(text == null ? null : text.ToString())
                });
            }
            return new SelectList(list, "Value", "Text", selectedValue == null ? null : selectedValue.ToString());
        }
    }
}
