using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Windows.Forms;
//using System.Text;

using Sce.Atf;
using Sce.Atf.Dom;
using Sce.Atf.Applications;
using Sce.Atf.Controls;
using Sce.Atf.Adaptation;
using System.Drawing;

namespace SceneEditor
{
    [Export(typeof(Editor))]
    [Export(typeof(IDocumentClient))]
    [Export(typeof(IInitializable))]
    [PartCreationPolicy(CreationPolicy.Any)]
    public class Editor : IDocumentClient, IControlHostClient, IInitializable
    {
        [ImportingConstructor]
        public Editor(
            IControlHostService controlHostService, 
            PropertyEditor propertyEditor, 
            IContextRegistry contextRegistry,
            IDocumentRegistry documentRegistry,
            IDocumentService documentService,
            SchemaLoader schemaLoader )
        {

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

            //m_sceneRoot = _CreateSceneRoot("Scene");
            //m_sceneRoot.InitializeExtensions();
            //m_editContext = m_sceneRoot.As<SceneEditingContext>();
            //Root = m_sceneRoot;

            m_controlHostService = controlHostService;
            m_contextRegistry = contextRegistry;
            m_documentService = documentService;
            m_documentRegistry = documentRegistry;
            m_documentRegistry.ActiveDocumentChanged += documentRegistry_ActiveDocumentChanged;
            m_schemaLoader = schemaLoader;
        }

        private DomNode _CreateSceneRoot(string name)
        {
            DomNode node = new DomNode(bitBoxSchema.graphType.Type, bitBoxSchema.sceneRootElement);
            node.SetAttribute(bitBoxSchema.graphType.nameAttribute, name );
            return node;
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
        /// Gets the TreeControl</summary>
        public TreeControl TreeControl
        {
            get { return m_treeControl; }
        }

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
            //if (control == m_treeControl.As<Control>())
            //{
            //    m_contextRegistry.ActiveContext = Root;
            //}
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
                        NodeEditingContext nodeEditCtx = Root.Cast<NodeEditingContext>();
                        nodeEditCtx.Set(node);
                        m_contextRegistry.ActiveContext = nodeEditCtx;
                        m_propertyEditor.PropertyGrid.Bind(node);
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

        #region IDocumentClient Members

        /// <summary>
        /// Gets information about the document client, such as the file type and file
        /// extensions it supports, whether or not it allows multiple documents to be open, etc.</summary>
        public DocumentClientInfo Info
        {
            get { return s_info; }
        }

        /// <summary>
        /// Returns whether the client can open or create a document at the given URI</summary>
        /// <param name="uri">Document URI</param>
        /// <returns>True iff the client can open or create a document at the given URI</returns>
        public bool CanOpen(Uri uri)
        {
            return s_info.IsCompatibleUri(uri);
        }

        /// <summary>
        /// Info describing our document type</summary>
        private static DocumentClientInfo s_info =
            new DocumentClientInfo(
                "Scene".Localize(),   // file type
                ".source_scene",                // file extension
                null,                       // "new document" icon
                null);                      // "open document" icon

        /// <summary>
        /// Opens or creates a document at the given URI</summary>
        /// <param name="uri">Document URI</param>
        /// <returns>Document, or null if the document couldn't be opened or created</returns>
        public IDocument Open(Uri uri)
        {

            Document activeDoc = m_documentRegistry.GetActiveDocument<Document>();
            if (activeDoc != null)
            {
                if (!m_documentService.Close(activeDoc))
                    return null;
            }


            DomNode node = null;
            string filePath = uri.LocalPath;

            if (File.Exists(filePath))
            {
                // read existing document using standard XML reader
                using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    DomXmlReader reader = new DomXmlReader(m_schemaLoader);
                    node = reader.Read(stream, uri);
                }
            }
            else
            {
                // create new document by creating a Dom node of the root type defined by the schema
                node = _CreateSceneRoot("Scene");
            }

            Document document = null;
            if (node != null)
            {
                // Initialize Dom extensions now that the data is complete; after this, all Dom node
                //  adapters will have been bound to their underlying Dom node.
                node.InitializeExtensions();

                // get the root node's UIDocument adapter
                document = node.As<Document>();
                document.Uri = uri;

                // only allow 1 open document at a time
                Document activeDocument = m_documentRegistry.GetActiveDocument<Document>();
                if (activeDocument != null)
                    Close(activeDocument);


                m_editContext = node.As<SceneEditingContext>();
                Root = node;
            }

            return document;
        }

        /// <summary>
        /// Makes the document visible to the user</summary>
        /// <param name="document">Document to show</param>
        public void Show(IDocument document)
        {
            // set the active document and context; as there is only one editing context in
            //  a document, the document is also a context.
            m_contextRegistry.ActiveContext = document;
            m_documentRegistry.ActiveDocument = document;
        }

        /// <summary>
        /// Saves the document at the given URI</summary>
        /// <param name="document">Document to save</param>
        /// <param name="uri">New document URI</param>
        public void Save(IDocument document, Uri uri)
        {
            Document doc = document as Document;
            string filePath = uri.LocalPath;
            FileMode fileMode = File.Exists(filePath) ? FileMode.Truncate : FileMode.OpenOrCreate;
            using (FileStream stream = new FileStream(filePath, fileMode))
            {
                DomXmlWriter writer = new DomXmlWriter(m_schemaLoader.TypeCollection);
                writer.Write(doc.DomNode, stream, uri);
            }
        }

        /// <summary>
        /// Closes the document and removes any views of it from the UI</summary>
        /// <param name="document">Document to close</param>
        public void Close(IDocument document)
        {
            m_contextRegistry.RemoveContext(document);
            m_documentRegistry.Remove(document);
            Root = null;
            m_propertyEditor.PropertyGrid.Bind(null);
        }

        #endregion

        private void documentRegistry_ActiveDocumentChanged(object sender, EventArgs e)
        {
            Root = m_documentRegistry.GetActiveDocument<DomNode>();
        }

        private readonly IControlHostService m_controlHostService;
        private readonly IContextRegistry m_contextRegistry;
        private readonly IDocumentService m_documentService;
        private readonly IDocumentRegistry m_documentRegistry;

        private readonly PropertyEditor m_propertyEditor;
        private readonly TreeControl m_treeControl;
        private readonly TreeControlAdapter m_treeControlAdapter;
        private readonly SchemaLoader m_schemaLoader;

        private SceneEditingContext m_editContext;
    }
}
