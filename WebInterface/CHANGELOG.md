# WebGL API 更新说明

## 概述

本次更新为 Rain Player 添加了全新的 WebGL API，允许开发者直接通过 JavaScript 传入谱面 JSON、音频和封面图片，无需打包成 ZIP 文件。

## 问题背景

用户提出需求：
> 我要打包 WebGL，想直接通过 API 传入 .json，封面，音频，如何做？怎么修改？另一边我是 Vue+js 的项目，如何接入？

## 解决方案

### 1. 新增 JavaScript API 层

**修改文件**: `Assets/Plugins/WebGL/Helper.jslib`

新增功能：
- 添加直接资源存储结构（chartJson, audioData, coverData）
- 实现字节数组管理（用于音频和图片二进制数据）
- 提供 WebGL 与 Unity 之间的数据传递接口

关键方法：
- `WebGLHelper_HasDirectAssets()` - 检查是否有直接传入的资源
- `WebGLHelper_GetChartJson()` - 获取谱面 JSON
- `WebGLHelper_GetAudioData()` - 获取音频数据
- `WebGLHelper_GetCoverData()` - 获取封面数据

### 2. 扩展 C# WebGL 助手

**修改文件**: `Assets/Scripts/WebGLHelper.cs`

新增功能：
- 声明新的 P/Invoke 方法
- 实现字节数组包装器
- 提供统一的数据访问接口

### 3. 实现直接 API 加载路径

**修改文件**: `Assets/Scripts/StartPlay.cs`

新增功能：
- 添加 `ChartLoaderFromDirectAPI()` 方法
- 自动检测并选择加载方式（ZIP vs 直接 API）
- 从 JavaScript 读取并解析资源
- 保持向后兼容性

工作流程：
1. 检查是否有直接 API 资源（`HasDirectAssets()`）
2. 从 JavaScript 获取 JSON 字符串并反序列化
3. 获取音频和封面的二进制数据
4. 创建临时文件并加载到 Unity

### 4. 增强 JavaScript 前端 API

**修改文件**: `WebInterface/rain_player.js`

新增功能：
- 支持 `chartData` / `chartJson` 参数
- 支持 `audioUrl` / `audioBlob` / `audioData` 参数
- 支持 `coverUrl` / `coverBlob` / `coverData` 参数
- 异步获取 URL 资源并转换为字节数组
- 在 Unity 加载前将资源设置到 window 对象

API 使用方式：
```javascript
RainPlayer.instantiate({
    chartData: chartObject,      // 或 chartJson: jsonString
    audioUrl: "/audio.mp3",      // 或 audioBlob / audioData
    coverUrl: "/cover.jpg",      // 或 coverBlob / coverData
    container: document.body,
    // ...其他选项
});
```

## 技术实现细节

### 数据流

```
JavaScript (浏览器)
    ↓ 设置 window.WebGLHelper_Data
Unity WebGL (C#)
    ↓ P/Invoke 调用
JavaScript 库 (Helper.jslib)
    ↓ 返回数据
Unity (C#)
    ↓ 解析并使用
```

### 内存管理

1. **字符串管理**：使用 ID 映射，通过 `makeString()` 创建，`ReleaseString()` 释放
2. **字节数组管理**：使用独立的 ID 映射，通过 `makeByteArray()` 创建，`ReleaseByteArray()` 释放
3. **自动清理**：Unity 端在读取数据后立即释放 JavaScript 端的缓存

### 向后兼容性

- 保留原有的 `chartUrl` 参数支持
- 使用 `chartUrl` 时，自动走原有的 ZIP 加载流程
- 使用新 API 时，自动跳过 ZIP 下载和解压

## 文档和示例

### 1. API 文档
**文件**: `WebInterface/API.md`

内容：
- 完整的 API 参数说明
- 使用示例（URL、Blob、ArrayBuffer）
- 与传统方式的对比
- 注意事项和最佳实践

### 2. Vue.js 集成指南
**文件**: `WebInterface/VUE_INTEGRATION.md`

内容：
- 安装和配置步骤
- Vue 3 组件封装示例
- Composable 模式
- 完整应用示例
- TypeScript 类型定义
- 路由集成
- 文件上传示例

### 3. 交互式演示页面
**文件**: `WebInterface/example.html`

功能：
- 文件选择界面
- 游戏选项配置
- 实时加载和播放
- 完整的用户体验展示

### 4. Vue 组件
**文件**: `WebInterface/RainPlayer.vue`

功能：
- 开箱即用的 Vue 3 组件
- 支持所有 API 选项
- 加载状态处理
- 错误处理和重试
- 事件发射（loaded, error, backToHub）

### 5. API 测试工具
**文件**: `WebInterface/test-api.js`

功能：
- API 可用性检查
- 参数验证测试
- 自动化测试套件
- 使用示例展示

## Vue.js 集成示例

### 基础使用

```vue
<template>
  <RainPlayer
    :chart-data="chartData"
    audio-url="/audio.mp3"
    cover-url="/cover.jpg"
    @loaded="onLoaded"
    @back-to-hub="onBackToHub"
  />
</template>

<script>
import RainPlayer from '@/components/RainPlayer.vue';

export default {
  components: { RainPlayer },
  data() {
    return {
      chartData: { /* 谱面数据 */ }
    };
  },
  methods: {
    onLoaded(player) {
      console.log('Player loaded');
    },
    onBackToHub() {
      this.$router.push('/');
    }
  }
};
</script>
```

### 从 API 加载

```javascript
// 在 Vue 组件中
async mounted() {
  const response = await fetch('/api/charts/123');
  const data = await response.json();
  
  this.chartData = data.chart;
  this.audioUrl = data.audioUrl;
  this.coverUrl = data.coverUrl;
}
```

## 优势

1. **更快的加载速度**：跳过 ZIP 打包和解压缩
2. **更灵活的集成**：直接从 API 或文件上传获取数据
3. **更好的开发体验**：无需处理 ZIP 文件格式
4. **向后兼容**：不影响现有使用方式
5. **类型安全**：提供 TypeScript 类型定义

## 使用场景

### 场景 1：在线谱面库
从后端 API 直接获取谱面数据，无需生成 ZIP 文件。

```javascript
const response = await fetch('/api/charts/123');
const chart = await response.json();

RainPlayer.instantiate({
  chartData: chart.data,
  audioUrl: chart.audioUrl,
  coverUrl: chart.coverUrl,
  container: document.getElementById('game')
});
```

### 场景 2：用户上传
允许用户上传独立的 JSON、音频和图片文件。

```javascript
const chartFile = document.getElementById('chart').files[0];
const audioFile = document.getElementById('audio').files[0];
const coverFile = document.getElementById('cover').files[0];

const chartText = await chartFile.text();
const chartData = JSON.parse(chartText);

RainPlayer.instantiate({
  chartData: chartData,
  audioBlob: audioFile,
  coverBlob: coverFile,
  container: document.body
});
```

### 场景 3：Vue 单页应用
完全集成到 Vue 路由和状态管理中。

```vue
<template>
  <div class="game-page">
    <RainPlayer
      :chart-data="$store.state.currentChart"
      :audio-url="$store.getters.audioUrl"
      :cover-url="$store.getters.coverUrl"
      :options="gameOptions"
      @back-to-hub="$router.push('/')"
    />
  </div>
</template>
```

## 测试和验证

### 在浏览器控制台中测试

```javascript
// 1. 加载测试工具
// <script src="test-api.js"></script>

// 2. 运行所有测试
RainPlayerTest.runAllTests();

// 3. 查看使用示例
RainPlayerTest.showExamples();
```

### 使用交互式演示

1. 打开 `example.html`
2. 选择谱面 JSON、音频和封面文件
3. 调整游戏选项
4. 点击"开始游戏"

## 注意事项

1. **Storyboard 支持**：使用新 API 时，暂不支持 storyboard 图片资源（仅影响高级功能）
2. **CORS 配置**：使用 URL 方式时，确保服务器配置了正确的 CORS 头
3. **文件大小**：音频文件可能较大，使用 Blob 方式时注意内存占用
4. **浏览器兼容性**：需要支持 Fetch API 和 async/await 的现代浏览器

## 未来计划

- [ ] 支持 storyboard 资源的独立加载
- [ ] 添加资源预加载和缓存机制
- [ ] 提供 React 集成示例
- [ ] 添加更多自定义选项

## 贡献者

感谢 @SeRazon 提出需求并协助测试。

---

如有问题或建议，请在 GitHub Issues 中提出。
