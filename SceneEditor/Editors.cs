//Copyright © 2014 Sony Computer Entertainment America LLC. See License.txt.

using System;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Windows.Forms;

using Sce.Atf;
using Sce.Atf.Applications;
using Sce.Atf.Controls;
using Sce.Atf.Controls.PropertyEditing;

namespace SceneEditor
{
    /// <summary>
    /// Common data item editor derived from TreeListViewEditor used for all the tree view editors
    /// except RawTreeListView.</summary>
    [InheritedExport(typeof(IInitializable))]
    class CommonEditor : TreeListViewEditor, IInitializable, IControlHostClient
    {
        /// <summary>
        /// Constructor that configures TreeListView. Creates and registers control it populates with desired buttons
        /// that have the handler method BtnClick().</summary>
        /// <param name="name">Name of editor</param>
        /// <param name="style">TreeListView style</param>
        /// <param name="flags">Flags indicating which buttons appear for this editor</param>
        /// <param name="contextRegistry">Context registry</param>
        /// <param name="settingsService">Settings service</param>
        /// <param name="controlHostService">Control host service</param>
        public CommonEditor(
            string name,
            TreeListView.Style style,
            Buttons flags,
            IContextRegistry contextRegistry,
            ISettingsService settingsService,
            IControlHostService controlHostService)
            : base(style)
        {
            m_contextRegistry = contextRegistry;

            TreeListView.Name = name;
            TreeListView.NodeExpandedChanged += TreeListViewNodeExpandedChanged;

            {
                var owner =
                    string.Format(
                        "{0}-{1}-TreeListView",
                        this,
                        TreeListView.Name);

                settingsService.RegisterSettings(
                    owner,
                    new BoundPropertyDescriptor(
                        TreeListView,
                        () => TreeListView.PersistedSettings,
                        SettingsDisplayName,
                        SettingsCategory,
                        SettingsDescription));
            }

            {
                //
                // Create custom control to contain any
                // data creation buttons + TreeListView
                //

                m_uberControl = new UserControl {Dock = DockStyle.Fill};

                int x = 2, y = 2;
                var buttonHeight = -1;

                if ((flags & Buttons.AddItem) == Buttons.AddItem)
                {
                    var btn = CreateButton(AddItemText, ref x, ref y, ref buttonHeight);
                    btn.Tag = Buttons.AddItem;
                    btn.Click += BtnClick;
                    m_uberControl.Controls.Add(btn);
                }

                if ((flags & Buttons.Reload) == Buttons.Reload)
                {
                    var btn = CreateButton(ReloadText, ref x, ref y, ref buttonHeight);
                    btn.Tag = Buttons.Reload;
                    btn.Click += BtnClick;
                    m_uberControl.Controls.Add(btn);
                }

                if ((flags & Buttons.ExpandAll) == Buttons.ExpandAll)
                {
                    var btn = CreateButton(ExpandAllText, ref x, ref y, ref buttonHeight);
                    btn.Tag = Buttons.ExpandAll;
                    btn.Click += BtnClick;
                    m_uberControl.Controls.Add(btn);
                }

                if ((flags & Buttons.CollapseAll) == Buttons.CollapseAll)
                {
                    var btn = CreateButton(CollapseAllText, ref x, ref y, ref buttonHeight);
                    btn.Tag = Buttons.CollapseAll;
                    btn.Click += BtnClick;
                    m_uberControl.Controls.Add(btn);
                }

                if ((flags & Buttons.RemoveItem) == Buttons.RemoveItem)
                {
                    var btn = CreateButton(RemoveItemText, ref x, ref y, ref buttonHeight);
                    btn.Tag = Buttons.RemoveItem;
                    btn.Click += BtnClick;
                    m_uberControl.Controls.Add(btn);
                }

                if ((flags & Buttons.ModifySelected) == Buttons.ModifySelected)
                {
                    var btn = CreateButton(ModifySelectedText, ref x, ref y, ref buttonHeight);
                    btn.Tag = Buttons.ModifySelected;
                    btn.Click += BtnClick;
                    m_uberControl.Controls.Add(btn);
                }

                if ((flags & Buttons.SelectAll) == Buttons.SelectAll)
                {
                    var btn = CreateButton(SelectAllText, ref x, ref y, ref buttonHeight);
                    btn.Tag = Buttons.SelectAll;
                    btn.Click += BtnClick;
                    m_uberControl.Controls.Add(btn);
                }

                {
                    TreeListView.Control.Location = new Point(0, buttonHeight + 2);
                    TreeListView.Control.Anchor =
                        AnchorStyles.Left | AnchorStyles.Top |
                        AnchorStyles.Right | AnchorStyles.Bottom;

                    TreeListView.Control.Width = m_uberControl.Width;
                    TreeListView.Control.Height = m_uberControl.Height - buttonHeight - 2;

                    m_uberControl.Controls.Add(TreeListView);
                }

                var info =
                    new ControlInfo(
                        TreeListView.Name,
                        TreeListView.Name + " - TreeListView",
                        StandardControlGroup.CenterPermanent);

                controlHostService.RegisterControl(
                    m_uberControl,
                    info,
                    this);
            }
        }

        /// <summary>
        /// Flags for various buttons used by editors</summary>
        [Flags]
        public enum Buttons
        {
            /// <summary>
            /// No buttons</summary>
            None = 0,
            /// <summary>
            /// Add flat data button</summary>
            AddItem = (1 << 0),
            /// <summary>
            /// Reload data button</summary>
            Reload = (1 << 1),
            /// <summary>
            /// Expand all button</summary>
            ExpandAll = (1 << 2),
            /// <summary>
            /// Collapse all button</summary>
            CollapseAll = (1 << 3),
            /// <summary>
            /// Remove item button</summary>
            RemoveItem = (1 << 4),
            /// <summary>
            /// Modify selected button</summary>
            ModifySelected = (1 << 5),
            /// <summary>
            /// Select all button</summary>
            SelectAll = (1 << 6),
        }

        /// <summary>
        /// Initialize</summary>
        void IInitializable.Initialize()
        {
            // So the GUI will show up since nothing else imports it...
        }

        #region IControlHostClient Interface

        /// <summary>
        /// Notifies the client that its Control has been activated. Activation occurs when
        /// the Control gets focus, or a parent "host" Control gets focus.</summary>
        /// <param name="control">Client Control that was activated</param>
        /// <remarks>This method is only called by IControlHostService if the Control was previously
        /// registered for this IControlHostClient.</remarks>
        public void Activate(Control control)
        {
            if (ReferenceEquals(control, m_uberControl) && View != null)
                m_contextRegistry.ActiveContext = View;
        }

        /// <summary>
        /// Notifies the client that its Control has been deactivated. Deactivation occurs when
        /// another Control or "host" Control gets focus.</summary>
        /// <param name="control">Client Control that was deactivated</param>
        /// <remarks>This method is only called by IControlHostService if the Control was previously
        /// registered for this IControlHostClient.</remarks>
        public void Deactivate(Control control)
        {
        }

        /// <summary>
        /// Requests permission to close the client's Control</summary>
        /// <param name="control">Client Control to be closed</param>
        /// <returns>True if the Control can close, or false to cancel</returns>
        /// <remarks>
        /// 1. This method is only called by IControlHostService if the Control was previously
        /// registered for this IControlHostClient.
        /// 2. If true is returned, the IControlHostService calls its own
        /// UnregisterControl. The IControlHostClient has to call RegisterControl again
        /// if it wants to re-register this Control.</remarks>
        public bool Close(Control control)
        {
            return true;
        }

        #endregion

        private void TreeListViewNodeExpandedChanged(object sender, TreeListView.NodeEventArgs e)
        {
            Outputs.WriteLine(
                OutputMessageType.Info,
                "{0} {1}",
                e.Node.Label,
                e.Node.Expanded ? "expanded!" : "collapsed!");
        }

        private void BtnClick(object sender, EventArgs e)
        {
            var btn = (Button)sender;
            var flags = (Buttons)btn.Tag;

            switch (flags)
            {
                case Buttons.AddItem:
                    DataContainer.GenerateFlat(
                        View,
                        (TreeListView.TheStyle != TreeListView.Style.TreeList) &&
                        (TreeListView.TheStyle != TreeListView.Style.CheckedTreeList)
                            ? null
                            : TreeListViewAdapter.LastHit);
                    break;

                case Buttons.Reload:
                    DataContainer.Reload(View);
                    break;

                case Buttons.ExpandAll:
                    TreeListView.ExpandAll();
                    break;

                case Buttons.CollapseAll:
                    TreeListView.CollapseAll();
                    break;

                case Buttons.RemoveItem:
                    DataContainer.RemoveItem(View, TreeListViewAdapter.LastHit);
                    break;

                case Buttons.ModifySelected:
                    DataContainer.ModifySelected(View, TreeListViewAdapter.Selection);
                    break;

                case Buttons.SelectAll:
                    {
                        TreeListView.BeginUpdate();
                        foreach (var node in TreeListView.Nodes)
                            node.Selected = true;
                        TreeListView.EndUpdate();
                    }
                    break;
            }
        }

        private static Button CreateButton(string text, ref int x, ref int y, ref int height)
        {
            var btn = new Button {Text = text};

            var size = TextRenderer.MeasureText(btn.Text, btn.Font);
            btn.Width = size.Width + 20;

            btn.Location = new Point(x, y);
            btn.Anchor = AnchorStyles.Left | AnchorStyles.Top;

            x += btn.Width + 2;

            if (height == -1)
                height = btn.Height;

            return btn;
        }

        private readonly UserControl m_uberControl;
        private readonly IContextRegistry m_contextRegistry;

        private const string AddItemText = "Add Item";
        private const string AddHierarchicalText = "Add Hierarchical";
        private const string AddVirtualText = "Add Virtual";
        private const string ReloadText = "Reload";
        private const string ExpandAllText = "Expand All";
        private const string CollapseAllText = "Collapse All";
        private const string RemoveItemText = "Remove Item";
        private const string ModifySelectedText = "Modify Selected";
        private const string SelectAllText = "Select All";
        private const string RecursiveCheckBoxesOnText = "Recursive CheckBoxes: on";
        private const string RecursiveCheckBoxesOffText = "Recursive CheckBoxes: off";
        private const string SettingsDisplayName = "TreeListView Persisted Settings";
        private const string SettingsCategory = "TreeListView";
        private const string SettingsDescription = "TreeListView Persisted Settings";
    }
    
    /// <summary>
    /// Tree list view editor component deriving from CommonEditor</summary>
    [Export(typeof(TreeList))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    class TreeList : CommonEditor
    {
        /// <summary>
        /// Constructor</summary>
        /// <param name="contextRegistry">Context registry</param>
        /// <param name="settingsService">Settings service</param>
        /// <param name="controlHostService">Control host service</param>
        [ImportingConstructor]
        public TreeList(
            IContextRegistry contextRegistry,
            ISettingsService settingsService,
            IControlHostService controlHostService)
            : base(
                "Tree List",
                TreeListView.Style.TreeList,
                Buttons.AddItem | Buttons.Reload | Buttons.ExpandAll |
                Buttons.CollapseAll | Buttons.RemoveItem,
                contextRegistry,
                settingsService,
                controlHostService)
        {
            TreeListView.NodeSorter = new DataComparer(TreeListView);

            View = new DataContainer();
        }
    }
}