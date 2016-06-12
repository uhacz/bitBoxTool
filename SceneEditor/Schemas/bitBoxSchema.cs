// -------------------------------------------------------------------------------------------------------------------
// Generated code, do not edit
// Command Line:  DomGen "bitBox.xsd" "bitBoxSchema.cs" "bitBox" "SceneEditor" "-a"
// -------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using Sce.Atf.Dom;

namespace SceneEditor
{
    public static class bitBoxSchema
    {
        public const string NS = "bitBox";

        public static void Initialize(XmlSchemaTypeCollection typeCollection)
        {
            Initialize((ns,name)=>typeCollection.GetNodeType(ns,name),
                (ns,name)=>typeCollection.GetRootElement(ns,name));
        }

        public static void Initialize(IDictionary<string, XmlSchemaTypeCollection> typeCollections)
        {
            Initialize((ns,name)=>typeCollections[ns].GetNodeType(name),
                (ns,name)=>typeCollections[ns].GetRootElement(name));
        }

        private static void Initialize(Func<string, string, DomNodeType> getNodeType, Func<string, string, ChildInfo> getRootElement)
        {
            graphType.Type = getNodeType("bitBox", "graphType");
            graphType.nameAttribute = graphType.Type.GetAttributeInfo("name");
            graphType.nodeChild = graphType.Type.GetChildInfo("node");

            nodeType.Type = getNodeType("bitBox", "nodeType");
            nodeType.nameAttribute = nodeType.Type.GetAttributeInfo("name");
            nodeType.nodeChild = nodeType.Type.GetChildInfo("node");

            LocatorNode.Type = getNodeType("bitBox", "LocatorNode");
            LocatorNode.nameAttribute = LocatorNode.Type.GetAttributeInfo("name");
            LocatorNode.posAttribute = LocatorNode.Type.GetAttributeInfo("pos");
            LocatorNode.rotAttribute = LocatorNode.Type.GetAttributeInfo("rot");
            LocatorNode.scaleAttribute = LocatorNode.Type.GetAttributeInfo("scale");
            LocatorNode.nodeChild = LocatorNode.Type.GetChildInfo("node");

            MeshNode.Type = getNodeType("bitBox", "MeshNode");
            MeshNode.nameAttribute = MeshNode.Type.GetAttributeInfo("name");
            MeshNode.posAttribute = MeshNode.Type.GetAttributeInfo("pos");
            MeshNode.rotAttribute = MeshNode.Type.GetAttributeInfo("rot");
            MeshNode.scaleAttribute = MeshNode.Type.GetAttributeInfo("scale");
            MeshNode.meshAttribute = MeshNode.Type.GetAttributeInfo("mesh");
            MeshNode.materialAttribute = MeshNode.Type.GetAttributeInfo("material");
            MeshNode.nodeChild = MeshNode.Type.GetChildInfo("node");

            sceneRootElement = getRootElement(NS, "scene");
        }

        public static class graphType
        {
            public static DomNodeType Type;
            public static AttributeInfo nameAttribute;
            public static ChildInfo nodeChild;
        }

        public static class nodeType
        {
            public static DomNodeType Type;
            public static AttributeInfo nameAttribute;
            public static ChildInfo nodeChild;
        }

        public static class LocatorNode
        {
            public static DomNodeType Type;
            public static AttributeInfo nameAttribute;
            public static AttributeInfo posAttribute;
            public static AttributeInfo rotAttribute;
            public static AttributeInfo scaleAttribute;
            public static ChildInfo nodeChild;
        }

        public static class MeshNode
        {
            public static DomNodeType Type;
            public static AttributeInfo nameAttribute;
            public static AttributeInfo posAttribute;
            public static AttributeInfo rotAttribute;
            public static AttributeInfo scaleAttribute;
            public static AttributeInfo meshAttribute;
            public static AttributeInfo materialAttribute;
            public static ChildInfo nodeChild;
        }

        public static ChildInfo sceneRootElement;
    }

    public partial class graphType : DomNodeAdapter
    {
        public string name
        {
            get { return GetAttribute<string>(bitBoxSchema.graphType.nameAttribute); }
            set { SetAttribute(bitBoxSchema.graphType.nameAttribute, value); }
        }
        public IList<nodeType> node
        {
            get { return GetChildList<nodeType>(bitBoxSchema.graphType.nodeChild); }
        }
    }

    public partial class nodeType : DomNodeAdapter
    {
        public string name
        {
            get { return GetAttribute<string>(bitBoxSchema.nodeType.nameAttribute); }
            set { SetAttribute(bitBoxSchema.nodeType.nameAttribute, value); }
        }
        public IList<nodeType> node
        {
            get { return GetChildList<nodeType>(bitBoxSchema.nodeType.nodeChild); }
        }
    }

    public partial class LocatorNode : nodeType
    {
        public float[] pos
        {
            get { return GetAttribute<float[]>(bitBoxSchema.LocatorNode.posAttribute); }
            set { SetAttribute(bitBoxSchema.LocatorNode.posAttribute, value); }
        }
        public float[] rot
        {
            get { return GetAttribute<float[]>(bitBoxSchema.LocatorNode.rotAttribute); }
            set { SetAttribute(bitBoxSchema.LocatorNode.rotAttribute, value); }
        }
        public float[] scale
        {
            get { return GetAttribute<float[]>(bitBoxSchema.LocatorNode.scaleAttribute); }
            set { SetAttribute(bitBoxSchema.LocatorNode.scaleAttribute, value); }
        }
    }

    public partial class MeshNode : LocatorNode
    {
        public string mesh
        {
            get { return GetAttribute<string>(bitBoxSchema.MeshNode.meshAttribute); }
            set { SetAttribute(bitBoxSchema.MeshNode.meshAttribute, value); }
        }
        public string material
        {
            get { return GetAttribute<string>(bitBoxSchema.MeshNode.materialAttribute); }
            set { SetAttribute(bitBoxSchema.MeshNode.materialAttribute, value); }
        }
    }

}
