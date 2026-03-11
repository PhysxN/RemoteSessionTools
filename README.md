# RemoteSessionWatcher

RemoteSessionWatcher — небольшая утилита для Windows, работающая в системном трее.

Программа отслеживает заданные процессы и параметры командной строки, например:

- `AnyDesk.exe --backend`
- `rudesktop.exe --cm`

При срабатывании правила запускается служебный процесс `RemoteSessionMarker.exe`, который можно добавить в правила Wallpaper Engine.

Это позволяет автоматически:

- ставить обои на паузу
- или полностью останавливать их с освобождением памяти

После завершения удалённой сессии marker-процесс закрывается автоматически.

## Возможности

- работа в системном трее
- отслеживание по имени процесса
- отслеживание по параметрам командной строки
- несколько правил в `config.json`
- уведомления в трее
- автозапуск через Планировщик заданий Windows
- запуск с повышенными правами
- PDF-инструкция по настройке Wallpaper Engine

## Как это работает

1. `RemoteSessionWatcher.exe` отслеживает процессы и параметры командной строки.
2. Если одно из правил совпало, запускается `service\RemoteSessionMarker.exe`.
3. В Wallpaper Engine настраивается правило на `RemoteSessionMarker.exe`.
4. Пока marker запущен, Wallpaper Engine останавливает или приостанавливает обои.
5. Когда удалённая сессия завершается, marker закрывается.

## Поддерживаемые примеры правил

### AnyDesk
- Процесс: `AnyDesk.exe`
- Параметр: `--backend`

### RuDesktop
- Процесс: `rudesktop.exe`
- Параметр: `--cm`

## Пример `config.json`

```json
{
  "CheckIntervalMs": 1500,
  "RequiredHitsToActivate": 2,
  "RequiredHitsToDeactivate": 2,
  "MarkerExePath": "service\\RemoteSessionMarker.exe",
  "Rules": [
    {
      "Name": "AnyDesk remote session",
      "Enabled": true,
      "ProcessName": "AnyDesk.exe",
      "CommandLineContains": [
        "--backend"
      ],
      "CommandLineNotContains": []
    },
    {
      "Name": "RuDesktop remote session",
      "Enabled": true,
      "ProcessName": "rudesktop.exe",
      "CommandLineContains": [
        "--cm"
      ],
      "CommandLineNotContains": []
    }
  ]
}
```

## Важно

Если для правила не нужны параметры командной строки, можно оставить массив `CommandLineContains` пустым:

```json
"CommandLineContains": []
```

В этом случае правило будет срабатывать только по имени процесса.

## Структура файлов рядом с программой

```text
RemoteSessionWatcher.exe
config.json
docs/
    HowToSetup.pdf
service/
    RemoteSessionMarker.exe
```

## Автозапуск

Для корректного чтения параметров командной строки программа должна запускаться с правами администратора.

Рекомендуемый вариант автозапуска:
- через Планировщик заданий Windows
- при входе текущего пользователя
- с параметром «Выполнять с наивысшими правами»

## Настройка Wallpaper Engine

В комплекте может быть PDF-инструкция:

`docs/HowToSetup.pdf`

В Wallpaper Engine нужно создать правило для:

`RemoteSessionMarker.exe`

Рекомендуемые параметры:
- Условие: `Запущено`
- Воспроизведение обоев: `Остановить (освободить память)`  
  или `Пауза`

## Сборка

Проект рассчитан на:
- C#
- WinForms
- .NET Framework 4.8
- Visual Studio

## Назначение

Программа полезна в случаях, когда при удалённом подключении через AnyDesk, RuDesktop или другие подобные приложения нужно автоматически отключать живые обои в Wallpaper Engine.
