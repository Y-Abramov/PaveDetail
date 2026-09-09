using System.Collections.Generic;

namespace PaveDetail.Core
{
    public sealed class ConstructionGroup
    {
        public string Key;
        public NodeInput Sample;                              // по нему строится узел
        public List<string> RegionNames = new List<string>(); // где применена
    }

    public sealed class BatchResult
    {
        public List<ConstructionGroup> Groups = new List<ConstructionGroup>();
        public List<string> Skipped = new List<string>();     // регионы без конструкции
    }

    public static class BatchGrouping
    {
        public static BatchResult Group(IEnumerable<NodeInput> nodes)
        {
            var result = new BatchResult();
            var index = new Dictionary<string, ConstructionGroup>();

            foreach (var node in nodes)
            {
                if (node.Layers == null || node.Layers.Count == 0)
                {
                    result.Skipped.Add(node.RegionName);
                    continue;
                }

                var key = ConstructionKey.Of(node.Layers);
                ConstructionGroup group;
                if (!index.TryGetValue(key, out group))
                {
                    group = new ConstructionGroup { Key = key, Sample = node };
                    index[key] = group;
                    result.Groups.Add(group);
                }
                group.RegionNames.Add(node.RegionName);
            }

            return result;
        }
    }
}
