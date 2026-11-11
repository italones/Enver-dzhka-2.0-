using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

public partial class TextAnalyzerForm : Form
{
    private CancellationTokenSource _cts;

    public TextAnalyzerForm()
    {
        InitializeComponent();

        // Задание 3: список полей отчёта
        clbFields.Items.AddRange(new object[]
        {
            "Предложения", "Символы", "Слова",
            "Вопросительные", "Восклицательные"
        });
        for (int i = 0; i < clbFields.Items.Count; i++)
            clbFields.SetItemChecked(i, true);

        rbScreen.Checked = true;
        btnCancel.Enabled = false;
    }

    private HashSet<string> GetSelectedFields()
    {
        var map = new Dictionary<int, string>
        {
            [0] = "sentences",
            [1] = "characters",
            [2] = "words",
            [3] = "questions",
            [4] = "exclamations"
        };
        var set = new HashSet<string>();
        foreach (int i in clbFields.CheckedIndices)
            set.Add(map[i]);
        return set;
    }

    private async void btnAnalyze_Click(object sender, EventArgs e)
    {
        string text = txtInput.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            MessageBox.Show("Введите текст.");
            return;
        }

        var fields = GetSelectedFields();
        if (fields.Count == 0)
        {
            MessageBox.Show("Выберите хотя бы одно поле отчёта.");
            return;
        }

        // Задание 2: подготовка к отмене
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        btnAnalyze.Enabled = false;
        btnCancel.Enabled = true;
        txtReport.Clear();
        progress.Style = ProgressBarStyle.Marquee;
        lblStatus.Text = "Анализ...";

        try
        {
            // Задание 1: анализ в фоне через Task
            TextReport report = await Task.Run(
                () => AnalyzeText(text, token), token);

            token.ThrowIfCancellationRequested();

            string result = report.BuildReport(fields);

            if (rbFile.Checked)
            {
                using var sfd = new SaveFileDialog
                {
                    Filter = "Текстовые файлы (*.txt)|*.txt",
                    FileName = "report.txt"
                };
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(sfd.FileName, result, Encoding.UTF8);
                    lblStatus.Text = "Отчёт сохранён в файл";
                }
                else
                {
                    lblStatus.Text = "Сохранение отменено";
                }
            }
            else
            {
                txtReport.Text = result;
                lblStatus.Text = "Готово";
            }
        }
        catch (OperationCanceledException)
        {
            lblStatus.Text = "Анализ отменён";
        }
        catch (Exception ex)
        {
            MessageBox.Show("Ошибка: " + ex.Message);
            lblStatus.Text = "Ошибка";
        }
        finally
        {
            progress.Style = ProgressBarStyle.Blocks;
            btnAnalyze.Enabled = true;
            btnCancel.Enabled = false;
        }
    }

    private void btnCancel_Click(object sender, EventArgs e)
    {
        // Задание 2: остановка
        _cts?.Cancel();
    }

    // Сам анализ текста
    private static TextReport AnalyzeText(string text, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        var sentences = Regex.Split(text, @"(?<=[.!?])\s+")
                             .Where(s => s.Trim().Length > 0)
                             .ToList();

        int chars = text.Length;

        var words = Regex.Matches(text, @"\b[\p{L}\p{N}]+\b")
                         .Cast<Match>()
                         .Select(m => m.Value)
                         .ToList();

        int questions = sentences.Count(s => s.TrimEnd().EndsWith("?"));
        int exclamations = sentences.Count(s => s.TrimEnd().EndsWith("!"));

        token.ThrowIfCancellationRequested();

        return new TextReport
        {
            Sentences = sentences.Count,
            Characters = chars,
            Words = words.Count,
            QuestionSentences = questions,
            ExclamationSentences = exclamations
        };
    }
}