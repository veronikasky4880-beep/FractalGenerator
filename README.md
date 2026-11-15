# Fractal Generator

An interactive desktop fractal visualization and exploration studio built with **C#** and **Windows Forms (.NET)**. The application renders both recursive geometric fractals and complex-plane escape-time fractals with multi-threaded pixel generation, smooth deep-zoom animations, and high-resolution (4K / 8K) print and PDF export.

---

## Features

### 1. Supported Fractals
* **Fractal Tree** — Recursive branching tree with customizable recursion depth (`0–10`), branch angle (`10°–60°`), and length decay ratio (`0.30–0.90`).
* **Sierpinski Triangle** — Recursive subdivision equilateral triangle with depth-controlled detail.
* **Mandelbrot Set** — Escape-time visualization ($z_{n+1} = z_n^2 + c$) on the complex plane with smooth color gradients and up to `1000` iterations.
* **Julia Set** — Interactive complex dynamics ($z_{n+1} = z_n^2 + c$) with real-time adjustment of the complex constant $c = a + bi$.
* **L-System** — Lindenmayer string-rewriting fractal plant generator rendered via turtle graphics.

### 2. Personal Twist: *Viper Spiral* & *Kill Bill* Palette
* **Kill Bill Theme** — A high-contrast cinema-inspired color palette combining deep obsidian blacks with vibrant Bride Yellow (`#F5C30F`) gradients.
* **Viper Spiral Preset** — A one-click preset button that configures the **Julia Set** at $c = -0.745 + 0.113i$ (`350` iterations, `1.25x` initial zoom) in the **Kill Bill** palette, revealing twin interlocking vortex spirals.

### 3. Interactive Exploration & Deep Zoom
* **Mouse Wheel Zoom** — Cursor-centered zooming up to $10^{12}\times$ magnification.
* **Box Zoom (Mandelbrot)** — Left-click and drag to draw a selection rectangle and zoom directly into any region.
* **Click-and-Drag Panning** — Right-click drag on the Mandelbrot Set (or left-click drag on the Julia Set) to pan smoothly across the complex plane with instant bilinear preview scaling.
* **Zoom 1000x Demo** — Automated 60-frame cinematic deep-zoom animation (targeting *Seahorse Valley* in Mandelbrot mode or the spiral core in Julia mode) using dynamic `0.5x` intermediate frame scaling followed by a full-resolution `1.0x` final frame.

### 4. High-Resolution Export & Print Mode
* **PNG Export** — Save the current viewport directly to a lossless `.png` image.
* **4K & 8K Rendering** — Off-screen high-resolution rendering at `3840 × 2160` (4K) or `7680 × 4320` (8K).
* **PDF & Direct Print** — Generate auto-oriented A4 PDF documents via **PDFsharp** or send fractals directly to a system printer.

---

## Controls & Shortcuts

| Action | Control / Gesture |
| :--- | :--- |
| **Switch Fractal** | `Fractal` dropdown (`Fractal Tree`, `Sierpinski Triangle`, `Mandelbrot Set`, `Julia Set`, `L-System`) |
| **Change Color Theme** | `Palette` dropdown (`Emerald`, `Ocean`, `Sunset`, `Violet`, `Kill Bill`) |
| **Viper Spiral Easter Egg** | Click the yellow **Viper Spiral** button |
| **Zoom In / Out** | Mouse Wheel, or `2x` / `0.5x` buttons |
| **Area Selection Zoom** | Left-click + Drag on **Mandelbrot Set** |
| **Pan Canvas** | Right-click + Drag (**Mandelbrot**) / Left-click + Drag (**Julia**) |
| **Cinematic Deep Zoom** | Click **Zoom 1000x** (click **Stop Zoom** anytime to halt and render in full quality) |
| **Reset Coordinates** | Click **Reset View** |
| **Export** | **Save PNG**, **Save PDF**, or **Print** (`4K` / `8K` selector) |

---

## Architecture & Performance

* **Asynchronous Rendering Pipeline** — Complex-plane fractals (`Mandelbrot` and `JuliaSet`) are computed off the UI thread using `Task.Run` with `CancellationToken` support and a `60 ms` debounce timer so rapid zooming and parameter adjustments never freeze the interface.
* **Zero-Flicker Double Buffering** — Custom `DoubleBufferedPanel` with real-time affine preview scaling displays the previous frame immediately while high-resolution pixels calculate in the background.
* **Live Performance Telemetry** — The window title bar displays real-time canvas resolution, iteration count, and exact render time in milliseconds (`ms`) via `Stopwatch`.

### Render Benchmarks
*(Measured on local machine at default window size `1150 × 760`)*

| Fractal Mode | Resolution | Iterations / Depth | Render Time |
| :--- | :--- | :--- | :--- |
| Fractal Tree | `1150 × 760` | Depth `6` | `~2 ms` |
| Sierpinski Triangle | `1150 × 760` | Depth `6` | `~3 ms` |
| Mandelbrot Set | `1150 × 760` | `300` iterations | `~35 ms` |
| Julia Set (*Viper Spiral*) | `1150 × 760` | `350` iterations | `~40 ms` |

---

## Getting Started

### Prerequisites
* [.NET 8.0 SDK](https://dotnet.microsoft.com/download) (or .NET 6.0+ for Windows)
* Windows OS (Windows Forms)
* NuGet Package: `PDFsharp`

FractalGenerator/
├── Form1.cs            # Main UI, input handling, async render loop, PDF/PNG export
├── Mandelbrot.cs       # Escape-time Mandelbrot Set bitmap generator
├── JuliaSet.cs         # Escape-time Julia Set bitmap generator
├── LSystem.cs          # Lindenmayer system string expansion & turtle renderer
└── Program.cs          # Application entry point

# Fractal Generator

Інтерактивний десктопний застосунок для візуалізації та дослідження фракталів, створений на **C#** та **Windows Forms (.NET)**. Програма будує як рекурсивні геометричні фрактали, так і фрактали на комплексній площині з багатопотоковою генерацією пікселів, плавною анімацією глибокого масштабування та експортом у 4K / 8K для друку й PDF.

---

## Основні можливості

### 1. Підтримувані фрактали
* **Fractal Tree (Фрактальне дерево)** — рекурсивне розгалужене дерево з налаштуванням глибини рекурсії (`0–10`), кута розгалуження (`10°–60°`) та коефіцієнта зменшення довжини гілок (`0.30–0.90`).
* **Sierpinski Triangle (Трикутник Серпінського)** — рекурсивний рівносторонній трикутник із регульованою глибиною деталізації.
* **Mandelbrot Set (Множина Мандельброта)** — візуалізація часу втечі ($z_{n+1} = z_n^2 + c$) на комплексній площині з плавними градієнтами та підтримкою до `1000` ітерацій.
* **Julia Set (Множина Жюліа)** — інтерактивна комплексна динаміка ($z_{n+1} = z_n^2 + c$) зі зміною дійсної та уявної частин константи $c = a + bi$ у реальному часі.
* **L-System (L-система)** — генератор фрактальних рослин на основі L-систем Лінденмаєра з побудовою через черепашачу графіку.

### 2. Особлива фішка: *Viper Spiral* та палітра *Kill Bill*
* **Палітра Kill Bill** — висококонтрастна кінематографічна кольорова схема, що поєднує глибокий чорний фон із яскравими жовтими градієнтами (`#F5C30F`).
* **Пресет Viper Spiral** — кнопка швидкого запуску, яка одним натисканням налаштовує **множину Жюліа** у точці $c = -0.745 + 0.113i$ (`350` ітерацій, стартовий зум `1.25x`) у палітрі **Kill Bill**, відкриваючи симетричні подвійні вихрові спіралі.

### 3. Інтерактивна навігація та Deep Zoom
* **Масштабування коліщатком миші** — плавний зум відносно курсора до збільшення у $10^{12}$ разів.
* **Виділення області (Mandelbrot)** — затисніть ліву кнопку миші та виділіть прямокутну ділянку, щоб наблизити її на весь екран.
* **Перетягування полотна (Pan)** — переміщення по комплексній площині правою кнопкою миші для *Mandelbrot Set* або лівою кнопкою для *Julia Set* із миттєвим білінійним попереднім переглядом.
* **Анімація Zoom 1000x** — автоматична 60-кадрова анімація глибокого занурення (у *Seahorse Valley* для Мандельброта або в центр спіралі для Жюліа) з динамічним рендерингом проміжних кадрів у масштабі `0.5x` та фінального кадру у повній якості `1.0x`.

### 4. Експорт у високій роздільній здатності та друк
* **Збереження у PNG** — миттєвий експорт поточного зображення без втрати якості.
* **Рендеринг у 4K та 8K** — фонова побудова зображень у роздільній здатності `3840 × 2160` (4K) або `7680 × 4320` (8K).
* **Збереження у PDF та друк** — створення PDF-документів формату A4 з автоматичною орієнтацією сторінки через бібліотеку **PDFsharp** або прямий вивід на принтер.

---

## Керування

| Дія | Елемент керування / Жест |
| :--- | :--- |
| **Вибір фрактала** | Випадаючий список `Fractal` (`Fractal Tree`, `Sierpinski Triangle`, `Mandelbrot Set`, `Julia Set`, `L-System`) |
| **Зміна кольорової палітри** | Випадаючий список `Palette` (`Emerald`, `Ocean`, `Sunset`, `Violet`, `Kill Bill`) |
| **Пресет Viper Spiral** | Натиснути жовту кнопку **Viper Spiral** |
| **Збільшити / Зменшити масштаб** | Коліщатко миші або кнопки `2x` / `0.5x` |
| **Зум за виділеною областю** | Ліва кнопка миші + перетягування на **Mandelbrot Set** |
| **Переміщення по фракталу** | Права кнопка миші + перетягування (**Mandelbrot**) / Ліва кнопка + перетягування (**Julia**) |
| **Демо-анімація глибокого зуму** | Натиснути **Zoom 1000x** (натисніть **Stop Zoom**, щоб зупинити та дорендерити в повній якості) |
| **Скинути вид** | Натиснути **Reset View** |
| **Експорт та друк** | **Save PNG**, **Save PDF** або **Print** (з вибором `4K` / `8K`) |

---

## Архітектура та продуктивність

* **Асинхронний конвеєр рендерингу** — обчислення фракталів комплексної площини (`Mandelbrot` та `JuliaSet`) виконуються у фоновому потоці через `Task.Run` із підтримкою скасування (`CancellationToken`) та таймером затримки `60 мс`, завдяки чому інтерфейс залишається чуйним під час швидкого зуму.
* **Подвійна буферизація без мерехтіння** — власний компонент `DoubleBufferedPanel` миттєво масштабує попередній кадр під час навігації, поки нові пікселі обчислюються у фоні.
* **Телеметрія продуктивності** — у заголовку вікна в реальному часі відображається поточна роздільна здатність полотна, кількість ітерацій та точний час рендерингу в мілісекундах (`ms`) за допомогою `Stopwatch`.

### Бенчмарки рендерингу
*(Виміряно локально при стандартному розмірі вікна `1150 × 760`)*

| Режим фрактала | Роздільна здатність | Ітерації / Глибина | Час рендерингу |
| :--- | :--- | :--- | :--- |
| Fractal Tree | `1150 × 760` | Глибина `6` | `~2 ms` |
| Sierpinski Triangle | `1150 × 760` | Глибина `6` | `~3 ms` |
| Mandelbrot Set | `1150 × 760` | `300` ітерацій | `~35 ms` |
| Julia Set (*Viper Spiral*) | `1150 × 760` | `350` ітерацій | `~40 ms` |

---

## Запуск проєкту

### Вимоги
* [.NET 8.0 SDK](https://dotnet.microsoft.com/download) (або .NET 6.0+ для Windows)
* ОС Windows (Windows Forms)
* NuGet-пакет: `PDFsharp`

### Збірка та запуск
```bash
git clone [https://github.com/your-username/FractalGenerator.git](https://github.com/your-username/FractalGenerator.git)
cd FractalGenerator
dotnet restore
dotnet run
