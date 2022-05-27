using System;
using System.Linq;
using VDS.RDF;
using VDS.RDF.Nodes;

namespace OnViAT.Helpers
{
    public static class OntologyGraphExtensions
    {
        public static bool ContainClass(this IGraph graph, string uri)
        {
            try
            {
                var rdfType = graph.GetUriNode("rdf:type");
                var owlClass = graph.GetUriNode("owl:Class");

                var node = graph.GetUriNode(new Uri(uri));
                var triples = graph.GetTriplesWithPredicateObject(rdfType, owlClass);

                return node != null &&
                       triples
                           .Select(r => r.Subject.AsValuedNode().AsString())
                           .Any(r => r == node.AsValuedNode().AsString());
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
