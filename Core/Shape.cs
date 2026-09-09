namespace PaveDetail.Core
{
    // Примитивы вывода в координатах плана (метры). Y растёт вверх,
    // верх покрытия лежит на Y = 0, слои уходят в минус.
    public abstract class Shape
    {
    }

    public sealed class RectShape : Shape
    {
        public double Left, Right, Top, Bottom;
        public LayerStyle Style;
        public int ColorArgb;
    }

    public sealed class LineShape : Shape
    {
        public double X1, Y1, X2, Y2;
        public double Weight = 1;
        public bool IsLayer;   // true - это слой нулевой толщины, а не контур
    }

    public sealed class TextShape : Shape
    {
        public double X, Y;
        public string Text;
        public double Height;
        public bool IsExplication; // true - строка экспликации, false - заголовок или подпись
        public bool IsNumber;      // true - голая цифра номера слоя у грани разреза
        public bool Bold;
        public bool IsHeader;      // true - подпись графы в шапке таблицы
        public bool IsSectionTitle;// true - строка-раздел во всю ширину листа
    }
}
