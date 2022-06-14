using System.Reflection;

namespace OnViAT.Paths
{
    public class Paths
    {
        public static  string BASE_ONTOLOGY =>$"{System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}/Resources/BASE_ONTOLOGY.owl";
        public static string MARKUP_BASE =>$"{System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}/Resources/MARKUP_BASE.owl";
        public static string ADDITION_GRAPH_CACHE => $"{System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}/Resources/OnViAT_Addition_Graph_Cache.xml";//"OnViAT_Addition_Graph_Cache.xml";
    }
}