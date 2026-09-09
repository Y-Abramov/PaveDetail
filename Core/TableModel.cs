using System.Collections.Generic;

namespace PaveDetail.Core
{
    // Одна строка списка слоёв: что писать в графе «Материал слоя», что в графе толщины
    // и из какого слоя модели рисовать полосу мини-разреза.
    public sealed class TableLayer
    {
        public string Text;          // формулировка по ГОСТ или имя из модели
        public string ThicknessMm;   // «50», «-» для слоя без толщины
        public LayerInput Source;    // для штриховки мини-разреза
    }

    // Строка таблицы = один тип покрытия.
    public sealed class TableRow
    {
        public string Key;       // ConstructionKey, связь с файлом проекта
        public string Section;   // раздел: Проезжая часть дорог, Тротуары
        public string Code;      // шифр: ПД-4*
        public string Name;      // наименование покрытия
        public string Modulus;   // модуль упругости, МПа
        public string Note;      // примечание
        public List<TableLayer> Layers = new List<TableLayer>();
    }

    // Ширины граф заданы в долях высоты текста: пока узел живёт в единицах плана,
    // это единственный способ держать пропорции листа неизменными. В волне 2 они
    // станут производными от масштаба печати.
    public sealed class TableOptions
    {
        public double TextHeightM = 0.08;
        public double LineStepFactor = 1.6;   // межстрочный интервал внутри ячейки
        public double PaddingFactor = 0.5;    // поля ячейки слева и сверху
        public double SectionHeightM = 0.30;  // высота мини-разреза в графе «Сечение»
        public double MinBandM = 0.03;        // минимальная видимая полоса слоя
        public double UnitScale = 1.0;   // множитель для геометрии, взятой из модели (толщина слоя в мини-разрезе)

        public double CodeWidth(double h) { return h * 6; }
        public double NameWidth(double h) { return h * 10; }
        public double SectionWidth(double h) { return h * 11; }
        public double MaterialWidth(double h) { return h * 30; }
        public double ThicknessWidth(double h) { return h * 6; }
        public double ModulusWidth(double h) { return h * 6; }
        public double NoteWidth(double h) { return h * 14; }

        public double[] Widths()
        {
            double h = TextHeightM;
            return new[] { CodeWidth(h), NameWidth(h), SectionWidth(h),
                           MaterialWidth(h), ThicknessWidth(h), ModulusWidth(h), NoteWidth(h) };
        }

        // Копия опций под другие единицы модели: на плане площадки размеры в метрах,
        // на листе «Чертёж» - в единицах листа. Множители (LineStepFactor, PaddingFactor)
        // и ширины граф не трогаются: ширины уже выражены долями высоты текста.
        public TableOptions Scaled(double k)
        {
            return new TableOptions
            {
                TextHeightM = TextHeightM * k,
                LineStepFactor = LineStepFactor,
                PaddingFactor = PaddingFactor,
                SectionHeightM = SectionHeightM * k,
                MinBandM = MinBandM * k,
                UnitScale = UnitScale * k
            };
        }
    }
}
