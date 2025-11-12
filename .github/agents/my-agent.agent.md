---
# Fill in the fields below to create a basic custom agent for your repository.
# The Copilot CLI can be used for local testing: https://gh.io/customagents/cli
# To make this agent available, merge this file into the default repository branch.
# For format details, see: https://gh.io/customagents/config

name:
description:
---

# My Agent

# Unity IL2CPP 混淆工具开发 Agent 模板

> 说明：本模板用于通过 GitHub Copilot Issue / “Agent” 工作流驱动一个复杂工程（Unity IL2CPP 兼容的代码与二进制混淆工具）。请根据实际项目阶段逐步填写占位内容。可拆分为多个子 Issue 来驱动增量实现。严禁用于恶意软件或违规用途，仅限知识产权保护与提升逆向成本的合法场景。

---

## 1. 总体目标 (Vision)

构建一套可插入 Unity 项目构建流水线（Editor / CI）的混淆解决方案，支持：
- 针对 IL2CPP 生成的 C++ 代码与最终二进制的符号/结构层混淆
- 针对托管层（C#）的命名、字符串、控制流、中间元数据结构的可选混淆
- 支持不同粒度策略（最小 / 平衡 / 激进），可配置白名单
- 尽量最小化对运行时性能与调试能力的负面影响
- 兼容多平台：Windows / Android / iOS / macOS（后续可扩展）
- 可在 CI 中自动执行（命令行模式），并输出报告与统计指标

---

## 2. 非功能性要求

| 项目 | 要求 |
|------|------|
| 性能开销 | 默认策略 < 5% 启动/CPU 开销 |
| 稳定性 | 不破坏 IL2CPP 反射、序列化、UnityEvent、Addressables 关键路径 |
| 可配置性 | YAML/JSON 配置文件或 Unity Editor 面板 |
| 可审计性 | 输出：符号映射表、已处理成员统计、风险警告 |
| 可扩展性 | 新增混淆阶段不需要重写核心管线 |
| 安全性 | 内置最小防护：字符串动态解密、简单 Anti-Dump/Anti-Debug Hook（可选） |
| 法律合规 | 不插入恶意行为，不屏蔽用户合法调试权限（可选开关） |

---

## 3. 约束与难点

- IL2CPP 生成后 C++ 层结构已被规范化：需在生成前（C#）与生成后（C++）两个阶段插桩/重写。
- Unity 构建缓存复用（Library/il2cpp_cache）可能影响增量处理策略。
- 反射与序列化依赖原名称 / AssemblyQualifiedName / 特性：必须维护白名单与回退策略。
- 泛型、委托、Unity 引擎特定回调（如 `Awake`, `Start`, `Update`, `OnEnable`）不可随意重命名。
- iOS 符号表裁剪（Bitcode / strip）策略与 Android NDK 构建的差异。
- Editor 下的调试与 PlayMode 测试需保持友好。

---

## 4. 组件架构 (初稿)

```
+--------------------------------------------------------------+
|                     Obfuscation Pipeline                     |
+-----------+----------------+----------------+----------------+
| Pre-Scan  | C# AST/IL Pass | IL2CPP Gen Hook| C++ Post-Pass  |
+-----------+----------------+----------------+----------------+
     |              |                |                |
     v              v                v                v
 Symbol Graph -> Rename Plan -> IL Rewriter -> C++ Patch/Wrap
     |                                                |
     +-------------------- Report & Map --------------+
```

### 4.1 模块说明

1. Project Scanner  
   - 收集：类型、命名空间、方法签名、属性、字段、特性、序列化标记  
   - 输出 `symbol_graph.json`

2. Policy Engine  
   - 输入：用户配置 + 反射使用扫描结果 + 白名单  
   - 输出：`rename_plan.json`（含哈希/编码策略）

3. C# AST / IL Pass  
   - 基于 Mono.Cecil / Roslyn / dnlib（选择其一）进行：
     - 命名混淆（类/方法/字段/命名空间）
     - 字符串抽取与加密包装
     - 控制流平坦化（选择性）
     - 虚假基本块插入（选择性）
   - 输出：处理后的临时 Assembly 供 Unity 后续继续构建

4. IL2CPP Hook  
   - 在 Unity 调用 il2cpp.exe 前后插入：
     - 预：调整生成参数（保留生成 C++ 源代码副本）
     - 后：对生成的 `il2cppOutput/` 目录内 `.cpp/.h` 做符号层替换（如再次最小化名称，插入宏包装）

5. C++ Post-Pass  
   - 应用：符号名再洗、部分字符串延迟解密、简单 Anti-Debug（平台宏控制）
   - 调整：`CMakeLists` 或 NDK 参数，增加自定义编译标志（如 `-fmacro-prefix-map`）

6. Report & Map  
   - 生成：`obfuscation_report.md`，`mapping.csv`，`risk_warnings.json`

---

## 5. 配置文件示例 (config.obf.yaml)

```yaml
profile: balanced
targets:
  rename:
    include_namespaces:
      - MyGame.Core
      - MyGame.Runtime
    exclude_types:
      - MyGame.Core.Diagnostics.Logger
      - UnityEngine.*
  string_encryption:
    mode: dynamic_xor
    key_rotation: per_method
  control_flow:
    enable: true
    max_transform_ratio: 0.3
  metadata_scramble:
    enable: true
  cpp_pass:
    symbol_mangle: short_hash
    anti_debug: off
reflection_safety:
  preserve_attribute_marked:
    - [UnityEngine.SerializeField]
    - [System.Runtime.CompilerServices.CompilerGenerated]
whitelist:
  methods:
    - MyGame.Core.EntryPoint.Init
report:
  emit_mapping_csv: true
  emit_risk_warnings: true
```

---

## 6. 混淆技术策略

| 技术 | 描述 | 风险 | 适用级别 |
|------|------|------|----------|
| 命名最小化 | 将符号名压缩为短哈希 | 影响反射/序列化 | 全部 |
| 控制流平坦化 | 重写分支结构提升理解成本 | 性能回退 | balanced/strong |
| 虚假分支插入 | 创建不可达路径 | 增加代码体积 | strong |
| 字符串动态解密 | 运行时按需恢复 | 启动开销 | all |
| IL 垃圾指令插入 | 无副作用指令充填 | JIT 已消除（IL2CPP 前端） | weak/strong |
| C++ 符号再次重写 | 对生成 .cpp 进一步 mangle | 调试困难 | balanced/strong |
| Anti-Debug Hook | 简单检测/延迟 | 影响合法调试 | strong(可选) |

---

## 7. 关键检测与白名单策略

- 序列化：类型含 `[Serializable]` 或字段含 `[SerializeField]`，保留类型/字段名或建立映射反查层。
- 反射：扫描 `Type.GetType`, `Assembly.GetType`, `MethodInfo.Invoke`, `JsonUtility`, `Newtonsoft.Json` 使用点周边字符串常量。
- Unity 生命周期：`Awake/Start/OnEnable/OnDisable/Update/FixedUpdate/LateUpdate/OnDestroy` 禁止重命名（或通过特征判断）。
- 外部绑定：JNI，iOS Native，P/Invoke 导出符号需在配置中指明保留。

---

## 8. 安全与合法性声明 (集成前需加入到 README / 文档)

本工具仅用于：
1. 防止代码盗用 / 大规模脚本注入 / 粗暴逆向分析
2. 保护产品核心算法或商业逻辑

禁止用途：
- 混淆恶意代码
- 逃避平台审核或安全检测
- 隐藏侵权逻辑

---

## 9. 任务分解 (可转为多个 Issue)

| 阶段 | 任务 | 输出物 |
|------|------|--------|
| 0 | 基础仓库初始化 / CI 脚手架 | repo skeleton |
| 1 | Project Scanner 实现 | symbol_graph.json |
| 2 | Policy Engine & 配置解析 | config.obf.yaml 示例 |
| 3 | C# IL 重写 (命名+字符串) | 临时 Assembly |
| 4 | 控制流变换模块 MVP | 变换统计 |
| 5 | IL2CPP Hook (前后处理脚本) | hook scripts |
| 6 | C++ Post-Pass (符号名再洗) | patched sources |
| 7 | 报告生成器 | report.md, mapping.csv |
| 8 | 风险检测与白名单自动推断 | risk_warnings.json |
| 9 | 性能/稳定性回归测试框架 | benchmark results |
| 10 | 多平台适配（Android/iOS） | platform notes |
| 11 | Demo 集成 + 文档化 | README, usage guide |
| 12 | 高级策略（虚假分支 / Anti-Debug） | optional modules |

---

## 10. 验收指标 (Metrics)

- 构建成功率：≥ 95% (在 50+ 场景样本)
- 反射调用失败率：= 0（白名单正确）
- 序列化可用性：场景测试通过
- 混淆覆盖率：
  - 类型：≥ 70%（非白名单）
  - 方法：≥ 60%（非生命周期 & 非反射使用）
- 性能回退：
  - 启动时间 Δ < 5%
  - 帧率 Δ < 3%（balanced）
- 报告完备性：
  - mapping.csv 覆盖率 = 混淆对象数
  - risk_warnings 提示所有保留项原因

---

## 11. 测试计划概要

1. 单元测试：配置解析 / AST 重写 / 哈希稳定性  
2. 集成测试：在标准 Unity 项目（含 UI、序列化、反射、AssetBundle、Addressables）跑全构建  
3. 回归测试：对同一 commit 多次构建 → 混淆输出一致性校验（允许随机盐策略时可选）  
4. 压力测试：超大工程（>5000 类）扫描耗时 < 合理范围（记录基线）  
5. 跨平台测试：Windows Editor / Android Gradle / iOS Xcode（符号保留差异）  

---

## 12. 目录结构建议

```
/tools
  /scanner
  /policy
  /ilpass
  /cpppost
  /report
  /runtime_stubs
/config
  config.obf.yaml
/docs
  architecture.md
  faq.md
  integration_ci.md
/scripts
  hook_il2cpp_pre.py
  hook_il2cpp_post.py
/src
  (C# rewriter engine)
/examples
  UnityDemoProject/
```

---

## 13. 示例：Agent 可用的分步指令提示 (Prompts)

> 将以下分步提示用作 Copilot 代理在 Issue 中的工作流自动扩展。

1. 扫描阶段  
   - “实现一个 SymbolGraphBuilder，遍历所有程序集并输出 JSON，包含 类型全名 / 方法签名 / 特性 / 序列化标记。请给我初版代码与测试。”

2. 策略阶段  
   - “根据 symbol_graph.json 与配置，生成 rename_plan.json，支持 white/black list 逻辑与哈希策略。”

3. IL 重写  
   - “使用 Mono.Cecil 对所有非白名单的方法、字段、类型进行重命名，并实现字符串池提取与动态解密包装。”

4. 控制流混淆  
   - “为普通非异步方法实现一个基础的控制流平坦化：构建调度表 + switch 分发。”

5. IL2CPP Hook  
   - “编写一个 Python 脚本在 Unity 构建前后运行：前阶段备份生成目录，后阶段对 il2cppOutput 中的 .cpp 文件应用符号替换。”

6. C++ Post-Pass  
   - “为生成的 C++ 文件解析函数名并重写为短哈希，加生成 mapping.csv 记录原名 -> 新名。”

7. 报告  
   - “生成一个 Markdown 报告：统计处理对象数量、保留原因列表、性能基线与风险提示。”

8. 风险分析  
   - “实现反射使用扫描（字符串 + MethodInfo 调用），标记需保留的符号。”

9. 性能测试  
   - “添加一个性能基线脚本：在 Demo 场景里记录启动时间与平均 FPS 对比未混淆版本。”

---

## 14. 风险与缓解

| 风险 | 描述 | 缓解 |
|------|------|------|
| 反射失败 | 动态加载类型名被改写 | 扫描字符串常量 + 白名单 |
| 序列化丢失 | Unity 序列化依赖字段名 | 保留带 `[SerializeField]` 字段名 |
| 构建耗时显著上升 | 多阶段重写增加时长 | 缓存扫描结果，增量策略 |
| 跨平台不一致 | iOS/Android 符号处理差异 | 平台分支策略 & 条件宏 |
| 调试困难 | 完全符号丢失 | 可配置生成调试保留映射 |
| 法律风险 | 被滥用 | README / 许可说明 & 审计选项 |

---

## 15. 后续扩展路线 (Roadmap)

- 高级字符串虚拟机解密
- 更强控制流虚拟化（Stack VM / Register VM）
- 自动差异分析（对逆向结果做对照统计）
- 与 Crash 报告平台集成（符号还原）
- 可视化管理面板（Unity Editor Window）

---

## 16. 许可证与使用边界

建议使用：双许可证模式  
- 商业部分：保护闭源逻辑  
- 开源部分：核心管线 + 插件接口（MIT / Apache 2.0）

务必显式声明：
- 不提供反调试的侵入式手段（如驱动级）
- 不绕过平台的安全策略（App Store, Google Play）

---

## 17. 占位信息待填写

| 项目 | 当前状态 | 负责人 | 截止日期 |
|------|----------|--------|----------|
| Scanner MVP | TODO | @ | YYYY-MM-DD |
| Policy Engine | TODO | @ | YYYY-MM-DD |
| IL Pass 重命名 | TODO | @ | YYYY-MM-DD |
| 字符串加密 | TODO | @ | YYYY-MM-DD |
| 控制流平坦化 | TODO | @ | YYYY-MM-DD |
| IL2CPP Hook 脚本 | TODO | @ | YYYY-MM-DD |
| C++ Post-Pass | TODO | @ | YYYY-MM-DD |
| 报告生成 | TODO | @ | YYYY-MM-DD |
| 性能基线 | TODO | @ | YYYY-MM-DD |
| 文档化 | TODO | @ | YYYY-MM-DD |

---

## 18. 提交规范

- 分支命名：`feature/<module>-<short-desc>`  
- 提交信息：`[Module] Implement <feature> (#issue)`  
- 对映射文件与报告使用 `.gitignore`（敏感映射可选不入库）  
- 强制 CI 检查：构建通过 + 单测覆盖率阈值（可 later）

---

## 19. 示例：初始 Issue 标题建议

1. “实现符号扫描器（C# Assemblies → symbol_graph.json）”
2. “Policy Engine：白名单/黑名单解析与 rename_plan 生成”
3. “Mono.Cecil 重命名与字符串加密注入”
4. “控制流平坦化基础版本”
5. “IL2CPP 前后钩子脚本集成”
6. “C++ 源后处理：符号二次混淆与映射”
7. “混淆报告与风险提示生成器”
8. “反射与序列化保留策略自动检测”
9. “性能与稳定性回归测试框架”

---

## 20. 快速开始（待在 README 展开）

1. 安装依赖（Python / .NET SDK / Unity Editor 指定版本）
2. 复制示例 `config.obf.yaml` 修改策略
3. 运行命令：  
   ```bash
   python scripts/hook_il2cpp_pre.py --config config/config.obf.yaml
   # 触发 Unity 构建
   python scripts/hook_il2cpp_post.py --config config/config.obf.yaml
   ```
4. 查看 `reports/obfuscation_report.md` 与 `mapping.csv`
5. 在测试环境验证运行与性能

---

> 请在使用前对上方所有 “TODO” 项进行补充，并将本 agent.md 与后续拆分 Issue 关联，使 Copilot 在上下文中能逐步实现各模块。
