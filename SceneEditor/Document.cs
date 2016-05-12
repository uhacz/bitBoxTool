using Sce.Atf;
using Sce.Atf.Dom;

namespace SceneEditor
{
    public class Document : DomDocument
    {
        /// <summary>
        /// Gets the document client's file type name</summary>
        public override string Type
        {
            get { return "Scene".Localize(); }
        }
    }
}

