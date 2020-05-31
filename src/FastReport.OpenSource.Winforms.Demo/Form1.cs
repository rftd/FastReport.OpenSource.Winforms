using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using FastReport;
using FastReport.OpenSource.Winforms;
using FastReport.Utils;

namespace WinFormsOpenSource
{
    public partial class Form1 : Form
    {
        public string ReportsPath = Config.ApplicationFolder + @"..\..\Reports\";

        public Form1()
        {
            InitializeComponent();
            LoadReportList();
        }

        public Report Report { get; set; } = new Report();

        public void LoadReportList()
        {
            var filesname = Directory.GetFiles(ReportsPath, "*.frx").ToList();

            foreach (var file in filesname)
            {
                ReportsList.Items.Add(Path.GetFileNameWithoutExtension(file));
            }
        }

        private void ReportsList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ReportsList.SelectedItem == null) return;
            Report.Load(ReportsPath + ReportsList.SelectedItem.ToString() + ".frx"); //Загружаем шаблон отчета
            DataSet data = new DataSet(); //Создаем источник данных
            data.ReadXml(ReportsPath + "nwind.xml"); //Загружаем в источник данных базу
            Report.RegisterData(data, "NorthWind"); //Регистрируем источник данных в отчете
            Report.Prepare(); //Выполняем предварительное построение отчета
            Report.Preview(frPrintPreviewControl1);
        }
    }
}