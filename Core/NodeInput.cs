using System.Collections.Generic;

namespace PaveDetail.Core
{
    public enum NodeKind
    {
        Simple = 0,     // только слои
        WithBorder = 1, // слои + борт слева
        Junction = 2    // две конструкции по обе стороны борта
    }

    public sealed class NodeInput
    {
        public NodeInput()
        {
            Layers = new List<LayerInput>();
            RightLayers = new List<LayerInput>();
        }

        public string RegionName { get; set; }             // имя региона слева
        public string RightRegionName { get; set; }        // имя региона справа, только для Junction
        public IList<LayerInput> Layers { get; set; }      // сверху вниз
        public IList<LayerInput> RightLayers { get; set; } // сверху вниз, только для Junction
        public BorderInput Border { get; set; }            // null = без борта
        public NodeKind Kind { get; set; }
    }
}
