using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace PaveDetail.Core
{
    public sealed class BorderCatalog
    {
        public List<BorderInput> Borders = new List<BorderInput>();
        public string LoadError;

        public static BorderCatalog BuiltIn()
        {
            var c = new BorderCatalog();
            c.Borders.Add(new BorderInput
            {
                Name = "БР 100.30.15", WidthM = 0.15, HeightM = 0.30, RiseM = 0.15,
                BeddingMaterial = "Бетон класса В15", BeddingThicknessM = 0.10, BeddingOverhangM = 0.05
            });
            c.Borders.Add(new BorderInput
            {
                Name = "БР 100.20.8", WidthM = 0.08, HeightM = 0.20, RiseM = 0.05,
                BeddingMaterial = "Бетон класса В15", BeddingThicknessM = 0.10, BeddingOverhangM = 0.04
            });
            return c;
        }

        public static BorderCatalog Parse(string json)
        {
            try
            {
                var dto = new JavaScriptSerializer().Deserialize<Dto>(json);
                if (dto == null || dto.borders == null || dto.borders.Count == 0)
                    throw new InvalidOperationException("пустой каталог бортов");

                var c = new BorderCatalog();
                foreach (var b in dto.borders)
                {
                    c.Borders.Add(new BorderInput
                    {
                        Name = b.name,
                        WidthM = b.width,
                        HeightM = b.height,
                        RiseM = b.rise,
                        BeddingMaterial = b.bedding == null ? null : b.bedding.material,
                        BeddingThicknessM = b.bedding == null ? 0 : b.bedding.thickness,
                        BeddingOverhangM = b.bedding == null ? 0 : b.bedding.overhang
                    });
                }
                return c;
            }
            catch (Exception ex)
            {
                var c = BuiltIn();
                c.LoadError = ex.Message;
                return c;
            }
        }

        private sealed class Dto { public List<BorderDto> borders { get; set; } }

        private sealed class BorderDto
        {
            public string name { get; set; }
            public double width { get; set; }
            public double height { get; set; }
            public double rise { get; set; }
            public BeddingDto bedding { get; set; }
        }

        private sealed class BeddingDto
        {
            public string material { get; set; }
            public double thickness { get; set; }
            public double overhang { get; set; }
        }
    }
}
