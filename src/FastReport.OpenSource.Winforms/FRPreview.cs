using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.Windows.Forms;

namespace FastReport.OpenSource.Winforms
{
    [ToolboxItem(false)]
    internal partial class FRPreview : UserControl
    {
        #region Fields

        private PrintDocument doc;
        private ZoomMode zoomMode;
        private double zoom;
        private int startPage;
        private Brush backBrush;
        private Point ptLast;
        private PointF himm2Pix = new PointF(-1, -1);
        private bool cancel;
        private const int MARGIN = 4;
        private bool Inicializando;
        private readonly List<Image> pageImages;

        #endregion Fields

        #region Constructors

        public FRPreview()
        {
            Inicializando = true;
            pageImages = new List<Image>();
            BackColor = SystemColors.AppWorkspace;
            zoomMode = ZoomMode.FullPage;
            startPage = 0;
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            InitializeComponent();
            Inicializando = false;
        }

        #endregion Constructors

        #region Propriedades

        /// <summary>
        /// Gets or sets the <see cref="PrintDocument"/> being previewed.
        /// </summary>
        public PrintDocument Document
        {
            get => doc;
            set
            {
                if (value == doc)
                    return;

                doc = value;
                RefreshPreview();
            }
        }

        /// <summary>
        /// Gets a value that indicates whether the <see cref="Document"/> is being rendered.
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsRendering { get; private set; }

        /// <summary>
        /// Gets or sets how the zoom should be adjusted when the control is resized.
        /// </summary>
        [DefaultValue(ZoomMode.FullPage)]
        public ZoomMode ZoomMode
        {
            get => zoomMode;
            set
            {
                if (value == zoomMode)
                    return;

                zoomMode = value;
                UpdateScrollBars();
                OnZoomModeChanged();
            }
        }

        /// <summary>
        /// Gets or sets a custom zoom factor used when the <see cref="ZoomMode"/> property
        /// is set to <b>Custom</b>.
        /// </summary>
        [Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public double Zoom
        {
            get => zoom;
            set
            {
                if (Equals(value, zoom) &&
                    ZoomMode == ZoomMode.Custom)
                    return;

                ZoomMode = ZoomMode.Custom;
                zoom = value;
                UpdateScrollBars();
                OnZoomModeChanged();
            }
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
            get => startPage;
            set
            {
                // validate new setting
                if (value > PageCount - 1)
                    value = PageCount - 1;

                if (value < 0)
                    value = 0;

                // apply new setting
                if (value == startPage)
                    return;

                startPage = value;
                UpdateScrollBars();
                OnStartPageChanged();
            }
        }

        /// <summary>
        /// Gets the number of pages available for preview.
        /// </summary>
        /// <remarks>
        /// This number increases as the document is rendered into the control.
        /// </remarks>
        [Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int PageCount => pageImages.Count;

        /// <summary>
        /// Gets or sets the control's background color.
        /// </summary>
        [DefaultValue(typeof(Color), "AppWorkspace")]
        public sealed override Color BackColor
        {
            get => base.BackColor;
            set
            {
                base.BackColor = value;
                backBrush = new SolidBrush(value);
            }
        }

        #endregion Propriedades

        #region Methods

        /// <summary>
        /// Prints the current document honoring the selected page range.
        /// </summary>
        public void Print(bool showPrintDialog = false)
        {
            PrinterSettings ps;
            if (showPrintDialog)
            {
                using (var dlg = new PrintDialog())
                {
                    // configure dialog
                    dlg.AllowSomePages = true;
                    dlg.AllowSelection = true;
                    dlg.UseEXDialog = true;
                    dlg.Document = doc;

                    // show allowed page range
                    ps = dlg.PrinterSettings;
                    ps.MinimumPage = ps.FromPage = 1;
                    ps.MaximumPage = ps.ToPage = PageCount;

                    // show dialog
                    if (dlg.ShowDialog(this) == DialogResult.Cancel)
                        return;
                }
            }
            else
            {
                ps = new PrinterSettings();
                ps.MinimumPage = ps.FromPage = 1;
                ps.MaximumPage = ps.ToPage = PageCount;
            }

            var page = (PageSettings)doc.DefaultPageSettings.Clone();
            doc.PrinterSettings = ps;
            doc.DefaultPageSettings = page;

            var first = ps.MinimumPage - 1;
            var last = ps.MaximumPage - 1;
            switch (ps.PrintRange)
            {
                case PrintRange.AllPages:
                    Document.Print();
                    return;

                case PrintRange.CurrentPage:
                    first = last = StartPage;
                    break;

                case PrintRange.Selection:
                    first = last = StartPage;
                    if (ZoomMode == ZoomMode.TwoPages)
                    {
                        last = Math.Min(first + 1, PageCount - 1);
                    }
                    break;

                case PrintRange.SomePages:
                    first = ps.FromPage - 1;
                    last = ps.ToPage - 1;
                    break;
            }

            // print using helper class
            var dp = new DocumentPrinter(this, first, last);
            dp.Print();
        }

        /// <summary>
        /// Regenerates the preview to reflect changes in the document layout.
        /// </summary>
        public void RefreshPreview()
        {
            // render into PrintController
            if (doc != null)
            {
                // prepare to render preview document
                pageImages.Clear();
                var savePc = doc.PrintController;

                // render preview document
                try
                {
                    cancel = false;
                    IsRendering = true;
                    doc.PrintController = new PreviewPrintController();
                    doc.PrintPage += _doc_PrintPage;
                    doc.EndPrint += _doc_EndPrint;
                    doc.Print();
                }
                finally
                {
                    cancel = false;
                    IsRendering = false;
                    doc.PrintPage -= _doc_PrintPage;
                    doc.EndPrint -= _doc_EndPrint;
                    doc.PrintController = savePc;
                }
            }

            // update
            OnPageCountChanged();
            UpdatePreview();
            UpdateScrollBars();
        }

        /// <summary>
        /// Stops rendering the <see cref="Document"/>.
        /// </summary>
        public void Cancel()
        {
            cancel = true;
        }

        #endregion Methods

        #region Events

        /// <summary>
        /// Occurs when the value of the <see cref="StartPage"/> property changes.
        /// </summary>
        public event EventHandler<EventArgs> StartPageChanged;

        /// <summary>
        /// Raises the <see cref="StartPageChanged"/> event.
        /// </summary>
        private void OnStartPageChanged()
        {
            if (StartPageChanged == null) return;
            if (!(StartPageChanged.Target is ISynchronizeInvoke synchronizeInvoke))
                StartPageChanged.DynamicInvoke(this, EventArgs.Empty);
            else
                synchronizeInvoke.Invoke(StartPageChanged, new object[] { this, EventArgs.Empty });
        }

        /// <summary>
        /// Occurs when the value of the <see cref="PageCount"/> property changes.
        /// </summary>
        public event EventHandler<EventArgs> PageCountChanged;

        /// <summary>
        /// Raises the <see cref="PageCountChanged"/> event.
        /// </summary>
        private void OnPageCountChanged()
        {
            if (PageCountChanged == null) return;
            if (!(PageCountChanged.Target is ISynchronizeInvoke synchronizeInvoke))
                PageCountChanged.DynamicInvoke(this, EventArgs.Empty);
            else
                synchronizeInvoke.Invoke(PageCountChanged, new object[] { this, EventArgs.Empty });
        }

        /// <summary>
        /// Occurs when the value of the <see cref="ZoomMode"/> property changes.
        /// </summary>
        public event EventHandler<EventArgs> ZoomModeChanged;

        /// <summary>
        /// Raises the <see cref="ZoomModeChanged"/> event.
        /// </summary>
        private void OnZoomModeChanged()
        {
            if (ZoomModeChanged == null) return;
            if (!(ZoomModeChanged.Target is ISynchronizeInvoke synchronizeInvoke))
                ZoomModeChanged.DynamicInvoke(this, EventArgs.Empty);
            else
                synchronizeInvoke.Invoke(ZoomModeChanged, new object[] { this, EventArgs.Empty });
        }

        #endregion Events

        #region Overrides

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // we're painting it all, so don't call base class
            //base.OnPaintBackground(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var img = GetImage(StartPage);
            if (img != null)
            {
                var rc = GetImageRectangle(img);
                if (rc.Width > 2 && rc.Height > 2)
                {
                    // adjust for scrollbars
                    rc.Offset(AutoScrollPosition);

                    // render single page
                    if (zoomMode != ZoomMode.TwoPages)
                    {
                        RenderPage(e.Graphics, img, rc);
                    }
                    else // render two pages
                    {
                        // render first page
                        rc.Width = (rc.Width - MARGIN) / 2;
                        RenderPage(e.Graphics, img, rc);

                        // render second page
                        img = GetImage(StartPage + 1);
                        if (img != null)
                        {
                            // update bounds in case orientation changed
                            rc = GetImageRectangle(img);
                            rc.Width = (rc.Width - MARGIN) / 2;

                            // render second page
                            rc.Offset(rc.Width + MARGIN, 0);
                            RenderPage(e.Graphics, img, rc);
                        }
                    }
                }
            }

            // paint background
            e.Graphics.FillRectangle(backBrush, ClientRectangle);
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            UpdateScrollBars();
            base.OnSizeChanged(e);
        }

        // pan by dragging preview pane
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left ||
                AutoScrollMinSize == Size.Empty)
                return;

            Cursor = Cursors.NoMove2D;
            ptLast = new Point(e.X, e.Y);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left && Cursor == Cursors.NoMove2D)
                Cursor = Cursors.Default;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (Cursor != Cursors.NoMove2D)
                return;

            var dx = e.X - ptLast.X;
            var dy = e.Y - ptLast.Y;
            if (dx == 0 && dy == 0)
                return;

            var pt = AutoScrollPosition;
            AutoScrollPosition = new Point(-(pt.X + dx), -(pt.Y + dy));
            ptLast = new Point(e.X, e.Y);
        }

        // zoom in/out using control+wheel
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if ((ModifierKeys & Keys.Control) != 0)
            {
                // calculate event position in document percentage units
                var asp = AutoScrollPosition;
                var sms = AutoScrollMinSize;
                var ptMouse = e.Location;
                var ptDoc = new PointF(
                    sms.Width > 0 ? (ptMouse.X - asp.X) / (float)sms.Width : 0,
                    sms.Height > 0 ? (ptMouse.Y - asp.Y) / (float)sms.Height : 0
                );

                // update the zoom
                var valor = Zoom * (e.Delta > 0 ? 1.1 : 0.9);
                Zoom = Math.Min(5, Math.Max(.1, valor));

                // restore position in document percentage units
                sms = AutoScrollMinSize;
                AutoScrollPosition = new Point(
                    (int)(ptDoc.X * sms.Width - ptMouse.X),
                    (int)(ptDoc.Y * sms.Height - ptMouse.Y));
            }
            else
            {
                // allow base class to scroll only if not zooming
                base.OnMouseWheel(e);
            }
        }

        // keyboard support
        protected override bool IsInputKey(Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Left:
                case Keys.Up:
                case Keys.Right:
                case Keys.Down:
                case Keys.PageUp:
                case Keys.PageDown:
                case Keys.Home:
                case Keys.End:
                    return true;
            }
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Handled)
                return;

            switch (e.KeyCode)
            {
                // arrow keys scroll or browse, depending on ZoomMode
                case Keys.Left:
                case Keys.Up:
                case Keys.Right:
                case Keys.Down:

                    // browse
                    if (ZoomMode == ZoomMode.FullPage || ZoomMode == ZoomMode.TwoPages)
                    {
                        switch (e.KeyCode)
                        {
                            case Keys.Left:
                            case Keys.Up:
                                StartPage--;
                                break;

                            case Keys.Right:
                            case Keys.Down:
                                StartPage++;
                                break;
                        }
                        break;
                    }

                    // scroll
                    var pt = AutoScrollPosition;
                    switch (e.KeyCode)
                    {
                        case Keys.Left: pt.X += 20; break;
                        case Keys.Right: pt.X -= 20; break;
                        case Keys.Up: pt.Y += 20; break;
                        case Keys.Down: pt.Y -= 20; break;
                    }
                    AutoScrollPosition = new Point(-pt.X, -pt.Y);
                    break;

                // page up/down browse pages
                case Keys.PageUp:
                    StartPage--;
                    break;

                case Keys.PageDown:
                    StartPage++;
                    break;

                // home/end
                case Keys.Home:
                    AutoScrollPosition = Point.Empty;
                    StartPage = 0;
                    break;

                case Keys.End:
                    AutoScrollPosition = Point.Empty;
                    StartPage = PageCount - 1;
                    break;

                default:
                    return;
            }

            // if we got here, the event was handled
            e.Handled = true;
        }

        #endregion Overrides

        #region Implementation

        private void _doc_PrintPage(object sender, PrintPageEventArgs e)
        {
            SyncPageImages(false);
            if (cancel)
            {
                e.Cancel = true;
            }
        }

        private void _doc_EndPrint(object sender, PrintEventArgs e)
        {
            SyncPageImages(true);
        }

        private void SyncPageImages(bool lastPageReady)
        {
            var pv = (PreviewPrintController)doc.PrintController;
            if (pv == null) return;

            var pageInfo = pv.GetPreviewPageInfo();
            var count = lastPageReady ? pageInfo.Length : pageInfo.Length - 1;
            for (var i = pageImages.Count; i < count; i++)
            {
                var img = pageInfo[i].Image;
                pageImages.Add(img);

                OnPageCountChanged();

                if (StartPage < 0) StartPage = 0;
                if (i == StartPage || i == StartPage + 1)
                {
                    Refresh();
                }
                Application.DoEvents();
            }
        }

        private Image GetImage(int page)
        {
            return page > -1 && page < PageCount ? pageImages[page] : null;
        }

        private Rectangle GetImageRectangle(Image img)
        {
            // start with regular image rectangle
            var sz = GetImageSizeInPixels(img);
            var rc = new Rectangle(0, 0, sz.Width, sz.Height);

            // calculate zoom
            var rcCli = this.ClientRectangle;
            switch (zoomMode)
            {
                case ZoomMode.ActualSize:
                    zoom = 1;
                    break;

                case ZoomMode.TwoPages:
                    rc.Width *= 2; // << two pages side-by-side
                    goto case ZoomMode.FullPage;

                case ZoomMode.FullPage:
                    var zoomX = (rc.Width > 0) ? rcCli.Width / (double)rc.Width : 0;
                    var zoomY = (rc.Height > 0) ? rcCli.Height / (double)rc.Height : 0;
                    zoom = Math.Min(zoomX, zoomY);
                    break;

                case ZoomMode.PageWidth:
                    zoom = (rc.Width > 0) ? rcCli.Width / (double)rc.Width : 0;
                    break;
            }

            // apply zoom factor
            rc.Width = (int)(rc.Width * zoom);
            rc.Height = (int)(rc.Height * zoom);

            // center image
            var dx = (rcCli.Width - rc.Width) / 2;
            if (dx > 0) rc.X += dx;
            var dy = (rcCli.Height - rc.Height) / 2;
            if (dy > 0) rc.Y += dy;

            // add some extra space
            rc.Inflate(-MARGIN, -MARGIN);
            if (zoomMode == ZoomMode.TwoPages)
            {
                rc.Inflate(-MARGIN / 2, 0);
            }

            // done
            return rc;
        }

        private Size GetImageSizeInPixels(Image img)
        {
            // get image size
            var szf = img.PhysicalDimension;

            // if it is a metafile, convert to pixels
            if (img is Metafile)
            {
                // get screen resolution
                if (himm2Pix.X < 0)
                {
                    using (var g = CreateGraphics())
                    {
                        himm2Pix.X = g.DpiX / 2540f;
                        himm2Pix.Y = g.DpiY / 2540f;
                    }
                }

                // convert himetric to pixels
                szf.Width *= himm2Pix.X;
                szf.Height *= himm2Pix.Y;
            }

            // done
            return Size.Truncate(szf);
        }

        private static void RenderPage(Graphics g, Image img, Rectangle rc)
        {
            // draw the page
            rc.Offset(1, 1);
            g.DrawRectangle(Pens.Black, rc);
            rc.Offset(-1, -1);
            g.FillRectangle(Brushes.White, rc);
            g.DrawImage(img, rc);
            g.DrawRectangle(Pens.Black, rc);

            // exclude cliprect to paint background later
            rc.Width++;
            rc.Height++;
            g.ExcludeClip(rc);
            rc.Offset(1, 1);
            g.ExcludeClip(rc);
        }

        private void UpdateScrollBars()
        {
            // get image rectangle to adjust scroll size
            var rc = Rectangle.Empty;
            var img = GetImage(StartPage);
            if (img != null)
            {
                rc = GetImageRectangle(img);
            }

            // calculate new scroll size
            var scrollSize = new Size(0, 0);
            switch (zoomMode)
            {
                case ZoomMode.PageWidth:
                    scrollSize = new Size(0, rc.Height + 2 * MARGIN);
                    break;

                case ZoomMode.ActualSize:
                case ZoomMode.Custom:
                    scrollSize = new Size(rc.Width + 2 * MARGIN, rc.Height + 2 * MARGIN);
                    break;
            }

            // apply if needed
            if (scrollSize != AutoScrollMinSize)
                AutoScrollMinSize = scrollSize;

            // ready to update
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            // validate current page
            if (startPage < 0) startPage = 0;
            if (startPage > PageCount - 1) startPage = PageCount - 1;

            // repaint
            Invalidate();
        }

        #endregion Implementation

        #region Nested Class

        // helper class that prints the selected page range in a PrintDocument.
        private class DocumentPrinter : PrintDocument
        {
            private readonly int first;
            private readonly int last;
            private int index;
            private readonly List<Image> imgList;

            public DocumentPrinter(FRPreview preview, int first, int last)
            {
                // save page range and image list
                this.first = first;
                this.last = last;
                imgList = preview.pageImages;

                // copy page and printer settings from original document
                DefaultPageSettings = preview.Document.DefaultPageSettings;
                PrinterSettings = preview.Document.PrinterSettings;
            }

            protected override void OnBeginPrint(PrintEventArgs e)
            {
                // start from the first page
                index = first;
            }

            protected override void OnPrintPage(PrintPageEventArgs e)
            {
                // render the current page and increment the index
                e.Graphics.PageUnit = GraphicsUnit.Display;
                e.Graphics.DrawImage(imgList[index++], e.PageBounds);

                // stop when we reach the last page in the range
                e.HasMorePages = index <= last;
            }
        }

        #endregion Nested Class
    }
}