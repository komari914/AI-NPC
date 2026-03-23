# Audio Setup Guide - NPC Voice Output

## 📢 问题诊断

如果你听不到NPC的语音，最可能的原因是：
1. **AudioSource未配置** - TTSManager需要AudioSource组件
2. **AudioListener缺失** - 场景中需要AudioListener
3. **音量设置为0** - 检查AudioSource和TTSManager的音量设置
4. **API错误** - OpenAI TTS API可能返回错误

## 🎵 完整Audio设置步骤

### 方法1：自动设置（推荐）

TTSManager会在Awake时自动创建AudioSource组件，但你需要确保场景中有AudioListener。

#### 步骤：

1. **确保主摄像机有AudioListener**
   - 选择Main Camera
   - 检查Inspector中是否有 `Audio Listener` 组件
   - 如果没有，点击 `Add Component` > `Audio` > `Audio Listener`
   - ⚠️ **重要**：整个场景中只能有一个AudioListener

2. **让TTSManager自动创建AudioSource**
   - TTSManager的代码会自动添加AudioSource组件
   - 不需要手动添加

3. **运行测试**
   - 进入Play模式
   - 检查Console是否有 `[TTS]` 开头的日志
   - 如果看到 "Playing audio clip"，说明TTS正在工作

### 方法2：手动设置（完全控制）

如果你想完全控制AudioSource的设置：

#### 步骤：

1. **创建Audio Manager GameObject**
   ```
   Hierarchy右键 > Create Empty
   重命名为 "AudioManager"
   ```

2. **添加TTSManager组件**
   - 选择AudioManager GameObject
   - Inspector > Add Component > 搜索 "TTSManager"
   - 添加TTSManager脚本

3. **添加AudioSource组件**
   - 同样在AudioManager GameObject上
   - Add Component > Audio > Audio Source
   - 这会在同一个GameObject上创建AudioSource

4. **配置AudioSource**（重要！）
   ```
   AudioSource组件设置：
   ✓ 不勾选 Play On Awake
   ✓ 不勾选 Loop
   ✓ Volume: 1.0
   ✓ Spatial Blend: 0 (2D音频)
   ✓ Priority: 128 (默认)
   ```

5. **配置TTSManager**
   - 在TTSManager组件中：
   - `Audio Source` 字段：拖拽同GameObject上的AudioSource组件
   - `Volume`: 1.0
   - `OpenAI API Key`: 粘贴你的API密钥
   - `Voice Type`: "alloy" (或其他音色)
   - `Model`: "tts-1"
   - `Speed`: 1.0

6. **确保Main Camera有AudioListener**
   - 选择Main Camera
   - 确认有Audio Listener组件

## 🔍 详细配置说明

### AudioSource设置详解

| 参数 | 值 | 说明 |
|------|-----|------|
| **Play On Awake** | ☐ (不勾选) | TTS音频是动态生成的，不能自动播放 |
| **Loop** | ☐ (不勾选) | NPC对话不应该循环播放 |
| **Volume** | 1.0 | 最大音量（可以在TTSManager中调整） |
| **Pitch** | 1.0 | 正常音调 |
| **Spatial Blend** | 0 (2D) | UI对话应该是2D音频，不受距离影响 |
| **Reverb Zone Mix** | 1.0 | 默认值 |
| **Priority** | 128 | 默认优先级 |

### TTSManager设置详解

| 参数 | 值 | 说明 |
|------|-----|------|
| **OpenAI API Key** | 你的密钥 | 必填！没有密钥无法使用TTS |
| **Voice Type** | alloy | 音色选择（见下表） |
| **Model** | tts-1 | 标准模型（更快） |
| **Speed** | 1.0 | 正常语速 |
| **Volume** | 1.0 | 音量（0-1范围） |
| **Audio Source** | (自动/手动) | AudioSource组件引用 |

### 可用的Voice Types

| Voice | 特点 | 适合角色 |
|-------|------|----------|
| **alloy** | 中性、平衡 | 默认选择 |
| **echo** | 男性、沉稳 | 权威型导师 |
| **fable** | 女性、温和 | 同理心型导师 |
| **onyx** | 男性、深沉 | 严肃型导师 |
| **nova** | 女性、活泼 | 友好型导师 |
| **shimmer** | 女性、柔和 | 支持型导师 |

**建议：**
- Empathic Persona: 使用 `fable` 或 `nova`
- Task-Focused Persona: 使用 `echo` 或 `onyx`

## 🧪 测试Audio设置

### 测试1：检查AudioSource

1. 进入Play模式
2. 在Hierarchy中找到TTSManager所在的GameObject
3. 查看Inspector中AudioSource组件
4. 应该看到：
   - ✓ AudioSource组件存在
   - ✓ Volume不是0
   - ✓ Play On Awake未勾选

### 测试2：检查AudioListener

1. 在Hierarchy中搜索 "AudioListener"
2. 确保只有一个GameObject有AudioListener
3. 通常在Main Camera上
4. 如果有多个，删除多余的（保留Main Camera上的）

### 测试3：运行Voice Scenario

1. 启动游戏
2. 选择Scenario 2或4（Voice模式）
3. 按住V键说话
4. 观察Console日志：
   ```
   [TTS] Requesting speech for: ...
   [TTS] Received audio data: XXXX bytes
   [TTS] Saved audio to: ...
   [TTS] Playing audio clip (length: X.Xs)
   [TTS] Speech finished
   ```
5. 如果看到这些日志但听不到声音，检查：
   - Windows系统音量
   - Unity Editor音量（顶部Game窗口右上角小喇叭图标）
   - AudioSource的Volume

### 测试4：检查API响应

如果看到错误日志：
```
[TTS] TTS API error: ...
```

可能的原因：
- API密钥无效或过期
- API额度用完
- 网络连接问题
- API服务暂时不可用

## 🔧 常见问题解决

### 问题1：完全没有声音

**检查清单：**
- [ ] AudioListener存在且只有一个
- [ ] AudioSource组件存在
- [ ] AudioSource的Volume > 0
- [ ] TTSManager的Volume > 0
- [ ] Windows系统音量 > 0
- [ ] Unity Editor音量未静音
- [ ] 在Voice模式的Scenario中测试
- [ ] Console没有错误日志

### 问题2：Console显示 "Failed to load audio file"

**原因：** Unity无法解码MP3文件

**解决方案：**
1. 确保Unity版本支持MP3（Unity 2020.3+）
2. 检查平台设置：
   - Edit > Project Settings > Audio
   - 确认Default Speaker Mode设置正确
3. 尝试使用`tts-1-hd`模型
4. 重启Unity Editor

### 问题3：声音很小或失真

**解决方案：**
1. 调整TTSManager的Volume参数（尝试0.5-1.0）
2. 调整AudioSource的Volume
3. 检查Speed参数（保持在0.8-1.2之间）
4. 确保Spatial Blend设为0（2D音频）

### 问题4：API错误 "401 Unauthorized"

**原因：** API密钥无效

**解决方案：**
1. 重新生成OpenAI API密钥
2. 确保密钥正确粘贴（没有多余空格）
3. 检查密钥权限（需要TTS权限）

### 问题5：延迟太长

**原因：** TTS API调用需要时间

**解决方案：**
1. 使用`tts-1`而非`tts-1-hd`（更快）
2. 检查网络连接
3. 考虑缓存常用语音片段（高级功能）

## 📋 推荐的GameObject结构

```
Hierarchy:
├── Main Camera
│   └── [Audio Listener] ← 必须有且只能有一个
│
├── Managers (Empty GameObject)
│   ├── ScenarioManager
│   ├── TimerManager
│   └── DataRecorder
│
├── AudioManager (Empty GameObject)
│   ├── [TTSManager] ← TTS控制
│   └── [Audio Source] ← 自动创建或手动添加
│
├── MentorNPC
│   ├── MentorNPC script
│   └── (references TTSManager)
│
└── UI Canvas
    └── ...
```

## 🎯 完整配置示例（截图说明）

### Inspector中的TTSManager应该看起来像这样：

```
TTSManager (Script)
├─ API Settings
│  └─ OpenAI API Key: "sk-..." (你的密钥)
├─ TTS Settings
│  ├─ Voice Type: "alloy"
│  ├─ Model: "tts-1"
│  └─ Speed: 1.0
├─ Audio Settings
│  ├─ Audio Source: AudioSource (AudioManager) ← 自动分配
│  └─ Volume: 1.0
└─ State (运行时)
   ├─ Is Speaking: false
   └─ Is Processing: false
```

### Inspector中的AudioSource应该看起来像这样：

```
Audio Source
├─ Audio Clip: (None) ← 动态设置，初始为空
├─ Output: (None - Master)
├─ ☐ Play On Awake ← 不勾选！
├─ ☐ Loop ← 不勾选！
├─ Priority: 128
├─ Volume: 1.0
├─ Pitch: 1.0
├─ Stereo Pan: 0
├─ Spatial Blend: 0 (2D) ← 设为2D！
├─ Reverb Zone Mix: 1.0
└─ ...
```

## 💡 高级提示

### 1. 音色选择策略

根据实验设计选择合适的音色：
- **Empathic Persona**: 选择温和、友好的音色（fable, nova）
- **Task-Focused Persona**: 选择严肃、专业的音色（echo, onyx）

这样可以增强persona的效果对比。

### 2. 调整语速

```csharp
// 在TTSManager Inspector中
Speed = 1.0;  // 正常语速
Speed = 0.9;  // 稍慢（更清晰）
Speed = 1.1;  // 稍快（更高效）
```

建议：
- 如果参与者反馈听不清，降低到0.9
- 如果6分钟时间紧张，可以提高到1.1

### 3. 音量平衡

确保NPC语音和UI音效音量适中：
- NPC语音：TTSManager.Volume = 0.8-1.0
- UI音效：AudioSource.Volume = 0.5-0.7

### 4. 调试模式

在开发时，可以临时启用AudioSource的可视化：
```
AudioSource组件 > 右键 > Debug
```
这样可以看到更多运行时信息。

## 🎬 快速开始步骤

如果你只想快速开始，按以下最小步骤操作：

1. **Main Camera** - 确认有AudioListener
2. **创建Empty GameObject** - 命名为 "AudioManager"
3. **添加TTSManager** - 到AudioManager上
4. **设置API密钥** - 在TTSManager组件中
5. **测试** - 运行Voice scenario

就这么简单！TTSManager会自动创建AudioSource。

---

## 📞 如果还是没声音...

请检查：

1. **Unity Editor的Audio设置**
   - Edit > Preferences > Audio
   - 确认 "Disable Audio" 未勾选

2. **Windows混音器**
   - 右键任务栏音量图标
   - 打开音量混音器
   - 确认Unity Editor音量不是静音

3. **硬件**
   - 耳机/扬声器是否连接
   - 尝试播放其他音频测试硬件

4. **Console日志**
   - 查找 `[TTS]` 开头的日志
   - 复制完整的错误信息用于调试

---

**提示：** 在正式实验前，一定要用所有4个scenario都测试一遍Audio功能！
