📱 QRGenius — генератор QR-кодов с логотипом
Профессиональный генератор QR-кодов с поддержкой вставки логотипа, настройки цвета, размера, уровня коррекции ошибок и сохранения в PNG/SVG.
Реализован на 7 языках программирования для демонстрации различных подходов к генерации QR-кодов и работе с графикой.

https://img.shields.io/github/repo-size/yourname/qrgenius
https://img.shields.io/github/stars/yourname/qrgenius?style=social
https://img.shields.io/badge/License-MIT-blue.svg

🧠 Концепция
QRGenius — это мощный генератор QR-кодов, который позволяет:

✅ Генерировать QR-коды из любого текста или URL.

✅ Вставлять логотип в центр QR-кода (PNG, JPG, SVG).

✅ Настраивать цвет переднего и заднего плана.

✅ Выбирать размер от 100 до 1000 пикселей.

✅ Задавать уровень коррекции ошибок (L, M, Q, H).

✅ Сохранять в PNG или SVG (в зависимости от библиотеки).

✅ Предпросмотр в GUI-версиях.

✅ Пакетная генерация (в некоторых версиях).

✅ Поддержка Unicode (кириллица, эмодзи).

✅ Горячие клавиши в GUI (Ctrl+G — генерировать, Ctrl+S — сохранить).

🚀 Как запустить
Каждая версия использует популярные библиотеки для работы с QR-кодами. Установите зависимости и запустите:

bash
# Python (qrcode + Pillow)
pip install qrcode[pil] pillow
python qrgenius_python.py

# C++ (Qt + qrencode)
# Требуется сборка с qrencode (sudo apt install libqrencode-dev)
qmake && make
./qrgenius_cpp

# Java (ZXing Core + JavaFX для GUI)
javac -cp .:zxing-core.jar qrgenius_java.java && java qrgenius_java

# C# (QRCoder)
dotnet add package QRCoder
dotnet run

# Go (go-qrcode)
go get github.com/skip2/go-qrcode
go run qrgenius_go.go

# Rust (qrcode crate)
cargo build --release && ./target/release/qrgenius_rs

# JavaScript (Node.js + qrcode)
npm install qrcode
node qrgenius_js.js
🧩 Пример использования (GUI-версия)
text
+---------------------------------------------+
|  📱 QRGenius                                  |
|  Текст: https://github.com/yourname/qrgenius |
|  Размер: 400  |  Уровень: H  |  Цвет: #000000|
|  Логотип: [Выбрать] (logo.png)               |
|  [Сгенерировать] [Сохранить] [Очистить]      |
|  [ Предпросмотр QR-кода с логотипом ]        |
+---------------------------------------------+
📦 Содержимое репозитория
Файл	Язык	Особенности
qrgenius_python.py	Python	Tkinter GUI, предпросмотр, выбор цвета, вставка логотипа
qrgenius_cpp.cpp	C++	Qt, встроенный предпросмотр, SVG-экспорт
qrgenius_java.java	Java	Swing + ZXing, предпросмотр, редактирование
qrgenius_cs.cs	C#	WPF + QRCoder, перетаскивание логотипа, настройки
qrgenius_go.go	Go	консоль, генерация с логотипом, сохранение в PNG
qrgenius_rs.rs	Rust	консоль, генерация SVG и PNG, вставка логотипа
qrgenius_js.js	JavaScript	Node.js + qrcode, пакетная генерация, сохранение
🔮 Расширенные функции
SVG-экспорт — векторный формат для масштабирования.

Пакетная обработка — генерация QR-кодов из CSV-файла (опционально).

Создание "красивого" QR — закругление модулей, градиент (в планах).

📜 Лицензия
MIT — свободно используйте, модифицируйте и распространяйте.

🤝 Вклад
Приветствуются пул-реквесты с улучшениями, новыми языками и поддержкой мобильных платформ.

⭐ Если проект помогает вам создавать QR-коды — поставьте звёздочку!
