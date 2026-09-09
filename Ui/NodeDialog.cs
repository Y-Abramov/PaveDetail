using System;
using System.Drawing;
using System.Windows.Forms;
using Abr.Sdk;
using PaveDetail.Core;

namespace PaveDetail.Ui
{
    internal sealed class NodeDialog : Form
    {
        private readonly DataGridView _grid = new DataGridView();
        private readonly RadioButton _kindSimple = new RadioButton { Text = "Простой" };
        private readonly RadioButton _kindBorder = new RadioButton { Text = "С бортовым камнем" };
        private readonly RadioButton _kindJunction = new RadioButton { Text = "Сопряжение двух регионов" };
        private readonly ComboBox _borderCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly TextBox _width = new TextBox { Text = "1.5" };
        private readonly TextBox _textHeight = new TextBox { Text = "0.08" };
        private readonly BorderCatalog _borderCatalog;

        public NodeInput Input { get; private set; }
        public NodeOptions Options { get; private set; }

        public NodeDialog(NodeInput input, MaterialResolver resolver, BorderCatalog borders)
        {
            Input = input;
            Options = new NodeOptions();
            _borderCatalog = borders;

            Icon = AbrIcon.Create();
            Text = "Узел дорожной одежды";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(560, 460);

            BuildGrid(input, resolver);
            BuildControls(input, borders);
        }

        private void BuildGrid(NodeInput input, MaterialResolver resolver)
        {
            _grid.Location = new Point(12, 12);
            _grid.Size = new Size(536, 220);
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToDeleteRows = false;
            _grid.RowHeadersVisible = false;
            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _grid.ReadOnly = true;

            _grid.Columns.Add("material", "Материал");
            _grid.Columns.Add("thickness", "Толщина, мм");
            _grid.Columns.Add("pattern", "Штриховка");

            foreach (var layer in input.Layers)
            {
                var style = resolver.Resolve(layer);
                int mm = (int)Math.Round(layer.ThicknessM * 1000.0, MidpointRounding.AwayFromZero);
                int row = _grid.Rows.Add(layer.Material, mm == 0 ? "-" : mm.ToString(),
                    style.RenderAsLine ? "линия" : style.PatternName);

                // Промах правила материала - подсвечиваем строку, юзер видит, что штриховка условная.
                if (style.UseLayerColor)
                    _grid.Rows[row].DefaultCellStyle.BackColor = Color.FromArgb(255, 244, 214);
            }

            Controls.Add(_grid);
        }

        private void BuildControls(NodeInput input, BorderCatalog borders)
        {
            var kindLabel = new Label { Text = "Тип узла:", Location = new Point(12, 244), AutoSize = true };
            _kindSimple.Location = new Point(24, 266);
            _kindBorder.Location = new Point(24, 290);
            _kindJunction.Location = new Point(24, 314);
            _kindSimple.AutoSize = _kindBorder.AutoSize = _kindJunction.AutoSize = true;
            _kindSimple.Checked = input.Kind == NodeKind.Simple;
            _kindBorder.Checked = input.Kind == NodeKind.WithBorder;
            _kindJunction.Checked = input.Kind == NodeKind.Junction;

            var borderLabel = new Label { Text = "Бортовой камень:", Location = new Point(300, 266), AutoSize = true };
            _borderCombo.Location = new Point(300, 288);
            _borderCombo.Width = 240;
            if (input.Border != null && input.Border.FromModel)
                _borderCombo.Items.Add("Из модели: ширина " + input.Border.WidthM.ToString("F2") + " м");
            foreach (var b in borders.Borders) _borderCombo.Items.Add(b.Name);
            if (_borderCombo.Items.Count > 0) _borderCombo.SelectedIndex = 0;

            var widthLabel = new Label { Text = "Ширина фрагмента, м:", Location = new Point(12, 348), AutoSize = true };
            _width.Location = new Point(180, 345);
            _width.Width = 80;

            var textLabel = new Label { Text = "Высота текста, м:", Location = new Point(300, 348), AutoSize = true };
            _textHeight.Location = new Point(430, 345);
            _textHeight.Width = 80;

            var ok = new Button { Text = "Построить", DialogResult = DialogResult.OK, Location = new Point(360, 400), Size = new Size(90, 28) };
            var cancel = new Button { Text = "Отмена", DialogResult = DialogResult.Cancel, Location = new Point(458, 400), Size = new Size(90, 28) };
            ok.Click += OnOk;

            Controls.AddRange(new Control[]
            {
                kindLabel, _kindSimple, _kindBorder, _kindJunction,
                borderLabel, _borderCombo, widthLabel, _width, textLabel, _textHeight, ok, cancel
            });

            AcceptButton = ok;
            CancelButton = cancel;
        }

        private void OnOk(object sender, EventArgs e)
        {
            Input.Kind = _kindJunction.Checked ? NodeKind.Junction
                : _kindBorder.Checked ? NodeKind.WithBorder
                : NodeKind.Simple;

            if (Input.Kind != NodeKind.Simple && (Input.Border == null || !Input.Border.FromModel)
                && _borderCombo.SelectedItem != null)
            {
                var name = _borderCombo.SelectedItem.ToString();
                foreach (var b in _borderCatalog.Borders)
                    if (b.Name == name) { Input.Border = b; break; }
            }

            double w, h;
            if (double.TryParse(_width.Text.Replace(',', '.'),
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out w) && w > 0)
                Options.WidthM = w;

            if (double.TryParse(_textHeight.Text.Replace(',', '.'),
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out h) && h > 0)
                Options.TextHeightM = h;
        }
    }
}
