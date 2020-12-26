using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Text;
using System.Windows.Forms;
namespace IISLog
{
    public partial class Form1 : Form
    {
        IISLog mLog = null;
        public Form1()
        {
            InitializeComponent();
            listBox1.DoubleClick += ListBox1_DoubleClick;
            label2.Text = "";
            comboBox1.TextChanged += ComboBox1_TextChanged;

            Type tmpStatus = typeof(HttpStatusCode);
            comboBox2.Items.Add("");
            foreach (var s in tmpStatus.GetEnumNames())
                comboBox2.Items.Add($"{(int)Enum.Parse(tmpStatus, s)} - {s}");

            comboBox2.Text = "200 - OK";
            comboBox2.DropDownStyle = ComboBoxStyle.DropDownList;
            dataGridView1.CellMouseDoubleClick += DataGridView1_CellMouseDoubleClick;
        }
        bool mAgOnly = false;
        string mCurSelGuid = "";
        private void DataGridView1_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex > 0)
            {
                mAgOnly = true;
                mCurSelGuid = Convert.ToString(dataGridView1["guid", e.RowIndex].Value);
                comboBox1.Text = textBox2.Text;
                button2_Click(this, EventArgs.Empty);
                mCurSelGuid = "";
                mAgOnly = false;
            }
        }

        private void ComboBox1_TextChanged(object sender, EventArgs e)
        {
            if (mLog != null && mLog.LogDetail != null && !mAgOnly)
            {
                if (comboBox1.Text.Length > 0)
                {
                    mLog.LogDetail.DefaultView.RowFilter = $"[cs-uri-stem]='{comboBox1.Text}'";
                    dataGridView1.DataSource = mLog.LogDetail.DefaultView;
                }
                else
                {
                    mLog.LogDetail.DefaultView.RowFilter ="";
                    dataGridView1.DataSource = mLog.LogDetail.DefaultView;

                }
            }
        }

        private void ListBox1_DoubleClick(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem != null)
            {
                LogFileInfo tmpLogFile = (LogFileInfo)listBox1.SelectedItem;
                richTextBox1.Text = File.ReadAllText(tmpLogFile.Path);

                mLog = new IISLog(tmpLogFile.Path);
                label2.Text = $"版本：{mLog.Version}\t日期：{mLog.Date}";
                mLog.Read();
                mLog.Requests.Sort();
                comboBox1.Items.AddRange(mLog.Requests.ToArray());
                dataGridView1.DataSource = mLog.LogDetail;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog tmpFbd = new FolderBrowserDialog();
            tmpFbd.SelectedPath = label1.Text;
            if (tmpFbd.ShowDialog() == DialogResult.OK)
            {
                LoadFiles(tmpFbd.SelectedPath);
            }
        }
        #region 
        void LoadFiles(string pPath)
        {
            label1.Text = pPath;
            string[] tmpFiles = Directory.GetFiles(pPath);
            listBox1.Items.Clear();
            for (int i = 0; i < tmpFiles.Length; i++)
            {
                listBox1.Items.Add(new LogFileInfo() { Name = Path.GetFileName(tmpFiles[i]), Path = tmpFiles[i] });
            }
        }
        class LogFileInfo
        {
            public string Name { get; set; }
            public string Path { get; set; }
            public override string ToString()
            {
                return Name;
            }

        }
        #endregion

        private void dataGridView1_RowEnter(object sender, DataGridViewCellEventArgs e)
        {
            if (mLog != null && mLog.LogDetail != null)
            {
                if (mLog.LogDetail.Columns.Contains("cs-uri-stem"))
                {
                    textBox2.Text = Convert.ToString(dataGridView1["cs-uri-stem", e.RowIndex].Value);
                    if (textBox2.Text == "-")
                        textBox2.Text = "";

                }
                if (mLog.LogDetail.Columns.Contains("cs-uri-query"))
                {
                    textBox3.Text = Convert.ToString(dataGridView1["cs-uri-query", e.RowIndex].Value);
                    if (textBox3.Text == "-")
                        textBox3.Text = "";
                }
                if (mLog.LogDetail.Columns.Contains("date") &&
                    mLog.LogDetail.Columns.Contains("time"))
                {
                    string tmpDt = $"{dataGridView1["date", e.RowIndex].Value} {dataGridView1["time", e.RowIndex].Value}";
                    DateTime tmpDate;
                    if (DateTime.TryParse(tmpDt, out tmpDate))
                    {
                        label6.Text = string.Format("时间：{0:MM-dd HH:mm:ss}", (tmpDate + TimeSpan.FromHours(Convert.ToDouble(textBox1.Text))));
                    }
                }
                if (mLog.LogDetail.Columns.Contains("s-port") &&
                    mLog.LogDetail.Columns.Contains("s-ip"))
                {
                    textBox4.Text = $"http://{dataGridView1["s-ip", e.RowIndex].Value}:{dataGridView1["s-port", e.RowIndex].Value}{textBox2.Text}{(textBox3.Text.Length>0?"?":"")}{textBox3.Text}";
                }
            }
        }
        DataTable mPageDetail;
        private void button2_Click(object sender, EventArgs e)
        {
            mPageDetail = new DataTable();
            mPageDetail.Columns.Add("guid", typeof(string));
            mPageDetail.Columns.Add("datetime", typeof(string));
            mPageDetail.Columns.Add("url", typeof(string));
            if (mLog != null && mLog.LogDetail != null && comboBox1.Text.Length>0)
            {
                DataRow[] tmpRows = mLog.LogDetail.Select($"[cs-uri-stem]='{comboBox1.Text}'");
                for (int i = 0; i < tmpRows.Length; i++)
                {
                    List<string> tmpValues = new List<string>();
                    string tmpParams = Convert.ToString(tmpRows[i]["cs-uri-query"]);
                    if (tmpParams.Length > 0)
                    {
                        string[] tmpParam = tmpParams.Split(new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
                        for (int j = 0; j < tmpParam.Length; j++)
                        {
                            if (tmpParam[j].IndexOf("=") > -1)
                            {
                                string tmpField = tmpParam[j].Split(new char[] { '=' })[0];
                                string tmpValue = tmpParam[j].Split(new char[] { '=' })[1];
                                if (!mPageDetail.Columns.Contains(tmpField))
                                    mPageDetail.Columns.Add(tmpField, typeof(string));
                                //tmpValues.Add(tmpValue);
                            }
                        }
                        DataRow tmpAppRow = mPageDetail.NewRow();
                        tmpAppRow["guid"] = Convert.ToString(tmpRows[i]["guid"]);
                        tmpAppRow["datetime"] = Convert.ToString(tmpRows[i]["datetime"]);
                        tmpAppRow["url"] = ($"http://{tmpRows[i]["s-ip"]}:{tmpRows[i]["s-port"]}{tmpRows[i]["cs-uri-stem"]}{(Convert.ToString(tmpRows[i]["cs-uri-query"]) != "-" ? "?" : "")}{tmpRows[i]["cs-uri-query"]}");

                        for (int j = 0; j < tmpParam.Length; j++)
                        {
                            if (tmpParam[j].IndexOf("=") > -1)
                            {
                                string tmpField = tmpParam[j].Split(new char[] { '=' })[0];
                                string tmpValue = tmpParam[j].Split(new char[] { '=' })[1];

                                if (mPageDetail.Columns.Contains(tmpField))
                                    tmpAppRow[tmpField] = tmpValue;
                            }
                        }
                        //mPageDetail.Rows.Add(tmpValues.ToArray());
                        mPageDetail.Rows.Add(tmpAppRow);
                    }
                }
            }
            
            dataGridView2.DataSource = mPageDetail;
            if(mCurSelGuid.Length>0)
            {
                mPageDetail.DefaultView.Sort = "guid";
                int tmpCurRow = mPageDetail.DefaultView.Find(mCurSelGuid);
                if (tmpCurRow > -1)
                {
                    dataGridView2.FirstDisplayedScrollingRowIndex = tmpCurRow;
                    dataGridView2.Rows[tmpCurRow].Selected = true;
                    //mPageDetail.DefaultView.Sort = "date,time";
                }
            }
            tabControl2.SelectedIndex = 1;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (mLog != null && mLog.LogDetail != null)
            {
                string tmpBDate = "";
                string tmpEDate = "";
                string tmpStatus = "";
                if (dateTimePicker1.Checked)
                    tmpBDate = string.Format("{0:yyyy-MM-dd HH:mm:ss}", dateTimePicker1.Value);
                else
                    tmpBDate = string.Format("{0:yyyy-MM-dd HH:mm:ss}", DateTime.MinValue);
                if (dateTimePicker2.Checked)
                    tmpEDate = string.Format("{0:yyyy-MM-dd HH:mm:ss}", dateTimePicker2.Value);
                else
                    tmpEDate = string.Format("{0:yyyy-MM-dd HH:mm:ss}", DateTime.MaxValue);
                if (comboBox2.Text.Length > 0)
                    tmpStatus = comboBox2.Text.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries)[0].Trim();


                mLog.LogDetail.DefaultView.RowFilter = $"datetime >='{tmpBDate}' AND datetime <='{tmpEDate}' AND ([sc-status]='{tmpStatus}' OR '{tmpStatus}'='')";
                dataGridView1.DataSource = mLog.LogDetail.DefaultView;

            }
        }
    }
    public class IISLog
    {
        public List<string> Requests;
        public string Version = "";
        public string Date = "";
        public string FieldList = "";
        public string LogFile = "";
        public DataTable LogDetail = null;
        public IISLog(string pFile)
        {
            LogFile = pFile;
            GetInfo();
            CreateLogDB();
            
        }
        public void Read()
        {
            if (FieldList.Length > 0)
                AppendDetail();
        }
        void GetInfo()
        {
            using (FileStream tmpFs = new FileStream(LogFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (TextReader tmpReader = new StreamReader(tmpFs,Encoding.UTF8))
            {
                string tmpLine = null;
                while ((tmpLine = tmpReader.ReadLine()) != null)
                {
                    if (tmpLine.ToLower().StartsWith("#software:"))
                        Version = tmpLine.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries)[1];
                    if (tmpLine.ToLower().StartsWith("#date:"))
                        Date = tmpLine.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries)[1];
                    if (tmpLine.ToLower().StartsWith("#fields:"))
                        FieldList = tmpLine.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries)[1];
                    if (tmpLine.Length > 0 && !tmpLine.StartsWith("#"))
                        break;
                }
            }
        }
        void CreateLogDB()
        {
            LogDetail = new DataTable();
            LogDetail.Columns.Add("guid", typeof(string));
            if (FieldList.Length > 0)
            {
                string[] tmpFields = FieldList.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < tmpFields.Length; i++)
                {
                    if (!LogDetail.Columns.Contains(tmpFields[i]))
                        LogDetail.Columns.Add(tmpFields[i], typeof(string));
                }
            }
            LogDetail.Columns.Add("datetime", typeof(DateTime));
            LogDetail.RowChanged += LogDetail_RowChanged;
        }

        private void LogDetail_RowChanged(object sender, DataRowChangeEventArgs e)
        {
            switch (e.Action)
            {
                case DataRowAction.Nothing:
                    break;
                case DataRowAction.Delete:
                    break;
                case DataRowAction.Change:
                    break;
                case DataRowAction.Rollback:
                    break;
                case DataRowAction.Commit:
                    break;
                case DataRowAction.Add:
                    if(e.Row["datetime"] is null || e.Row["datetime"] .GetType()==typeof(DBNull))
                    {
                        string tmpDT = string.Format("{0} {1}", e.Row["date"], e.Row["time"]);
                        DateTime tmpRst;
                        if (DateTime.TryParse(tmpDT, out tmpRst))
                            e.Row["datetime"] = tmpRst;
                    }
                    break;
                case DataRowAction.ChangeOriginal:
                    break;
                case DataRowAction.ChangeCurrentAndOriginal:
                    break;
                default:
                    break;
            }   
        }

        void AppendDetail()
        {
            int r = 0;
            Requests = new List<string>();
            using (TextReader tmpReader = File.OpenText(LogFile))
            {
                string tmpLine = null;
                while ((tmpLine = tmpReader.ReadLine()) != null)
                {
                    if (!string.IsNullOrEmpty(tmpLine) &&
                        !tmpLine.StartsWith("#"))
                    {
                        string[] tmpRecord = (string.Format("{0:0000000000}",++r) + " " + tmpLine).Split(new char[] { ' ' });
                        DataRow tmpRow = LogDetail.Rows.Add(tmpRecord);
                        if (tmpRow.Table.Columns.Contains("cs-uri-stem"))
                        {
                            string tmpUri = Convert.ToString(tmpRow["cs-uri-stem"]);
                            if (!Requests.Contains(tmpUri))
                                Requests.Add(tmpUri);
                        }
                    }
                }
            }
        }
    }
}