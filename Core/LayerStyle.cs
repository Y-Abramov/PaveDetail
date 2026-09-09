namespace PaveDetail.Core
{
    public sealed class LayerStyle
    {
        public string PatternName = "SOLID"; // имя штриховки Robur
        public double Scale = 0.02;          // масштаб штриховки в единицах плана
        public double Angle;                 // угол штриховки, градусы
        public bool RenderAsLine;            // true - слой рисуется линией, а не полосой
        public double LineWeight = 3;        // толщина линии для RenderAsLine
        public bool UseLayerColor;           // true - заливать цветом слоя из модели
    }
}
