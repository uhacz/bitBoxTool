using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Text;

using Sce.Atf;
using Sce.Atf.Dom;
using Sce.Atf.Applications;
using Sce.Atf.Adaptation;

namespace SceneEditor
{
    public class SceneEditingContext : EditingContext, ITreeView, IItemView, IObservableContext, IInstancingContext, IEnumerableContext
    {
        public DomNode RootNode
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
                }

                m_root = value;

                if (m_root != null)
                {
                    m_root.AttributeChanged += root_AttributeChanged;
                    m_root.ChildInserted += root_ChildInserted;
                    m_root.ChildRemoving += root_ChildRemoving;
                    m_root.ChildRemoved += root_ChildRemoved;
                }

                Reloaded.Raise(this, EventArgs.Empty);
            }
        }

        //public bool ShowAdapters
        //{
        //    get { return m_showAdapters; }
        //    set { m_showAdapters = value; }
        //}

        #region ITreeView Members

        public object Root
        {
            get { return m_root; }
        }

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
        #region IInstancingContext Members

        /// <summary>
        /// Returns whether the context can copy the selection</summary>
        /// <returns>True iff the context can copy</returns>
        public bool CanCopy()
        {
            return Selection.Count > 0;
        }

        /// <summary>
        /// Copies the selection. Returns a data object representing the copied items.</summary>
        /// <returns>Data object representing the copied items; e.g., a
        /// System.Windows.Forms.IDataObject object</returns>
        public object Copy()
        {
            IEnumerable<DomNode> resources = Selection.AsIEnumerable<DomNode>();
            List<object> copies = new List<object>(DomNode.Copy(resources));
            return new DataObject(copies.ToArray());
        }

        /// <summary>
        /// Returns whether the context can insert the data object</summary>
        /// <param name="insertingObject">Data to insert; e.g., System.Windows.Forms.IDataObject</param>
        /// <returns>True iff the context can insert the data object</returns>
        public bool CanInsert(object insertingObject)
        {
            IDataObject dataObject = (IDataObject)insertingObject;
            object[] items = dataObject.GetData(typeof(object[])) as object[];
            if (items == null)
                return false;

            foreach (object item in items)
                if (!item.Is<DomNode>())
                    return false;

            return true;
        }

        /// <summary>
        /// Inserts the data object into the context</summary>
        /// <param name="insertingObject">Data to insert; e.g., System.Windows.Forms.IDataObject</param>
        public void Insert(object insertingObject)
        {
            IDataObject dataObject = (IDataObject)insertingObject;
            object[] items = dataObject.GetData(typeof(object[])) as object[];
            if (items == null)
                return;

            DomNode[] itemCopies = DomNode.Copy(items.AsIEnumerable<DomNode>());

            foreach (DomNode dnode in itemCopies)
            {
                RootNode.GetChildList(bitBoxSchema.graphType.nodeChild).Add(dnode);
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

        public bool HasChildren(object item)
        {
            foreach (object child in (this).GetChildren(item))
                return true;
            return false;
        }

        private void root_AttributeChanged(object sender, AttributeEventArgs e)
        {
            ItemChanged.Raise(this, new ItemChangedEventArgs<object>(e.DomNode));
        }

        private void root_ChildInserted(object sender, ChildEventArgs e)
        {
            int index = GetChildIndex(e.Child, e.Parent);
            if (index >= 0)
            {
                ItemInserted.Raise(this, new ItemInsertedEventArgs<object>(index, e.Child, e.Parent));
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

        private DomNode m_root;
        private int m_lastRemoveIndex;
        //private bool m_showAdapters = true;
    }
}
