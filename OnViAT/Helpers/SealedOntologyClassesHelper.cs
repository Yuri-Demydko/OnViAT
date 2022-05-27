using System.Collections.Generic;

namespace OnViAT.Helpers
{
    public static class SealedOntologyClassesHelper
    {
        private static readonly List<string> SealedClasses = new List<string>()
        {
            $"{Constants.Constants.BASE_URI}#AbstractObject",
            $"{Constants.Constants.BASE_URI}#ConcreteObject",
            $"{Constants.Constants.BASE_URI}#ContentObject",
        };

        public static bool Sealed(string uri)
            =>!string.IsNullOrWhiteSpace(uri)
              && SealedClasses.Contains(uri)
              ||SealedClasses.Contains($"{Constants.Constants.BASE_URI}#{uri}");
    }
}