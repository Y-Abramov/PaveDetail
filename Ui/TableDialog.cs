using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Abr.Sdk;
using PaveDetail.Core;

namespace PaveDetail.Ui
{
    // Грид конструкций площадки: пять граф таблицы, которых нет в модели Robur.
    internal sealed class TableDialog : Form
    {
        private readonly DataGridView _grid = new DataGridView();
        private readonly IList<TableRow> _rows;

        public IList<TableRow> Rows { get { return _rows; } }

        public TableDialog(IList<TableRow> rows, IList<ProjectConstruction> orphans, bool canSave)
        {
            _rows = rows;

            Icon = AbrIcon.Create();
            Text = "Детали покрытий";
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(900, 480);
            MinimumSize = new Size(700, 400);

            // Порядок важен: Dock.Bottom добавляется ДО Dock.Fill - иначе грид,
            // добавленный первым, забирает всю клиентскую область и перекрывает
            // кнопки нижней панели (WinForms докует в порядке добавления).
            BuildFooter(orphans, canSave);
            BuildGrid();
        }

        private void BuildGrid()
        {
            _grid.Dock = DockStyle.Fill;
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToDeleteRows = false;
            _grid.RowHeadersVisible = false;
            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _grid.EditMode = DataGridViewEditMode.EditOnEnter;

            _grid.Columns.Add("code", "Тип покр.");
            _grid.Columns.Add("section", "Раздел");
            _grid.Columns.Add("name", "Наименование покрытия");
            _grid.Columns.Add("modulus", "Модуль упругости, МПа");
            _grid.Columns.Add("note", "Примечание");
            _grid.Columns.Add("composition", "Состав");
            _grid.Columns["composition"].ReadOnly = true;
            _grid.Columns["code"].FillWeight = 60;
            _grid.Columns["modulus"].FillWeight = 70;
            _grid.Columns["composition"].FillWeight = 140;

            foreach (var row in _rows)
            {
                var composition = new System.Text.StringBuilder();
                foreach (var l in row.Layers)
                {
                    if (composition.Length > 0) composition.Append(" · ");
                    composition.Append(l.Text).Append(' ').Append(l.ThicknessMm);
                }
                _grid.Rows.Add(row.Code, row.Section, row.Name, row.Modulus, row.Note, composition.ToString());
            }

            Controls.Add(_grid);
        }

        private void BuildFooter(IList<ProjectConstruction> orphans, bool canSave)
        {
            // Width задан явно (не дефолтные 200 у голого Panel) - иначе Anchor у ok/cancel
            // считает отступ от правого края от дефолтной ширины, и после Dock.Bottom
            // растягивает панель до ClientSize.Width обе кнопки утаскивает за край окна.
            var panel = new Panel { Dock = DockStyle.Bottom, Height = 76, Width = ClientSize.Width };

            var hint = new Label
            {
                AutoSize = false, Dock = DockStyle.Top, Height = 34,
                Text = canSave
                    ? "Значения сохранятся рядом с проектом и подтянутся при следующем построении."
                    : "Проект не сохранён - введённые значения не переживут закрытие Robur."
            };
            if (!canSave) hint.ForeColor = Color.FromArgb(140, 60, 0);

            if (orphans != null && orphans.Count > 0)
            {
                var names = new List<string>();
                foreach (var o in orphans) names.Add(string.IsNullOrEmpty(o.code) ? o.name : o.code);
                hint.Text += "  Записи без конструкции в площадке: " + string.Join(", ", names.ToArray())
                          + " - слои правили, данные отвязались.";
            }

            var ok = new Button { Text = "Построить", DialogResult = DialogResult.OK, Width = 100, Height = 28 };
            var cancel = new Button { Text = "Отмена", DialogResult = DialogResult.Cancel, Width = 100, Height = 28 };
            ok.Location = new Point(660, 40);
            cancel.Location = new Point(770, 40);
            ok.Anchor = cancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            ok.Click += OnOk;

            panel.Controls.Add(hint);
            panel.Controls.Add(ok);
            panel.Controls.Add(cancel);
            Controls.Add(panel);

            AcceptButton = ok;
            CancelButton = cancel;
        }

        private void OnOk(object sender, EventArgs e)
        {
            _grid.EndEdit();
            for (int i = 0; i < _rows.Count && i < _grid.Rows.Count; i++)
            {
                _rows[i].Code = Cell(i, "code");
                _rows[i].Section = Cell(i, "section");
                _rows[i].Name = Cell(i, "name");
                _rows[i].Modulus = Cell(i, "modulus");
                _rows[i].Note = Cell(i, "note");
            }
        }

        private string Cell(int row, string column)
        {
            var value = _grid.Rows[row].Cells[column].Value;
            return value == null ? string.Empty : value.ToString().Trim();
        }
    }
}
