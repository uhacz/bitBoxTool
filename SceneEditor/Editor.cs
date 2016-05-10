//using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Collections.Generic;
using System.Windows.Forms;
//using System.Text;

using Sce.Atf;
using Sce.Atf.Dom;
using Sce.Atf.Applications;
using Sce.Atf.Controls;
using Sce.Atf.Controls.PropertyEditing;
using Sce.Atf.Adaptation;
using System.Drawing;

namespace SceneEditor
{
    [Export(typeof(Editor))]
    [Export(typeof(IInitializable))]
    [PartCreationPolicy(CreationPolicy.Any)]
    public class Editor : IControlHostClient, IInitializable
    {
        [ImportingConstructor]
        public Editor(IControlHostService controlHostService, PropertyEditor propertyEditor, IContextRegistry contextRegistry )
        {
            m_controlHostService = controlHostService;

            m_treeControl = new TreeControl();
            m_treeControl.Dock = DockStyle.Fill;
            m_treeControl.AllowDrop = true;
            m_treeControl.SelectionMode = SelectionMode.MultiExtended;
            m_treeControl.ImageList = ResourceUtil.GetImageList16();
            m_treeControl.NodeSelectedChanged += treeControl_NodeSelectedChanged;
            m_treeControl.DragOver += treeControll_DragOver;
            m_treeControl.DragDrop += treeControll_DragDrop;
            m_treeControlAdapter = new TreeControlAdapter(m_treeControl);

            m_propertyEditor = propertyEditor;

            m_sceneRoot = new DomNode(bitBoxSchema.graphType.Type, bitBoxSchema.sceneRootElement);
            m_sceneRoot.SetAttribute(bitBoxSchema.graphType.nameAttribute, "Scene");
            m_sceneRoot.InitializeExtensions();
            m_editContext = m_sceneRoot.As<SceneEditingContext>();
            Root = m_sceneRoot;

            m_contextRegistry = contextRegistry;

            
            
        }

        public DomNode Root
        {
            get { return m_editContext.RootNode; }
            set
            {
                if (value != null)
                {
                    m_editContext.RootNode = value;
                    m_treeControlAdapter.TreeView = m_editContext;
                }
                else
                {
                    m_treeControlAdapter.TreeView = null;
                    m_editContext.RootNode = null;
                }
            }
        }
        /// <summary>
        /// Gets or sets whether DomNode adapters are displayed in the tree view</summary>
        //public bool ShowAdapters
        //{
        //    get { return m_domTreeView.ShowAdapters; }
        //    set { m_domTreeView.ShowAdapters = value; }
        //}

        /// <summary>
        /// Gets the TreeControl</summary>
        public TreeControl TreeControl
        {
            get { return m_treeControl; }
        }

        /// <summary>
        /// Gets the TreeControlAdapter</summary>
        //public TreeControlAdapter TreeControlAdapter
        //{
        //    get { return m_treeControlAdapter; }
        //}

        #region IInitializable Members
        public virtual void Initialize()
        {
            m_controlHostService.RegisterControl(
                    m_treeControl,
                    "Scene Explorer".Localize(),
                    "".Localize(),
                    StandardControlGroup.Center,
                    this
                );
        }
        #endregion

        #region IControlHostClient Members

        /// <summary>
        /// Activates the client control</summary>
        /// <param name="control">Client control to be activated</param>
        void IControlHostClient.Activate(Control control)
        {
        }

        /// <summary>
        /// Notifies the client that its Control has been deactivated. Deactivation occurs when
        /// another control or "host" control gets focus.</summary>
        /// <param name="control">Client Control that was deactivated</param>
        void IControlHostClient.Deactivate(Control control)
        {
        }

        /// <summary>
        /// Requests permission to close the client's Control.</summary>
        /// <param name="control">Client control to be closed</param>
        /// <returns>True if the control can close, or false to cancel</returns>
        bool IControlHostClient.Close(Control control)
        {
            return true;
        }

        #endregion

        private void treeControl_NodeSelectedChanged(object sender, TreeControl.NodeEventArgs e)
        {
            if (e.Node.Selected)
            {
                object item = e.Node.Tag;
                {
                    DomNode node = item as DomNode;
                    if (node != null)
                    {
                        NodeEditingContext nodeEditCtx = m_sceneRoot.Cast<NodeEditingContext>();
                        nodeEditCtx.Set(node);
                        m_contextRegistry.ActiveContext = nodeEditCtx;
                    }
                    else
                    {
                        m_propertyEditor.PropertyGrid.Bind(null);
                    }
                }
            }
            else
            {
                m_propertyEditor.PropertyGrid.Bind(null);
            }
        }

        private void treeControll_DragOver(object sender, DragEventArgs e)
        {
            bool canInsert = false;

            if (TreeControl.DragBetween)
            {
                TreeControl.Node parent, before;
                Point clientPoint = TreeControl.PointToClient(new Point(e.X, e.Y));
                if (TreeControl.GetInsertionNodes(clientPoint, out parent, out before))
                {
                    canInsert = ApplicationUtil.CanInsertBetween(
                        m_treeControlAdapter.TreeView,
                        parent != null ? parent.Tag : null,
                        before != null ? before.Tag : null,
                        e.Data);
                }
            }
            else
            {
                canInsert = ApplicationUtil.CanInsert(
                    m_treeControlAdapter.TreeView,
                    m_treeControlAdapter.LastHit,
                    e.Data);
            }


            e.Effect = DragDropEffects.None;
            if ( canInsert )
            {
                e.Effect = DragDropEffects.Move;
                ((Control)sender).Focus();
            }

            // A refresh is required to display the drag-between cue.  
            if (TreeControl.ShowDragBetweenCue)
                TreeControl.Invalidate();

            //
            //if (m_editContext.CanInsert(e.Data))
            //{
            //    ;
            //    ((Control)sender).Focus(); // Focus the list view; this will cause its context to become active
            //}
        }

        private void treeControll_DragDrop(object sender, DragEventArgs e)
        {
            //if (m_editContext.CanInsert(e.Data))
            //{
            //    ITransactionContext transactionContext = m_editContext as ITransactionContext;
            //    transactionContext.DoTransaction(delegate
            //        {
            //            m_editContext.Insert(e.Data);
            //        }, "Drag and Drop".Localize());
            //}
            if (TreeControl.DragBetween)
            {
                TreeControl.Node parent, before;
                Point clientPoint = TreeControl.PointToClient(new Point(e.X, e.Y));
                if (TreeControl.GetInsertionNodes(clientPoint, out parent, out before))
                {
                    ApplicationUtil.InsertBetween(
                        m_treeControlAdapter.TreeView,
                        parent != null ? parent.Tag : null,
                        before != null ? before.Tag : null,
                        e.Data,
                        "Drag and Drop",
                        null );
                }
            }
            else
            {
                ApplicationUtil.Insert(
                    m_treeControlAdapter.TreeView,
                    m_treeControlAdapter.LastHit,
                    e.Data,
                    "Drag and Drop",
                    null );
            }


            if (!TreeControl.ShowDragBetweenCue)
                return;


            TreeControl.Invalidate();
        }

        private readonly IControlHostService m_controlHostService;
        private readonly IContextRegistry m_contextRegistry;

        private readonly PropertyEditor m_propertyEditor;
        private readonly TreeControl m_treeControl;
        private readonly TreeControlAdapter m_treeControlAdapter;

        private readonly SceneEditingContext m_editContext;
        private readonly DomNode m_sceneRoot;
    }
}
