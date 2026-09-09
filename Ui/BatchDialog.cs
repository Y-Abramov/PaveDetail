using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Abr.Sdk;
using PaveDetail.Core;

namespace PaveDetail.Ui
{
    internal sealed class BatchDialog : Form
    {
        private readonly CheckedListBox _list = new CheckedListBox();
        private readonly TextBox _step = new TextBox { Text = "3.0" };
        private readonly BatchResult _result;

        public List<ConstructionGroup> Selected { get; private set; }
        public double StepM { get; private set; }

        public BatchDialog(BatchResult result)
        {
            _result = result;
            Selected = new List<ConstructionGroup>();
            StepM = 3.0;

            Icon = AbrIcon.Create();
            Text = "Узлы по площадке";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(520, 400);

            _list.Location = new Point(12, 12);
            _list.Size = new Size(496, 280);
            _list.CheckOnClick = true;
            foreach (var g in result.Groups)
                _list.Items.Add(Describe(g), true);

            var stepLabel = new Label { Text = "Шаг раскладки, м:", Location = new Point(12, 306), AutoSize = true };
            _step.Location = new Point(140, 303);
            _step.Width = 80;

            var skipped = new Label
            {
                Location = new Point(12, 332),
                AutoSize = true,
                MaximumSize = new Size(496, 40),
                ForeColor = Color.FromArgb(150, 90, 0),
                Text = result.Skipped.Count == 0
                    ? string.Empty
                    : "Без конструкции, будут пропущены: " + string.Join(", ", result.Skipped.ToArray())
            };

            var ok = new Button { Text = "Построить", DialogResult = DialogResult.OK, Location = new Point(320, 356), Size = new Size(90, 28) };
            var cancel = new Button { Text = "Отмена", DialogResult = DialogResult.Cancel, Location = new Point(418, 356), Size = new Size(90, 28) };
            ok.Click += OnOk;

            Controls.AddRange(new Control[] { _list, stepLabel, _step, skipped, ok, cancel });
            AcceptButton = ok;
            CancelButton = cancel;
        }

        private void OnOk(object sender, System.EventArgs e)
        {
            Selected.Clear();
            for (int i = 0; i < _list.Items.Count; i++)
                if (_list.GetItemChecked(i)) Selected.Add(_result.Groups[i]);

            double v;
            if (double.TryParse(_step.Text.Replace(',', '.'),
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out v) && v > 0)
                StepM = v;
        }

        private static string Describe(ConstructionGroup g)
        {
            var head = g.Sample.Layers.Count + " слоёв";
            return head + " - регионов: " + g.RegionNames.Count + " (" + string.Join(", ", g.RegionNames.ToArray()) + ")";
        }
    }
}
