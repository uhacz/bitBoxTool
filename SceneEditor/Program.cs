﻿//Copyright � 2014 Sony Computer Entertainment America LLC. See License.txt.

using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;

using Sce.Atf;
using Sce.Atf.Adaptation;
using Sce.Atf.Applications;
//using Sce.Atf.Applications.WebServices;
using Sce.Atf.Dom;

namespace SceneEditor
{
    /// <summary>
    /// This is a tree list editor sample application.
    /// This sample editor shows how to create and add entries to various kinds of Tree lists, 
    /// including a hierarchical list to display selected folders' underlying folders and files. 
    /// It demonstrates ATF features such as using the application shell framework, 
    /// use of controls such as TreeListView and use of the SettingsService to persist list column widths. 
    /// The sample shows adding and removing list items and notifying the user of these events.
    /// For more information, see https://github.com/SonyWWS/ATF/wiki/ATF-Tree-List-Editor-Sample. </summary>
    static class Program
    {
        /// <summary>
        /// The main entry point for the application</summary>
        [STAThread]
        static void Main()
        {
            //// important to call these before creating application host
            //Application.EnableVisualStyles();
            //Application.DoEvents(); // see http://www.codeproject.com/buglist/EnableVisualStylesBug.asp?df=100&forumid=25268&exp=0&select=984714

            //// Set up localization support early on, so that user-readable strings will be localized
            ////  during the initialization phase below. Use XML files that are embedded resources.
            //Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.CurrentCulture;
            //Localizer.SetStringLocalizer(new EmbeddedResourceStringLocalizer());

            //// Enable metadata driven property editing for the DOM
            //DomNodeType.BaseOfAllTypes.AddAdapterCreator(new AdapterCreator<CustomTypeDescriptorNodeAdapter>());

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.DoEvents(); // see http://www.codeproject.com/buglist/EnableVisualStylesBug.asp?df=100&forumid=25268&exp=0&select=984714

            // Set up localization support early on, so that user-readable strings will be localized
            //  during the initialization phase below. Use XML files that are embedded resources.
            Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.CurrentCulture;
            Localizer.SetStringLocalizer(new EmbeddedResourceStringLocalizer());

            // Enable metadata driven property editing for the DOM
            DomNodeType.BaseOfAllTypes.AddAdapterCreator(new AdapterCreator<CustomTypeDescriptorNodeAdapter>());

            using (
                var catalog =
                    new TypeCatalog(

                        /* Standard ATF stuff */
                        typeof(SettingsService),                // persistent settings and user preferences dialog
                        typeof(StatusService),                  // status bar at bottom of main Form
                        typeof(CommandService),                 // handles commands in menus and toolbars
                        typeof(ControlHostService),             // docking control host
                        typeof(WindowLayoutService),            // multiple window layout support
                        typeof(WindowLayoutServiceCommands),    // window layout commands
                        typeof(OutputService),                  // rich text box for displaying error and warning messages. Implements IOutputWriter.
                        typeof(Outputs),                        // passes messages to all log writers
                        typeof(CrashLogger),                    // logs unhandled exceptions to an ATF server
                        typeof(UnhandledExceptionService),      // catches unhandled exceptions, displays info, and gives user a chance to save

                        typeof(DocumentRegistry),               // central document registry with change notification
                        typeof(FileDialogService),              // standard Windows file dialogs
                        typeof(AutoDocumentService),            // opens documents from last session, or creates a new document, on startup
                        typeof(RecentDocumentCommands),         // standard recent document commands in File menu
                        typeof(StandardFileCommands),           // standard File menu commands for New, Open, Save, SaveAs, Close
                        typeof(StandardFileExitCommand),        // standard File exit menu command
                        typeof(TabbedControlSelector),          // enable ctrl-tab selection of documents and controls within the app

                        typeof(AtfUsageLogger),                 // logs computer info to an ATF server
                        typeof(CrashLogger),                    // logs unhandled exceptions to an ATF server
                        typeof(UnhandledExceptionService),      // catches unhandled exceptions, displays info, and gives user a chance to save
                        
                        typeof(ContextRegistry),                // central context registry with change notification
                        typeof(StandardEditCommands),           // standard Edit menu commands for copy/paste
                        typeof(StandardEditHistoryCommands),    // standard Edit menu commands for undo/redo
                        typeof(StandardSelectionCommands),      // standard Edit menu selection commands
                        
                        typeof(PaletteService),                 // global palette, for drag/drop instancing
                        typeof(PropertyEditor),                 // property grid for editing selected objects
                        typeof(PropertyEditingCommands),        // commands for PropertyEditor and GridPropertyEditor, like Reset,

                        typeof(SchemaLoader),
                        typeof(PaletteClient),                  // component which adds items to palette
                        typeof(Editor)                     // tree list view editor component
                    ))
            {
                using (var container = new CompositionContainer(catalog))
                {
                    var toolStripContainer = new ToolStripContainer();
                    toolStripContainer.Dock = DockStyle.Fill;

                    using (var mainForm = new MainForm(toolStripContainer))
                    {
                        mainForm.Text = "SceneEditor".Localize();
                        mainForm.Icon = GdiUtil.CreateIcon(ResourceUtil.GetImage(Sce.Atf.Resources.AtfIconImage));

                        var batch = new CompositionBatch();
                        batch.AddPart(mainForm);
                        batch.AddPart(new WebHelpCommands("https://github.com/SonyWWS/ATF/wiki/ATF-Tree-List-Editor-Sample".Localize()));
                        container.Compose(batch);

                        container.InitializeAll();

                        //// Set the switch level for the Atf TraceSource instance so everything is traced. 
                        //Outputs.TraceSource.Switch.Level = SourceLevels.All;
                        //// a very verbose data display that includes callstacks
                        //Outputs.TraceSource.Listeners["Default"].TraceOutputOptions =
                        //    TraceOptions.Callstack | TraceOptions.DateTime |
                        //    TraceOptions.ProcessId | TraceOptions.ThreadId |
                        //    TraceOptions.Timestamp;
                        
                        Application.Run(mainForm);

                        // Give components a chance to clean up.
                        container.Dispose();
                    }
                }
            }
        }
    }
}