// qrgenius_rs.rs — генератор QR-кодов с логотипом на Rust (консоль + изображения)

extern crate qrcode;
extern crate image;

use image::{ImageBuffer, Rgba, RgbaImage, GenericImageView, DynamicImage};
use qrcode::QrCode;
use qrcode::types::QrErrorCorrectionLevel;
use std::fs::File;
use std::io::{self, Write, BufRead};
use std::path::Path;
use std::process;
use serde::{Deserialize, Serialize};
use serde_json;

#[derive(Serialize, Deserialize, Clone)]
struct Config {
    text: String,
    size: u32,
    error: String,
    color: String, // hex, например "#000000"
    logo: String,
}

impl Default for Config {
    fn default() -> Self {
        Config {
            text: "https://github.com/yourname/qrgenius".to_string(),
            size: 400,
            error: "H".to_string(),
            color: "#000000".to_string(),
            logo: "".to_string(),
        }
    }
}

struct App {
    config: Config,
    state_file: String,
}

impl App {
    fn new() -> Self {
        App {
            config: Config::default(),
            state_file: "qrgenius_state.json".to_string(),
        }
    }

    fn load(&mut self) {
        if let Ok(data) = std::fs::read_to_string(&self.state_file) {
            if let Ok(cfg) = serde_json::from_str(&data) {
                self.config = cfg;
            }
        }
    }

    fn save(&self) {
        if let Ok(data) = serde_json::to_string_pretty(&self.config) {
            let _ = std::fs::write(&self.state_file, data);
        }
    }

    fn generate_qr(&self) -> Result<(), String> {
        let text = &self.config.text;
        if text.is_empty() {
            return Err("Текст не может быть пустым".to_string());
        }
        let size = self.config.size.max(100).min(1000);
        let level = match self.config.error.as_str() {
            "L" => QrErrorCorrectionLevel::L,
            "M" => QrErrorCorrectionLevel::M,
            "Q" => QrErrorCorrectionLevel::Q,
            _ => QrErrorCorrectionLevel::H,
        };
        // Генерация QR
        let code = QrCode::with_error_correction_level(text, level)
            .map_err(|e| format!("Ошибка генерации QR: {}", e))?;
        let qr_img = code.render::<Rgba<u8>>().build();

        // Вставка логотипа
        let mut img = qr_img;
        if !self.config.logo.is_empty() && Path::new(&self.config.logo).exists() {
            if let Ok(logo_img) = image::open(&self.config.logo) {
                let (qr_w, qr_h) = img.dimensions();
                let logo_size = ((qr_w.min(qr_h)) as f32 * 0.2) as u32;
                let logo = logo_img.resize(logo_size, logo_size, image::imageops::FilterType::Lanczos3);
                let (logo_w, logo_h) = logo.dimensions();
                let x = (qr_w - logo_w) / 2;
                let y = (qr_h - logo_h) / 2;
                // Наложение с прозрачностью
                image::imageops::overlay(&mut img, &logo, x, y);
            }
        }

        // Сохраняем во временный файл
        let temp_file = "qr_temp.png";
        img.save(temp_file).map_err(|e| format!("Ошибка сохранения: {}", e))?;
        println!("QR-код сгенерирован: {}", temp_file);
        Ok(())
    }

    fn interactive(&mut self) {
        let stdin = io::stdin();
        let mut reader = stdin.lock();
        println!("📱 QRGenius — Rust Edition");
        println!("Команды: set <key> <value>, generate, save, logo, exit");
        loop {
            print!("> ");
            io::stdout().flush().unwrap();
            let mut line = String::new();
            if reader.read_line(&mut line).is_err() { break; }
            let line = line.trim();
            if line.is_empty() { continue; }
            let parts: Vec<&str> = line.splitn(3, ' ').collect();
            if parts.is_empty() { continue; }
            match parts[0] {
                "set" => {
                    if parts.len() < 3 {
                        println!("Использование: set <key> <value> (keys: text, size, error, color)");
                        continue;
                    }
                    let key = parts[1];
                    let val = parts[2];
                    match key {
                        "text" => self.config.text = val.to_string(),
                        "size" => {
                            if let Ok(s) = val.parse::<u32>() {
                                self.config.size = s;
                            }
                        }
                        "error" => {
                            if val == "L" || val == "M" || val == "Q" || val == "H" {
                                self.config.error = val.to_string();
                            }
                        }
                        "color" => self.config.color = val.to_string(),
                        "logo" => self.config.logo = val.to_string(),
                        _ => println!("Неизвестный ключ"),
                    }
                    println!("Установлено: {} = {}", key, val);
                }
                "generate" => {
                    if let Err(e) = self.generate_qr() {
                        println!("Ошибка: {}", e);
                    }
                }
                "save" => {
                    self.save();
                    println!("Настройки сохранены");
                }
                "logo" => {
                    println!("Текущий логотип: {}", self.config.logo);
                    print!("Введите путь к логотипу (или Enter для очистки): ");
                    io::stdout().flush().unwrap();
                    let mut path = String::new();
                    reader.read_line(&mut path).unwrap();
                    let path = path.trim();
                    if path.is_empty() {
                        self.config.logo.clear();
                    } else if Path::new(path).exists() {
                        self.config.logo = path.to_string();
                    } else {
                        println!("Файл не найден");
                    }
                }
                "exit" => {
                    self.save();
                    println!("До свидания!");
                    break;
                }
                _ => println!("Неизвестная команда"),
            }
        }
    }
}

fn main() {
    let mut app = App::new();
    app.load();
    app.interactive();
}
