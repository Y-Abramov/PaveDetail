using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Abr.Sdk;
using PaveDetail.Core;

namespace PaveDetail.Ui
{
    // Правка каталогов гридом. На диске остаётся тот же JSON: грид разбирает файл
    // через MaterialCatalog.Parse / BorderCatalog.Parse и собирает обратно
    // через CatalogJson. Формат файла не меняется - его по-прежнему можно править
    // руками через «Открыть папку».
    internal sealed class CatalogDialog : Form
    {
        // Понятные подписи известных штриховок. Список редактируемый: пользователь
        // может ввести имя, которого здесь нет, - оно уйдёт в файл как есть.
        private static readonly string[] PatternItems =
        {
            "Наклонная (ANSI31)",
            "Щебень (GRAVEL)",
            "Песок (AR-SAND)",
            "Бетон (AR-CONC)",
            "Грунт (EARTH)",
            "Сплошная (SOLID)"
        };

        private const string RenderHatch = "Штриховка";
        private const string RenderLine = "Линия";

        private readonly DataGridView _materials = new DataGridView();
        private readonly DataGridView _borders = new DataGridView();
        private readonly Label _error = new Label();

        public CatalogDialog()
        {
            Icon = AbrIcon.Create();
            Text = "Каталоги оформления";
            FormBorderStyle = FormBorderStyle.Sizable;
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(760, 460);

            var tabs = new TabControl { Dock = DockStyle.Fill };

            var tabMaterials = new TabPage("Материалы");
            tabMaterials.Controls.Add(BuildMaterialsPage());
            tabs.TabPages.Add(tabMaterials);

            var tabBorders = new TabPage("Борта");
            tabBorders.Controls.Add(BuildBordersPage());
            tabs.TabPages.Add(tabBorders);

            // Ширина - от суммы графов грида материалов, не подобранное на глаз число.
            // Было ClientSize = 980 фиксировано - на экране с другим DPI/системным
            // шрифтом графы Толщина/Образец срезало без всякой прокрутки (все графы
            // фиксированной ширины, DataGridView.Columns.Width в расчёт не входил).
            const int sidePanelWidth = 130;
            const int gridChrome = 40; // рамка грида + вертикальная прокрутка про запас
            int width = MaterialsColumnsWidth() + sidePanelWidth + gridChrome;
            ClientSize = new Size(Math.Max(900, width), 560);

            // Порядок важен: Dock.Bottom добавляется ДО Dock.Fill - иначе tabs,
            // добавленный первым, забирает всю клиентскую область и перекрывает
            // кнопки нижней панели (см. тот же баг и фикс в TableDialog.cs).
            Controls.Add(BuildFooter());
            Controls.Add(tabs);

            LoadMaterials();
            LoadBorders();
        }

        // --- Материалы -----------------------------------------------------

        private Control BuildMaterialsPage()
        {
            var host = new Panel { Dock = DockStyle.Fill };

            _materials.Dock = DockStyle.Fill;
            _materials.AllowUserToAddRows = false;
            _materials.RowHeadersVisible = false;
            // Все графы - фиксированная ширина, ни одна не Fill. Fill-графа делит
            // оставшееся место по хитрой формуле (вес/остаток/минимум) - именно эта
            // арифметика на реальном экране (другой DPI/системный шрифт) съедала
            // запас впритык и срезала Толщина/Образец без прокрутки. Фиксированные
            // графы предсказуемы: лишнее всегда уходит в горизонтальную прокрутку
            // грида, а не пропадает без следа - см. MaterialsColumnsWidth() выше,
            // окно по умолчанию открывается на сумму этих ширин, прокрутка - только
            // подстраховка на случай, если юзер сам сузит окно.
            _materials.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            _materials.RowTemplate.Height = 30;

            _materials.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "match", HeaderText = "Ключевые слова", Width = 170 });
            _materials.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "fullName", HeaderText = "Наименование по ГОСТ", Width = 260 });

            var render = new DataGridViewComboBoxColumn
            { Name = "render", HeaderText = "Как рисовать", Width = 100 };
            render.Items.AddRange(RenderHatch, RenderLine);
            _materials.Columns.Add(render);

            var pattern = new DataGridViewComboBoxColumn
            {
                Name = "pattern", HeaderText = "Штриховка", Width = 160,
                DropDownWidth = 200
            };
            pattern.Items.AddRange(PatternItems);
            pattern.DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton;
            _materials.Columns.Add(pattern);

            // Список штриховок закрытый: присвоение ячейке значения, которого нет
            // в Items, поднимает DataError и в Robur выглядит как зависший диалог.
            // Имя из файла, которого нет в списке, добавляется в Items при загрузке
            // (см. EnsurePatternItem), а этот обработчик - страховка от остального.
            _materials.DataError += delegate (object s, DataGridViewDataErrorEventArgs args)
            {
                args.ThrowException = false;
                args.Cancel = true;
            };

            _materials.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "scale", HeaderText = "Масштаб", Width = 75 });
            _materials.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "angle", HeaderText = "Угол, °", Width = 70 });
            _materials.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "lineWeight", HeaderText = "Толщина линии", Width = 120 });
            _materials.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "preview", HeaderText = "Образец", ReadOnly = true, Width = 90 });

            _materials.CellPainting += OnPreviewPaint;
            _materials.CellValueChanged += delegate { _materials.Invalidate(); };
            _materials.CurrentCellDirtyStateChanged += delegate
            {
                // Списки фиксируют выбор только после ухода из ячейки - без этого
                // образец обновится с опозданием на одно действие.
                if (_materials.IsCurrentCellDirty)
                    _materials.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };

            var side = new FlowLayoutPanel
            {
                Dock = DockStyle.Right, Width = 130, FlowDirection = FlowDirection.TopDown,
                Padding = new Padding(6)
            };
            side.Controls.Add(MakeSideButton("Добавить", delegate { _materials.Rows.Add(); }));
            side.Controls.Add(MakeSideButton("Удалить", delegate { RemoveCurrent(_materials); }));
            side.Controls.Add(MakeSideButton("Вверх", delegate { MoveRow(_materials, -1); }));
            side.Controls.Add(MakeSideButton("Вниз", delegate { MoveRow(_materials, +1); }));

            var hint = new Label
            {
                Dock = DockStyle.Bottom, Height = 38, ForeColor = SystemColors.GrayText,
                Text = "Правило срабатывает, если название слоя из модели содержит любое из "
                     + "ключевых слов (через запятую).\r\nПравила проверяются сверху вниз, "
                     + "побеждает первое - порядок важен."
            };

            _error.Dock = DockStyle.Top;
            _error.Height = 0;
            _error.ForeColor = Color.White;
            _error.BackColor = Color.FromArgb(180, 60, 40);
            _error.Padding = new Padding(6, 4, 6, 4);
            _error.Visible = false;

            // Dock.Fill добавляется ПОСЛЕДНИМ - иначе _materials перекрывает
            // side/hint/_error (тот же баг, что в TableDialog.cs).
            host.Controls.Add(side);
            host.Controls.Add(hint);
            host.Controls.Add(_error);
            host.Controls.Add(_materials);
            return host;
        }

        // Сумма ширин графов, а не число, подобранное на глаз - при добавлении/
        // изменении графы окно само подстроится, не срежет новую графу молча.
        private int MaterialsColumnsWidth()
        {
            int width = 0;
            foreach (DataGridViewColumn c in _materials.Columns) width += c.Width;
            return width;
        }

        private void OnPreviewPaint(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (_materials.Columns[e.ColumnIndex].Name != "preview") return;

            e.Paint(e.CellBounds, DataGridViewPaintParts.Background | DataGridViewPaintParts.Border);

            var row = _materials.Rows[e.RowIndex];
            var box = new Rectangle(e.CellBounds.X + 4, e.CellBounds.Y + 4,
                                    Math.Max(4, e.CellBounds.Width - 8),
                                    Math.Max(4, e.CellBounds.Height - 8));

            HatchPreview.Draw(e.Graphics, box,
                PatternCode(CellText(row, "pattern")),
                Number(CellText(row, "scale"), 0.02),
                Number(CellText(row, "angle"), 0.0),
                CellText(row, "render") == RenderLine);

            e.Handled = true;
        }

        private void LoadMaterials()
        {
            var catalog = MaterialCatalog.Parse(CatalogStore.ReadOrSeed("materials.json"));
            ShowError("materials.json", catalog.LoadError);

            _materials.Rows.Clear();
            foreach (var r in catalog.Rules)
            {
                int i = _materials.Rows.Add();
                var row = _materials.Rows[i];
                row.Cells["match"].Value = r.match == null ? string.Empty : string.Join(", ", r.match);
                row.Cells["fullName"].Value = r.fullName ?? string.Empty;
                row.Cells["render"].Value = r.render == "line" ? RenderLine : RenderHatch;

                var label = PatternLabel(r.pattern);
                EnsurePatternItem(label);
                row.Cells["pattern"].Value = label;

                row.Cells["scale"].Value = r.scale.HasValue ? Format(r.scale.Value) : string.Empty;
                row.Cells["angle"].Value = r.angle.HasValue ? Format(r.angle.Value) : string.Empty;
                row.Cells["lineWeight"].Value = r.lineWeight.HasValue ? Format(r.lineWeight.Value) : string.Empty;
            }
        }

        private IList<MaterialRule> CollectMaterials()
        {
            var rules = new List<MaterialRule>();
            foreach (DataGridViewRow row in _materials.Rows)
            {
                if (row.IsNewRow) continue;

                var match = Split(CellText(row, "match"));
                if (match.Length == 0) continue;   // правило без ключевых слов не сработает никогда

                bool asLine = CellText(row, "render") == RenderLine;
                var rule = new MaterialRule
                {
                    match = match,
                    fullName = Empty(CellText(row, "fullName")),
                    render = asLine ? "line" : null,
                    pattern = asLine ? null : Empty(PatternCode(CellText(row, "pattern"))),
                    scale = asLine ? null : Optional(CellText(row, "scale")),
                    angle = asLine ? null : Optional(CellText(row, "angle")),
                    lineWeight = asLine ? Optional(CellText(row, "lineWeight")) : null
                };
                rules.Add(rule);
            }
            return rules;
        }

        // --- Борта ---------------------------------------------------------

        private Control BuildBordersPage()
        {
            var host = new Panel { Dock = DockStyle.Fill };

            _borders.Dock = DockStyle.Fill;
            _borders.AllowUserToAddRows = false;
            _borders.RowHeadersVisible = false;
            _borders.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            _borders.Columns.Add("name", "Марка");
            _borders.Columns.Add("width", "Ширина, м");
            _borders.Columns.Add("height", "Высота, м");
            _borders.Columns.Add("rise", "Возвышение, м");
            _borders.Columns.Add("material", "Материал подстилки");
            _borders.Columns.Add("thickness", "Толщина подстилки, м");
            _borders.Columns.Add("overhang", "Свес подстилки, м");
            _borders.Columns["name"].FillWeight = 110;
            _borders.Columns["material"].FillWeight = 140;

            var side = new FlowLayoutPanel
            {
                Dock = DockStyle.Right, Width = 130, FlowDirection = FlowDirection.TopDown,
                Padding = new Padding(6)
            };
            side.Controls.Add(MakeSideButton("Добавить", delegate { _borders.Rows.Add(); }));
            side.Controls.Add(MakeSideButton("Удалить", delegate { RemoveCurrent(_borders); }));

            // Dock.Fill добавляется ПОСЛЕДНИМ - тот же баг, что в BuildMaterialsPage.
            host.Controls.Add(side);
            host.Controls.Add(_borders);
            return host;
        }

        private void LoadBorders()
        {
            var catalog = BorderCatalog.Parse(CatalogStore.ReadOrSeed("borders.json"));
            if (catalog.LoadError != null) ShowError("borders.json", catalog.LoadError);

            _borders.Rows.Clear();
            foreach (var b in catalog.Borders)
                _borders.Rows.Add(b.Name, Format(b.WidthM), Format(b.HeightM), Format(b.RiseM),
                    b.BeddingMaterial, Format(b.BeddingThicknessM), Format(b.BeddingOverhangM));
        }

        private IList<BorderInput> CollectBorders()
        {
            var list = new List<BorderInput>();
            foreach (DataGridViewRow row in _borders.Rows)
            {
                if (row.IsNewRow) continue;
                var name = CellText(row, "name");
                if (name.Length == 0) continue;

                list.Add(new BorderInput
                {
                    Name = name,
                    WidthM = Number(CellText(row, "width"), 0.0),
                    HeightM = Number(CellText(row, "height"), 0.0),
                    RiseM = Number(CellText(row, "rise"), 0.0),
                    BeddingMaterial = CellText(row, "material"),
                    BeddingThicknessM = Number(CellText(row, "thickness"), 0.0),
                    BeddingOverhangM = Number(CellText(row, "overhang"), 0.0)
                });
            }
            return list;
        }

        // --- Низ окна ------------------------------------------------------

        private Control BuildFooter()
        {
            // Width задан явно - тот же риск анкера от дефолтной ширины Panel, что в TableDialog.cs.
            var panel = new Panel { Dock = DockStyle.Bottom, Height = 70, Width = ClientSize.Width };

            var pathLabel = new Label
            {
                Text = CatalogStore.Folder,
                Location = new Point(12, 8), AutoSize = true,
                ForeColor = SystemColors.GrayText
            };

            var open = new Button { Text = "Открыть папку", Location = new Point(12, 32), Size = new Size(120, 28) };
            open.Click += delegate { try { Process.Start(CatalogStore.Folder); } catch { } };

            var reset = new Button { Text = "Вернуть заводские", Location = new Point(140, 32), Size = new Size(150, 28) };
            reset.Click += OnReset;

            var save = new Button
            {
                Text = "Сохранить", Size = new Size(100, 28),
                Location = new Point(ClientSize.Width - 224, 32),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            save.Click += OnSave;

            var close = new Button
            {
                Text = "Закрыть", DialogResult = DialogResult.Cancel, Size = new Size(100, 28),
                Location = new Point(ClientSize.Width - 116, 32),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };

            panel.Controls.AddRange(new Control[] { pathLabel, open, reset, save, close });
            CancelButton = close;
            return panel;
        }

        private void OnReset(object sender, EventArgs e)
        {
            var answer = MessageBox.Show(
                "Заменить оба каталога заводскими? Ваши правки будут потеряны.",
                "Детали покрытий", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
            if (answer != DialogResult.OK) return;

            CatalogStore.ResetToBuiltIn("materials.json");
            CatalogStore.ResetToBuiltIn("borders.json");
            LoadMaterials();
            LoadBorders();
        }

        private void OnSave(object sender, EventArgs e)
        {
            _materials.EndEdit();
            _borders.EndEdit();

            var rules = CollectMaterials();
            if (rules.Count == 0)
            {
                MessageBox.Show("В каталоге материалов нет ни одного правила с ключевыми словами.",
                    "Детали покрытий");
                return;
            }

            var borders = CollectBorders();
            if (borders.Count == 0)
            {
                MessageBox.Show("В каталоге бортов нет ни одного типоразмера с маркой.",
                    "Детали покрытий");
                return;
            }

            var materialsJson = CatalogJson.Materials(rules);
            var bordersJson = CatalogJson.Borders(borders);

            // Собранный JSON обязан разбираться обратно: битый каталог на диск не уедет.
            var checkMat = MaterialCatalog.Parse(materialsJson);
            if (checkMat.LoadError != null)
            {
                MessageBox.Show("Каталог материалов не собрался:\r\n" + checkMat.LoadError, "Детали покрытий");
                return;
            }
            var checkBor = BorderCatalog.Parse(bordersJson);
            if (checkBor.LoadError != null)
            {
                MessageBox.Show("Каталог бортов не собрался:\r\n" + checkBor.LoadError, "Детали покрытий");
                return;
            }

            try
            {
                Directory.CreateDirectory(CatalogStore.Folder);
                File.WriteAllText(Path.Combine(CatalogStore.Folder, "materials.json"), materialsJson);
                File.WriteAllText(Path.Combine(CatalogStore.Folder, "borders.json"), bordersJson);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось записать каталоги:\r\n" + ex.Message, "Детали покрытий");
                return;
            }

            ShowError(null, null);
            MessageBox.Show("Каталоги сохранены.", "Детали покрытий");
        }

        // --- Вспомогательное -----------------------------------------------

        private void ShowError(string fileName, string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                _error.Visible = false;
                _error.Height = 0;
                return;
            }

            // Молчаливая подмена недопустима: пользователь должен понимать,
            // что видит встроенные правила, а не свой файл.
            _error.Text = "Файл " + fileName + " не разобрался: " + message
                        + ". Показаны встроенные правила. Сохранение перезапишет файл.";
            _error.Height = 40;
            _error.Visible = true;
        }

        private static Button MakeSideButton(string text, EventHandler onClick)
        {
            var b = new Button { Text = text, Size = new Size(110, 28), Margin = new Padding(3, 3, 3, 6) };
            b.Click += onClick;
            return b;
        }

        private static void RemoveCurrent(DataGridView grid)
        {
            if (grid.CurrentRow == null || grid.CurrentRow.IsNewRow) return;
            grid.Rows.Remove(grid.CurrentRow);
        }

        private static void MoveRow(DataGridView grid, int delta)
        {
            var row = grid.CurrentRow;
            if (row == null || row.IsNewRow) return;

            int target = row.Index + delta;
            if (target < 0 || target >= grid.Rows.Count) return;

            grid.Rows.Remove(row);
            grid.Rows.Insert(target, row);
            grid.CurrentCell = grid.Rows[target].Cells[0];
            grid.Invalidate();
        }

        private static string CellText(DataGridViewRow row, string column)
        {
            var value = row.Cells[column].Value;
            return value == null ? string.Empty : value.ToString().Trim();
        }

        private static string[] Split(string value)
        {
            var parts = value.Split(',');
            var list = new List<string>();
            foreach (var p in parts)
            {
                var t = p.Trim();
                if (t.Length > 0) list.Add(t);
            }
            return list.ToArray();
        }

        private static string Empty(string value)
        {
            return value.Length == 0 ? null : value;
        }

        // Число из ячейки: принимаем и точку, и запятую - пользователь набирает
        // на русской раскладке, а хранение всегда инвариантное.
        private static double Number(string value, double fallback)
        {
            double parsed;
            var text = value.Replace(',', '.');
            return double.TryParse(text, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out parsed) ? parsed : fallback;
        }

        private static double? Optional(string value)
        {
            if (value.Length == 0) return null;
            double parsed;
            var text = value.Replace(',', '.');
            return double.TryParse(text, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out parsed)
                ? (double?)parsed : null;
        }

        private static string Format(double value)
        {
            return value.ToString("0.############", System.Globalization.CultureInfo.InvariantCulture);
        }

        // Имя штриховки из файла, которого нет в готовом списке, добавляется в Items:
        // иначе присвоение ячейке поднимет DataError и значение потеряется.
        private void EnsurePatternItem(string label)
        {
            if (string.IsNullOrEmpty(label)) return;

            var column = (DataGridViewComboBoxColumn)_materials.Columns["pattern"];
            if (!column.Items.Contains(label)) column.Items.Add(label);
        }

        // «Наклонная (ANSI31)» ↔ «ANSI31». Имя, которого нет в списке, проходит как есть.
        private static string PatternLabel(string code)
        {
            if (string.IsNullOrEmpty(code)) return string.Empty;
            foreach (var item in PatternItems)
                if (PatternCode(item) == code.ToUpperInvariant()) return item;
            return code;
        }

        private static string PatternCode(string label)
        {
            if (string.IsNullOrEmpty(label)) return string.Empty;
            int open = label.LastIndexOf('(');
            int close = label.LastIndexOf(')');
            if (open >= 0 && close > open) return label.Substring(open + 1, close - open - 1).Trim();
            return label.Trim();
        }
    }
}
