using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Comp_Laba1
{
    public partial class Form3 : Form
    {
        private TextBox txtAst;
        private Label lblTitle;
        private Button btnCopy;
        private Button btnClose;
        private Label lblErrorCount;

        public Form3()
        {
            InitializeComponent();
            InitializeCustomComponents();
        }

        private void InitializeCustomComponents()
        {
            this.lblTitle = new Label();
            this.lblTitle.Text = "AST и результаты семантического анализа";
            this.lblTitle.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            this.lblTitle.Location = new Point(12, 9);
            this.lblTitle.Size = new Size(560, 30);
            this.lblTitle.TextAlign = ContentAlignment.MiddleLeft;

            this.txtAst = new TextBox();
            this.txtAst.Font = new Font("Consolas", 10F);
            this.txtAst.Location = new Point(12, 55);
            this.txtAst.Multiline = true;
            this.txtAst.ReadOnly = true;
            this.txtAst.ScrollBars = ScrollBars.Both;
            this.txtAst.Size = new Size(760, 450);
            this.txtAst.WordWrap = false;

            this.lblErrorCount = new Label();
            this.lblErrorCount.Font = new Font("Segoe UI", 10F);
            this.lblErrorCount.Location = new Point(12, 515);
            this.lblErrorCount.Size = new Size(500, 25);

            this.btnCopy = new Button();
            this.btnCopy.Text = "Копировать в буфер обмена";
            this.btnCopy.Location = new Point(12, 550);
            this.btnCopy.Size = new Size(200, 35);
            this.btnCopy.Click += BtnCopy_Click;

            this.btnClose = new Button();
            this.btnClose.Text = "Закрыть";
            this.btnClose.Location = new Point(672, 550);
            this.btnClose.Size = new Size(100, 35);
            this.btnClose.Click += BtnClose_Click;

            this.ClientSize = new Size(784, 601);
            this.Controls.Add(this.txtAst);
            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.lblErrorCount);
            this.Controls.Add(this.btnCopy);
            this.Controls.Add(this.btnClose);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Просмотр AST";
        }

        public void DisplayResults(string astJson, List<SemanticErrorInfo> errors, int errorCount)
        {
            string output = "";

            if (errors != null && errors.Count > 0)
            {
                output += "=== СЕМАНТИЧЕСКИЕ ОШИБКИ ===\n\n";
                foreach (var error in errors)
                {
                    output += $"[{error.Position}] {error.ErrorMessage}\n";
                }
                output += $"\nКоличество ошибок: {errorCount}\n\n";
                lblErrorCount.Text = $"Найдено ошибок: {errorCount}";
                lblErrorCount.ForeColor = Color.Red;
            }
            else
            {
                output += "=== СЕМАНТИЧЕСКИХ ОШИБОК НЕТ ===\n\n";
                lblErrorCount.Text = "Ошибок не найдено";
                lblErrorCount.ForeColor = Color.Green;
            }

            output += "=== AST (Abstract Syntax Tree) в формате JSON ===\n\n";
            output += astJson;

            txtAst.Text = output;
        }

        private void BtnCopy_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(txtAst.Text))
            {
                Clipboard.SetText(txtAst.Text);
                MessageBox.Show("Текст скопирован в буфер обмена", "Успех",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void Form3_Load(object sender, EventArgs e)
        {
            // Пустой метод для дизайнера
        }
    }
}