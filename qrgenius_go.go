// qrgenius_go.go — генератор QR-кодов с логотипом на Go (консоль + изображения)

package main

import (
	"bufio"
	"encoding/base64"
	"encoding/json"
	"fmt"
	"image"
	"image/color"
	"image/draw"
	"image/png"
	"io/ioutil"
	"os"
	"path/filepath"
	"strconv"
	"strings"

	"github.com/skip2/go-qrcode"
)

type Config struct {
	Text   string `json:"text"`
	Size   int    `json:"size"`
	Error  string `json:"error"`
	Color  string `json:"color"`
	Logo   string `json:"logo"`
}

type App struct {
	config Config
}

func NewApp() *App {
	return &App{
		config: Config{
			Text:   "https://github.com/yourname/qrgenius",
			Size:   400,
			Error:  "H",
			Color:  "#000000",
			Logo:   "",
		},
	}
}

func (a *App) loadState() {
	data, err := ioutil.ReadFile("qrgenius_state.json")
	if err != nil {
		return
	}
	var cfg Config
	err = json.Unmarshal(data, &cfg)
	if err == nil {
		a.config = cfg
	}
}

func (a *App) saveState() {
	data, _ := json.MarshalIndent(a.config, "", "  ")
	ioutil.WriteFile("qrgenius_state.json", data, 0644)
}

func (a *App) generateQR() error {
	text := a.config.Text
	if text == "" {
		return fmt.Errorf("текст не может быть пустым")
	}
	size := a.config.Size
	if size < 100 {
		size = 100
	}
	if size > 1000 {
		size = 1000
	}
	// Уровень коррекции
	level := qrcode.Highest
	switch a.config.Error {
	case "L":
		level = qrcode.Low
	case "M":
		level = qrcode.Medium
	case "Q":
		level = qrcode.High
	default:
		level = qrcode.Highest
	}
	// Генерация QR
	qr, err := qrcode.New(text, level)
	if err != nil {
		return err
	}
	img := qr.Image(size)

	// Вставка логотипа
	if a.config.Logo != "" && fileExists(a.config.Logo) {
		logoFile, err := os.Open(a.config.Logo)
		if err == nil {
			defer logoFile.Close()
			logo, _, err := image.Decode(logoFile)
			if err == nil {
				// Масштабируем логотип до 20% от размера QR
				bounds := img.Bounds()
				qrWidth := bounds.Dx()
				qrHeight := bounds.Dy()
				logoSize := int(float64(qrWidth) * 0.2)
				logo = scaleImage(logo, logoSize, logoSize)
				// Позиция в центре
				logoBounds := logo.Bounds()
				x := (qrWidth - logoBounds.Dx()) / 2
				y := (qrHeight - logoBounds.Dy()) / 2
				// Наложение с маской прозрачности
				draw.Draw(img, image.Rect(x, y, x+logoBounds.Dx(), y+logoBounds.Dy()), logo, image.Point{0, 0}, draw.Over)
			}
		}
	}

	// Сохраняем во временный файл для предпросмотра (в консоли показываем путь)
	tempFile := "qr_temp.png"
	file, err := os.Create(tempFile)
	if err != nil {
		return err
	}
	defer file.Close()
	err = png.Encode(file, img)
	if err != nil {
		return err
	}
	fmt.Printf("QR-код сгенерирован: %s\n", tempFile)
	return nil
}

func scaleImage(src image.Image, width, height int) image.Image {
	dst := image.NewRGBA(image.Rect(0, 0, width, height))
	// Простое масштабирование - можно использовать билинейную интерполяцию, но для простоты используем draw.Scale
	// Встроенного масштабирования в стандартной библиотеке нет, используем приблизительное
	// Для упрощения используем Resize из сторонней библиотеки, но мы не будем добавлять зависимости
	// Вместо этого просто используем draw с изменением размера через Nearest Neighbor
	// Реализуем простой ресайз
	srcBounds := src.Bounds()
	srcW := srcBounds.Dx()
	srcH := srcBounds.Dy()
	for y := 0; y < height; y++ {
		for x := 0; x < width; x++ {
			srcX := x * srcW / width
			srcY := y * srcH / height
			dst.Set(x, y, src.At(srcX, srcY))
		}
	}
	return dst
}

func fileExists(path string) bool {
	_, err := os.Stat(path)
	return err == nil
}

func (a *App) interactive() {
	scanner := bufio.NewScanner(os.Stdin)
	fmt.Println("📱 QRGenius — Go Edition")
	fmt.Println("Команды: set <key> <value>, generate, save, logo, exit")
	for {
		fmt.Print("> ")
		if !scanner.Scan() {
			break
		}
		line := strings.TrimSpace(scanner.Text())
		if line == "" {
			continue
		}
		parts := strings.SplitN(line, " ", 3)
		cmd := parts[0]
		switch cmd {
		case "set":
			if len(parts) < 3 {
				fmt.Println("Использование: set <key> <value> (keys: text, size, error, color)")
				continue
			}
			key := parts[1]
			val := parts[2]
			switch key {
			case "text":
				a.config.Text = val
			case "size":
				if s, err := strconv.Atoi(val); err == nil {
					a.config.Size = s
				}
			case "error":
				if val == "L" || val == "M" || val == "Q" || val == "H" {
					a.config.Error = val
				}
			case "color":
				a.config.Color = val
			case "logo":
				a.config.Logo = val
			default:
				fmt.Println("Неизвестный ключ")
			}
			fmt.Printf("Установлено: %s = %s\n", key, val)
		case "generate":
			err := a.generateQR()
			if err != nil {
				fmt.Println("Ошибка:", err)
			}
		case "save":
			a.saveState()
			fmt.Println("Настройки сохранены")
		case "logo":
			fmt.Println("Текущий логотип:", a.config.Logo)
			fmt.Print("Введите путь к логотипу (или Enter для очистки): ")
			scanner.Scan()
			path := strings.TrimSpace(scanner.Text())
			if path == "" {
				a.config.Logo = ""
			} else if fileExists(path) {
				a.config.Logo = path
			} else {
				fmt.Println("Файл не найден")
			}
		case "exit":
			a.saveState()
			fmt.Println("До свидания!")
			return
		default:
			fmt.Println("Неизвестная команда")
		}
	}
}

func main() {
	app := NewApp()
	app.loadState()
	app.interactive()
}
