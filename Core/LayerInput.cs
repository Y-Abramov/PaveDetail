namespace PaveDetail.Core
{
    // Слой конструкции в том виде, в каком его понимает ядро: без единой ссылки на Topomatic.
    public sealed class LayerInput
    {
        public string Material { get; set; }      // имя материала из семантики слоя
        public double ThicknessM { get; set; }    // толщина в метрах; 0 = слой без толщины (геотекстиль)
        public int Code { get; set; }             // PavementLayer.Code, запасной ключ резолва
        public int ColorArgb { get; set; }        // цвет слоя из модели, используется при промахе правила
    }
}
