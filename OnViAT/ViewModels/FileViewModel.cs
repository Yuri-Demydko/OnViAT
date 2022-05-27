using System.IO;

namespace OnViAT.ViewModels
{
    public class FileViewModel
    {
        public readonly FileInfo FileInfo;
        private readonly string _shortenedName;

        public FileViewModel(FileInfo fi)
        {
            FileInfo = fi;
            if (fi.Name.Length > 15)
                _shortenedName = fi.Name[..15] + "...";
            else _shortenedName = fi.Name;
        }

        public override string ToString()
        {
            return _shortenedName;
        }
    }
}