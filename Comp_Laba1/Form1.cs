using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Comp_Laba1
{
    public partial class Form1 : Form
    {
        List<DocumentTab> documents = new List<DocumentTab>();
        DocumentTab currentDocument;
        int nextNewFileNumber = 2;
        private RichTextBoxEditOperations editOps;
        private LineNumberManager lineNumberManager;
        List<ScanToken> result_lecs;
        List<SyntaxError> result_parser;
        SemanticResult semanticResult;


        public Form1()
        {
            InitializeComponent();
            editOps = new RichTextBoxEditOperations(richTextBox1);
            documents.Add(new DocumentTab("File1"));
            currentDocument = documents[0];
            UpdateOpenFilesMenu();
            this.FormClosing += Form1_FormClosing;
            TextSizeManager.TextSizeChanged += (s, e) => UpdateTextSize();
            UpdateTextSize();
            SetupLineNumbering();
            this.AllowDrop = true;
            this.DragEnter += Form1_DragEnter;
            this.DragDrop += Form1_DragDrop;
            dataGridView1.ScrollBars = ScrollBars.Vertical;
            dataGridView1.CellClick += dataGridView1_CellClick;

        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            DialogResult result = MessageBox.Show(
                "Вы действительно хотите закрыть программу?\n Точно все сохранили?",
                "Подтверждение закрытия",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.No)
            {
                e.Cancel = true;
            }
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            if (files != null && files.Length > 0)
            {
                foreach (string filePath in files)
                {
                    OpenDroppedFile(filePath);
                }
            }
        }

        private void OpenDroppedFile(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLower();

            if (extension == ".txt" || extension == ".rtf")
            {
                try
                {
                    string fileName = Path.GetFileName(filePath);
                    string fileContent = File.ReadAllText(filePath, Encoding.UTF8);
                    var newDoc = new DocumentTab(fileName, filePath);
                    newDoc.TextContent = fileContent;
                    newDoc.IsModified = false;

                    documents.Add(newDoc);
                    SwitchToDocument(newDoc);
                    UpdateOpenFilesMenu();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при открытии файла {filePath}:\n{ex.Message}",
                                    "Ошибка",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show($"Файл {Path.GetFileName(filePath)} не является текстовым файлом.",
                                "Неподдерживаемый формат",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
            }
        }

        private void SetupLineNumbering()
        {
            richTextBox1.WordWrap = true; 

            richTextBox2.ReadOnly = true;
            richTextBox2.BackColor = Color.LightGray;
            richTextBox2.WordWrap = false;

            lineNumberManager = new LineNumberManager(richTextBox1, richTextBox2);
            this.Resize += (s, e) => AdjustLineNumberWidth();
        }

        private void AdjustLineNumberWidth()
        {
            if (lineNumberManager != null)
            {
                lineNumberManager.AdjustWidth();
            }
        }

        private void UpdateTextSize()
        {
            richTextBox1.Font = new Font(richTextBox1.Font.FontFamily,
                                          TextSizeManager.CurrentSize,
                                          richTextBox1.Font.Style);

            dataGridView1.Font = new Font(dataGridView1.Font.FontFamily,
                                           TextSizeManager.CurrentSize,
                                           dataGridView1.Font.Style);

            dataGridView1.ColumnHeadersDefaultCellStyle.Font =
                new Font(dataGridView1.Font.FontFamily,
                         TextSizeManager.CurrentSize,
                         FontStyle.Bold);

         
        }


        private void CreateNewDocument()
        {
            string fileName = $"File{nextNewFileNumber++}";
            var newDoc = new DocumentTab(fileName);
            documents.Add(newDoc);
            if (currentDocument == null)
            {
                SwitchToDocument(newDoc);

                UpdateOpenFilesMenu();
            }
            UpdateOpenFilesMenu();
        }

        private void UpdateOpenFilesMenu()
        {
            menuStrip3.Items.Clear();
            if (documents.Count == 0)
            {
                var emptyItem = new ToolStripMenuItem("(нет файлов)");
                emptyItem.Enabled = false;
                menuStrip3.Items.Add(emptyItem);
                return;
            }
            foreach (var doc in documents)
            {

                var item = new ToolStripMenuItem(doc.FileName);
                item.Tag = doc;

                item.Click += (s, e) => {
                    var clickedDoc = (s as ToolStripMenuItem)?.Tag as DocumentTab;
                    if (clickedDoc != null && clickedDoc != currentDocument)
                    {
                        SwitchToDocument(clickedDoc);
                    }
                };
                menuStrip3.Items.Add(item);
            }
            HighlightCurrentDocument();
        }




        private void SwitchToDocument(DocumentTab doc)
        {
            if (doc == null) return;
            dataGridView1.Rows.Clear();
            if(result_lecs!=null)
            { result_lecs.Clear(); }
            if (result_parser != null) {  result_parser.Clear(); }
            if (currentDocument != null)
            {
                currentDocument.TextContent = richTextBox1.Text;
            }

            currentDocument = doc;
            richTextBox1.Text = doc.TextContent;

            HighlightCurrentDocument();
        }

        private void HighlightCurrentDocument()
        {
            foreach (ToolStripMenuItem item in menuStrip3.Items)
            {
                var doc = item.Tag as DocumentTab;

                if (doc != null)
                {
                    if (doc == currentDocument)
                    {
                        item.Font = new Font(item.Font, FontStyle.Bold);
                        item.ForeColor = Color.Red;
                    }
                    else
                    {
                        item.Font = new Font(item.Font, FontStyle.Regular);
                        item.ForeColor = Color.Black;
                    }
                }
            }
        }

        private void CloseCurrentDocument()
        {

            DialogResult result = MessageBox.Show($"Закрыть документ \"{currentDocument.FileName}\" без сохраниния?",
                                                   "Подтверждение",
                                                   MessageBoxButtons.YesNo,
                                                   MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                int index = documents.IndexOf(currentDocument);
                documents.Remove(currentDocument);

                int newIndex = (index >= documents.Count) ? documents.Count - 1 : index;
                SwitchToDocument(documents[newIndex]);
                UpdateOpenFilesMenu();
            }
        }

        private void OpenTextFile()
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {

                openFileDialog.Title = "Открыть текстовый файл";
                openFileDialog.Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*";
                openFileDialog.FilterIndex = 1;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string filePath = openFileDialog.FileName;
                    string fileName = Path.GetFileName(filePath);

                    try
                    {
                        string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

                        var newDoc = new DocumentTab(fileName);
                        newDoc.TextContent = fileContent;
                        newDoc.FilePath = filePath;

                        documents.Add(newDoc);
                        SwitchToDocument(newDoc);
                        UpdateOpenFilesMenu();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при открытии файла: {ex.Message}",
                                        "Ошибка",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Error);
                    }
                }
            }
        }



        private void SaveFile()
        {
            if (currentDocument == null) return;
            if (string.IsNullOrEmpty(currentDocument.FilePath))
            {
                SaveFileAs();
            }
            else
            {
                try
                {
                    File.WriteAllText(currentDocument.FilePath, currentDocument.TextContent, Encoding.UTF8);
                    currentDocument.IsModified = false;

                    UpdateOpenFilesMenu();

                    this.Text = $"Редактор - {currentDocument.FileName}";

                    MessageBox.Show("Файл успешно сохранён",
                                   "Сохранение",
                                   MessageBoxButtons.OK,
                                   MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при сохранении файла: {ex.Message}",
                                   "Ошибка",
                                   MessageBoxButtons.OK,
                                   MessageBoxIcon.Error);
                }
            }
        }

        private void SaveFileAs()
        {
            if (currentDocument == null) return;

            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                string defaultPath = @"D:\Универ\6 семестр\Компиляторы\Comp_Laba1";
                if (Directory.Exists(defaultPath))
                {
                    saveFileDialog.InitialDirectory = defaultPath;
                }

                saveFileDialog.Title = "Сохранить файл как...";
                saveFileDialog.Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*";
                saveFileDialog.FilterIndex = 1;
                saveFileDialog.RestoreDirectory = true;

                if (!string.IsNullOrEmpty(currentDocument.FileName))
                {
                    saveFileDialog.FileName = currentDocument.FileName;
                }
                else
                {
                    saveFileDialog.FileName = "Новый документ.txt";
                }

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string filePath = saveFileDialog.FileName;
                        string fileName = Path.GetFileName(filePath);

                        File.WriteAllText(filePath, currentDocument.TextContent, Encoding.UTF8);

                        currentDocument.FilePath = filePath;
                        currentDocument.FileName = fileName;
                        currentDocument.IsModified = false;

                        UpdateOpenFilesMenu();

                        this.Text = $"Редактор - {fileName}";

                        MessageBox.Show($"Файл сохранён как:\n{filePath}",
                                       "Сохранение",
                                       MessageBoxButtons.OK,
                                       MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при сохранении файла: {ex.Message}",
                                       "Ошибка",
                                       MessageBoxButtons.OK,
                                       MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void аToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void справкаToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void иконочкиToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void пускToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void текстToolStripMenuItem1_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            if (currentDocument != null)
            {
                currentDocument.TextContent = richTextBox1.Text;
                currentDocument.IsModified = true;
                string title = $"Редактор - {currentDocument.FileName}";
                if (currentDocument.IsModified)
                {
                    title += "*";
                }
                this.Text = title;
            }
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
           
        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            try
            {
                string place = dataGridView1.Rows[e.RowIndex].Cells["Location"].Value?.ToString();
                if (string.IsNullOrEmpty(place)) return;

                var match = System.Text.RegularExpressions.Regex.Match(place, @"\((\d+),\s*(\d+)-(\d+)\)");

                if (match.Success)
                {
                    int line = int.Parse(match.Groups[1].Value);
                    int start = int.Parse(match.Groups[2].Value);
                    int end = int.Parse(match.Groups[3].Value);

                    HighlightLine(line, start, end);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при переходе: {ex.Message}");
            }
        }


        private void HighlightLine(int lineIdx, int startCol, int endCol)
        {
            if (richTextBox1 == null || lineIdx < 0 || lineIdx >= richTextBox1.Lines.Length) return;
            int currentCaret = richTextBox1.SelectionStart;
            richTextBox1.SelectAll();
            richTextBox1.SelectionBackColor = Color.White;


            int lineStartPos = richTextBox1.GetFirstCharIndexFromLine(lineIdx);
            if (lineStartPos < 0) return;
            int selectionStart = lineStartPos + startCol;
            int selectionLength = endCol - startCol;

            if (selectionStart < 0) selectionStart = 0;
            if (selectionStart + selectionLength > richTextBox1.Text.Length)
                selectionLength = richTextBox1.Text.Length - selectionStart;

            richTextBox1.Focus();
            richTextBox1.Select(selectionStart, Math.Max(0, selectionLength));
            richTextBox1.SelectionBackColor = Color.LightCoral;


            richTextBox1.ScrollToCaret();
        }


        private void файлToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CreateNewDocument();
        }

        private void richTextBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void file1ToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void создатьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CreateNewDocument();
        }

        private void выходToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void открытьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenTextFile();
        }

        private void пToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenTextFile();
        }

        private void сохранитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFile();
        }

        private void сохранитьКакToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileAs();
        }

        private void пToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            SaveFile();
        }

        private void оПрограммеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form f = new Form2();
            f.Visible = true;
        }

        private void отменитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            editOps.Undo();
        }

        private void пToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            editOps.Undo();
        }

        private void повторитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            editOps.Redo();
        }

        private void пToolStripMenuItem3_Click(object sender, EventArgs e)
        {
            editOps.Redo();
        }

        private void вырезатьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            editOps.Cut();
        }

        private void пToolStripMenuItem5_Click(object sender, EventArgs e)
        {
            editOps.Cut();
        }

        private void пToolStripMenuItem4_Click(object sender, EventArgs e)
        {
            editOps.Copy();
        }

        private void копироватьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            editOps.Copy();
        }

        private void вставToolStripMenuItem_Click(object sender, EventArgs e)
        {
            editOps.Paste();
        }

        private void пToolStripMenuItem6_Click(object sender, EventArgs e)
        {
            editOps.Paste();
        }

        private void удалитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CloseCurrentDocument();
        }

        private void выделитьВсеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            editOps.SelectAll();
        }

        private void правкаToolStripMenuItem1_Click(object sender, EventArgs e)
        {

        }

        private void пToolStripMenuItem8_Click(object sender, EventArgs e)
        {
            string url = "https://docs.google.com/document/d/1Rzmagq5oPo1kBg_uRBE7TA7xVFDxVzWDO_y2QE76Ows/edit?usp=sharing";
            System.Diagnostics.Process.Start(url);
        }

        private void пToolStripMenuItem9_Click(object sender, EventArgs e)
        {
            Form f = new Form2();
            f.Visible = true;
        }

        private void увеличитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TextSizeManager.IncreaseSize();
        }

        private void уменьшитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TextSizeManager.DecreaseSize();
        }

        private void сброситьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TextSizeManager.ResetSize();
        }

        private void вызовСправкиToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string url = "https://docs.google.com/document/d/1Rzmagq5oPo1kBg_uRBE7TA7xVFDxVzWDO_y2QE76Ows/edit?usp=sharing";
            System.Diagnostics.Process.Start(url);
        }

        private void пToolStripMenuItem7_Click(object sender, EventArgs e)
        {
            LexicalAnalyzer scanner = new LexicalAnalyzer();
            string inputText = richTextBox1.Text;
            if (string.IsNullOrWhiteSpace(currentDocument.FilePath))
            {
                MessageBox.Show("Введите текст для анализа!", "Предупреждение",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            result_lecs = scanner.AnalyzeText(currentDocument.FilePath);
            
            

            Parser parser = new Parser(result_lecs);
            result_parser =parser.Parse();
            //ShowErrorsTable();

            AstBuilder astBuilder = new AstBuilder(result_lecs);
            semanticResult = astBuilder.Build();
            ShowAstWindow(semanticResult.AstText);
            ShowSemaTable();
        }

        public void ShowAstWindow(string astText)
        {
            System.Windows.Forms.Form popup = new System.Windows.Forms.Form();
            popup.Text = "Абстрактное синтаксическое дерево (AST)";
            popup.Width = 600;
            popup.Height = 500;
            popup.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;

            System.Windows.Forms.TextBox txt = new System.Windows.Forms.TextBox();
            txt.Multiline = true;
            txt.Dock = System.Windows.Forms.DockStyle.Fill;
            txt.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            txt.Font = new System.Drawing.Font("Consolas", 11F);
            txt.Text = astText;
            txt.ReadOnly = true;
            txt.SelectionLength = 0;

            popup.Controls.Add(txt);
            popup.ShowDialog();
        }

        public void DisplayTokens(List<ScanToken> tokens)
        {
            dataGridView1.Rows.Clear();
            var errors = tokens.Where(t => t.Type == "ERROR").ToList();
            foreach (var token in tokens)
            {
                int rowIndex = dataGridView1.Rows.Add();
                dataGridView1.Rows[rowIndex].Cells["Place"].Value = token.Place;
                dataGridView1.Rows[rowIndex].Cells["Type_lecsem"].Value = token.Type;
                dataGridView1.Rows[rowIndex].Cells["Lecsema"].Value = token.Lecsema;
                dataGridView1.Rows[rowIndex].Cells["Usl_code"].Value = token.Usl_code;
                if (token.Type == "ERROR")
                {
                    dataGridView1.Rows[rowIndex].DefaultCellStyle.BackColor = Color.LightCoral;
                }
            }

            string message = errors.Any()
                ? $"Найдено ошибок: {errors.Count}."
                : $"Ошибок не найдено. Всего токенов: {tokens.Count}";

            MessageBox.Show(message, "Результат анализа", MessageBoxButtons.OK,
                            errors.Any() ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
        }

        private void пускToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            LexicalAnalyzer scanner = new LexicalAnalyzer();
            string inputText = richTextBox1.Text;
            if (string.IsNullOrWhiteSpace(currentDocument.FilePath))
            {
                MessageBox.Show("Введите текст для анализа!", "Предупреждение",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            result_lecs = scanner.AnalyzeText(currentDocument.FilePath);



            Parser parser = new Parser(result_lecs);
            result_parser = parser.Parse();
            ShowErrorsTable();

        }

        private void лексемыToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowTokensTable();
            ClearHighlight();
        }

        private void парсерToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowErrorsTable();
            ClearHighlight();
        }

        private void ShowTokensTable()
        {
            if (result_lecs == null || result_lecs.Count == 0)
            {
                MessageBox.Show("Нет данных о лексемах. Сначала выполните анализ.",
                    "Нет данных", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            dataGridView1.Rows.Clear();
            dataGridView1.Columns.Clear();

            dataGridView1.Columns.Add("UslCode", "Код");
            dataGridView1.Columns.Add("Type", "Тип");
            dataGridView1.Columns.Add("Lecsema", "Лексема");
            dataGridView1.Columns.Add("Location", "Позиция");

            dataGridView1.Columns["UslCode"].Width = 80;
            dataGridView1.Columns["Type"].Width = 120;
            dataGridView1.Columns["Lecsema"].Width = 150;
            dataGridView1.Columns["Location"].Width = 100;

            foreach (var token in result_lecs)
            {
                int rowIndex = dataGridView1.Rows.Add(
                    token.Usl_code,
                    token.Type,
                    token.Lecsema,
                    token.Place
                );

                if (token.Type == "ERROR")
                {
                    dataGridView1.Rows[rowIndex].DefaultCellStyle.BackColor = System.Drawing.Color.LightCoral;
                    dataGridView1.Rows[rowIndex].DefaultCellStyle.ForeColor = System.Drawing.Color.DarkRed;
                }
                else if (token.Type == "error")
                {
                    dataGridView1.Rows[rowIndex].DefaultCellStyle.BackColor = System.Drawing.Color.LightCoral;
                    dataGridView1.Rows[rowIndex].DefaultCellStyle.ForeColor = System.Drawing.Color.DarkRed;
                }
                
            }

            лексемыToolStripMenuItem.BackColor = System.Drawing.Color.LightBlue;
            парсерToolStripMenuItem.BackColor = System.Drawing.Color.LightCoral;
            семантикаToolStripMenuItem.BackColor = System.Drawing.Color.LightCoral;
            dataGridView1.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
        }

        private void ShowErrorsTable()
        {
            if (result_parser == null || result_parser.Count == 0)
            {
                dataGridView1.Rows.Clear();
                dataGridView1.Columns.Clear();
                MessageBox.Show("Нет данных об ошибках. Сначала выполните анализ.",
                        "Нет данных", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            dataGridView1.Rows.Clear();
            dataGridView1.Columns.Clear();
            dataGridView1.Columns.Add("Fragment", "Неверный фрагмент");
            dataGridView1.Columns.Add("Location", "Местоположение");
            dataGridView1.Columns.Add("Description", "Описание ошибки");
            dataGridView1.Columns["Fragment"].Width = 200;
            dataGridView1.Columns["Location"].Width = 120;
            dataGridView1.Columns["Description"].Width = 200;

            foreach (var error in result_parser)
            {
                dataGridView1.Rows.Add(
                    error.Fragment,
                    error.Location,
                    error.Description
                );
            }

            парсерToolStripMenuItem.BackColor = System.Drawing.Color.LightBlue;
            лексемыToolStripMenuItem.BackColor = System.Drawing.Color.LightCoral;
            семантикаToolStripMenuItem.BackColor = System.Drawing.Color.LightCoral;
        }

        private void ShowSemaTable()
        {

            if (semanticResult.ErrorCount ==0) 
            {
                dataGridView1.Rows.Clear();
                dataGridView1.Columns.Clear();
                MessageBox.Show("Ошибок нет.",
                        "Нет данных", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }   

            dataGridView1.Rows.Clear();
            dataGridView1.Columns.Clear();
            dataGridView1.Columns.Add("Type", "Описание ошибки");
            dataGridView1.Columns.Add("Fragment", "Неверный фрагмент");
            dataGridView1.Columns.Add("Description", "Описание ошибки");
            dataGridView1.Columns["Type"].Width = 200;
            dataGridView1.Columns["Fragment"].Width = 200;
            dataGridView1.Columns["Description"].Width = 200;
            foreach (var error in semanticResult.Errors)
            {
                dataGridView1.Rows.Add(
                    error.Type,
                    error.Fragment,
                    error.Description
                );
            }
            парсерToolStripMenuItem.BackColor = System.Drawing.Color.LightBlue;
            лексемыToolStripMenuItem.BackColor = System.Drawing.Color.LightBlue;
            семантикаToolStripMenuItem.BackColor = System.Drawing.Color.LightCoral;
        }

        private void ClearHighlight()
        {
            if (richTextBox1 == null) return;

            try
            {
                richTextBox1.SelectAll();
                richTextBox1.SelectionBackColor = Color.White;
                richTextBox1.SelectionColor = Color.Black;
                richTextBox1.Select(0, 0);

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка сброса подсветки: {ex.Message}");
            }
        }

        private void справкаToolStripMenuItem1_Click(object sender, EventArgs e)
        {

        }

        private void постановкаЗадачиToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string url = "https://docs.google.com/document/d/1Rzmagq5oPo1kBg_uRBE7TA7xVFDxVzWDO_y2QE76Ows/edit?tab=t.69zdg59479kx";
            System.Diagnostics.Process.Start(url);
        }

        private void граматикаToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string url = "https://docs.google.com/document/d/1Rzmagq5oPo1kBg_uRBE7TA7xVFDxVzWDO_y2QE76Ows/edit?tab=t.1txr5vskdeqp";
            System.Diagnostics.Process.Start(url);
        }

        private void классификацияToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string url = "https://docs.google.com/document/d/1Rzmagq5oPo1kBg_uRBE7TA7xVFDxVzWDO_y2QE76Ows/edit?tab=t.h2mjo8ryi6d4";
            System.Diagnostics.Process.Start(url);
        }

        private void методАнализаToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string url = "https://docs.google.com/document/d/1Rzmagq5oPo1kBg_uRBE7TA7xVFDxVzWDO_y2QE76Ows/edit?tab=t.uk6bnvyyxolx";
            System.Diagnostics.Process.Start(url);
        }

        private void тестовыйПримерToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string url = "https://docs.google.com/document/d/1Rzmagq5oPo1kBg_uRBE7TA7xVFDxVzWDO_y2QE76Ows/edit?tab=t.kdowgfjlgr5e";
            System.Diagnostics.Process.Start(url);
        }

        private void списокЛитературыToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string url = "https://docs.google.com/document/d/1Rzmagq5oPo1kBg_uRBE7TA7xVFDxVzWDO_y2QE76Ows/edit?tab=t.tuhqu7ups3hg";
            System.Diagnostics.Process.Start(url);
        }

        private void исходныйКодПрограммыToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string url = "https://docs.google.com/document/d/1Rzmagq5oPo1kBg_uRBE7TA7xVFDxVzWDO_y2QE76Ows/edit?tab=t.sia181eg2ep3";
            System.Diagnostics.Process.Start(url);
        }

        private void семантикаToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowSemaTable();
            ClearHighlight();
        }
    }
}
