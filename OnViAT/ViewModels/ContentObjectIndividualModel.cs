using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace OnViAT.ViewModels
{
    public class ContentObjectIndividualModel
    {
        public string Uri => _uri;
        public string UoName => _uoName;
        public string Quantity => _quantity;
        public Dictionary<string, string> AdditionalDatatypeProperties => _additionalDatatypeProperties;
        public string ClassUri => _classUri;
        private string _uri;
        private protected string _classUri;
        private protected string _uoName;
        private protected string _quantity="0";
        private string _parentFragmentUri;
        private Dictionary<string, string> _additionalDatatypeProperties;
        public ContentObjectIndividualModel([NotNull] string uri, [NotNull] string classUri, [NotNull] string uoName,
            [NotNull] string quantity, [NotNull] string parentFragmentId)
        {
            _uri = uri ?? throw new ArgumentNullException(nameof(uri));
            _classUri = classUri ?? throw new ArgumentNullException(nameof(classUri));
            _uoName = uoName ?? throw new ArgumentNullException(nameof(uoName));
            _quantity = quantity ?? throw new ArgumentNullException(nameof(quantity));
            _parentFragmentUri = parentFragmentId ?? throw new ArgumentNullException(nameof(parentFragmentId));
        }
        public string GetAllInfo()
        {
            return "Название: "+_uoName + "\n" + "Тип: " + Technical.Options.TryToResolveHeader(_classUri.Split("#").Last())+"\n"+"Количество: "+_quantity+"\n"+"URI: "+_uri;
        }
        public override string ToString()
        {
            if (UoName.Length > 15)
                return UoName.Substring(0, 15) + "...";
            return UoName;
        }
    }
}