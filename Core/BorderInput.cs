namespace PaveDetail.Core
{
    // Бортовой камень: либо снят с SiteBorder, либо взят из borders.json.
    public sealed class BorderInput
    {
        public string Name { get; set; }              // «БР 100.30.15»
        public double WidthM { get; set; }            // ширина камня
        public double HeightM { get; set; }           // полная высота камня
        public double RiseM { get; set; }             // возвышение над покрытием
        public string BeddingMaterial { get; set; }   // материал обоймы
        public double BeddingThicknessM { get; set; } // толщина обоймы под камнем
        public double BeddingOverhangM { get; set; }  // вынос обоймы в стороны
        public bool FromModel { get; set; }           // true - размеры сняты с SiteBorder
    }
}
