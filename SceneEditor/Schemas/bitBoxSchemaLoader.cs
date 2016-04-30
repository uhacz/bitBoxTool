//Copyright © 2014 Sony Computer Entertainment America LLC. See License.txt.

using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Reflection;
using System.Xml.Schema;

using Sce.Atf;
using Sce.Atf.Dom;

namespace SceneEditor
{
    /// <summary>
    /// Loads the game schema and defines data extensions on the DOM types</summary>
    [Export(typeof(BitBoxSchemaLoader))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class BitBoxSchemaLoader : XmlSchemaTypeLoader
    {
        /// <summary>
        /// Constructor</summary>
        [ImportingConstructor]
        public BitBoxSchemaLoader()
        {
            // set resolver to locate embedded .xsd file
            SchemaResolver = new ResourceStreamResolver(Assembly.GetExecutingAssembly(), "SceneEditor/Schemas");
            Load("bitBox.xsd");
        }

        /// <summary>
        /// Gets the game namespace</summary>
        public string NameSpace
        {
            get { return m_namespace; }
        }
        private string m_namespace;

        /// <summary>
        /// Gets the game type collection</summary>
        public XmlSchemaTypeCollection TypeCollection
        {
            get { return m_typeCollection; }
        }
        private XmlSchemaTypeCollection m_typeCollection;

        /// <summary>
        /// Method called after the schema set has been loaded and the DomNodeTypes have been created, but
        /// before the DomNodeTypes have been frozen. This means that DomNodeType.SetIdAttribute, for example, has
        /// not been called on the DomNodeTypes. Is called shortly before OnDomNodeTypesFrozen.
        /// Defines DOM adapters on the DOM types.</summary>
        /// <param name="schemaSet">XML schema sets being loaded</param>
        protected override void OnSchemaSetLoaded(XmlSchemaSet schemaSet)
        {
            foreach (XmlSchemaTypeCollection typeCollection in GetTypeCollections())
            {
                m_namespace = typeCollection.TargetNamespace;
                m_typeCollection = typeCollection;
                bitBoxSchema.Initialize(typeCollection);
                                
                // register extensions
                //bitBoxSchema.graphType.Type.Define(new ExtensionInfo<Game>());
                //bitBoxSchema.graphType.Type.Define(new ExtensionInfo<ReferenceValidator>());
                //bitBoxSchema.graphType.Type.Define(new ExtensionInfo<UniqueIdValidator>());
                //
                //bitBoxSchema.nodeType.Type.Define(new ExtensionInfo<GameObject>());
                //bitBoxSchema.MeshNode.Type.Define(new ExtensionInfo<Dwarf>());
                break;
            }
        }
    }
}
