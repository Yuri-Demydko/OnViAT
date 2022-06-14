using System;
using System.Collections.Generic;
using System.Linq;
using OnViAT.Enums;
using OnViAT.ViewModels;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Query.Datasets;


namespace OnViAT.Models
{
    public class OntologySearchModel
    {
        private IGraph _mergedGraph;

        private List<SearchIndividualModel> _searchObjects;
        public SearchExportModel SearchResults;

        public OntologySearchModel(StorageModel storage)
        {
            var baseGraph = new Graph();
            RdfXmlParser parser = new RdfXmlParser();
            parser.Load(baseGraph, Paths.Paths.BASE_ONTOLOGY);
            
            MergeMarkupGraphs(storage,baseGraph);

        }

        public void FillSearchObjects(IEnumerable<SearchIndividualModel> searchObjects)
        {
            this._searchObjects = new List<SearchIndividualModel>(searchObjects);
            this.SearchResults = SearchByIndividuals();
            
        }

        private void MergeMarkupGraphs(StorageModel storage, IGraph baseGraph)
        {
            //Проход по всем онтологиям размеченных файлов в хранилище
            foreach (var ont in 
                storage.FilesToMarkupModels.Values.Select(markup => markup.Ontology))
            {
                //Экспорт графа и добавление его триплетов к _mergedGraph
                var g = ont.ExportGraph();
                if (_mergedGraph == null)
                    _mergedGraph = new Graph(g.Triples);
                else
                    _mergedGraph.Merge(g,true);
                _mergedGraph.NamespaceMap.Import(g.NamespaceMap);
            }
            //Присоединение базового графа
            _mergedGraph.Merge(baseGraph);
            _mergedGraph.NamespaceMap.Import(baseGraph.NamespaceMap);
        }

        public void SearchByDescription(bool files, bool fragments, string searchString)
        {
            //Если обе переменные равны false - это исключительная ситуация
            if (!files && !fragments)
                throw new Exception("At least one search mode must be true!");

            //Инициализация необходимых для выполнения запроса объектов и структуры для хранения результатов 
            SearchExportModel res=new SearchExportModel();
            SparqlParameterizedString queryString = new SparqlParameterizedString();
            queryString.Namespaces.AddNamespace("ont", new Uri(Constants.Constants.BASE_URI+"#"));
            queryString.Namespaces.AddNamespace("rdf",new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#"));
            queryString.Namespaces.AddNamespace("rdfs",new Uri("http://www.w3.org/2000/01/rdf-schema#"));
            queryString.Namespaces.AddNamespace("xsd",new Uri("http://www.w3.org/2001/XMLSchema#"));
            queryString.Namespaces.AddNamespace("ma-ont",new Uri("http://www.w3.org/ns/ma-ont#"));
            TripleStore store = new TripleStore();
            store.Add(_mergedGraph);
            ISparqlDataset ds = new InMemoryDataset(store,true);
            ISparqlQueryProcessor processor = new LeviathanQueryProcessor(ds);
            SparqlQueryParser parser = new SparqlQueryParser();
            //Построение строки запроса, выполнение запроса и передача результатов в SearchExportModel при поиске по описаниям файлов
            if (files)
            {
                string qs = "SELECT * WHERE {" +
                               "?file rdf:type ma-ont:MediaResource." +
                               $"?file ma-ont:description ?desc FILTER ( contains(lcase(str(?desc)),\"{searchString.ToLower()}\") ) ." +
                               "}";
                queryString.CommandText = qs;
                SparqlQuery query = parser.ParseFromString(queryString);
                var results = processor.ProcessQuery(query);
                if (results is SparqlResultSet)
                {
                    SparqlResultSet rset = (SparqlResultSet)results;
                    res.FillByFilesDescriptionSearch(rset);
                }
            }
            //Построение строки запроса, выполнение запроса и передача результатов в SearchExportModel при поиске по описаниям фрагментов видео
            if (fragments)
            {
                string qs = "SELECT * WHERE {" +
                            "?frag rdf:type ma-ont:MediaFragment." +
                            $"?frag ma-ont:description ?desc FILTER ( contains(lcase(str(?desc)),\"{searchString.ToLower()}\") ) ." +
                            "?file ma-ont:hasFragment ?frag ." +
                            "}";
                queryString.CommandText = qs;
                SparqlQuery query = parser.ParseFromString(queryString);
                var results = processor.ProcessQuery(query);
                if (results is SparqlResultSet)
                {
                    SparqlResultSet rset = (SparqlResultSet)results;
                    res.FillByFragmentsDescriptionSearch(rset);
                    
                }
            }
            this.SearchResults = res;
        }
        
        private string BuildSPARQLForIndividualSearch(SearchIndividualModel obj)
        { 
            //Учет режима сравнения класса индивида (заданный тип/иерархия)
          string objType =
              obj.HierarchySearch==HierarchySearchMode.TypeOnly ? 
                  "rdf:type" : "rdf:type/rdfs:subClassOf*";
          //Шаблон триплета для выбора uri файла
          string objFile = "?file ma-ont:hasFragment ?frag .";
          //Шаблон триплета для выбора uri фрагмента
          string objFragment = "?frag ont:HasContentObject ?obj .";
          //Шаблон триплета для выбора индивида по классу с выбранным режимом сравнения
          string objClass = $"?obj {objType} ont:{obj.ClassUri.Split("#").Last()} .";
          
          //Формирование шаблона триплета для выбора индивида по имени в зависимости от режима сравнения
          string objName = "";
          switch (obj.NameComparison)
          {
              case NameComparisonMode.Equals:
              {
                  objName = $"?obj ont:UniversalObjectName ?name FILTER ( lcase(str(?name)) = \"{obj.UoName.ToLower()}\" ) .";
                  break;
              }
              case NameComparisonMode.DontCount:
              {
                  break;
              }
              case NameComparisonMode.Contains:
              {
                  objName = $"?obj ont:UniversalObjectName ?name FILTER ( contains(lcase(str(?name)),\"{obj.UoName.ToLower()}\") ) .";
                  break;
              }
          }
          
          //Формирование шаблона триплета для выбора индивида по количеству в зависимости от режима сравнения
          string objQuantity = "";
          switch (obj.QuantityComparison)
          {
              case QuantityComparisonMode.Equal:
              {
                  objQuantity = $"?obj ont:QuantityOfSameObjects ?q FILTER(xsd:integer(?q) = {obj.Quantity}) .";
                  break;
              }
              case QuantityComparisonMode.Greater:
              {
                  objQuantity = $"?obj ont:QuantityOfSameObjects ?q FILTER(xsd:integer(?q) > {obj.Quantity}) .";
                  break;
              }
              case QuantityComparisonMode.Less:
              {
                  objQuantity = $"?obj ont:QuantityOfSameObjects ?q FILTER(xsd:integer(?q) < {obj.Quantity}) .";
                  break;
              }
              case QuantityComparisonMode.LessOrEqual:
              {
                  objQuantity = $"?obj ont:QuantityOfSameObjects ?q FILTER(xsd:integer(?q) < {obj.Quantity} || xsd:integer(?q) = {obj.Quantity}) .";
                  break;
              }
              case QuantityComparisonMode.GreaterOrEqual:
              {
                  objQuantity = $"?obj ont:QuantityOfSameObjects ?q FILTER(xsd:integer(?q) > {obj.Quantity} || xsd:integer(?q) = {obj.Quantity}) .";
                  break;
              }
              default:
                  break;
          }
          
          //Формирование строки запроса из частей
          string query = "SELECT * WHERE { " +objClass+objName+objQuantity+objFragment+objFile+" }";
            
          return query;
        }
        
        private SearchExportModel SearchByIndividuals()
        {
            //Инициализация поля-структуры результатов поиска
            SearchExportModel res=new SearchExportModel();
            //Создание модели строки запроса с параметрами и добавление пространств имен используемых онтологий
            SparqlParameterizedString queryString = new SparqlParameterizedString();
            queryString.Namespaces.AddNamespace("ont", new Uri(Constants.Constants.BASE_URI+"#"));
            queryString.Namespaces.AddNamespace("rdf",new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#"));
            queryString.Namespaces.AddNamespace("rdfs",new Uri("http://www.w3.org/2000/01/rdf-schema#"));
            queryString.Namespaces.AddNamespace("xsd",new Uri("http://www.w3.org/2001/XMLSchema#"));
            queryString.Namespaces.AddNamespace("ma-ont",new Uri("http://www.w3.org/ns/ma-ont#"));
            
            //Конвертация объединенного графа датасет для запроса
            TripleStore store = new TripleStore();
            store.Add(_mergedGraph);
            ISparqlDataset ds = new InMemoryDataset(store,true);
            
            //Инициализация процессора запросов на основе датасета
            ISparqlQueryProcessor processor = new LeviathanQueryProcessor(ds);
            //Инициализация парсера строки запроса
            SparqlQueryParser parser = new SparqlQueryParser();
            //Проход по объектам списка _searchObjects
            foreach (var sobj in _searchObjects)
            {
                //Построение текста запроса для данного объекта
                var qs = BuildSPARQLForIndividualSearch(sobj);
                queryString.CommandText = qs;
                //Создание объекта-модели запроса, выполнение запроса
                SparqlQuery query = parser.ParseFromString(queryString);
                var results = processor.ProcessQuery(query);
                //Парсинг результатов запроса
                if (results is SparqlResultSet)
                {
                    SparqlResultSet rset = (SparqlResultSet)results;
                    //Загрузка результатов в структуру SearchExportModel
                    res.FillByIndividualsSearch(rset);
                }
            }
            return res;
        }
    }

    public class SearchExportModel
    {
        private Dictionary<string, List<string>> _FileToFragments;
        private Dictionary<string, List<string>> _FragmentToIndividuals;
        //Парсинг результатов при поиске по описанию видеофайлов
        public void FillByFilesDescriptionSearch(SparqlResultSet sparql_results)
        {
            _FileToFragments ??= new Dictionary<string, List<string>>();
            _FragmentToIndividuals ??= new Dictionary<string, List<string>>();
            var files = sparql_results.Results.Select(x => x["file"].ToString()).ToList();
            foreach (var file in files)
            {
                //null вместо списка фрагментов обрабатывается в методе фильтрации на главной форме как сигнал к выводу всех фрагментов данного файла
                _FileToFragments.Add(file,null);
            }
        }
        //Парсинг результатов при поиске по описанию фрагментов файлов
        public void FillByFragmentsDescriptionSearch(SparqlResultSet sparql_results)
        {
            _FileToFragments ??= new Dictionary<string, List<string>>();
            _FragmentToIndividuals ??= new Dictionary<string, List<string>>();
                
            var files = sparql_results.Results.Select(x => x["file"].ToString()).ToList();
            foreach (var fileUri in files)
            {
                var fragsForFile = sparql_results.Results
                    .Where(x => x["file"].ToString() == fileUri)
                    .Select(x => x["frag"].ToString())
                    .ToList();
                if(!_FileToFragments.ContainsKey(fileUri))
                    _FileToFragments.Add(fileUri,fragsForFile);
                else
                {
                    _FileToFragments[fileUri] ??= new List<string>();
                    _FileToFragments[fileUri].AddRange(fragsForFile);
                }
            }
        }
            
        public void FillByIndividualsSearch(SparqlResultSet sparql_results)
        {
            //Инициализация словарей в случае, если они не инициализированы
            _FileToFragments ??= new Dictionary<string, List<string>>();
            _FragmentToIndividuals ??= new Dictionary<string, List<string>>();
                
            //Получение списка URI файлов
            var files = sparql_results.Results.Select(x => x["file"].ToString()).ToList();
                
            //Перебор URI файлов в списке
            foreach (var fileUri in files)
            {
                //Выборка URI фрагментов для данного файла и размещение их в соответствующем словаре
                var fragsForFile = sparql_results.Results
                    .Where(x => x["file"].ToString() == fileUri)
                    .Select(x => x["frag"].ToString())
                    .ToList();
                if(!_FileToFragments.ContainsKey(fileUri))
                    _FileToFragments.Add(fileUri,fragsForFile);
                else
                    _FileToFragments[fileUri].AddRange(fragsForFile);
                foreach (var fragUri in fragsForFile)
                {
                    //Выборка URI индивидов для данного фрагмента и размещение их в соответствующем словаре
                    var indsForFrag = sparql_results.Results
                        .Where(x => x["file"].ToString() == fileUri
                                    && x["frag"].ToString() == fragUri)
                        .Select(x => x["obj"].ToString())
                        .ToList();
                    if(!_FragmentToIndividuals.ContainsKey(fragUri)) 
                        _FragmentToIndividuals.Add(fragUri,indsForFrag);
                    else
                        _FragmentToIndividuals[fragUri].AddRange(indsForFrag);
                }
            }
        }
            

        public List<string> GetFiles() => _FileToFragments.Keys.ToList();
            
        public List<string> GetFragmentsByFile(string fileUri)
        {
            return _FileToFragments[fileUri] == null ? null : new List<string>(_FileToFragments[fileUri]);
        }

        public List<string> GetIndividualsByFragment(string fragUri)
        {
            if (!_FragmentToIndividuals.ContainsKey(fragUri))
                return null;
            return _FragmentToIndividuals[fragUri] == null ? null : new List<string>(_FragmentToIndividuals[fragUri]);
        }
            
        public List<string> getIndividualsByFile(string fileUri)
        {
            List<string> res = new List<string>();
            foreach (var fragUri in GetFragmentsByFile(fileUri))
            {
                res.AddRange(GetIndividualsByFragment(fragUri));
            }

            return res;
        }
    }
}