using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;

namespace FastReport.OpenSource.Winforms
{
    [ToolboxBitmap(typeof(FRPrintPreviewDialog), @"FastReport.OpenSource.Winforms.Print.print_preview.bmp"),
     DesignerCategory(@"FastReport.OpenSource.Winforms"), DesignTimeVisible(true), ToolboxItem(true)]
    public partial class FRPrintPreviewControl : UserControl
    {
        #region Events

        public event EventHandler<EventArgs> StartPageChanged;

        public event EventHandler<EventArgs> PageCountChanged;

        public event EventHandler<EventArgs> ZoomModeChanged;

        private void preview_PageCountChanged(object sender, EventArgs e)
        {
            Update();
            Application.DoEvents();
            if (lblPageCount != null)
                lblPageCount.Text = $@" de {preview.PageCount}";

            if (PageCountChanged == null) return;
            if (!(PageCountChanged.Target is ISynchronizeInvoke synchronizeInvoke))
                PageCountChanged.DynamicInvoke(this, EventArgs.Empty);
            else
                synchronizeInvoke.Invoke(PageCountChanged, new object[] { this, EventArgs.Empty });
        }

        private void preview_ZoomModeChanged(object sender, EventArgs e)
        {
            if (ZoomModeChanged == null) return;
            if (!(ZoomModeChanged.Target is ISynchronizeInvoke synchronizeInvoke))
                ZoomModeChanged.DynamicInvoke(this, EventArgs.Empty);
            else
                synchronizeInvoke.Invoke(ZoomModeChanged, new object[] { this, EventArgs.Empty });
        }

        private void preview_StartPageChanged(object sender, EventArgs e)
        {
            var page = preview.StartPage + 1;
            if (txtStartPage != null)
                txtStartPage.Text = page.ToString();

            if (StartPageChanged == null) return;
            if (!(StartPageChanged.Target is ISynchronizeInvoke synchronizeInvoke))
                StartPageChanged.DynamicInvoke(this, EventArgs.Empty);
            else
                synchronizeInvoke.Invoke(StartPageChanged, new object[] { this, EventArgs.Empty });
        }

        #endregion Events

        #region Fields

        private PrintDocument doc;

        #endregion Fields

        #region Constructors

        public FRPrintPreviewControl()
        {
            InitializeComponent();
        }

        #endregion Constructors

        #region Propriedades

        /// <summary>
        /// Gets or sets the <see cref="PrintDocument"/> being previewed.
        /// </summary>
        public PrintDocument Document
        {
            get { return doc; }
            set
            {
                // unhook event handlers
                if (doc != null)
                {
                    doc.BeginPrint -= doc_BeginPrint;
                    doc.EndPrint -= doc_EndPrint;
                }

                // save the value
                doc = value;

                // hook up event handlers
                if (doc != null)
                {
                    doc.BeginPrint += doc_BeginPrint;
                    doc.EndPrint += doc_EndPrint;
                }

                // don't assign document to preview until this form becomes visible
                preview.Document = Document;
            }
        }

        /// <summary>
        /// Gets a value that indicates whether the <see cref="Document"/> is being rendered.
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsRendering { get { return preview.IsRendering; } }

        /// <summary>
        /// Gets or sets how the zoom should be adjusted when the control is resized.
        /// </summary>
        [DefaultValue(ZoomMode.FullPage)]
        public ZoomMode ZoomMode
        {
            get { return preview.ZoomMode; }
            set { preview.ZoomMode = value; }
        }

        /// <summary>
        /// Gets or sets a custom zoom factor used when the <see cref="ZoomMode"/> property
        /// is set to <b>Custom</b>.
        /// </summary>
        [Browsable(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public double Zoom
        {
            get { return preview.Zoom; }
            set { preview.Zoom = value; }
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
            get { return preview.StartPage; }
            set { preview.StartPage = value; }
        }

        /// <summary>
        /// Gets the number of pages available for preview.
        /// </summary>
        /// <remarks>
        /// This number increases as the document is rendered into the control.
        /// </remarks>
        [Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int PageCount
        {
            get { return preview.PageCount; }
        }

        /// <summary>
        /// Gets or sets the control's background color.
        /// </summary>
        [DefaultValue(typeof(Color), "AppWorkspace")]
        public override sealed Color BackColor
        {
            get { return preview.BackColor; }
            set { preview.BackColor = value; }
        }

        /// <summary>
        /// Gets a list containing the images of the pages in the document.
        /// </summary>
        [Browsable(false)]
        public FRPageCollection PageImages
        {
            get { return preview.PageImages; }
        }

        public bool ShowToolbar
        {
            get { return _toolStrip.Visible; }
            set { _toolStrip.Visible = value; }
        }

        #endregion Propriedades

        #region Methods

        /// <summary>
        /// Prints the current document honoring the selected page range.
        /// </summary>
        public void Print(bool showPrintDialog = false)
        {
            preview.Print(showPrintDialog);
        }

        /// <summary>
        /// Regenerates the preview to reflect changes in the document layout.
        /// </summary>
        public void RefreshPreview()
        {
            preview.RefreshPreview();
        }

        /// <summary>
        /// Stops rendering the <see cref="Document"/>.
        /// </summary>
        public void Cancel()
        {
            preview.Cancel();
        }

        public void PrimeiraPagina()
        {
            preview.StartPage = 0;
        }

        public void PaginaAnterior()
        {
            preview.StartPage--;
        }

        public void ProximaPagina()
        {
            preview.StartPage++;
        }

        public void UltimaPagina()
        {
            preview.StartPage = preview.PageCount - 1;
        }

        public void IrParaPagina(int page)
        {
            preview.StartPage = page - 1;
        }

        #endregion Methods

        #region Toolbar

        #region Main Commands

        private void btnQuickPrint_Click(object sender, EventArgs e)
        {
            preview.Print();
        }

        private void btnPrint_Click(object sender, EventArgs e)
        {
            preview.Print(true);
        }

        #endregion Main Commands

        #region Zoom

        private void btnZoom_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if (e.ClickedItem == _itemActualSize)
            {
                preview.ZoomMode = ZoomMode.ActualSize;
            }
            else if (e.ClickedItem == _itemFullPage)
            {
                preview.ZoomMode = ZoomMode.FullPage;
            }
            else if (e.ClickedItem == _itemPageWidth)
            {
                preview.ZoomMode = ZoomMode.PageWidth;
            }
            else if (e.ClickedItem == _itemTwoPages)
            {
                preview.ZoomMode = ZoomMode.TwoPages;
            }
            if (e.ClickedItem == _item10)
            {
                preview.Zoom = .1;
            }
            else if (e.ClickedItem == _item100)
            {
                preview.Zoom = 1;
            }
            else if (e.ClickedItem == _item150)
            {
                preview.Zoom = 1.5;
            }
            else if (e.ClickedItem == _item200)
            {
                preview.Zoom = 2;
            }
            else if (e.ClickedItem == _item25)
            {
                preview.Zoom = .25;
            }
            else if (e.ClickedItem == _item50)
            {
                preview.Zoom = .5;
            }
            else if (e.ClickedItem == _item500)
            {
                preview.Zoom = 5;
            }
            else if (e.ClickedItem == _item75)
            {
                preview.Zoom = .75;
            }
        }

        #endregion Zoom

        #region Page Navigation

        private void btnFirst_Click(object sender, EventArgs e)
        {
            PrimeiraPagina();
        }

        private void btnPrev_Click(object sender, EventArgs e)
        {
            PaginaAnterior();
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            ProximaPagina();
        }

        private void btnLast_Click(object sender, EventArgs e)
        {
            UltimaPagina();
        }

        private void txtStartPage_Enter(object sender, EventArgs e)
        {
            txtStartPage.SelectAll();
        }

        private void txtStartPage_Validating(object sender, CancelEventArgs e)
        {
            CommitPageNumber();
        }

        private void txtStartPage_KeyPress(object sender, KeyPressEventArgs e)
        {
            var c = e.KeyChar;
            if (c == (char)13)
            {
                CommitPageNumber();
                e.Handled = true;
            }
            else if (c > ' ' && !char.IsDigit(c))
            {
                e.Handled = true;
            }
        }

        private void CommitPageNumber()
        {
            int page;
            if (int.TryParse(txtStartPage.Text, out page))
            {
                IrParaPagina(page);
            }
        }

        #endregion Page Navigation

        #region Job Control

        private void btnCancel_Click(object sender, EventArgs e)
        {
            if (preview.IsRendering)
            {
                preview.Cancel();
            }
            else
            {
                ParentForm?.Close();
            }
        }

        private void doc_BeginPrint(object sender, PrintEventArgs e)
        {
            btnCancel.Text = @"&Cancelar";
            btnCancel.ToolTipText = @"&Cancelar";
            btnQuickPrint.Enabled = false;
            btnPrint.Enabled = false;
        }

        private void doc_EndPrint(object sender, PrintEventArgs e)
        {
            btnCancel.Text = @"&Fechar";
            btnCancel.ToolTipText = @"&Fechar";
            btnQuickPrint.Enabled = true;
            btnPrint.Enabled = true;
        }

        #endregion Job Control

        #endregion Toolbar
    }
}