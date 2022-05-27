using System;
using VDS.RDF;

namespace OnViAT.Helpers
{
    public class OntologyGraphValidatorHelper
    {
        public static bool IsValid(string graphString, string marker = null)
        {
            try
            {
                var g = new Graph();
                g.LoadFromString(graphString);

                return marker == null || graphString.Contains(marker);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}