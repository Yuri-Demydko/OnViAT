using System.IO;

namespace OnViAT.Helpers
{
    public static class AdditionalGraphCachePathHelper
    {
        public static string CheckCache()
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), Constants.Constants.ADDITION_GRAPH_CACHE);

            if (File.Exists(path) &&
                OntologyGraphValidatorHelper.IsValid(File.ReadAllText(path), Constants.Constants.ONTOLOGY_ADDITION_MARKER))
            {
                return path;
            }

            return null;
        }
    }
}