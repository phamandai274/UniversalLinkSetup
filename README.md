# UniversalLinkSetup
# 🚀 Universal Link Setup

**Универсальный инструмент для управления символическими ссылками, запуском программ и настройками реестра Windows**

[![Version](https://img.shields.io/badge/version-1.0.0-blue.svg)](https://github.com/phamandai274/UniversalLinkSetup)
[![.NET](https://img.shields.io/badge/.NET-Framework%204.0+-purple.svg)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/platform-Windows-lightgrey.svg)](https://www.microsoft.com/windows)

---

## 📋 Содержание

- [О проекте](#-о-проекте)
- [Возможности](#-возможности)
- [Требования](#-требования)
- [Установка](#-установка)
- [Быстрый старт](#-быстрый-старт)
- [Использование](#-использование)
- [Конфигурация](#-конфигурация)
- [Примеры](#-примеры)
- [FAQ](#-faq)
- [Устранение проблем](#-устранение-проблем)
- [Разработка](#-разработка)
- [Вклад](#-вклад)
- [Лицензия](#-лицензия)

---

## 🎯 О проекте

**Universal Link Setup** — мощный инструмент для автоматизации настройки игр и приложений на Windows. Программа создает символические ссылки, управляет реестром, запускает пред-программы и автоматически сохраняет обновления.

### 🌟 Ключевые особенности

- ✅ **Символические ссылки** — Автоматическое создание symlink для игр и приложений
- ✅ **Управление реестром** — Изменение записей реестра Windows
- ✅ **Пред-программы** — Запуск VPN, Discord и других программ перед лаунчером
- ✅ **Кастомные аргументы** — Профили для разных платформ (Steam, Epic, Rockstar)
- ✅ **Автоматический бэкап** — Копирование обновлений через 3 минуты
- ✅ **Импорт .reg файлов** — Автоматическая загрузка настроек реестра
- ✅ **Службы Windows** — Создание и управление службами

---

## 💡 Возможности

### 🔗 Символические ссылки

Создавайте symlink для игр и приложений в любых папках:
- Игровые папки (`GameLinks`)
- Системные папки (`AppDataLocal`, `AppDataRoaming`, `ProgramData`)
- Пользовательские папки (`CustomLinks`)

### ⚙️ Управление реестром

Изменяйте записи реестра Windows:
- Строки (`REG_SZ`)
- Числа (`REG_DWORD`, `REG_QWORD`)
- Бинарные данные (`REG_BINARY`)
- Многострочные значения (`REG_MULTI_SZ`)
- Расширяемые строки (`REG_EXPAND_SZ`)

### 🎮 Кастомные профили

Создавайте профили для разных платформ:
```cmd
UniversalLinkSetup.exe -steam    # Профиль Steam
UniversalLinkSetup.exe -epic     # Профиль Epic Games
UniversalLinkSetup.exe -rockstar # Профиль Rockstar
```

### 🔄 Автоматический бэкап

Программа автоматически проверяет обновления через 3 минуты и копирует их в целевые папки.

---

## 📦 Требования

### Системные требования

- **ОС:** Windows 7/8/10/11 (x64/x86)
- **Права:** Администратор (для создания symlink)
- **.NET Framework:** 4.0 или выше
- **Место на диске:** ~500 KB

### Рекомендуемые

- **ОС:** Windows 10/11
- **.NET Framework:** 4.8
- **PowerShell:** 5.1+

---

## 📥 Установка

### Способ 1: Готовый exe (рекомендуется)

1. **Скачайте** последний релиз:
   ```
   https://github.com/phamandai274/UniversalLinkSetup/releases/latest
   ```

2. **Распакуйте** архив в любую папку:
   ```
   C:\Tools\UniversalLinkSetup\
   ```

3. **Создайте** `config.ini` (см. [Конфигурация](#-конфигурация))

### Способ 2: Компиляция из исходников

1. **Клонируйте** репозиторий:
   ```bash
   git clone https://github.com/phamandai274/UniversalLinkSetup.git
   cd UniversalLinkSetup
   ```

2. **Скомпилируйте** с помощью `csc`:
   ```cmd
   csc /target:winexe /out:UniversalLinkSetup.exe UniversalLinkSetup.cs
   ```

3. **Или используйте Visual Studio:**
   - Откройте проект
   - Build → Build Solution
   - Найдите exe в `bin\Release\`

---

## 🚀 Быстрый старт

### 1. Базовая настройка

Создайте `config.ini` рядом с exe:

```ini
[Settings]
GameFolder=C:\Program Files\Rockstar Games
LauncherExe=%AppDataLocal%\FiveM\FiveM.exe
PreExe=C:\Tools\VPN.exe -connect

[GameLinks]
Grand Theft Auto V=D:\Games\GTA5

[ProgramLinks]
AppDataLocal=FiveM
```

### 2. Первый запуск

Запустите программу **от администратора**:

```cmd
UniversalLinkSetup.exe
```

Программа:
1. ✅ Создаст символические ссылки
2. ✅ Запустит VPN (PreExe)
3. ✅ Запустит FiveM

### 3. С отладкой

Для просмотра логов:

```cmd
UniversalLinkSetup.exe -console
```

---

## 📖 Использование

### Аргументы командной строки

| Аргумент | Описание |
|----------|----------|
| *(нет)* | Полная настройка + запуск лаунчера |
| `-install` | Только установка (без запуска лаунчера) |
| `-console` | Показать консоль с логами |
| `-debug` | Режим отладки (аналог `-console`) |
| `-[имя]` | Кастомный профиль из `[Arg_*]` |

### Примеры команд

**Базовый запуск:**
```cmd
UniversalLinkSetup.exe
```

**Только установка:**
```cmd
UniversalLinkSetup.exe -install
```

**С консолью:**
```cmd
UniversalLinkSetup.exe -console
```

**Профиль Steam:**
```cmd
UniversalLinkSetup.exe -steam
```

**Несколько аргументов:**
```cmd
UniversalLinkSetup.exe -steam -console
```

**Передача аргументов лаунчеру:**
```cmd
UniversalLinkSetup.exe --server 123.45.67.89:30120
```

---

## ⚙️ Конфигурация

### 📄 Структура config.ini

```ini
[Settings]              # Основные настройки
[GameLinks]            # Игровые ссылки
[ProgramLinks]         # Программные ссылки
[CustomLinks]          # Пользовательские ссылки
[Registry]             # Реестр Windows
[Services]             # Службы Windows
[Arg_*]                # Кастомные профили
```

---

### 🔧 Секция [Settings]

Основные настройки программы.

```ini
[Settings]
GameFolder=C:\Program Files\Rockstar Games
LauncherExe=%AppDataLocal%\FiveM\FiveM.exe
PreExe=C:\Tools\VPN.exe -connect
PreExe2=C:\Tools\Discord.exe --minimized
LinkedGameFolder=D:\AllGames
BackupFolders=Grand Theft Auto V, AppDataLocal, AppDataRoaming
```

#### Параметры:

| Параметр | Описание | Пример |
|----------|----------|--------|
| `GameFolder` | Папка для игр | `C:\Program Files\Rockstar Games` |
| `LauncherExe` | Путь к лаунчеру | `%AppDataLocal%\FiveM\FiveM.exe` |
| `PreExe` | Программа перед лаунчером | `C:\Tools\VPN.exe -connect` |
| `PreExe2`, `PreExe3`... | Дополнительные программы | `C:\Tools\Discord.exe` |
| `LinkedGameFolder` | Symlink для всей папки игр | `D:\AllGames` |
| `BackupFolders` | Папки для автобэкапа | `GTA5, AppDataLocal` |

#### Поддерживаемые переменные:

| Переменная | Путь |
|------------|------|
| `%AppDataLocal%` | `C:\Users\[user]\AppData\Local` |
| `%AppDataRoaming%` | `C:\Users\[user]\AppData\Roaming` |
| `%ProgramData%` | `C:\ProgramData` |
| `%ProgramFiles%` | `C:\Program Files` |
| `%ProgramFilesx86%` | `C:\Program Files (x86)` |
| `%UserProfile%` | `C:\Users\[user]` |
| `%Desktop%` | Рабочий стол |
| `%Documents%` | Документы |
| `%Windows%` | `C:\Windows` |
| `%System32%` | `C:\Windows\System32` |
| `%Temp%` | Временная папка |

---

### 🎮 Секция [GameLinks]

Создает символические ссылки внутри `GameFolder`.

```ini
[GameLinks]
Grand Theft Auto V=D:\Games\GTA5
Red Dead Redemption 2=D:\Games\RDR2
Cyberpunk 2077=E:\Steam\Cyberpunk2077
```

**Результат:**
```
C:\Program Files\Rockstar Games\Grand Theft Auto V -> D:\Games\GTA5
C:\Program Files\Rockstar Games\Red Dead Redemption 2 -> D:\Games\RDR2
C:\Program Files\Rockstar Games\Cyberpunk 2077 -> E:\Steam\Cyberpunk2077
```

---

### 💻 Секция [ProgramLinks]

Создает ссылки в системных папках.

```ini
[ProgramLinks]
# AppData\Local
AppDataLocal=FiveM
AppDataLocal2=DigitalEntitlements
AppDataLocal3=EpicGamesLauncher

# AppData\Roaming
AppDataRoaming=Discord
AppDataRoaming2=Telegram Desktop

# ProgramData
ProgramData=Epic\EpicGamesLauncher

# Program Files
ProgramFiles=Common Files\Services

# Program Files (x86)
ProgramFilesx86=Steam
```

**Результат:**
```
%AppDataLocal%\FiveM -> [exe]\Data\AppDataLocal\FiveM
%AppDataLocal%\DigitalEntitlements -> [exe]\Data\AppDataLocal\DigitalEntitlements
%AppDataRoaming%\Discord -> [exe]\Data\AppDataRoaming\Discord
```

---

### 🔗 Секция [CustomLinks]

Создает ссылки в произвольных местах.

```ini
[CustomLinks]
MyFolder=C:\CustomLocation\MyFolder
GameSaves=%Documents%\My Games\Saves
```

**Результат:**
```
C:\CustomLocation\MyFolder -> [exe]\Data\MyFolder
C:\Users\[user]\Documents\My Games\Saves -> [exe]\Data\GameSaves
```

---

### 📝 Секция [Registry]

Изменяет записи реестра Windows.

**Новый формат (v1.0):**
```ini
[Registry]
# Формат: ИмяПараметра=Ключ|Тип|Данные

# Строка (REG_SZ)
InstallPath=HKCU\Software\MyGame|REG_SZ|%ProgramFiles%\MyGame
Version=HKCU\Software\MyGame|REG_SZ|1.0.0

# Число 32-бит (REG_DWORD)
Enabled=HKCU\Software\MyGame|REG_DWORD|1
MaxPlayers=HKCU\Software\MyGame|REG_DWORD|64

# Число 64-бит (REG_QWORD)
MaxSize=HKLM\Software\MyApp|REG_QWORD|9223372036854775807

# Бинарные данные (REG_BINARY)
BinaryData=HKCU\Software\MyApp|REG_BINARY|01FF2A3B

# Многострочное значение (REG_MULTI_SZ)
SearchPaths=HKCU\Software\MyApp|REG_MULTI_SZ|C:\Path1|D:\Path2

# Расширяемая строка (REG_EXPAND_SZ)
TempDir=HKCU\Software\MyApp|REG_EXPAND_SZ|%Temp%\MyApp
```

#### Типы данных:

| Тип | Описание | Пример |
|-----|----------|--------|
| `REG_SZ` | Строка | `Hello` |
| `REG_DWORD` | 32-бит число | `1` |
| `REG_QWORD` | 64-бит число | `9223372036854775807` |
| `REG_BINARY` | Бинарные данные | `01FF2A3B` |
| `REG_MULTI_SZ` | Многострочная строка | `Path1|Path2` |
| `REG_EXPAND_SZ` | Расширяемая строка | `%Temp%\App` |

#### Сокращения ключей:

| Полное | Короткое |
|--------|----------|
| `HKEY_CURRENT_USER` | `HKCU` |
| `HKEY_LOCAL_MACHINE` | `HKLM` |
| `HKEY_CLASSES_ROOT` | `HKCR` |
| `HKEY_USERS` | `HKU` |
| `HKEY_CURRENT_CONFIG` | `HKCC` |

---

### 🛠️ Секция [Services]

Создает и управляет службами Windows.

```ini
[Services]
# Формат: ИмяСлужбы=Путь|ТипЗапуска|АвтоСтарт

MyService=C:\Services\service.exe|auto|true
BackupService=C:\Services\backup.exe|demand|false
```

#### Параметры:

| Поле | Описание | Значения |
|------|----------|----------|
| Путь | Путь к exe службы | Полный путь |
| ТипЗапуска | Тип запуска | `auto`, `demand`, `disabled` |
| АвтоСтарт | Запустить сразу | `true`, `false` |

---

### 🎯 Секции [Arg_*]

Создают кастомные профили для разных платформ.

```ini
[Arg_steam]
PreExe=C:\Tools\SteamVPN.exe -connect
PreExe2=C:\Tools\SteamHelper.exe
Link_Grand Theft Auto V=D:\SteamGames\GTA5
Reg_Platform=HKCU\Software\MyGame|REG_SZ|steam
Reg_SteamPath=HKCU\Software\Valve\Steam|REG_SZ|D:\Steam

[Arg_epic]
PreExe=C:\Tools\EpicVPN.exe
Link_Grand Theft Auto V=D:\EpicGames\GTA5
Reg_Platform=HKCU\Software\MyGame|REG_SZ|epic

[Arg_rockstar]
PreExe=C:\Tools\RockstarVPN.exe
Link_Grand Theft Auto V=D:\RockstarGames\GTA5
Reg_Platform=HKCU\Software\MyGame|REG_SZ|rockstar
```

**Использование:**
```cmd
UniversalLinkSetup.exe -steam
UniversalLinkSetup.exe -epic
UniversalLinkSetup.exe -rockstar
```

#### Параметры:

| Параметр | Описание |
|----------|----------|
| `PreExe`, `PreExe2`... | Пред-программы для профиля |
| `Link_*` | Ссылки (переопределяют `[GameLinks]`) |
| `Reg_*` | Реестр (формат: `Ключ|Тип|Данные`) |

---

## 📚 Примеры

### Пример 1: Базовая конфигурация GTA 5

```ini
[Settings]
GameFolder=C:\Program Files\Rockstar Games
LauncherExe=%AppDataLocal%\FiveM\FiveM.exe

[GameLinks]
Grand Theft Auto V=D:\Games\GTA5

[ProgramLinks]
AppDataLocal=FiveM
```

**Запуск:**
```cmd
UniversalLinkSetup.exe
```

---

### Пример 2: С VPN и Discord

```ini
[Settings]
GameFolder=C:\Program Files\Rockstar Games
LauncherExe=%AppDataLocal%\FiveM\FiveM.exe
PreExe=C:\Tools\VPN.exe -connect
PreExe2=C:\Tools\Discord.exe --minimized

[GameLinks]
Grand Theft Auto V=D:\Games\GTA5

[ProgramLinks]
AppDataLocal=FiveM
AppDataLocal2=DigitalEntitlements
AppDataRoaming=Discord
```

**Запуск:**
```cmd
UniversalLinkSetup.exe
```

**Порядок выполнения:**
1. VPN запускается
2. Discord запускается (minimized)
3. FiveM запускается

---

### Пример 3: Несколько платформ

```ini
[Settings]
GameFolder=C:\Program Files\Rockstar Games
LauncherExe=%AppDataLocal%\FiveM\FiveM.exe
BackupFolders=Grand Theft Auto V, AppDataLocal

[GameLinks]
Grand Theft Auto V=D:\Games\GTA5

[ProgramLinks]
AppDataLocal=FiveM

[Registry]
Version=HKCU\Software\MyGame|REG_SZ|1.0.0

[Arg_steam]
PreExe=C:\Tools\SteamVPN.exe -connect
Link_Grand Theft Auto V=D:\SteamGames\GTA5
Reg_Platform=HKCU\Software\MyGame|REG_SZ|steam

[Arg_epic]
PreExe=C:\Tools\EpicVPN.exe
Link_Grand Theft Auto V=D:\EpicGames\GTA5
Reg_Platform=HKCU\Software\MyGame|REG_SZ|epic
```

**Запуск для Steam:**
```cmd
UniversalLinkSetup.exe -steam
```

**Запуск для Epic:**
```cmd
UniversalLinkSetup.exe -epic
```

---

### Пример 4: Полная конфигурация

```ini
[Settings]
GameFolder=C:\Program Files\Rockstar Games
LauncherExe=%AppDataLocal%\altv\altv.exe
PreExe=C:\Tools\VPN.exe -connect
PreExe2=C:\Tools\Discord.exe --minimized
PreExe3=C:\Tools\Firewall.exe --enable
LinkedGameFolder=D:\AllGames
BackupFolders=Grand Theft Auto V, AppDataLocal, AppDataRoaming

[GameLinks]
Grand Theft Auto V=D:\Games\GTA5
Red Dead Redemption 2=D:\Games\RDR2

[ProgramLinks]
AppDataLocal=altv
AppDataLocal2=DigitalEntitlements
AppDataLocal3=FiveM
AppDataRoaming=Discord
AppDataRoaming2=Telegram Desktop
ProgramData=Epic\EpicGamesLauncher

[CustomLinks]
MyFolder=C:\CustomLocation\MyFolder

[Registry]
InstallPath=HKCU\Software\MyGame|REG_SZ|%ProgramFiles%\MyGame
Version=HKCU\Software\MyGame|REG_SZ|1.0.0
Enabled=HKCU\Software\MyGame|REG_DWORD|1

[Services]
MyService=C:\Services\service.exe|auto|true

[Arg_steam]
PreExe=C:\Tools\SteamVPN.exe -connect
Link_Grand Theft Auto V=D:\SteamGames\GTA5
Reg_Platform=HKCU\Software\MyGame|REG_SZ|steam
```

---

## ❓ FAQ

### ❔ Нужны ли права администратора?

**Да**, для создания символических ссылок требуются права администратора. При первом запуске программа попросит повышение прав через UAC.

### ❔ Как сохранить данные администратора?

Программа автоматически сохраняет данные администратора в реестре после первого ввода:
```
HKEY_CURRENT_USER\SOFTWARE\UniversalLinkSetup\AdminCredentials
```

Для очистки удалите этот ключ через `regedit`.

### ❔ Как работает автоматический бэкап?

Через 3 минуты после запуска программа проверяет указанные папки в `BackupFolders`. Если symlink заменена на реальную папку (например, после обновления игры), программа копирует изменения обратно в целевую папку.

### ❔ Можно ли использовать без config.ini?

Да, но программа ничего не сделает. Config.ini обязателен для настройки.

### ❔ Поддерживаются ли относительные пути?

Нет, используйте только полные пути или переменные (`%AppDataLocal%`, `%ProgramFiles%`, и т.д.)

### ❔ Можно ли запустить несколько раз?

Да. Если лаунчер уже запущен, программа пропустит настройку и перейдет сразу к применению реестра и запуску PreExe.

### ❔ Как удалить программу?

1. Удалите созданные symlink вручную или запустите с `-install` перед удалением
2. Удалите папку с программой
3. (Опционально) Удалите данные администратора из реестра
4. (Опционально) Удалите импортированные .reg файлы через `regedit`

---

## 🔧 Устранение проблем

### ⚠️ "Требуются права администратора"

**Проблема:** Программа не может создать symlink без прав администратора.

**Решение:**
1. Запустите программу **от администратора**
2. Или введите данные администратора в консоли

### ⚠️ "Файл не найден"

**Проблема:** Программа не может найти `LauncherExe` или `PreExe`.

**Решение:**
1. Проверьте путь в `config.ini`
2. Убедитесь, что файл существует
3. Используйте полный путь без переменных для отладки

### ⚠️ "Не удалось создать ключ реестра"

**Проблема:** Программа не может записать в реестр.

**Решение:**
1. Проверьте права администратора
2. Убедитесь, что путь к ключу правильный
3. Проверьте тип данных (`REG_SZ`, `REG_DWORD`, и т.д.)

### ⚠️ "Symlink уже существует"

**Проблема:** Папка уже существует и не является symlink.

**Решение:**
1. Удалите папку вручную
2. Или переместите содержимое в целевую папку
3. Запустите программу снова

### ⚠️ Программа не запускает лаунчер

**Проблема:** Лаунчер не запускается после настройки.

**Решение:**
1. Проверьте `LauncherExe` в config.ini
2. Запустите с `-console` для просмотра ошибок
3. Проверьте, что файл существует

### ⚠️ Бэкап не работает

**Проблема:** Автоматический бэкап не копирует изменения.

**Решение:**
1. Убедитесь, что папки указаны в `BackupFolders`
2. Подождите 3 минуты после запуска
3. Проверьте, что папка стала реальной (не symlink)

### 🐛 Отладка

Запустите с консолью для просмотра логов:

```cmd
UniversalLinkSetup.exe -console
```

Логи покажут:
- ✅ Успешные операции
- ❌ Ошибки
- ℹ️ Информационные сообщения
- 🔍 Найденные файлы и папки

---

## 👨‍💻 Разработка

### Сборка из исходников

**Требования:**
- Visual Studio 2019+ или .NET SDK
- C# 7.0+

**Компиляция через csc:**
```cmd
csc /target:winexe /out:UniversalLinkSetup.exe UniversalLinkSetup.cs
```

**Компиляция через MSBuild:**
```cmd
msbuild UniversalLinkSetup.csproj /p:Configuration=Release
```

### Структура проекта

```
UniversalLinkSetup/
├── UniversalLinkSetup.cs    # Исходный код
├── config.ini                # Конфигурация (пример)
├── README.md                 # Документация
├── LICENSE                   # Лицензия MIT
└── Data/                     # Данные программы
    ├── Regs/                 # .reg файлы
    │   ├── settings/         # Настройки (всегда)
    │   └── games/            # Игровые (пропуск с -install)
    ├── AppDataLocal/         # Данные для AppDataLocal
    ├── AppDataRoaming/       # Данные для AppDataRoaming
    ├── ProgramData/          # Данные для ProgramData
    ├── ProgramFiles/         # Данные для ProgramFiles
    └── ProgramFilesx86/      # Данные для ProgramFiles(x86)
```

### Архитектура

**Основные компоненты:**

1. **Config Loader** — Загрузка config.ini
2. **Symlink Manager** — Создание символических ссылок
3. **Registry Manager** — Управление реестром Windows
4. **Process Manager** — Запуск программ и лаунчеров
5. **Backup Manager** — Автоматическое копирование обновлений
6. **Admin Manager** — Управление правами администратора

**Порядок выполнения:**

```
1. Загрузка config.ini
2. Обработка аргументов командной строки
3. Проверка прав администратора
4. Создание папок Data/
5. Удаление существующих symlink
6. Создание новых symlink:
   - Базовые программные ссылки [ProgramLinks]
   - Кастомные программные ссылки [Arg_*]
   - Базовые игровые ссылки [GameLinks]
   - Кастомные игровые ссылки [Arg_*]
7. Управление службами [Services]
8. Импорт .reg файлов
9. Применение реестра:
   - Базовые записи [Registry]
   - Кастомные записи [Arg_*]
10. Запуск пред-программ:
    - Базовые PreExe [Settings]
    - Кастомные PreExe [Arg_*]
11. Запуск лаунчера
12. Автоматический бэкап через 3 минуты
```

---

## 🤝 Вклад

Мы приветствуем вклад в проект! Вот как вы можете помочь:

### Отчеты об ошибках

Создайте issue на GitHub с описанием:
- Что произошло
- Что ожидалось
- Шаги для воспроизведения
- Версия Windows
- Версия программы

### Предложения

Создайте issue с тегом `enhancement` и опишите:
- Что вы хотите добавить
- Зачем это нужно
- Как это должно работать

### Pull Requests

1. Fork репозитория
2. Создайте ветку (`git checkout -b feature/amazing-feature`)
3. Commit изменений (`git commit -m 'Add amazing feature'`)
4. Push в ветку (`git push origin feature/amazing-feature`)
5. Откройте Pull Request

### Стиль кода

- Используйте 4 пробела для отступов
- Комментируйте сложные участки кода
- Следуйте C# Coding Conventions

---

## 📄 Лицензия

Этот проект распространяется под лицензией **MIT License**.

```
MIT License

Copyright (c) 2025 phamandai274

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

---

## 📞 Контакты

- **GitHub:** [@phamandai274](https://github.com/phamandai274)
- **Issues:** [GitHub Issues](https://github.com/phamandai274/UniversalLinkSetup/issues)
- **Email:** phamandai274@example.com

---

## 🙏 Благодарности

- Microsoft — за .NET Framework
- Сообщество GitHub — за отличные инструменты
- Все контрибьюторы проекта

---

## 📊 Статистика

![GitHub stars](https://img.shields.io/github/stars/phamandai274/UniversalLinkSetup?style=social)
![GitHub forks](https://img.shields.io/github/forks/phamandai274/UniversalLinkSetup?style=social)
![GitHub issues](https://img.shields.io/github/issues/phamandai274/UniversalLinkSetup)
![GitHub pull requests](https://img.shields.io/github/issues-pr/phamandai274/UniversalLinkSetup)

---

## 🔄 История версий

### v1.0.0 (2025-10-20)
- ✅ Первый релиз
- ✅ Символические ссылки
- ✅ Управление реестром (новый формат)
- ✅ Множественные PreExe
- ✅ Кастомные профили
- ✅ Автоматический бэкап
- ✅ Импорт .reg файлов
- ✅ Управление службами
- ✅ Оптимизация кода (-86%)

---

<div align="center">

**Made with ❤️ by [@phamandai274](https://github.com/phamandai274)**

⭐ **Поставьте звезду, если проект полезен!** ⭐

[⬆ Наверх](#-universal-link-setup)

</div>
