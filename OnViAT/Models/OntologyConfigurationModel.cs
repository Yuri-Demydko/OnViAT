using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OnViAT.Enums;
using OnViAT.Helpers;
using VDS.RDF;
using VDS.RDF.Nodes;
using VDS.RDF.Parsing;
using VDS.RDF.Writing;
using StringWriter = VDS.RDF.Writing.StringWriter;

namespace OnViAT.Models
{
    public class OntologyOperation
    {
        public string OldUri { get; set; }
        public string NewUri { get; set; }
        public string NeighbourUri { get; set; }
        public OntologyOperationKind Kind { get; set; }
    }
    public class OntologyConfigurationModel
    {
        private IGraph baseGraph;
        
        private IGraph additionsGraph;
        
        private List<OntologyOperation> operations=new List<OntologyOperation>();

        public List<OntologyOperation> Operations => new List<OntologyOperation>(operations);

        public OntologyConfigurationModel(string baseGraphPath, string additionsGraphPath=null)
        {
            baseGraph = new Graph();
            //standardGraph = new Graph();
            RdfXmlParser parser = new RdfXmlParser();
            parser.Load(baseGraph, baseGraphPath);
            //parser.Load(standardGraph,baseGraphPath);
            additionsGraph = new Graph();
            additionsGraph.NamespaceMap.Import(baseGraph.NamespaceMap);
            
            
            if (!string.IsNullOrWhiteSpace(additionsGraphPath)&&File.Exists(additionsGraphPath))
            {
                parser.Load(additionsGraph, additionsGraphPath);
            }
        }

        public string SaveAdditionsGraph(string path=null)
        {
            path = path ?? Path.Combine(Directory.GetCurrentDirectory(), Constants.Constants.ADDITION_GRAPH_CACHE);
            var writer = new RdfXmlWriter();

            var graphString = StringWriter.Write(additionsGraph, writer)+Constants.Constants.ONTOLOGY_ADDITION_MARKER;
            
            File.WriteAllText(path,graphString);
            
            return path;
        }

        public void AddParallelClass(string parallelParentUri, string parallelChildUri)
        {
            baseGraph.Merge(additionsGraph);
            
            var (rdfType,owlClass,rdfsSubClassOf)= GetBasicUriNodes();

            var parallelParent = baseGraph.GetUriNode(new Uri(parallelParentUri));
            var parent = baseGraph.GetTriplesWithSubjectPredicate(parallelParent, rdfsSubClassOf).FirstOrDefault()?.Object;
            
            var triplesBase=baseGraph.GetTriplesWithPredicateObject(rdfType, owlClass);

            var triplesAdd = additionsGraph.GetTriplesWithPredicateObject(rdfType, owlClass);
            var permit = triplesBase.Union(triplesAdd).Select(r => r.Subject)
                              .All(r => r.AsValuedNode().AsString() != $"{Constants.Constants.BASE_URI}#{parallelChildUri}")
                          &&parent!=null
                          &&!SealedOntologyClassesHelper.Sealed(parent.AsValuedNode().AsString())
                          &&!ParallelSealedOntologyClassesHelper.ParallelSealed(parallelParentUri);

            if (!permit)
                throw new Exception(
                    "class URI duplication, sealed parent class or parallel sealed parallel parent class");
            
            var newClass = additionsGraph.CreateUriNode($":{parallelChildUri}");
            var newClassTriple = new Triple(newClass.CopyNode(additionsGraph), rdfType.CopyNode(additionsGraph), owlClass.CopyNode(additionsGraph));
            var subClassTriple = new Triple(newClass.CopyNode(additionsGraph), rdfsSubClassOf.CopyNode(additionsGraph), parent.CopyNode(additionsGraph));

            additionsGraph.Assert(newClassTriple);
            additionsGraph.Assert(subClassTriple);
            
            operations.Add(new OntologyOperation()
            {
                Kind = OntologyOperationKind.AddParallelClass,
                NewUri = $"{Constants.Constants.BASE_URI}#{parallelChildUri}",
                OldUri = null,
                NeighbourUri = parallelParentUri
            });
        }
        
        public void AddSubClass(string parentUri, string childClassName)
        {
            baseGraph.Merge(additionsGraph);
            bool permit = true;
            var (rdfType,owlClass,rdfsSubClassOf)= GetBasicUriNodes();

            var parent = baseGraph.GetUriNode(new Uri(parentUri));
            
            var triplesBase=baseGraph.GetTriplesWithPredicateObject(rdfType, owlClass);

            var triplesAdd = additionsGraph.GetTriplesWithPredicateObject(rdfType, owlClass);
            permit = 
                triplesBase.Union(triplesAdd).Select(r => r.Subject)
                    .All(r => r.AsValuedNode().AsString() != $"{Constants.Constants.BASE_URI}#{childClassName}")
                     &&!SealedOntologyClassesHelper.Sealed(parentUri)
                     &&parent!=null;
            if (!permit)
                throw new Exception("class URI duplication or sealed parent class");
            
            var newClass = additionsGraph.CreateUriNode(new Uri($"{Constants.Constants.BASE_URI}#{childClassName}"));
            var newClassTriple = new Triple(newClass.CopyNode(additionsGraph), rdfType.CopyNode(additionsGraph), owlClass.CopyNode(additionsGraph));
            var subClassTriple = new Triple(newClass.CopyNode(additionsGraph), rdfsSubClassOf.CopyNode(additionsGraph), parent.CopyNode(additionsGraph));

            additionsGraph.Assert(newClassTriple);
            additionsGraph.Assert(subClassTriple);
            
            operations.Add(new OntologyOperation()
            {
                Kind = OntologyOperationKind.AddSubClass,
                NewUri = $"{Constants.Constants.BASE_URI}#{childClassName}",
                OldUri = null,
                NeighbourUri = parentUri
            });

        }

        public void RemoveCustomClass(string oldUri)
        {
            if (!additionsGraph.ContainClass(oldUri))
            {
                throw new Exception($"Graph doesn't contain such URI: {oldUri}");
            }
            
            var oldNode = additionsGraph.GetUriNode(new Uri(oldUri));
            
            var triplesAddition = 
                additionsGraph.GetTriplesWithSubject(oldNode)
                    .Union(additionsGraph.GetTriplesWithObject(oldNode));
            
            var triplesBase = 
                baseGraph.GetTriplesWithSubject(oldNode)
                    .Union(baseGraph.GetTriplesWithObject(oldNode));

            foreach (var triple in triplesAddition)
            {
                additionsGraph.Retract(triple);
            }
            
            foreach (var triple in triplesBase)
            {
                additionsGraph.Retract(triple);
            }
            
            operations.Add(new OntologyOperation()
            {
                Kind = OntologyOperationKind.RemoveClass,
                NewUri = null,
                OldUri = oldUri,
                NeighbourUri = null
            });
        }

        public bool IsCustomClass(string uri)
        {
            return additionsGraph.ContainClass(uri);
        }
        
        public void RenameCustomClass(string oldUri, string newName)
        {
            //baseGraph.Merge(additionsGraph);

            // if (standardGraph.ContainClass(oldUri))
            // {
            //     throw new Exception($"RenameClass restricted on that URI: {oldUri}");
            // }

            if (!IsCustomClass(oldUri))
            {
                throw new Exception($"Graph doesn't contain such URI: {oldUri}");
            }

            if (additionsGraph.ContainClass($"{Constants.Constants.BASE_URI}#{newName}"))
            {
                throw new Exception($"Graph already contain that URI: {Constants.Constants.BASE_URI}#{newName}");
            }

            var oldNode = additionsGraph.GetUriNode(new Uri(oldUri));
            var newNode = additionsGraph.CreateUriNode(new Uri($"{Constants.Constants.BASE_URI}#{newName}"));

            var triples = 
                additionsGraph.GetTriplesWithSubject(oldNode)
                    .Union(additionsGraph.GetTriplesWithObject(oldNode));
            
            foreach (var triple in triples)
            {
                var newTriple = triple.Object.AsValuedNode().AsString() == oldNode.AsValuedNode().AsString() ? 
                    new Triple(triple.Subject, triple.Predicate, newNode) : 
                    new Triple(newNode, triple.Predicate, triple.Object);

                additionsGraph.Retract(triple);

                additionsGraph.Assert(newTriple);

            }

            operations.Add(new OntologyOperation()
            {
                Kind = OntologyOperationKind.RenameClass,
                NewUri = $"{Constants.Constants.BASE_URI}#{newName}",
                OldUri = oldUri,
                NeighbourUri = null
            });
        }
        
        private (IUriNode, IUriNode, IUriNode) GetBasicUriNodes()
        {
            return (baseGraph.GetUriNode("rdf:type"), baseGraph.GetUriNode("owl:Class"),
                baseGraph.GetUriNode("rdfs:subClassOf"));
        }
    }
}