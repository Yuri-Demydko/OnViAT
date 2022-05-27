using System.Collections.Generic;

namespace OnViAT.Technical
{
    public class Options
    {
        public static Dictionary<string, string> HeaderResolverDictionary
            = new Dictionary<string, string>()
            {
                {"ContentObject","Объект видеоконтента"},
                {"AbstractObject", "Абстрактный объект"},
                {"ConcreteObject","Конкретный объект"}
            };

        public static string TryToResolveHeader(string header)
        {
            if (HeaderResolverDictionary.ContainsKey(header))
                return HeaderResolverDictionary[header];
            return header;
        }

    }
}