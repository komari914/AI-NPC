# 场景切换问题修复说明

## ❌ 原问题

从MainMenu进入SampleScene时：
- Manager对象消失
- NPC没有声音
- Timer停在6:00
- 游戏无法正常运行

## 🔍 问题原因

**DontDestroyOnLoad冲突：**
1. ScenarioManager使用了`DontDestroyOnLoad()`
2. MainMenuUI创建了新的ScenarioManager
3. 这个新的Manager在场景切换时保留
4. SampleScene中原有的Managers对象与新的Manager冲突
5. 单例模式导致SampleScene的Managers被销毁

**结果：**
- TimerManager, TTSManager等组件失去引用
- 所有依赖这些Manager的功能失效

## ✅ 解决方案

**使用PlayerPrefs传递配置，而不是DontDestroyOnLoad：**

### 修改1：ScenarioManager.cs

**移除DontDestroyOnLoad：**
```csharp
// 旧代码（有问题）
void Awake()
{
    Instance = this;
    DontDestroyOnLoad(gameObject); // ❌ 导致冲突
}

// 新代码（修复）
void Awake()
{
    Instance = this;
    // ✅ 不使用DontDestroyOnLoad
    LoadScenarioFromPrefs(); // 从PlayerPrefs加载配置
}
```

**新增方法：**
- `SaveScenarioToPrefs()` - 保存配置到PlayerPrefs
- `LoadScenarioFromPrefs()` - 从PlayerPrefs加载配置
- `NotifyScenarioLoaded()` - 通知其他系统配置已加载

### 修改2：MainMenuUI.cs

**不再创建ScenarioManager：**
```csharp
// 旧代码（有问题）
void StartScenario(int index)
{
    // 创建新的ScenarioManager
    GameObject smObj = new GameObject("ScenarioManager"); // ❌
    ScenarioManager sm = smObj.AddComponent<ScenarioManager>();
    sm.StartScenarioByIndex(index);
}

// 新代码（修复）
void StartScenario(int index)
{
    // 只保存配置到PlayerPrefs
    ScenarioConfig config = ScenarioManager.GetScenarioConfigByIndex(index);
    PlayerPrefs.SetInt("ScenarioIndex", config.scenarioIndex);
    PlayerPrefs.SetInt("ScenarioPersona", (int)config.persona);
    PlayerPrefs.SetInt("ScenarioModality", (int)config.modality);
    PlayerPrefs.Save();

    // 直接加载场景，让SampleScene的ScenarioManager读取配置
    SceneManager.LoadScene("SampleScene");
}
```

## 🎯 工作流程（修复后）

### 1. MainMenu场景
```
用户点击Scenario按钮
    ↓
MainMenuUI保存配置到PlayerPrefs
    ↓
加载SampleScene
```

### 2. SampleScene加载
```
SampleScene加载
    ↓
ScenarioManager.Awake()
    ↓
从PlayerPrefs读取配置
    ↓
应用配置（persona, modality）
    ↓
所有Manager正常工作 ✅
```

## 📋 场景结构要求

### MainMenu场景（简单）
```
MainMenu场景：
├── Canvas
│   └── MainMenuUI (脚本)
│       └── UI按钮
└── EventSystem

⚠️ 不需要ScenarioManager
⚠️ 不需要其他Manager
```

### SampleScene场景（完整）
```
SampleScene场景：
├── Main Camera
│   └── [Audio Listener] ✅ 必须有
├── Managers (GameObject)
│   ├── ScenarioManager ✅
│   ├── TimerManager ✅
│   └── DataRecorder ✅
├── AudioManager (GameObject)
│   └── TTSManager ✅
├── VoiceManager (GameObject)
│   └── VoiceInputManager
├── CaseProgressManager ✅
└── 其他游戏对象...
```

## 🔧 配置步骤

### 1. SampleScene设置

确保SampleScene中有所有需要的Manager：

```
1. 检查是否有 "Managers" GameObject
   - 包含 ScenarioManager
   - 包含 TimerManager
   - 包含 DataRecorder

2. 检查是否有 "AudioManager" GameObject
   - 包含 TTSManager

3. 检查是否有 "VoiceManager" GameObject
   - 包含 VoiceInputManager

4. 检查是否有 CaseProgressManager
```

### 2. MainMenu场景设置

MainMenu场景不需要任何Manager：

```
1. 移除任何ScenarioManager实例
2. 移除任何Manager GameObject
3. 只保留MainMenuUI和UI元素
```

### 3. 验证场景名称

确保场景名称正确：

```csharp
ScenarioManager组件（在SampleScene中）：
├─ Main Menu Scene Name: "MainMenu"
└─ Game Scene Name: "SampleScene"
```

### 4. Build Settings

```
File > Build Settings
确保场景顺序：
0. MainMenu
1. SampleScene
```

## 🧪 测试步骤

### 测试1：直接启动SampleScene
```
1. 打开SampleScene
2. 点击Play
3. 检查Manager对象是否存在 ✅
4. 检查Timer是否计时 ✅
5. 检查NPC对话是否正常 ✅
```

### 测试2：从MainMenu启动
```
1. 打开MainMenu场景
2. 点击Play
3. 点击任意Scenario按钮
4. 场景切换到SampleScene
5. 检查Manager对象是否存在 ✅
6. 检查Timer是否计时 ✅
7. 检查Scenario配置是否正确 ✅
8. 检查NPC对话是否正常 ✅
```

### 测试3：Scenario配置
```
1. 从MainMenu选择Scenario 2（Voice）
2. 进入游戏后查看左上角Overlay
3. 应显示：
   - Scenario: 2
   - Persona: Empathic
   - Modality: Voice ✅
```

## 🔍 调试方法

### 如果Manager还是消失：

**检查1：Scene中的Manager**
```
运行时在Hierarchy中查找：
- Managers GameObject存在吗？
- 是否有多个ScenarioManager实例？
```

**检查2：Console日志**
```
应该看到：
[ScenarioManager] Loaded from PlayerPrefs: Scenario X (...)
[ScenarioManager] Scenario X is active
[Timer] Started: 360 seconds (6.0 minutes)
```

**检查3：PlayerPrefs**
```csharp
// 在Unity Console中执行（使用Debug.Log）
PlayerPrefs.GetInt("ScenarioIndex", -1); // 应该是1-4
PlayerPrefs.GetInt("ScenarioPersona", -1); // 应该是0或1
PlayerPrefs.GetInt("ScenarioModality", -1); // 应该是0或1
```

### 如果Timer不启动：

**检查：**
```
1. TimerManager组件存在吗？
2. Auto Start是否勾选？
3. Total Duration是否设为360？
4. Game End UI是否分配？
```

### 如果NPC没声音：

**检查：**
```
1. TTSManager存在吗？
2. AudioSource自动创建了吗？（运行时检查）
3. Main Camera有AudioListener吗？
4. 是否在Voice模式下测试？（Scenario 2或4）
5. API密钥设置了吗？
```

## 📊 数据流图

```
MainMenu
   ├─ 用户选择Scenario
   ├─ 保存到PlayerPrefs
   │   ├─ ScenarioIndex
   │   ├─ ScenarioPersona
   │   └─ ScenarioModality
   └─ 加载SampleScene
       ↓
SampleScene加载
   ├─ ScenarioManager.Awake()
   │   └─ LoadScenarioFromPrefs()
   ├─ TimerManager.Awake()
   │   └─ 创建Instance
   ├─ TTSManager.Awake()
   │   └─ 创建AudioSource
   └─ DataRecorder.Awake()
       └─ 创建Session
       ↓
所有系统正常工作 ✅
```

## 💡 关键改变总结

| 内容 | 修改前 | 修改后 |
|------|--------|--------|
| ScenarioManager | 使用DontDestroyOnLoad | 不使用，每个场景有自己的实例 |
| 配置传递 | 静态变量 | PlayerPrefs |
| MainMenu | 创建新Manager | 只保存配置 |
| SampleScene | 可能被覆盖 | 始终使用场景中的Manager |

## ✅ 优点

1. **简单清晰** - 每个场景独立管理自己的Manager
2. **无冲突** - 不使用DontDestroyOnLoad避免单例冲突
3. **易调试** - 可以直接启动任何场景测试
4. **配置持久** - PlayerPrefs确保配置在场景间传递

## ⚠️ 注意事项

1. **不要在MainMenu场景添加Manager** - 只在SampleScene中
2. **确保场景名称正确** - "MainMenu" 和 "SampleScene"
3. **Build Settings中添加两个场景** - 否则切换会失败
4. **测试时清除PlayerPrefs**（可选）：
   ```csharp
   PlayerPrefs.DeleteAll(); // 清除所有
   ```

---

现在场景切换应该完全正常工作了！🎉
