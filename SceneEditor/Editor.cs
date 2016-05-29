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


namespace SceneEditor
{
    [Export(typeof(IDocumentClient))]
    [Export(typeof(Editor))]
    [Export(typeof(IInitializable))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class Editor : IDocumentClient, IControlHostClient, IInitializable
    {
        [ImportingConstructor]
        public Editor(
            IControlHostService controlHostService, 
            PropertyEditor propertyEditor, 
            IContextRegistry contextRegistry,
            IDocumentRegistry documentRegistry,
            IDocumentService documentService,
            ICommandService commandService,
            SchemaLoader schemaLoader )
        {
            m_propertyEditor = propertyEditor;
            m_controlHostService = controlHostService;
            m_contextRegistry = contextRegistry;
            m_documentService = documentService;
            m_documentRegistry = documentRegistry;
            m_commandService = commandService;
            //m_documentRegistry.ActiveDocumentChanged += documentRegistry_ActiveDocumentChanged;
            m_schemaLoader = schemaLoader;  
        }

        #region IInitializable Members
        public virtual void Initialize()
        {
            //m_controlHostService.RegisterControl(
            //        m_treeControl,
            //        "Scene Explorer".Localize(),
            //        "".Localize(),
            //        StandardControlGroup.Center,
            //        this
            //    );
        }
        #endregion

        #region IControlHostClient Members

        /// <summary>
        /// Activates the client control</summary>
        /// <param name="control">Client control to be activated</param>
        void IControlHostClient.Activate(Control control)
        {
            SceneDocument document = control.Tag as SceneDocument;
            if (document != null)
            {
                m_documentRegistry.ActiveDocument = document;

                SceneEditingContext context = document.As<SceneEditingContext>();
                m_contextRegistry.ActiveContext = context;
                //context.TreeEditor.TreeControl.Focus();
            }
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
            bool closed = true;

            SceneDocument document = control.Tag as SceneDocument;
            if (document != null)
            {
                SceneEditingContext context = document.As<SceneEditingContext>();
                if (context != null)
                {
                    m_controlHostService.UnregisterControl(context.TreeEditor.TreeControl);
                }

                closed = m_documentService.Close(document);
                if (closed)
                {
                    //m_currentEditContext = null;
                    m_propertyEditor.PropertyGrid.Bind(null);
                    m_contextRegistry.RemoveContext(document);
                }
            }

            return closed;
        }

        #endregion

        #region IDocumentClient Members

        /// <summary>
        /// Gets information about the document client, such as the file type and file
        /// extensions it supports, whether or not it allows multiple documents to be open, etc.</summary>
        public DocumentClientInfo Info
        {
            get { return s_info; }
        }

        public static DocumentClientInfo DocInfo
        {
            get { return s_info; }
        }
        /// <summary>
        /// Info describing our document type</summary>
        private static DocumentClientInfo s_info =
            new DocumentClientInfo(
                "Scene".Localize(),   // file type
                ".source_scene",                // file extension
                null,                       // "new document" icon
                null,                      // "open document" icon
                true);
        /// <summary>
        /// Returns whether the client can open or create a document at the given URI</summary>
        /// <param name="uri">Document URI</param>
        /// <returns>True iff the client can open or create a document at the given URI</returns>
        public bool CanOpen(Uri uri)
        {
            return s_info.IsCompatibleUri(uri);
        }

        /// <summary>
        /// Opens or creates a document at the given URI</summary>
        /// <param name="uri">Document URI</param>
        /// <returns>Document, or null if the document couldn't be opened or created</returns>
        public IDocument Open(Uri uri)
        {
            DomNode node = null;
            string filePath = uri.LocalPath;
            string fileName = Path.GetFileName(filePath);

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
                node = SceneEditingContext._CreateSceneRoot("Scene");
            }



            SceneDocument document = null;
            if (node != null)
            {
                // Initialize Dom extensions now that the data is complete; after this, all Dom node
                //  adapters will have been bound to their underlying Dom node.
                node.InitializeExtensions();

                SceneEditingContext context = node.As<SceneEditingContext>();

                ControlInfo controlInfo = new ControlInfo(fileName, filePath, StandardControlGroup.Center);
                controlInfo.IsDocument = true;
                context.ControlInfo = controlInfo;

                document = node.As<SceneDocument>();
                document.Uri = uri;

                context.PropertyEditor = m_propertyEditor;
                context.ContextRegistry = m_contextRegistry;
                context.Initialize(m_commandService);
                context.TreeEditor.TreeControl.Tag = document;

                context.Root = node;
                m_controlHostService.RegisterControl(context.TreeEditor.TreeControl, context.ControlInfo, this);
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

            SceneEditingContext context = document.As<SceneEditingContext>();
            m_controlHostService.Show(context.TreeEditor.TreeControl);
        }

        /// <summary>
        /// Saves the document at the given URI</summary>
        /// <param name="document">Document to save</param>
        /// <param name="uri">New document URI</param>
        public void Save(IDocument document, Uri uri)
        {
            SceneDocument doc = document as SceneDocument;
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
            m_documentRegistry.Remove(document);
        }

        #endregion

        private void documentRegistry_ActiveDocumentChanged(object sender, EventArgs e)
        {
            //m_currentEditContext = m_documentRegistry.GetActiveDocument<SceneEditingContext>();
        }
        private readonly IControlHostService m_controlHostService;
        private readonly IContextRegistry m_contextRegistry;
        private readonly IDocumentService m_documentService;
        private readonly IDocumentRegistry m_documentRegistry;
        private readonly ICommandService m_commandService;

        private readonly PropertyEditor m_propertyEditor;
        private readonly SchemaLoader m_schemaLoader;
    }
}
