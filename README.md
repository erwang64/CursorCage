<div align="center">

<img src="Logo/Logo_fond_blanc_full.png" alt="CursorCage" width="200"/>

# CursorCage

**Limitez le curseur à l’écran ou à la fenêtre active** — pratique pour le jeu sur **multi-moniteurs**. Verrouillez / déverrouillez avec un **raccourci global**, sans quitter votre jeu.

<br/>

[![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white)](https://learn.microsoft.com/dotnet/csharp/)
[![WPF](https://img.shields.io/badge/WPF-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://learn.microsoft.com/dotnet/desktop/wpf/)

<br/>

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Windows-x64-0078D6?style=flat-square&logo=windows&logoColor=white)](https://www.microsoft.com/windows)
[![GitHub](https://img.shields.io/badge/GitHub-erwang64%2FCursorCage-181717?style=flat-square&logo=github)](https://github.com/erwang64/CursorCage)

</div>

---

## Fonctionnalités

- **Verrouillage du curseur** sur la zone cible (écran sous le pointeur, mode actuel du projet).
- **Raccourci clavier global** configurable (paramètres).
- **Icône dans la zone de notification** : ouvrir l’app, paramètres, quitter, notifications.
- **Interface WPF** sombre, **français** et **anglais**.
- **Mise à jour** optionnelle via les **releases GitHub** (vérification au démarrage ou menu).
- **Installateur Windows** (Inno Setup) : `CursorCage-Setup.exe`.

## Prérequis

- **Windows 10/11** (x64)
- [**SDK .NET 10**](https://dotnet.microsoft.com/download) (pour compiler depuis les sources)

## Compiler

```bash
git clone https://github.com/erwang64/CursorCage.git
cd CursorCage
dotnet build -c Release
```

L’exécutable se trouve sous `bin/Release/net10.0-windows/`.

## Installateur (.exe)

1. Installez [Inno Setup 6](https://jrsoftware.org/isinfo.php).
2. Depuis la racine du dépôt :

```powershell
.\scripts\Build-Installer.ps1
```

Le script publie l’app en **self-contained** `win-x64` puis lance `ISCC` sur **`CursorCage.iss`**.  
Résultat : `artifacts/installer/CursorCage-Setup.exe` (à joindre aux releases GitHub pour les mises à jour intégrées).

> Pensez à aligner la version dans **`CursorCage.csproj`** (`<Version>`) et **`CursorCage.iss`** (`#define MyAppVersion`).

## Structure du projet

| Élément | Rôle |
|--------|------|
| `Views/` | Pages WPF (accueil, paramètres) |
| `Services/` | Raccourcis, curseur, tray, mises à jour GitHub, réglages |
| `Native/` | P/Invoke Win32 |
| `Resources/` | Dictionnaires de traduction `Lang.en.xaml` / `Lang.fr.xaml` |
| `CursorCage.iss` | Script Inno Setup |
| `scripts/Build-Installer.ps1` | Publication + build de l’installateur |

## Stack technique

- **Langage :** C#  
- **UI :** WPF (+ WinForms pour `NotifyIcon` dans la barre des tâches)  
- **Cible :** `net10.0-windows`

---

<div align="center">

Construit avec **C#** et **WPF**

</div>
