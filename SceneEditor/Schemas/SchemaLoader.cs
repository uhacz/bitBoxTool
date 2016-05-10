//Copyright © 2014 Sony Computer Entertainment America LLC. See License.txt.

using System.Linq;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Reflection;
using System.Xml.Schema;
//using System.Collections.Generic;

using Sce.Atf;
using Sce.Atf.Dom;
using Sce.Atf.Adaptation;
using Sce.Atf.Controls.PropertyEditing;

//using PropertyDescriptor = Sce.Atf.Dom.PropertyDescriptor;

namespace SceneEditor
{
    internal class PropertyEditorFactory
    {
        //public PropertyEditorFactory()
        //{
        //    m_creatorDict.Add( "float3_t", typeName => new NumericTupleEditor() );

        //}

        public delegate IPropertyEditor editorCreator(string typeName);
        
        public static IPropertyEditor createEditorForAttribute( AttributeInfo info )
        {
            switch (info.Type.Type )
            {
                case AttributeTypes.Boolean:
                    {
                        return new BoolEditor();
                    }
                case AttributeTypes.Int32:
                    {
                        return new BoundedIntEditor();
                    }
                case AttributeTypes.Single:
                    {
                        return new BoundedFloatEditor();
                    }
                case AttributeTypes.SingleArray:
                    {
                        return new NumericTupleEditor();
                    }
                default:
                    {
                        return null;
                    }

            }
        }

        //private Dictionary<string, editorCreator> m_creatorDict;
    }
    /// <summary>
    /// Loads the game schema and defines data extensions on the DOM types</summary>
    [Export(typeof(SchemaLoader))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class SchemaLoader : XmlSchemaTypeLoader
    {
        /// <summary>
        /// Constructor</summary>
        [ImportingConstructor]
        public SchemaLoader()
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

                bitBoxSchema.graphType.Type.Define(new ExtensionInfo<SceneEditingContext>());
                bitBoxSchema.graphType.Type.Define(new ExtensionInfo<NodeEditingContext>());
                //bitBoxSchema.graphType.Type.Define(new ExtensionInfo<UniqueIdValidator>());

                // register extensions
                //bitBoxSchema.graphType.Type.Define(new ExtensionInfo<Game>());
                //bitBoxSchema.graphType.Type.Define(new ExtensionInfo<ReferenceValidator>());
                //bitBoxSchema.graphType.Type.Define(new ExtensionInfo<UniqueIdValidator>());
                //
                //bitBoxSchema.nodeType.Type.Define(new ExtensionInfo<GameObject>());
                //bitBoxSchema.MeshNode.Type.Define(new ExtensionInfo<Dwarf>());

                //bitBoxSchema.MeshNode.Type.Define(new ExtensionInfo<MeshNode>());


                var creator = new AdapterCreator<CustomTypeDescriptorNodeAdapter>();

                foreach (DomNodeType type in GetNodeTypes(bitBoxSchema.nodeType.Type))
                {
                    type.AddAdapterCreator(creator);

                    string defaultNodeName = type.Name.Split(':').Last().Replace("Node", "");
                    type.SetTag( new NodeTypePaletteItem( type, defaultNodeName, defaultNodeName.Localize(), null));

                    PropertyDescriptorCollection propDescs = new PropertyDescriptorCollection( null );
                    foreach (AttributeInfo attr in type.Attributes)
                    {
                        IPropertyEditor editor = PropertyEditorFactory.createEditorForAttribute(attr);
                        AttributePropertyDescriptor attributePropDesc = new AttributePropertyDescriptor( attr.Name.Localize(), attr, "Attributes".Localize(), null, false, editor );

                        propDescs.Add(attributePropDesc);
                    }

                    type.SetTag(propDescs);

                }
                break;
            }
        }
    }
}
