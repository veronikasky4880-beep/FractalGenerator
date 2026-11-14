using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace FractalGenerator
{
    public partial class Form1 : Form
    {
        private NumericUpDown depthInput;
        private NumericUpDown angleInput;
        private NumericUpDown lengthRatioInput;
        private NumericUpDown iterationsInput;
        private NumericUpDown cRealInput;
        private NumericUpDown cImaginaryInput;

        private DoubleBufferedPanel drawingPanel;
        private ComboBox fractalSelector;
        private ComboBox paletteSelector;
        private ComboBox resolutionSelector;

        private Button saveButton;
        private Button savePdfButton;
        private Button printButton;
        private Button resetViewButton;
        private Button zoomInButton;
        private Button zoomOutButton;
        private Button zoomDemoButton;
        private Button viperSpiralButton;
        private Label zoomLabel;

        private Mandelbrot mandelbrot;
        private JuliaSet juliaSet;
        private LSystem lSystem;

        private Bitmap? mandelbrotBitmap;

        // Параметри області, для якої було створено поточне зображення
        private double bitmapCenterReal = -0.75;
        private double bitmapCenterImaginary = 0;
        private double bitmapViewWidth = 0;
        private double bitmapViewHeight = 0;

        private CancellationTokenSource? renderCancellation;
        private CancellationTokenSource? zoomDemoCancellation;
        private int renderVersion = 0;

        private System.Windows.Forms.Timer renderTimer;

        // Параметри області множини Мандельброта / Жюліа
        private double centerReal = -0.75;
        private double centerImaginary = 0;
        private double zoom = 1.0;

        // Параметри переміщення зображення та виділення області мишкою
        private bool isDragging = false;
        private Point lastMousePosition;
        private bool isSelectingArea = false;
        private Point selectionStart;
        private Point selectionEnd;

        private bool isApplyingViperPreset = false;

        public Form1()
        {
            InitializeComponent();

            mandelbrot = new Mandelbrot();
            juliaSet = new JuliaSet();
            lSystem = new LSystem();

            renderTimer = new System.Windows.Forms.Timer();
            renderTimer.Interval = 60;
            renderTimer.Tick += RenderTimer_Tick;

            Text = "Fractal Generator";
            ClientSize = new Size(1150, 760);
            BackColor = Color.FromArgb(15, 20, 30);
            DoubleBuffered = true;

            // Панель для малювання фрактала
            drawingPanel = new DoubleBufferedPanel();
            drawingPanel.Dock = DockStyle.Fill;
            drawingPanel.BackColor = Color.FromArgb(15, 20, 30);
            drawingPanel.TabStop = true;

            drawingPanel.Paint += DrawingPanel_Paint;
            drawingPanel.Resize += DrawingPanel_Resize;

            // Події для керування масштабом мишкою
            drawingPanel.MouseEnter += DrawingPanel_MouseEnter;
            drawingPanel.MouseWheel += DrawingPanel_MouseWheel;
            drawingPanel.MouseDown += DrawingPanel_MouseDown;
            drawingPanel.MouseMove += DrawingPanel_MouseMove;
            drawingPanel.MouseUp += DrawingPanel_MouseUp;

            // Панель керування
            FlowLayoutPanel controlsPanel = new FlowLayoutPanel();
            controlsPanel.Dock = DockStyle.Top;
            controlsPanel.AutoSize = true;
            controlsPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            controlsPanel.Padding = new Padding(10, 8, 10, 8);
            controlsPanel.BackColor = Color.FromArgb(30, 40, 55);
            controlsPanel.WrapContents = true;

            // Глибина
            Label depthLabel = CreateLabel("Depth:", new Padding(5, 7, 5, 0));

            depthInput = new NumericUpDown();
            depthInput.Minimum = 0;
            depthInput.Maximum = 10;
            depthInput.Value = 6;
            depthInput.Width = 55;
            depthInput.Margin = new Padding(0, 4, 0, 4);
            depthInput.ValueChanged += Parameters_ValueChanged;

            // Кут гілок
            Label angleLabel = CreateLabel("Branch angle:", new Padding(12, 7, 5, 0));

            angleInput = new NumericUpDown();
            angleInput.Minimum = 10;
            angleInput.Maximum = 60;
            angleInput.Value = 30;
            angleInput.Width = 55;
            angleInput.Margin = new Padding(0, 4, 0, 4);
            angleInput.ValueChanged += Parameters_ValueChanged;

            // Коефіцієнт довжини
            Label ratioLabel = CreateLabel("Length ratio:", new Padding(12, 7, 5, 0));

            lengthRatioInput = new NumericUpDown();
            lengthRatioInput.Minimum = 0.30M;
            lengthRatioInput.Maximum = 0.90M;
            lengthRatioInput.DecimalPlaces = 2;
            lengthRatioInput.Increment = 0.05M;
            lengthRatioInput.Value = 0.70M;
            lengthRatioInput.Width = 65;
            lengthRatioInput.Margin = new Padding(0, 4, 0, 4);
            lengthRatioInput.ValueChanged += Parameters_ValueChanged;

            // Кількість ітерацій
            Label iterationsLabel = CreateLabel("Iterations:", new Padding(12, 7, 5, 0));

            iterationsInput = new NumericUpDown();
            iterationsInput.Minimum = 50;
            iterationsInput.Maximum = 1000;
            iterationsInput.Increment = 50;
            iterationsInput.Value = 300;
            iterationsInput.Width = 75;
            iterationsInput.Margin = new Padding(0, 4, 0, 4);
            iterationsInput.ValueChanged += Parameters_ValueChanged;

            // Дійсна частина константи c
            Label cRealLabel = CreateLabel("Julia c real:", new Padding(12, 7, 5, 0));

            cRealInput = new NumericUpDown();
            cRealInput.Minimum = -2;
            cRealInput.Maximum = 2;
            cRealInput.DecimalPlaces = 3;
            cRealInput.Increment = 0.01M;
            cRealInput.Value = -0.800M;
            cRealInput.Width = 82;
            cRealInput.Margin = new Padding(0, 4, 0, 4);
            cRealInput.ValueChanged += Parameters_ValueChanged;

            // Уявна частина константи c
            Label cImaginaryLabel = CreateLabel("Julia c imaginary:", new Padding(12, 7, 5, 0));

            cImaginaryInput = new NumericUpDown();
            cImaginaryInput.Minimum = -2;
            cImaginaryInput.Maximum = 2;
            cImaginaryInput.DecimalPlaces = 3;
            cImaginaryInput.Increment = 0.01M;
            cImaginaryInput.Value = 0.156M;
            cImaginaryInput.Width = 75;
            cImaginaryInput.Margin = new Padding(0, 4, 0, 4);
            cImaginaryInput.ValueChanged += Parameters_ValueChanged;

            // Тип фрактала
            Label fractalLabel = CreateLabel("Fractal:", new Padding(12, 7, 5, 0));

            fractalSelector = new ComboBox();
            fractalSelector.DropDownStyle = ComboBoxStyle.DropDownList;
            fractalSelector.Items.Add("Fractal Tree");
            fractalSelector.Items.Add("Sierpinski Triangle");
            fractalSelector.Items.Add("Mandelbrot Set");
            fractalSelector.Items.Add("Julia Set");
            fractalSelector.Items.Add("L-System");
            fractalSelector.SelectedIndex = 0;
            fractalSelector.Width = 155;
            fractalSelector.Margin = new Padding(0, 3, 0, 3);
            fractalSelector.SelectedIndexChanged += FractalSelector_SelectedIndexChanged;

            // Кольорова палітра (додано Kill Bill)
            Label paletteLabel = CreateLabel("Palette:", new Padding(12, 7, 5, 0));

            paletteSelector = new ComboBox();
            paletteSelector.DropDownStyle = ComboBoxStyle.DropDownList;
            paletteSelector.Items.Add("Emerald");
            paletteSelector.Items.Add("Ocean");
            paletteSelector.Items.Add("Sunset");
            paletteSelector.Items.Add("Violet");
            paletteSelector.Items.Add("Kill Bill");
            paletteSelector.SelectedIndex = 0;
            paletteSelector.Width = 95;
            paletteSelector.Margin = new Padding(0, 3, 0, 3);
            paletteSelector.SelectedIndexChanged += PaletteSelector_SelectedIndexChanged;

            // Personal Twist кнопка (Viper Spiral — Julia Set у палітрі Kill Bill)
            viperSpiralButton = CreateStyledButton("Viper Spiral", new Padding(8, 2, 5, 2));
            viperSpiralButton.BackColor = Color.FromArgb(220, 175, 15);
            viperSpiralButton.ForeColor = Color.FromArgb(15, 15, 15);
            viperSpiralButton.Font = new Font(Font, FontStyle.Bold);
            viperSpiralButton.FlatAppearance.BorderColor = Color.FromArgb(255, 220, 50);
            viperSpiralButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(245, 200, 25);
            viperSpiralButton.EnabledChanged += (s, e) =>
            {
                viperSpiralButton.BackColor = Color.FromArgb(220, 175, 15);
                viperSpiralButton.ForeColor = Color.FromArgb(15, 15, 15);
            };
            viperSpiralButton.Click += ViperSpiralButton_Click;

            // Відображення поточного масштабу
            zoomLabel = CreateLabel("Zoom: 1.00x", new Padding(8, 7, 5, 0));

            // Збільшення масштабу у 2 рази
            zoomInButton = CreateStyledButton("2x", new Padding(4, 2, 2, 2));
            zoomInButton.Click += ZoomInButton_Click;

            // Зменшення масштабу у 2 рази
            zoomOutButton = CreateStyledButton("0.5x", new Padding(2, 2, 2, 2));
            zoomOutButton.Click += ZoomOutButton_Click;

            // Анімація 1000x зуму за 60 кадрів (Deep Zoom quest)
            zoomDemoButton = CreateStyledButton("Zoom 1000x", new Padding(4, 2, 4, 2));
            zoomDemoButton.Click += ZoomDemoButton_Click;

            // Повернення до початкового вигляду
            resetViewButton = CreateStyledButton("Reset View", new Padding(2, 2, 6, 2));
            resetViewButton.Click += ResetViewButton_Click;

            // Збереження зображення
            saveButton = CreateStyledButton("Save PNG", new Padding(4, 2, 4, 2));
            saveButton.Click += SaveButton_Click;

            // Вибір роздільної здатності для друку
            Label resolutionLabel = CreateLabel("Print size:", new Padding(8, 7, 5, 0));

            resolutionSelector = new ComboBox();
            resolutionSelector.DropDownStyle = ComboBoxStyle.DropDownList;
            resolutionSelector.Items.Add("4K (3840 x 2160)");
            resolutionSelector.Items.Add("8K (7680 x 4320)");
            resolutionSelector.SelectedIndex = 0;
            resolutionSelector.Width = 165;
            resolutionSelector.Margin = new Padding(0, 3, 4, 3);

            // Збереження у PDF
            savePdfButton = CreateStyledButton("Save PDF", new Padding(4, 2, 4, 2));
            savePdfButton.Click += SavePdfButton_Click;

            // Друк фрактала
            printButton = CreateStyledButton("Print", new Padding(4, 2, 4, 2));
            printButton.Click += PrintButton_Click;

            // Додаємо елементи керування
            controlsPanel.Controls.Add(depthLabel);
            controlsPanel.Controls.Add(depthInput);

            controlsPanel.Controls.Add(angleLabel);
            controlsPanel.Controls.Add(angleInput);

            controlsPanel.Controls.Add(ratioLabel);
            controlsPanel.Controls.Add(lengthRatioInput);

            controlsPanel.Controls.Add(iterationsLabel);
            controlsPanel.Controls.Add(iterationsInput);

            controlsPanel.Controls.Add(cRealLabel);
            controlsPanel.Controls.Add(cRealInput);

            controlsPanel.Controls.Add(cImaginaryLabel);
            controlsPanel.Controls.Add(cImaginaryInput);

            controlsPanel.Controls.Add(fractalLabel);
            controlsPanel.Controls.Add(fractalSelector);

            controlsPanel.Controls.Add(paletteLabel);
            controlsPanel.Controls.Add(paletteSelector);

            controlsPanel.Controls.Add(viperSpiralButton);

            controlsPanel.Controls.Add(zoomLabel);
            controlsPanel.Controls.Add(zoomInButton);
            controlsPanel.Controls.Add(zoomOutButton);
            controlsPanel.Controls.Add(zoomDemoButton);
            controlsPanel.Controls.Add(resetViewButton);
            controlsPanel.Controls.Add(saveButton);
            controlsPanel.Controls.Add(resolutionLabel);
            controlsPanel.Controls.Add(resolutionSelector);
            controlsPanel.Controls.Add(savePdfButton);
            controlsPanel.Controls.Add(printButton);

            Controls.Add(drawingPanel);
            Controls.Add(controlsPanel);

            UpdateControls();

            FormClosing += Form1_FormClosing;
        }

        private Label CreateLabel(string text, Padding margin)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                ForeColor = Color.White,
                Margin = margin
            };
        }

        private Button CreateStyledButton(string text, Padding margin)
        {
            Button btn = new Button
            {
                Text = text,
                AutoSize = true,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(48, 62, 85),
                ForeColor = Color.White,
                Margin = margin,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(80, 100, 130);
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(65, 85, 115);

            btn.EnabledChanged += (s, e) =>
            {
                btn.BackColor = btn.Enabled
                    ? Color.FromArgb(48, 62, 85)
                    : Color.FromArgb(35, 45, 62);
            };

            return btn;
        }

        private Color GetCanvasBackgroundColor()
        {
            // Для теми Kill Bill робимо глибокий кінематографічний чорний фон
            return paletteSelector.SelectedIndex == 4
                ? Color.FromArgb(10, 10, 12)
                : Color.FromArgb(15, 20, 30);
        }

        // Обробник зміни палітри
        private void PaletteSelector_SelectedIndexChanged(object? sender, EventArgs e)
        {
            drawingPanel.BackColor = GetCanvasBackgroundColor();
            Parameters_ValueChanged(sender, e);
        }

        // Personal Twist: "Viper Spiral" (Julia Set у палітрі Kill Bill)
        private void ViperSpiralButton_Click(object? sender, EventArgs e)
        {
            StopZoomDemo();

            isApplyingViperPreset = true;
            try
            {
                // Вмикаємо чорно-жовту палітру Kill Bill
                paletteSelector.SelectedIndex = 4;

                // Перемикаємо на Julia Set
                if (fractalSelector.SelectedIndex != 3)
                {
                    fractalSelector.SelectedIndex = 3;
                }

                // Координати подвійної спіралі "Viper Spiral"
                cRealInput.DecimalPlaces = 3;
                cImaginaryInput.DecimalPlaces = 3;
                cRealInput.Value = -0.745M;
                cImaginaryInput.Value = 0.113M;

                centerReal = 0.0;
                centerImaginary = 0.0;
                zoom = 1.25;
                iterationsInput.Value = 350;

                UpdateZoomLabel();
                ScheduleMandelbrotRender();
            }
            finally
            {
                isApplyingViperPreset = false;
            }
        }

        // Плавна анімація 1000x зуму за 60 кадрів (0.5x під час руху + 1.0x фінальний кадр)
        private async void ZoomDemoButton_Click(object? sender, EventArgs e)
        {
            if (zoomDemoCancellation != null)
            {
                StopZoomDemo();
                ScheduleMandelbrotRender(); // Дорендерити в повній якості при зупинці
                return;
            }

            if (!IsComplexFractal())
            {
                fractalSelector.SelectedIndex = 3; // Julia Set
            }

            CancellationTokenSource cts = new CancellationTokenSource();
            zoomDemoCancellation = cts;
            zoomDemoButton.Text = "Stop Zoom";

            // Якщо вибрано Mandelbrot на початковому зумі — наводимо на Seahorse Valley
            if (fractalSelector.SelectedIndex == 2 && zoom < 2.0)
            {
                centerReal = -0.743643887037151;
                centerImaginary = 0.131825904205330;
            }
            // Для Julia Set наближаємо прямо в центр симетрії (0, 0), де закручується спіраль

            double startZoom = zoom;
            double targetZoom = Math.Min(startZoom * 1000.0, 1e12);
            int totalFrames = 60;

            try
            {
                for (int frame = 1; frame <= totalFrames; frame++)
                {
                    cts.Token.ThrowIfCancellationRequested();

                    double progress = frame / (double)totalFrames;
                    zoom = startZoom * Math.Pow(targetZoom / startZoom, progress);
                    UpdateZoomLabel();

                    // Проміжні кадри рендеримо в 0.5x (4x швидше), фінальний 60-й кадр — у 1.0x
                    double scale = (frame < totalFrames) ? 0.5 : 1.0;

                    await RenderMandelbrotAsync(cts.Token, scale);

                    if (frame < totalFrames)
                    {
                        await Task.Delay(12, cts.Token);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Зупинено користувачем
            }
            finally
            {
                if (ReferenceEquals(zoomDemoCancellation, cts))
                {
                    zoomDemoCancellation = null;
                }
                cts.Dispose();
                if (!IsDisposed)
                {
                    zoomDemoButton.Text = "Zoom 1000x";
                }
            }
        }

        private void StopZoomDemo()
        {
            zoomDemoCancellation?.Cancel();
            zoomDemoCancellation = null;
            if (!IsDisposed)
            {
                zoomDemoButton.Text = "Zoom 1000x";
            }
        }

        // Обробник зміни параметрів
        private void Parameters_ValueChanged(object? sender, EventArgs e)
        {
            if (IsComplexFractal())
            {
                ScheduleMandelbrotRender();
            }
            else
            {
                Stopwatch sw = Stopwatch.StartNew();
                drawingPanel.Invalidate();
                drawingPanel.Update();
                sw.Stop();
                Text = $"Fractal Generator — {drawingPanel.Width}x{drawingPanel.Height} | {sw.ElapsedMilliseconds} ms";
            }
        }

        // Обробник вибору фрактала
        private void FractalSelector_SelectedIndexChanged(object? sender, EventArgs e)
        {
            StopZoomDemo();
            isSelectingArea = false;
            isDragging = false;
            drawingPanel.Cursor = Cursors.Default;

            if (fractalSelector.SelectedIndex == 2) // Mandelbrot Set
            {
                centerReal = -0.75;
                centerImaginary = 0;
                zoom = 1.0;
                UpdateZoomLabel();
            }
            else if (fractalSelector.SelectedIndex == 3) // Julia Set
            {
                centerReal = 0.0;
                centerImaginary = 0;
                zoom = 1.0;

                if (!isApplyingViperPreset)
                {
                    cRealInput.Value = -0.800M;
                    cImaginaryInput.Value = 0.156M;
                    iterationsInput.Value = 300;
                }

                UpdateZoomLabel();
            }

            UpdateControls();

            mandelbrotBitmap?.Dispose();
            mandelbrotBitmap = null;

            if (IsComplexFractal())
            {
                ScheduleMandelbrotRender();
            }
            else
            {
                CancelMandelbrotRender();
                drawingPanel.Invalidate();
            }
        }

        // Перевірка, чи вибрано фрактал на основі комплексних чисел
        private bool IsComplexFractal()
        {
            return fractalSelector.SelectedIndex == 2 ||
                   fractalSelector.SelectedIndex == 3;
        }

        // Увімкнення або вимкнення параметрів
        private void UpdateControls()
        {
            bool isTree = fractalSelector.SelectedIndex == 0;
            bool isJulia = fractalSelector.SelectedIndex == 3;
            bool isComplex = IsComplexFractal();

            depthInput.Enabled = !isComplex;

            angleInput.Enabled = isTree;
            lengthRatioInput.Enabled = isTree;

            iterationsInput.Enabled = isComplex;

            cRealInput.Enabled = isJulia;
            cImaginaryInput.Enabled = isJulia;

            zoomLabel.ForeColor = isComplex ? Color.White : Color.FromArgb(120, 135, 155);
            zoomInButton.Enabled = isComplex;
            zoomOutButton.Enabled = isComplex;
            zoomDemoButton.Enabled = isComplex;
            resetViewButton.Enabled = isComplex;
        }

        // Отримання основного кольору палітри
        private Color GetBaseColor()
        {
            switch (paletteSelector.SelectedIndex)
            {
                case 1:
                    return Color.FromArgb(30, 130, 220); // Ocean

                case 2:
                    return Color.FromArgb(230, 100, 60); // Sunset

                case 3:
                    return Color.FromArgb(150, 90, 220); // Violet

                case 4:
                    return Color.FromArgb(245, 195, 15); // Kill Bill (The Bride Yellow)

                default:
                    return Color.FromArgb(30, 180, 140); // Emerald
            }
        }

        // Отримання кольору гілки залежно від глибини
        private Color GetBranchColor(int depth, int maxDepth)
        {
            Color baseColor = GetBaseColor();

            float brightness = 0.65f + 0.35f * depth / maxDepth;

            int red = (int)(baseColor.R * brightness);
            int green = (int)(baseColor.G * brightness);
            int blue = (int)(baseColor.B * brightness);

            return Color.FromArgb(red, green, blue);
        }

        // Обробник зміни розміру панелі
        private void DrawingPanel_Resize(object? sender, EventArgs e)
        {
            if (IsComplexFractal())
            {
                ScheduleMandelbrotRender();
            }
            else
            {
                drawingPanel.Invalidate();
            }
        }

        // Передаємо фокус панелі для роботи коліщатка миші
        private void DrawingPanel_MouseEnter(object? sender, EventArgs e)
        {
            drawingPanel.Focus();
        }

        // Зміна масштабу коліщатком миші
        private void DrawingPanel_MouseWheel(object? sender, MouseEventArgs e)
        {
            if (!IsComplexFractal())
            {
                return;
            }

            StopZoomDemo();

            double oldViewHeight = 2.4 / zoom;
            double oldViewWidth =
                oldViewHeight * drawingPanel.Width / drawingPanel.Height;

            double mouseReal = centerReal +
                (e.X / (double)drawingPanel.Width - 0.5) * oldViewWidth;

            double mouseImaginary = centerImaginary +
                (0.5 - e.Y / (double)drawingPanel.Height) * oldViewHeight;

            double previousZoom = zoom;

            if (e.Delta > 0)
            {
                zoom = Math.Min(zoom * 1.5, 1000000000000.0);
            }
            else
            {
                zoom = Math.Max(zoom / 1.5, 0.1);
            }

            if (zoom == previousZoom)
            {
                return;
            }

            double newViewHeight = 2.4 / zoom;
            double newViewWidth =
                newViewHeight * drawingPanel.Width / drawingPanel.Height;

            centerReal = mouseReal -
                (e.X / (double)drawingPanel.Width - 0.5) * newViewWidth;

            centerImaginary = mouseImaginary -
                (0.5 - e.Y / (double)drawingPanel.Height) * newViewHeight;

            UpdateZoomLabel();
            ScheduleMandelbrotRender();
        }

        // Початок виділення області або переміщення
        private void DrawingPanel_MouseDown(object? sender, MouseEventArgs e)
        {
            if (!IsComplexFractal())
            {
                return;
            }

            StopZoomDemo();

            if (fractalSelector.SelectedIndex == 2 &&
                e.Button == MouseButtons.Left)
            {
                isSelectingArea = true;
                selectionStart = e.Location;
                selectionEnd = e.Location;

                drawingPanel.Capture = true;
                drawingPanel.Invalidate();
                return;
            }

            bool isMandelbrotPan = fractalSelector.SelectedIndex == 2 && e.Button == MouseButtons.Right;
            bool isJuliaPan = fractalSelector.SelectedIndex == 3 && e.Button == MouseButtons.Left;

            if (isMandelbrotPan || isJuliaPan)
            {
                isDragging = true;
                lastMousePosition = e.Location;
                drawingPanel.Cursor = Cursors.Hand;
                drawingPanel.Capture = true;
                drawingPanel.Focus();
            }
        }

        // Оновлення рамки виділення або переміщення зображення
        private void DrawingPanel_MouseMove(object? sender, MouseEventArgs e)
        {
            if (isSelectingArea)
            {
                selectionEnd = e.Location;
                drawingPanel.Invalidate();
                return;
            }

            if (!isDragging || !IsComplexFractal())
            {
                return;
            }

            double viewHeight = 2.4 / zoom;
            double viewWidth = viewHeight *
                drawingPanel.Width / drawingPanel.Height;

            int deltaX = e.X - lastMousePosition.X;
            int deltaY = e.Y - lastMousePosition.Y;

            centerReal -= deltaX * viewWidth / drawingPanel.Width;
            centerImaginary += deltaY * viewHeight / drawingPanel.Height;

            lastMousePosition = e.Location;

            drawingPanel.Invalidate();
        }

        // Завершення виділення області або переміщення зображення
        private void DrawingPanel_MouseUp(object? sender, MouseEventArgs e)
        {
            if (isSelectingArea && e.Button == MouseButtons.Left)
            {
                isSelectingArea = false;
                drawingPanel.Capture = false;

                Rectangle selection = GetSelectionRectangle();

                if (selection.Width >= 8 && selection.Height >= 8)
                {
                    ZoomToSelection(selection);
                }

                drawingPanel.Invalidate();
                return;
            }

            if (isDragging)
            {
                isDragging = false;
                drawingPanel.Capture = false;
                drawingPanel.Cursor = Cursors.Default;

                ScheduleMandelbrotRender();
            }
        }

        private Rectangle GetSelectionRectangle()
        {
            int left = Math.Min(selectionStart.X, selectionEnd.X);
            int top = Math.Min(selectionStart.Y, selectionEnd.Y);

            int right = Math.Max(selectionStart.X, selectionEnd.X);
            int bottom = Math.Max(selectionStart.Y, selectionEnd.Y);

            return Rectangle.Intersect(
                Rectangle.FromLTRB(left, top, right, bottom),
                drawingPanel.ClientRectangle);
        }

        private void ZoomToSelection(Rectangle selection)
        {
            double viewHeight = 2.4 / zoom;
            double viewWidth = viewHeight *
                drawingPanel.Width / drawingPanel.Height;

            double centerX = selection.Left + selection.Width / 2.0;
            double centerY = selection.Top + selection.Height / 2.0;

            centerReal +=
                (centerX - drawingPanel.Width / 2.0) *
                viewWidth / drawingPanel.Width;

            centerImaginary -=
                (centerY - drawingPanel.Height / 2.0) *
                viewHeight / drawingPanel.Height;

            double selectedViewWidth =
                selection.Width * viewWidth / drawingPanel.Width;

            double selectedViewHeight =
                selection.Height * viewHeight / drawingPanel.Height;

            double newViewHeight = Math.Max(
                selectedViewHeight,
                selectedViewWidth * drawingPanel.Height / drawingPanel.Width);

            double newZoom = 2.4 / newViewHeight;

            if (newZoom > zoom)
            {
                zoom = Math.Min(newZoom, 1e12);
                UpdateZoomLabel();
                ScheduleMandelbrotRender();
            }
        }

        private void UpdateZoomLabel()
        {
            zoomLabel.Text = "Zoom: " + zoom.ToString("G4") + "x";
        }

        private void ResetViewButton_Click(object? sender, EventArgs e)
        {
            StopZoomDemo();
            centerReal = fractalSelector.SelectedIndex == 3 ? 0.0 : -0.75;
            centerImaginary = 0;
            zoom = 1.0;

            UpdateZoomLabel();
            ScheduleMandelbrotRender();
        }

        private void ZoomInButton_Click(object? sender, EventArgs e)
        {
            StopZoomDemo();
            ZoomByFactor(2.0);
        }

        private void ZoomOutButton_Click(object? sender, EventArgs e)
        {
            StopZoomDemo();
            ZoomByFactor(0.5);
        }

        private void ZoomByFactor(double factor)
        {
            if (!IsComplexFractal())
            {
                return;
            }

            zoom = Math.Clamp(zoom * factor, 0.1, 1e12);

            UpdateZoomLabel();
            ScheduleMandelbrotRender();
        }

        private void ScheduleMandelbrotRender()
        {
            if (drawingPanel.Width <= 0 || drawingPanel.Height <= 0)
            {
                return;
            }

            renderTimer.Stop();

            renderVersion++;

            renderCancellation?.Cancel();
            renderCancellation = null;

            renderTimer.Start();

            drawingPanel.Invalidate();
        }

        private async void RenderTimer_Tick(object? sender, EventArgs e)
        {
            renderTimer.Stop();

            await RenderMandelbrotAsync(CancellationToken.None);
        }

        private async Task RenderMandelbrotAsync(CancellationToken externalToken, double renderScale = 1.0)
        {
            CancellationTokenSource cancellation = externalToken.CanBeCanceled
                ? CancellationTokenSource.CreateLinkedTokenSource(externalToken)
                : new CancellationTokenSource();

            renderCancellation = cancellation;

            int currentVersion = ++renderVersion;

            int panelWidth = drawingPanel.Width;
            int panelHeight = drawingPanel.Height;
            int width = Math.Max(1, (int)(panelWidth * renderScale));
            int height = Math.Max(1, (int)(panelHeight * renderScale));
            int iterations = (int)iterationsInput.Value;

            Color baseColor = GetBaseColor();

            double currentCenterReal = centerReal;
            double currentCenterImaginary = centerImaginary;
            double currentZoom = zoom;

            bool isJulia = fractalSelector.SelectedIndex == 3;

            double currentCReal = (double)cRealInput.Value;
            double currentCImaginary = (double)cImaginaryInput.Value;

            double currentViewHeight = 2.4 / currentZoom;
            double currentViewWidth =
                currentViewHeight * panelWidth / panelHeight;

            drawingPanel.Invalidate();

            Stopwatch sw = Stopwatch.StartNew();

            try
            {
                Bitmap newBitmap = await Task.Run(
                    () =>
                    {
                        if (isJulia)
                        {
                            return juliaSet.CreateBitmap(
                                width,
                                height,
                                iterations,
                                baseColor,
                                currentCenterReal,
                                currentCenterImaginary,
                                currentZoom,
                                currentCReal,
                                currentCImaginary,
                                cancellation.Token);
                        }

                        return mandelbrot.CreateBitmap(
                            width,
                            height,
                            iterations,
                            baseColor,
                            currentCenterReal,
                            currentCenterImaginary,
                            currentZoom,
                            cancellation.Token);
                    },
                    cancellation.Token);

                sw.Stop();

                if (currentVersion != renderVersion ||
                    IsDisposed ||
                    drawingPanel.IsDisposed)
                {
                    newBitmap.Dispose();
                    return;
                }

                Bitmap? oldBitmap = mandelbrotBitmap;
                mandelbrotBitmap = newBitmap;

                bitmapCenterReal = currentCenterReal;
                bitmapCenterImaginary = currentCenterImaginary;
                bitmapViewWidth = currentViewWidth;
                bitmapViewHeight = currentViewHeight;

                oldBitmap?.Dispose();

                // Виводимо час рендеру для бенчмарку в README
                Text = $"Fractal Generator — {width}x{height} | {iterations} iter | {sw.ElapsedMilliseconds} ms";
            }
            catch (OperationCanceledException)
            {
                // Скасоване обчислення не потребує обробки
            }
            catch (Exception ex)
            {
                if (!IsDisposed && currentVersion == renderVersion)
                {
                    MessageBox.Show(
                        "Rendering error: " + ex.Message,
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
            finally
            {
                if (currentVersion == renderVersion)
                {
                    if (ReferenceEquals(renderCancellation, cancellation))
                    {
                        renderCancellation = null;
                    }

                    if (!IsDisposed && !drawingPanel.IsDisposed)
                    {
                        drawingPanel.Invalidate();
                    }
                }

                cancellation.Dispose();
            }
        }

        private void CancelMandelbrotRender()
        {
            renderTimer.Stop();

            renderVersion++;

            renderCancellation?.Cancel();
            renderCancellation = null;
        }

        private void DrawingPanel_Paint(object? sender, PaintEventArgs e)
        {
            if (IsComplexFractal())
            {
                bool drewPreview = false;

                if (mandelbrotBitmap != null)
                {
                    double currentViewHeight = 2.4 / zoom;
                    double currentViewWidth =
                        currentViewHeight * drawingPanel.Width /
                        drawingPanel.Height;

                    double scaleX = bitmapViewWidth / currentViewWidth;
                    double scaleY = bitmapViewHeight / currentViewHeight;

                    if (scaleX >= 0.05 && scaleX <= 20.0 &&
                        scaleY >= 0.05 && scaleY <= 20.0)
                    {
                        float offsetX = (float)(
                            (bitmapCenterReal - centerReal) /
                            currentViewWidth * drawingPanel.Width +
                            (1 - scaleX) * drawingPanel.Width / 2);

                        float offsetY = (float)(
                            (centerImaginary - bitmapCenterImaginary) /
                            currentViewHeight * drawingPanel.Height +
                            (1 - scaleY) * drawingPanel.Height / 2);

                        float imageWidth = (float)(drawingPanel.Width * scaleX);
                        float imageHeight = (float)(drawingPanel.Height * scaleY);

                        e.Graphics.InterpolationMode =
                            InterpolationMode.HighQualityBilinear;

                        e.Graphics.DrawImage(
                            mandelbrotBitmap,
                            offsetX,
                            offsetY,
                            imageWidth,
                            imageHeight);

                        drewPreview = true;
                    }
                }

                if (!drewPreview)
                {
                    e.Graphics.Clear(GetCanvasBackgroundColor());

                    string loadingText = fractalSelector.SelectedIndex == 3
                        ? "Calculating Julia Set..."
                        : "Calculating Mandelbrot...";

                    using (SolidBrush brush = new SolidBrush(GetBaseColor()))
                    {
                        e.Graphics.DrawString(
                            loadingText,
                            Font,
                            brush,
                            15,
                            15);
                    }
                }

                if (isSelectingArea)
                {
                    Rectangle selection = GetSelectionRectangle();

                    if (selection.Width > 0 && selection.Height > 0)
                    {
                        Color selColor = GetBaseColor();
                        using (SolidBrush brush = new SolidBrush(
                            Color.FromArgb(50, selColor.R, selColor.G, selColor.B)))
                        {
                            e.Graphics.FillRectangle(brush, selection);
                        }

                        using (Pen pen = new Pen(Color.FromArgb(220, selColor.R, selColor.G, selColor.B), 2))
                        {
                            pen.DashStyle = DashStyle.Dash;
                            e.Graphics.DrawRectangle(pen, selection);
                        }
                    }
                }

                return;
            }

            RenderFractal(
                e.Graphics,
                drawingPanel.Width,
                drawingPanel.Height);
        }

        private void RenderFractal(Graphics graphics, int width, int height)
        {
            graphics.Clear(GetCanvasBackgroundColor());
            graphics.SmoothingMode = SmoothingMode.AntiAlias;

            int depth = (int)depthInput.Value;

            if (fractalSelector.SelectedIndex == 0)
            {
                float startX = width / 2;
                float startY = height - 20;

                float angle = (float)angleInput.Value;
                float lengthRatio = (float)lengthRatioInput.Value;

                DrawTree(
                    graphics,
                    startX,
                    startY,
                    130,
                    90,
                    depth,
                    angle,
                    lengthRatio,
                    depth);
            }
            else if (fractalSelector.SelectedIndex == 1)
            {
                float triangleHeight = Math.Min(
                    height - 50,
                    (width - 40) / 1.1547f);

                float triangleWidth = triangleHeight * 1.1547f;

                float centerX = width / 2;
                float topY = (height - triangleHeight) / 2;

                PointF top = new PointF(centerX, topY);

                PointF left = new PointF(
                    centerX - triangleWidth / 2,
                    topY + triangleHeight);

                PointF right = new PointF(
                    centerX + triangleWidth / 2,
                    topY + triangleHeight);

                DrawSierpinski(
                    graphics,
                    top,
                    left,
                    right,
                    depth);
            }
            else if (fractalSelector.SelectedIndex == 4)
            {
                lSystem.Draw(
                    graphics,
                    width,
                    height,
                    depth,
                    GetBaseColor());
            }
        }

        private void DrawTree(
            Graphics graphics,
            float x,
            float y,
            float length,
            float angle,
            int depth,
            float branchAngle,
            float lengthRatio,
            int maxDepth)
        {
            if (depth <= 0)
            {
                return;
            }

            double radians = angle * Math.PI / 180;

            float endX = x + length * (float)Math.Cos(radians);
            float endY = y - length * (float)Math.Sin(radians);

            Color branchColor = GetBranchColor(depth, maxDepth);

            float width = Math.Max(1, depth * 1.2f);

            using (Pen pen = new Pen(branchColor, width))
            {
                graphics.DrawLine(pen, x, y, endX, endY);
            }

            DrawTree(
                graphics,
                endX,
                endY,
                length * lengthRatio,
                angle - branchAngle,
                depth - 1,
                branchAngle,
                lengthRatio,
                maxDepth);

            DrawTree(
                graphics,
                endX,
                endY,
                length * lengthRatio,
                angle + branchAngle,
                depth - 1,
                branchAngle,
                lengthRatio,
                maxDepth);
        }

        private void DrawSierpinski(
            Graphics graphics,
            PointF top,
            PointF left,
            PointF right,
            int depth)
        {
            if (depth <= 0)
            {
                return;
            }

            PointF middleLeft = new PointF(
                (top.X + left.X) / 2,
                (top.Y + left.Y) / 2);

            PointF middleRight = new PointF(
                (top.X + right.X) / 2,
                (top.Y + right.Y) / 2);

            PointF middleBottom = new PointF(
                (left.X + right.X) / 2,
                (left.Y + right.Y) / 2);

            if (depth == 1)
            {
                using (SolidBrush brush = new SolidBrush(GetBaseColor()))
                {
                    PointF[] points = { top, left, right };
                    graphics.FillPolygon(brush, points);
                }

                return;
            }

            DrawSierpinski(
                graphics,
                top,
                middleLeft,
                middleRight,
                depth - 1);

            DrawSierpinski(
                graphics,
                middleLeft,
                left,
                middleBottom,
                depth - 1);

            DrawSierpinski(
                graphics,
                middleRight,
                middleBottom,
                right,
                depth - 1);
        }

        private async void SaveButton_Click(object? sender, EventArgs e)
        {
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Filter = "PNG Image|*.png";
                dialog.Title = "Save Fractal Image";
                dialog.FileName = "Fractal.png";

                if (dialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                saveButton.Enabled = false;

                try
                {
                    if (IsComplexFractal())
                    {
                        int width = drawingPanel.Width;
                        int height = drawingPanel.Height;
                        int iterations = (int)iterationsInput.Value;

                        Color baseColor = GetBaseColor();

                        double currentCenterReal = centerReal;
                        double currentCenterImaginary = centerImaginary;
                        double currentZoom = zoom;

                        bool isJulia = fractalSelector.SelectedIndex == 3;

                        double currentCReal = (double)cRealInput.Value;
                        double currentCImaginary = (double)cImaginaryInput.Value;

                        using (Bitmap bitmap = await Task.Run(
                            () =>
                            {
                                if (isJulia)
                                {
                                    return juliaSet.CreateBitmap(
                                        width,
                                        height,
                                        iterations,
                                        baseColor,
                                        currentCenterReal,
                                        currentCenterImaginary,
                                        currentZoom,
                                        currentCReal,
                                        currentCImaginary,
                                        CancellationToken.None);
                                }

                                return mandelbrot.CreateBitmap(
                                    width,
                                    height,
                                    iterations,
                                    baseColor,
                                    currentCenterReal,
                                    currentCenterImaginary,
                                    currentZoom,
                                    CancellationToken.None);
                            }))
                        {
                            bitmap.Save(dialog.FileName, ImageFormat.Png);
                        }
                    }
                    else
                    {
                        using (Bitmap bitmap = new Bitmap(
                            drawingPanel.Width,
                            drawingPanel.Height))
                        {
                            using (Graphics graphics = Graphics.FromImage(bitmap))
                            {
                                RenderFractal(
                                    graphics,
                                    bitmap.Width,
                                    bitmap.Height);
                            }

                            bitmap.Save(dialog.FileName, ImageFormat.Png);
                        }
                    }

                    MessageBox.Show(
                        "Image saved successfully!",
                        "Save PNG",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "Could not save image: " + ex.Message,
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
                finally
                {
                    if (!IsDisposed)
                    {
                        saveButton.Enabled = true;
                    }
                }
            }
        }

        private async Task<Bitmap> CreatePrintBitmapAsync()
        {
            int width;
            int height;

            switch (resolutionSelector.SelectedIndex)
            {
                case 1:
                    width = 7680;
                    height = 4320;
                    break;

                default:
                    width = 3840;
                    height = 2160;
                    break;
            }

            if (IsComplexFractal())
            {
                int iterations = (int)iterationsInput.Value;
                Color baseColor = GetBaseColor();

                double currentCenterReal = centerReal;
                double currentCenterImaginary = centerImaginary;
                double currentZoom = zoom;

                bool isJulia = fractalSelector.SelectedIndex == 3;

                double currentCReal = (double)cRealInput.Value;
                double currentCImaginary = (double)cImaginaryInput.Value;

                return await Task.Run(() =>
                {
                    if (isJulia)
                    {
                        return juliaSet.CreateBitmap(
                            width,
                            height,
                            iterations,
                            baseColor,
                            currentCenterReal,
                            currentCenterImaginary,
                            currentZoom,
                            currentCReal,
                            currentCImaginary,
                            CancellationToken.None);
                    }

                    return mandelbrot.CreateBitmap(
                        width,
                        height,
                        iterations,
                        baseColor,
                        currentCenterReal,
                        currentCenterImaginary,
                        currentZoom,
                        CancellationToken.None);
                });
            }

            Bitmap bitmap = new Bitmap(width, height);

            try
            {
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    RenderFractal(graphics, width, height);
                }

                return bitmap;
            }
            catch
            {
                bitmap.Dispose();
                throw;
            }
        }

        private async void SavePdfButton_Click(object? sender, EventArgs e)
        {
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Filter = "PDF Document|*.pdf";
                dialog.Title = "Save Fractal as PDF";
                dialog.FileName = "Fractal.pdf";

                if (dialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                savePdfButton.Enabled = false;
                printButton.Enabled = false;

                try
                {
                    string temporaryImage = System.IO.Path.Combine(
                        System.IO.Path.GetTempPath(),
                        Guid.NewGuid().ToString() + ".png");

                    try
                    {
                        int bmpWidth;
                        int bmpHeight;

                        using (Bitmap bitmap = await CreatePrintBitmapAsync())
                        {
                            bmpWidth = bitmap.Width;
                            bmpHeight = bitmap.Height;
                            bitmap.Save(temporaryImage, ImageFormat.Png);
                        }

                        using (PdfDocument document = new PdfDocument())
                        {
                            PdfPage page = document.AddPage();

                            bool landscape = bmpWidth >= bmpHeight;

                            double pageWidth = landscape ? 842 : 595;
                            double pageHeight = landscape ? 595 : 842;

                            page.Width = XUnit.FromPoint(pageWidth);
                            page.Height = XUnit.FromPoint(pageHeight);

                            using (XGraphics graphics = XGraphics.FromPdfPage(page))
                            using (XImage image = XImage.FromFile(temporaryImage))
                            {
                                double margin = 25;

                                double availableWidth = pageWidth - margin * 2;
                                double availableHeight = pageHeight - margin * 2;

                                double scale = Math.Min(
                                    availableWidth / bmpWidth,
                                    availableHeight / bmpHeight);

                                double imageWidth = bmpWidth * scale;
                                double imageHeight = bmpHeight * scale;

                                double x = (pageWidth - imageWidth) / 2;
                                double y = (pageHeight - imageHeight) / 2;

                                graphics.DrawImage(
                                    image,
                                    x,
                                    y,
                                    imageWidth,
                                    imageHeight);
                            }

                            document.Save(dialog.FileName);
                        }
                    }
                    finally
                    {
                        if (System.IO.File.Exists(temporaryImage))
                        {
                            System.IO.File.Delete(temporaryImage);
                        }
                    }

                    MessageBox.Show(
                        "PDF document saved successfully!",
                        "Print Mode",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "Could not create PDF: " + ex.Message,
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
                finally
                {
                    if (!IsDisposed)
                    {
                        savePdfButton.Enabled = true;
                        printButton.Enabled = true;
                    }
                }
            }
        }

        private async void PrintButton_Click(object? sender, EventArgs e)
        {
            savePdfButton.Enabled = false;
            printButton.Enabled = false;

            try
            {
                using (Bitmap bitmap = await CreatePrintBitmapAsync())
                using (PrintDocument document = new PrintDocument())
                using (PrintDialog dialog = new PrintDialog())
                {
                    document.DocumentName = "Fractal Generator";

                    document.DefaultPageSettings.Landscape =
                        bitmap.Width >= bitmap.Height;

                    document.PrintPage += (s, args) =>
                    {
                        if (args.Graphics != null)
                        {
                            Rectangle bounds = args.MarginBounds;

                            float scale = Math.Min(
                                bounds.Width / (float)bitmap.Width,
                                bounds.Height / (float)bitmap.Height);

                            int imageWidth = (int)(bitmap.Width * scale);
                            int imageHeight = (int)(bitmap.Height * scale);

                            int x = bounds.Left + (bounds.Width - imageWidth) / 2;
                            int y = bounds.Top + (bounds.Height - imageHeight) / 2;

                            args.Graphics.DrawImage(
                                bitmap,
                                x,
                                y,
                                imageWidth,
                                imageHeight);
                        }

                        args.HasMorePages = false;
                    };

                    dialog.Document = document;
                    dialog.UseEXDialog = true;

                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        document.Print();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Could not print image: " + ex.Message,
                    "Print Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                if (!IsDisposed)
                {
                    savePdfButton.Enabled = true;
                    printButton.Enabled = true;
                }
            }
        }

        private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
        {
            StopZoomDemo();
            CancelMandelbrotRender();

            renderTimer.Dispose();

            mandelbrotBitmap?.Dispose();
            mandelbrotBitmap = null;
        }
    }

    public class DoubleBufferedPanel : Panel
    {
        public DoubleBufferedPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
        }
    }
}