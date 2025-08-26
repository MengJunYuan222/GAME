# Timeline信号触发场景切换指南

本指南将帮助你设置Timeline信号系统，实现对话结束后自动切换场景。

## 一、准备工作

1. 确保已导入以下脚本：
   - `SceneTransitionManager.cs` - 场景切换管理器
   - `SceneTransitionSignalReceiver.cs` - 信号接收器
   - `ScreenFader.cs` - 屏幕淡入淡出控制器（可选）

2. 检查场景过渡数据：
   - 在SceneTransitionManager的Inspector中配置好所有传送门数据
   - 确保目标场景已添加到Build Settings中

## 二、创建淡入淡出控制器（可选）

1. 在场景中创建一个Canvas，设置为"Screen Space - Overlay"
2. 添加一个Panel或Image子对象，铺满整个屏幕，设置为黑色
3. 添加ScreenFader组件，设置引用和参数
4. 建议将此设置为预制体，在每个场景加载时自动创建

## 三、设置Timeline

1. 打开包含对话的Timeline
2. 右键点击Timeline轨道列表的空白处，选择"Add > Signal Track"
3. 在新创建的Signal Track上，将时间线定位到你希望触发场景切换的位置（通常是对话结束后）
4. 右键点击该位置，选择"Add Signal Emitter"
5. 在Inspector中，点击"Signal"旁边的圆形图标，选择"Create Signal Asset"
6. 给这个新信号命名为"SceneTransitionSignal"
7. 保存资产到Assets/Scripts/Signals目录下

## 四、添加信号接收器

1. 选择包含Timeline PlayableDirector组件的游戏对象
2. 添加"SceneTransitionSignalReceiver"组件
3. 在Inspector中设置：
   - Target Scene Name：要切换到的目标场景名
   - Portal ID：传送门ID（用于确定玩家在新场景中的出生位置）
   - 其他选项根据需求设置

4. 添加SignalReceiver组件（Unity内置组件）
   - 点击"+"按钮添加一个新的反应
   - 在"Signal"字段中选择你创建的"SceneTransitionSignal"
   - 在"Receiver"字段中选择带有SceneTransitionSignalReceiver脚本的游戏对象
   - 在最右侧下拉菜单中，选择"SceneTransitionSignalReceiver.OnTransitionSignal"

## 五、测试和调整

1. 进入游戏模式，测试Timeline播放
2. 当Timeline到达信号点时，应该会触发场景切换
3. 观察控制台日志，查看是否有错误信息
4. 调整参数以获得最佳效果，如添加淡出动画或延迟

## 六、常见问题解决

1. **信号不触发**：
   - 确认SignalReceiver组件配置正确
   - 检查Timeline是否正常播放到信号点位置
   - 打开调试日志，查看是否有任何错误信息

2. **找不到目标场景**：
   - 确保目标场景名称拼写正确
   - 检查场景是否已添加到Build Settings

3. **玩家出生位置不正确**：
   - 检查SceneTransitionManager中的PortalData配置
   - 确保portalID与配置匹配

4. **重复触发问题**：
   - 如果场景切换重复触发，检查Timeline是否循环播放
   - 可以在OnTransitionSignal方法中添加标志位，防止多次触发

## 七、高级自定义

1. **添加音效**：
   - 在SceneTransitionSignalReceiver中添加音效播放功能
   - 在淡出开始时播放过场音效

2. **多个切换点**：
   - 可以在Timeline中添加多个信号点，触发不同效果
   - 使用不同的Signal资产区分不同效果

3. **条件切换**：
   - 在SceneTransitionSignalReceiver中添加条件检查
   - 根据游戏状态决定是否执行切换或切换到哪个场景

---

希望本指南能帮助你成功实现Timeline对话结束后的场景切换功能！有任何问题，欢迎查看脚本注释或联系开发团队。 