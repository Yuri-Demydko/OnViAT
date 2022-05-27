using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OnViAT.ViewModels;

namespace OnViAT.Models
{
    public class StorageModel
    {
        private DirectoryInfo _directoryInfo;
        private FileInfo[] _files;
        private FileInfo _selectedFile;
        public FileInfo SelectedFile => _selectedFile;
        
        private Dictionary<FileInfo, MarkupModel> _filesToMarkupModels;
        public Dictionary<FileInfo, MarkupModel> FilesToMarkupModels => _filesToMarkupModels;

        public string DirectoryPath => _directoryInfo.FullName;

        public void DeselectAll()
        {
            _selectedFile = null;
        }
        public string SelectedFileName
        {
            get
            {
                if (_selectedFile != null)
                    return _selectedFile.FullName;
                return null;
            }
        }
        public StorageModel(string directory)
        {
            if (Directory.Exists(directory))
            {
                this._directoryInfo = new DirectoryInfo(directory);
                _files = _directoryInfo.GetFiles("*.*", SearchOption.TopDirectoryOnly)
                    .Where(s=>s.Name.EndsWith(".mp4") || s.Name.EndsWith(".avi")||s.Name.EndsWith(".mpg"))
                    .ToArray();
                FillMarkupModels();
                CheckAndFixNonUniqueUris();
            }
            else throw new ArgumentException("Wrong directory!");
        }

        private void FillMarkupModels()
        {
            _filesToMarkupModels = new Dictionary<FileInfo, MarkupModel>();
            foreach (var file in _files)
            {
                if (!_filesToMarkupModels.ContainsKey(file))
                {
                    _filesToMarkupModels[file] = new MarkupModel(file.FullName);
                }
            }
        }

        public void RegenerateAllUris()
        {
            foreach (var markup in _filesToMarkupModels.Values)
            {
                markup.Ontology.RegenerateMediaResourceUri();
                markup.ReWriteMarkup();
            }
        }
        private void CheckAndFixNonUniqueUris()
        {
            foreach (var markup in _filesToMarkupModels.Values)
            {
                bool nonUnique = _filesToMarkupModels
                    .Any(x =>
                        x.Value.Ontology.GetMediaResourceUri() == markup.Ontology.GetMediaResourceUri()
                            && x.Value!=markup);
                if (nonUnique)
                {
                    markup.Ontology.RegenerateMediaResourceUri();
                    markup.ReWriteMarkup();
                }
            }
            
        }
        public void SelectFile(FileInfo fi)
        {
            foreach (var fileInfo in _files)
            {
                if (fileInfo == fi)
                {
                    _selectedFile = fileInfo;
                    if (!_filesToMarkupModels.ContainsKey(_selectedFile))
                    {
                        _filesToMarkupModels[_selectedFile] = new MarkupModel(_selectedFile.FullName);
                    }
                    break;
                }
            }
            if(_selectedFile==null)
                throw new Exception("No such file!");
        }

        public string GetSelectedFileInfo()
        {
            string res = "Название: "+_selectedFile.Name+"\n" +
                         "Путь: "+_selectedFile.FullName+"\n" +
                         "Размер: "+Math.Round((double)_selectedFile.Length/(1024*1024), 2)+" Мб"+"\n" +
                         "Дата создания: "+_selectedFile.CreationTime.Date+"\n" +
                         "Дата последнего изменения: "+_selectedFile.LastWriteTime.Date;
            return res;
        }
        public List<FileViewModel> GetPseudoFileInfos()
        {
            var res = new List<FileViewModel>();
            foreach (var fileInfo in _files)
            {
                res.Add(new FileViewModel(fileInfo));
            }
            return res;
        }
        

        public List<FileViewModel> GetFileInfosBySE(SearchExportModel se)
        {
            var urisList = se.GetFiles();

            return (from finfo 
                in _filesToMarkupModels.Keys 
                let uri = _filesToMarkupModels[finfo].Ontology.GetMediaResourceUri() 
                where urisList.Contains(uri) 
                select new FileViewModel(finfo)).ToList();
        }
    }
}