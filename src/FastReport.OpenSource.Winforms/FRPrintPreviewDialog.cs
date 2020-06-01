using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;

namespace FastReport.OpenSource.Winforms
{
    [ToolboxBitmap(typeof(FRPrintPreviewDialog), @"FastReport.OpenSource.Winforms.Print.window_print.bmp"),
     DesignerCategory(@"FastReport.OpenSource.Winforms"), DesignTimeVisible(true), ToolboxItem(true),
     Designer("System.ComponentModel.Design.ComponentDesigner"),
     ToolboxItemFilter("System.Windows.Forms.Control.TopLevel")]
    public partial class FRPrintPreviewDialog : Form
    {
        #region Constructors

        public FRPrintPreviewDialog()
        {
            InitializeComponent();
        }

        #endregion Constructors

        #region Propriedades

        /// <summary>
        /// Gets or sets the <see cref="PrintDocument"/> being previewed.
        /// </summary>
        public PrintDocument Document { get; set; }

        /// <summary>
        /// Gets a value that indicates whether the <see cref="Document"/> is being rendered.
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsRendering => preview.IsRendering;

        /// <summary>
        /// Gets or sets how the zoom should be adjusted when the control is resized.
        /// </summary>
        [DefaultValue(ZoomMode.FullPage)]
        public ZoomMode ZoomMode
        {
            get => preview.ZoomMode;
            set => preview.ZoomMode = value;
        }

        /// <summary>
        /// Gets or sets a custom zoom factor used when the <see cref="ZoomMode"/> property
        /// is set to <b>Custom</b>.
        /// </summary>
        [Browsable(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public double Zoom
        {
            get => preview.Zoom;
            set => preview.Zoom = value;
        }

        /// <summary>
        /// Gets or sets the first page being previewed.
        /// </summary>
        /// <remarks>
        /// There may be one or two pages visible depending on the setting of the
        /// <see cref="ZoomMode"/> property.
        /// </remarks>
        [Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int StartPage
        {
            get => preview.StartPage;
            set => preview.StartPage = value;
        }

        /// <summary>
        /// Gets the number of pages available for preview.
        /// </summary>
        /// <remarks>
        /// This number increases as the document is rendered into the control.
        /// </remarks>
        [Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int PageCount => preview.PageCount;

        /// <summary>
        /// Gets or sets the control's background color.
        /// </summary>
        [DefaultValue(typeof(Color), "AppWorkspace")]
        public sealed override Color BackColor
        {
            get => preview.BackColor;
            set => preview.BackColor = value;
        }

        #endregion Propriedades

        #region Overloads

        /// <summary>
        /// Creates the handle for the form. If a derived class overrides this function, it must call the base implementation.
        /// </summary>
        /// <exception cref="System.Drawing.Printing.InvalidPrinterException"></exception>
        protected override void CreateHandle()
        {
            // We want to check printer settings before we push the modal message loop,
            // so the user has a chance to catch the exception instead of letting go to
            // the windows forms exception dialog.
            if (Document != null && !Document.PrinterSettings.IsValid)
                throw new InvalidPrinterException(Document.PrinterSettings);

            base.CreateHandle();
        }

        /// <summary>
        /// Overridden to assign document to preview control only after the
        /// initial activation.
        /// </summary>
        /// <param name="e"><see cref="EventArgs"/> that contains the event data.</param>
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            preview.Document = Document;
        }

        /// <summary>
        /// Overridden to cancel any ongoing previews when closing form.
        /// </summary>
        /// <param name="e"><see cref="FormClosingEventArgs"/> that contains the event data.</param>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            if (preview.IsRendering && !e.Cancel)
            {
                preview.Cancel();
            }
        }

        #endregion Overloads
    }
}