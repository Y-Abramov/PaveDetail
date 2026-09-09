using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace PaveDetail.Core
{
    // Текст слоя, вписанный пользователем для конкретной конструкции.
    public sealed class ProjectLayerText
    {
        public int index { get; set; }   // номер слоя сверху вниз, с единицы
        public string text { get; set; }
    }

    // Графы таблицы, которых нет в модели Robur.
    public sealed class ProjectConstruction
    {
        public string key { get; set; }       // ConstructionKey.Of(layers)
        public string code { get; set; }      // шифр типа: ПД-4*, ПП-1у
        public string section { get; set; }   // раздел: Проезжая часть дорог, Тротуары
        public string name { get; set; }      // наименование покрытия
        public string modulus { get; set; }   // модуль упругости, МПа - строкой, заказчик пишет и «-»
        public string note { get; set; }      // примечание
        public List<ProjectLayerText> layers { get; set; }

        public ProjectConstruction() { layers = new List<ProjectLayerText>(); }

        public string LayerText(int index)
        {
            foreach (var l in layers)
                if (l != null && l.index == index) return l.text;
            return null;
        }
    }

    // Файл-спутник проекта. Живёт рядом с .rbprojx, уезжает вместе с проектом -
    // тот же принцип, что у паспорта RailPassport.
    public sealed class ProjectData
    {
        public List<ProjectConstruction> Constructions = new List<ProjectConstruction>();

        public ProjectConstruction Find(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            foreach (var c in Constructions)
                if (c != null && string.Equals(c.key, key, StringComparison.Ordinal)) return c;
            return null;
        }

        // Запись, чьей конструкции больше нет в площадке: слои правили, ключ уехал.
        public IList<ProjectConstruction> Orphans(IEnumerable<string> liveKeys)
        {
            var live = new List<string>(liveKeys ?? new string[0]);
            var result = new List<ProjectConstruction>();
            foreach (var c in Constructions)
                if (c != null && !live.Contains(c.key)) result.Add(c);
            return result;
        }

        public static ProjectData Parse(string json)
        {
            var data = new ProjectData();
            if (string.IsNullOrWhiteSpace(json)) return data;
            try
            {
                var dto = new JavaScriptSerializer().Deserialize<Dto>(json);
                if (dto != null && dto.constructions != null) data.Constructions = dto.constructions;
            }
            catch
            {
                // Битый файл не должен ронять команду: работаем как с пустым,
                // при сохранении он будет перезаписан целиком.
            }
            return data;
        }

        public static string ToJson(ProjectData data)
        {
            var dto = new Dto { constructions = data == null ? new List<ProjectConstruction>() : data.Constructions };
            return new JavaScriptSerializer().Serialize(dto);
        }

        private sealed class Dto
        {
            public List<ProjectConstruction> constructions { get; set; }
        }
    }
}
