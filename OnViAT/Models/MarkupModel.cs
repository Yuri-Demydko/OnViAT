using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using OnViAT.Enums;
using File = System.IO.File;

namespace OnViAT.Models
{
    public class MarkupModel
    {
        private FileInfo _currentFile;
        
        private string _fileMarkup;
        public string DecodedFileMarkup => _fileMarkup;
        
        private OntologyModel _ontology;
        public OntologyModel Ontology => _ontology;
        public Dictionary<MetaDataTypes, string> FileMainMetadata => _ontology.GetMediaResourceMetadata();
        public MarkupModel(string path)
        {
            if (File.Exists(path))
            {
                _currentFile = new FileInfo(path);
                var tfile = TagLib.File.Create(path);
                var description = DecodeFrom64(tfile.Tag.Description);
              
                if (string.IsNullOrWhiteSpace(description) || !description.Contains(Constants.Constants.MARKUP_MARKER))
                {
                    _ontology = new OntologyModel(Constants.Constants.MARKUP_BASE, Constants.Constants.BASE_ONTOLOGY,Constants.Constants.ADDITION_GRAPH_CACHE);
                   _fileMarkup= _ontology.InitMediaResourceMetadata
                   (_currentFile.Name,
                       "Нет описания",
                        _currentFile.CreationTime,
                       DateTime.Now, tfile.Properties.Duration.TotalSeconds.ToString(CultureInfo.InvariantCulture))
                                +Constants.Constants.MARKUP_MARKER;
                   tfile.Tag.Description = EncodeTo64(_fileMarkup);
                   tfile.Save();
                }
                else
                {
                    _fileMarkup = DecodeFrom64(tfile.Tag.Description);
                    _ontology = new OntologyModel(_fileMarkup);
                    _ontology.InitBaseGraph(Constants.Constants.BASE_ONTOLOGY);
                    _ontology.SetAdditionsGraph(Constants.Constants.ADDITION_GRAPH_CACHE);
                }
            }
            else throw new ArgumentException("No File");
        }
        public void ClearMarkup()
        {
            var tfile = TagLib.File.Create(_currentFile.FullName);
            _ontology = new OntologyModel(Constants.Constants.MARKUP_BASE, Constants.Constants.BASE_ONTOLOGY);
            _fileMarkup= _ontology.InitMediaResourceMetadata(_currentFile.Name, "Нет описания",
                _currentFile.CreationTime, DateTime.Now, tfile.Properties.Duration.TotalSeconds.ToString(CultureInfo.InvariantCulture))+Constants.Constants.MARKUP_MARKER;
            _ontology.SetAdditionsGraph(Constants.Constants.ADDITION_GRAPH_CACHE);
            //
            tfile.Tag.Description = EncodeTo64(_fileMarkup);
            tfile.Save();
        }
        public void ReWriteMarkup()
        {
            if (_currentFile.Exists)
            {
                _ontology.EditMediaResourceMetadata(DateTime.Now.ToString("yyyy-MM-dd"),
                    MetaDataTypes.EditDate);
                 var tfile = TagLib.File.Create(_currentFile.FullName);//Создание объекта TagLib.File
                _fileMarkup = _ontology.ExportGraphAsString() + Constants.Constants.MARKUP_MARKER;//Получение разметки в виде строки
                tfile.Tag.Description = EncodeTo64(_fileMarkup);//Обновление тега Description
                tfile.Save();//Сохранение изменений
            }
            else
            {
                throw new Exception("File not found!");
            }
        }
        static public string EncodeTo64(string toEncode)
        {
            byte[] toEncodeAsBytes = System.Text.UTF8Encoding.UTF8.GetBytes(toEncode);
            string returnValue = System.Convert.ToBase64String(toEncodeAsBytes);
            return returnValue;
        }
        static public string DecodeFrom64(string encodedData)
        {
            try
            {
                byte[] encodedDataAsBytes = System.Convert.FromBase64String(encodedData);
                string returnValue = System.Text.UTF8Encoding.UTF8.GetString(encodedDataAsBytes);
                return returnValue;
            }
            catch (Exception e)
            {
                return null;
            }
        }
    }
}