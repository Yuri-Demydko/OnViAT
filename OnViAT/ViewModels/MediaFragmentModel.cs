using System;
using JetBrains.Annotations;
using OnViAT.Models;

namespace OnViAT.ViewModels
{
    public class MediaFragmentModel:IComparable
    {
        private string _uri;
        private string _beginsAt;
        private string _endsAt;
        private string _description;
        private OntologyModel _parentOntology;
        public string Uri => _uri; //URI
        public string BeginsAt => _beginsAt;
        public string EndsAt => _endsAt;
        public string Description => _description;
        public MediaFragmentModel([NotNull] string uri, [NotNull] string beginsAt, [NotNull] string endsAt,
            [NotNull] string description, OntologyModel parentOntology)
        {
            _uri = uri ?? throw new ArgumentNullException(nameof(uri));
            _beginsAt = beginsAt ?? throw new ArgumentNullException(nameof(beginsAt));
            _endsAt = endsAt ?? throw new ArgumentNullException(nameof(endsAt));
            _description = description ?? throw new ArgumentNullException(nameof(description));
            _parentOntology = parentOntology;
        }
        public override string ToString()
        {
            string res = BeginsAt + "-" + EndsAt + "\n";
            if (Description.Length >= 15)
                res += Description.Substring(0, 15)+"...";
            else
                res += Description;
            return res;
        }
        public int CompareTo(object obj)
        {
            if (obj == null) return 1;
            MediaFragmentModel other_fragment = obj as MediaFragmentModel;
            if (other_fragment != null)
            {
                var milisecs1 = TimeSpan.Parse(this.BeginsAt).TotalMilliseconds;
                var milisecs2 = TimeSpan.Parse(other_fragment.BeginsAt).TotalMilliseconds;
                return milisecs1.CompareTo(milisecs2);
            }
            throw new ArgumentException("This isn't MediaFragmentModel object!");
        }
        public string GetAllInfo()
        {
            return "Начало: " + BeginsAt + "\n" + "Конец: " + EndsAt + "\n" + "Описание: " + Description + "\n" +
                   "URi: " + Uri;
        }
    }
}