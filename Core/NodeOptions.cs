namespace PaveDetail.Core
{
    public sealed class NodeOptions
    {
        // Умолчания приняты в плане от 2026-08-27; меняются в диалоге узла.
        public double WidthM = 1.5;              // ширина фрагмента конструкции
        public double TextHeightM = 0.08;        // высота текста подписей и экспликации
        public double ExplicationGapM = 0.40;    // отступ экспликации вправо от разреза
        public double SubgradeThicknessM = 0.20; // высота полосы «уплотнённый грунт основания»
        public double UnitScale = 1.0;   // множитель для геометрии, взятой из модели (толщина слоя, борт)

        // Копия опций под другие единицы модели - см. TableOptions.Scaled.
        public NodeOptions Scaled(double k)
        {
            return new NodeOptions
            {
                WidthM = WidthM * k,
                TextHeightM = TextHeightM * k,
                ExplicationGapM = ExplicationGapM * k,
                SubgradeThicknessM = SubgradeThicknessM * k,
                UnitScale = UnitScale * k
            };
        }
    }
}
