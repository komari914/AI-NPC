# Detective Game - Setup Guide

This guide explains how to set up and configure the newly implemented features for your thesis experiment project.

## 📋 Overview

Three major systems have been implemented:
1. **Scenario Management System** - Switch between 4 experimental scenarios
2. **Timer System** - 6-minute countdown for each scenario
3. **Voice Interaction System** - Speech-to-Text and Text-to-Speech (English only)

## 🎯 4 Scenarios

| Scenario | Persona | Modality |
|----------|---------|----------|
| 1 | Empathic | Text |
| 2 | Empathic | Voice |
| 3 | Task-Focused | Text |
| 4 | Task-Focused | Voice |

## 🔧 Setup Instructions

### 1. Scene Setup

#### Create Main Menu Scene
1. Create a new scene: `File > New Scene`
2. Save as `MainMenu.unity` in `Assets/Scenes/`
3. Add Canvas (UI > Canvas)
4. Add the following UI elements:
   - **Main Panel** - Container for scenario buttons
   - **4 Buttons** - One for each scenario
   - **Instructions Button**
   - **Quit Button**
   - **Instruction Panel** (initially hidden)
   - **Back Button** (in instruction panel)
   - Optional: **Scenario Info Text** (TextMeshPro)

5. Add `MainMenuUI` component to a GameObject
6. Assign UI references in the Inspector

#### Update Game Scene (SampleScene)
1. Open your game scene (`SampleScene.unity`)
2. Create an empty GameObject named "Managers"
3. Add the following components to "Managers":
   - `ScenarioManager`
   - `TimerManager`
   - `DataRecorder`
   - `TTSManager`

4. Create a UI Canvas and add:
   - **Timer Display** - Add `TimerUI` component
     - Add TextMeshPro for timer text
     - Add Image for progress bar (optional)
   - **Game End Panel** - Add `GameEndUI` component
     - Result text, time text, message text
     - Restart, Main Menu, Next Scenario buttons

5. Create an empty GameObject named "VoiceManager"
   - Add `VoiceInputManager` component
   - Create UI for recording indicator (optional)

### 2. Component Configuration

#### ScenarioManager
- `Main Menu Scene Name`: "MainMenu"
- `Game Scene Name`: "SampleScene"
- Set initial `Scenario Index`: 1-4
- Set `Persona` and `Modality` (will be overridden by selection)

#### TimerManager
- `Total Duration`: 360 (6 minutes)
- `Auto Start`: ✓ (checked)
- `Warning Time`: 60 (1 minute warning)
- `Auto End Game On Complete`: ✓ (checked)
- Assign `Game End UI` reference

#### TimerUI
- Assign `Timer Text` (TextMeshPro)
- Assign `Progress Bar` (Image, Fill type)
- Set `Normal Color`, `Warning Color`, `Critical Color`

#### GameEndUI
- Assign `End Panel` GameObject
- Assign all text references (result, time, message)
- Assign all buttons (restart, main menu, next scenario)

#### MentorNPC
- Assign `Scenario Manager` reference
- Assign `TTS Manager` reference
- Assign `Voice Input Manager` reference
- **IMPORTANT**: Fill in the two system prompts:
  - `Empathic System Prompt` - High empathy mentor personality
  - `Task Focused System Prompt` - Low empathy, task-oriented mentor

#### VoiceInputManager
- Set `OpenAI API Key` (for Whisper API)
- `Max Recording Duration`: 10 seconds
- `Record Key`: V (default)
- `Toggle Mode`: ☐ (unchecked for push-to-talk)
- Assign `Recording Indicator` UI (optional)
- Assign `Mentor NPC` reference

#### TTSManager
- Set `OpenAI API Key` (for TTS API)
- `Voice Type`: "alloy" (or: echo, fable, onyx, nova, shimmer)
- `Model`: "tts-1" (faster) or "tts-1-hd" (higher quality)
- `Speed`: 1.0 (normal speed)
- `Volume`: 1.0

#### DataRecorder
- Set `Player Id`: "P001" (change for each participant)
- `Auto Save On Game End`: ✓ (checked)
- `Save Directory`: "ExperimentData"

### 3. API Keys Configuration

⚠️ **IMPORTANT**: You need OpenAI API keys for the following features:

1. **Chat Responses** (OpenAIClient)
   - Model: `gpt-4.1-mini` or similar
   - Used for: Mentor NPC dialogue

2. **Speech-to-Text** (VoiceInputManager)
   - Model: `whisper-1`
   - Used for: Converting player voice to text

3. **Text-to-Speech** (TTSManager)
   - Model: `tts-1` or `tts-1-hd`
   - Used for: Converting NPC text to speech

**How to set API keys:**
- In Unity Inspector, paste your API key into each component
- **For production**: Use a server-side proxy instead of client-side keys
- **For testing**: Client-side keys are acceptable

### 4. Build Settings

1. `File > Build Settings`
2. Add scenes in order:
   - `MainMenu` (index 0)
   - `SampleScene` (index 1)
3. Configure player settings as needed

## 🎮 How to Use

### For Researchers/Developers

#### Starting a Session
1. Run the MainMenu scene
2. Select a scenario (1-4)
3. Game loads automatically with correct configuration

#### Text Modality (Scenarios 1 & 3)
- Press **F** near mentor to open chat
- Type message and press **Enter**
- Mentor responds with text

#### Voice Modality (Scenarios 2 & 4)
- **Hold V** to record your voice (push-to-talk)
- Release V to send
- Mentor responds with voice (and subtitles)

#### Evidence Collection
- Press **E** to inspect evidence
- Evidence is automatically recorded
- Progress tracked by CaseProgressManager

#### Timer
- 6-minute countdown starts automatically
- Warning at 1 minute remaining
- Game ends when time expires or case is solved

### Data Collection

Experiment data is automatically saved when:
- Game ends (time up or case solved)
- Application quits

**Data Location:**
- Path: `Application.persistentDataPath/ExperimentData/`
- Windows: `C:\Users\[Username]\AppData\LocalLow\[CompanyName]\[ProductName]\ExperimentData\`

**Data Files:**
- `Session_[ID]_[PlayerID]_S[Scenario].json` - Full session data
- `Conversations_[ID].csv` - All dialogues
- `Evidence_[ID].csv` - Evidence collection log

**Data Includes:**
- Session metadata (scenario, persona, modality)
- Complete conversation history
- Evidence collection timestamps
- Phase transitions
- Final answer and correctness
- Total duration

## 📝 Persona Prompts

You need to create two distinct mentor personas. Here's a template:

### Empathic Persona Example
```
You are an experienced detective mentor who is empathetic, supportive, and patient.

Personality:
- Use encouraging language and positive reinforcement
- Show understanding when the player is confused
- Provide emotional support during challenging moments
- Use phrases like "I understand...", "That's a good observation...", "Take your time..."

Communication Style:
- Warm and friendly tone
- Ask about the player's thought process
- Validate their feelings and efforts
- Provide gentle guidance

Example responses:
- "I can see you're working hard on this. Let's think through it together."
- "That's an interesting observation! What made you notice that?"
```

### Task-Focused Persona Example
```
You are an experienced detective mentor who is direct, efficient, and focused on results.

Personality:
- Use concise, factual language
- Focus on evidence and logic only
- Minimal emotional engagement
- Use phrases like "Focus on...", "The facts show...", "Next step is..."

Communication Style:
- Professional and business-like tone
- Direct questions and instructions
- Emphasize efficiency and accuracy
- Provide straightforward guidance

Example responses:
- "Review the timeline. What's the logical conclusion?"
- "The evidence contradicts that theory. Re-examine the facts."
```

## 🔍 Testing Checklist

### Basic Functionality
- [ ] Main menu loads correctly
- [ ] All 4 scenarios can be started
- [ ] Timer counts down from 6:00
- [ ] Timer UI updates correctly
- [ ] Timer warning appears at 1:00
- [ ] Game ends at 0:00

### Text Modality (Scenarios 1 & 3)
- [ ] F key opens dialogue input
- [ ] Text input works
- [ ] Mentor responds with correct persona
- [ ] Subtitles display correctly

### Voice Modality (Scenarios 2 & 4)
- [ ] Microphone is detected
- [ ] V key records voice
- [ ] Voice is transcribed (Whisper API)
- [ ] Mentor responds with voice (TTS API)
- [ ] Subtitles sync with audio

### Evidence System
- [ ] Evidence can be inspected (E key)
- [ ] Evidence is marked as collected
- [ ] Progress manager tracks evidence
- [ ] Evidence affects dialogue context

### Data Recording
- [ ] Session data is created
- [ ] Conversations are logged
- [ ] Evidence collection is logged
- [ ] Phase transitions are tracked
- [ ] Final answer is recorded
- [ ] JSON and CSV files are saved

### Game Flow
- [ ] Opening sequence plays
- [ ] Case phases transition correctly
- [ ] Final question triggers properly
- [ ] Correct answer is recognized
- [ ] Game end UI appears
- [ ] Can restart scenario
- [ ] Can return to main menu
- [ ] Can proceed to next scenario

## 🐛 Troubleshooting

### Voice Input Not Working
- Check microphone permissions
- Verify OpenAI API key is set
- Check console for API errors
- Ensure you're in Voice modality scenario

### TTS Not Playing
- Check OpenAI API key
- Verify AudioSource component exists
- Check volume settings
- MP3 codec might not be available on some platforms

### Timer Not Starting
- Verify TimerManager is in scene
- Check "Auto Start" is enabled
- Make sure only one TimerManager instance exists

### Data Not Saving
- Check write permissions
- Verify DataRecorder is in scene
- Check console for save errors
- Ensure "Auto Save On Game End" is enabled

### Mentor Not Responding
- Check OpenAI API key in OpenAIClient
- Verify system prompts are filled in
- Check network connectivity
- Look for errors in console

## 🎓 For Participants

**Controls:**
- **WASD** - Move
- **Mouse** - Look around
- **E** - Inspect evidence
- **F** - Talk to mentor (Text mode)
- **V** - Hold to speak (Voice mode)
- **ESC** - Pause menu

**Objective:**
Solve the murder case within 6 minutes by:
1. Collecting evidence
2. Discussing findings with your mentor
3. Identifying the correct killer with evidence-based reasoning

## 📊 Experiment Protocol Suggestions

1. **Randomize scenario order** for each participant
2. **Brief participants** on controls before starting
3. **Test microphone** before Voice scenarios
4. **Set unique Player ID** for each participant
5. **Save data immediately** after each session
6. **Backup data files** regularly

## 🔒 Important Notes

- API keys should be kept secure
- Consider using a server proxy for production
- Test all scenarios before running actual experiments
- Ensure stable internet connection for API calls
- Have backup plan if APIs fail
- Get participant consent for voice recording

## 📞 Support

For issues or questions:
1. Check Unity Console for error messages
2. Verify all components are assigned in Inspector
3. Test with a simple scenario first
4. Review API documentation for errors

---

**Version:** 1.0
**Last Updated:** 2026-02-16
