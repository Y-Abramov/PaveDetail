using System;
using System.IO;
using Topomatic.ApplicationPlatform.Core;

namespace PaveDetail.Cad
{
    // Где лежат данные таблицы, которых нет в модели. Рядом с проектом, а не в профиле:
    // шифры и примечания - часть работы над проектом и должны уезжать вместе с ним.
    internal static class ProjectPaths
    {
        private const string Extension = ".pavedetail.json";

        // PluginCoreOps.GetFileName возвращает голое имя файла без пути (проверено на
        // RailPassport живьём) - настоящий путь берётся из model.Uri.AsAbsoluteUri.
        internal static string DataFile(IProjectModel anyModel)
        {
            if (anyModel == null) return null;

            string modelPath = null;
            try { modelPath = new Uri(anyModel.Uri.AsAbsoluteUri).LocalPath; }
            catch { }
            if (string.IsNullOrEmpty(modelPath)) return null;

            try
            {
                // Модели лежат в подпапках проекта, .rbprojx - в корне: идём вверх.
                var dir = new DirectoryInfo(Path.GetDirectoryName(modelPath));
                while (dir != null)
                {
                    var projects = dir.GetFiles("*.rbprojx");
                    if (projects.Length > 0)
                        return Path.Combine(dir.FullName,
                            Path.GetFileNameWithoutExtension(projects[0].Name) + Extension);
                    dir = dir.Parent;
                }
            }
            catch { }
            return null;
        }

        internal static ProjectDataFile Open(IProjectModel anyModel)
        {
            return new ProjectDataFile(DataFile(anyModel));
        }
    }

    // Чтение и запись файла проекта. Отсутствие пути (проект не сохранён) - не ошибка:
    // таблица строится, данные просто не переживут сессию, о чём говорит диалог.
    internal sealed class ProjectDataFile
    {
        private readonly string _path;

        internal ProjectDataFile(string path) { _path = path; }

        internal bool HasPath { get { return !string.IsNullOrEmpty(_path); } }
        internal string Path { get { return _path; } }

        internal PaveDetail.Core.ProjectData Read()
        {
            if (!HasPath || !File.Exists(_path)) return new PaveDetail.Core.ProjectData();
            try { return PaveDetail.Core.ProjectData.Parse(File.ReadAllText(_path)); }
            catch { return new PaveDetail.Core.ProjectData(); }
        }

        internal bool Write(PaveDetail.Core.ProjectData data)
        {
            if (!HasPath) return false;
            try
            {
                File.WriteAllText(_path, PaveDetail.Core.ProjectData.ToJson(data), System.Text.Encoding.UTF8);
                return true;
            }
            catch { return false; }
        }
    }
}
