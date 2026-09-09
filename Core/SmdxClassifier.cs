using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace PaveDetail.Core
{
    // Классификатор Robur: %ProgramData%\Topomatic\{Продукт}\{Версия}\Support\Smdx\materials.smdx.
    // JSON-массив {id, name, parent}; нам нужен только id -> name.
    // SitePavement хранит слои кодами (SmdxGenplanSmallAsphalt), "Контроллер КДО" региона -
    // готовым русским текстом. Resolve работает для обоих: код резолвится в имя,
    // произвольный текст без совпадения возвращается как есть.
    public sealed class SmdxClassifier
    {
        private readonly Dictionary<string, string> _names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public string LoadError;

        public static SmdxClassifier Empty()
        {
            return new SmdxClassifier();
        }

        public static SmdxClassifier Parse(string json)
        {
            var c = new SmdxClassifier();
            try
            {
                var entries = new JavaScriptSerializer().Deserialize<List<Entry>>(json);
                if (entries == null) throw new InvalidOperationException("пустой классификатор");
                foreach (var e in entries)
                {
                    if (e != null && !string.IsNullOrEmpty(e.id) && !string.IsNullOrEmpty(e.name))
                        c._names[e.id] = e.name;
                }
            }
            catch (Exception ex)
            {
                c.LoadError = ex.Message;
            }
            return c;
        }

        public string Resolve(string idOrText)
        {
            if (string.IsNullOrEmpty(idOrText)) return idOrText;
            string name;
            return _names.TryGetValue(idOrText, out name) ? name : idOrText;
        }

        private sealed class Entry
        {
            public string id { get; set; }
            public string name { get; set; }
            public string parent { get; set; }
        }
    }
}
