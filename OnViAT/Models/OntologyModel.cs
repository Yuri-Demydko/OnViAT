using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using OnViAT.Enums;
using OnViAT.ViewModels;
//using Microsoft.CodeAnalysis.CSharp.Syntax;
using VDS.RDF;
using VDS.RDF.Nodes;
using VDS.RDF.Parsing;
using VDS.RDF.Writing;
using StringWriter = VDS.RDF.Writing.StringWriter;

namespace OnViAT.Models
{
    public class OntologyModel
    {
        private IGraph _markupGraph;
        private IGraph _baseGraph;
        private IGraph _additionsGraph;
        public OntologyModel(string editableGraphPath, string baseGraphPath, string additionsGraphPath=null)
        {
            try
            {
                _markupGraph = new Graph();
                _baseGraph = new Graph();
                RdfXmlParser parser = new RdfXmlParser();
                parser.Load(_markupGraph, editableGraphPath);
                parser.Load(_baseGraph, baseGraphPath);

                if (additionsGraphPath != null&&File.Exists(additionsGraphPath))
                {
                    _additionsGraph = new Graph();
                    parser.Load(_additionsGraph,additionsGraphPath);
                    _baseGraph.Merge(_additionsGraph);
                    _additionsGraph.NamespaceMap.Import(_baseGraph.NamespaceMap);
                }
            }
            catch (RdfParseException parseEx)
            {
                Console.WriteLine("Parser Error");
                Console.WriteLine(parseEx.Message);
            }
            catch (RdfException rdfEx)
            {
                Console.WriteLine("RDF Error");
                Console.WriteLine(rdfEx.Message);
            }
        }

        public void SetAdditionsGraph(string path)
        {
            RdfXmlParser parser = new RdfXmlParser();
            if (path != null&&File.Exists(path))
            {
                _additionsGraph = new Graph();
                parser.Load(_additionsGraph,path);
                _baseGraph.Merge(_additionsGraph);
            }
        }

        public void ApplyOperations(List<OntologyOperation> operations)
        {
            foreach (var op in operations)
            {
                switch (op.Kind)
                {
                    case OntologyOperationKind.RemoveClass:
                    {
                        var oldNode = _markupGraph.GetUriNode(new Uri(op.OldUri));
                        var newNode = _baseGraph.GetUriNode(new Uri($"{Constants.Constants.BASE_URI}#ContentObject")).CopyNode(_markupGraph);
                        if(oldNode==null) continue;
                        var triplesOldObj = _markupGraph.GetTriplesWithObject(oldNode);
                        var triplesOldSubj=   _markupGraph.GetTriplesWithSubject(oldNode);

                        foreach (var triple in triplesOldObj)
                        {
                            _markupGraph.Retract(triple);
                            _markupGraph.Assert(new Triple(triple.Subject, triple.Predicate, newNode));
                        }
                        
                        foreach (var triple in triplesOldSubj)
                        {
                            _markupGraph.Retract(triple);
                            _markupGraph.Assert(new Triple(newNode, triple.Predicate, triple.Object));
                        }
                    }
                        break;
                    case OntologyOperationKind.RenameClass:
                    {
                        var oldNode = _markupGraph.GetUriNode(new Uri(op.OldUri));
                        if(oldNode==null) continue;
                        var newNode = _markupGraph.CreateUriNode(new Uri(op.NewUri));
                        var triplesOldObj = _markupGraph.GetTriplesWithObject(oldNode);
                        var triplesOldSubj=   _markupGraph.GetTriplesWithSubject(oldNode);

                        foreach (var triple in triplesOldObj)
                        {
                            _markupGraph.Retract(triple);
                            _markupGraph.Assert(new Triple(triple.Subject, triple.Predicate, newNode.CopyNode(_markupGraph)));
                        }
                        
                        foreach (var triple in triplesOldSubj)
                        {
                            _markupGraph.Retract(triple);
                            _markupGraph.Assert(new Triple(newNode, triple.Predicate, triple.Object));
                        }
                        
                        break;
                    }
                    default:
                        continue;
                }
            }
        }

        public OntologyModel(string ontology)
        {
            try
            {
                _markupGraph = new Graph();//инициализация графа разметки
                RdfXmlParser parser = new RdfXmlParser(RdfXmlParserMode.Streaming); //создание объекта парсера
                parser.Load(_markupGraph, new StringReader(ontology));//парсинг строкового представления онтологии в граф
            }
            catch (RdfParseException parseEx)
            {
                Console.WriteLine("Parser Error");
                Console.WriteLine(parseEx.Message);
            }
            catch (RdfException rdfEx)
            {
                Console.WriteLine("RDF Error");
                Console.WriteLine(rdfEx.Message);
            }
        }
        public void InitBaseGraph(string baseGraphPath)
        {
            if (_baseGraph == null)
            {
                _baseGraph = new Graph();
                RdfXmlParser parser = new RdfXmlParser();
                parser.Load(_baseGraph, baseGraphPath);
                // _tripleStore.Add(_baseGraph);
            }
            else throw new Exception("Base graph already set!");
        }

        public string ExportGraphAsString()
        {
            var rdfXmlWriter = new RdfXmlWriter();
            var graphString = StringWriter.Write(_markupGraph, rdfXmlWriter);

            return graphString;
        }

        public IGraph ExportGraph()
        {
            return new Graph(_markupGraph.Triples);
        }

        public void RegenerateMediaResourceUri()
        {
            var resNode = GetMediaResourceNode();
            IUriNode rdfType = _markupGraph.GetUriNode("rdf:type");
            IUriNode mediaResourceType = _markupGraph.GetUriNode("ma-ont:MediaResource");

            //Генерация и запись нового URI файла
            var triple = _markupGraph.GetTriplesWithPredicateObject(rdfType,mediaResourceType )
                .WithSubject(resNode).FirstOrDefault();
            if (triple == null) return;
            _markupGraph.Retract(triple);
            IUriNode newRes = _markupGraph.CreateUriNode(":" + GenerateUniqueId());
            Triple newIsMediaRes = new Triple(newRes, rdfType, mediaResourceType);
            _markupGraph.Assert(newIsMediaRes);

            //Замена всех вхождений старого URI на новый
            var oldTriples = _markupGraph.GetTriplesWithSubject(resNode).ToList();
            for(int i=0; i<oldTriples.Count;i++)
            {
                var tr = oldTriples[i];
                var obj = tr.Object;
                var pred = tr.Predicate;
                _markupGraph.Retract(tr);
                var newTr = new Triple(newRes, pred, obj);
                _markupGraph.Assert(newTr);
            }
            
            //Генерация новых URI всем фрагментам
            IUriNode hasFragmentProperty = _markupGraph.GetUriNode("ma-ont:hasFragment");
            if (hasFragmentProperty == null)
                return;
            var hasFragmentTriplesOld = _markupGraph.GetTriplesWithSubjectPredicate(newRes, hasFragmentProperty).ToList();
            IUriNode fragmentType = _markupGraph.CreateUriNode("ma-ont:MediaFragment"); 
            for (int i = 0; i < hasFragmentTriplesOld.Count; i++)
            {
                var tr = hasFragmentTriplesOld[i];
                var obj = tr.Object;

                var isFragTr = _markupGraph.GetTriplesWithPredicateObject(rdfType,fragmentType )
                    .WithSubject(obj).FirstOrDefault();
                if(isFragTr==null) continue;
                _markupGraph.Retract(isFragTr);
                
                IUriNode newFrag = _markupGraph.CreateUriNode(":" + GenerateUniqueId());
                Triple newIsFrag = new Triple(newFrag, rdfType, fragmentType);
                _markupGraph.Assert(newIsFrag);

                _markupGraph.Retract(tr);
                var newHasFrag = new Triple(newRes,hasFragmentProperty,newFrag);
                _markupGraph.Assert(newHasFrag);
                
                var oldTriplesWFrag = _markupGraph.GetTriplesWithSubject(obj).ToList();

                for (int j = 0; j < oldTriplesWFrag.Count; j++)
                {
                    var oftr = oldTriplesWFrag[j];
                    var ofObj = oftr.Object;
                    var ofPred = oftr.Predicate;
                    _markupGraph.Retract(oftr);
                    var nfTr = new Triple(newFrag, ofPred, ofObj);
                    _markupGraph.Assert(nfTr);
                }
            }
            
            //Генерация новых URI всем индивидам
            var hasCoProperty = _markupGraph.GetUriNode(":HasContentObject");
            if (hasCoProperty == null)
                return;
            var coTriples = _markupGraph.GetTriplesWithPredicate(hasCoProperty).ToList();
            for (int i = 0; i < coTriples.Count; i++)
            {
                var indTr = coTriples[i];
                var ind = indTr.Object;
                _markupGraph.Retract(indTr);

                var indClassTr = _markupGraph.GetTriplesWithSubjectPredicate(ind, rdfType).First();
                _markupGraph.Retract(indClassTr);
                
                var indClass = indClassTr.Object;
                
                IUriNode newInd = _markupGraph.CreateUriNode(":" + GenerateUniqueId());
                
                 var newIndClassTr = new Triple(newInd,rdfType,indClass);
                 _markupGraph.Assert(newIndClassTr);

                 var newIndTr = new Triple(indTr.Subject, hasCoProperty, newInd);
                 _markupGraph.Assert(newIndTr);
                 
                 var oldTriplesWInd = _markupGraph.GetTriplesWithSubject(ind).ToList();
                 
                 for (int j = 0; j < oldTriplesWInd.Count; j++)
                 {
                     var oiTr = oldTriplesWInd[j];
                     var oiObj = oiTr.Object;
                     var oiPred = oiTr.Predicate;
                     _markupGraph.Retract(oiTr);
                     var niTr = new Triple(newInd, oiPred, oiObj);
                     _markupGraph.Assert(niTr);
                 }
            }
        }
        
        public static ClassHierarchyTreeModel ExportOntologyClassesAsTree(string baseGraphPath, string additionalGraphPath=null)
        {
            var baseGraph = new Graph();
            RdfXmlParser parser = new RdfXmlParser();
            parser.Load(baseGraph, baseGraphPath);

            if (!string.IsNullOrWhiteSpace(additionalGraphPath))
            {
                var additionalGraph = new Graph();
                parser.Load(additionalGraph,additionalGraphPath);
                additionalGraph.NamespaceMap.Import(baseGraph.NamespaceMap);
                baseGraph.Merge(additionalGraph);
            }

            IUriNode rdfsSubclassOf = baseGraph.GetUriNode("rdfs:subClassOf");
            IUriNode contentObjectClass = baseGraph.GetUriNode(":ContentObject");

            ClassHierarchyTreeModel res =
                new ClassHierarchyTreeModel("ContentObject", contentObjectClass.AsValuedNode().AsString());
            GetSubHierarchy(res);

            void GetSubHierarchy(ClassHierarchyTreeModel parent)
            {
                IUriNode parentNode = baseGraph.GetUriNode(new Uri(parent.URI));
                var triples = baseGraph.GetTriplesWithPredicateObject(rdfsSubclassOf, parentNode);
                foreach (var triple in triples)
                {
                    IUriNode subject = triple.Subject as IUriNode;
                    ClassHierarchyTreeModel objTm = new ClassHierarchyTreeModel(
                        subject.AsValuedNode().AsString().Split("#").Last(), subject.AsValuedNode().AsString());
                    var subtriples = baseGraph.GetTriplesWithPredicateObject(rdfsSubclassOf, subject);
                    if (subtriples.Any())
                        GetSubHierarchy(objTm);
                    parent.SubNodes.Add(objTm);
                }
            }

            return res;
        }
        public ClassHierarchyTreeModel ExportGraphAsTree()
        {
            _additionsGraph = _additionsGraph ?? new Graph();
            _additionsGraph.NamespaceMap.Import(_baseGraph.NamespaceMap);
            _additionsGraph.Merge(_baseGraph,true);

            IUriNode rdfsSubclassOf = _additionsGraph.GetUriNode("rdfs:subClassOf");
            IUriNode contentObjectClass = _additionsGraph.GetUriNode(":ContentObject");

            ClassHierarchyTreeModel res =
                new ClassHierarchyTreeModel("ContentObject", contentObjectClass.AsValuedNode().AsString());
            GetSubHierarchy(res);

            void GetSubHierarchy(ClassHierarchyTreeModel parent)
            {
                IUriNode parentNode = _additionsGraph.GetUriNode(new Uri(parent.URI));
                var triples = _additionsGraph.GetTriplesWithPredicateObject(rdfsSubclassOf, parentNode);
                foreach (var triple in triples)
                {
                    IUriNode subject = triple.Subject as IUriNode;
                    ClassHierarchyTreeModel objTm = new ClassHierarchyTreeModel(
                        subject.AsValuedNode().AsString().Split("#").Last(), subject.AsValuedNode().AsString());
                    var subtriples = _additionsGraph.GetTriplesWithPredicateObject(rdfsSubclassOf, subject);
                    if (subtriples.Any())
                        GetSubHierarchy(objTm);
                    parent.SubNodes.Add(objTm);
                }
            }

            return res;
        }
        public string InitMediaResourceMetadata(string title, string description, DateTime creationDate,
            DateTime editDate, string durationSeconds)
        {
            IUriNode mediaResourceType = _markupGraph.CreateUriNode("ma-ont:MediaResource");

            IUriNode mediaResource = _markupGraph.CreateUriNode(":" + GenerateUniqueId());
            IUriNode rdfType = _markupGraph.CreateUriNode("rdf:type");
            Triple isMediaResource = new Triple(mediaResource, rdfType, mediaResourceType);
            _markupGraph.Assert(isMediaResource);

            //title init
            ILiteralNode titleLiteral =
                _markupGraph.CreateLiteralNode(title, UriFactory.Create(XmlSpecsHelper.XmlSchemaDataTypeString));
            IUriNode titleProperty = _markupGraph.CreateUriNode("ma-ont:title");
            Triple titleTriple = new Triple(mediaResource, titleProperty, titleLiteral);
            _markupGraph.Assert(titleTriple);

            //description init
            ILiteralNode descLiteral =
                _markupGraph.CreateLiteralNode(description, UriFactory.Create(XmlSpecsHelper.XmlSchemaDataTypeString));
            IUriNode descProperty = _markupGraph.CreateUriNode("ma-ont:description");
            Triple descTriple = new Triple(mediaResource, descProperty, descLiteral);
            _markupGraph.Assert(descTriple);

            //creation date init
            ILiteralNode crDateLiteral = _markupGraph.CreateLiteralNode(creationDate.ToString("yyyy-MM-dd"),
                UriFactory.Create(XmlSpecsHelper.XmlSchemaDataTypeDate));
            IUriNode crDateProperty = _markupGraph.CreateUriNode("ma-ont:creationDate");
            Triple crDateTriple = new Triple(mediaResource, crDateProperty, crDateLiteral);
            _markupGraph.Assert(crDateTriple);
            //edit date init
            ILiteralNode edDateLiteral = _markupGraph.CreateLiteralNode(editDate.ToString("yyyy-MM-dd"),
                UriFactory.Create(XmlSpecsHelper.XmlSchemaDataTypeDate));
            IUriNode edDateProperty = _markupGraph.CreateUriNode("ma-ont:editDate");
            Triple edDateTriple = new Triple(mediaResource, edDateProperty, edDateLiteral);
            _markupGraph.Assert(edDateTriple);
            //duration init
            ILiteralNode durationLiteral = _markupGraph.CreateLiteralNode(durationSeconds,
                UriFactory.Create(XmlSpecsHelper.XmlSchemaDataTypeDecimal));
            IUriNode durationProperty = _markupGraph.CreateUriNode("ma-ont:duration");
            Triple durationTriple = new Triple(mediaResource, durationProperty, durationLiteral);
            _markupGraph.Assert(durationTriple);

            return ExportGraphAsString();
        }
        public Dictionary<MetaDataTypes, string> GetMediaResourceMetadata()
        {
            var res = new Dictionary<MetaDataTypes, string>();

            IUriNode titleProperty = _markupGraph.GetUriNode("ma-ont:title");
            IUriNode descProperty = _markupGraph.GetUriNode("ma-ont:description");
            IUriNode crDateProperty = _markupGraph.GetUriNode("ma-ont:creationDate");
            IUriNode edDateProperty = _markupGraph.GetUriNode("ma-ont:editDate");
            IUriNode durationProperty = _markupGraph.GetUriNode("ma-ont:duration");

            var mediaResource = GetMediaResourceNode();

            var mediaResourceTriples = _markupGraph.GetTriplesWithSubject(mediaResource);
            foreach (var triple in mediaResourceTriples)
            {
                if (triple.HasPredicate(titleProperty))
                {
                    res.Add(MetaDataTypes.Title, triple.Object.AsValuedNode().AsString());
                }

                if (triple.HasPredicate(descProperty))
                {
                    res.Add(MetaDataTypes.Description, triple.Object.AsValuedNode().AsString());
                }

                if (triple.HasPredicate(crDateProperty))
                {
                    res.Add(MetaDataTypes.CreationDate, triple.Object.AsValuedNode().AsString());
                }

                if (triple.HasPredicate(edDateProperty))
                {
                    res.Add(MetaDataTypes.EditDate, triple.Object.AsValuedNode().AsString());
                }

                if (triple.HasPredicate(durationProperty))
                {
                    res.Add(MetaDataTypes.Duration, triple.Object.AsValuedNode().AsString());
                }
            }

            return res;
        }
        public string GetMediaResourceUri()
        {
            return GetMediaResourceNode().AsValuedNode().AsString();
        }
        private INode GetMediaResourceNode()
        {
            IUriNode mediaResourceType = _markupGraph.GetUriNode("ma-ont:MediaResource");
            IUriNode rdfType = _markupGraph.GetUriNode("rdf:type");
            var mrTriple = _markupGraph.GetTriplesWithPredicateObject(rdfType, mediaResourceType).First();
            return mrTriple.Subject;
        }
        public void EditMediaResourceMetadata(string data, MetaDataTypes type)
        {
            var mediaResource = GetMediaResourceNode();
            switch (type)
            {
                case MetaDataTypes.Title:
                {
                    IUriNode titleProperty = _markupGraph.GetUriNode("ma-ont:title");
                    Triple titleTriple =
                        _markupGraph.GetTriplesWithSubjectPredicate(mediaResource, titleProperty).First();
                    _markupGraph.Retract(titleTriple);

                    ILiteralNode newTitleLiteral =
                        _markupGraph.CreateLiteralNode(data, UriFactory.Create(XmlSpecsHelper.XmlSchemaDataTypeString));
                    Triple newTitleTriple = new Triple(mediaResource, titleProperty, newTitleLiteral);
                    _markupGraph.Assert(newTitleTriple);

                    break;
                }
                case MetaDataTypes.Description:
                {
                    IUriNode descProperty = _markupGraph.GetUriNode("ma-ont:description");
                    Triple descTriple =
                        _markupGraph.GetTriplesWithSubjectPredicate(mediaResource, descProperty).First();
                    _markupGraph.Retract(descTriple);

                    ILiteralNode newDescLiteral =
                        _markupGraph.CreateLiteralNode(data, UriFactory.Create(XmlSpecsHelper.XmlSchemaDataTypeString));
                    Triple newDescTriple = new Triple(mediaResource, descProperty, newDescLiteral);
                    _markupGraph.Assert(newDescTriple);

                    break;
                }
                case MetaDataTypes.EditDate:
                {
                    IUriNode edProperty = _markupGraph.GetUriNode("ma-ont:editDate");
                    Triple edTriple =
                        _markupGraph.GetTriplesWithSubjectPredicate(mediaResource, edProperty).First();
                    _markupGraph.Retract(edTriple);
                    
                    ILiteralNode newEdLiteral =
                        _markupGraph.CreateLiteralNode(data, UriFactory.Create(XmlSpecsHelper.XmlSchemaDataTypeString));
                    Triple newEdTriple = new Triple(mediaResource, edProperty, newEdLiteral);
                    _markupGraph.Assert(newEdTriple);

                    break;
                }
                default:
                {
                    throw new ArgumentException("Unable to change that type of Metadata!");
                }
            }
        }
        public void RemoveMediaFragment(string id)
        {
            var fragment = _markupGraph.GetUriNode(new Uri(id));
        
            if (fragment!=null)
            {
                var allTriplesForFragment = _markupGraph.GetTriplesWithSubject(fragment)
                    .Union(_markupGraph.GetTriplesWithObject(fragment)).ToList();
                for (int i = 0; i < allTriplesForFragment.Count(); i++)
                    _markupGraph.Retract(
                        allTriplesForFragment[i]);
            }
        }
        public void RemoveCoIndividual(string uri)
        {
            var individual = _markupGraph.GetUriNode(new Uri(uri));
            if (individual != null)
            {
                var allTriplesForIndividual = _markupGraph.GetTriplesWithSubject(individual)
                    .Union(_markupGraph.GetTriplesWithObject(individual)).ToList();
                for (int i = 0; i < allTriplesForIndividual.Count(); i++)
                    _markupGraph.Retract(
                        allTriplesForIndividual[i]);
            }
        }
        public void EditMediaFragment(string id, string start, string end, string description)
        {
            var resource = GetMediaResourceNode();
            IUriNode hasFragmentProperty = _markupGraph.GetUriNode("ma-ont:hasFragment");
            if (hasFragmentProperty != null)
            {
                IUriNode beginsAtProperty = _markupGraph.GetUriNode("timeline:beginsAt");
                IUriNode endsAtProperty = _markupGraph.GetUriNode("timeline:EndsAt");
                IUriNode fragmentDescriptionProperty = _markupGraph.GetUriNode("ma-ont:description");
                
                var triples = _markupGraph.GetTriplesWithSubjectPredicate(resource, hasFragmentProperty);
                foreach (var triple in triples)
                {
                    var tId = triple.Object.AsValuedNode().AsString();
                    if (tId == id)
                    {
                        var beginsT = _markupGraph.GetTriplesWithSubjectPredicate(triple.Object, beginsAtProperty).First();
                        var endsT = _markupGraph.GetTriplesWithSubjectPredicate(triple.Object, endsAtProperty).First();
                        var descT = _markupGraph.GetTriplesWithSubjectPredicate(triple.Object, fragmentDescriptionProperty).First();
                        _markupGraph.Retract(beginsT);
                        _markupGraph.Retract(endsT);
                        _markupGraph.Retract(descT);
                        _markupGraph.Assert(new Triple(beginsT.Subject, beginsT.Predicate,
                            _markupGraph.CreateLiteralNode(start,
                                UriFactory.Create(XmlSpecsHelper.XmlSchemaDataTypeString))));
                        _markupGraph.Assert(new Triple(endsT.Subject, endsT.Predicate,
                            _markupGraph.CreateLiteralNode(end,
                                UriFactory.Create(XmlSpecsHelper.XmlSchemaDataTypeString))));
                        _markupGraph.Assert(new Triple(descT.Subject, descT.Predicate,
                            _markupGraph.CreateLiteralNode(description,
                                UriFactory.Create(XmlSpecsHelper.XmlSchemaDataTypeString))));
                    }
                }
            }
        }
        public void EditContentObjectIndividual(string individualUri, string uoname,
            string quantity)
        {
            var indvidualNode = _markupGraph.GetUriNode(new Uri(individualUri));
            if (indvidualNode != null)
            {
                var uonameProperty = _markupGraph.GetUriNode(":UniversalObjectName");
                var qtyProperty =  _markupGraph.GetUriNode(":QuantityOfSameObjects");

                var uonameTriple = _markupGraph.GetTriplesWithSubjectPredicate(indvidualNode, uonameProperty).First();
                var qtyTriple = _markupGraph.GetTriplesWithSubjectPredicate(indvidualNode, qtyProperty).First();
                _markupGraph.Retract(uonameTriple);
                _markupGraph.Retract(qtyTriple);
                
                _markupGraph.Assert(new Triple(uonameTriple.Subject, uonameTriple.Predicate,
                    _markupGraph.CreateLiteralNode(uoname,
                        UriFactory.Create(XmlSpecsHelper.XmlSchemaDataTypeString))));
                _markupGraph.Assert(new Triple(qtyTriple.Subject, qtyTriple.Predicate,
                    _markupGraph.CreateLiteralNode(quantity,
                        UriFactory.Create(XmlSpecsHelper.XmlSchemaDataTypeString))));
            }
        }
        public void AddContentObjectIndividual(string fragmentUri, string classUri, string uoName, string quantity)
        {
            //IUriNode test = new UriNode()
            IUriNode coType;
            IUriNode hasCoProperty;
            IUriNode uonameProperty;
            IUriNode qtyProperty;
            if(_markupGraph.GetUriNode(new Uri(classUri))==null)
            {
                coType =  (IUriNode) Tools.CopyNode(_baseGraph.GetUriNode(new Uri(classUri)), _markupGraph, true);
            }
            else
            {
                coType = _markupGraph.GetUriNode(new Uri(classUri));
            }
            if(_markupGraph.GetUriNode(":HasContentObject")==null)
            {
                 hasCoProperty = (IUriNode) Tools.CopyNode(_baseGraph.GetUriNode(":HasContentObject"), _markupGraph, true);
            }
            else
            {
                 hasCoProperty = _markupGraph.GetUriNode(":HasContentObject");
            }
            if(_markupGraph.GetUriNode(":UniversalObjectName")==null)
            {
                 uonameProperty =(IUriNode) Tools.CopyNode(_baseGraph.GetUriNode(":UniversalObjectName"), _markupGraph, true);
            }
            else
            {
                 uonameProperty = _markupGraph.GetUriNode(":UniversalObjectName");
            }
            if(_markupGraph.GetUriNode(":QuantityOfSameObjects")==null)
            {
                 qtyProperty=(IUriNode) Tools.CopyNode(_baseGraph.GetUriNode(":QuantityOfSameObjects"), _markupGraph, true);
            }
            else
            {
                 qtyProperty =  _markupGraph.GetUriNode(":QuantityOfSameObjects");
            }
            
            IUriNode rdfType = _markupGraph.GetUriNode("rdf:type");
            IUriNode fragmentInd = _markupGraph.GetUriNode(new Uri(fragmentUri));
            var id = GenerateUniqueId();

            IUriNode coInd = _markupGraph.CreateUriNode(":" + id);
            Triple coTriple = new Triple(coInd, rdfType, coType);
            _markupGraph.Assert(coTriple);

            Triple fragmentHasCoTriple = new Triple(fragmentInd, hasCoProperty, coInd);
            _markupGraph.Assert(fragmentHasCoTriple);
            
            ILiteralNode uonameLiteral =
                _markupGraph.CreateLiteralNode(uoName, UriFactory.Create(XmlSpecsHelper.XmlSchemaDataTypeString));
            _markupGraph.Assert(new Triple(coInd, uonameProperty, uonameLiteral));
            
            ILiteralNode qtyLiteral =
                _markupGraph.CreateLiteralNode(quantity, UriFactory.Create(XmlSpecsHelper.XmlSchemaDataTypeString));
            _markupGraph.Assert(new Triple(coInd, qtyProperty, qtyLiteral));
        }
        public void AddMediaFragment(string start, string end, string description)
        {
            var mediaRes = GetMediaResourceNode();//получение узла медиа-ресурса (видеоролика)
            //получение узлов необходимых классов и свойств
            IUriNode fragmentType = _markupGraph.CreateUriNode("ma-ont:MediaFragment"); 
            IUriNode beginsAtProperty = _markupGraph.CreateUriNode("timeline:beginsAt");
            IUriNode endsAtProperty = _markupGraph.CreateUriNode("timeline:EndsAt");
            IUriNode hasFragmentProperty = _markupGraph.CreateUriNode("ma-ont:hasFragment");
            IUriNode fragmentDescriptionProperty = _markupGraph.CreateUriNode("ma-ont:description");
            IUriNode rdfType = _markupGraph.GetUriNode("rdf:type");
            //----
            var id = GenerateUniqueId();//генерация уникального идентификатора, обеспечивающего уникальность URI объекта
            
            IUriNode fragmentInd = _markupGraph.CreateUriNode(":" + id);//создание индивида фрагмента видео
            //создание триплета "индивид Fragment_ind имеет тип ma:MediaFragment"
            Triple fragmentTriple = new Triple(fragmentInd, rdfType, fragmentType);
            _markupGraph.Assert(fragmentTriple);//запись созданного триплета

            Triple mediaResHasFragmentTriple = new Triple(mediaRes, hasFragmentProperty, fragmentInd);
            _markupGraph.Assert(mediaResHasFragmentTriple);
            //создание литерала типа xsd:string
            ILiteralNode beginsAtLiteral =
                _markupGraph.CreateLiteralNode(start, UriFactory.Create(XmlSpecsHelper.XmlSchemaDataTypeString));
            Triple fragmentBeginsAtTriple = new Triple(fragmentInd, beginsAtProperty, beginsAtLiteral);
            _markupGraph.Assert(fragmentBeginsAtTriple);
            ILiteralNode endsAtLiteral =
                _markupGraph.CreateLiteralNode(end, UriFactory.Create(XmlSpecsHelper.XmlSchemaDataTypeString));
            Triple fragmentEndsAtTriple = new Triple(fragmentInd, endsAtProperty, endsAtLiteral);
            _markupGraph.Assert(fragmentEndsAtTriple);

            ILiteralNode descLiteral =
                _markupGraph.CreateLiteralNode(description, UriFactory.Create(XmlSpecsHelper.XmlSchemaDataTypeString));
            Triple fragmentDescTriple = new Triple(fragmentInd, fragmentDescriptionProperty, descLiteral);
            _markupGraph.Assert(fragmentDescTriple);
        }
        private string GenerateUniqueId()
        {
            //id generation
            StringBuilder builder = new StringBuilder();
            Enumerable
                .Range(65, 26)
                .Select(e => ((char) e).ToString())
                .Concat(Enumerable.Range(97, 26).Select(e => ((char) e).ToString()))
                .Concat(Enumerable.Range(0, 10).Select(e => e.ToString()))
                .OrderBy(e => Guid.NewGuid())
                .Take(11)
                .ToList().ForEach(e => builder.Append(e));
            var id = builder.ToString();
            return id;
        }
        public IEnumerable<ContentObjectIndividualModel> GetCoIndividualsCollection(string fragmentUri)
        {
            var fragment = _markupGraph.GetUriNode(new Uri(fragmentUri));
            var coIndividualsCollection = new List<ContentObjectIndividualModel>();
            if (fragment != null)
            {
                IUriNode hasCoProperty = _markupGraph.GetUriNode(":HasContentObject");
                if (hasCoProperty != null)
                {
                    var uonameProperty = _markupGraph.GetUriNode(":UniversalObjectName");
                    var qtyProperty =  _markupGraph.GetUriNode(":QuantityOfSameObjects");
                    var rdfType = _markupGraph.GetUriNode("rdf:type");

                    var triples = _markupGraph.GetTriplesWithSubjectPredicate(fragment, hasCoProperty);
                    foreach (var triple in triples)
                    {
                        var obj = triple.Object;
                        var classObj = _markupGraph.GetTriplesWithSubjectPredicate(obj, rdfType).First().Object;
                        var uonameObj =_markupGraph.GetTriplesWithSubjectPredicate(obj, uonameProperty).First().Object;
                        var qtyObj=_markupGraph.GetTriplesWithSubjectPredicate(obj, qtyProperty).First().Object;

                        string uri = obj.AsValuedNode().AsString();
                        string classUri = classObj.AsValuedNode().AsString();
                        string uoname = uonameObj.AsValuedNode().AsString();
                        string qty = qtyObj.AsValuedNode().AsString();

                        var cOindContext = new ContentObjectIndividualModel(uri, classUri, uoname, qty, fragmentUri);
                        coIndividualsCollection.Add(cOindContext);
                    }
                }
            }

            return coIndividualsCollection;
        }
        public IEnumerable<MediaFragmentModel> GetFragmentsCollection()
        {
            var resource = GetMediaResourceNode();
            IUriNode hasFragmentProperty = _markupGraph.GetUriNode("ma-ont:hasFragment");

            var fragmentsCollection = new List<MediaFragmentModel>();

            if (hasFragmentProperty != null)
            {
                //IUriNode Fragment_type = _markupGraph.GetUriNode("ma-ont:MediaFragment");
                IUriNode beginsAtProperty = _markupGraph.GetUriNode("timeline:beginsAt");
                IUriNode endsAtProperty = _markupGraph.GetUriNode("timeline:EndsAt");
                IUriNode fragmentDescriptionProperty = _markupGraph.GetUriNode("ma-ont:description");

                //_markupGraph.Get
                var triples = _markupGraph.GetTriplesWithSubjectPredicate(resource, hasFragmentProperty);
                foreach (var t in triples)
                {
                    var obj = t.Object;
                    var beginsObj = _markupGraph.GetTriplesWithSubjectPredicate(obj, beginsAtProperty).First().Object;
                    var endsObj = _markupGraph.GetTriplesWithSubjectPredicate(obj, endsAtProperty).First().Object;
                    var descObj = _markupGraph.GetTriplesWithSubjectPredicate(obj, fragmentDescriptionProperty).First()
                        .Object;

                    var id = obj.AsValuedNode().AsString();
                    var begins = beginsObj.AsValuedNode().AsString();
                    var ends = endsObj.AsValuedNode().AsString();
                    var desc = descObj.AsValuedNode().AsString();

                    var fragmentContext = new MediaFragmentModel(id, begins, ends, desc, this);
                    fragmentsCollection.Add(fragmentContext);
                }
            }

            fragmentsCollection.Sort();
            return fragmentsCollection;
        }
    }
}