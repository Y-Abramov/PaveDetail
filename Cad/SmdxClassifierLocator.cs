using System;
using System.IO;

namespace PaveDetail.Cad
{
    // Путь к классификатору materials.smdx. Тот же паттерн вычисления продукта/версии,
    // что Shared\Bootstrap\TpmInstaller.FindPackagesJson - из имени папки установки
    // ("Topomatic Robur Road 16.0" -> продукт "Robur Road", версия "16.0").
    internal static class SmdxClassifierLocator
    {
        public static string FindMaterialsSmdxPath()
        {
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\', '/');
                var dirName = Path.GetFileName(baseDir);
                var parts = dirName.Split(' ');
                var version = parts.Length > 1 ? parts[parts.Length - 1] : "16.0";
                var productName = parts.Length > 1 ? string.Join(" ", parts, 0, parts.Length - 1) : "Robur Road";

                const string prefix = "Topomatic ";
                if (productName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    productName = productName.Substring(prefix.Length);

                var appData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                var path = Path.Combine(appData, "Topomatic", productName, version, "Support", "Smdx", "materials.smdx");
                return File.Exists(path) ? path : null;
            }
            catch
            {
                return null;
            }
        }
    }
}
