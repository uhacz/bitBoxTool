using System;
using System.Collections.Generic;
using System.Windows.Forms;

using Sce.Atf;
using Sce.Atf.Dom;
using Sce.Atf.Applications;
using Sce.Atf.Adaptation;
using Sce.Atf.Controls;

namespace SceneEditor
{
    public class SceneEditingContext :
        EditingContext, ITreeView, IItemView,
        IObservableContext,
        IInstancingContext,
        IHierarchicalInsertionContext,
        IOrderedInsertionContext,
        IEnumerableContext
    {
        public SceneEditingContext()
        {
            m_uniqueNamer = new UniqueNamer();
        }

        public void Initialize( ICommandService commandService )
        {
            m_treeControlEditor = new FilteredTreeControlEditor(commandService);
            m_treeControlEditor.TreeControl.ShowDragBetweenCue = true;
            m_treeControlEditor.TreeControl.AllowDrop = true;
                       
            //m_treeControlEditor.TreeControl.MouseDown += treeControl_MouseDown;
            //m_treeControlEditor.TreeControl.MouseUp += treeControl_MouseUp;
        }

        static public DomNode _CreateSceneRoot(string name)
        {
            DomNode node = new DomNode(bitBoxSchema.graphType.Type, bitBoxSchema.sceneRootElement);
            node.SetAttribute(bitBoxSchema.graphType.nameAttribute, name);
            return node;
        }

        public object Root
        {
            get { return m_root; }
            set
            {
                if (m_root != null)
                {
                    m_root.AttributeChanged -= root_AttributeChanged;
                    m_root.ChildInserted -= root_ChildInserted;
                    m_root.ChildRemoving -= root_ChildRemoving;
                    m_root.ChildRemoved -= root_ChildRemoved;
                    m_treeControlEditor.TreeView = null;
                    m_uniqueNamer.Clear();
                    //m_treeControlAdapter.TreeView = null;
                }

                m_root = value.As<DomNode>();

                if (m_root != null)
                {
                    m_root.AttributeChanged += root_AttributeChanged;
                    m_root.ChildInserted += root_ChildInserted;
                    m_root.ChildRemoving += root_ChildRemoving;
                    m_root.ChildRemoved += root_ChildRemoved;

                    m_treeControlEditor.TreeView = this; 
                    foreach (DomNode node in m_root.Subtree)
                    {
                        _CheckNodeName(node);
                    }
                }

                Reloaded.Raise(this, EventArgs.Empty);
            }
        }

        public ControlInfo ControlInfo
        {
            get { return m_controlInfo; }
            set { m_controlInfo = value; }
        }
        
        #region ITreeView Members

        //public object Root
        //{
        //    get { return m_root; }
        //}

        public IEnumerable<object> GetChildren(object parent)
        {
            DomNode node = parent as DomNode;
            if (node != null)
            {
                //if (m_showAdapters)
                //{
                //    // get all adapters, and wrap so that the TreeControlAdapter doesn't confuse
                //    //  them with their parent DomNode; remember that the DomNode and its adapters
                //    //  are logically Equal.
                //    IEnumerable<DomNodeAdapter> adapters = node.AsAll<DomNodeAdapter>();
                //    foreach (DomNodeAdapter adapter in adapters)
                //        yield return new Adapter(adapter);
                //}
                // get child Dom objects
                foreach (DomNode child in node.Children)
                    yield return child;
            }
        }

        #endregion
        #region IItemView Members

        public void GetInfo(object item, ItemInfo info)
        {
            info.IsLeaf = !HasChildren(item);

            DomNode node = item as DomNode;
            if (node != null)
            {
                AttributeInfo attrInfo = node.Type.GetAttributeInfo("name");
                if (attrInfo != null)
                {
                    info.Label = (string)node.GetAttribute(attrInfo);
                }
            }
        }

        #endregion
        #region IObservableContext Members

        /// <summary>
        /// Event that is raised when a tree item is inserted</summary>
        public event EventHandler<ItemInsertedEventArgs<object>> ItemInserted;

        /// <summary>
        /// Event that is raised when a tree item is removed</summary>
        public event EventHandler<ItemRemovedEventArgs<object>> ItemRemoved;

        /// <summary>
        /// Event that is raised when a tree item is changed</summary>
        public event EventHandler<ItemChangedEventArgs<object>> ItemChanged;

        /// <summary>
        /// Event that is raised when the tree is reloaded</summary>
        public event EventHandler Reloaded;

        #endregion
        #region IEnumerableContext Members

        /// <summary>
        /// Gets an enumeration of all of the items of this context</summary>
        IEnumerable<object> IEnumerableContext.Items
        {
            get { return GetChildren(m_root.As<object>()); }
        }

        #endregion

        private bool _IsDomNodeCheck(object obj)
        {
            if (obj == null)
                return false;

            IDataObject dataObject = (IDataObject)obj;
            object[] items = dataObject.GetData(typeof(object[])) as object[];
            if (items == null)
                return false;

            foreach (object item in items)
                if (!item.Is<DomNode>())
                    return false;

            return true;
        }

        private DomNode[] _GetDomNodes(object obj/*, bool copy*/ )
        {
            IDataObject dataObject = (IDataObject)obj;
            object[] items = dataObject.GetData(typeof(object[])) as object[];
            if (items == null)
            {
                return null;
            }

            List<DomNode> list = new List<DomNode>();
            foreach (object item in items)
            {
                DomNode u = item as DomNode;

                if (u.Parent == null)
                {
                    _CheckNodeName(u);
                }

                if (u != null)
                    list.Add(u);
            }

            return list.ToArray();
        }

        public void _CheckNodeName(DomNode node)
        {
            if (bitBoxSchema.nodeType.Type.IsAssignableFrom(node.Type))
            {
                string name = (string)node.GetAttribute(bitBoxSchema.nodeType.nameAttribute);
                string newName = m_uniqueNamer.Name(name);
                node.SetAttribute(bitBoxSchema.nodeType.nameAttribute, newName);
            }
        }

        #region IInstancingContext Members

        /// <summary>
        /// Returns whether the context can copy the selection</summary>
        /// <returns>True iff the context can copy</returns>
        bool IInstancingContext.CanCopy()
        {
            return Selection.Count > 0;
        }

        /// <summary>
        /// Copies the selection. Returns a data object representing the copied items.</summary>
        /// <returns>Data object representing the copied items; e.g., a
        /// System.Windows.Forms.IDataObject object</returns>
        object IInstancingContext.Copy()
        {
            IEnumerable<DomNode> resources = Selection.AsIEnumerable<DomNode>();
            List<object> copies = new List<object>(DomNode.Copy(resources));
            //List<object> copies = new List<object>();
            //foreach (DomNode node in resources)
            //{
            //    DomNode nodeCopy = DomNode.Copy(node);
            //    _CheckNodeName(nodeCopy);
            //    //if (bitBoxSchema.nodeType.Type.IsAssignableFrom(node.Type))
            //    //{
            //    //    DomNode parent = node.Parent;
            //    //    parent.GetChildList(bitBoxSchema.nodeType.nodeChild).Add(nodeCopy);
            //    //}

            //    copies.Add(nodeCopy);
            //}

            //foreach (object cpy in copies)
            //{
            //    DomNode node = cpy as DomNode;
            //    if( node != null )
            //    {
            //        _CheckNodeName(cpy as DomNode);
            //    }
            //}
            return new DataObject(copies.ToArray());
        }

        /// <summary>
        /// Returns whether the context can insert the data object</summary>
        /// <param name="insertingObject">Data to insert; e.g., System.Windows.Forms.IDataObject</param>
        /// <returns>True iff the context can insert the data object</returns>
        /// 
        public bool CanInsert(object insertingObject)
        {
            return _IsDomNodeCheck(insertingObject);
        }

        /// <summary>
        /// Inserts the data object into the context</summary>
        /// <param name="insertingObject">Data to insert; e.g., System.Windows.Forms.IDataObject</param>
        /// 
        public void Insert(object insertingObject)
        {
            DomNode[] itemCopies = _GetDomNodes(insertingObject/*, m_middleDown*/);

            foreach (DomNode dnode in itemCopies)
            {
                Root.As<DomNode>().GetChildList(bitBoxSchema.graphType.nodeChild).Add(dnode);
            }

            Selection.SetRange(itemCopies);
        }

        /// <summary>
        /// Returns whether the context can delete the selection</summary>
        /// <returns>True if the context can delete</returns>
        public bool CanDelete()
        {
            return Selection.Count > 0;
        }

        /// <summary>
        /// Deletes the selection</summary>
        public void Delete()
        {
            foreach (DomNode node in Selection.AsIEnumerable<DomNode>())
                node.RemoveFromParent();

            Selection.Clear();
        }

        #endregion
        #region IHierarchicalInsertionContext
        /// <summary>
        /// Returns true if context can insert the child object</summary>
        /// <param name="parent">The proposed parent of the object to insert</param>
        /// <param name="child">Child to insert</param>
        /// <returns>True iff the context can insert the child</returns>


        bool IHierarchicalInsertionContext.CanInsert(object parent, object child)
        {
            bool canInsertChild = _IsDomNodeCheck(child);
            return canInsertChild;
        }

        /// <summary>
        /// Inserts the child object into the context</summary>
        /// <param name="parent">The parent of the object to insert</param>
        /// <param name="child">Data to insert</param>
        void IHierarchicalInsertionContext.Insert(object parent, object child)
        {
            DomNode parentNode;
            if (parent != null)
            {
                parentNode = parent.As<DomNode>();
            }
            else
            {
                parentNode = m_root;
            }

            ChildInfo childInfo = null;
            if (parentNode.Type == bitBoxSchema.graphType.Type)
            {
                childInfo = bitBoxSchema.graphType.nodeChild;
            }
            else if (bitBoxSchema.nodeType.Type.IsAssignableFrom(parentNode.Type))
            {
                childInfo = parentNode.Type.GetChildInfo("node");
            }

            if( childInfo != null )
            {
                DomNode[] itemCopies = _GetDomNodes(child);
                
                foreach (DomNode dnode in itemCopies)
                {
                    parentNode.GetChildList(childInfo).Add(dnode);
                }
            }
        }
        #endregion
        #region IOrderedInsertionContext
        /// <summary>
        /// Returns true if 'item' can be inserted.</summary>
        /// <param name="parent">The object that will become the parent of the inserted object.
        /// Can be null if the list of objects is a flat list or if the root should be replaced.</param>
        /// <param name="before">The object that is immediately before the inserted object.
        /// Can be null to indicate that the inserted item should become the first child.</param>
        /// <param name="item">The item to be inserted. Consider using Util.ConvertData(item, false)
        /// to retrieve the final one or more items to be inserted.</param>
        /// <returns>True iff 'item' can be successfully inserted</returns>
        bool IOrderedInsertionContext.CanInsert(object parent, object before, object item)
        {
            DomNode parentDom = parent as DomNode;
            DomNode beforeDom = before as DomNode;
            if (parentDom == null )
                return false;

            //if (!bitBoxSchema.nodeType.Type.IsAssignableFrom(parentDom.Type))
            //    return false;

            if (!_IsDomNodeCheck(item))
                return false;

            //if (!bitBoxSchema.nodeType.Type.IsAssignableFrom(itemDom.Type))
            //    return false;

            return true;
        }

        /// <summary>
        /// Inserts 'item' into the set of objects at the desired position. Can only be called
        /// if CanInsert() returns true.</summary>
        /// <param name="parent">The object that will become the parent of the inserted object.
        /// Can be null if the list of objects is a flat list or if the root should be replaced.</param>
        /// <param name="before">The object that is immediately before the inserted object.
        /// Can be null to indicate that the inserted item should become the first child.</param>
        /// <param name="item">The item to be inserted. Consider using Util.ConvertData(item, false)
        /// to retrieve the final one or more items to be inserted.</param>
        void IOrderedInsertionContext.Insert(object parent, object before, object item)
        {
            DomNode parentDom = parent as DomNode;
            DomNode beforeDom = before as DomNode;
            DomNode[] itemsDom = _GetDomNodes(item);

            ChildInfo childInfo = parentDom.Type.GetChildInfo("node");
            IList<DomNode> childList = parentDom.GetChildList(childInfo);

            if (before == null)
            {
                int index = 0;
                foreach ( DomNode node in itemsDom )
                {
                    childList.Insert(++index, node);
                }
            }

            int beforeIndex = childList.IndexOf(beforeDom);
            if (beforeIndex == childList.Count - 1)
            {
                foreach (DomNode node in itemsDom)
                {
                    childList.Add(node);
                }
            }
            else
            {
                foreach (DomNode node in itemsDom)
                {
                    childList.Insert(++beforeIndex, node);
                }
            }
        }

        #endregion
        public bool HasChildren(object item)
        {
            foreach (object child in (this).GetChildren(item))
                return true;
            return false;
        }

        private void root_AttributeChanged(object sender, AttributeEventArgs e)
        {
            ItemChanged.Raise(this, new ItemChangedEventArgs<object>(e.DomNode));
            m_root.As<SceneDocument>().Dirty = true;
        }

        private void root_ChildInserted(object sender, ChildEventArgs e)
        {
            int index = GetChildIndex(e.Child, e.Parent);
            if (index >= 0)
            {
                ItemInserted.Raise(this, new ItemInsertedEventArgs<object>(index, e.Child, e.Parent));
                m_root.As<SceneDocument>().Dirty = true;
            }
        }

        private void root_ChildRemoving(object sender, ChildEventArgs e)
        {
            m_lastRemoveIndex = GetChildIndex(e.Child, e.Parent);
        }

        private void root_ChildRemoved(object sender, ChildEventArgs e)
        {
            if (m_lastRemoveIndex >= 0)
            {
                ItemRemoved.Raise(this, new ItemRemovedEventArgs<object>(m_lastRemoveIndex, e.Child, e.Parent));
                m_root.As<SceneDocument>().Dirty = true;
            }
        }

        private int GetChildIndex(object child, object parent)
        {
            // get child index by re-constructing what we'd give the tree control
            IEnumerable<object> treeChildren = GetChildren(parent);
            int i = 0;
            foreach (object treeChild in treeChildren)
            {
                if (treeChild.Equals(child))
                    return i;
                i++;
            }
            return -1;
        }

        private void treeControl_NodeSelectedChanged(object sender, TreeControl.NodeEventArgs e)
        {
            if (e.Node.Selected)
            {
                object item = e.Node.Tag;
                {
                    DomNode node = item as DomNode;
                    if (node != null)
                    {
                        NodeEditingContext nodeEditCtx = Root.Cast<NodeEditingContext>();
                        nodeEditCtx.Set(node);
                        ContextRegistry.ActiveContext = nodeEditCtx;
                        PropertyEditor.PropertyGrid.Bind(node);
                    }
                    else
                    {
                        PropertyEditor.PropertyGrid.Bind(null);
                    }
                }
            }
            else
            {
                PropertyEditor.PropertyGrid.Bind(null);
            }
        }

        private DomNode m_root;
        private int m_lastRemoveIndex;
        public IContextRegistry ContextRegistry { get;  set; }
        public PropertyEditor PropertyEditor { get; set; }
        public TreeControlEditor TreeEditor
        {
            get { return m_treeControlEditor; }
        }

        private FilteredTreeControlEditor m_treeControlEditor;
        private ControlInfo m_controlInfo;
        private UniqueNamer m_uniqueNamer;
    }
}
