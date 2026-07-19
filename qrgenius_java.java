// qrgenius_java.java — генератор QR-кодов с логотипом на Java (Swing + ZXing)

import javax.swing.*;
import javax.swing.event.*;
import java.awt.*;
import java.awt.event.*;
import java.awt.image.BufferedImage;
import java.io.*;
import javax.imageio.ImageIO;
import com.google.zxing.*;
import com.google.zxing.common.BitMatrix;
import com.google.zxing.qrcode.QRCodeWriter;
import com.google.zxing.qrcode.decoder.ErrorCorrectionLevel;
import java.util.Hashtable;

public class QRGeniusJava extends JFrame {
    private static final String STATE_FILE = "qrgenius_state.json";
    private JTextField textField;
    private JSpinner sizeSpinner;
    private JComboBox<String> errorCombo;
    private JButton colorBtn;
    private JLabel logoLabel, previewLabel, statusLabel;
    private String logoPath = null;
    private BufferedImage currentQR = null;
    private Color qrColor = Color.BLACK;

    public QRGeniusJava() {
        setTitle("📱 QRGenius — Java");
        setSize(800, 700);
        setDefaultCloseOperation(EXIT_ON_CLOSE);
        setLayout(new BorderLayout());
        loadState();
        createUI();
    }

    private void createUI() {
        // Параметры
        JPanel paramPanel = new JPanel(new GridBagLayout());
        GridBagConstraints gbc = new GridBagConstraints();
        gbc.insets = new Insets(5,5,5,5);
        gbc.anchor = GridBagConstraints.WEST;

        gbc.gridx = 0; gbc.gridy = 0;
        paramPanel.add(new JLabel("Текст / URL:"), gbc);
        textField = new JTextField(30);
        textField.setText("https://github.com/yourname/qrgenius");
        gbc.gridx = 1; gbc.gridwidth = 3;
        paramPanel.add(textField, gbc);
        gbc.gridwidth = 1;

        gbc.gridx = 0; gbc.gridy = 1;
        paramPanel.add(new JLabel("Размер:"), gbc);
        sizeSpinner = new JSpinner(new SpinnerNumberModel(400, 100, 1000, 50));
        gbc.gridx = 1;
        paramPanel.add(sizeSpinner, gbc);

        gbc.gridx = 2;
        paramPanel.add(new JLabel("Коррекция:"), gbc);
        errorCombo = new JComboBox<>(new String[]{"L", "M", "Q", "H"});
        errorCombo.setSelectedIndex(3);
        gbc.gridx = 3;
        paramPanel.add(errorCombo, gbc);

        gbc.gridx = 0; gbc.gridy = 2;
        paramPanel.add(new JLabel("Цвет:"), gbc);
        colorBtn = new JButton();
        colorBtn.setBackground(Color.BLACK);
        colorBtn.setPreferredSize(new Dimension(30, 30));
        colorBtn.addActionListener(e -> {
            Color c = JColorChooser.showDialog(this, "Выберите цвет", qrColor);
            if (c != null) {
                qrColor = c;
                colorBtn.setBackground(c);
            }
        });
        gbc.gridx = 1;
        paramPanel.add(colorBtn, gbc);

        gbc.gridx = 2;
        paramPanel.add(new JLabel("Логотип:"), gbc);
        logoLabel = new JLabel("Не выбран");
        gbc.gridx = 3;
        paramPanel.add(logoLabel, gbc);

        gbc.gridx = 0; gbc.gridy = 3;
        JButton logoBtn = new JButton("Выбрать логотип");
        logoBtn.addActionListener(e -> chooseLogo());
        gbc.gridx = 1;
        paramPanel.add(logoBtn, gbc);
        JButton clearLogoBtn = new JButton("Очистить");
        clearLogoBtn.addActionListener(e -> clearLogo());
        gbc.gridx = 2;
        paramPanel.add(clearLogoBtn, gbc);

        add(paramPanel, BorderLayout.NORTH);

        // Кнопки действий
        JPanel btnPanel = new JPanel();
        JButton genBtn = new JButton("Сгенерировать");
        genBtn.setBackground(Color.GREEN);
        genBtn.setForeground(Color.WHITE);
        genBtn.addActionListener(e -> generateQR());
        btnPanel.add(genBtn);
        JButton saveBtn = new JButton("Сохранить");
        saveBtn.addActionListener(e -> saveQR());
        btnPanel.add(saveBtn);
        JButton resetBtn = new JButton("Сбросить");
        resetBtn.addActionListener(e -> reset());
        btnPanel.add(resetBtn);
        add(btnPanel, BorderLayout.CENTER);

        // Предпросмотр
        JPanel previewPanel = new JPanel(new BorderLayout());
        previewPanel.setBorder(BorderFactory.createTitledBorder("Предпросмотр"));
        previewLabel = new JLabel("QR-код появится здесь", SwingConstants.CENTER);
        previewLabel.setPreferredSize(new Dimension(400, 400));
        previewLabel.setBorder(BorderFactory.createLineBorder(Color.GRAY));
        previewPanel.add(previewLabel, BorderLayout.CENTER);
        add(previewPanel, BorderLayout.SOUTH);

        // Статус
        statusLabel = new JLabel("Готов");
        add(statusLabel, BorderLayout.SOUTH);

        // Hotkeys
        getRootPane().registerKeyboardAction(e -> generateQR(),
                KeyStroke.getKeyStroke(KeyEvent.VK_G, InputEvent.CTRL_MASK), JComponent.WHEN_IN_FOCUSED_WINDOW);
        getRootPane().registerKeyboardAction(e -> saveQR(),
                KeyStroke.getKeyStroke(KeyEvent.VK_S, InputEvent.CTRL_MASK), JComponent.WHEN_IN_FOCUSED_WINDOW);
    }

    private void generateQR() {
        String text = textField.getText().trim();
        if (text.isEmpty()) {
            JOptionPane.showMessageDialog(this, "Введите текст или URL");
            return;
        }
        int size = (int) sizeSpinner.getValue();
        ErrorCorrectionLevel errLevel;
        switch (errorCombo.getSelectedIndex()) {
            case 0: errLevel = ErrorCorrectionLevel.L; break;
            case 1: errLevel = ErrorCorrectionLevel.M; break;
            case 2: errLevel = ErrorCorrectionLevel.Q; break;
            default: errLevel = ErrorCorrectionLevel.H; break;
        }
        try {
            QRCodeWriter writer = new QRCodeWriter();
            Hashtable<EncodeHintType, Object> hints = new Hashtable<>();
            hints.put(EncodeHintType.ERROR_CORRECTION, errLevel);
            BitMatrix matrix = writer.encode(text, BarcodeFormat.QR_CODE, size, size, hints);

            BufferedImage img = new BufferedImage(size, size, BufferedImage.TYPE_INT_RGB);
            for (int x = 0; x < size; x++) {
                for (int y = 0; y < size; y++) {
                    img.setRGB(x, y, matrix.get(x, y) ? qrColor.getRGB() : Color.WHITE.getRGB());
                }
            }

            // Вставляем логотип
            if (logoPath != null && !logoPath.isEmpty()) {
                try {
                    BufferedImage logo = ImageIO.read(new File(logoPath));
                    if (logo != null) {
                        int logoSize = (int)(size * 0.2);
                        Image scaledLogo = logo.getScaledInstance(logoSize, logoSize, Image.SCALE_SMOOTH);
                        BufferedImage logoImg = new BufferedImage(logoSize, logoSize, BufferedImage.TYPE_INT_ARGB);
                        Graphics2D g = logoImg.createGraphics();
                        g.drawImage(scaledLogo, 0, 0, null);
                        g.dispose();
                        int x = (size - logoSize) / 2;
                        int y = (size - logoSize) / 2;
                        Graphics2D g2 = img.createGraphics();
                        g2.drawImage(logoImg, x, y, null);
                        g2.dispose();
                    }
                } catch (Exception e) {
                    statusLabel.setText("Ошибка вставки логотипа: " + e.getMessage());
                }
            }

            currentQR = img;
            // Предпросмотр
            ImageIcon icon = new ImageIcon(img.getScaledInstance(400, 400, Image.SCALE_SMOOTH));
            previewLabel.setIcon(icon);
            previewLabel.setText("");
            statusLabel.setText("QR-код сгенерирован, размер: " + size + "x" + size);
        } catch (Exception e) {
            JOptionPane.showMessageDialog(this, "Ошибка генерации: " + e.getMessage());
        }
    }

    private void saveQR() {
        if (currentQR == null) {
            JOptionPane.showMessageDialog(this, "Сначала сгенерируйте QR-код");
            return;
        }
        JFileChooser chooser = new JFileChooser();
        chooser.setFileFilter(new javax.swing.filechooser.FileNameExtensionFilter("PNG", "png"));
        if (chooser.showSaveDialog(this) == JFileChooser.APPROVE_OPTION) {
            File file = chooser.getSelectedFile();
            try {
                ImageIO.write(currentQR, "png", file);
                statusLabel.setText("Сохранено в " + file.getName());
            } catch (IOException e) {
                JOptionPane.showMessageDialog(this, "Ошибка сохранения: " + e.getMessage());
            }
        }
    }

    private void chooseLogo() {
        JFileChooser chooser = new JFileChooser();
        chooser.setFileFilter(new javax.swing.filechooser.FileNameExtensionFilter("Images", "png", "jpg", "jpeg", "bmp", "gif"));
        if (chooser.showOpenDialog(this) == JFileChooser.APPROVE_OPTION) {
            logoPath = chooser.getSelectedFile().getAbsolutePath();
            logoLabel.setText(chooser.getSelectedFile().getName());
            statusLabel.setText("Логотип выбран: " + chooser.getSelectedFile().getName());
        }
    }

    private void clearLogo() {
        logoPath = null;
        logoLabel.setText("Не выбран");
        statusLabel.setText("Логотип очищен");
    }

    private void reset() {
        textField.setText("https://github.com/yourname/qrgenius");
        sizeSpinner.setValue(400);
        errorCombo.setSelectedIndex(3);
        qrColor = Color.BLACK;
        colorBtn.setBackground(Color.BLACK);
        clearLogo();
        currentQR = null;
        previewLabel.setIcon(null);
        previewLabel.setText("QR-код появится здесь");
        statusLabel.setText("Сброшено");
    }

    private void loadState() {
        try (BufferedReader br = new BufferedReader(new FileReader(STATE_FILE))) {
            String line = br.readLine();
            if (line != null) {
                // Упрощённый парсинг
                // Для простоты не будем парсить, оставим дефолтные значения
            }
        } catch (IOException e) {}
    }

    private void saveState() {
        // Не сохраняем для простоты
    }

    public static void main(String[] args) throws Exception {
        UIManager.setLookAndFeel(UIManager.getSystemLookAndFeelClassName());
        SwingUtilities.invokeLater(() -> new QRGeniusJava().setVisible(true));
    }
}
