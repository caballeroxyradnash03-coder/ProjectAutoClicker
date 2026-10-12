# Project ni Xyrad

Usa ka custom automation tool nga nag-apil og duha ka dagkong features:
1. **🖱️ Mouse Hover Recorder** — Pag-record sa tibuok lihok/path sa mouse ug pag-replay sa background samtang ga-Alt+Tab ka!
2. **🏷️ Dynamic Attributes & Auto-Next Frame** — Auto-click sa 3D annotation / Visibility radio buttons (`0%`, `0%-25%`, `25%-50%`, `50%-75%`, `75%-100%`) nga may **automatic abante sa sunod nga frame gamit ang `+` key**!

---

## 🌟 Mga Dagkong Features:

### Tab 1: 🖱️ Mouse Hover Recorder
- **True Background Mode (PostMessage)**: Replay sa background nga **dili gyud mo-lihok ang physical cursor**! Makagamit ka og mouse sa ubang apps/tabs samtang ga-replay.
- **Hardware Cursor Mode (SendInput)**: Physical cursor movement nga may infinite loop.
- **Custom Speeds & Auto-clicks**: Adjustable replay speed, interval clicks, audio beeps, ug loop delay.

### Tab 2: 🏷️ Dynamic Attributes & Auto-Next Frame (Bag-o!)
- **⏩ Auto-Next Frame (+ key)**:
  - Inig pindot nimo sa attribute hotkey (e.g. `1` para `0%`):
    1. Mo-click sa `0%` radio button.
    2. Mo-hulat og gamay (adjustable delay, default 60ms).
    3. **Automatic dayon mopindot sa `+` key aron mo-abante sa Frame 2!**
    4. Dire-diretso gikan Frame 1 hangtod Frame 42 nga walay usik sa oras!
  - Pwede i-on o i-off ang checkbox kung dili nimo gusto mo-next frame.
  - Naay `Test [+]` button aron dali ra ma-test.
- **🟢 / 🔴 Master ON/OFF Switch (Hotkey: `F7`):**
  - **Kung NAKA-ON:** Ang hotkeys (default: `1`, `2`, `3`, `4`, `5`) mo-click dayon sa gitakdang Visibility radio buttons ug mo-next frame.
  - **Kung NAKA-OFF:** Ang mga hotkeys 1-5 **gi-release ug dili manghilabot sa laing apps**! Gawasnon nimo magamit ang 1, 2, 3, 4, 5 para sa chat, browser, o pag-type!
- **⌨️ Editable Hotkeys:**
  - Matag slot pwede ilisan og hotkey pinaagi sa pag-click sa button (e.g. `[Key: 1]`) ug pagpindot sa bag-ong key.
  - Ang Next Frame key pud pwede ilisan kung dili `+` imong gamiton.
- **⏱️ 3-Second Countdown Capture (`Set Pos (3s)`):**
  - I-click ang `Set Pos (3s)` unya i-hover ang mouse sa radio button sulod sa 3 segundos — automatic ma-capture ang coordinates!
- **⚡ Instant Jump & Auto-Restore:**
  - Mo-ambak dayon ang mouse sa radio button, mo-click, ug **diha-diha dayon mobalik sa imong gitrabahoan nga dapit sa 3D canvas**!
- **💾 Auto-Save:**
  - Gi-save automatic ang tanang posisyon, hotkeys, ug settings sa `attributes_config.txt`.

---

## ⌨️ Global Hotkeys:

| Hotkey | Aksyon | Deskripsyon |
| :---: | :---: | :--- |
| **F7** | **Attributes ON / OFF** | I-toggle ang Dynamic Attributes hotkeys ON o OFF. Kung OFF, magamit nimo ang 1-5 sa lain! |
| **F8** | **Start / Stop Record** | Sugod o hunong sa pag-record sa mouse path. |
| **F6** | **Play / Stop Loop** | Sugod o hunong sa pag-replay sa na-record nga hover. |
| **ESC** | **Emergency Stop** | Diha-diha dayon nga paghunong sa playback. |
| **F9** | **Clear Recording** | Papason ang gi-record aron makasugod og bag-o. |
| **1 .. 5** *(Editable)* | **Visibility Click + Auto-Next** | Mo-click sa visibility option unya automatic mo-padala sa `+` para sa sunod nga frame! |

---

## 🎮 Unsaon Paggamit sa Attributes + Auto-Next:

1. Ablihi ang [BackgroundAutoClicker.exe](file:///c:/Users/PCStudio/RF%20CLICKER/BackgroundAutoClicker/BackgroundAutoClicker.exe) ug adto sa **🏷️ 2. Dynamic Attributes & Visibility** tab.
2. I-set ang 5 ka posisyon gamit ang **"Set Pos (3s)"** tupad sa matag visibility option.
3. Siguroha nga naka-check ang **`⏩ Auto-Next Frame human og click`**.
4. Pagsugod sa Frame 1:
   - Pindot **`1`** ➔ mo-click `0%` ➔ auto-pindot `+` ➔ abante sa Frame 2!
   - Sa Frame 2: Pindot pananglitan **`2`** ➔ mo-click `0%-25%` ➔ auto-pindot `+` ➔ abante sa Frame 3!
   - Pas-pas kaayo mahuman ang tanang 42 frames!
5. Kung human na ka, pindota ang **F7** para ma-OFF ug magamit nimo ang 1, 2, 3, 4, 5 sa ubang bintana!

---

## 📁 Source & Executable:
- Application: [BackgroundAutoClicker.exe](file:///c:/Users/PCStudio/RF%20CLICKER/BackgroundAutoClicker/BackgroundAutoClicker.exe)
- Source Code: [Program.cs](file:///c:/Users/PCStudio/RF%20CLICKER/BackgroundAutoClicker/Program.cs)
