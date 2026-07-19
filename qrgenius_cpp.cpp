// qrgenius_cpp.cpp — генератор QR-кодов с логотипом на C++ (Qt + qrencode)

#include <QApplication>
#include <QMainWindow>
#include <QWidget>
#include <QVBoxLayout>
#include <QHBoxLayout>
#include <QPushButton>
#include <QLabel>
#include <QLineEdit>
#include <QSpinBox>
#include <QComboBox>
#include <QColorDialog>
#include <QFileDialog>
#include <QMessageBox>
#include <QImage>
#include <QPixmap>
#include <QPainter>
#include <QBuffer>
#include <QByteArray>
#include <qrencode.h>
#include <QSettings>
#include <QScrollArea>

class QRGenius : public QMainWindow {
    Q_OBJECT
public:
    QRGenius(QWidget *parent = nullptr) : QMainWindow(parent) {
        setWindowTitle("📱 QRGenius — C++");
        resize(800, 700);
        createUI();
        loadState();
    }

private slots:
    void generateQR() {
        QString text = textEdit->text().trimmed();
        if (text.isEmpty()) {
            QMessageBox::warning(this, "Ошибка", "Введите текст или URL");
            return;
        }
        int size = sizeSpin->value();
        QColor color = colorBtn->palette().color(QPalette::Button);
        QColor bgColor = Qt::white;
        QrCodeErrorCorrectionLevel errLevel;
        switch (errorCombo->currentIndex()) {
            case 0: errLevel = QR_ECLEVEL_L; break;
            case 1: errLevel = QR_ECLEVEL_M; break;
            case 2: errLevel = QR_ECLEVEL_Q; break;
            default: errLevel = QR_ECLEVEL_H; break;
        }

        // Генерация QR-кода
        QRcode *qrcode = QRcode_encodeString(text.toUtf8().constData(), 0, errLevel, QR_MODE_8, 1);
        if (!qrcode) {
            QMessageBox::warning(this, "Ошибка", "Не удалось сгенерировать QR-код");
            return;
        }

        // Создаём QImage
        int qrSize = qrcode->width;
        double scale = (double)size / qrSize;
        QImage img(size, size, QImage::Format_ARGB32);
        img.fill(bgColor);
        QPainter painter(&img);
        painter.setPen(Qt::NoPen);
        painter.setBrush(color);

        for (int y = 0; y < qrSize; ++y) {
            for (int x = 0; x < qrSize; ++x) {
                if (qrcode->data[y * qrSize + x] & 1) {
                    int px = x * scale;
                    int py = y * scale;
                    int w = qCeil(scale);
                    int h = qCeil(scale);
                    painter.drawRect(px, py, w, h);
                }
            }
        }
        QRcode_free(qrcode);

        // Вставляем логотип
        if (!logoPath.isEmpty()) {
            QImage logo(logoPath);
            if (!logo.isNull()) {
                int logoSize = size * 0.2;
                logo = logo.scaled(logoSize, logoSize, Qt::KeepAspectRatio, Qt::SmoothTransformation);
                int x = (size - logo.width()) / 2;
                int y = (size - logo.height()) / 2;
                painter.drawImage(x, y, logo);
            }
        }

        currentQR = img;
        QPixmap pixmap = QPixmap::fromImage(img);
        // Масштабируем для предпросмотра
        int previewSize = 400;
        pixmap = pixmap.scaled(previewSize, previewSize, Qt::KeepAspectRatio, Qt::SmoothTransformation);
        previewLabel->setPixmap(pixmap);
        previewLabel->setAlignment(Qt::AlignCenter);
        statusLabel->setText("QR-код сгенерирован, размер: " + QString::number(size) + "x" + QString::number(size));
    }

    void saveQR() {
        if (currentQR.isNull()) {
            QMessageBox::warning(this, "Предупреждение", "Сначала сгенерируйте QR-код");
            return;
        }
        QString filename = QFileDialog::getSaveFileName(this, "Сохранить QR-код", "", "PNG (*.png);;SVG (*.svg)");
        if (filename.isEmpty()) return;
        if (filename.endsWith(".svg", Qt::CaseInsensitive)) {
            // SVG не поддерживаем, сохраняем PNG
            filename.replace(".svg", ".png");
        }
        currentQR.save(filename);
        statusLabel->setText("Сохранено в " + filename);
    }

    void chooseColor() {
        QColor color = QColorDialog::getColor(Qt::black, this, "Выберите цвет QR-кода");
        if (color.isValid()) {
            QString style = QString("background-color: %1;").arg(color.name());
            colorBtn->setStyleSheet(style);
        }
    }

    void chooseLogo() {
        QString path = QFileDialog::getOpenFileName(this, "Выберите логотип", "", "Images (*.png *.jpg *.jpeg *.bmp *.gif)");
        if (!path.isEmpty()) {
            logoPath = path;
            logoLabel->setText(QFileInfo(path).fileName());
            statusLabel->setText("Логотип выбран: " + QFileInfo(path).fileName());
        }
    }

    void clearLogo() {
        logoPath.clear();
        logoLabel->setText("Не выбран");
        statusLabel->setText("Логотип очищен");
    }

    void reset() {
        textEdit->clear();
        textEdit->setText("https://github.com/yourname/qrgenius");
        sizeSpin->setValue(400);
        errorCombo->setCurrentIndex(3); // H
        colorBtn->setStyleSheet("background-color: black;");
        clearLogo();
        currentQR = QImage();
        previewLabel->clear();
        statusLabel->setText("Сброшено");
    }

protected:
    void closeEvent(QCloseEvent *event) override {
        saveState();
        QMainWindow::closeEvent(event);
    }

private:
    QLineEdit *textEdit;
    QSpinBox *sizeSpin;
    QComboBox *errorCombo;
    QPushButton *colorBtn;
    QLabel *logoLabel;
    QLabel *previewLabel;
    QLabel *statusLabel;
    QString logoPath;
    QImage currentQR;

    void createUI() {
        QWidget *central = new QWidget(this);
        setCentralWidget(central);
        QVBoxLayout *mainLayout = new QVBoxLayout(central);

        // Параметры
        QGridLayout *grid = new QGridLayout;
        grid->addWidget(new QLabel("Текст / URL:"), 0, 0);
        textEdit = new QLineEdit;
        textEdit->setText("https://github.com/yourname/qrgenius");
        grid->addWidget(textEdit, 0, 1, 1, 3);

        grid->addWidget(new QLabel("Размер:"), 1, 0);
        sizeSpin = new QSpinBox;
        sizeSpin->setRange(100, 1000);
        sizeSpin->setSingleStep(50);
        sizeSpin->setValue(400);
        grid->addWidget(sizeSpin, 1, 1);

        grid->addWidget(new QLabel("Коррекция:"), 1, 2);
        errorCombo = new QComboBox;
        errorCombo->addItems({"L", "M", "Q", "H"});
        errorCombo->setCurrentIndex(3);
        grid->addWidget(errorCombo, 1, 3);

        grid->addWidget(new QLabel("Цвет:"), 2, 0);
        colorBtn = new QPushButton;
        colorBtn->setFixedSize(30, 30);
        colorBtn->setStyleSheet("background-color: black;");
        connect(colorBtn, &QPushButton::clicked, this, &QRGenius::chooseColor);
        grid->addWidget(colorBtn, 2, 1);

        grid->addWidget(new QLabel("Логотип:"), 3, 0);
        logoLabel = new QLabel("Не выбран");
        grid->addWidget(logoLabel, 3, 1);
        QPushButton *logoBtn = new QPushButton("Выбрать логотип");
        connect(logoBtn, &QPushButton::clicked, this, &QRGenius::chooseLogo);
        grid->addWidget(logoBtn, 3, 2);
        QPushButton *clearLogoBtn = new QPushButton("Очистить");
        connect(clearLogoBtn, &QPushButton::clicked, this, &QRGenius::clearLogo);
        grid->addWidget(clearLogoBtn, 3, 3);

        mainLayout->addLayout(grid);

        // Кнопки действий
        QHBoxLayout *btnLayout = new QHBoxLayout;
        QPushButton *genBtn = new QPushButton("Сгенерировать");
        genBtn->setStyleSheet("background-color: green; color: white;");
        connect(genBtn, &QPushButton::clicked, this, &QRGenius::generateQR);
        btnLayout->addWidget(genBtn);
        QPushButton *saveBtn = new QPushButton("Сохранить");
        connect(saveBtn, &QPushButton::clicked, this, &QRGenius::saveQR);
        btnLayout->addWidget(saveBtn);
        QPushButton *resetBtn = new QPushButton("Сбросить");
        connect(resetBtn, &QPushButton::clicked, this, &QRGenius::reset);
        btnLayout->addWidget(resetBtn);
        mainLayout->addLayout(btnLayout);

        // Предпросмотр
        QGroupBox *previewGroup = new QGroupBox("Предпросмотр");
        QVBoxLayout *previewLayout = new QVBoxLayout(previewGroup);
        previewLabel = new QLabel;
        previewLabel->setAlignment(Qt::AlignCenter);
        previewLabel->setMinimumSize(400, 400);
        previewLabel->setStyleSheet("border: 1px solid gray; background-color: white;");
        previewLayout->addWidget(previewLabel);
        mainLayout->addWidget(previewGroup);

        // Статус
        statusLabel = new QLabel("Готов");
        mainLayout->addWidget(statusLabel);

        // Горячие клавиши
        genBtn->setShortcut(QKeySequence("Ctrl+G"));
        saveBtn->setShortcut(QKeySequence("Ctrl+S"));
    }

    void saveState() {
        QSettings settings("MyApp", "QRGenius");
        settings.setValue("text", textEdit->text());
        settings.setValue("size", sizeSpin->value());
        settings.setValue("error", errorCombo->currentIndex());
        settings.setValue("color", colorBtn->palette().color(QPalette::Button).name());
        settings.setValue("logo", logoPath);
    }

    void loadState() {
        QSettings settings("MyApp", "QRGenius");
        textEdit->setText(settings.value("text", "https://github.com/yourname/qrgenius").toString());
        sizeSpin->setValue(settings.value("size", 400).toInt());
        errorCombo->setCurrentIndex(settings.value("error", 3).toInt());
        QString color = settings.value("color", "#000000").toString();
        colorBtn->setStyleSheet(QString("background-color: %1;").arg(color));
        logoPath = settings.value("logo", "").toString();
        if (!logoPath.isEmpty() && QFile::exists(logoPath)) {
            logoLabel->setText(QFileInfo(logoPath).fileName());
        }
    }
};

int main(int argc, char *argv[]) {
    QApplication app(argc, argv);
    QRGenius w;
    w.show();
    return app.exec();
}

#include "qrgenius_cpp.moc"
