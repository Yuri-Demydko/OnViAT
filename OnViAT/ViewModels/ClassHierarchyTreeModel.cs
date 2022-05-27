using System.Collections.Generic;

namespace OnViAT.ViewModels
{

    public struct ClassHierarchyTreeModel
    {
        public readonly string Header;
        public readonly string URI;
        public List<ClassHierarchyTreeModel> SubNodes;

        public ClassHierarchyTreeModel(string header, string uri)
        {
            Header = header;
            URI = uri;
            SubNodes = new List<ClassHierarchyTreeModel>();
        }

        public override string ToString()
        {
            return Technical.Options.TryToResolveHeader(Header);
        }
    }
}