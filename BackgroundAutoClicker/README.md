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

### Tab 2: 🏷️ Dynamic Attributes & Auto-Next Frame
- **⏩ Auto-Next Frame (+ key)**: Auto-click sa visibility option unya auto-advance dayon sa sunod nga frame gamit ang `+` key.
- **🟢 / 🔴 Master ON/OFF Switch (Hotkey: `F7`)**: Toggle sa Dynamic Attributes hotkeys ON o OFF.
- **⌨️ Editable Hotkeys**: Matag slot pwede ilisan og hotkey pinaagi sa pag-click sa button (e.g. `[Key: 1]`).
- **⏱️ 3-Second Countdown Capture (`Set Pos (3s)`)**: Dali nga pagkuha sa posisyon sa radio buttons.

### Tab 3: 📦 Cuboid Automations (1-Click Auto-Setup!)
- **🎯 1-Click Calibration via "..." Button**:
  - Dili na kinahanglan i-set tagsa-tagsa ang posisyon sa matag menu item!
  - I-set lang kausa ang **"..." (3 dots)** context menu button gamit ang `Set Pos (3s)`.
  - Automatic na nga kalkulado sa app ang eksaktong offsets para sa tanang menu options.
- **⚡ 4 ka Pangunang Aksyon sa Cuboid:**
  - **Set Floor to Cuboid Bottom** (Default Hotkey: `4`)
  - **Copy Attributes** (Default Hotkey: `2`)
  - **Copy Motion** (Default Hotkey: `1`)
  - **Delete Cuboid** (Default Hotkey: `3` — may native Delete key + menu click + Enter confirm fallback)
- **🌀 Make Dynamic (Hotkey: `X`):**
  - Mopindot sa "..." ug mo-click sa "Make Dynamic" dropdown option dayon mobalik sa daang posisyon sa mouse.
- **🟢 / 🔴 Cuboid Master Switch (Hotkey: `F4`):**
  - I-toggle ang Cuboid hotkeys ON o OFF aron dili mag-conflict sa normal typing.

---

## ⌨️ Global Hotkeys:

| Hotkey | Aksyon | Deskripsyon |
| :---: | :---: | :--- |
| **F4** | **Cuboid Master ON / OFF** | I-toggle ang Cuboid hotkeys (1, 2, 3, 4, X) ON o OFF. |
| **F7** | **Attributes ON / OFF** | I-toggle ang Dynamic Attributes hotkeys ON o OFF. |
| **F8** | **Start / Stop Record** | Sugod o hunong sa pag-record sa mouse path. |
| **F6** | **Play / Stop Loop** | Sugod o hunong sa pag-replay sa na-record nga hover. |
| **ESC** | **Emergency Stop** | Diha-diha dayon nga paghunong sa playback. |
| **F9** | **Clear Recording** | Papason ang gi-record aron makasugod og bag-o. |
| **4** *(Editable)* | **Set Floor to Cuboid Bottom** | Auto-click sa Floor option pinaagi sa "..." button. |
| **2** *(Editable)* | **Copy Attributes** | Auto-click sa Copy Attributes option. |
| **1** *(Editable)* | **Copy Motion** | Auto-click sa Copy Motion option. |
| **3** *(Editable)* | **Delete Cuboid** | Auto-delete sa cuboid (Delete key + menu click). |
| **X** *(Editable)* | **Make Dynamic** | Auto-click sa Make Dynamic option gikan sa "...". |

---

## 🎮 Unsaon Paggamit sa Cuboid Automations:

1. Ablihi ang [BackgroundAutoClicker.exe](file:///c:/Users/PCStudio/RF%20CLICKER/BackgroundAutoClicker/BackgroundAutoClicker.exe) ug adto sa **📦 3. Cuboid Automations** tab.
2. I-set ang posisyon sa **"..." (3 dots)** button sa SensorFusion gamit ang **"Set Pos (3s)"**.
3. Pindota ang **F4** aron ma-**ON** ang Cuboid hotkeys.
4. Diha sa imong trabaho:
   - Pindot **`4`** para Set Floor.
   - Pindot **`2`** para Copy Attributes.
   - Pindot **`1`** para Copy Motion.
   - Pindot **`X`** para Make Dynamic.
   - Pindot **`3`** para Delete Cuboid.
5. Inig human, pindota ang **F4** balik aron ma-OFF.

---

## 📁 Source & Executable:
- Application: [BackgroundAutoClicker.exe](file:///c:/Users/PCStudio/RF%20CLICKER/BackgroundAutoClicker/BackgroundAutoClicker.exe)
- Source Code: [Program.cs](file:///c:/Users/PCStudio/RF%20CLICKER/BackgroundAutoClicker/Program.cs)
- Background Asset: [bg.jpg](file:///c:/Users/PCStudio/RF%20CLICKER/BackgroundAutoClicker/bg.jpg)

