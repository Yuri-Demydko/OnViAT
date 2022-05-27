using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using OnViAT.Enums;

namespace OnViAT.ViewModels
{
    public class SearchIndividualModel : ContentObjectIndividualModel
    {
        private static Dictionary<HierarchySearchMode, string> HierarchySearchModeNames =
            new Dictionary<HierarchySearchMode, string>()
            {
                {HierarchySearchMode.TypeOnly, "Точно"},
                {HierarchySearchMode.TypeAndSubclasses, "Иерархия"}
            };
        private static Dictionary<NameComparisonMode, string> NameComparisonModeNames =
            new Dictionary<NameComparisonMode, string>()
            {
                {NameComparisonMode.DontCount, "Не учитывать"},
                {NameComparisonMode.Equals, "Равно"},
                {NameComparisonMode.Contains, "Содержит"}
            };
        private static Dictionary<QuantityComparisonMode, string> QuantityComparisonModeNames =
            new Dictionary<QuantityComparisonMode, string>()
            {
                {QuantityComparisonMode.DontCount,""},
                {QuantityComparisonMode.Equal,"="},
                {QuantityComparisonMode.Greater,">"},
                {QuantityComparisonMode.Less,"<"},
                {QuantityComparisonMode.GreaterOrEqual,">="},
                {QuantityComparisonMode.LessOrEqual,"<="}
            };
        
        //Конструктор класса. Вызывает базовый, подставляя пустые строки вместо значений uri и uri родительского фрагмента
        public SearchIndividualModel([NotNull] string classUri, [NotNull] string uoName,
            [NotNull] string quantity) : base(string.Empty,classUri,uoName,quantity,string.Empty)
        {
        }
        //Поля хранят конкретные режимы сравнения характеристик для объекта
        public HierarchySearchMode HierarchySearch = HierarchySearchMode.TypeAndSubclasses;
        public QuantityComparisonMode QuantityComparison = QuantityComparisonMode.DontCount;
        public NameComparisonMode NameComparison = NameComparisonMode.Equals;
        public override string ToString()
        {
            var ShortenName = UoName;
            if (UoName.Length > 15)
                ShortenName= UoName.Substring(0, 15) + "...";
            string res = $"Тип: {_classUri.Split("#").Last()} ({HierarchySearchModeNames[this.HierarchySearch]})";
            res += (this.NameComparison != NameComparisonMode.DontCount)
                ? $"\nНазвание: ({NameComparisonModeNames[NameComparison]}) {ShortenName}"
                : "";
            
            res += (this.QuantityComparison != QuantityComparisonMode.DontCount)
                ? $"\nКоличество: {QuantityComparisonModeNames[this.QuantityComparison]} {_quantity}"
                : "";
            

            return res;
        }
    }
}