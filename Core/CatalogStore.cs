using System;
using System.IO;
using System.Reflection;

namespace PaveDetail.Core
{
    // Каталоги: ресурс в сборке, копия в %AppData% при первом запуске, дальше правит юзер.
    // Тот же паттерн, что каталог изделий PavePlan.
    public static class CatalogStore
    {
        public static string Folder
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "ABR", "PaveDetail");
            }
        }

        public static MaterialCatalog Materials()
        {
            return MaterialCatalog.Parse(ReadOrSeed("materials.json"));
        }

        public static BorderCatalog Borders()
        {
            return BorderCatalog.Parse(ReadOrSeed("borders.json"));
        }

        public static void ResetToBuiltIn(string fileName)
        {
            var path = Path.Combine(Folder, fileName);
            if (File.Exists(path)) File.Delete(path);
            ReadOrSeed(fileName);
        }

        public static string ReadOrSeed(string fileName)
        {
            try
            {
                Directory.CreateDirectory(Folder);
                var path = Path.Combine(Folder, fileName);
                if (File.Exists(path)) return File.ReadAllText(path);

                var text = ReadResource(fileName);
                File.WriteAllText(path, text);
                return text;
            }
            catch
            {
                // Папка недоступна (права, профиль на сетевом диске) - работаем на ресурсе.
                return ReadResource(fileName);
            }
        }

        private static string ReadResource(string fileName)
        {
            var asm = Assembly.GetExecutingAssembly();
            foreach (var name in asm.GetManifestResourceNames())
            {
                if (!name.EndsWith(fileName, StringComparison.OrdinalIgnoreCase)) continue;
                using (var s = asm.GetManifestResourceStream(name))
                using (var r = new StreamReader(s))
                    return r.ReadToEnd();
            }
            return string.Empty;
        }
    }
}
