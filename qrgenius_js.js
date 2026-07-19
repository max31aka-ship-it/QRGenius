// qrgenius_js.js — генератор QR-кодов с логотипом на JavaScript (Node.js + readline)

const fs = require('fs');
const readline = require('readline');
const QRCode = require('qrcode');
const { createCanvas, loadImage } = require('canvas');

const rl = readline.createInterface({
    input: process.stdin,
    output: process.stdout,
    prompt: '> '
});

class App {
    constructor() {
        this.config = {
            text: 'https://github.com/yourname/qrgenius',
            size: 400,
            error: 'H',
            color: '#000000',
            logo: ''
        };
        this.stateFile = 'qrgenius_state.json';
        this.loadState();
    }

    loadState() {
        try {
            const data = fs.readFileSync(this.stateFile, 'utf8');
            const cfg = JSON.parse(data);
            Object.assign(this.config, cfg);
        } catch (e) {}
    }

    saveState() {
        fs.writeFileSync(this.stateFile, JSON.stringify(this.config, null, 2));
    }

    async generateQR() {
        const text = this.config.text.trim();
        if (!text) {
            console.log('Текст не может быть пустым');
            return;
        }
        const size = Math.min(1000, Math.max(100, this.config.size));
        const errorLevel = this.config.error.toLowerCase();

        // Генерируем QR-код
        const canvas = createCanvas(size, size);
        const ctx = canvas.getContext('2d');

        // Сначала генерируем QR как изображение
        const qrData = await QRCode.toDataURL(text, {
            errorCorrectionLevel: errorLevel,
            width: size,
            margin: 2,
            color: {
                dark: this.config.color,
                light: '#ffffff'
            }
        });

        // Рисуем QR на canvas
        const qrImage = await loadImage(qrData);
        ctx.drawImage(qrImage, 0, 0, size, size);

        // Вставляем логотип
        if (this.config.logo && fs.existsSync(this.config.logo)) {
            try {
                const logo = await loadImage(this.config.logo);
                const logoSize = size * 0.2;
                const x = (size - logoSize) / 2;
                const y = (size - logoSize) / 2;
                ctx.drawImage(logo, x, y, logoSize, logoSize);
            } catch (e) {
                console.log('Ошибка вставки логотипа:', e.message);
            }
        }

        // Сохраняем как PNG
        const outFile = 'qr_temp.png';
        const buffer = canvas.toBuffer('image/png');
        fs.writeFileSync(outFile, buffer);
        console.log(`QR-код сгенерирован: ${outFile}`);
    }

    interactive() {
        console.log('📱 QRGenius — JavaScript Edition');
        console.log('Команды: set <key> <value>, generate, save, logo, exit');
        rl.prompt();

        rl.on('line', async (line) => {
            const parts = line.trim().split(' ');
            const cmd = parts[0];
            if (!cmd) { rl.prompt(); return; }
            switch (cmd) {
                case 'set':
                    if (parts.length < 3) {
                        console.log('Использование: set <key> <value> (keys: text, size, error, color)');
                        break;
                    }
                    const key = parts[1];
                    const val = parts.slice(2).join(' ');
                    switch (key) {
                        case 'text': this.config.text = val; break;
                        case 'size': 
                            const s = parseInt(val);
                            if (!isNaN(s)) this.config.size = s;
                            break;
                        case 'error':
                            if (['L','M','Q','H'].includes(val.toUpperCase())) this.config.error = val.toUpperCase();
                            break;
                        case 'color': this.config.color = val; break;
                        default: console.log('Неизвестный ключ');
                    }
                    console.log(`Установлено: ${key} = ${val}`);
                    break;
                case 'generate':
                    await this.generateQR();
                    break;
                case 'save':
                    this.saveState();
                    console.log('Настройки сохранены');
                    break;
                case 'logo':
                    console.log(`Текущий логотип: ${this.config.logo}`);
                    rl.question('Введите путь к логотипу (или Enter для очистки): ', (path) => {
                        if (path.trim() === '') {
                            this.config.logo = '';
                            console.log('Логотип очищен');
                        } else if (fs.existsSync(path.trim())) {
                            this.config.logo = path.trim();
                            console.log(`Логотип установлен: ${this.config.logo}`);
                        } else {
                            console.log('Файл не найден');
                        }
                        rl.prompt();
                    });
                    break;
                case 'exit':
                    this.saveState();
                    console.log('До свидания!');
                    rl.close();
                    return;
                default:
                    console.log('Неизвестная команда');
            }
            rl.prompt();
        }).on('close', () => {
            process.exit(0);
        });
    }
}

const app = new App();
app.interactive();
