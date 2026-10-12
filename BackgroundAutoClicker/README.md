# Project ni Xyrad

Usa ka custom automation tool nga nag-apil og duha ka dagkong features:
1. **🖱️ Mouse Hover Recorder** — Pag-record sa tibuok lihok/path sa mouse ug pag-replay sa background samtang ga-Alt+Tab ka!
2. **🏷️ Dynamic Attributes & Visibility Clicker** — Auto-click sa 3D annotation / Visibility radio buttons (`0%`, `0%-25%`, `25%-50%`, `50%-75%`, `75%-100%`) gamit ang customizable hotkeys ug ON/OFF switch!

---

## 🌟 Mga Dagkong Features:

### Tab 1: 🖱️ Mouse Hover Recorder
- **True Background Mode (PostMessage)**: Replay sa background nga **dili gyud mo-lihok ang physical cursor**! Makagamit ka og mouse sa ubang apps/tabs samtang ga-replay.
- **Hardware Cursor Mode (SendInput)**: Physical cursor movement nga may infinite loop.
- **Custom Speeds & Auto-clicks**: Adjustable replay speed, interval clicks, audio beeps, ug loop delay.

### Tab 2: 🏷️ Dynamic Attributes & Visibility (Bag-o!)
- **ON/OFF Master Switch (F7)**:
  - Kung **ON**: Ang mga hotkeys (default: `1`, `2`, `3`, `4`, `5`) mo-click dayon sa gitakdang Visibility radio buttons.
  - Kung **OFF**: Ang mga hotkeys 1-5 **gi-release ug dili manghilabot sa laing apps**! Gawasnon nimo magamit ang 1, 2, 3, 4, 5 para sa chat, browser, o pag-type!
- **Editable Hotkeys**: Pwede nimo ilisan ang bisan unsang hotkey para sa matag slot pinaagi sa pag-click sa button (e.g. `[Key: 1]`) ug pagpindot sa bag-ong key!
- **3-Second Countdown Capture**: I-click ang `Set Pos (3s)` unya i-hover ang mouse sa radio button sulod sa 3 segundos — automatic ma-capture ang screen ug client coordinates!
- **Instant Jump & Auto-Restore**: Mo-ambak dayon ang mouse sa radio button, mo-click, ug **diha-diha dayon mobalik sa imong gitrabahoan nga dapit sa 3D canvas** aron dili mabalda ang imong annotation!
- **True Background PostMessage Support**: Opsyon nga dili gyud molihok ang mouse gamit ang PostMessage.
- **Auto-Save**: Automatic ma-save ang tanang posisyon ug keys sa `attributes_config.txt`.

---

## ⌨️ Global Hotkeys:

| Hotkey | Aksyon | Deskripsyon |
| :---: | :---: | :--- |
| **F7** | **Attributes ON / OFF** | I-toggle ang Dynamic Attributes hotkeys ON o OFF. Kung OFF, magamit nimo ang 1-5 sa lain! |
| **F8** | **Start / Stop Record** | Sugod o hunong sa pag-record sa mouse path. |
| **F6** | **Play / Stop Loop** | Sugod o hunong sa pag-replay sa na-record nga hover. |
| **ESC** | **Emergency Stop** | Diha-diha dayon nga paghunong sa playback. |
| **F9** | **Clear Recording** | Papason ang gi-record aron makasugod og bag-o. |
| **1 .. 5** *(Editable)* | **Visibility Click** | Mo-click sa tagsa-tagsa ka visibility option (`0%`, `0-25%`, `25-50%`, `50-75%`, `75-100%`). |

---

## 🎮 Unsaon Paggamit sa Attributes:

1. Ablihi ang app ug adto sa **🏷️ 2. Dynamic Attributes & Visibility** tab.
2. I-click ang **"Set Pos (3s)"** tupad sa `0%`.
3. Sulod sa 3 segundos, i-hover imong mouse sa `0%` radio button sa imong labeling tool. Madungog nimo ang beep pag ma-capture na!
4. Buhata kini para sa tanang 5 ka options.
5. Karon, samtang ga-label ka sa 3D tool, pindota lang ang:
   - `1` = Auto-click sa `0%`
   - `2` = Auto-click sa `0%-25%`
   - `3` = Auto-click sa `25%-50%`
   - `4` = Auto-click sa `50%-75%`
   - `5` = Auto-click sa `75%-100%`
6. Kung human na ka mag-attributes, pindota ang **F7** (o i-click ang dakong ON/OFF button) para ma-**OFF** kini ug magamit nimo ang 1, 2, 3, 4, 5 para sa pag-type o sa ubang apps!

---

## 📁 Source & Executable:
- Application: [BackgroundAutoClicker.exe](file:///c:/Users/PCStudio/RF%20CLICKER/BackgroundAutoClicker/BackgroundAutoClicker.exe)
- Source Code: [Program.cs](file:///c:/Users/PCStudio/RF%20CLICKER/BackgroundAutoClicker/Program.cs)
