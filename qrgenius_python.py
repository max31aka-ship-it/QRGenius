# qrgenius_python.py — генератор QR-кодов с логотипом на Python (Tkinter)

import tkinter as tk
from tkinter import ttk, filedialog, messagebox, colorchooser
import qrcode
from qrcode.image.styledpil import StyledPilImage
from qrcode.image.styles.moduledrawers import RoundedModuleDrawer
from PIL import Image, ImageDraw, ImageTk
import os
import json

class QRGenius:
    def __init__(self, root):
        self.root = root
        self.root.title("📱 QRGenius — Python")
        self.root.geometry("800x700")
        self.filename = "qrgenius_state.json"
        self.logo_path = None
        self.qr_image = None
        self.preview_image = None
        self.load_state()
        self.create_widgets()
        self.root.protocol("WM_DELETE_WINDOW", self.save_state)

    def create_widgets(self):
        # Основные параметры
        main_frame = tk.Frame(self.root)
        main_frame.pack(padx=10, pady=10, fill=tk.BOTH, expand=True)

        # Верхняя панель с параметрами
        param_frame = tk.LabelFrame(main_frame, text="Настройки QR-кода", padx=5, pady=5)
        param_frame.pack(fill=tk.X, pady=5)

        # Текст
        tk.Label(param_frame, text="Текст / URL:").grid(row=0, column=0, sticky=tk.W, pady=2)
        self.text_entry = tk.Entry(param_frame, width=50)
        self.text_entry.grid(row=0, column=1, columnspan=3, sticky=tk.W, pady=2)
        self.text_entry.insert(0, "https://github.com/yourname/qrgenius")

        # Размер
        tk.Label(param_frame, text="Размер:").grid(row=1, column=0, sticky=tk.W, pady=2)
        self.size_var = tk.IntVar(value=400)
        tk.Spinbox(param_frame, from_=100, to=1000, increment=50, textvariable=self.size_var, width=8).grid(row=1, column=1, sticky=tk.W, pady=2)

        # Уровень коррекции
        tk.Label(param_frame, text="Коррекция:").grid(row=1, column=2, sticky=tk.W, pady=2, padx=10)
        self.error_var = tk.StringVar(value="H")
        error_combo = ttk.Combobox(param_frame, textvariable=self.error_var, values=["L", "M", "Q", "H"], width=5)
        error_combo.grid(row=1, column=3, sticky=tk.W, pady=2)

        # Цвет переднего плана
        tk.Label(param_frame, text="Цвет:").grid(row=2, column=0, sticky=tk.W, pady=2)
        self.color_var = tk.StringVar(value="#000000")
        tk.Entry(param_frame, textvariable=self.color_var, width=10).grid(row=2, column=1, sticky=tk.W, pady=2)
        tk.Button(param_frame, text="Выбрать", command=self.choose_color).grid(row=2, column=2, pady=2)

        # Логотип
        tk.Label(param_frame, text="Логотип:").grid(row=3, column=0, sticky=tk.W, pady=2)
        self.logo_label = tk.Label(param_frame, text="Не выбран", fg="gray")
        self.logo_label.grid(row=3, column=1, sticky=tk.W, pady=2)
        tk.Button(param_frame, text="Выбрать логотип", command=self.choose_logo).grid(row=3, column=2, pady=2)
        tk.Button(param_frame, text="Очистить логотип", command=self.clear_logo).grid(row=3, column=3, pady=2)

        # Кнопки действий
        action_frame = tk.Frame(main_frame)
        action_frame.pack(fill=tk.X, pady=5)
        tk.Button(action_frame, text="Сгенерировать", command=self.generate_qr, bg="green", fg="white").pack(side=tk.LEFT, padx=5)
        tk.Button(action_frame, text="Сохранить", command=self.save_qr).pack(side=tk.LEFT, padx=5)
        tk.Button(action_frame, text="Сбросить", command=self.reset).pack(side=tk.LEFT, padx=5)

        # Предпросмотр
        self.preview_frame = tk.LabelFrame(main_frame, text="Предпросмотр", padx=5, pady=5)
        self.preview_frame.pack(fill=tk.BOTH, expand=True, pady=5)
        self.preview_label = tk.Label(self.preview_frame, text="QR-код появится здесь", bg="white", width=50, height=25)
        self.preview_label.pack(fill=tk.BOTH, expand=True)

        # Статус
        self.status = tk.Label(self.root, text="Готов", anchor=tk.W)
        self.status.pack(fill=tk.X, padx=10, pady=5)

        # Горячие клавиши
        self.root.bind("<Control-g>", lambda e: self.generate_qr())
        self.root.bind("<Control-s>", lambda e: self.save_qr())

    def choose_color(self):
        color = colorchooser.askcolor(title="Выберите цвет QR-кода", color=self.color_var.get())
        if color:
            self.color_var.set(color[1])

    def choose_logo(self):
        path = filedialog.askopenfilename(filetypes=[("Изображения", "*.png *.jpg *.jpeg *.bmp *.gif")])
        if path:
            self.logo_path = path
            self.logo_label.config(text=os.path.basename(path), fg="black")
            self.status.config(text=f"Логотип выбран: {os.path.basename(path)}")

    def clear_logo(self):
        self.logo_path = None
        self.logo_label.config(text="Не выбран", fg="gray")
        self.status.config(text="Логотип очищен")

    def generate_qr(self):
        text = self.text_entry.get().strip()
        if not text:
            messagebox.showerror("Ошибка", "Введите текст или URL")
            return
        try:
            size = self.size_var.get()
            error_level = self.error_var.get()
            color = self.color_var.get()
        except:
            messagebox.showerror("Ошибка", "Некорректные параметры")
            return

        # Генерация QR
        qr = qrcode.QRCode(
            version=None,
            error_correction=qrcode.constants.ERROR_CORRECT_H if error_level == "H" else
                            qrcode.constants.ERROR_CORRECT_Q if error_level == "Q" else
                            qrcode.constants.ERROR_CORRECT_M if error_level == "M" else
                            qrcode.constants.ERROR_CORRECT_L,
            box_size=10,
            border=4,
        )
        qr.add_data(text)
        qr.make(fit=True)

        # Создаём изображение с логотипом
        img = qr.make_image(fill_color=color, back_color="white")
        img = img.convert("RGBA")

        # Вставляем логотип, если он есть
        if self.logo_path and os.path.exists(self.logo_path):
            try:
                logo = Image.open(self.logo_path).convert("RGBA")
                # Масштабируем логотип до 20% от размера QR
                qr_width, qr_height = img.size
                logo_size = int(min(qr_width, qr_height) * 0.2)
                logo.thumbnail((logo_size, logo_size), Image.Resampling.LANCZOS)

                # Позиция в центре
                logo_x = (qr_width - logo.width) // 2
                logo_y = (qr_height - logo.height) // 2

                # Создаём маску для прозрачности
                mask = logo.split()[3] if logo.mode == "RGBA" else None
                img.paste(logo, (logo_x, logo_y), mask)
            except Exception as e:
                self.status.config(text=f"Ошибка вставки логотипа: {e}")

        # Масштабируем до нужного размера
        if img.size != (size, size):
            img = img.resize((size, size), Image.Resampling.LANCZOS)

        self.qr_image = img
        self.show_preview(img)
        self.status.config(text=f"QR-код сгенерирован, размер: {size}x{size}")

    def show_preview(self, img):
        # Масштабируем для отображения в окне
        preview_size = 400
        preview = img.copy()
        preview.thumbnail((preview_size, preview_size), Image.Resampling.LANCZOS)
        self.preview_image = ImageTk.PhotoImage(preview)
        self.preview_label.config(image=self.preview_image, text="")
        self.preview_label.image = self.preview_image

    def save_qr(self):
        if self.qr_image is None:
            messagebox.showwarning("Предупреждение", "Сначала сгенерируйте QR-код")
            return
        filename = filedialog.asksaveasfilename(defaultextension=".png", filetypes=[("PNG", "*.png"), ("SVG", "*.svg")])
        if filename:
            if filename.lower().endswith(".svg"):
                # SVG не поддерживается в этой библиотеке, конвертируем в PNG
                self.qr_image.save(filename.replace(".svg", ".png"), "PNG")
                self.status.config(text=f"Сохранено как PNG (SVG не поддерживается)")
            else:
                self.qr_image.save(filename, "PNG")
                self.status.config(text=f"Сохранено в {filename}")

    def reset(self):
        self.text_entry.delete(0, tk.END)
        self.text_entry.insert(0, "https://github.com/yourname/qrgenius")
        self.size_var.set(400)
        self.error_var.set("H")
        self.color_var.set("#000000")
        self.clear_logo()
        self.qr_image = None
        self.preview_label.config(image="", text="QR-код появится здесь")
        self.status.config(text="Сброшено")

    def save_state(self):
        data = {
            "text": self.text_entry.get(),
            "size": self.size_var.get(),
            "error": self.error_var.get(),
            "color": self.color_var.get(),
            "logo": self.logo_path
        }
        with open(self.filename, 'w') as f:
            json.dump(data, f)

    def load_state(self):
        if os.path.exists(self.filename):
            with open(self.filename, 'r') as f:
                data = json.load(f)
                self.text_entry.delete(0, tk.END)
                self.text_entry.insert(0, data.get("text", ""))
                self.size_var.set(data.get("size", 400))
                self.error_var.set(data.get("error", "H"))
                self.color_var.set(data.get("color", "#000000"))
                logo = data.get("logo")
                if logo and os.path.exists(logo):
                    self.logo_path = logo
                    self.logo_label.config(text=os.path.basename(logo), fg="black")

if __name__ == "__main__":
    root = tk.Tk()
    app = QRGenius(root)
    root.mainloop()
