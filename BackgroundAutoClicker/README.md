# Project ni Xyrad

Usa ka custom automation tool nga nag-record sa tibuok lihok/path sa imong mouse ug nag-replay niini sa background para makagamit ka og **Alt+Tab** ug makalihok sa imong mouse sa ubang tabs o bintana!

---

## 🌟 2 ka Playback Modes:

### 1. 🌟 True Background Mode (PostMessage) — REKOMENDADO PARA SA ALT+TAB
- **Dili gyud mo-lihok ang imong physical mouse cursor!**
- Nagpadala kini og mouse messages direkta sa target window o child browser canvas (`Chrome_RenderWidgetHostHWND`).
- **Makagamit ka sa imong mouse sa bisan asa**: Pwede ka mag-Alt+Tab, mag-Facebook, mag-YouTube, o mag-trabaho sa pikas tab samtang ang target window ga-replay sa imong gi-record nga hover ug click!

### 2. 🎯 Hardware Cursor Mode (SendInput)
- Mo-lihok ang physical cursor sa imong screen.
- Gigamit para sa mga karaan nga games o bintana nga nagkinahanglan og direct hardware cursor input.
- **Fixed Infinite Looping**: Walay kalit nga pag-undang; magpadayon og loop hangtod pinduton ang F6 o ESC.

---

## 🎮 Unsaon Paggamit (Step-by-Step):

1. **Ablihi ang app**: [BackgroundAutoClicker.exe](file:///c:/Users/PCStudio/RF%20CLICKER/BackgroundAutoClicker/BackgroundAutoClicker.exe)
2. **Pag-Record**:
   - I-hover ang mouse sa imong target window (pananglitan: browser / Remetasks).
   - Pindota ang **F8** (madungog nimo ang beep nga nagsugod ang recording).
   - Lihoka imong mouse sa tanang dapit nga gusto nimo i-hover ug i-click.
   - Pindota ang **F8** balik aron mahuman ang recording.
3. **Pilia ang Mode**:
   - Siguroha nga naka-check ang **True Background Mode (PostMessage)** para makalihok imong mouse sa ubang apps.
4. **I-Play**:
   - Pindota ang **F6**.
   - Mag-loop kini nga walay hunong!
   - Pwede na ka mo-Alt+Tab sa laing tab o window!

---

## ⌨️ Global Hotkeys:

| Hotkey | Aksyon | Deskripsyon |
| :---: | :---: | :--- |
| **F8** | **Start / Stop Record** | Sugod o hunong sa pag-record sa mouse path. |
| **F6** | **Play / Stop Loop** | Sugod o hunong sa pag-replay sa na-record. |
| **ESC** | **Emergency Stop** | Diha-diha dayon nga paghunong sa playback. |
| **F9** | **Clear Recording** | Papason ang gi-record aron makasugod og bag-o. |

---

## 📁 Source & Executable:
- Application: [BackgroundAutoClicker.exe](file:///c:/Users/PCStudio/RF%20CLICKER/BackgroundAutoClicker/BackgroundAutoClicker.exe)
- Source Code: [Program.cs](file:///c:/Users/PCStudio/RF%20CLICKER/BackgroundAutoClicker/Program.cs)
