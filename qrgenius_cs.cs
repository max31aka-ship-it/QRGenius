// qrgenius_cs.cs — генератор QR-кодов с логотипом на C# (WPF)

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using QRCoder;

namespace QRGeniusWPF
{
    public partial class MainWindow : Window
    {
        private string logoPath = null;
        private Bitmap currentQR = null;
        private System.Windows.Media.Color qrColor = Colors.Black;
        private string stateFile = "qrgenius_state.json";

        private TextBox textBox;
        private ComboBox errorCombo;
        private Slider sizeSlider;
        private Label sizeLabel;
        private Button colorBtn;
        private Label logoLabel;
        private Image previewImage;
        private Label statusLabel;

        public MainWindow()
        {
            InitializeComponent();
            CreateUI();
            LoadState();
        }

        private void CreateUI()
        {
            Title = "📱 QRGenius — C#";
            Width = 800;
            Height = 700;
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Параметры
            var paramPanel = new StackPanel { Margin = new Thickness(10) };
            paramPanel.Children.Add(new Label { Content = "Текст / URL:" });
            textBox = new TextBox { Text = "https://github.com/yourname/qrgenius", Margin = new Thickness(0,0,0,5) };
            paramPanel.Children.Add(textBox);

            var sizePanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0,0,0,5) };
            sizePanel.Children.Add(new Label { Content = "Размер:", Width = 60 });
            sizeSlider = new Slider { Minimum = 100, Maximum = 1000, Value = 400, TickFrequency = 50, IsSnapToTickEnabled = true, Width = 200 };
            sizePanel.Children.Add(sizeSlider);
            sizeLabel = new Label { Content = "400", Width = 40 };
            sizePanel.Children.Add(sizeLabel);
            sizeSlider.ValueChanged += (s, e) => sizeLabel.Content = ((int)e.NewValue).ToString();
            paramPanel.Children.Add(sizePanel);

            var errorPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0,0,0,5) };
            errorPanel.Children.Add(new Label { Content = "Коррекция:", Width = 80 });
            errorCombo = new ComboBox { Width = 60 };
            errorCombo.Items.Add("L");
            errorCombo.Items.Add("M");
            errorCombo.Items.Add("Q");
            errorCombo.Items.Add("H");
            errorCombo.SelectedIndex = 3;
            errorPanel.Children.Add(errorCombo);
            paramPanel.Children.Add(errorPanel);

            var colorPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0,0,0,5) };
            colorPanel.Children.Add(new Label { Content = "Цвет:", Width = 60 });
            colorBtn = new Button { Width = 30, Height = 30, Background = new SolidColorBrush(Colors.Black) };
            colorBtn.Click += (s, e) => {
                var dialog = new System.Windows.Forms.ColorDialog();
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                    qrColor = System.Windows.Media.Color.FromArgb(dialog.Color.A, dialog.Color.R, dialog.Color.G, dialog.Color.B);
                    colorBtn.Background = new SolidColorBrush(qrColor);
                }
            };
            colorPanel.Children.Add(colorBtn);
            paramPanel.Children.Add(colorPanel);

            var logoPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0,0,0,5) };
            logoPanel.Children.Add(new Label { Content = "Логотип:", Width = 60 });
            logoLabel = new Label { Content = "Не выбран" };
            logoPanel.Children.Add(logoLabel);
            var logoBtn = new Button { Content = "Выбрать" };
            logoBtn.Click += (s, e) => ChooseLogo();
            logoPanel.Children.Add(logoBtn);
            var clearLogoBtn = new Button { Content = "Очистить" };
            clearLogoBtn.Click += (s, e) => ClearLogo();
            logoPanel.Children.Add(clearLogoBtn);
            paramPanel.Children.Add(logoPanel);
            Grid.SetRow(paramPanel, 0);
            grid.Children.Add(paramPanel);

            // Кнопки
            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(10) };
            var genBtn = new Button { Content = "Сгенерировать", Background = new SolidColorBrush(Colors.Green), Foreground = new SolidColorBrush(Colors.White), Width = 100 };
            genBtn.Click += (s, e) => GenerateQR();
            btnPanel.Children.Add(genBtn);
            var saveBtn = new Button { Content = "Сохранить", Width = 100 };
            saveBtn.Click += (s, e) => SaveQR();
            btnPanel.Children.Add(saveBtn);
            var resetBtn = new Button { Content = "Сбросить", Width = 100 };
            resetBtn.Click += (s, e) => Reset();
            btnPanel.Children.Add(resetBtn);
            Grid.SetRow(btnPanel, 1);
            grid.Children.Add(btnPanel);

            // Предпросмотр
            var previewGroup = new GroupBox { Header = "Предпросмотр", Margin = new Thickness(10) };
            previewImage = new Image { Stretch = Stretch.Uniform, Margin = new Thickness(5) };
            previewImage.Source = null;
            previewGroup.Content = previewImage;
            Grid.SetRow(previewGroup, 2);
            grid.Children.Add(previewGroup);

            // Статус
            statusLabel = new Label { Content = "Готов", Margin = new Thickness(10) };
            Grid.SetRow(statusLabel, 3);
            grid.Children.Add(statusLabel);

            Content = grid;

            // Hotkeys
            this.KeyDown += (s, e) => {
                if (e.Key == Key.G && Keyboard.Modifiers == ModifierKeys.Control) GenerateQR();
                if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control) SaveQR();
            };
        }

        private void GenerateQR()
        {
            string text = textBox.Text.Trim();
            if (string.IsNullOrEmpty(text)) {
                MessageBox.Show("Введите текст или URL");
                return;
            }
            int size = (int)sizeSlider.Value;
            QRCodeGenerator.ECCLevel errLevel;
            switch (errorCombo.SelectedIndex) {
                case 0: errLevel = QRCodeGenerator.ECCLevel.L; break;
                case 1: errLevel = QRCodeGenerator.ECCLevel.M; break;
                case 2: errLevel = QRCodeGenerator.ECCLevel.Q; break;
                default: errLevel = QRCodeGenerator.ECCLevel.H; break;
            }
            QRCodeGenerator qrGen = new QRCodeGenerator();
            QRCodeData qrData = qrGen.CreateQrCode(text, errLevel);
            QRCode qrCode = new QRCode(qrData);
            Bitmap qrBitmap = qrCode.GetGraphic(20, ToDrawingColor(qrColor), System.Drawing.Color.White, true);

            // Вставка логотипа
            if (!string.IsNullOrEmpty(logoPath) && File.Exists(logoPath)) {
                try {
                    using (var logo = new Bitmap(logoPath)) {
                        int logoSize = (int)(size * 0.2);
                        using (var resizedLogo = new Bitmap(logo, new System.Drawing.Size(logoSize, logoSize))) {
                            using (Graphics g = Graphics.FromImage(qrBitmap)) {
                                int x = (qrBitmap.Width - logoSize) / 2;
                                int y = (qrBitmap.Height - logoSize) / 2;
                                g.DrawImage(resizedLogo, x, y, logoSize, logoSize);
                            }
                        }
                    }
                } catch (Exception ex) {
                    statusLabel.Content = "Ошибка вставки логотипа: " + ex.Message;
                }
            }

            currentQR = qrBitmap;
            previewImage.Source = BitmapToImageSource(qrBitmap);
            statusLabel.Content = $"QR-код сгенерирован, размер: {size}x{size}";
        }

        private System.Drawing.Color ToDrawingColor(System.Windows.Media.Color color) {
            return System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        private BitmapImage BitmapToImageSource(Bitmap bitmap) {
            using (MemoryStream ms = new MemoryStream()) {
                bitmap.Save(ms, ImageFormat.Png);
                ms.Position = 0;
                BitmapImage bi = new BitmapImage();
                bi.BeginInit();
                bi.StreamSource = ms;
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.EndInit();
                return bi;
            }
        }

        private void SaveQR()
        {
            if (currentQR == null) {
                MessageBox.Show("Сначала сгенерируйте QR-код");
                return;
            }
            SaveFileDialog dialog = new SaveFileDialog { Filter = "PNG (*.png)|*.png" };
            if (dialog.ShowDialog() == true) {
                currentQR.Save(dialog.FileName, ImageFormat.Png);
                statusLabel.Content = "Сохранено в " + System.IO.Path.GetFileName(dialog.FileName);
            }
        }

        private void ChooseLogo()
        {
            OpenFileDialog dialog = new OpenFileDialog { Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.gif" };
            if (dialog.ShowDialog() == true) {
                logoPath = dialog.FileName;
                logoLabel.Content = System.IO.Path.GetFileName(dialog.FileName);
                statusLabel.Content = "Логотип выбран: " + System.IO.Path.GetFileName(dialog.FileName);
            }
        }

        private void ClearLogo()
        {
            logoPath = null;
            logoLabel.Content = "Не выбран";
            statusLabel.Content = "Логотип очищен";
        }

        private void Reset()
        {
            textBox.Text = "https://github.com/yourname/qrgenius";
            sizeSlider.Value = 400;
            errorCombo.SelectedIndex = 3;
            qrColor = Colors.Black;
            colorBtn.Background = new SolidColorBrush(Colors.Black);
            ClearLogo();
            currentQR = null;
            previewImage.Source = null;
            statusLabel.Content = "Сброшено";
        }

        private void LoadState()
        {
            // Для простоты не загружаем
        }

        private void SaveState()
        {
            // Для простоты не сохраняем
        }

        [STAThread]
        static void Main()
        {
            var app = new Application();
            app.Run(new MainWindow());
        }
    }
}
